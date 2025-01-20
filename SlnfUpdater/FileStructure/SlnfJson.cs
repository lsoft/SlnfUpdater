using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace SlnfUpdater.FileStructure;

public sealed class SlnfJson
{
    [JsonProperty("solution")]
    public SlnfSolutionJson Solution { get; set; }

    [JsonConstructor]
    public SlnfJson() { }

    public static SlnfJson Deserialize(
        string slnfFullFilePath
    )
    {
        var slnf = JsonConvert.DeserializeObject<SlnfJson>(File.ReadAllText(slnfFullFilePath));
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
            Solution.ChangeSlashesToLinuxStyle();
        }
    }

    public void SerializeToFile(
        string slnfFullFilePath
    )
    {
        var body = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(slnfFullFilePath, body);
    }
}