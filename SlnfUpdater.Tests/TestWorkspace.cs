using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SlnfUpdater.Processor;

namespace SlnfUpdater.Tests
{
    /// <summary>
    /// Temporary folder with projects and slnf files, removed on dispose.
    /// </summary>
    internal sealed class TestWorkspace : IDisposable
    {
        public string RootPath
        {
            get;
        }

        public TestWorkspace()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "SlnfUpdater.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string FullPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(RootPath, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        }

        public void WriteFile(string relativePath, string body)
        {
            var fullPath = FullPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, body);
        }

        /// <summary>
        /// Writes a minimal non-SDK project, its evaluation does not require any SDK.
        /// </summary>
        public void WriteProject(string relativePath, params string[] projectReferences)
        {
            var references = string.Concat(
                projectReferences.Select(r => $"""    <ProjectReference Include="{r}" />{Environment.NewLine}""")
                );

            WriteFile(relativePath, $"""
                <Project>
                  <ItemGroup>
                {references}  </ItemGroup>
                </Project>
                """);
        }

        public void WriteSlnf(string relativePath, string slnPath, params string[] projects)
        {
            var json = new JObject
            {
                ["solution"] = new JObject
                {
                    ["path"] = slnPath,
                    ["projects"] = new JArray(projects.Cast<object>().ToArray())
                }
            };

            WriteFile(relativePath, json.ToString());
        }

        public string ReadFile(string relativePath)
        {
            return File.ReadAllText(FullPath(relativePath));
        }

        /// <summary>
        /// Returns projects exactly as they are written in the slnf file.
        /// </summary>
        public string[] ReadSlnfProjects(string relativePath)
        {
            var json = JObject.Parse(ReadFile(relativePath));
            return json["solution"]!["projects"]!.Select(p => p.Value<string>()!).ToArray();
        }

        public int Process(string slnfFolderRelativePath, BuildFromRootsMode mode, params string[] slnfFileNames)
        {
            var processor = new SlnfFilesProcessor(
                FullPath(slnfFolderRelativePath),
                new List<string>(slnfFileNames),
                mode
                );

            return processor.Process();
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootPath, true);
            }
            catch (IOException)
            {
                //not critical for tests
            }
        }
    }
}
