using System.IO;

using Dalamud.Game.Text.Evaluator;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload containing an auto-translation/completion chat message.
/// </summary>
public class AutoTranslatePayload : Payload, ITextProvider
{
    private string? text;
    private ReadOnlySeString payload;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoTranslatePayload"/> class.
    /// Creates a new auto-translate payload.
    /// </summary>
    /// <param name="group">The group id for this message.</param>
    /// <param name="key">The key/row id for this message.  Which table this is in depends on the group id and details the Completion table.</param>
    /// <remarks>
    /// This table is somewhat complicated in structure, and so using this constructor may not be very nice.
    /// There is probably little use to create one of these, however.
    /// </remarks>
    public AutoTranslatePayload(uint group, uint key)
    {
        // TODO: friendlier ctor? not sure how to handle that given how weird the tables are
        this.Group = group;
        this.Key = key;

        var ssb = Lumina.Text.SeStringBuilder.SharedPool.Get();
        this.payload = ssb.BeginMacro(MacroCode.Fixed)
                     .AppendUIntExpression(group - 1)
                     .AppendUIntExpression(key)
                     .EndMacro()
                     .ToReadOnlySeString();
        Lumina.Text.SeStringBuilder.SharedPool.Return(ssb);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoTranslatePayload"/> class.
    /// </summary>
    internal AutoTranslatePayload()
    {
        this.payload = default; // parsed by DecodeImpl
    }
    
    /// <summary>
    /// Gets the autotranslate group.
    /// </summary>
    [JsonProperty("group")]
    public uint Group { get; private set; }

    /// <summary>
    /// Gets the autotranslate key.
    /// </summary>
    [JsonProperty("key")]
    public uint Key { get; private set; }

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.AutoTranslateText;

    /// <summary>
    /// Gets the actual text displayed in-game for this payload.
    /// </summary>
    /// <remarks>
    /// Value is evaluated lazily and cached.
    /// </remarks>
    public string Text
    {
        get
        {
            if (this.Group is 100 or 200)
            {
                return this.text ??= Service<SeStringEvaluator>.Get().Evaluate(this.payload).ToString();
            }

            // wrap the text in the colored brackets that is uses in-game, since those are not actually part of any of the payloads
            return this.text ??= $"{(char)SeIconChar.AutoTranslateOpen} {Service<SeStringEvaluator>.Get().Evaluate(this.payload)} {(char)SeIconChar.AutoTranslateClose}";
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{this.Type} - Group: {this.Group}, Key: {this.Key}, Text: {this.Text}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        return this.payload.Data.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        var body = reader.ReadBytes((int)(endOfStream - reader.BaseStream.Position));
        var rosps = new ReadOnlySePayloadSpan(ReadOnlySePayloadType.Macro, MacroCode.Fixed, body.AsSpan());

        var span = rosps.EnvelopeByteLength <= 512 ? stackalloc byte[rosps.EnvelopeByteLength] : new byte[rosps.EnvelopeByteLength];
        rosps.WriteEnvelopeTo(span);
        this.payload = new ReadOnlySeString(span);

        if (rosps.TryGetExpression(out var expr1, out var expr2)
            && expr1.TryGetUInt(out var group)
            && expr2.TryGetUInt(out var key))
        {
            this.Group = group + 1;
            this.Key = key;
        }
    }
}
