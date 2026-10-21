using System.Linq;
using Newtonsoft.Json;
using SlnfUpdater.Helper;

namespace SlnfUpdater.FileStructure.Json;

public sealed class SlnfSolutionJson
{
    /// <summary>
    /// Path to sln file.
    /// </summary>
    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Relative paths to included csprojes.
    /// Paths here are relative against sln file, NOT against a slnF file!
    /// </summary>
    [JsonProperty("projects")]
    public string[] Projects { get; set; } = [];

    [JsonConstructor]
    public SlnfSolutionJson() { }

    internal void ChangeSlashesToOsStyle()
    {
        Path = Path.ToOsSeparators();

        for (var pi = 0; pi < Projects.Length; pi++)
        {
            Projects[pi] = Projects[pi].ToOsSeparators();
        }
    }

    internal SlnfSolutionJson CloneWithSlnfStyleSlashes()
    {
        return new SlnfSolutionJson
        {
            Path = Path.ToSlnfSeparators(),
            Projects = Projects.Select(p => p.ToSlnfSeparators()).ToArray()
        };
    }
}
