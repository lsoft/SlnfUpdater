using Microsoft.Build.Evaluation;
using SlnfUpdater.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlnfUpdater
{
    public sealed class EvaluationProjectWrapper : IDisposable
    {
        public Project Project
        {
            get;
        }

        private int _disposed = 0;

        public EvaluationProjectWrapper(
            Microsoft.Build.Evaluation.Project project
            )
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            Project = project;
        }

        public IReadOnlySet<string> ScanForProjectFiles(
            string rootFolder,
            string csprojFullFolderPath
            )
        {
            var skippedFolders = BuildSkippedFolders(csprojFullFolderPath);

            var filteredItems = new HashSet<string>();
            foreach (var item in Project.AllEvaluatedItems)
            {
                //нам не нужны эти "итемы", они не представляют собой реальные файл
                if (item.ItemType.In(
                    "PackageReference",
                    "ProjectReference",
                    "PackageVersion",
                    "_UnmanagedRegistrationCache",
                    "_ResolveComReferenceCache",
                    "_AllDirectoriesAbove",
                    "PotentialEditorConfigFiles",
                    "GlobalAnalyzerConfigFiles"
                    ))
                {
                    continue;
                }

                var evaluatedFullPaths = new List<string>();
                if (item.ItemType == "Reference")
                {
                    foreach (var pm in item.Metadata)
                    {
                        if (pm.Name == "HintPath")
                        {
                            var ev = pm.EvaluatedValue;
                            var evFullPath = PathHelper.GetFullPath(csprojFullFolderPath, ev);
                            evaluatedFullPaths.Add(evFullPath);
                        }
                    }
                }
                else
                {
                    var ei = item.EvaluatedInclude;
                    var eiFullPath = PathHelper.GetFullPath(csprojFullFolderPath, ei);
                    evaluatedFullPaths.Add(eiFullPath);
                }

                foreach (var eiFullPath in evaluatedFullPaths)
                {
                    if (skippedFolders.Any(sf => eiFullPath.StartsWith(sf)))
                    {
                        continue;
                    }

                    if (File.Exists(eiFullPath))
                    {
                        if (eiFullPath.Length > rootFolder.Length)
                        {
                            var unrooted = eiFullPath.Substring(rootFolder.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            filteredItems.Add(unrooted);
                        }
                    }
                }
            }

            return filteredItems;
        }


        private IReadOnlySet<string> BuildSkippedFolders(
            string csprojFullFolderPath
            )
        {
            var skippedFolders = new HashSet<string>();
            foreach (var propertyName in new string[]
                {
                    "BaseIntermediateOutputPath",
                    "NuGetPackageRoot",
                    "MSBuildToolsPath",
                    "MSBuildBinPath",
                    "FrameworkPathOverride",
                    "NuGetPackageFolders",
                    "OutputPath",
                    "IntermediateOutputPath",
                    "PackageOutputPath"
                })
            {
                var property = Project.AllEvaluatedProperties.FirstOrDefault(p => p.Name == propertyName);
                if (property != null)
                {
                    var evaluatedValue = property.EvaluatedValue;
                    if (string.IsNullOrEmpty(evaluatedValue))
                    {
                        continue;
                    }

                    var propertyValues =
                        evaluatedValue.Contains(";")
                            ? evaluatedValue.Split(";", StringSplitOptions.RemoveEmptyEntries)
                            : new string[] { evaluatedValue }
                        ;

                    foreach (var propertyValue in propertyValues)
                    {
                        if (Path.IsPathRooted(propertyValue))
                        {
                            if (Directory.Exists(propertyValue))
                            {
                                skippedFolders.Add(propertyValue);
                            }
                        }
                        else
                        {
                            var fdp = Path.Combine(
                                csprojFullFolderPath,
                                propertyValue
                                );

                            //на случай путей типа ../../somefile.cs
                            fdp = Path.GetFullPath(fdp);

                            if (Directory.Exists(fdp))
                            {
                                skippedFolders.Add(fdp);
                            }
                        }
                    }
                }
            }

            return skippedFolders;
        }

        public IReadOnlyList<string> DetermineConfigurations(
            )
        {
            var result = new List<string>();
            var sProperty = Project.AllEvaluatedProperties.FirstOrDefault(p => p.Name == "Configurations");
            if (sProperty is not null)
            {
                result.AddRange(
                    sProperty.EvaluatedValue.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    );
            }
            else
            {
                var property = Project.AllEvaluatedProperties.FirstOrDefault(p => p.Name == "Configuration");
                if (property is not null)
                {
                    result.Add(
                        property.EvaluatedValue
                        );
                }
            }

            return result;
        }

        public IReadOnlyList<string> DetermineTargetFrameworks(
            )
        {
            var result = new List<string>();
            var sProperty = Project.AllEvaluatedProperties.FirstOrDefault(p => p.Name == "TargetFrameworks");
            if (sProperty is not null)
            {
                result.AddRange(
                    sProperty.EvaluatedValue.Split(";", StringSplitOptions.RemoveEmptyEntries)
                    );
            }
            else
            {
                var property = Project.AllEvaluatedProperties.FirstOrDefault(p => p.Name == "TargetFramework");
                if (property is not null)
                {
                    result.Add(
                        property.EvaluatedValue
                        );
                }
            }

            return result;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadProject(Project);
        }
    }
}
