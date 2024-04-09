using Microsoft.Build.Definition;
using Microsoft.Build.Locator;
using SlnfUpdater.Helper;
using System.Runtime;

namespace SlnfUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();

            var slnfFolder = Path.GetFullPath(args[0]);
            var slnfFileMask = args[1];

            if (!Directory.Exists(slnfFolder))
            {
                Console.WriteLine($"Folder {slnfFolder} does not exist.");
                return;
            }

            Console.WriteLine($"Found folder {slnfFolder}");

            var slnfFiles = Directory.GetFiles(slnfFolder, slnfFileMask, SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f).Name)
                .ToList();
            if (slnfFiles.Count == 0)
            {
                Console.WriteLine($"Folder {slnfFolder} does not contains a slnf files.");
                return;
            }

            Console.WriteLine("Found slnf files:");

            foreach (var slnfFile in slnfFiles)
            {
                Console.WriteLine($"    {slnfFile}");
            }


            foreach (var slnfFile in slnfFiles)
            {
                ProcessSlnfFile(
                    slnfFolder,
                    slnfFile
                    );
            }

            Console.WriteLine("Finished.");
        }

        private static void ProcessSlnfFile(
            string slnfFolderPath,
            string slnfFileName
            )
        {
            var slnfFilePath = Path.Combine(slnfFolderPath, slnfFileName);

            Console.WriteLine($"Processing {slnfFilePath}...");

            var slnFile = Microsoft.Build.Construction.SolutionFile.Parse(
                slnfFilePath
                );

            //TODO: https://github.com/dotnet/msbuild/issues/9981
            var slnfProjectsRelativeSlnField = slnFile.GetType().GetField("_solutionFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slnfProjectsRelativeSln = (IReadOnlySet<string>)slnfProjectsRelativeSlnField.GetValue(slnFile);

            var slnFullPathProperty = slnFile.GetType().GetProperty("FullPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var slnFullPath = (string)slnFullPathProperty.GetValue(slnFile);

            var context = new SearchReferenceContext(
                slnFullPath,
                slnfFilePath,
                slnFile,
                slnfProjectsRelativeSln
                );

            //process any found projects for its reference, which must be added to slnf later
            foreach (var slnfProjectRelativeSln in slnfProjectsRelativeSln)
            {
                var projectFilePath = Path.GetFullPath(
                    Path.Combine(
                        context.SlnfFolderPath,
                        slnfProjectRelativeSln
                        )
                    );
                var projectFileInfo = new FileInfo(projectFilePath);
                var projectFolderPath = projectFileInfo.Directory.FullName;

                ProcessProjectFromSlnf(
                    context,
                    projectFileInfo
                    );
            }

            if (context.AddedReferences.Count > 0)
            {
                Console.WriteLine(
                    "New references:"
                    );

                Console.WriteLine(
                    string.Join(
                        Environment.NewLine,
                        context.AddedReferences.Select(r => "    " + r)
                        )
                    );

                var slnfBody = File.ReadAllLines(context.SlnfFilePath).ToList();
                var si = slnfBody.FindIndex(p => p.EndsWith("["));
                var ei = slnfBody.FindIndex(p => p.EndsWith("]"));

                slnfBody.RemoveRange(si + 1, ei - si - 1);

                var addedReferencesWithSort = context.GetSortedProjects();
                for (var i = 0; i < addedReferencesWithSort.Count; i++)
                {
                    var addedReferenceWithSort = addedReferencesWithSort[i]
                        .Replace("\\", "\\\\");
                    var last = i == 0; //because of descending sorting!

                    if (last)
                    {
                        slnfBody.Insert(si + 1, $"      \"{addedReferenceWithSort}\"");
                    }
                    else
                    {
                        slnfBody.Insert(si + 1, $"      \"{addedReferenceWithSort}\",");
                    }
                }

                File.WriteAllText(
                    context.SlnfFilePath,
                    string.Join(
                        Environment.NewLine,
                        slnfBody
                        )
                    );
            }
            else
            {
                Console.WriteLine(
                    "No new references found."
                    );
            }
        }

        private static void ProcessProjectFromSlnf(
            SearchReferenceContext context,
            FileInfo projectFileInfo
            )
        {
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

            var projectXmlRoot = Microsoft.Build.Construction.ProjectRootElement.Open(
                projectFilePath
                );

            var evaluatedProject = Microsoft.Build.Evaluation.Project.FromProjectRootElement(
                projectXmlRoot,
                new Microsoft.Build.Definition.ProjectOptions { }
                );

            using var evaluatedProjectWrapper = new EvaluationProjectWrapper(evaluatedProject);

            var projectReferences = evaluatedProjectWrapper.Project.AllEvaluatedItems
                .Where(p => p.ItemType == "ProjectReference")
                .ToList();
            foreach (var projectReference in projectReferences)
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
        }
    }
}
