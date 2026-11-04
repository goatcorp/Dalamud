using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Serialization;

namespace Dalamud.Configuration.Internal;

/// <summary>Reads the type names of font ids saved before they moved to <see cref="Interface.Fonts"/> or were
/// renamed.</summary>
internal sealed partial class MovedTypeSerializationBinder : DefaultSerializationBinder
{
    private static readonly FrozenDictionary<string, string> RenamedFontTypes = new Dictionary<string, string>
    {
        ["DalamudAssetFontAndFamilyId"] = "DalamudAssetFontId",
        ["DalamudDefaultFontAndFamilyId"] = "DalamudDefaultFontId",
        ["GameFontAndFamilyId"] = "GameFontId",
        ["SystemFontId"] = "SystemFontFaceId",
    }.ToFrozenDictionary();

    /// <summary>Gets the instance.</summary>
    public static MovedTypeSerializationBinder Instance { get; } = new();

    /// <summary>Maps the font type names in a type name, as saved, to those of the current types.</summary>
    /// <param name="typeName">The type name, with or without an assembly, possibly with generic arguments.</param>
    /// <returns>The type name of the current types.</returns>
    public static string MapTypeName(string typeName) =>
        typeName.Contains("Dalamud.Interface.Font", StringComparison.Ordinal)
            ? FontTypeNameRegex().Replace(
                typeName,
                static m => "Dalamud.Interface.Fonts." +
                            RenamedFontTypes.GetValueOrDefault(m.Groups[1].Value, m.Groups[1].Value))
            : typeName;

    /// <inheritdoc/>
    public override Type BindToType(string? assemblyName, string typeName) =>
        base.BindToType(assemblyName, MapTypeName(typeName));

    [GeneratedRegex(@"\bDalamud\.Interface\.(?:FontIdentifier|Fonts)\.(\w+)\b")]
    private static partial Regex FontTypeNameRegex();
}
