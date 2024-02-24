namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Describes a font based on game resource file.
/// </summary>
public struct GameFontStyle
{
    /// <summary>
    /// Font family of the font.
    /// </summary>
    public GameFontFamilyAndSize FamilyAndSize;

    /// <summary>
    /// Size of the font in pixels unit.
    /// </summary>
    public float SizePx;

    /// <summary>
    /// Weight of the font.
    ///
    /// 0 is unaltered.
    /// Any value greater than 0 will make it bolder.
    /// </summary>
    public float Weight;

    /// <summary>
    /// Skewedness of the font.
    ///
    /// 0 is unaltered.
    /// Greater than 1 will make upper part go rightwards.
    /// Less than 1 will make lower part go rightwards.
    /// </summary>
    public float SkewStrength;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontStyle"/> struct.
    /// </summary>
    /// <param name="family">Font family.</param>
    /// <param name="sizePx">Size in pixels.</param>
    public GameFontStyle(GameFontFamily family, float sizePx)
    {
        this.FamilyAndSize = GetRecommendedFamilyAndSize(family, sizePx * 3 / 4);
        this.Weight = this.SkewStrength = 0f;
        this.SizePx = sizePx;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFontStyle"/> struct.
    /// </summary>
    /// <param name="familyAndSize">Font family and size.</param>
    public GameFontStyle(GameFontFamilyAndSize familyAndSize)
    {
        this.FamilyAndSize = familyAndSize;
        this.Weight = this.SkewStrength = 0f;

        // Dummy assignment to satisfy requirements
        this.SizePx = 0;

        this.SizePx = this.BaseSizePx;
    }

    /// <summary>
    /// Gets or sets the size of the font in points unit.
    /// </summary>
    public float SizePt
    {
        readonly get => this.SizePx * 3 / 4;
        set => this.SizePx = value * 4 / 3;
    }

    /// <summary>
    /// Gets or sets the base skew strength.
    /// </summary>
    public float BaseSkewStrength
    {
        readonly get => this.SkewStrength * this.BaseSizePx / this.SizePx;
        set => this.SkewStrength = value * this.SizePx / this.BaseSizePx;
    }

    /// <summary>
    /// Gets the font family.
    /// </summary>
    public readonly GameFontFamily Family => this.FamilyAndSize switch
    {
        GameFontFamilyAndSize.Undefined => GameFontFamily.Undefined,
        GameFontFamilyAndSize.Axis96 => GameFontFamily.Axis,
        GameFontFamilyAndSize.Axis12 => GameFontFamily.Axis,
        GameFontFamilyAndSize.Axis14 => GameFontFamily.Axis,
        GameFontFamilyAndSize.Axis18 => GameFontFamily.Axis,
        GameFontFamilyAndSize.Axis36 => GameFontFamily.Axis,
        GameFontFamilyAndSize.Jupiter16 => GameFontFamily.Jupiter,
        GameFontFamilyAndSize.Jupiter20 => GameFontFamily.Jupiter,
        GameFontFamilyAndSize.Jupiter23 => GameFontFamily.Jupiter,
        GameFontFamilyAndSize.Jupiter45 => GameFontFamily.JupiterNumeric,
        GameFontFamilyAndSize.Jupiter46 => GameFontFamily.Jupiter,
        GameFontFamilyAndSize.Jupiter90 => GameFontFamily.JupiterNumeric,
        GameFontFamilyAndSize.Meidinger16 => GameFontFamily.Meidinger,
        GameFontFamilyAndSize.Meidinger20 => GameFontFamily.Meidinger,
        GameFontFamilyAndSize.Meidinger40 => GameFontFamily.Meidinger,
        GameFontFamilyAndSize.MiedingerMid10 => GameFontFamily.MiedingerMid,
        GameFontFamilyAndSize.MiedingerMid12 => GameFontFamily.MiedingerMid,
        GameFontFamilyAndSize.MiedingerMid14 => GameFontFamily.MiedingerMid,
        GameFontFamilyAndSize.MiedingerMid18 => GameFontFamily.MiedingerMid,
        GameFontFamilyAndSize.MiedingerMid36 => GameFontFamily.MiedingerMid,
        GameFontFamilyAndSize.TrumpGothic184 => GameFontFamily.TrumpGothic,
        GameFontFamilyAndSize.TrumpGothic23 => GameFontFamily.TrumpGothic,
        GameFontFamilyAndSize.TrumpGothic34 => GameFontFamily.TrumpGothic,
        GameFontFamilyAndSize.TrumpGothic68 => GameFontFamily.TrumpGothic,
        _ => throw new InvalidOperationException(),
    };

    /// <summary>
    /// Gets the corresponding GameFontFamilyAndSize but with minimum possible font sizes.
    /// </summary>
    public readonly GameFontFamilyAndSize FamilyWithMinimumSize => this.Family switch
    {
        GameFontFamily.Axis => GameFontFamilyAndSize.Axis96,
        GameFontFamily.Jupiter => GameFontFamilyAndSize.Jupiter16,
        GameFontFamily.JupiterNumeric => GameFontFamilyAndSize.Jupiter45,
        GameFontFamily.Meidinger => GameFontFamilyAndSize.Meidinger16,
        GameFontFamily.MiedingerMid => GameFontFamilyAndSize.MiedingerMid10,
        GameFontFamily.TrumpGothic => GameFontFamilyAndSize.TrumpGothic184,
        _ => GameFontFamilyAndSize.Undefined,
    };

    /// <summary>
    /// Gets the base font size in point unit.
    /// </summary>
    public readonly float BaseSizePt => this.FamilyAndSize switch
    {
        GameFontFamilyAndSize.Undefined => 0,
        GameFontFamilyAndSize.Axis96 => 9.6f,
        GameFontFamilyAndSize.Axis12 => 12,
        GameFontFamilyAndSize.Axis14 => 14,
        GameFontFamilyAndSize.Axis18 => 18,
        GameFontFamilyAndSize.Axis36 => 36,
        GameFontFamilyAndSize.Jupiter16 => 16,
        GameFontFamilyAndSize.Jupiter20 => 20,
        GameFontFamilyAndSize.Jupiter23 => 23,
        GameFontFamilyAndSize.Jupiter45 => 45,
        GameFontFamilyAndSize.Jupiter46 => 46,
        GameFontFamilyAndSize.Jupiter90 => 90,
        GameFontFamilyAndSize.Meidinger16 => 16,
        GameFontFamilyAndSize.Meidinger20 => 20,
        GameFontFamilyAndSize.Meidinger40 => 40,
        GameFontFamilyAndSize.MiedingerMid10 => 10,
        GameFontFamilyAndSize.MiedingerMid12 => 12,
        GameFontFamilyAndSize.MiedingerMid14 => 14,
        GameFontFamilyAndSize.MiedingerMid18 => 18,
        GameFontFamilyAndSize.MiedingerMid36 => 36,
        GameFontFamilyAndSize.TrumpGothic184 => 18.4f,
        GameFontFamilyAndSize.TrumpGothic23 => 23,
        GameFontFamilyAndSize.TrumpGothic34 => 34,
        GameFontFamilyAndSize.TrumpGothic68 => 68,
        _ => throw new InvalidOperationException(),
    };

    /// <summary>
    /// Gets the base font size in pixel unit.
    /// </summary>
    public readonly float BaseSizePx => this.BaseSizePt * 4 / 3;

    /// <summary>
    /// Gets or sets a value indicating whether this font is bold.
    /// </summary>
    public bool Bold
    {
        readonly get => this.Weight > 0f;
        set => this.Weight = value ? 1f : 0f;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this font is italic.
    /// </summary>
    public bool Italic
    {
        readonly get => this.SkewStrength != 0;
        set => this.SkewStrength = value ? this.SizePx / 6 : 0;
    }

    /// <summary>
    /// Gets the recommend GameFontFamilyAndSize given family and size.
    /// </summary>
    /// <param name="family">Font family.</param>
    /// <param name="size">Font size in points.</param>
    /// <returns>Recommended GameFontFamilyAndSize.</returns>
    public static GameFontFamilyAndSize GetRecommendedFamilyAndSize(GameFontFamily family, float size) =>
        family switch
        {
            _ when size <= 0 => GameFontFamilyAndSize.Undefined,
            GameFontFamily.Undefined => GameFontFamilyAndSize.Undefined,
            GameFontFamily.Axis => size switch
            {
                <= ((int)((9.6f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Axis96,
                <= ((int)((12f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Axis12,
                <= ((int)((14f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Axis14,
                <= ((int)((18f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Axis18,
                _ => GameFontFamilyAndSize.Axis36,
            },
            GameFontFamily.Jupiter => size switch
            {
                <= ((int)((16f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Jupiter16,
                <= ((int)((20f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Jupiter20,
                <= ((int)((23f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Jupiter23,
                _ => GameFontFamilyAndSize.Jupiter46,
            },
            GameFontFamily.JupiterNumeric => size switch
            {
                <= ((int)((45f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Jupiter45,
                _ => GameFontFamilyAndSize.Jupiter90,
            },
            GameFontFamily.Meidinger => size switch
            {
                <= ((int)((16f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Meidinger16,
                <= ((int)((20f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.Meidinger20,
                _ => GameFontFamilyAndSize.Meidinger40,
            },
            GameFontFamily.MiedingerMid => size switch
            {
                <= ((int)((10f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.MiedingerMid10,
                <= ((int)((12f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.MiedingerMid12,
                <= ((int)((14f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.MiedingerMid14,
                <= ((int)((18f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.MiedingerMid18,
                _ => GameFontFamilyAndSize.MiedingerMid36,
            },
            GameFontFamily.TrumpGothic => size switch
            {
                <= ((int)((18.4f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.TrumpGothic184,
                <= ((int)((23f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.TrumpGothic23,
                <= ((int)((34f * 4f / 3f) + 0.5f) * 3f / 4f) + 0.001f => GameFontFamilyAndSize.TrumpGothic34,
                _ => GameFontFamilyAndSize.TrumpGothic68,
            },
            _ => GameFontFamilyAndSize.Undefined,
        };

    /// <summary>
    /// Creates a new scaled instance of <see cref="GameFontStyle"/> struct.
    /// </summary>
    /// <param name="scale">The scale.</param>
    /// <returns>The scaled instance.</returns>
    public readonly GameFontStyle Scale(float scale) => new()
    {
        FamilyAndSize = GetRecommendedFamilyAndSize(this.Family, this.SizePt * scale),
        SizePx = this.SizePx * scale,
        Weight = this.Weight,
        SkewStrength = this.SkewStrength * scale,
    };

    /// <summary>
    /// Calculates the adjustment to width resulting fron Weight and SkewStrength.
    /// </summary>
    /// <param name="header">Font header.</param>
    /// <param name="glyph">Glyph.</param>
    /// <returns>Width adjustment in pixel unit.</returns>
    public readonly int CalculateBaseWidthAdjustment(in FdtReader.FontTableHeader header, in FdtReader.FontTableEntry glyph)
    {
        var widthDelta = this.Weight;
        switch (this.BaseSkewStrength)
        {
            case > 0:
                widthDelta += (1f * this.BaseSkewStrength * (header.LineHeight - glyph.CurrentOffsetY))
                              / header.LineHeight;
                break;
            case < 0:
                widthDelta -= (1f * this.BaseSkewStrength * (glyph.CurrentOffsetY + glyph.BoundingHeight))
                              / header.LineHeight;
                break;
        }

        return (int)MathF.Ceiling(widthDelta);
    }

    /// <summary>
    /// Calculates the adjustment to width resulting fron Weight and SkewStrength.
    /// </summary>
    /// <param name="reader">Font information.</param>
    /// <param name="glyph">Glyph.</param>
    /// <returns>Width adjustment in pixel unit.</returns>
    public readonly int CalculateBaseWidthAdjustment(FdtReader reader, FdtReader.FontTableEntry glyph) =>
        this.CalculateBaseWidthAdjustment(reader.FontHeader, glyph);

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return $"GameFontStyle({this.FamilyAndSize}, {this.SizePt}pt, skew={this.SkewStrength}, weight={this.Weight})";
    }
}
