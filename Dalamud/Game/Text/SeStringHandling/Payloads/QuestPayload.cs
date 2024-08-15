using System.Collections.Generic;
using System.IO;

using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing an interactable quest link.
/// </summary>
public class QuestPayload : Payload
{
    [JsonProperty]
    private uint questId;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestPayload"/> class.
    /// Creates a payload representing an interactable quest link for the specified quest.
    /// </summary>
    /// <param name="questId">The id of the quest.</param>
    public QuestPayload(uint questId)
    {
        this.questId = questId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestPayload"/> class.
    /// Creates a payload representing an interactable quest link for the specified quest.
    /// </summary>
    internal QuestPayload()
    {
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Quest;

    /// <summary>
    /// Gets the underlying Lumina Quest represented by this payload.
    /// </summary>
    [JsonIgnore]
    public RowRef<Quest> Quest => LuminaUtils.CreateRef<Quest>(this.questId);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.Type} - QuestId: {this.questId}, Name: {this.Quest.ValueNullable?.Name.ExtractText() ?? "QUEST NOT FOUND"}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var idBytes = MakeInteger((ushort)this.questId);
        var chunkLen = idBytes.Length + 4;

        var bytes = new List<byte>()
        {
            START_BYTE, (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.QuestLink,
        };

        bytes.AddRange(idBytes);
        bytes.AddRange(new byte[] { 0x01, 0x01, END_BYTE });
        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        // Game uses int16, Luimina uses int32
        this.questId = GetInteger(reader) + 65536;
    }
}
