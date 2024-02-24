using System.Collections.Generic;
using System.Reflection;

using Dalamud.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dalamud.Plugin.Internal.Profiles;

/// <summary>
/// Class representing a profile.
/// </summary>
public abstract class ProfileModel
{
    /// <summary>
    /// Gets or sets the ID of the profile.
    /// </summary>
    [JsonProperty("id")]
    public Guid Guid { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the name of the profile.
    /// </summary>
    [JsonProperty("n")]
    public string Name { get; set; } = "New Collection";

    /// <summary>
    /// Deserialize a profile into a model.
    /// </summary>
    /// <param name="model">The string to decompress.</param>
    /// <returns>The parsed model.</returns>
    /// <exception cref="ArgumentException">Thrown when the parsed string is not a valid profile.</exception>
    public static ProfileModel? Deserialize(string model)
    {
        var json = Util.DecompressString(Convert.FromBase64String(model.Substring(3)));

        if (model.StartsWith(ProfileModelV1.SerializedPrefix))
            return JsonConvert.DeserializeObject<ProfileModelV1>(json);

        throw new ArgumentException("Was not a compressed profile.");
    }

    /// <summary>
    /// Serialize this model into a string usable for sharing, without including GUIDs.
    /// </summary>
    /// <returns>The serialized representation of the model.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported model is serialized.</exception>
    public string SerializeForShare()
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

        // HACK: Just filter the ID for now, we should split the sharing + saving model
        var serialized = JsonConvert.SerializeObject(this, new JsonSerializerSettings()
                                                         { ContractResolver = new IgnorePropertiesResolver(new[] { "WorkingPluginId" }) });

        return prefix + Convert.ToBase64String(Util.CompressString(serialized));
    }
    
    // Short helper class to ignore some properties from serialization
    private class IgnorePropertiesResolver : DefaultContractResolver
    {
        private readonly HashSet<string> ignoreProps;

        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore)
        {
            this.ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (this.ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = _ => false;
            }
            
            return property;
        }
    }
}
