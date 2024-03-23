using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Game.Config;
using Dalamud.Interface.Spannables.Rendering.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace Dalamud.Interface.Spannables.Rendering;

/// <summary>Represents a text state.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct TextState
{
    /// <summary>Whether to show representations of control characters.</summary>
    public bool UseControlCharacter;

    /// <inheritdoc cref="Options.WordBreak"/>
    public WordBreakType WordBreak;

    /// <inheritdoc cref="Options.AcceptedNewLines"/>
    public NewLineType AcceptedNewLines;

    /// <inheritdoc cref="Options.TabWidth"/>
    public float TabWidth;

    /// <inheritdoc cref="Options.VerticalAlignment"/>
    public float VerticalAlignment;

    /// <summary>Resolved vertical shift value from <see cref="VerticalAlignment"/>.</summary>
    public float ShiftFromVerticalAlignment;

    /// <inheritdoc cref="Options.GraphicFontIconMode"/>
    public int GfdIndex;

    /// <summary>The index of the last line, including new lines from word wrapping.</summary>
    public int LineCount;

    /// <inheritdoc cref="Options.ControlCharactersStyle"/>
    public TextStyle ControlCharactersStyle;

    /// <inheritdoc cref="Options.InitialStyle"/>
    public TextStyle InitialStyle;

    /// <summary>The latest style.</summary>
    public TextStyle LastStyle;

    /// <inheritdoc cref="Options.WrapMarker"/>
    public ISpannable? WrapMarker;

    /// <summary>Initializes a new instance of the <see cref="TextState"/> struct.</summary>
    /// <param name="rendererOptions">the initial parameters.</param>
    /// <returns>A reference of this instance after the initialize operation is completed.</returns>
    /// <exception cref="InvalidOperationException">Called outside the main thread. If called from the main thread,
    /// but not during the drawing context, the behavior is undefined and may crash.</exception>
    public TextState(in Options rendererOptions)
    {
        ThreadSafety.DebugAssertMainThread();

        this.UseControlCharacter = rendererOptions.ControlCharactersStyle.HasValue;
        this.WrapMarker = rendererOptions.WrapMarker;
        this.WordBreak = rendererOptions.WordBreak ?? WordBreakType.Normal;
        this.AcceptedNewLines = rendererOptions.AcceptedNewLines ?? NewLineType.All;
        this.TabWidth = rendererOptions.TabWidth ?? -4;
        this.ControlCharactersStyle = rendererOptions.ControlCharactersStyle ?? default;
        this.InitialStyle = rendererOptions.InitialStyle ?? TextStyle.FromContext;
        this.VerticalAlignment = rendererOptions.VerticalAlignment ?? 0f;

        var gfdIndex = rendererOptions.GraphicFontIconMode ?? -1;
        if (gfdIndex < 0 || gfdIndex >= SpannableRenderer.GfdTexturePaths.Length)
        {
            gfdIndex =
                Service<GameConfig>.Get().TryGet(SystemConfigOption.PadSelectButtonIcon, out uint iconTmp)
                    ? (int)iconTmp
                    : 0;
        }

        this.GfdIndex = gfdIndex;
        this.LineCount = 0;
        this.LastStyle = this.InitialStyle;
    }

    /// <summary>Determine if properties are equal, using <see cref="object.ReferenceEquals"/> for reference types.
    /// </summary>
    /// <param name="l">The 1st text state to compare.</param>
    /// <param name="r">The 2nd text state to compare.</param>
    /// <returns><c>true</c> if they are equal.</returns>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "opportunistic")]
    public static bool PropertyReferenceEquals(in TextState l, in TextState r) =>
        l.UseControlCharacter == r.UseControlCharacter
        && l.WordBreak == r.WordBreak
        && l.AcceptedNewLines == r.AcceptedNewLines
        && l.TabWidth == r.TabWidth
        && l.VerticalAlignment == r.VerticalAlignment
        && l.ShiftFromVerticalAlignment == r.ShiftFromVerticalAlignment
        && l.GfdIndex == r.GfdIndex
        && l.LineCount == r.LineCount
        && TextStyle.PropertyReferenceEquals(l.ControlCharactersStyle, r.ControlCharactersStyle)
        && TextStyle.PropertyReferenceEquals(l.InitialStyle, r.InitialStyle)
        && TextStyle.PropertyReferenceEquals(l.LastStyle, r.LastStyle)
        && ReferenceEquals(l.WrapMarker, r.WrapMarker);

    /// <summary>Initial options for <see cref="ISpannableRenderer"/>.</summary>
    /// <remarks>Unspecified options (<c>null</c>) will use the default values.</remarks>
    public struct Options
    {
        /// <summary>Gets or sets the accepted line break types.</summary>
        /// <remarks>Default value is <see cref="NewLineType.All"/>.</remarks>
        public NewLineType? AcceptedNewLines { get; set; }

        /// <summary>Gets or sets the design parameters for displaying the invisible control characters.</summary>
        /// <remarks><c>null</c> specifies that it should not be displayed.</remarks>
        public TextStyle? ControlCharactersStyle { get; set; }

        /// <summary>Gets or sets the graphic font icon mode.</summary>
        /// <remarks>
        /// <para><c>null</c> or <c>-1</c> will use the one configured from the game configuration.</para>
        /// <para>Numbers outside the supported range will roll over.</para>
        /// </remarks>
        public int? GraphicFontIconMode { get; set; }

        /// <summary>Gets or sets the initial style.</summary>
        /// <remarks>Specifying <c>null</c> will use a default value that will only have
        /// <see cref="TextStyle.ForeColor"/> set to <c>ImGui.GetColorU32(ImGuiCol.Text)</c> multiplied by
        /// <c>ImGui.GetStyle().Alpha</c>.</remarks>
        public TextStyle? InitialStyle { get; set; }

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

        /// <summary>Gets or sets the vertical alignment, with respect to the vertical boundary.</summary>
        /// <remarks>
        /// <para><c>0</c> will align to top. <c>1</c> will align to right. <c>0.5</c> will align to center.
        /// Values outside the range of [0, 1] will be clamped.</para>
        /// <para>Does nothing if no (infinite) vertical boundary is set.</para>
        /// <para>Specifying <c>null</c> will use <c>0</c> as the default for now.</para>
        /// </remarks>
        public float? VerticalAlignment { get; set; }

        /// <summary>Gets or sets the word break mode.</summary>
        /// <remarks>Default value is <see cref="WordBreakType.Normal"/>.</remarks>
        public WordBreakType? WordBreak { get; set; }

        /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
        /// <remarks>Default value is <c>null</c>, indicating that wrap markers are disabled.</remarks>
        public ISpannable? WrapMarker { get; set; }
    }
}
