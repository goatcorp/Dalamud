using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dalamud.Plugin.Internal.Profiles;

public class ProfileModelV1 : ProfileModel
{
    public static string SerializedPrefix => "DP1";

    [JsonProperty("b")]
    public bool AlwaysEnableOnBoot { get; set; } = false;

    [JsonProperty("e")]
    public bool IsEnabled { get; set; } = false;

    [JsonProperty("c")]
    public uint Color { get; set; }

    public List<ProfileModelV1Plugin> Plugins { get; set; } = new();

    public class ProfileModelV1Plugin
    {
        public string InternalName { get; set; }

        public bool IsEnabled { get; set; }
    }
}
