using System;

using Dalamud.Utility;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Profiles;

public abstract class ProfileModel
{
    [JsonProperty("id")]
    public Guid Guid { get; set; } = Guid.Empty;

    [JsonProperty("n")]
    public string Name { get; set; } = "New Profile";

    public static ProfileModel? Deserialize(string model)
    {
        var json = Util.DecompressString(Convert.FromBase64String(model.Substring(3)));

        if (model.StartsWith(ProfileModelV1.SerializedPrefix))
            return JsonConvert.DeserializeObject<ProfileModelV1>(json);

        throw new ArgumentException("Was not a compressed style model.");
    }

    public string Serialize()
    {
        string prefix;
        switch (this)
        {
            case ProfileModelV1:
                prefix = ProfileModelV1.SerializedPrefix;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return prefix + Convert.ToBase64String(Util.CompressString(JsonConvert.SerializeObject(this)));
    }
}
