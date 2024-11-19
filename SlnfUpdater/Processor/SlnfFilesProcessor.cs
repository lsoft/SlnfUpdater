using Microsoft.Build.Evaluation;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using SlnfUpdater.Processor.ProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlnfUpdater.Processor
{
    public sealed class SlnfFilesProcessor
    {
        private readonly string _slnfFolderPath;
        private readonly List<string> _slnfFiles;
        private readonly BuildFromRootsMode _fromRootsMode;

        public SlnfFilesProcessor(
            string slnfFolderPath,
            List<string> slnfFiles,
            BuildFromRootsMode fromRootsMode
            )
        {
            if (slnfFolderPath is null)
            {
                throw new ArgumentNullException(nameof(slnfFolderPath));
            }

            if (slnfFiles is null)
            {
                throw new ArgumentNullException(nameof(slnfFiles));
            }

            if (fromRootsMode is null)
            {
                throw new ArgumentNullException(nameof(fromRootsMode));
            }

            _slnfFolderPath = slnfFolderPath;
            _slnfFiles = slnfFiles;
            _fromRootsMode = fromRootsMode;
        }

        public void Process(
            )
        {
            var processedCount = 0;
            //foreach (var slnfFile in _slnfFiles)
            Parallel.ForEach(_slnfFiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, slnfFileName =>
            {
                try
                {
                    var slnfFullFilePath = Path.Combine(_slnfFolderPath, slnfFileName);

                    Console.WriteLine($"Processing {slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}...");

                    var result = ProcessSlnfFile(
                        slnfFullFilePath
                        );

                    Interlocked.Increment(ref processedCount);

                    Console.WriteLine($"""
({processedCount}/{_slnfFiles.Count}) Finished {slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}.
{result}
""");
                }
                catch (Exception excp)
                {
                    throw new InvalidOperationException(
                        $"Error occurred while processing {slnfFileName}",
                        excp
                        );
                }
            }
            );
        }

        private string ProcessSlnfFile(
            string slnfFullFilePath
            )
        {
            var structuredJson = new SlnfJsonStructured(_slnfFolderPath, slnfFullFilePath);

            if (_fromRootsMode.IsEnabled)
            {
                var resultMessage = structuredJson.CleanupExceptRoots(_fromRootsMode.AdditionalRootWildcards);
                structuredJson.SerializeToItsFile();

                Console.WriteLine(resultMessage);
            }

            var projectFileProcessor = new ScanForChangesProcessor(
                structuredJson
                );

            //process any found projects for its reference, which must be added or deleted to/from slnf
            foreach (var projectFileFullPath in structuredJson.EnumerateProjectFullPaths())
            {
                projectFileProcessor.ProcessProjectFromSlnf(
                    projectFileFullPath
                    );

            }

            projectFileProcessor.ApplyAndSave();

            return projectFileProcessor.BuildResultMessage();
        }

    }
}
