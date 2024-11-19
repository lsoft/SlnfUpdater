using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using SlnfUpdater.Processor;

namespace SlnfUpdater
{

    internal class Program
    {
        public const string RebuildFromRootsKey = "-rebuild-from-roots";
        public const string AdditionalRootsKey = "-additional-roots:";

        static void Main(string[] args)
        {
            var before = DateTime.Now;

            var roots = ScanForRebuildRoots(args);

            //MSBuildLocator.RegisterMSBuildPath(".");
            MSBuildLocator.RegisterDefaults();

            var slnfFolderPath = Path.GetFullPath(args[0]);
            var slnfFileMask = args[1];

            if (!Directory.Exists(slnfFolderPath))
            {
                Console.WriteLine($"Folder {slnfFolderPath} does not exist.");
                return;
            }

            Console.WriteLine($"Found folder {slnfFolderPath}");

            var slnfFiles = Directory.GetFiles(slnfFolderPath, slnfFileMask, SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f).Name)
                .ToList();
            if (slnfFiles.Count == 0)
            {
                Console.WriteLine($"Folder {slnfFolderPath} does not contains a slnf files.");
                return;
            }

            Console.WriteLine($"Found {slnfFiles.Count} slnf files:");

            foreach (var slnfFile in slnfFiles)
            {
                Console.WriteLine($"    {slnfFile.Pastel(ColorTable.SolutionProjectColor)}");
            }

            var processor = new SlnfFilesProcessor(
                slnfFolderPath,
                slnfFiles,
                roots
                );
            processor.Process();

            var after = DateTime.Now;
            Console.WriteLine($"Finished (taken {(after - before).ToString().Pastel(ColorTable.TimeColor)}).");
        }

        private static BuildFromRootsMode ScanForRebuildRoots(
            string[] args
            )
        {
            if (args is null || args.Length == 0)
            {
                return BuildFromRootsMode.Disabled;
            }

            var rfrArg = args.FirstOrDefault(a => a.StartsWith(RebuildFromRootsKey));
            if (rfrArg is null)
            {
                return BuildFromRootsMode.Disabled;
            }

            var arArgs = args.Where(a => a.StartsWith(AdditionalRootsKey)).ToList();
            if (arArgs.Count <= 0)
            {
                return BuildFromRootsMode.Enabled;
            }

            var roots = new List<string>();
            foreach (var arArg in arArgs)
            {
                var tail = arArg.Substring(AdditionalRootsKey.Length);
                var tails = tail.Split(";");
                roots.AddRange(tails);
            }

            return BuildFromRootsMode.WithAdditionalRoots(roots);
        }
    }
}
