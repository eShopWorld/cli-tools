using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
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
    /// <summary>
    /// key vault top level command 
    /// </summary>
    
    [Command("keyvault", Description = "keyvault associated functionality"), HelpOption]
    [Subcommand(typeof(GeneratePOCOsCommand))]
    public class KeyVaultCommand
    {
        /// <summary>
        /// output appropriate message to denote sub-command is missing
        /// </summary>
        /// <param name="app">app instance</param>
        /// <param name="console">console</param>
        /// <returns></returns>
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {            
            console.Error.WriteLine("You must specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }

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
                var secrets =  (await _kvClient.GetAllSecrets(KeyVaultName))
                    .Select(s=> new Secret(s.SecretIdentifier.Name)).ToList();

                secrets.AddRange((await _kvClient.GetDisabledSecrets(KeyVaultName))
                    .Select(s=>new Secret(s.Identifier.Name, false)));

                Directory.CreateDirectory(OutputFolder);             

                //generate POCOs
                // ReSharper disable once IdentifierTypo
                var poco = GeneratePOCOSyntaxTree(BuildTree(secrets));
                File.WriteAllText(Path.Combine(OutputFolder, "Configuration.cs"), poco.NormalizeWhitespace().ToFullString());

                //generate csproj
                // ReSharper disable once StringLiteralTypo
                var csproj = ProjectFileBuilder.CreateEswNetStandard20NuGet(AppName, Version, $"c# poco representation of the {AppName} configuration Azure KeyVault");
                File.WriteAllText(Path.Combine(OutputFolder, $"{AppName}.csproj"), csproj.GetContent());
               
                _bigBrother.Publish(new KeyVaultPOCOGeneratedEvent{AppName = AppName, Version = Version, Namespace = Namespace, KeyVaultName = KeyVaultName});

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
                var currentClass = ClassDeclaration($"{node.Name}Type").AddModifiers(Token(SyntaxKind.PublicKeyword));

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
                                        Token(SyntaxKind.CloseParenToken ))))));
                    }

                    innerMembers.Add(member);
                    
                }

                return currentClass
                    .AddMembers(innerMembers.ToArray())
                    .AddMembers(innerClasses.ToArray());
            }

            private ConfigurationNode BuildTree(IEnumerable<Secret> secrets)
            {
                var topLevel = new ConfigurationNode {Name = AppName};
             
                foreach (var secret in secrets)
                {
                    var tokens = secret.Name.Split("--");
                    var valueNode = topLevel;
                    for (var x=0; x<tokens.Length; x++)
                    {
                        var token = tokens[x];
                        if (token.IsUnsignedInt())
                        {
                            valueNode.IsArray = true; //skip this level but treat it as an index to array represented by level above
                            continue;
                        }

                        valueNode = valueNode.AddChild(token.SanitizePropertyName(),
                            (x + 1) != tokens.Length || secret.Enabled); //enabled is considered only at leaf level
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

                internal ConfigurationNode AddChild(string name, bool enabled = true)
                {
                    ConfigurationNode existing;

                    if ((existing = Children.FirstOrDefault(n => n.Name.Equals(name, StringComparison.Ordinal))) != null)
                    {
                        return existing;
                    }

                    var newNode = new ConfigurationNode {Name = name, Enabled = enabled};
                    Children.Add(newNode);
                    return newNode;
                }
            }
        }
    }
}
