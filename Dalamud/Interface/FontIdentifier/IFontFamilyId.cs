using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.Interface.GameFonts;
using Dalamud.Utility;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font family identifier.
/// </summary>
[JsonConverter(typeof(FontFamilyIdConverter))]
public interface IFontFamilyId : IObjectWithLocalizableName
{
    /// <summary>
    /// Gets the type name of the current object.
    /// </summary>
    [JsonProperty]
    public string TypeName { get; }

    /// <summary>
    /// Gets the list of fonts under this family.
    /// </summary>
    [JsonIgnore]
    IReadOnlyList<IFontId> Fonts { get; }

    /// <summary>
    /// Finds the index of the font inside <see cref="Fonts"/> that best matches the given parameters.
    /// </summary>
    /// <param name="weight">The weight of the font.</param>
    /// <param name="stretch">The stretch of the font.</param>
    /// <param name="style">The style of the font.</param>
    /// <returns>The index of the font. Guaranteed to be a valid index.</returns>
    int FindBestMatch(int weight, int stretch, int style);

    /// <summary>
    /// Gets the list of Dalamud-provided fonts.
    /// </summary>
    /// <returns>The list of fonts.</returns>
    public static List<IFontFamilyId> ListDalamudFonts() =>
        new()
        {
            new DalamudAssetFontAndFamilyId(DalamudAsset.NotoSansJpMedium),
            new DalamudAssetFontAndFamilyId(DalamudAsset.InconsolataRegular),
            new DalamudAssetFontAndFamilyId(DalamudAsset.FontAwesomeFreeSolid),
        };

    /// <summary>
    /// Gets the list of Game-provided fonts.
    /// </summary>
    /// <returns>The list of fonts.</returns>
    public static List<IFontFamilyId> ListGameFonts() => new()
    {
        new GameFontAndFamilyId(GameFontFamily.Axis),
        new GameFontAndFamilyId(GameFontFamily.Jupiter),
        new GameFontAndFamilyId(GameFontFamily.JupiterNumeric),
        new GameFontAndFamilyId(GameFontFamily.Meidinger),
        new GameFontAndFamilyId(GameFontFamily.MiedingerMid),
        new GameFontAndFamilyId(GameFontFamily.TrumpGothic),
    };

    /// <summary>
    /// Gets the list of System-provided fonts.
    /// </summary>
    /// <param name="refresh">If <c>true</c>, try to refresh the list.</param>
    /// <returns>The list of fonts.</returns>
    public static unsafe List<IFontFamilyId> ListSystemFonts(bool refresh)
    {
        using var dwf = default(ComPtr<IDWriteFactory>);
        fixed (Guid* piid = &IID.IID_IDWriteFactory)
        {
            DirectX.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                piid,
                (IUnknown**)dwf.GetAddressOf()).ThrowOnError();
        }

        using var sfc = default(ComPtr<IDWriteFontCollection>);
        dwf.Get()->GetSystemFontCollection(sfc.GetAddressOf(), refresh).ThrowOnError();

        var count = (int)sfc.Get()->GetFontFamilyCount();
        var result = new List<IFontFamilyId>(count);
        for (var i = 0; i < count; i++)
        {
            using var ff = default(ComPtr<IDWriteFontFamily>);
            if (sfc.Get()->GetFontFamily((uint)i, ff.GetAddressOf()).FAILED)
            {
                // Ignore errors, if any
                continue;
            }

            try
            {
                result.Add(SystemFontFamilyId.FromDWriteFamily(ff));
            }
            catch
            {
                // ignore
            }
        }

        return result;
    }

    /// <summary>
    /// A class for properly de/serializing the outer class.
    /// </summary>
    private class FontFamilyIdConverter : JsonConverter<IFontFamilyId>
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override void WriteJson(JsonWriter writer, IFontFamilyId? value, JsonSerializer serializer) =>
            throw new NotSupportedException();

        public override IFontFamilyId? ReadJson(
            JsonReader reader, Type objectType, IFontFamilyId? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            existingValue = jsonObject["TypeName"]?.Value<string>() switch
            {
                nameof(SystemFontFamilyId) =>
                    (IFontFamilyId)RuntimeHelpers.GetUninitializedObject(typeof(SystemFontFamilyId)),
                nameof(GameFontAndFamilyId) =>
                    (IFontFamilyId)RuntimeHelpers.GetUninitializedObject(typeof(GameFontAndFamilyId)),
                nameof(DalamudDefaultFontAndFamilyId) =>
                    (IFontFamilyId)RuntimeHelpers.GetUninitializedObject(typeof(DalamudDefaultFontAndFamilyId)),
                nameof(DalamudAssetFontAndFamilyId) =>
                    (IFontFamilyId)RuntimeHelpers.GetUninitializedObject(typeof(DalamudAssetFontAndFamilyId)),
                _ => null,
            };

            if (existingValue is not null)
                serializer.Populate(jsonObject.CreateReader(), existingValue);
            return existingValue;
        }
    }
}
