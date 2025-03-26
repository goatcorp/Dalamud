using Newtonsoft.Json;

namespace Dalamud.Common.Game;

/// <summary>
/// Converts a <see cref="GameVersion"/> to and from a string (e.g. <c>"2010.01.01.1234.5678"</c>).
/// </summary>
public sealed class GameVersionConverter : JsonConverter
{
    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                break;
            case GameVersion:
                writer.WriteValue(value.ToString());
                break;
            default:
                throw new JsonSerializationException("Expected GameVersion object value");
        }
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            try
            {
                return new GameVersion((string)reader.Value!);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error parsing GameVersion string: {reader.Value}", ex);
            }
        }

        throw new JsonSerializationException($"Unexpected token or value when parsing GameVersion. Token: {reader.TokenType}, Value: {reader.Value}");
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(GameVersion);
    }
}
