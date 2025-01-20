using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pastel;
using SlnfUpdater.Helper;
using System.Text.RegularExpressions;

namespace SlnfUpdater.FileStructure
{
    public sealed class SlnfJsonStructured
    {
        private readonly string _slnfFileFullFolderPath;
        private readonly string _slnfFullFilePath;

        public SlnfJson JsonBody { get; set; }

        public string SlnFullFilePath => Path.GetFullPath(Path.Combine(_slnfFileFullFolderPath, JsonBody.Solution.Path));
        public string SlnFullFolderPath => new FileInfo(SlnFullFilePath).Directory.FullName;

        public SlnfJsonStructured(
            string slnfFileFullFolderPath,
            string slnfFullFilePath
        )
        {
            _slnfFileFullFolderPath = slnfFileFullFolderPath;
            _slnfFullFilePath = slnfFullFilePath;

            JsonBody = SlnfJson.Deserialize(slnfFullFilePath);

            if (JsonBody is null)
            {
                throw new InvalidOperationException($"{slnfFullFilePath} does not found.");
            }
        }

        public string CleanupExceptRoots(IReadOnlyList<string> wildcardRoots)
        {
            var filteredRoots = new List<string>();

            foreach (var projectFullPath in EnumerateProjectFullPaths())
            {
                foreach (var wildcardRoot in wildcardRoots)
                {
                    if (Regex.IsMatch(projectFullPath, wildcardRoot.WildCardToRegular()))
                    {
                        filteredRoots.Add(projectFullPath);
                    }
                }
            }

            JsonBody.Solution.Projects = filteredRoots
                    .Select(r => r.MakeRelativeAgainst(SlnFullFilePath))
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
                var projectFileFullPath = Path.Combine(SlnFullFolderPath, projectFileRelativePath);
                yield return projectFileFullPath;
            }
        }

        public SearchReferenceContext BuildSearchReferenceContext()
        {
            var context = new SearchReferenceContext(
                SlnFullFilePath,
                _slnfFullFilePath,
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
