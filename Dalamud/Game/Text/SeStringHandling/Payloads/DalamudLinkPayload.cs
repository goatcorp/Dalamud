using System.IO;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// This class represents a custom Dalamud clickable chat link.
/// </summary>
public class DalamudLinkPayload : Payload
{
    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.DalamudLink;

    /// <summary>Gets the plugin command ID to be linked.</summary>
    public Guid CommandId { get; internal set; }

    /// <summary>Gets an optional extra integer value 1.</summary>
    public int Extra1 { get; internal set; }

    /// <summary>Gets an optional extra integer value 2.</summary>
    public int Extra2 { get; internal set; }

    /// <summary>Gets the plugin name to be linked.</summary>
    public string Plugin { get; internal set; } = string.Empty;

    /// <summary>Gets an optional extra string.</summary>
    public string ExtraString { get; internal set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{this.Type} - {this.Plugin} ({this.CommandId}/{this.Extra1}/{this.Extra2}/{this.ExtraString})";

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var ssb = Lumina.Text.SeStringBuilder.SharedPool.Get();
        var res = ssb.BeginMacro(MacroCode.Link)
                     .AppendIntExpression((int)EmbeddedInfoType.DalamudLink - 1)
                     .AppendStringExpression(this.CommandId.ToString())
                     .AppendIntExpression(this.Extra1)
                     .AppendIntExpression(this.Extra2)
                     .BeginStringExpression()
                     .Append(JsonConvert.SerializeObject(new[] { this.Plugin, this.ExtraString }))
                     .EndExpression()
                     .EndMacro()
                     .ToArray();
        Lumina.Text.SeStringBuilder.SharedPool.Return(ssb);
        return res;
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // Note: Payload.DecodeChunk already took the first int expr (DalamudLink).

        var body = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
        var rosps = new ReadOnlySePayloadSpan(ReadOnlySePayloadType.Macro, MacroCode.Link, body.AsSpan());

        if (!rosps.TryGetExpression(
                out var commandIdExpression,
                out var extra1Expression,
                out var extra2Expression,
                out var compositeExpression))
        {
            if (!rosps.TryGetExpression(out var pluginExpression, out commandIdExpression))
                return;

            if (!pluginExpression.TryGetString(out var pluginString))
                return;

            if (!commandIdExpression.TryGetString(out var commandId))
                return;

            this.Plugin = pluginString.ExtractText();
            this.CommandId = Guid.Parse(commandId.ExtractText());
        }
        else
        {
            if (!commandIdExpression.TryGetString(out var commandId))
                return;

            if (!extra1Expression.TryGetInt(out var extra1))
                return;

            if (!extra2Expression.TryGetInt(out var extra2))
                return;

            if (!compositeExpression.TryGetString(out var compositeString))
                return;

            string[] extraData;
            try
            {
                extraData = JsonConvert.DeserializeObject<string[]>(compositeString.ExtractText());
            }
            catch
            {
                return;
            }

            this.CommandId = Guid.Parse(commandId.ExtractText());
            this.Extra1 = extra1;
            this.Extra2 = extra2;
            this.Plugin = extraData[0];
            this.ExtraString = extraData[1];
        }
    }
}
