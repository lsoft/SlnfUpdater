using Microsoft.Build.Evaluation;
using Pastel;
using SlnfUpdater.Helper;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SlnfUpdater.FileStructure
{
    public sealed class SlnfJsonStructured
    {
        private readonly string _slnfFileFullFolderPath;
        private readonly string _slnfFullFilePath;

        public SlnfJson JsonBody { get; set; }

        public string SlnFullFilePath => Path.GetFullPath(Path.Combine(_slnfFileFullFolderPath, JsonBody.solution.path));
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

        public string CleanupExceptRoots(
            IReadOnlyList<string> wildcardRoots
            )
        {
            var filteredRoots = new List<string>();
            foreach(var projectFullPath in EnumerateProjectFullPaths())
            {
                foreach (var wildcardRoot in wildcardRoots)
                {
                    if(Regex.IsMatch(projectFullPath, wildcardRoot.WildCardToRegular()))
                    {
                        filteredRoots.Add(projectFullPath);
                    }
                }
            }

            JsonBody.solution.projects = filteredRoots
                .Select(r => r.MakeRelativeAgainst(SlnFullFilePath))
                .ToArray()
                ;

            var survivedRootsMessage = string.Join(
                Environment.NewLine,
                JsonBody.solution.projects
                    .Select(r => "      " + r)
                ).Pastel(ColorTable.FilteredRootsColor);

            return $"""
   Survived roots from {_slnfFullFilePath.Pastel(ColorTable.SolutionProjectColor)}:
{survivedRootsMessage}
""";

        }

        public IEnumerable<string> EnumerateProjectFullPaths(
            )
        {
            foreach (var projectFileRelativePath in JsonBody.solution.projects)
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

    public class SlnfJson
    {
        public SlnfSolutionJson solution { get; set; }

        public static SlnfJson Deserialize(
            string slnfFullFilePath
            )
        {
            var slnf = JsonSerializer.Deserialize<SlnfJson>(File.ReadAllText(slnfFullFilePath));
            if (slnf is null)
            {
                throw new InvalidOperationException($"{slnfFullFilePath} does not found.");
            }

            slnf.UpdateSlashesInPaths();

            return slnf;
        }


        internal void UpdateSlashesInPaths()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                solution.ChangeSlashesToLinuxStyle();
            }
        }

        public void SerializeToFile(
            string slnfFullFilePath
            )
        {
            var body = JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
                );
            File.WriteAllText(slnfFullFilePath, body);
        }
    }

    public class SlnfSolutionJson
    {
        /// <summary>
        /// Path to sln file.
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// Relative paths to included csprojes.
        /// Paths here are relative against sln file, NOT against a slnF file!
        /// </summary>
        public string[] projects { get; set; }

        internal void ChangeSlashesToLinuxStyle()
        {
            path = path.Replace(
                "\\",
                "/"
                );

            for (var pi = 0; pi < projects.Length; pi++)
            {
                projects[pi] = projects[pi].Replace(
                    "\\",
                    "/"
                    );
            }
        }
    }
}
