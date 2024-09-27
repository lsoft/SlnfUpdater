using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Pastel;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
using System.Drawing;
using System.Runtime;
using System.Text;
using System.Text.Json;

namespace SlnfUpdater
{
    internal class Program
    {
        public static System.Drawing.Color SolutionProjectColor = Color.FromArgb(165, 229, 250);
        public static System.Drawing.Color TimeColor = Color.FromArgb(220, 110, 0);
        public static System.Drawing.Color NoReferenceColor = Color.FromArgb(255, 255, 0);
        public static System.Drawing.Color AddedReferenceColor = Color.FromArgb(0, 255, 0);
        public static System.Drawing.Color DeletedReferenceColor = Color.FromArgb(220, 80, 0);

        static void Main(string[] args)
        {
            var before = DateTime.Now;

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
                Console.WriteLine($"    {slnfFile.Pastel(SolutionProjectColor)}");
            }

            var processedCount = 0;
            //foreach (var slnfFile in slnfFiles)
            Parallel.ForEach(slnfFiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, slnfFileName =>
            {
                try
                {
                    var slnfFullFilePath = Path.Combine(slnfFolderPath, slnfFileName);

                    Console.WriteLine($"Processing {slnfFullFilePath.Pastel(SolutionProjectColor)}...");

                    var result = ProcessSlnfFile(
                        slnfFolderPath,
                        slnfFullFilePath
                        );

                    Interlocked.Increment(ref processedCount);

                    Console.WriteLine($"""
({processedCount}/{slnfFiles.Count}) Finished {slnfFullFilePath.Pastel(SolutionProjectColor)}.
{result}
""");
                }
                catch(Exception excp)
                {
                    throw new InvalidOperationException(
                        $"Error occured while processing {slnfFileName}",
                        excp
                        );
                }
            }
            );

            var after = DateTime.Now;
            Console.WriteLine($"Finished (taken {(after - before).ToString().Pastel(TimeColor)}).");
        }

        private static string ProcessSlnfFile(
            string slnfFolderPath,
            string slnfFullFilePath
            )
        {
            var structuredJson = new SlnfJsonStructured(slnfFolderPath, slnfFullFilePath);
            var context = structuredJson.BuildSearchReferenceContext();

            //process any found projects for its reference, which must be added or deleted to/from slnf
            foreach (var projectFileFullPath in structuredJson.EnumerateProjectFullPaths())
            {
                var projectFileInfo = new FileInfo(projectFileFullPath);
                if (!File.Exists(projectFileFullPath))
                {
                    context.DeleteReference(projectFileFullPath);
                }
                else
                {
                    var projectFolderPath = projectFileInfo.Directory.FullName;

                    ProcessProjectFromSlnf(
                        context,
                        projectFileInfo
                        );
                }
            }

            if (!context.HasChanges)
            {
                return $"   No references changed.".Pastel(NoReferenceColor);
            }

            context.ApplyChangesTo(structuredJson);
            structuredJson.SerializeToItsFile();

            return context.BuildResultMessage();
        }

        private static void ProcessProjectFromSlnf(
            SearchReferenceContext context,
            FileInfo projectFileInfo
            )
        {
            if (!projectFileInfo.Exists)
            {
                throw new InvalidOperationException($"Project does not found: {projectFileInfo.FullName}");
            }

            if (projectFileInfo.Extension.In(
                ".shproj",
                ".vcxproj"
                ))
            {
                //skip this type of project
                return;
            }

            var projectFilePath = projectFileInfo.FullName;
            var projectFolderPath = projectFileInfo.Directory.FullName;

            List<ProjectItem> projectReferences = ProvideProjectReferences(
                projectFilePath
                );

            foreach (var projectReference in projectReferences)
            {
                try
                {
                    var referenceProjectPathRelativeCurrentProject = projectReference.EvaluatedInclude;

                    var referenceProjectFullPath = Path.GetFullPath(
                        Path.Combine(
                            projectFolderPath,
                            referenceProjectPathRelativeCurrentProject
                            )
                        );

                    if (context.IsProcessed(referenceProjectFullPath))
                    {
                        continue;
                    }

                    context.AddReferenceFullPathIfNew(referenceProjectFullPath);

                    //process recursively
                    ProcessProjectFromSlnf(
                        context,
                        new FileInfo(referenceProjectFullPath)
                        );
                }
                catch(Exception excp)
                {
                    throw new InvalidOperationException(
                        $"Error occured while processing {projectFileInfo.FullName}",
                        excp
                        );

                }
            }
        }

        private static List<ProjectItem> ProvideProjectReferences(
            string projectFilePath
            )
        {
            var projectXmlRoot = Microsoft.Build.Construction.ProjectRootElement.Open(
                projectFilePath
                );

            using (var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection())
            {
                Microsoft.Build.Evaluation.Project? evaluatedProject = null;
                try
                {
                    evaluatedProject = Microsoft.Build.Evaluation.Project.FromProjectRootElement(
                        projectXmlRoot,
                        new Microsoft.Build.Definition.ProjectOptions
                        {
                            ProjectCollection = projectCollection
                        }
                        );

                    return evaluatedProject.AllEvaluatedItems
                        .Where(p => p.ItemType == "ProjectReference")
                        .ToList();
                }
                finally
                {
                    if (evaluatedProject is not null)
                    {
                        projectCollection.UnloadProject(evaluatedProject);
                    }
                }
            }
        }
    }
}
