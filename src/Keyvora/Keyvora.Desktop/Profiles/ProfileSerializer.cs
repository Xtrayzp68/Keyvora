namespace Keyvora.Desktop.Profiles;

using System.IO;
using Newtonsoft.Json;

public static class ProfileSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
    };

    public static string Serialize(Profile profile) =>
        JsonConvert.SerializeObject(profile, Settings);

    public static Profile? Deserialize(string json) =>
        JsonConvert.DeserializeObject<Profile>(json, Settings);

    public static void SerializeToFile(Profile profile, string path)
    {
        var json = Serialize(profile);
        File.WriteAllText(path, json);
    }

    public static Profile? DeserializeFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }
}
