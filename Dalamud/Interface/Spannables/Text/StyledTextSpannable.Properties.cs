using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Styles;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Spannable dealing with <see cref="StyledText"/>.</summary>
public sealed partial class StyledTextSpannable
{
    private bool displayControlCharacters;
    private WordBreakType wordBreak = WordBreakType.Normal;
    private NewLineType acceptedNewLines = NewLineType.All;
    private float tabWidth = -4;
    private float verticalAlignment;
    private int gfdIconMode = -1;
    private TextStyle controlCharactersStyle;
    private TextStyle style;
    private ISpannableTemplate? wrapMarker;

    /// <summary>Occurs when the property <see cref="DisplayControlCharacters"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? DisplayControlCharactersChange;

    /// <summary>Occurs when the property <see cref="WordBreak"/> is changing.</summary>
    public event PropertyChangeEventHandler<WordBreakType>? WordBreakChange;

    /// <summary>Occurs when the property <see cref="AcceptedNewLines"/> is changing.</summary>
    public event PropertyChangeEventHandler<NewLineType>? AcceptedNewLinesChange;

    /// <summary>Occurs when the property <see cref="TabWidth"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? TabWidthChange;

    /// <summary>Occurs when the property <see cref="VerticalAlignment"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? VerticalAlignmentChange;

    /// <summary>Occurs when the property <see cref="GfdIconMode"/> is changing.</summary>
    public event PropertyChangeEventHandler<int>? GfdIconModeChange;

    /// <summary>Occurs when the property <see cref="ControlCharactersStyle"/> is changing.</summary>
    public event PropertyChangeEventHandler<TextStyle>? ControlCharactersStyleChange;

    /// <summary>Occurs when the property <see cref="Style"/> is changing.</summary>
    public event PropertyChangeEventHandler<TextStyle>? StyleChange;

    /// <summary>Occurs when the property <see cref="WrapMarker"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate>? WrapMarkerChange;

    /// <summary>Gets or sets a value indicating whether to display representations of control characters, such as
    /// CR, LF, NBSP, and SHY.</summary>
    public bool DisplayControlCharacters
    {
        get => this.displayControlCharacters;
        set => this.HandlePropertyChange(
            nameof(this.DisplayControlCharacters),
            ref this.displayControlCharacters,
            value,
            this.displayControlCharacters == value,
            this.OnDisplayControlCharactersChange);
    }

    /// <summary>Gets or sets how to handle word break mode.</summary>
    public WordBreakType WordBreak
    {
        get => this.wordBreak;
        set => this.HandlePropertyChange(
            nameof(this.WordBreak),
            ref this.wordBreak,
            value,
            this.wordBreak == value,
            this.OnWordBreakChange);
    }

    /// <summary>Gets or sets the type of new line sequences to handle.</summary>
    public NewLineType AcceptedNewLines
    {
        get => this.acceptedNewLines;
        set => this.HandlePropertyChange(
            nameof(this.AcceptedNewLines),
            ref this.acceptedNewLines,
            value,
            this.acceptedNewLines == value,
            this.OnAcceptedNewLinesChange);
    }

    /// <summary>Gets or sets the tab size.</summary>
    /// <value><ul>
    /// <li><c>0</c> will treat tab characters as a whitespace character.</li>
    /// <li><b>Positive values</b> indicate the width in pixels.</li>
    /// <li><b>Negative values</b> indicate the width in the number of whitespace characters, multiplied by -1.</li>
    /// </ul></value>
    public float TabWidth
    {
        get => this.tabWidth;
        set => this.HandlePropertyChange(
            nameof(this.TabWidth),
            ref this.tabWidth,
            value,
            this.tabWidth - value == 0f,
            this.OnTabWidthChange);
    }

