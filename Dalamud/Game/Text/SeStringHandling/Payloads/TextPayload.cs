using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing a plain text string.
/// </summary>
public class TextPayload : Payload, ITextProvider
{
    [JsonProperty]
    private string? text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPayload"/> class.
    /// Creates a new TextPayload for the given text.
    /// </summary>
    /// <param name="text">The text to include for this payload.</param>
    public TextPayload(string? text)
    {
        this.text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPayload"/> class.
    /// Creates a new TextPayload for the given text.
    /// </summary>
    internal TextPayload()
    {
    }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.RawText;

    /// <summary>
    /// Gets or sets the text contained in this payload.
    /// This may contain SE's special unicode characters.
    /// </summary>
    [JsonIgnore]
    public string? Text
    {
        get => this.text;

        set
        {
            this.text = value;
            this.Dirty = true;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - Text: {this.Text}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        // special case to allow for empty text payloads, so users don't have to check
        // this may change or go away
        if (string.IsNullOrEmpty(this.text))
        {
            return Array.Empty<byte>();
        }

        return Encoding.UTF8.GetBytes(this.text);
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        var textBytes = new List<byte>();

        while (reader.BaseStream.Position < endOfStream)
        {
            var nextByte = reader.ReadByte();
            if (nextByte == START_BYTE)
            {
                // rewind since this byte isn't part of this payload
                reader.BaseStream.Position--;
                break;
            }

            textBytes.Add(nextByte);
        }

        if (textBytes.Count > 0)
        {
            // TODO: handling of the game's assorted special unicode characters
            this.text = Encoding.UTF8.GetString(textBytes.ToArray());
        }
    }
}
