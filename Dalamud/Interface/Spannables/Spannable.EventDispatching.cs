using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables;

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
// Note: see Mouse.cs and Keyboard.cs for respective event dispatching and handling.
public abstract partial class Spannable
{
    /// <summary>Occurs when the spannable receives focus.</summary>
    public event SpannableEventHandler? GotFocus;

    /// <summary>Occurs when the spannable loses focus.</summary>
    public event SpannableEventHandler? LostFocus;

    /// <summary>Occurs when the spannable will be receiving events shortly for the frame.</summary>
    public event SpannableEventHandler? PreDispatchEvents;

    /// <summary>Occurs when the spannable is done receiving events for the frame.</summary>
    public event SpannableEventHandler? PostDispatchEvents;

    /// <summary>Gets a value indicating whether to suppress all input events to self or children.</summary>
    private bool InputEventDispatchShouldSuppressAll =>
        this.visible && !this.enabled && this.eventEnabled;
    
    /// <summary>Gets a value indicating whether to dispatch input events to self.</summary>
    private bool InputEventDispatchShouldDispatchToSelf =>
        this.visible && this.enabled && this.eventEnabled;
    
    /// <summary>Gets a value indicating whether to dispatch keyboard input events to self.</summary>
    private bool InputEventDispatchShouldDispatchKeyboardToSelf =>
        this.InputEventDispatchShouldDispatchToSelf && this.ImGuiIsFocused;

    /// <summary>Gets a value indicating whether to dispatch input events to self.</summary>
    private bool InputEventDispatchShouldDispatchToChildren =>
        this.visible && (this.enabled || this.eventEnabled);

    /// <summary>Called before dispatching events.</summary>
    public void RenderPassPreDispatchEvents()
    {
        if (!this.visible || !this.enabled)
            return;

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPreDispatchEvents(e);
        SpannableEventArgsPool.Return(e);

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
            children[i]?.RenderPassPreDispatchEvents();
    }

    /// <summary>Called before dispatching events.</summary>
    public void RenderPassPostDispatchEvents()
    {
        if (!this.visible || !this.enabled || !this.eventEnabled)
        {
            this.DispatchEffectivelyDisabled();
            return;
        }

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        if (this.wasFocused != this.ImGuiIsFocused)
        {
            this.wasFocused = this.ImGuiIsFocused;
            e.Initialize(this, SpannableEventStep.DirectTarget);
            if (this.wasFocused)
                this.OnGotFocus(e);
            else
                this.OnLostFocus(e);
        }

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
            children[i]?.RenderPassPostDispatchEvents();

        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPostDispatchEvents(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Raises the <see cref="PreDispatchEvents"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPreDispatchEvents(SpannableEventArgs args) => this.PreDispatchEvents?.Invoke(args);

    /// <summary>Raises the <see cref="PostDispatchEvents"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPostDispatchEvents(SpannableEventArgs args) => this.PostDispatchEvents?.Invoke(args);

    /// <summary>Raises the <see cref="GotFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnGotFocus(SpannableEventArgs args) => this.GotFocus?.Invoke(args);

    /// <summary>Raises the <see cref="LostFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnLostFocus(SpannableEventArgs args) => this.LostFocus?.Invoke(args);
}
