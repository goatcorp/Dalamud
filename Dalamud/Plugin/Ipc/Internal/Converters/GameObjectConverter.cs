using System.IO;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

using Newtonsoft.Json;

namespace Dalamud.Plugin.Ipc.Internal.Converters;

/// <summary>
/// JSON converter for IGameObject and its derived types.
/// </summary>
internal sealed class GameObjectConverter : JsonConverter<IGameObject>
{
    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, IGameObject? value, JsonSerializer serializer) =>
        writer.WriteValue(value?.Address.ToString());

    /// <inheritdoc/>
    public override IGameObject? ReadJson(
        JsonReader reader,
        Type objectType,
        IGameObject? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.String)
            throw new InvalidDataException("String is expected.");
        
        if (!nint.TryParse(reader.Value as string, out var v))
            throw new InvalidDataException("Could not parse address.");
        
        if (!ThreadSafety.IsMainThread)
            throw new InvalidOperationException("Cannot send GameObjects from non-main thread over IPC.");
        
        var ot = Service<ObjectTable>.Get();
        foreach (var go in ot)
        {
            if (go.Address == v)
                return go;
        }

        return ot.CreateObjectReference(v);
    }
}
