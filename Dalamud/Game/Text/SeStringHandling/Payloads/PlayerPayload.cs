using System.Collections.Generic;
using System.IO;
using System.Text;

using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;
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
        var chunkLen = this.playerName.Length + 7;
        var bytes = new List<byte>()
        {
            START_BYTE,
            (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.PlayerName,
            /* unk */ 0x01,
            (byte)(this.serverId + 1), // I didn't want to deal with single-byte values in MakeInteger, so we have to do the +1 manually
            /* unk */ 0x01,
            /* unk */ 0xFF, // these sometimes vary but are frequently this
            (byte)(this.playerName.Length + 1),
        };

        bytes.AddRange(Encoding.UTF8.GetBytes(this.playerName));
        bytes.Add(END_BYTE);

        // TODO: should these really be here? additional payloads should come in separately already...

        // encoded names are followed by the name in plain text again
        // use the payload parsing for consistency, as this is technically a new chunk
        bytes.AddRange(new TextPayload(this.playerName).Encode());

        // unsure about this entire packet, but it seems to always follow a name
        bytes.AddRange(new byte[]
        {
            START_BYTE, (byte)SeStringChunkType.Interactable, 0x07, (byte)EmbeddedInfoType.LinkTerminator,
            0x01, 0x01, 0x01, 0xFF, 0x01,
            END_BYTE,
        });

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // unk
        reader.ReadByte();

        this.serverId = GetInteger(reader);

        // unk
        reader.ReadBytes(2);

        var nameLen = (int)GetInteger(reader);
        this.playerName = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
    }
}
