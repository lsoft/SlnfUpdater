using Microsoft.Build.Evaluation;
using SlnfUpdater.FileStructure;
using SlnfUpdater.Helper;
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

            if (projectFileInfo.Extension.In(
                ".shproj",
                ".vcxproj"
                ))
            {
                //skip this type of project
                return true;
            }

            return false;
        }
    }

    //public sealed class CleanupRootsProcessor : ProjectFileProcessor
    //{
    //    private readonly SlnfJsonStructured _slnf;
        
    //    /// <summary>
    //    /// projects from slnf which have to references from other slnf's projects.
    //    /// </summary>
    //    private List<string> _existingRoots = [];

    //    public CleanupRootsProcessor(
    //        SlnfJsonStructured slnf
    //        )
    //    {
    //        if (slnf is null)
    //        {
    //            throw new ArgumentNullException(nameof(slnf));
    //        }

    //        _slnf = slnf;
    //    }

    //    public override void ProcessProjectFromSlnf(
    //        string projectFileFullPath
    //        )
    //    {
    //        var projectFileInfo = new FileInfo(projectFileFullPath);
    //        if (!File.Exists(projectFileFullPath))
    //        {
    //            //project file does not exists, so skip it entirely
    //            return;
    //        }

    //    }
    //}

    public sealed class ScanForChangesProcessor : ProjectFileProcessor
    {
        private readonly Slnf _slnf;
        private readonly SearchReferenceContext _context;

        public ScanForChangesProcessor(
            Slnf slnf
            )
        {
            _slnf = slnf ?? throw new ArgumentNullException(nameof(slnf));
            _context = slnf.BuildSearchReferenceContext();
        }

        public void ApplyAndSave()
        {
            if (!_context.HasChanges)
            {
                return;
            }

            _context.ApplyChangesTo(_slnf);
            _slnf.SerializeToItsFile();
        }

        public string BuildResultMessage()
        {
            return _context.BuildResultMessage();
        }

        public override void ProcessProjectFromSlnf(
            string projectFileFullPath
            )
        {
            var projectFileInfo = new FileInfo(projectFileFullPath);

            if (!File.Exists(projectFileFullPath))
            {
                //project file does not exists, just delete its reference
                _context.DeleteReference(projectFileFullPath);
                return;
            }

            ProcessProjectFromSlnf(
                _context,
                projectFileInfo
                );
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
            var projectFolderPath = projectFileInfo.Directory.FullName;

            var projectReferences = ProvideProjectReferences(
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
                catch (Exception excp)
                {
                    throw new InvalidOperationException(
                        $"Error occurred while processing {projectFileInfo.FullName}",
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

            using (var projectCollection = new ProjectCollection())
            {
                Project? evaluatedProject = null;
                try
                {
                    evaluatedProject = Project.FromProjectRootElement(
                        projectXmlRoot,
                        new Microsoft.Build.Definition.ProjectOptions
                        {
                            ProjectCollection = projectCollection
                        }
                        );

                    return evaluatedProject.ItemsIgnoringCondition
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
