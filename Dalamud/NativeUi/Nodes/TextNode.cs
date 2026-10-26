using System.Numerics;

using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Extensions;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Data.Parsing.Uld;
using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games TextNode.
/// </summary>
internal unsafe class TextNode : NodeBase<AtkTextNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextNode"/> class.
    /// Constructs a new <see cref="TextNode"/>.
    /// </summary>
    /// <remarks>
    /// This will default various properties to standard values
    /// and colors for the theme that was active when this was constructed.
    /// </remarks>
    public TextNode()
        : base(NodeType.Text)
    {
        this.TextColor = NativeThemeColorHelper.GetColor(8);
        this.TextOutlineColor = NativeThemeColorHelper.GetColor(7);
        this.FontSize = 12;
        this.FontType = FontType.Axis;
        this.LineSpacing = 12;
        this.AlignmentType = AlignmentType.Left;

        if (AtkStage.Instance()->AtkUIColorHolder->ActiveColorThemeType is 0)
        {
            this.AddTextFlags(TextFlags.Emboss);
        }
    }

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    /// <remarks>
    /// Expects values between 0.0f and 1.0f.
    /// </remarks>
    public Vector4 TextColor
    {
        get => this.Node->TextColor.ToVector4();
        set => this.Node->TextColor = value.ToByteColor();
    }

    /// <summary>
    /// Gets or sets the outline color.
    /// </summary>
    /// <remarks>
    /// Expects values between 0.0f and 1.0f.
    /// </remarks>
    public Vector4 TextOutlineColor
    {
        get => this.Node->EdgeColor.ToVector4();
        set => this.Node->EdgeColor = value.ToByteColor();
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Vector4 BackgroundColor
    {
        get => this.Node->BackgroundColor.ToVector4();
        set => this.Node->BackgroundColor = value.ToByteColor();
    }

    /// <summary>
    /// Gets or sets the selection start.
    /// </summary>
    /// <remarks>
    /// This is used in conjunction with <see cref="BackgroundColor"/> and <see cref="SelectEnd"/>.
    /// </remarks>
    public uint SelectStart
    {
        get => this.Node->SelectStart;
        set => this.Node->SelectStart = value;
    }

    /// <summary>
    /// Gets or sets the selection end.
    /// </summary>
    /// <remarks>
    /// This is used in conjunction with <see cref="BackgroundColor"/> and <see cref="SelectStart"/>.
    /// </remarks>
    public uint SelectEnd
    {
        get => this.Node->SelectEnd;
        set => this.Node->SelectEnd = value;
    }

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public AlignmentType AlignmentType
    {
        get => this.Node->AlignmentType;
        set
        {
            this.Node->SetAlignment(value);
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the used font.
    /// </summary>
    public FontType FontType
    {
        get => this.Node->FontType;
        set
        {
            this.Node->SetFont(value);
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the text flags.
    /// </summary>
    public TextFlags TextFlags
    {
        get => this.Node->TextFlags;
        set
        {
            this.Node->TextFlags = value;
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public uint FontSize
    {
        get => this.Node->FontSize;
        set
        {
            this.Node->FontSize = (byte)value;
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the vertical line spacing.
    /// </summary>
    public uint LineSpacing
    {
        get => this.Node->LineSpacing;
        set
        {
            this.Node->LineSpacing = (byte)value;
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the character spacing.
    /// </summary>
    public uint CharSpacing
    {
        get => this.Node->CharSpacing;
        set
        {
            this.Node->CharSpacing = (byte)value;
            this.UpdateText();
        }
    }

    /// <summary>
    /// Gets or sets the sheet type used when setting text via <see cref="TextId"/>.
    /// </summary>
    public NodeData.SheetType SheetType
    {
        get => (NodeData.SheetType)this.Node->SheetType;
        set => this.Node->SheetType = (byte)value;
    }

    /// <summary>
    /// Gets or sets the textId, this is a row in a <see cref="NodeData.SheetType"/>.
    /// </summary>
    public uint TextId
    {
        get => this.Node->TextId;
        set => this.Node->TextId = value;
    }

    /// <summary>
    /// Gets or sets the displayed string.
    /// </summary>
    public ReadOnlySeString String
    {
        get => new(this.Node->GetText().AsSpan());
        set
        {
            using var builder = new RentedSeStringBuilder();
            this.Node->SetText(builder.Builder.Append(value).GetViewAsSpan());
        }
    }

    /// <summary>
    /// Gets or sets the nodes size, triggering a text update.
    /// </summary>
    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            this.UpdateText();
        }
    }

    /// <summary>
    /// Adds the specified text flags.
    /// </summary>
    /// <param name="flags">Flags to add.</param>
    public void AddTextFlags(params TextFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.TextFlags |= flag;
        }
    }

    /// <summary>
    /// Removes the specified text flags.
    /// </summary>
    /// <param name="flags">Flags to remove.</param>
    public void RemoveTextFlags(params TextFlags[] flags)
    {
        foreach (var flag in flags)
        {
            this.TextFlags &= ~flag;
        }
    }

    /// <summary>
    /// Sets the specified number using the provided formatting params.
    /// </summary>
    /// <param name="number">Number to set.</param>
    /// <param name="showCommas">Should show commas.</param>
    /// <param name="showPlusSign">Should show plus sign.</param>
    /// <param name="digits">How many digits to show.</param>
    /// <param name="zeroPad">If the number should be zero padded.</param>
    public void SetNumber(int number, bool showCommas = false, bool showPlusSign = false, int digits = 0, bool zeroPad = false)
        => this.Node->SetNumber(number, showCommas, showPlusSign, (byte)digits, zeroPad);

    /// <summary>
    /// Gets the size of the specified text if it were drawn with this nodes given params.
    /// </summary>
    /// <param name="text">Text string to calculate size for.</param>
    /// <param name="considerScale">If current scale should be applied before returning.</param>
    /// <returns>The actual draw size of the currently set text.</returns>
    public Vector2 GetTextDrawSize(ReadOnlySeString text, bool considerScale = true)
    {
        using var builder = new RentedSeStringBuilder();

        ushort sizeX = 0;
        ushort sizeY = 0;

        fixed (byte* ptr = builder.Builder.Append(text).GetViewAsSpan())
            this.Node->GetTextDrawSize(&sizeX, &sizeY, ptr, considerScale: considerScale);

        return new Vector2(sizeX, sizeY);
    }

    /// <summary>
    /// Gets the size of this nodes text.
    /// </summary>
    /// <param name="considerScale">If current scale should be applied before returning.</param>
    /// <returns>The actual draw size of the currently set text.</returns>
    public Vector2 GetTextDrawSize(bool considerScale = true)
    {
        ushort sizeX = 0;
        ushort sizeY = 0;

        this.Node->GetTextDrawSize(&sizeX, &sizeY, considerScale: considerScale);

        return new Vector2(sizeX, sizeY);
    }

    private void UpdateText()
    {
        using var builder = new RentedSeStringBuilder();
        this.Node->SetText(builder.Builder.Append(this.String).GetViewAsSpan());
    }
}
