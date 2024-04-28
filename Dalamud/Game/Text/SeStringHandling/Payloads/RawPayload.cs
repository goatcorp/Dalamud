using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing unhandled raw payload data.
/// Mainly useful for constructing unhandled hardcoded payloads, or forwarding any unknown
/// payloads without modification.
/// </summary>
public class RawPayload : Payload
{
    [JsonProperty]
    private byte chunkType;

    [JsonProperty]
    private byte[] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawPayload"/> class.
    /// </summary>
    /// <param name="data">The payload data.</param>
    public RawPayload(byte[] data)
    {
        // this payload is 'special' in that we require the entire chunk to be passed in
        // and not just the data after the header
        // This sets data to hold the chunk data fter the header, excluding the END_BYTE
        this.chunkType = data[1];
        this.data = data.Skip(3).Take(data.Length - 4).ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawPayload"/> class.
    /// </summary>
    /// <param name="chunkType">The chunk type.</param>
    [JsonConstructor]
    internal RawPayload(byte chunkType)
    {
        this.chunkType = chunkType;
    }

    /// <summary>
    /// Gets a fixed Payload representing a common link-termination sequence, found in many payload chains.
    /// </summary>
    public static RawPayload LinkTerminator => new(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 });

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.Unknown;

    /// <summary>
    /// Gets the entire payload byte sequence for this payload.
    /// The returned data is a clone and modifications will not be persisted.
    /// </summary>
    [JsonIgnore]
    public byte[] Data
    {
        // this is a bit different from the underlying data
        // We need to store just the chunk data for decode to behave nicely, but when reading data out
        // it makes more sense to get the entire payload
        get
        {
            // for now don't allow modifying the contents
            // because we don't really have a way to track Dirty
            return (byte[])this.Encode().Clone();
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is RawPayload rp)
        {
            if (rp.Data.Length != this.Data.Length) return false;
            return !this.Data.Where((t, i) => rp.Data[i] != t).Any();
        }

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Type, this.chunkType, this.data);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - Data: {BitConverter.ToString(this.Data).Replace("-", " ")}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var chunkLen = this.data.Length + 1;

        var bytes = new List<byte>()
        {
            START_BYTE,
            this.chunkType,
            (byte)chunkLen,
        };
        bytes.AddRange(this.data);

        bytes.Add(END_BYTE);

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.data = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position + 1));
    }
}
