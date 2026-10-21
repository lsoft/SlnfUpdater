using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using SlnfUpdater.FileStructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnfUpdater.Processor.ProjectFile
{
    public abstract class ProjectFileProcessor
    {
        public abstract void ProcessProjectFromSlnf(
            string projectFileFullPath
            );

        protected bool SkipThisProject(FileInfo projectFileInfo)
        {
            if (!projectFileInfo.Exists)
            {
                throw new InvalidOperationException($"Project does not found: {projectFileInfo.FullName}");
            }

            if (string.Equals(projectFileInfo.Extension, ".shproj", StringComparison.OrdinalIgnoreCase)
                || string.Equals(projectFileInfo.Extension, ".vcxproj", StringComparison.OrdinalIgnoreCase))
            {
                //skip this type of project
                return true;
            }

            return false;
        }
    }

    public sealed class ScanForChangesProcessor : ProjectFileProcessor, IDisposable
    {
        private readonly Slnf _slnf;
        private readonly SearchReferenceContext _context;

        /// <summary>
        /// One collection for the whole slnf: creating a collection per project is expensive.
        /// </summary>
        private readonly ProjectCollection _projectCollection;

        public ScanForChangesProcessor(
            Slnf slnf
            )
        {
            _slnf = slnf ?? throw new ArgumentNullException(nameof(slnf));
            _context = slnf.BuildSearchReferenceContext();
            _projectCollection = new ProjectCollection(BuildSolutionGlobalProperties(slnf.SlnFullFilePath));
        }

        public bool HasChanges => _context.HasChanges;

        public void ApplyChanges()
        {
            if (!_context.HasChanges)
            {
                return;
            }

            _context.ApplyChangesTo(_slnf);
        }

        public string BuildResultMessage()
        {
            return _context.BuildResultMessage();
        }

        public override void ProcessProjectFromSlnf(
            string projectFileFullPath
            )
        {
            projectFileFullPath = Path.GetFullPath(projectFileFullPath);

            if (_context.IsProcessed(projectFileFullPath))
            {
                //already processed as a reference of another project
                return;
            }

            if (!File.Exists(projectFileFullPath))
            {
                //project file does not exists, just delete its reference
                _context.DeleteReference(projectFileFullPath);
                return;
            }

            _context.MarkProcessed(projectFileFullPath);

            ProcessProjectFromSlnf(
                _context,
                new FileInfo(projectFileFullPath)
                );
        }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }

        private void ProcessProjectFromSlnf(
            SearchReferenceContext context,
            FileInfo projectFileInfo
            )
        {
            if (SkipThisProject(projectFileInfo))
            {
                return;
            }

            var projectFilePath = projectFileInfo.FullName;
            var projectFolderPath = projectFileInfo.DirectoryName!;

            var projectReferences = ProvideProjectReferences(
                projectFilePath
                );

            foreach (var referenceProjectPathRelativeCurrentProject in projectReferences)
            {
                try
                {
                    var referenceProjectFullPath = Path.GetFullPath(
                        Path.Combine(
                            projectFolderPath,
                            referenceProjectPathRelativeCurrentProject
                            )
                        );

                    if (!File.Exists(referenceProjectFullPath))
                    {
                        throw new InvalidOperationException(
                            $"{projectFilePath} references a project which does not exist: {referenceProjectFullPath}"
                            );
                    }

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
                catch (Exception excp)
                {
                    throw new InvalidOperationException(
                        $"Error occurred while processing {projectFileInfo.FullName}",
                        excp
                        );
                }
            }
        }

        private List<string> ProvideProjectReferences(
            string projectFilePath
            )
        {
            var projectXmlRoot = ProjectRootElement.Open(
                projectFilePath,
                _projectCollection
                );

            Project? evaluatedProject = null;
            try
            {
                evaluatedProject = Project.FromProjectRootElement(
                    projectXmlRoot,
                    new Microsoft.Build.Definition.ProjectOptions
                    {
                        ProjectCollection = _projectCollection
                    }
                    );

                return evaluatedProject.ItemsIgnoringCondition
                    .Where(p => p.ItemType == "ProjectReference")
                    .Select(p => p.EvaluatedInclude)
                    .ToList();
            }
            finally
            {
                if (evaluatedProject is not null)
                {
                    _projectCollection.UnloadProject(evaluatedProject);
                }
            }
        }

        /// <summary>
        /// Properties which MSBuild defines while building a solution,
        /// project references may use them, e.g. $(SolutionDir).
        /// </summary>
        private static Dictionary<string, string> BuildSolutionGlobalProperties(
            string slnFullFilePath
            )
        {
            var slnFolderPath = Path.GetDirectoryName(slnFullFilePath)!;

            return new Dictionary<string, string>
            {
                ["SolutionDir"] = slnFolderPath + Path.DirectorySeparatorChar,
                ["SolutionPath"] = slnFullFilePath,
                ["SolutionFileName"] = Path.GetFileName(slnFullFilePath),
                ["SolutionName"] = Path.GetFileNameWithoutExtension(slnFullFilePath),
                ["SolutionExt"] = Path.GetExtension(slnFullFilePath)
            };
        }
    }
}
