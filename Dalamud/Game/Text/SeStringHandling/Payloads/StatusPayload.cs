using System.Collections.Generic;
using System.IO;

using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing an interactable status link.
/// </summary>
public class StatusPayload : Payload
{
    [JsonProperty]
    private uint statusId;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusPayload"/> class.
    /// Creates a new StatusPayload for the given status id.
    /// </summary>
    /// <param name="statusId">The id of the Status for this link.</param>
    public StatusPayload(uint statusId)
    {
        this.statusId = statusId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusPayload"/> class.
    /// Creates a new StatusPayload for the given status id.
    /// </summary>
    internal StatusPayload()
    {
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Status;

    /// <summary>
    /// Gets the Lumina Status object represented by this payload.
    /// </summary>
    [JsonIgnore]
    public RowRef<Status> Status => LuminaUtils.CreateRef<Status>(this.statusId);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - StatusId: {this.statusId}, Name: {this.Status.ValueNullable?.Name}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var idBytes = MakeInteger(this.statusId);

        var chunkLen = idBytes.Length + 7;
        var bytes = new List<byte>()
        {
            START_BYTE, (byte)SeStringChunkType.Interactable, (byte)chunkLen, (byte)EmbeddedInfoType.Status,
        };

        bytes.AddRange(idBytes);
        // unk
        bytes.AddRange(new byte[] { 0x01, 0x01, 0xFF, 0x02, 0x20, END_BYTE });

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.statusId = GetInteger(reader);
    }
}
