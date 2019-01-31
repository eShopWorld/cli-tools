using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Helpers;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EShopWorld.Tools.Commands.KeyVault
{
    /// <summary>
    /// key vault top level command 
    /// </summary>
    [Command("keyvault", Description = "keyvault associated functionality"), HelpOption]
    [Subcommand(typeof(GeneratePOCOsCommand))]
    public class KeyVaultCommand
    {
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
                Description = "name of the class for the generated top level POCO",
                ShortName = "c",
                LongName = "className",
                ShowInHelpText = true)]
            [Required] 
            // ReSharper disable once MemberCanBePrivate.Global
            public string ClassName { get; set; }

            [Option(
                Description = "optional namespace for generated POCOs",
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
                //collect all secrets
                var secrets =  await _kvClient.GetAllSecrets(KeyVaultName);

                Directory.CreateDirectory(OutputFolder);             
                                      
                //generate POCOs
                var tree = BuildTree(secrets);
                var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(Namespace))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));                

                var topClass = BuildClassHierarchy(tree);

                @namespace = @namespace.AddMembers(topClass);

                //TODO: refresh
                //_bigBrother.Publish(new KeyVaultPOCOGeneratedEvent{AppName = AppName, Version = Version, Namespace = Namespace, KeyVaultName = KeyVaultName});

                return 0;
            }

            private ClassDeclarationSyntax BuildClassHierarchy(ConfigurationNode node)
            {                
                var currentClass = SyntaxFactory.ClassDeclaration($"{node.Name.ToPascalCase()}Type").AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                var innerMembers = new List<MemberDeclarationSyntax>();
                var innerClasses = new List<MemberDeclarationSyntax>();

                foreach (var subNode in node.Children)
                {

                    TypeSyntax memberType = null;

                    if (subNode.Children.Any())
                    {
                        var subclass = BuildClassHierarchy(subNode);
                        innerClasses.Add(subclass);
                        memberType = SyntaxFactory.ParseTypeName(subclass.Identifier.Text);
                    }
                    else
                    {
                        memberType = SyntaxFactory.ParseTypeName("string");
                    }

                    innerMembers.Add(SyntaxFactory
                        .PropertyDeclaration(memberType,
                            subNode.Name.ToPascalCase())
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));
                    
                }

                return currentClass
                    .AddMembers(innerMembers.ToArray())
                    .AddMembers(innerClasses.ToArray());
            }

            internal ConfigurationNode BuildTree(IList<SecretBundle> secrets)
            {
                var topLevel = new ConfigurationNode {Name = ClassName};

                foreach (var secret in secrets)
                {
                    var tokens = secret.SecretIdentifier.Name.Split("--");
                    ConfigurationNode valueNode = topLevel;
                    foreach (var token in tokens)
                    {
                        valueNode = valueNode.AddGetChild(token);
                    }

                    valueNode.Value = secret.Value;
                }

                return topLevel;
            }

            internal class ConfigurationNode
            {
                internal string Name { get; set; }
                internal string Value { get; set; }

                internal List<ConfigurationNode> Children = new List<ConfigurationNode>();

                internal ConfigurationNode AddGetChild(string name)
                {
                    ConfigurationNode existing;

                    if ((existing = Children.FirstOrDefault(n => n.Name.Equals(name, StringComparison.Ordinal))) != null)
                    {
                        return existing;
                    }

                    var newNode = new ConfigurationNode {Name = name};
                    Children.Add(newNode);
                    return newNode;
                }
            }
        }
    }
}
