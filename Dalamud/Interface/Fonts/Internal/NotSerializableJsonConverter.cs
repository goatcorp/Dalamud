using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>For marking a type unserializable.</summary>
internal sealed class NotSerializableJsonConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => true;

    /// <inheritdoc/>
    public override object ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        throw new NotSupportedException();

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        throw new JsonSerializationException($"{value?.GetType().Name} cannot be serialized.");
}
