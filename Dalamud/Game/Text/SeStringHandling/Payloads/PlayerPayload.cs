using System.Collections.Generic;
using System.IO;
using System.Text;

using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing a player link.
/// </summary>
public class PlayerPayload : Payload
{
    [JsonProperty]
    private uint serverId;

    [JsonProperty]
    private string playerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPayload"/> class.
    /// Create a PlayerPayload link for the specified player.
    /// </summary>
    /// <param name="playerName">The player's displayed name.</param>
    /// <param name="serverId">The player's home server id.</param>
    public PlayerPayload(string playerName, uint serverId)
    {
        this.playerName = playerName;
        this.serverId = serverId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPayload"/> class.
    /// Create a PlayerPayload link for the specified player.
    /// </summary>
    internal PlayerPayload()
    {
    }

    /// <summary>
    /// Gets the Lumina object representing the player's home server.
    /// </summary>
    [JsonIgnore]
    public RowRef<World> World => LuminaUtils.CreateRef<World>(this.serverId);

    /// <summary>
    /// Gets or sets the player's displayed name.  This does not contain the server name.
    /// </summary>
    [JsonIgnore]
    public string PlayerName
    {
        get
        {
            return this.playerName;
        }

        set
        {
            this.playerName = value;
            this.Dirty = true;
        }
    }

    /// <summary>
    /// Gets the text representation of this player link matching how it might appear in-game.
    /// The world name will always be present.
    /// </summary>
    [JsonIgnore]
    public string DisplayedName => $"{this.PlayerName}{(char)SeIconChar.CrossWorld}{this.World.ValueNullable?.Name}";

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Player;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - PlayerName: {this.PlayerName}, ServerId: {this.serverId}, ServerName: {this.World.ValueNullable?.Name}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var ssb = Lumina.Text.SeStringBuilder.SharedPool.Get();
        var res = ssb
                  .PushLinkCharacter(this.playerName, this.serverId)
                  .Append(this.playerName)
                  .PopLink()
                  .ToArray();
        Lumina.Text.SeStringBuilder.SharedPool.Return(ssb);
        return res;
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        var body = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
        var rosps = new ReadOnlySePayloadSpan(ReadOnlySePayloadType.Macro, MacroCode.Link, body.AsSpan());

        if (!rosps.TryGetExpression(out _, out var worldIdExpression, out _, out var characterNameExpression))
            return;

        if (!worldIdExpression.TryGetUInt(out var worldId))
            return;

        if (!characterNameExpression.TryGetString(out var characterName))
            return;

        this.serverId = worldId;
        this.playerName = characterName.ExtractText();
    }
}
