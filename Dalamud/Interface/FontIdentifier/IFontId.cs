using System.Runtime.CompilerServices;

using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font identifier.
/// </summary>
[JsonConverter(typeof(FontIdConverter))]
public interface IFontId : IObjectWithLocalizableName
{
    /// <summary>
    /// Gets the type name of the current object.
    /// </summary>
    [JsonProperty]
    string TypeName { get; }

    /// <summary>
    /// Gets the associated font family.
    /// </summary>
    IFontFamilyId Family { get; }

    /// <summary>
    /// Gets the font weight, ranging from 1 to 999.
    /// </summary>
    int Weight { get; }

    /// <summary>
    /// Gets the font stretch, ranging from 1 to 9.
    /// </summary>
    int Stretch { get; }

    /// <summary>
    /// Gets the font style. Treat as an opaque value.
    /// </summary>
    int Style { get; }

    /// <summary>
    /// Adds this font to the given font build toolkit.
    /// </summary>
    /// <param name="tk">The font build toolkit.</param>
    /// <param name="sizePx">The font size.</param>
    /// <param name="glyphRanges">The glyph range.</param>
    /// <param name="mergeFont">The font to merge to.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddToBuildToolkit(
        IFontAtlasBuildToolkitPreBuild tk,
        float sizePx,
        ushort[]? glyphRanges = null,
        ImFontPtr mergeFont = default);

    /// <summary>
    /// A class for properly de/serializing the outer class.
    /// </summary>
    private class FontIdConverter : JsonConverter<IFontId>
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, IFontId? value, JsonSerializer serializer) =>
            throw new NotSupportedException();

        public override IFontId? ReadJson(
            JsonReader reader, Type objectType, IFontId? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            existingValue = jsonObject["TypeName"]?.Value<string>() switch
            {
                nameof(SystemFontId) =>
                    (IFontId)RuntimeHelpers.GetUninitializedObject(typeof(SystemFontId)),
                nameof(GameFontAndFamilyId) =>
                    (IFontId)RuntimeHelpers.GetUninitializedObject(typeof(GameFontAndFamilyId)),
                nameof(DalamudDefaultFontAndFamilyId) =>
                    (IFontId)RuntimeHelpers.GetUninitializedObject(typeof(DalamudDefaultFontAndFamilyId)),
                nameof(DalamudAssetFontAndFamilyId) =>
                    (IFontId)RuntimeHelpers.GetUninitializedObject(typeof(DalamudAssetFontAndFamilyId)),
                _ => null,
            };

            if (existingValue is not null)
                serializer.Populate(jsonObject.CreateReader(), existingValue);
            return existingValue;
        }
    }
}
