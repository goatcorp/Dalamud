using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Plan on how glyphs will be rendered.
/// </summary>
public class GameFontLayoutPlan
{
    /// <summary>
    /// Horizontal alignment.
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>
        /// Align to left.
        /// </summary>
        Left,

        /// <summary>
        /// Align to center.
        /// </summary>
        Center,

        /// <summary>
        /// Align to right.
        /// </summary>
        Right,
    }

    /// <summary>
    /// Gets the associated ImFontPtr.
    /// </summary>
    public ImFontPtr ImFontPtr { get; internal set; }

    /// <summary>
    /// Gets the size in points of the text.
    /// </summary>
    public float Size { get; internal set; }

    /// <summary>
    /// Gets the x offset of the leftmost glyph.
    /// </summary>
    public float X { get; internal set; }

    /// <summary>
    /// Gets the width of the text.
    /// </summary>
    public float Width { get; internal set; }

    /// <summary>
    /// Gets the height of the text.
    /// </summary>
    public float Height { get; internal set; }

    /// <summary>
    /// Gets the list of plannen elements.
    /// </summary>
    public IList<Element> Elements { get; internal set; }

    /// <summary>
    /// Draws font to ImGui.
    /// </summary>
    /// <param name="drawListPtr">Target ImDrawList.</param>
    /// <param name="pos">Position.</param>
    /// <param name="col">Color.</param>
    public void Draw(ImDrawListPtr drawListPtr, Vector2 pos, uint col)
    {
        foreach (var element in this.Elements)
        {
            if (element.IsControl)
                continue;

            this.ImFontPtr.RenderChar(
                drawListPtr,
                this.Size,
                new Vector2(
                    this.X + pos.X + element.X,
                    pos.Y + element.Y),
                col,
                element.Glyph.Char);
        }
    }

    /// <summary>
    /// Plan on how each glyph will be rendered.
    /// </summary>
    public class Element
    {
        /// <summary>
        /// Gets the original codepoint.
        /// </summary>
        public int Codepoint { get; init; }

        /// <summary>
        /// Gets the corresponding or fallback glyph.
        /// </summary>
        public FdtReader.FontTableEntry Glyph { get; init; }

        /// <summary>
        /// Gets the X offset of this glyph.
        /// </summary>
        public float X { get; internal set; }

        /// <summary>
        /// Gets the Y offset of this glyph.
        /// </summary>
        public float Y { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether whether this codepoint is a control character.
        /// </summary>
        public bool IsControl
        {
            get
            {
                return this.Codepoint < 0x10000 && char.IsControl((char)this.Codepoint);
            }
        }

        /// <summary>
        /// Gets a value indicating whether whether this codepoint is a space.
        /// </summary>
        public bool IsSpace
        {
            get
            {
                return this.Codepoint < 0x10000 && char.IsWhiteSpace((char)this.Codepoint);
            }
        }

        /// <summary>
        /// Gets a value indicating whether whether this codepoint is a line break character.
        /// </summary>
        public bool IsLineBreak
        {
            get
            {
                return this.Codepoint == '\n' || this.Codepoint == '\r';
            }
        }

        /// <summary>
        /// Gets a value indicating whether whether this codepoint is a chinese character.
        /// </summary>
        public bool IsChineseCharacter
        {
            get
            {
                // CJK Symbols and Punctuation(〇)
                if (this.Codepoint >= 0x3007 && this.Codepoint <= 0x3007)
                    return true;

                // CJK Unified Ideographs Extension A
                if (this.Codepoint >= 0x3400 && this.Codepoint <= 0x4DBF)
                    return true;

                // CJK Unified Ideographs
                if (this.Codepoint >= 0x4E00 && this.Codepoint <= 0x9FFF)
                    return true;

                // CJK Unified Ideographs Extension B
                if (this.Codepoint >= 0x20000 && this.Codepoint <= 0x2A6DF)
                    return true;

                // CJK Unified Ideographs Extension C
                if (this.Codepoint >= 0x2A700 && this.Codepoint <= 0x2B73F)
                    return true;

                // CJK Unified Ideographs Extension D
                if (this.Codepoint >= 0x2B740 && this.Codepoint <= 0x2B81F)
                    return true;

                // CJK Unified Ideographs Extension E
                if (this.Codepoint >= 0x2B820 && this.Codepoint <= 0x2CEAF)
                    return true;

                // CJK Unified Ideographs Extension F
                if (this.Codepoint >= 0x2CEB0 && this.Codepoint <= 0x2EBEF)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether whether this codepoint is a good position to break word after.
        /// </summary>
        public bool IsWordBreakPoint
        {
            get
            {
                if (this.IsChineseCharacter)
                    return true;

                if (this.Codepoint >= 0x10000)
                    return false;

                // TODO: Whatever
                switch (char.GetUnicodeCategory((char)this.Codepoint))
                {
                    case System.Globalization.UnicodeCategory.SpaceSeparator:
                    case System.Globalization.UnicodeCategory.LineSeparator:
                    case System.Globalization.UnicodeCategory.ParagraphSeparator:
                    case System.Globalization.UnicodeCategory.Control:
                    case System.Globalization.UnicodeCategory.Format:
                    case System.Globalization.UnicodeCategory.Surrogate:
                    case System.Globalization.UnicodeCategory.PrivateUse:
                    case System.Globalization.UnicodeCategory.ConnectorPunctuation:
                    case System.Globalization.UnicodeCategory.DashPunctuation:
                    case System.Globalization.UnicodeCategory.OpenPunctuation:
                    case System.Globalization.UnicodeCategory.ClosePunctuation:
                    case System.Globalization.UnicodeCategory.InitialQuotePunctuation:
                    case System.Globalization.UnicodeCategory.FinalQuotePunctuation:
                    case System.Globalization.UnicodeCategory.OtherPunctuation:
                    case System.Globalization.UnicodeCategory.MathSymbol:
                    case System.Globalization.UnicodeCategory.ModifierSymbol:
                    case System.Globalization.UnicodeCategory.OtherSymbol:
                    case System.Globalization.UnicodeCategory.OtherNotAssigned:
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Build a GameFontLayoutPlan.
    /// </summary>
    public class Builder
    {
        private readonly ImFontPtr fontPtr;
        private readonly FdtReader fdt;
        private readonly string text;
        private int maxWidth = int.MaxValue;
        private float size;
        private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;

        /// <summary>
        /// Initializes a new instance of the <see cref="Builder"/> class.
        /// </summary>
        /// <param name="fontPtr">Corresponding ImFontPtr.</param>
        /// <param name="fdt">FDT file to base on.</param>
        /// <param name="text">Text.</param>
        public Builder(ImFontPtr fontPtr, FdtReader fdt, string text)
        {
            this.fontPtr = fontPtr;
            this.fdt = fdt;
            this.text = text;
            this.size = fdt.FontHeader.LineHeight;
        }

        /// <summary>
        /// Sets the size of resulting text.
        /// </summary>
        /// <param name="size">Size in pixels.</param>
        /// <returns>This.</returns>
        public Builder WithSize(float size)
        {
            this.size = size;
            return this;
        }

        /// <summary>
        /// Sets the maximum width of the text.
        /// </summary>
        /// <param name="maxWidth">Maximum width in pixels.</param>
        /// <returns>This.</returns>
        public Builder WithMaxWidth(int maxWidth)
        {
            this.maxWidth = maxWidth;
            return this;
        }

        /// <summary>
        /// Sets the horizontal alignment of the text.
        /// </summary>
        /// <param name="horizontalAlignment">Horizontal alignment.</param>
        /// <returns>This.</returns>
        public Builder WithHorizontalAlignment(HorizontalAlignment horizontalAlignment)
        {
            this.horizontalAlignment = horizontalAlignment;
            return this;
        }

        /// <summary>
        /// Builds the layout plan.
        /// </summary>
        /// <returns>Newly created layout plan.</returns>
        public GameFontLayoutPlan Build()
        {
            var scale = this.size / this.fdt.FontHeader.LineHeight;
            var unscaledMaxWidth = (float)Math.Ceiling(this.maxWidth / scale);
            var elements = new List<Element>();
            foreach (var c in this.text)
                elements.Add(new() { Codepoint = c, Glyph = this.fdt.GetGlyph(c), });

            var lastBreakIndex = 0;
            List<int> lineBreakIndices = new() { 0 };
            for (var i = 1; i < elements.Count; i++)
            {
                var prev = elements[i - 1];
                var curr = elements[i];

                if (prev.IsLineBreak)
                {
                    curr.X = 0;
                    curr.Y = prev.Y + this.fdt.FontHeader.LineHeight;
                    lineBreakIndices.Add(i);
                }
                else
                {
                    curr.X = prev.X + prev.Glyph.NextOffsetX + prev.Glyph.BoundingWidth + this.fdt.GetDistance(prev.Codepoint, curr.Codepoint);
                    curr.Y = prev.Y;
                }

                if (prev.IsWordBreakPoint)
                    lastBreakIndex = i;

                if (curr.IsSpace)
                    continue;

                if (curr.X + curr.Glyph.BoundingWidth < unscaledMaxWidth)
                    continue;

                if (!prev.IsSpace && elements[lastBreakIndex].X > 0)
                {
                    prev = elements[lastBreakIndex - 1];
                    curr = elements[lastBreakIndex];
                    i = lastBreakIndex;
                }
                else
                {
                    lastBreakIndex = i;
                }

                curr.X = 0;
                curr.Y = prev.Y + this.fdt.FontHeader.LineHeight;
                lineBreakIndices.Add(i);
            }

            lineBreakIndices.Add(elements.Count);

            var targetX = 0f;
            var targetWidth = 0f;
            var targetHeight = 0f;
            for (var i = 1; i < lineBreakIndices.Count; i++)
            {
                var from = lineBreakIndices[i - 1];
                var to = lineBreakIndices[i];
                while (to > from && elements[to - 1].IsSpace)
                {
                    to--;
                }

                if (from >= to)
                    continue;

                var right = 0f;
                for (var j = from; j < to; j++)
                {
                    var e = elements[j];
                    right = Math.Max(right, e.X + Math.Max(e.Glyph.BoundingWidth, e.Glyph.AdvanceWidth));
                    targetHeight = Math.Max(targetHeight, e.Y + e.Glyph.BoundingHeight);
                }

                targetWidth = Math.Max(targetWidth, right - elements[from].X);
                float offsetX;
                if (this.horizontalAlignment == HorizontalAlignment.Center)
                    offsetX = (unscaledMaxWidth - right) / 2;
                else if (this.horizontalAlignment == HorizontalAlignment.Right)
                    offsetX = unscaledMaxWidth - right;
                else if (this.horizontalAlignment == HorizontalAlignment.Left)
                    offsetX = 0;
                else
                    throw new ArgumentException("Invalid horizontal alignment");
                for (var j = from; j < to; j++)
                    elements[j].X += offsetX;
                targetX = i == 1 ? elements[from].X : Math.Min(targetX, elements[from].X);
            }

            targetHeight = Math.Max(targetHeight, this.fdt.FontHeader.LineHeight * (lineBreakIndices.Count - 1));

            targetWidth *= scale;
            targetHeight *= scale;
            targetX *= scale;
            foreach (var e in elements)
            {
                e.X *= scale;
                e.Y *= scale;
            }

            return new GameFontLayoutPlan()
            {
                ImFontPtr = this.fontPtr,
                Size = this.size,
                X = targetX,
                Width = targetWidth,
                Height = targetHeight,
                Elements = elements,
            };
        }
    }
}
