﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EShopWorld.Tools.Base;
using EShopWorld.Tools.Commands.KeyVault.Models;
using EShopWorld.Tools.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace EShopWorld.Tools.Commands.KeyVault
{
    [Command("keyvault", Description = "keyvault associated functionality"), HelpOption]
    [Subcommand(typeof(GeneratePOCOsCommand))]
    public class KeyVaultCommand : CommandBase
    {
        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.Error.WriteLine("You must specify a subcommand");
            app.ShowHelp();

#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
            return 1;
        }

        [Command("generatePOCOs", Description = "Generates the POCOs and the project file")]
        internal class GeneratePOCOsCommand 
        {
            [Option(
                Description = "application id - credential to access the vault",
                ShortName = "a",
                LongName = "appId",
                ShowInHelpText = true)]
            [Required]           
            public string AppId { get; set; }

            [Option(
                Description = "application secret given to the application id - credential to access the vault",
                ShortName = "s",
                LongName = "appSecret",
                ShowInHelpText = true)]
            [Required]        
            public string AppSecret { get; set; }

            [Option(
                Description = "tenant identifier hosting the vault",
                ShortName = "t",
                LongName = "tenantId",
                ShowInHelpText = true)]
            [Required]            
            public string TenantId { get; set; }

            [Option(
                Description = "name of the vault to open",
                ShortName = "k",
                LongName = "keyVault",
                ShowInHelpText = true)]
            [Required]
            public string KeyVaultName { get; set; }


            [Option(
                Description = "name of the application to generate the POCO for",
                ShortName = "m",
                LongName = "appName",
                ShowInHelpText = true)]
            [Required] 
            public string AppName { get; set; }

            [Option(
                Description = "optional namespace to use for generated POCOs",
                ShortName = "n",
                LongName = "namespace",
                ShowInHelpText = true)]
            [Required]
            public string Namespace { get; set; }

            [Option(
                Description = "name of the tag to denote obsolete status (defaults to 'Obsolete')",
                ShortName = "b",
                LongName = "obsoleteTag",
                ShowInHelpText = true)]
            public string ObsoleteTagName { get; set; } = "Obsolete";

            [Option(
                Description = "name of the tag denoting type name assignation (class) (defaults to 'Type')",
                ShortName = "g",
                LongName = "typeTag",
                ShowInHelpText = true)]
            public string TypeTagName { get; set; } = "Type";

            [Option(
                Description = "name of the tag denoting name of the field (defaults to 'Name')",
                ShortName = "f",
                LongName = "nameTag",
                ShowInHelpText = true)]
            public string NameTagName { get; set; } = "Name";

            [Option(
                Description = "folder to output generated files into (defaults to '.')",
                ShortName = "o",
                LongName = "output",
                ShowInHelpText = true)]
            public string OutputFolder { get; set; } = ".";

            [Option(
                Description = "version number to inject into nuspec",
                ShortName = "v",
                LongName = "version",
                ShowInHelpText = true)]
            [Required]    
            public string Version { get; set; }

            private int OnExecute(IConsole console)
            {
                //collect all secrets
                var secrets = KeyValueAccess.GetAllSecrets(TenantId, AppId, AppSecret, KeyVaultName, TypeTagName, NameTagName, AppName)
                    .GetAwaiter().GetResult();

                Directory.CreateDirectory(OutputFolder);

                // Initialize the necessary services
                var services = new ServiceCollection();
                AspNetRazorEngineServiceSetup.ConfigureDefaultServices<GeneratePocoClassInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                var provider = services.BuildServiceProvider();
                var serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
                using (serviceScope.CreateScope())
                {                   
                    //generate POCOs
                    var pocoCommand = provider.GetRequiredService<GeneratePocoClassInternalCommand>();
                    pocoCommand.Render(new GeneratePocoClassViewModel
                    {
                        Namespace = Namespace,
                        Fields = secrets.Select(i => new Tuple<string, bool>(
                            i.Tags != null && i.Tags.ContainsKey(NameTagName) ? i.Tags[NameTagName] : i.Identifier.Name,
                            i.Tags != null && i.Tags.ContainsKey(ObsoleteTagName) && Convert.ToBoolean(i.Tags[ObsoleteTagName])))
                    }, Path.Combine(OutputFolder, Path.Combine(OutputFolder, "ConfigurationSecrets.cs")));
                }

                AspNetRazorEngineServiceSetup.ConfigureDefaultServices<GeneratePocoProjectInternalCommand>(services, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                provider = services.BuildServiceProvider();
                serviceScope = provider.GetRequiredService<IServiceScopeFactory>();
                using (serviceScope.CreateScope())
                {
                    //generate project file
                    var projectCommand = provider.GetRequiredService<GeneratePocoProjectInternalCommand>();
                    projectCommand.Render(new GeneratePocoProjectViewModel {AppName = AppName, Version = Version},
                        Path.Combine(OutputFolder, $"{AppName}.csproj"));
                }

                return 0;
            }
        }
    }
}
