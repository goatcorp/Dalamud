using System.Collections.Generic;
using System.IO;

using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload representing a UI foreground color applied to following text payloads.
/// </summary>
public class UIForegroundPayload : Payload
{
    private UIColor color;

    [JsonProperty]
    private ushort colorKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIForegroundPayload"/> class.
    /// Creates a new UIForegroundPayload for the given UIColor key.
    /// </summary>
    /// <param name="colorKey">A UIColor key.</param>
    public UIForegroundPayload(ushort colorKey)
    {
        this.colorKey = colorKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UIForegroundPayload"/> class.
    /// Creates a new UIForegroundPayload for the given UIColor key.
    /// </summary>
    internal UIForegroundPayload()
    {
    }

    /// <summary>
    /// Gets a payload representing disabling foreground color on following text.
    /// </summary>
    // TODO Make this work with DI
    public static UIForegroundPayload UIForegroundOff => new(0);

    /// <inheritdoc/>
    public override PayloadType Type => PayloadType.UIForeground;

    /// <summary>
    /// Gets a value indicating whether or not this payload represents applying a foreground color, or disabling one.
    /// </summary>
    public bool IsEnabled => this.ColorKey != 0;

    /// <summary>
    /// Gets a Lumina UIColor object representing this payload.  The actual color data is at UIColor.UIForeground.
    /// </summary>
    /// <remarks>
    /// The value is evaluated lazily and cached.
    /// </remarks>
    [JsonIgnore]
    public UIColor UIColor => this.color ??= this.DataResolver.GetExcelSheet<UIColor>().GetRow(this.colorKey);

    /// <summary>
    /// Gets or sets the color key used as a lookup in the UIColor table for this foreground color.
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
    /// Gets the Red/Green/Blue values for this foreground color, encoded as a typical hex color.
    /// </summary>
    [JsonIgnore]
    public uint RGB => this.UIColor.UIForeground & 0xFFFFFF;

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
            START_BYTE, (byte)SeStringChunkType.UIForeground, (byte)chunkLen,
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
