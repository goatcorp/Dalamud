using System.Text;

using Dalamud.Interface.Spannables.EventHandlers;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable
{
    private bool takeKeyboardInputsOnFocus = true;
    private bool takeKeyboardInputsAlways = true;

    /// <summary>Occurs when a key is pressed while the control has focus.</summary>
    public event SpannableKeyEventHandler? KeyDown;

    /// <summary>Occurs when a character, space or backspace key is pressed while the control has focus.</summary>
    public event SpannableKeyPressEventHandler? KeyPress;

    /// <summary>Occurs when a key is released while the control has focus.</summary>
    public event SpannableKeyEventHandler? KeyUp;

    /// <summary>Occurs when the property <see cref="TakeKeyboardInputsOnFocus"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? TakeKeyboardInputsOnFocusChange;

    /// <summary>Occurs when the property <see cref="TakeKeyboardInputsAlways"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? TakeKeyboardInputsAlwaysChange;

    /// <summary>Gets or sets a value indicating whether to take and claim keyboard inputs when focused.</summary>
    /// <remarks>
    /// <para>If set to <c>true</c>, then the game will not receive keyboard inputs when this control is focused.</para>
    /// <para>Does nothing if <see cref="Spannable.Focusable"/> is <c>false</c>.</para>
    /// </remarks>
    public bool TakeKeyboardInputsOnFocus
    {
        get => this.takeKeyboardInputsOnFocus;
        set => this.HandlePropertyChange(
            nameof(this.TakeKeyboardInputsOnFocus),
            ref this.takeKeyboardInputsOnFocus,
            value,
            this.takeKeyboardInputsOnFocus == value,
            this.OnTakeKeyboardInputsOnFocusChange);
    }

    /// <summary>Gets or sets a value indicating whether to take and claim keyboard inputs when focused.</summary>
    /// <remarks>
    /// <para>If set to <c>true</c>, then the game will not receive keyboard inputs when this control is focused.</para>
    /// <para>Does nothing if <see cref="Spannable.Focusable"/> is <c>false</c>.</para>
    /// </remarks>
    public bool TakeKeyboardInputsAlways
    {
        get => this.takeKeyboardInputsAlways;
        set => this.HandlePropertyChange(
            nameof(this.TakeKeyboardInputsAlways),
            ref this.takeKeyboardInputsAlways,
            value,
            this.takeKeyboardInputsAlways == value,
            this.OnTakeKeyboardInputsAlwaysChange);
    }

    /// <summary>Takes over a key that usually results in a navigation from ImGui.</summary>
    /// <param name="key">One of the <see cref="ImGuiKey"/> values that represents the key to process.</param>
    /// <returns><c>true</c> if the character will be overtaken by the spannable; otherwise, <c>false</c>.</returns>
    protected virtual bool TakeOverNavKey(ImGuiKey key) => false;

    /// <summary>Raises the <see cref="KeyDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyDown(SpannableKeyEventArgs args) => this.KeyDown?.Invoke(args);

    /// <summary>Raises the <see cref="KeyPress"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyPressEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyPress(SpannableKeyPressEventArgs args) => this.KeyPress?.Invoke(args);

    /// <summary>Raises the <see cref="KeyUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyUp(SpannableKeyEventArgs args) => this.KeyUp?.Invoke(args);

    /// <summary>Raises the <see cref="TakeKeyboardInputsOnFocusChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTakeKeyboardInputsOnFocusChange(PropertyChangeEventArgs<bool> args) =>
        this.TakeKeyboardInputsOnFocusChange?.Invoke(args);

    /// <summary>Raises the <see cref="TakeKeyboardInputsAlwaysChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTakeKeyboardInputsAlwaysChange(PropertyChangeEventArgs<bool> args) =>
        this.TakeKeyboardInputsAlwaysChange?.Invoke(args);

    private bool DispatchKeyDown(ImGuiModFlags modifiers, ImGuiKey key, bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchEventToSelf = this.InputEventDispatchShouldDispatchKeyboardToSelf;
        SpannableKeyEventArgs? e = null;

        if (dispatchEventToSelf)
        {
            e = SpannableEventArgsPool.Rent<SpannableKeyEventArgs>();
            e.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            e.InitializeKeyEvent(modifiers, key);
            this.OnKeyDown(e);
            alreadyHandled |= e.SuppressHandling;
        }

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
            alreadyHandled |= children[i]?.DispatchKeyDown(modifiers, key, alreadyHandled) is true;

        if (dispatchEventToSelf)
        {
            e.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            e.InitializeKeyEvent(modifiers, key);
            this.OnKeyDown(e);
            alreadyHandled |= e.SuppressHandling;
        }

        SpannableEventArgsPool.Return(e);
        return alreadyHandled;
    }

    private bool DispatchKeyUp(ImGuiModFlags modifiers, ImGuiKey key, bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchEventToSelf = this.InputEventDispatchShouldDispatchKeyboardToSelf;
        SpannableKeyEventArgs? e = null;

        if (dispatchEventToSelf)
        {
            e = SpannableEventArgsPool.Rent<SpannableKeyEventArgs>();
            e.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            e.InitializeKeyEvent(modifiers, key);
            this.OnKeyUp(e);
            alreadyHandled |= e.SuppressHandling;
        }

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
            alreadyHandled |= children[i]?.DispatchKeyUp(modifiers, key, alreadyHandled) is true;

        if (dispatchEventToSelf)
        {
            e.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            e.InitializeKeyEvent(modifiers, key);
            this.OnKeyUp(e);
            alreadyHandled |= e.SuppressHandling;
        }

        SpannableEventArgsPool.Return(e);
        return alreadyHandled;
    }

    private bool DispatchKeyPress(Rune rune, bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchEventToSelf = this.InputEventDispatchShouldDispatchKeyboardToSelf;
        SpannableKeyPressEventArgs? e = null;

        if (dispatchEventToSelf)
        {
            e = SpannableEventArgsPool.Rent<SpannableKeyPressEventArgs>();
            e.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            e.InitializeKeyEvent(rune);
            this.OnKeyPress(e);
            alreadyHandled |= e.SuppressHandling;
        }

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
            alreadyHandled |= children[i]?.DispatchKeyPress(rune, alreadyHandled) is true;

        if (dispatchEventToSelf)
        {
            e.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            e.InitializeKeyEvent(rune);
            this.OnKeyPress(e);
            alreadyHandled |= e.SuppressHandling;
        }

        SpannableEventArgsPool.Return(e);
        return alreadyHandled;
    }
}
