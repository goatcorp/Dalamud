using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A custom text renderer.</summary>
public interface ISpannedStringRenderer : ISpannedStringBuilder<ISpannedStringRenderer>, IDisposable
{
    /// <summary>Renders the queued text. No further calls should be made.</summary>
    void Render();

    /// <summary>Renders the queued text. No further calls should be made.</summary>
    /// <param name="state">The final render state.</param>
    void Render(out RenderState state);

    /// <summary>Renders the queued text. No further calls should be made.</summary>
    /// <param name="state">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    /// <remarks>
    /// <para><paramref name="hoveredLink"/> is only valid until disposing this instance.</para>
    /// <para>If disposed without calling this, then the whole draw operation will be cancelled.</para>
    /// </remarks>
    bool Render(out RenderState state, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Renders the given prebuilt spannable, ignoring all the other calls made to this renderer.</summary>
    /// <param name="spannedString">The spannable.</param>
    /// <param name="state">The final render state.</param>
    /// <param name="hoveredLink">The payload being hovered, if any.</param>
    /// <returns><c>true</c> if any payload is currently being hovered.</returns>
    bool Render(SpannedString spannedString, out RenderState state, out ReadOnlySpan<byte> hoveredLink);

    /// <summary>Options.</summary>
    /// <remarks>Unspecified options (<c>null</c>) will use the default values.</remarks>
    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1201:Elements should appear in the correct order",
        Justification = "ffs")]
    [SuppressMessage(
        "ReSharper",
        "PropertyCanBeMadeInitOnly.Global",
        Justification = "This is effectively an extensible set of function arguments")]
    public record struct Options
    {
        /// <summary>Gets or sets the accepted line break types.</summary>
        /// <remarks>Default value is <see cref="NewLineType.All"/>.</remarks>
        public NewLineType? AcceptedNewLines { get; set; }

        /// <summary>Gets or sets the design parameters for displaying the invisible control characters.</summary>
        /// <remarks><c>null</c> specifies that it should not be displayed.</remarks>
        public SpanStyle? ControlCharactersSpanParams { get; set; }

        /// <summary>Gets or sets the graphic font icon mode.</summary>
        /// <remarks>
        /// <para><c>null</c> or <c>-1</c> will use the one configured from the game configuration.</para>
        /// <para>Numbers outside the supported range will roll over.</para>
        /// </remarks>
        public int? GraphicFontIconMode { get; set; }

        /// <summary>Gets or sets the initial style.</summary>
        /// <remarks>Specifying <c>null</c> will use a default value that will only have
        /// <see cref="SpanStyle.ForeColor"/> set to <c>ImGui.GetColorU32(ImGuiCol.Text)</c> multiplied by
        /// <c>ImGui.GetStyle().Alpha</c>.</remarks>
        public SpanStyle? InitialStyle { get; set; }

        /// <summary>Gets or sets horizontal offset at which point line break or ellipsis should happen.</summary>
        /// <remarks>Default value is <c>ImGui.GetColumnWidth()</c>.</remarks>
        public float? LineWrapWidth { get; set; }

        /// <summary>Gets or sets the tab size.</summary>
        /// <remarks>
        /// <para>
        /// <c>0</c> will treat tab characters as a whitespace character.<br />
        /// <b>Positive values</b> indicate the width in pixels.<br />
        /// <b>Negative values</b> indicate the width in the number of whitespace characters, multiplied by -1.
        /// </para>
        /// <para>
        /// <c>null</c> will use the width of a whitespace character multiplied by 4.
        /// This equals to specifying <c>-4</c>.
        /// </para>
        /// </remarks>
        public float? TabWidth { get; set; }

        /// <summary>Gets or sets the scale.</summary>
        /// <remarks>Defaults to <see cref="ImGuiHelpers.GlobalScale"/>.</remarks>
        public float? Scale { get; set; }

        /// <summary>Gets or sets a value indicating whether to handle links.</summary>
        /// <remarks>Default value is to enable link handling.</remarks>
        public bool? UseLinks { get; set; }

        /// <summary>Gets or sets the word break mode.</summary>
        /// <remarks>Default value is <see cref="WordBreakType.Normal"/>.</remarks>
        public WordBreakType? WordBreak { get; set; }

        /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
        /// <remarks>Default value is <c>null</c>, indicating that wrap markers are disabled.</remarks>
        public string? WrapMarker { get; set; }

        /// <summary>Gets or sets the wrap marker style.</summary>
        /// <remarks>Default value is <c>null</c>, specifying that whatever style was in use will also be used for
        /// drawing wrap markers.</remarks>
        public SpanStyle? WrapMarkerStyle { get; set; }

        /// <summary>Gets or sets the transformation matrix.</summary>
        public Matrix4x4? Transformation { get; set; }
    }

    /// <summary>Struct that defines the purpose of borrowing an instance of <see cref="ISpannedStringRenderer"/>.</summary>
    public ref struct Usage
    {
        /// <summary>Label in UTF-8.</summary>
        internal ReadOnlySpan<byte> LabelU8;

        /// <summary>Label in UTF-16.</summary>
        internal ReadOnlySpan<char> LabelU16;

        /// <summary>Numeric local ImGui ID.</summary>
        internal nint? Id;

        /// <summary>DrawList to draw to.</summary>
        internal ImDrawListPtr DrawListPtr;

        /// <summary>Whether to put <see cref="ImGui.Dummy"/>.</summary>
        internal bool PutDummy;

        public static implicit operator Usage(ReadOnlySpan<byte> labelU8) =>
            new() { LabelU8 = labelU8 };

        public static implicit operator Usage(ReadOnlySpan<char> labelU16) =>
            new() { LabelU16 = labelU16 };

        public static implicit operator Usage(ReadOnlyMemory<byte> labelU8) =>
            new() { LabelU8 = labelU8.Span };

        public static implicit operator Usage(ReadOnlyMemory<char> labelU16) =>
            new() { LabelU16 = labelU16.Span };

        public static implicit operator Usage(string label) => new() { LabelU16 = label };

        public static implicit operator Usage(int id) => new() { Id = id };

        public static implicit operator Usage(uint id) => new() { Id = unchecked((nint)id) };

        public static implicit operator Usage(nint id) => new() { Id = id };

        public static implicit operator Usage(nuint id) => new() { Id = unchecked((nint)id) };

        public static implicit operator Usage(ImDrawListPtr drawListPtr) =>
            new() { DrawListPtr = drawListPtr };

        public static unsafe implicit operator Usage(ImDrawList* drawListPtr) =>
            new() { DrawListPtr = drawListPtr };

        public static implicit operator Usage(bool b) => new() { PutDummy = b };
    }
}