    /// <summary>Gets or sets the vertical alignment, with respect to the measured vertical boundary.</summary>
    /// <value><ul>
    /// <li><c>0.0</c> will align to top.</li>
    /// <li><c>0.5</c> will align to center.</li>
    /// <li><c>1.0</c> will align to right.</li>
    /// <li>Values outside the range of [0, 1] will be clamped.</li>
    /// </ul></value>
    /// <remarks>Does nothing if no (infinite) vertical boundary is set.</remarks>
    public float VerticalAlignment
    {
        get => this.verticalAlignment;
        set => this.HandlePropertyChange(
            nameof(this.VerticalAlignment),
            ref this.verticalAlignment,
            value,
            this.verticalAlignment - value == 0f,
            this.OnVerticalAlignmentChange);
    }

    /// <summary>Gets or sets the graphic font icon mode.</summary>
    /// <remarks>A value outside the suported range will use the one configured from the game configuration via
    /// game controller configuration.</remarks>
    public int GfdIconMode
    {
        get => this.gfdIconMode;
        set => this.HandlePropertyChange(
            nameof(this.GfdIconMode),
            ref this.gfdIconMode,
            value,
            this.gfdIconMode == value,
            this.OnGfdIconModeChange);
    }

    /// <summary>Gets or sets the text style for the control characters.</summary>
    /// <remarks>Does nothing if <see cref="DisplayControlCharacters"/> is <c>false</c>.</remarks>
    public TextStyle ControlCharactersStyle
    {
        get => this.controlCharactersStyle;
        set => this.HandlePropertyChange(
            nameof(this.ControlCharactersStyle),
            ref this.controlCharactersStyle,
            value,
            TextStyle.PropertyReferenceEquals(this.controlCharactersStyle, value),
            this.OnControlCharactersStyleChange);
    }

    /// <summary>Gets or sets the initial text style.</summary>
    /// <remarks>Text styles may be altered in middle of the text, but this property will not change.
    /// Use <see cref="StyledTextSpannable.LastStyle"/> for that information.</remarks>
    public TextStyle Style
    {
        get => this.style;
        set => this.HandlePropertyChange(
            nameof(this.Style),
            ref this.style,
            value,
            TextStyle.PropertyReferenceEquals(this.style, value),
            this.OnStyleChange);
    }

    /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
    /// <value><c>null</c> indicates that wrap markers are disabled.</value>
    public ISpannableTemplate? WrapMarker
    {
        get => this.wrapMarker;
        set => this.HandlePropertyChange(
            nameof(this.WrapMarker),
            ref this.wrapMarker,
            value,
            ReferenceEquals(this.wrapMarker, value),
            this.OnWrapMarkerChange);
    }

    private void OnDisplayControlCharactersChange(PropertyChangeEventArgs<bool> args) =>
        this.DisplayControlCharactersChange?.Invoke(args);

    private void OnWordBreakChange(PropertyChangeEventArgs<WordBreakType> args) => this.WordBreakChange?.Invoke(args);

    private void OnAcceptedNewLinesChange(PropertyChangeEventArgs<NewLineType> args) =>
        this.AcceptedNewLinesChange?.Invoke(args);

    private void OnTabWidthChange(PropertyChangeEventArgs<float> args) => this.TabWidthChange?.Invoke(args);

    private void OnVerticalAlignmentChange(PropertyChangeEventArgs<float> args) =>
        this.VerticalAlignmentChange?.Invoke(args);

    private void OnGfdIconModeChange(PropertyChangeEventArgs<int> args) => this.GfdIconModeChange?.Invoke(args);

    private void OnControlCharactersStyleChange(PropertyChangeEventArgs<TextStyle> args) =>
        this.ControlCharactersStyleChange?.Invoke(args);

    private void OnStyleChange(PropertyChangeEventArgs<TextStyle> args) => this.StyleChange?.Invoke(args);

    private void OnWrapMarkerChange(PropertyChangeEventArgs<ISpannableTemplate> args) =>
        this.WrapMarkerChange?.Invoke(args);
}
