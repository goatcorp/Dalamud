using Dalamud.Configuration.Internal;
using Dalamud.Interface.Fonts;

using Newtonsoft.Json;

using Xunit;

namespace Dalamud.Test.Configuration;

public class MovedTypeSerializationBinderTests
{
    [Theory]
    [InlineData(
        "Dalamud.Interface.FontIdentifier.GameFontAndFamilyId, Dalamud",
        "Dalamud.Interface.Fonts.GameFontId, Dalamud")]
    [InlineData(
        "Dalamud.Interface.Fonts.DalamudAssetFontAndFamilyId, Dalamud",
        "Dalamud.Interface.Fonts.DalamudAssetFontId, Dalamud")]
    [InlineData(
        "Dalamud.Interface.FontIdentifier.DalamudDefaultFontAndFamilyId",
        "Dalamud.Interface.Fonts.DalamudDefaultFontId")]
    [InlineData("Dalamud.Interface.FontIdentifier.SystemFontId", "Dalamud.Interface.Fonts.SystemFontFaceId")]
    [InlineData("Dalamud.Interface.FontIdentifier.SystemFontFamilyId", "Dalamud.Interface.Fonts.SystemFontFamilyId")]
    [InlineData("Dalamud.Interface.Fonts.SystemFontFaceId", "Dalamud.Interface.Fonts.SystemFontFaceId")]
    [InlineData(
        "System.Collections.Generic.List`1[[Dalamud.Interface.FontIdentifier.SystemFontId, Dalamud]], System",
        "System.Collections.Generic.List`1[[Dalamud.Interface.Fonts.SystemFontFaceId, Dalamud]], System")]
    [InlineData("System.String", "System.String")]
    public void MapTypeName_MapsOldNames(string saved, string expected) =>
        Assert.Equal(expected, MovedTypeSerializationBinder.MapTypeName(saved));

    [Theory]
    [InlineData("Dalamud.Interface.FontIdentifier.GameFontAndFamilyId, Dalamud")]
    [InlineData("Dalamud.Interface.Fonts.GameFontAndFamilyId, Dalamud")]
    [InlineData("Dalamud.Interface.Fonts.GameFontId, Dalamud")]
    public void TextStyle_ReadsOldFontIdTypes(string typeName)
    {
        var json = $$$"""{"FamilyId":{"$type":"{{{typeName}}}","GameFontFamily":{{{(int)GameFontFamily.Axis}}}}}""";

        // Without the binder, as a plugin with its own settings would read it, the converter maps the names.
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
        var plain = JsonConvert.DeserializeObject<TextStyle>(json, settings);
        Assert.Equal(GameFontFamily.Axis, Assert.IsType<GameFontId>(plain.FamilyId).GameFontFamily);

        // With the settings Dalamud reads its configuration with.
        settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = MovedTypeSerializationBinder.Instance,
        };
        var bound = JsonConvert.DeserializeObject<TextStyle>(json, settings);
        Assert.Equal(GameFontFamily.Axis, Assert.IsType<GameFontId>(bound.FamilyId).GameFontFamily);
    }
}
