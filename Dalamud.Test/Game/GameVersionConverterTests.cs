using Dalamud.Common.Game;

using JetBrains.Annotations;

using Newtonsoft.Json;

using Xunit;

namespace Dalamud.Test.Game;

public class GameVersionConverterTests
{
    [Fact]
    public void ReadJson_ConvertsFromString()
    {
        var serialized = """
                         {
                            "Version": "2020.06.15.0000.0000"
                         }
                         """;
        var deserialized = JsonConvert.DeserializeObject<TestSerializationClass>(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(GameVersion.Parse("2020.06.15.0000.0000"), deserialized.Version);
    }


    [Fact]
    public void ReadJson_ConvertsFromNull()
    {
        var serialized = """
                         {
                            "Version": null
                         }
                         """;
        var deserialized = JsonConvert.DeserializeObject<TestSerializationClass>(serialized);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Version);
    }

    [Fact]
    public void ReadJson_WhenInvalidType_Throws()
    {
        var serialized = """
                         {
                            "Version": 2
                         }
                         """;
        Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<TestSerializationClass>(serialized));
    }

    [Fact]
    public void ReadJson_WhenInvalidVersion_Throws()
    {
        var serialized = """
                         {
                            "Version": "junk"
                         }
                         """;
        Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<TestSerializationClass>(serialized));
    }

    [Fact]
    public void WriteJson_ConvertsToString()
    {
        var deserialized = new TestSerializationClass
        {
            Version = GameVersion.Parse("2020.06.15.0000.0000"),
        };
        var serialized = JsonConvert.SerializeObject(deserialized);

        Assert.Equal("""{"Version":"2020.06.15.0000.0000"}""", RemoveWhitespace(serialized));
    }

    [Fact]
    public void WriteJson_ConvertsToNull()
    {
        var deserialized = new TestSerializationClass
        {
            Version = null,
        };
        var serialized = JsonConvert.SerializeObject(deserialized);

        Assert.Equal("""{"Version":null}""", RemoveWhitespace(serialized));
    }

    [Fact]
    public void WriteJson_WhenInvalidVersion_Throws()
    {
        var deserialized = new TestWrongTypeSerializationClass
        {
            Version = 42,
        };
        Assert.Throws<JsonSerializationException>(() => JsonConvert.SerializeObject(deserialized));
    }

    [Fact]
    public void CanConvert_WhenGameVersion_ReturnsTrue()
    {
        var converter = new GameVersionConverter();
        Assert.True(converter.CanConvert(typeof(GameVersion)));
    }

    [Fact]
    public void CanConvert_WhenNotGameVersion_ReturnsFalse()
    {
        var converter = new GameVersionConverter();
        Assert.False(converter.CanConvert(typeof(int)));
    }

    [Fact]
    public void CanConvert_WhenNull_ReturnsFalse()
    {
        var converter = new GameVersionConverter();
        Assert.False(converter.CanConvert(null!));
    }

    private static string RemoveWhitespace(string input)
    {
        return input.Replace(" ", "").Replace("\r", "").Replace("\n", "");
    }

    private class TestSerializationClass
    {
        [JsonConverter(typeof(GameVersionConverter))]
        [CanBeNull]
        public GameVersion Version { get; init; }
    }

    private class TestWrongTypeSerializationClass
    {
        [JsonConverter(typeof(GameVersionConverter))]
        public int Version { get; init; }
    }
}
