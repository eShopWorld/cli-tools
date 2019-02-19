using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using EShopWorld.Tools.Common;
using EShopWorld.Tools.Telemetry;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.Transform
{
    /// <summary>
    /// A command to transform and merge RESX files into their angular JSON equivalents.
    /// </summary>
    [Command("transform", Description = "data transformation tool set"), HelpOption]
    [Subcommand(typeof(Resx2JsonCommand))] 
    public class TransformCommand 
    {                            
        /// <summary>
        /// Runs this command.
        /// </summary>
        public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("Please specify a sub-command");
            app.ShowHelp();

            return Task.FromResult(1);
        }
        
        /// <summary>
        /// command implementation for resx2json
        /// </summary>
        [Command("resx2json", Description = "Resx 2 json transform")]
        protected internal class Resx2JsonCommand : TransformBase
        {
            private readonly IBigBrother _bigBrother;
            private readonly PathService _pathService;

            public Resx2JsonCommand(IBigBrother bigBrother, PathService pathService)
            {
                _bigBrother = bigBrother;
                _pathService = pathService;
            }

            /// <summary>
            /// The path to the folder that contains the RESX files. Can be absolute or relative.
            /// </summary>
            [Option(
                Description = "The source folder containing the RESX files. Can be absolute or relative.",
                ShortName = "r",
                LongName = "resx-project",
                ShowInHelpText = true)]
            [Required]
            public string ResxProject { get; set; }

            /// <summary>
            /// The path to the folder that will contain the JSON files. Can be absolute or relative.
            /// </summary>
            [Option(
                Description = "The target folder containing the JSON files. Can be absolute or relative.",
                ShortName = "j",
                LongName = "json-project",
                ShowInHelpText = true)]
            [Required]
            public string JsonProject { get; set; }

            public Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                var sourceFolder = Path.GetFullPath(ResxProject);
                var outputFolder = Path.GetFullPath(JsonProject);

                _pathService.CreateDirectory(outputFolder);

                var resxFiles = Directory.GetFiles(sourceFolder, "*.resx", SearchOption.AllDirectories)
                                         .Select(Path.GetFullPath);

                foreach (var resxFile in resxFiles.OrderBy(f => f?.Split('\\').Length))
                {
                    var fileContent = GetMergedResource(resxFile);

                    var json = ConvertResx2Json(fileContent);

                    // always insert culture on JSON file names using the default culture constant
                    var jsonFilePath = GetJsonPath(_pathService.EnforceSameFolders(sourceFolder, outputFolder, resxFile), resxFile);
                    File.WriteAllText(jsonFilePath, json);
                }

                _bigBrother.Publish(new ResxTransformedEvent{ResxProject = ResxProject});

                return Task.FromResult(0);
            }
        }
    }
}
