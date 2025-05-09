using System.Collections.Generic;
using System.IO;

using Dalamud.Data;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>
/// An SeString Payload that allows text to have a specific color. The color selected will be determined by the
/// <see cref="Lumina.Excel.Sheets.UIColor.Dark"/> theme's coloring, regardless of the active theme.
/// </summary>
public class UIForegroundPayload : Payload
{
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
    /// Gets a value indicating whether this payload represents applying a foreground color, or disabling one.
    /// </summary>
    public bool IsEnabled => this.ColorKey != 0;

    /// <summary>
    /// Gets a Lumina UIColor object representing this payload.  The actual color data is at UIColor.UIForeground.
    /// </summary>
    [JsonIgnore]
    public RowRef<UIColor> UIColor => LuminaUtils.CreateRef<UIColor>(this.colorKey);

    /// <summary>
    /// Gets or sets the color key used as a lookup in the UIColor table for this foreground color.
    /// </summary>
    [JsonIgnore]
    public ushort ColorKey
    {
        get => this.colorKey;

        set
        {
            this.colorKey = value;
            this.Dirty = true;
        }
    }

    /// <summary>
    /// Gets the Red/Green/Blue/Alpha values for this foreground color, encoded as a typical hex color.
    /// </summary>
    [JsonIgnore]
    public uint RGBA => this.UIColor.Value.Dark;

    /// <summary>
    /// Gets the ABGR value for this foreground color, as ImGui requires it in PushColor.
    /// </summary>
    [JsonIgnore]
    public uint ABGR => Interface.ColorHelpers.SwapEndianness(this.UIColor.Value.Dark);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{this.Type} - UIColor: {this.colorKey} color: {(this.IsEnabled ? this.RGBA : 0)}";
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
