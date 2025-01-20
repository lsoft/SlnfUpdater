using Newtonsoft.Json;

namespace SlnfUpdater.FileStructure;

public sealed class SlnfSolutionJson
{
    /// <summary>
    /// Path to sln file.
    /// </summary>
    [JsonProperty("path")]
    public string Path { get; set; }

    /// <summary>
    /// Relative paths to included csprojes.
    /// Paths here are relative against sln file, NOT against a slnF file!
    /// </summary>
    [JsonProperty("projects")]
    public string[] Projects { get; set; }

    [JsonConstructor]
    public SlnfSolutionJson() { }

    internal void ChangeSlashesToLinuxStyle()
    {
        Path = Path.Replace(
            "\\",
            "/"
            );

        for (var pi = 0; pi < Projects.Length; pi++)
        {
            Projects[pi] = Projects[pi].Replace(
                "\\",
                "/"
                );
        }
    }
}