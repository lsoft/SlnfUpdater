using System;
using System.IO;
using System.Linq;
using Pastel;
using SlnfUpdater.Helper;
using SlnfUpdater.Processor;

namespace SlnfUpdater
{
    file static class Program
    {
        private const int SuccessExitCode = 0;
        private const int FailureExitCode = 1;

        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception excp)
            {
                Console.Error.WriteLine(excp.BuildMessageChain().Pastel(ColorTable.ErrorColor));
                return FailureExitCode;
            }
        }

        private static int Run(string[] args)
        {
            if (!CommandLineOptions.TryParse(args, out var options, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine(CommandLineOptions.Usage);
                return FailureExitCode;
            }

            var before = DateTime.Now;

            if (!MsBuildRegistration.TryRegister(out var msbuildError))
            {
                Console.Error.WriteLine(msbuildError);
                return FailureExitCode;
            }

            var slnfFolderPath = Path.GetFullPath(options.SlnfFolderPath);
            var slnfFileMask = options.SlnfFileMask;

            if (!Directory.Exists(slnfFolderPath))
            {
                Console.Error.WriteLine($"Folder {slnfFolderPath} does not exist.");
                return FailureExitCode;
            }

            Console.WriteLine($"Found folder {slnfFolderPath}");

            var slnfFiles = Directory.GetFiles(slnfFolderPath, slnfFileMask, SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f).Name)
                .ToList();
            if (slnfFiles.Count == 0)
            {
                Console.Error.WriteLine($"Folder {slnfFolderPath} does not contains a slnf files.");
                return FailureExitCode;
            }

            Console.WriteLine($"Found {slnfFiles.Count} slnf files:");

            foreach (var slnfFile in slnfFiles)
            {
                Console.WriteLine($"    {slnfFile.Pastel(ColorTable.SolutionProjectColor)}");
            }

            var processor = new SlnfFilesProcessor(
                slnfFolderPath,
                slnfFiles,
                options.FromRootsMode
                );

            var failedCount = processor.Process();

            var after = DateTime.Now;
            Console.WriteLine($"Finished (taken {(after - before).ToString().Pastel(ColorTable.TimeColor)}).");

            if (failedCount > 0)
            {
                Console.Error.WriteLine($"{failedCount} of {slnfFiles.Count} slnf files failed.".Pastel(ColorTable.ErrorColor));
                return FailureExitCode;
            }

            return SuccessExitCode;
        }
    }
}
