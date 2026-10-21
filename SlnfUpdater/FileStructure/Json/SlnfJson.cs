using System;
using System.IO;
using Newtonsoft.Json;

namespace SlnfUpdater.FileStructure.Json;

public sealed class SlnfJson
{
    [JsonProperty("solution")]
    public SlnfSolutionJson Solution { get; set; } = new();

    [JsonConstructor]
    public SlnfJson() { }

    public static SlnfJson Deserialize(
        string slnfFullFilePath
    )
    {
        var slnf = JsonConvert.DeserializeObject<SlnfJson>(File.ReadAllText(slnfFullFilePath));
        if (slnf?.Solution is null || string.IsNullOrWhiteSpace(slnf.Solution.Path))
        {
            throw new InvalidOperationException($"{slnfFullFilePath} is not a valid slnf file: solution path is missing.");
        }

        slnf.Solution.Projects ??= [];
        slnf.Solution.ChangeSlashesToOsStyle();

        return slnf;
    }

    public void SerializeToFile(
        string slnfFullFilePath
    )
    {
        var slnf = new SlnfJson
        {
            Solution = Solution.CloneWithSlnfStyleSlashes()
        };

        var body = JsonConvert.SerializeObject(slnf, Formatting.Indented);
        File.WriteAllText(slnfFullFilePath, body);
    }
}
