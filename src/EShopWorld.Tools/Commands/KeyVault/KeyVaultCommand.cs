using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Commands.KeyVault.Models;
using EShopWorld.Tools.Helpers;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;

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
                Description = "name of the application to generate the POCO for",
                ShortName = "m",
                LongName = "appName",
                ShowInHelpText = true)]
            [Required] 
            // ReSharper disable once MemberCanBePrivate.Global
            public string AppName { get; set; }

            [Option(
                Description = "optional namespace to use for generated POCOs",
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
            private readonly GeneratePocoProjectInternalCommand _projectFileCommand;
            private readonly IBigBrother _bigBrother;
            private readonly GeneratePocoClassInternalCommand _pocoClassCommand;

            public GeneratePOCOsCommand(KeyVaultClient kvClient, GeneratePocoProjectInternalCommand projectFileCommand, GeneratePocoClassInternalCommand pocoClassCommand, IBigBrother bigBrother)
            {
                _kvClient = kvClient;
                _projectFileCommand = projectFileCommand;
                _bigBrother = bigBrother;
                _pocoClassCommand = pocoClassCommand;
            }

            public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                //collect all secrets
                var secrets = await _kvClient.GetAllSecrets(KeyVaultName);

                Directory.CreateDirectory(OutputFolder);             
                                      
                //generate POCOs
               
                _pocoClassCommand.Render(new GeneratePocoClassViewModel
                {
                    Namespace = Namespace,
                    Fields = secrets.Select(i => new Tuple<string, bool>(
                        i.Identifier.Name,
                        false))
                }, Path.Combine(OutputFolder, Path.Combine(OutputFolder, "ConfigurationSecrets.cs")));
                
                //generate project file
                _projectFileCommand.Render(new GeneratePocoProjectViewModel {AppName = AppName, Version = Version},
                    Path.Combine(OutputFolder, $"{AppName}.csproj"));
      
                _bigBrother.Publish(new KeyVaultPOCOGeneratedEvent{AppName = AppName, Version = Version, Namespace = Namespace, KeyVaultName = KeyVaultName});

                return 0;
            }
        }
    }
}
