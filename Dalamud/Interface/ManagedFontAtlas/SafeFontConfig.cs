using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Managed version of <see cref="ImFontConfig"/>, to avoid unnecessary heap allocation and use of unsafe blocks.
/// </summary>
public struct SafeFontConfig
{
    /// <summary>
    /// The raw config.
    /// </summary>
    public ImFontConfig Raw;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeFontConfig"/> struct.
    /// </summary>
    public SafeFontConfig()
    {
        this.OversampleH = 1;
        this.OversampleV = 1;
        this.PixelSnapH = true;
        this.GlyphMaxAdvanceX = float.MaxValue;
        this.RasterizerMultiply = 1f;
        this.RasterizerGamma = 1.7f;
        this.EllipsisChar = unchecked((char)-1);
        this.Raw.FontDataOwnedByAtlas = 1;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeFontConfig"/> struct,
    /// copying applicable values from an existing instance of <see cref="ImFontConfigPtr"/>.
    /// </summary>
    /// <param name="config">Config to copy from.</param>
    public unsafe SafeFontConfig(ImFontConfigPtr config)
        : this()
    {
        if (config.NativePtr is not null)
        {
            this.Raw = *config.NativePtr;
            this.Raw.GlyphRanges = null;
        }
    }

    /// <summary>
    /// Gets or sets the index of font within a TTF/OTF file.
    /// </summary>
    public int FontNo
    {
        get => this.Raw.FontNo;
        set => this.Raw.FontNo = EnsureRange(value, 0, int.MaxValue);
    }

    /// <summary>
    /// Gets or sets the desired size of the new font, in pixels.<br />
    /// Effectively, this is the line height.<br />
    /// Value is tied with <see cref="SizePt"/>.
    /// </summary>
    public float SizePx
    {
        get => this.Raw.SizePixels;
        set => this.Raw.SizePixels = EnsureRange(value, float.Epsilon, float.MaxValue);
    }

    /// <summary>
    /// Gets or sets the desired size of the new font, in points.<br />
    /// Effectively, this is the line height.<br />
    /// Value is tied with <see cref="SizePx"/>.
    /// </summary>
    public float SizePt
    {
        get => (this.Raw.SizePixels * 3) / 4;
        set => this.Raw.SizePixels = EnsureRange((value * 4) / 3, float.Epsilon, float.MaxValue);
    }

    /// <summary>
    /// Gets or sets the horizontal oversampling pixel count.<br />
    /// Rasterize at higher quality for sub-pixel positioning.<br />
    /// Note the difference between 2 and 3 is minimal so you can reduce this to 2 to save memory.<br />
    /// Read https://github.com/nothings/stb/blob/master/tests/oversample/README.md for details.
    /// </summary>
    public int OversampleH
    {
        get => this.Raw.OversampleH;
        set => this.Raw.OversampleH = EnsureRange(value, 1, int.MaxValue);
    }

    /// <summary>
    /// Gets or sets the vertical oversampling pixel count.<br />
    /// Rasterize at higher quality for sub-pixel positioning.<br />
    /// This is not really useful as we don't use sub-pixel positions on the Y axis.
    /// </summary>
    public int OversampleV
    {
        get => this.Raw.OversampleV;
        set => this.Raw.OversampleV = EnsureRange(value, 1, int.MaxValue);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to align every glyph to pixel boundary.<br />
    /// Useful e.g. if you are merging a non-pixel aligned font with the default font.<br />
    /// If enabled, you can set <see cref="OversampleH"/> and <see cref="OversampleV"/> to 1.
    /// </summary>
    public bool PixelSnapH
    {
        get => this.Raw.PixelSnapH != 0;
        set => this.Raw.PixelSnapH = value ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Gets or sets the extra spacing (in pixels) between glyphs.<br />
    /// Only X axis is supported for now.<br />
    /// Effectively, it is the letter spacing.
    /// </summary>
    public Vector2 GlyphExtraSpacing
    {
        get => this.Raw.GlyphExtraSpacing;
        set => this.Raw.GlyphExtraSpacing = new(
                   EnsureRange(value.X, float.MinValue, float.MaxValue),
                   EnsureRange(value.Y, float.MinValue, float.MaxValue));
    }

    /// <summary>
    /// Gets or sets the offset all glyphs from this font input.<br />
    /// Use this to offset fonts vertically when merging multiple fonts.
    /// </summary>
    public Vector2 GlyphOffset
    {
        get => this.Raw.GlyphOffset;
        set => this.Raw.GlyphOffset = new(
                   EnsureRange(value.X, float.MinValue, float.MaxValue),
                   EnsureRange(value.Y, float.MinValue, float.MaxValue));
    }

    /// <summary>
    /// Gets or sets the glyph ranges, which is a user-provided list of Unicode range.
    /// Each range has 2 values, and values are inclusive.<br />
    /// The list must be zero-terminated.<br />
    /// If empty or null, then all the glyphs from the font that is in the range of UCS-2 will be added.
    /// </summary>
    public ushort[]? GlyphRanges { get; set; }

    /// <summary>
    /// Gets or sets the minimum AdvanceX for glyphs.<br />
    /// Set only <see cref="GlyphMinAdvanceX"/> to align font icons.<br />
    /// Set both <see cref="GlyphMinAdvanceX"/>/<see cref="GlyphMaxAdvanceX"/> to enforce mono-space font.
    /// </summary>
    public float GlyphMinAdvanceX
    {
        get => this.Raw.GlyphMinAdvanceX;
        set => this.Raw.GlyphMinAdvanceX =
                   float.IsFinite(value)
                       ? value
                       : throw new ArgumentOutOfRangeException(
                             nameof(value),
                             value,
                             $"{nameof(this.GlyphMinAdvanceX)} must be a finite number.");
    }

    /// <summary>
    /// Gets or sets the maximum AdvanceX for glyphs.
    /// </summary>
    public float GlyphMaxAdvanceX
    {
        get => this.Raw.GlyphMaxAdvanceX;
        set => this.Raw.GlyphMaxAdvanceX =
                   float.IsFinite(value)
                       ? value
                       : throw new ArgumentOutOfRangeException(
                             nameof(value),
                             value,
                             $"{nameof(this.GlyphMaxAdvanceX)} must be a finite number.");
    }

    /// <summary>
    /// Gets or sets a value that either brightens (&gt;1.0f) or darkens (&lt;1.0f) the font output.<br />
    /// Brightening small fonts may be a good workaround to make them more readable.
    /// </summary>
    public float RasterizerMultiply
    {
        get => this.Raw.RasterizerMultiply;
        set => this.Raw.RasterizerMultiply = EnsureRange(value, float.Epsilon, float.MaxValue);
    }

    /// <summary>
    /// Gets or sets the gamma value for fonts.
    /// </summary>
    public float RasterizerGamma
    {
        get => this.Raw.RasterizerGamma;
        set => this.Raw.RasterizerGamma = EnsureRange(value, float.Epsilon, float.MaxValue);
    }

    /// <summary>
    /// Gets or sets a value explicitly specifying unicode codepoint of the ellipsis character.<br />
    /// When fonts are being merged first specified ellipsis will be used.
    /// </summary>
    public char EllipsisChar
    {
        get => (char)this.Raw.EllipsisChar;
        set => this.Raw.EllipsisChar = value;
    }

    /// <summary>
    /// Gets or sets the desired name of the new font. Names longer than 40 bytes will be partially lost.
    /// </summary>
    public unsafe string Name
    {
        get
        {
            fixed (void* pName = this.Raw.Name)
            {
                var span = new ReadOnlySpan<byte>(pName, 40);
                var firstNull = span.IndexOf((byte)0);
                if (firstNull != -1)
                    span = span[..firstNull];
                return Encoding.UTF8.GetString(span);
            }
        }

        set
        {
            fixed (void* pName = this.Raw.Name)
            {
                var span = new Span<byte>(pName, 40);
                Encoding.UTF8.GetBytes(value, span);
            }
        }
    }

    /// <summary>
    /// Gets or sets the desired font to merge with, if set.
    /// </summary>
    public unsafe ImFontPtr MergeFont
    {
        get => this.Raw.DstFont is not null ? this.Raw.DstFont : default;
        set
        {
            this.Raw.MergeMode = value.NativePtr is null ? (byte)0 : (byte)1;
            this.Raw.DstFont = value.NativePtr is null ? default : value.NativePtr;
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> with appropriate messages,
    /// if this <see cref="SafeFontConfig"/> has invalid values.
    /// </summary>
    public readonly void ThrowOnInvalidValues()
    {
        if (!(this.Raw.FontNo >= 0))
            throw new ArgumentException($"{nameof(this.FontNo)} must not be a negative number.");

        if (!(this.Raw.SizePixels > 0))
            throw new ArgumentException($"{nameof(this.SizePx)} must be a positive number.");

        if (!(this.Raw.OversampleH >= 1))
            throw new ArgumentException($"{nameof(this.OversampleH)} must be a negative number.");

        if (!(this.Raw.OversampleV >= 1))
            throw new ArgumentException($"{nameof(this.OversampleV)} must be a negative number.");

        if (!float.IsFinite(this.Raw.GlyphMinAdvanceX))
            throw new ArgumentException($"{nameof(this.GlyphMinAdvanceX)} must be a finite number.");

        if (!float.IsFinite(this.Raw.GlyphMaxAdvanceX))
            throw new ArgumentException($"{nameof(this.GlyphMaxAdvanceX)} must be a finite number.");

        if (!(this.Raw.RasterizerMultiply > 0))
            throw new ArgumentException($"{nameof(this.RasterizerMultiply)} must be a positive number.");

        if (!(this.Raw.RasterizerGamma > 0))
            throw new ArgumentException($"{nameof(this.RasterizerGamma)} must be a positive number.");

        if (this.GlyphRanges is { Length: > 0 } ranges)
        {
            if (ranges[0] == 0)
            {
                throw new ArgumentException(
                    "Font ranges cannot start with 0.",
                    nameof(this.GlyphRanges));
            }

            if (ranges[(ranges.Length - 1) & ~1] != 0)
            {
                throw new ArgumentException(
                    "Font ranges must terminate with a zero at even indices.",
                    nameof(this.GlyphRanges));
            }
        }
    }

    private static T EnsureRange<T>(T value, T min, T max, [CallerMemberName] string callerName = "")
        where T : INumber<T>
    {
        if (value < min)
            throw new ArgumentOutOfRangeException(callerName, value, $"{callerName} cannot be less than {min}.");
        if (value > max)
            throw new ArgumentOutOfRangeException(callerName, value, $"{callerName} cannot be more than {max}.");

        return value;
    }
}
