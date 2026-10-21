using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pastel;
using SlnfUpdater.Helper;
using SlnfUpdater.FileStructure.Json;

namespace SlnfUpdater.FileStructure
{
    public sealed class Slnf
    {
        private readonly string _slnfFullFilePath;

        public SlnfJson JsonBody { get; }

        public string SlnFullFilePath { get; }
        public string SlnFullFolderPath { get; }

        public Slnf(
            string slnfFullFilePath
        )
        {
            _slnfFullFilePath = Path.GetFullPath(slnfFullFilePath);

            JsonBody = SlnfJson.Deserialize(_slnfFullFilePath);

            var slnfFullFolderPath = Path.GetDirectoryName(_slnfFullFilePath)!;
            SlnFullFilePath = Path.GetFullPath(Path.Combine(slnfFullFolderPath, JsonBody.Solution.Path));
            SlnFullFolderPath = Path.GetDirectoryName(SlnFullFilePath)!;
        }

        public string CleanupExceptRoots(IReadOnlyList<string> wildcardRoots)
        {
            var filteredRoots = EnumerateProjectFullPaths()
                .Where(projectFullPath => wildcardRoots.Any(projectFullPath.MatchesWildcard))
                .Distinct(PathHelper.PathComparer)
                .ToList();

            if (filteredRoots.Count == 0)
            {
                //removing everything is never what user wants, so leave this slnf as is
                return $"   No roots found in {_slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}, its projects are kept as is."
                    .Pastel(ColorTable.NoReferenceColor);
            }

            JsonBody.Solution.Projects = filteredRoots
                    .Select(r => Path.GetRelativePath(SlnFullFolderPath, r))
                    .ToArray()
                ;

            var survivedRootsMessage = string.Join(
                    Environment.NewLine,
                    JsonBody.Solution.Projects
                        .Select(r => "      " + r)
                    )
                .Pastel(ColorTable.FilteredRootsColor);

            return $"""
                   Survived roots from {_slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}:
                {survivedRootsMessage}
                """;
        }

        public IEnumerable<string> EnumerateProjectFullPaths()
        {
            foreach (var projectFileRelativePath in JsonBody.Solution.Projects)
            {
                //GetFullPath is required to normalize paths like ..\Project\Project.csproj
                var projectFileFullPath = Path.GetFullPath(Path.Combine(SlnFullFolderPath, projectFileRelativePath));
                yield return projectFileFullPath;
            }
        }

        public SearchReferenceContext BuildSearchReferenceContext()
        {
            var context = new SearchReferenceContext(
                SlnFullFilePath,
                EnumerateProjectFullPaths()
                );

            return context;
        }

        public void SerializeToItsFile()
        {
            JsonBody.SerializeToFile(_slnfFullFilePath);
        }
    }
}
