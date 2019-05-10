using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Eshopworld.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Azure.KeyVault;
using EShopWorld.Tools.Common;
using Kusto.Cloud.Platform.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EShopWorld.Tools.Commands.KeyVault
{
    [Command("export", Description = "export keyvault contents to JSON"), HelpOption]
    public class ExportKeyVaultCommand
    {
        private readonly KeyVaultClient _kvClient;
        private readonly IBigBrother _bigBrother;

        [Option(
            Description = "name of the vault to open",
            ShortName = "k",
            LongName = "keyVault",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string KeyVaultName { get; set; }
        
        [Option(
            Description = "file path to export to",
            ShortName = "o",
            LongName = "output",
            ShowInHelpText = true)]
        [Required]
        // ReSharper disable once MemberCanBePrivate.Global
        public string OutputFilePath { get; set; }

        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="kvClient">key vault client</param>
        /// <param name="bigBrother">big brother instance</param>
        public ExportKeyVaultCommand(KeyVaultClient kvClient, IBigBrother bigBrother)
        {
            _kvClient = kvClient;
            _bigBrother = bigBrother;
        }

        /// <summary>
        /// execute command
        /// </summary>
        /// <param name="app">app instance</param>
        /// <param name="console">console instance</param>
        /// <returns>return code</returns>
        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            //load secrets
            var secrets = await _kvClient.GetAllSecrets(KeyVaultName);

            var topLevel = new JObject();

            secrets.ForEach(s => topLevel[s.SecretIdentifier.Name.Replace("--", ":")] = s.Value);
            
            using (var stream = new StreamWriter(new FileStream(OutputFilePath, FileMode.Create)))
            {
                JsonSerializer.Create(new JsonSerializerSettings() {Formatting = Formatting.Indented})
                    .Serialize(stream, topLevel);
            }

            return 0;
        }
    }
}
