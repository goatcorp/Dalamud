using System.IO;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

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
        return new Lumina.Text.SeStringBuilder()
            .BeginMacro(MacroCode.Link)
            .AppendIntExpression((int)EmbeddedInfoType.DalamudLink - 1)
            .AppendStringExpression(this.Plugin)
            .AppendUIntExpression(this.CommandId)
            .EndMacro()
            .ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // Note: Payload.DecodeChunk already took the first int expr (DalamudLink).

        var body = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
        var rosps = new ReadOnlySePayloadSpan(ReadOnlySePayloadType.Macro, MacroCode.Link, body.AsSpan());

        if (!rosps.TryGetExpression(out var pluginExpression, out var commandIdExpression))
            return;

        if (!pluginExpression.TryGetString(out var pluginString))
            return;

        if (!commandIdExpression.TryGetUInt(out var commandId))
            return;

        this.Plugin = pluginString.ExtractText();
        this.CommandId = commandId;
    }
}
