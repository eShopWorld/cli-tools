using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Commands.AzScan;
using EShopWorld.Tools.Common;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace EShopWorld.Tools.Commands.KeyVault
{
    [Command("generatePOCOs", Description = "Generates the POCOs and the project file")]
    // ReSharper disable once InconsistentNaming
    internal class GeneratePOCOsCommand
    {
        [Option(
            Description = "name of the vault to open",
            ShortName = "k",
            LongName = "keyVault",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string KeyVaultName { get; set; }

        [Option(
            Description = "namespace for generated POCOs",
            ShortName = "n",
            LongName = "namespace",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string Namespace { get; set; }

        [Option(
            Description = "folder to output generated files into (defaults to '.')",
            ShortName = "o",
            LongName = "output",
            ShowInHelpText = true)]
        // ReSharper disable once MemberCanBePrivate.Global
        public string OutputFolder { get; set; } = ".";

        [Option(
            Description = "name of the application to generate the POCO for",
            ShortName = "m",
            LongName = "appName",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string AppName { get; set; }

        [Option(
            Description = "version number to inject into NuSpec",
            ShortName = "v",
            LongName = "version",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string Version { get; set; }

        [Option(
            Description = "evolution mode",
            ShortName = "e",
            LongName = "evoMode",
            ShowInHelpText = true)]
        // evolution mode signals to interpret POCO types and fields as evolution configuration management types (not generic ones)
        // current usage is for Dns cascade
        // ReSharper disable once MemberCanBePrivate.Global
        public bool EvoMode { get; set; } = false;

        private readonly KeyVaultClient _kvClient;
        private readonly IBigBrother _bigBrother;

        public GeneratePOCOsCommand(KeyVaultClient kvClient, IBigBrother bigBrother)
        {
            _kvClient = kvClient;
            _bigBrother = bigBrother;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {

            ////collect all secrets - enabled and disabled
            var secrets = (await _kvClient.GetAllSecrets(KeyVaultName))
                .Select(s => new Secret(s.SecretIdentifier.Name)).ToList();

            secrets.AddRange((await _kvClient.GetDeletedSecrets(KeyVaultName))
                .Select(s => new Secret(s.Identifier.Name, false)));

            Directory.CreateDirectory(OutputFolder);

            //generate POCOs
            // ReSharper disable once IdentifierTypo
            var poco = GeneratePOCOSyntaxTree(BuildTree(secrets));
            File.WriteAllText(Path.Combine(OutputFolder, "Configuration.cs"), poco.NormalizeWhitespace().ToFullString());

            //generate csproj
            // ReSharper disable once StringLiteralTypo
            var csproj = ProjectFileBuilder.CreateEswNetStandard20NuGet(AppName, Version,
                $"c# poco representation of the {AppName} configuration Azure KeyVault",
                packageDependencies: EvoMode ? new[] {("Eshopworld.Core", "2.*")} : null);

            File.WriteAllText(Path.Combine(OutputFolder, $"{AppName}.csproj"), csproj.GetContent());

            _bigBrother.Publish(new KeyVaultPOCOGeneratedEvent { AppName = AppName, Version = Version, Namespace = Namespace, KeyVaultName = KeyVaultName });

            return 0;
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private SyntaxNode GeneratePOCOSyntaxTree(ConfigurationNode tree)
        {
            var @namespace = NamespaceDeclaration(ParseName(Namespace))
                .AddUsings(UsingDirective(ParseName("System")));

            var topClass = BuildClassHierarchy(tree);

            @namespace = @namespace.AddMembers(topClass);

            return @namespace;
        }

        private ClassDeclarationSyntax BuildClassHierarchy(ConfigurationNode node)
        {
            var currentClass = ClassDeclaration($"{node.Name}Configuration").AddModifiers(Token(SyntaxKind.PublicKeyword));

            if (!string.IsNullOrWhiteSpace(node.BaseType))
            {
                currentClass = currentClass.AddBaseListTypes(SimpleBaseType(ParseTypeName(node.BaseType)));
            }

            var innerMembers = new List<MemberDeclarationSyntax>();
            var innerClasses = new List<MemberDeclarationSyntax>();

            foreach (var subNode in node.Children)
            {
                TypeSyntax memberType;

                if (subNode.Children.Any())
                {
                    var subclass = BuildClassHierarchy(subNode);
                    innerClasses.Add(subclass);
                    memberType = subNode.IsArray
                        ? ArrayType(ParseTypeName(subclass.Identifier.Text), new SyntaxList<ArrayRankSpecifierSyntax>(ArrayRankSpecifier()))
                        : ParseTypeName(subclass.Identifier.Text);
                }
                else
                {
                    memberType = ParseTypeName("string");
                }

                var member = PropertyDeclaration(memberType,
                        subNode.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                if (!subNode.Enabled)
                {
                    member = member.AddAttributeLists(AttributeList(
                        SingletonSeparatedList(
                            Attribute(IdentifierName(typeof(ObsoleteAttribute).FullName),
                                AttributeArgumentList(Token(SyntaxKind.OpenParenToken),
                                    SingletonSeparatedList(
                                        AttributeArgument(
                                            ParseExpression(
                                                "\"The underlying platform resource is no longer provisioned\""))),
                                    Token(SyntaxKind.CloseParenToken))))));
                }

                innerMembers.Add(member);

            }

            return currentClass
                .AddMembers(innerMembers.ToArray())
                .AddMembers(innerClasses.ToArray());
        }

        private ConfigurationNode BuildTree(IEnumerable<Secret> secrets)
        {
            var topLevel = new ConfigurationNode { Name = AppName };

            var postProcessDnsSecrets = new List<ConfigurationNode>();

            foreach (var secret in secrets)
            {
                var tokens = secret.Name.Split("--");
                var valueNode = topLevel;
                ConfigurationNode secretApiLevelNode = null;
                for (var x = 0; x < tokens.Length; x++)
                {
                    var token = tokens[x];

                    if (token.IsUnsignedInt())
                    {
                        valueNode.IsArray =
                            true; //skip this level but treat it as an index to array represented by level above
                        continue;
                    }

                    valueNode = valueNode.AddChild(token.SanitizePropertyName(),
                        (x + 1) != tokens.Length || secret.Enabled /* enabled is considered only at leaf level*/);

                    if (EvoMode && x == 1)
                    {
                        secretApiLevelNode = valueNode;
                    }
                }

                //check for expected naming convention - Platform--ABC-{Global|Cluster|Proxy}
                if (!EvoMode || tokens.Length != 3 ||
                    !AzScanDNSCommand.PlatformPrefix.Equals(tokens[0], StringComparison.Ordinal)) continue;

                /*evo mode enabled and secrets prefixed with "Platform"
                 so now decorate second level with the interface and ensure all fields are present 
                 (in case some secrets are not generated e.g. reverse proxy not enabled) */

                if (secretApiLevelNode == null || !secretApiLevelNode.Children.Any())
                    continue;

                postProcessDnsSecrets.Add(secretApiLevelNode);
            }

            foreach (var node in postProcessDnsSecrets.Distinct())
            {
                if (node.Children.All(n => !n.Enabled))
                {
                    continue;
                }
                //"old" secrets, ignore for this purpose

                node.BaseType = typeof(IDnsConfigurationCascade).FullName;

                if (!node.Children.Any(n => AzScanDNSCommand.ProxySecretSuffix.Equals(n.Name)))
                {
                    node.AddChild(AzScanDNSCommand.ProxySecretSuffix);
                }

                if (!node.Children.Any(n => AzScanDNSCommand.ClusterSecretSuffix.Equals(n.Name)))
                {
                    node.AddChild(AzScanDNSCommand.ClusterSecretSuffix);
                }

                if (!node.Children.Any(n => AzScanDNSCommand.FrontDoorSecretSuffix.Equals(n.Name)))
                {
                    node.AddChild(AzScanDNSCommand.FrontDoorSecretSuffix);
                }
            }

            return topLevel;
        }

        private struct Secret
        {
            internal string Name { get; }
            internal bool Enabled { get; }

            internal Secret(string name, bool enabled = true)
            {
                Name = name;
                Enabled = enabled;
            }
        }

        internal class ConfigurationNode
        {
            internal string Name { get; set; }
            internal bool IsArray { get; set; }
            internal readonly List<ConfigurationNode> Children = new List<ConfigurationNode>();
            internal bool Enabled { get; set; } = true;
            internal string BaseType { get; set; }
            internal ConfigurationNode AddChild(string name, bool enabled = true)
            {
                ConfigurationNode existing;

                if ((existing = Children.FirstOrDefault(n => n.Name.Equals(name, StringComparison.Ordinal))) != null)
                {
                    return existing;
                }

                var newNode = new ConfigurationNode { Name = name, Enabled = enabled };
                Children.Add(newNode);
                return newNode;
            }
        }
    }

}
