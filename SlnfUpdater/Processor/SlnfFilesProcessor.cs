using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using SlnfUpdater.Processor.ProjectFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            _slnfFolderPath = slnfFolderPath ?? throw new ArgumentNullException(nameof(slnfFolderPath));
            _slnfFiles = slnfFiles ?? throw new ArgumentNullException(nameof(slnfFiles));
            _fromRootsMode = fromRootsMode ?? throw new ArgumentNullException(nameof(fromRootsMode));
        }

        /// <summary>
        /// Processes all slnf files; a failure in one file does not stop processing of others.
        /// </summary>
        /// <returns>Count of failed slnf files.</returns>
        public int Process(
            )
        {
            var processedCount = 0;
            var failedCount = 0;

            Parallel.ForEach(_slnfFiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, slnfFileName =>
            {
                var slnfFullFilePath = Path.Combine(_slnfFolderPath, slnfFileName);

                try
                {
                    Console.WriteLine($"Processing {slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}...");

                    var result = ProcessSlnfFile(
                        slnfFullFilePath
                        );

                    var currentCount = Interlocked.Increment(ref processedCount);

                    Console.WriteLine($"""
({currentCount}/{_slnfFiles.Count}) Finished {slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}.
{result}
""");
                }
                catch (Exception excp)
                {
                    Interlocked.Increment(ref failedCount);
                    var currentCount = Interlocked.Increment(ref processedCount);

                    Console.Error.WriteLine($"""
({currentCount}/{_slnfFiles.Count}) Failed {slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}, the file is left unchanged:
{excp.BuildMessageChain().Pastel(ColorTable.ErrorColor)}
""");
                }
            }
            );

            return failedCount;
        }

        private string ProcessSlnfFile(
            string slnfFullFilePath
            )
        {
            var slnf = new Slnf(slnfFullFilePath);
            var originalProjects = slnf.JsonBody.Solution.Projects.ToArray();

            var resultMessage = new StringBuilder();

            //all changes are made in memory, slnf file is written only if everything succeeded
            if (_fromRootsMode.IsEnabled)
            {
                resultMessage.AppendLine(slnf.CleanupExceptRoots(_fromRootsMode.AdditionalRootWildcards));
            }

            using (var projectFileProcessor = new ScanForChangesProcessor(slnf))
            {
                //process any found projects for its reference, which must be added or deleted to/from slnf
                foreach (var projectFileFullPath in slnf.EnumerateProjectFullPaths().ToList())
                {
                    projectFileProcessor.ProcessProjectFromSlnf(projectFileFullPath);
                }

                projectFileProcessor.ApplyChanges();

                resultMessage.Append(projectFileProcessor.BuildResultMessage());
            }

            if (!originalProjects.SequenceEqual(slnf.JsonBody.Solution.Projects, StringComparer.Ordinal))
            {
                slnf.SerializeToItsFile();
            }

            return resultMessage.ToString();
        }
    }
}
