using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using SlnfUpdater.Processor;

namespace SlnfUpdater
{
    file static class Program
    {
        private const string RebuildFromRootsKey = "-rebuild-from-roots";
        private const string AdditionalRootsKey = "-additional-roots:";

        static void Main(string[]? args)
        {
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("You need to provide at least 2 arguments: path to the root directory and slnf file mask!");
                return;
            }

            var before = DateTime.Now;

            var roots = ScanForRebuildRoots(args);

            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch
            {
                // failed to register default, registering in dirty way
                MSBuildLocator.RegisterMSBuildPath(Directory.GetCurrentDirectory());
            }

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
            string[]? args
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
