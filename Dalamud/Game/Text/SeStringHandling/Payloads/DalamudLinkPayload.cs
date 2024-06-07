using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// This class represents a custom Dalamud clickable chat link.
/// </summary>
public class DalamudLinkPayload : Payload
{
    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.DalamudLink;

    /// <summary>
    /// Gets the plugin command ID to be linked.
    /// </summary>
    public uint CommandId { get; internal set; } = 0;

    /// <summary>
    /// Gets the plugin name to be linked.
    /// </summary>
    public string Plugin { get; internal set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} -  Plugin: {this.Plugin}, Command: {this.CommandId}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var pluginBytes = Encoding.UTF8.GetBytes(this.Plugin);
        var commandBytes = MakeInteger(this.CommandId);
        var chunkLen = 3 + pluginBytes.Length + commandBytes.Length;

        if (chunkLen > 255)
        {
            throw new Exception("Chunk is too long. Plugin name exceeds limits for DalamudLinkPayload");
        }

        var bytes = new List<byte> { START_BYTE, (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.DalamudLink };
        bytes.Add((byte)pluginBytes.Length);
        bytes.AddRange(pluginBytes);
        bytes.AddRange(commandBytes);
        bytes.Add(END_BYTE);
        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.Plugin = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
        this.CommandId = GetInteger(reader);
    }
}
