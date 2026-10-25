using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Serialization;

namespace Dalamud.Configuration.Internal;

/// <summary>Maps legacy serialized font type names from before they moved to <see cref="Interface.Fonts"/> or were
/// renamed.</summary>
internal sealed partial class MovedTypeSerializationBinder : DefaultSerializationBinder
{
    private static readonly FrozenDictionary<string, string> RenamedFontTypes = new Dictionary<string, string>
    {
        ["DalamudAssetFontAndFamilyId"] = "DalamudAssetFontId",
        ["DalamudDefaultFontAndFamilyId"] = "DalamudDefaultFontId",
        ["GameFontAndFamilyId"] = "GameFontId",
        ["IFontId"] = "IFontFaceId",
        ["SystemFontId"] = "SystemFontFaceId",
    }.ToFrozenDictionary();

    /// <summary>Gets the instance.</summary>
    public static MovedTypeSerializationBinder Instance { get; } = new();

    /// <summary>Rewrites legacy font type names within a serialized type name (including generic arguments) to current
    /// names.</summary>
    /// <param name="typeName">The serialized type name.</param>
    /// <returns>The updated type name.</returns>
    public static string MapTypeName(string typeName) =>
        typeName.Contains("Dalamud.Interface.", StringComparison.Ordinal)
            ? FontTypeNameRegex().Replace(
                typeName,
                static m => "Dalamud.Interface.Fonts." +
                            RenamedFontTypes.GetValueOrDefault(m.Groups[1].Value, m.Groups[1].Value))
            : typeName;

    /// <inheritdoc/>
    public override Type BindToType(string? assemblyName, string typeName) =>
        base.BindToType(assemblyName, MapTypeName(typeName));

    [GeneratedRegex(@"\bDalamud\.Interface\.(?:FontIdentifier|Fonts|GameFonts)\.(\w+)\b")]
    private static partial Regex FontTypeNameRegex();
}
