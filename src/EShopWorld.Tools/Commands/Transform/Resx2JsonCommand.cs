using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace EShopWorld.Tools.Commands.Transform
{
    /// <summary>
    /// A command to transform and merge RESX files into their angular JSON equivalents.
    /// </summary>
    [Command("transform", Description = "Transforms Resx files into Json for use in Angular Projects"), HelpOption]
    [Subcommand("run", typeof(Run))] 
    public class Resx2JsonCommand : CommandBase
    {       
        /// <summary>
        /// 
        /// </summary>
        public Resx2JsonCommand()
        { }
               
        /// <summary>
        /// Runs this command.
        /// </summary>
        private int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("Please specify a subcommand");
            app.ShowHelp();

#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif

            return 1;
        }
        
        /// <summary>
        /// 
        /// </summary>
        [Command("run", Description = "Runs the transforms")]
        protected internal class Run : TransfromBase
        {
            /// <summary>
            /// The path to the folder that contains the RESX files. Can be absolute or relative.
            /// </summary>
            [Option(
                Description = "The source folder containing the RESX files. Can be absolute or relative.",
                ShortName = "s",
                LongName = "resx-project",
                ShowInHelpText = true)]
            [Required]
            public string ResxProject { get; set; }

            /// <summary>
            /// The path to the folder that will contain the JSON files. Can be absolute or relative.
            /// </summary>
            [Option(
                Description = "The target folder containing the JSON files. Can be absolute or relative.",
                ShortName = "o",
                LongName = "json-project",
                ShowInHelpText = true)]
            [Required]
            public string JsonProject { get; set; }

            private void OnExecute(IConsole console)
            {
                var sourceFolder = Path.GetFullPath(ResxProject);
                var outputFolder = Path.GetFullPath(JsonProject);

                var pathHelper = new PathHelper();
                pathHelper.CreateDirectory(outputFolder);

                var resxFiles = Directory.GetFiles(sourceFolder, "*.resx", SearchOption.AllDirectories)
                                         .Select(Path.GetFullPath);

                foreach (var resxFile in resxFiles.OrderBy(f => f?.Split('\\').Length))
                {
                    var fileContent = GetMergedResource(resxFile);

                    var json = ConvertResx2Json(fileContent);

                    // always insert culture on JSON file names using the default culture constant
                    var jsonFilePath = GetJsonPath(pathHelper.EnforceSameFolders(sourceFolder, outputFolder, resxFile), resxFile);
                    File.WriteAllText(jsonFilePath, json);
                }
            }
        }
    }
}
