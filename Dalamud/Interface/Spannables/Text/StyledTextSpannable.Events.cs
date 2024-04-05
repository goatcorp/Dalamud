using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Spannable dealing with <see cref="StyledText"/>.</summary>
public sealed partial class StyledTextSpannable
{
    /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseEnter;

    /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseLeave;

    /// <summary>Occurs when a link in the control just got held down.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseDown;

    /// <summary>Occurs when a link in the control just got released.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseUp;

    /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
    public event SpannableMouseLinkEventHandler? LinkMouseClick;

    /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    private void OnLinkMouseEnter(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    private void OnLinkMouseLeave(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    private void OnLinkMouseDown(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    private void OnLinkMouseUp(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
    private void OnLinkMouseClick(SpannableMouseLinkEventArgs args) =>
        this.LinkMouseClick?.Invoke(args);
}
