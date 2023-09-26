using System;
using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace Dalamud;

/// <summary>
/// Converts a <see cref="OSPlatform"/> to and from a string (e.g. <c>"FreeBSD"</c>).
/// </summary>
public sealed class OSPlatformConverter : JsonConverter
{
    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else if (value is OSPlatform)
        {
            writer.WriteValue(value.ToString());
        }
        else
        {
            throw new JsonSerializationException("Expected OSPlatform object value");
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
        else
        {
            if (reader.TokenType == JsonToken.String)
            {
                try
                {
                    return OSPlatform.Create((string)reader.Value!);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Error parsing OSPlatform string: {reader.Value}", ex);
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected token or value when parsing OSPlatform. Token: {reader.TokenType}, Value: {reader.Value}");
            }
        }
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
        return objectType == typeof(OSPlatform);
    }
}
