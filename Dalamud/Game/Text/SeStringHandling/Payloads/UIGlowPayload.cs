using System.Collections.Generic;
using System.IO;

using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing a UI glow color applied to following text payloads.
/// </summary>
public class UIGlowPayload : Payload
{
    private UIColor color;

    [JsonProperty]
    private ushort colorKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIGlowPayload"/> class.
    /// Creates a new UIForegroundPayload for the given UIColor key.
    /// </summary>
    /// <param name="colorKey">A UIColor key.</param>
    public UIGlowPayload(ushort colorKey)
    {
        this.colorKey = colorKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIGlowPayload"/> class.
    /// Creates a new UIForegroundPayload for the given UIColor key.
    /// </summary>
    internal UIGlowPayload()
    {
    }

    /// <summary>
    /// Gets a payload representing disabling glow color on following text.
    /// </summary>
    // TODO Make this work with DI
    public static UIGlowPayload UIGlowOff => new(0);

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.UIGlow;

    /// <summary>
    /// Gets or sets the color key used as a lookup in the UIColor table for this glow color.
    /// </summary>
    [JsonIgnore]
    public ushort ColorKey
    {
        get
        {
            return this.colorKey;
        }

        set
        {
            this.colorKey = value;
            this.color = null;
            this.Dirty = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether or not this payload represents applying a glow color, or disabling one.
    /// </summary>
    public bool IsEnabled => this.ColorKey != 0;

    /// <summary>
    /// Gets the Red/Green/Blue values for this glow color, encoded as a typical hex color.
    /// </summary>
    [JsonIgnore]
    public uint RGB => this.UIColor.UIGlow & 0xFFFFFF;

    /// <summary>
    /// Gets a Lumina UIColor object representing this payload.  The actual color data is at UIColor.UIGlow.
    /// </summary>
    /// <remarks>
    /// The value is evaluated lazily and cached.
    /// </remarks>
    [JsonIgnore]
    public UIColor UIColor => this.color ??= this.DataResolver.GetExcelSheet<UIColor>().GetRow(this.colorKey);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - UIColor: {this.colorKey} color: {(this.IsEnabled ? this.RGB : 0)}";
    }

    /// <inheritdoc/>
    protected override byte[] EncodeImpl()
    {
        var colorBytes = MakeInteger(this.colorKey);
        var chunkLen = colorBytes.Length + 1;

        var bytes = new List<byte>(new byte[]
        {
            START_BYTE, (byte)SeStringChunkType.UIGlow, (byte)chunkLen,
        });

        bytes.AddRange(colorBytes);
        bytes.Add(END_BYTE);

        return bytes.ToArray();
    }

    /// <inheritdoc/>
    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        this.colorKey = (ushort)GetInteger(reader);
    }
}
