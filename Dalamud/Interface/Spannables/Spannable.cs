using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

#pragma warning disable SA1010

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable : IDisposable
{
    private readonly int selfInnerId;

    private bool enabled = true;
    private bool focusable;
    private bool visible = true;

    private bool measureRequested = true;
    private Matrix4x4 fullTransformation = Matrix4x4.Identity;
    private Matrix4x4 localTransformation = Matrix4x4.Identity;

    /// <summary>Initializes a new instance of the <see cref="Spannable"/> class.</summary>
    /// <param name="options">Options to use.</param>
    protected Spannable(SpannableOptions options)
    {
        this.Options = options ?? throw new NullReferenceException();
        this.Options.PropertyChanged += this.PropertyOnPropertyChanged;
        this.selfInnerId = this.InnerIdAvailableSlot++;
    }

    /// <summary>Occurs when the control receives focus.</summary>
    public event SpannableEventHandler? GotFocus;

    /// <summary>Occurs when the control loses focus.</summary>
    public event SpannableEventHandler? LostFocus;

    /// <summary>Occurs when the property <see cref="Enabled"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? EnabledChange;

    /// <summary>Occurs when the property <see cref="Focusable"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? FocusableChange;

    /// <summary>Occurs when the property <see cref="Visible"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? VisibleChange;

    /// <summary>Occurs when anything about the spannable changes.</summary>
    /// <remarks>Used to determine when to measure again.</remarks>
    public event PropertyChangeEventHandler? PropertyChange;

    /// <summary>Occurs when the spannable will be receiving events shortly for the frame.</summary>
    public event SpannableEventHandler? PreDispatchEvents;

    /// <summary>Occurs when the spannable is done receiving events for the frame.</summary>
    public event SpannableEventHandler? PostDispatchEvents;

    /// <summary>Occurs when the spannable needs to be measured.</summary>
    public event SpannableEventHandler? Measure;

    /// <summary>Occurs when the spannable needs to be placed.</summary>
    public event SpannableEventHandler? Place;

    /// <summary>Occurs when the spannable needs to be drawn.</summary>
    public event SpannableDrawEventHandler? Draw;

    /// <summary>Gets the guaranteed starting value of <see cref="InnerIdAvailableSlot"/> when extending directly from
    /// this class.</summary>
    public static int InnerIdAvailableSlotStart => 1;

    /// <summary>Gets or sets a value indicating whether this control is enabled.</summary>
    public bool Enabled
    {
        get => this.enabled;
        set => this.HandlePropertyChange(
            nameof(this.Enabled),
            ref this.enabled,
            value,
            this.enabled == value,
            this.OnEnabledChange);
    }

    /// <summary>Gets or sets a value indicating whether this control is focusable.</summary>
    public bool Focusable
    {
        get => this.focusable;
        set => this.HandlePropertyChange(
            nameof(this.Focusable),
            ref this.focusable,
            value,
            this.focusable == value,
            this.OnFocusableChange);
    }

    /// <summary>Gets or sets a value indicating whether this control is visible.</summary>
    // TODO: a property indicating whether to assume zero size when invisible, so that it can skip measure pass
    public bool Visible
    {
        get => this.visible;
        set => this.HandlePropertyChange(
            nameof(this.Visible),
            ref this.visible,
            value,
            this.visible == value,
            this.OnVisibleChange);
    }

    /// <summary>Gets or sets the renderer being used.</summary>
    public ISpannableRenderer? Renderer { get; set; }

    /// <summary>Gets or sets the ImGui global ID.</summary>
    public uint ImGuiGlobalId { get; set; }

    /// <summary>Gets or sets the source template, if available.</summary>
    public ISpannableTemplate? SourceTemplate { get; protected set; }

    /// <summary>Gets or sets the measured boundary.</summary>
    /// <remarks>Boundary may extend leftward or upward past zero.</remarks>
    public RectVector4 Boundary { get; protected set; }

    /// <summary>Gets the mutable options for <see cref="Spannable"/>.</summary>
    public SpannableOptions Options { get; }

    /// <summary>Gets an immutable reference to the full transformation matrix.</summary>
    public ref readonly Matrix4x4 FullTransformation => ref this.fullTransformation;

    /// <summary>Gets an immutable reference to the local transformation returned from
    /// <see cref="TransformLocalTransformation"/> during <see cref="Place"/>.</summary>
    protected ref readonly Matrix4x4 LocalTransformation => ref this.localTransformation;

    /// <summary>Gets a value indicating whether <see cref="IDisposable.Dispose"/> has been called.</summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>Gets all the child spannables.</summary>
    /// <returns>A collection of every <see cref="Spannable"/> children. May contain nulls.</returns>
    public virtual IReadOnlyList<Spannable?> GetAllChildSpannables() => Array.Empty<Spannable?>();

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Requests the spannable to process next <see cref="RenderPassMeasure"/> again.</summary>
    public void RequestMeasure() => this.measureRequested = true;

    /// <summary>Called before dispatching events.</summary>
    public void RenderPassPreDispatchEvents()
    {
        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPreDispatchEvents(e);

        if (!e.SuppressHandling)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
                children[i]?.RenderPassPreDispatchEvents();
        }

        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Called before dispatching events.</summary>
    public void RenderPassPostDispatchEvents()
    {
        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPostDispatchEvents(e);

        if (!e.SuppressHandling)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
                children[i]?.RenderPassPostDispatchEvents();
        }

        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Measures the spannable according to the parameters specified, and updates the result properties.
    /// </summary>
    /// <returns><c>true</c> if the spannable has just been measured; <c>false</c> if no measurement has happened
    /// because nothing changed.</returns>
    public bool RenderPassMeasure()
    {
        if (!this.ShouldMeasureAgain())
            return false;

        this.measureRequested = false;
        this.Boundary = RectVector4.InvertedExtrema;

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnMeasure(e);
        SpannableEventArgsPool.Return(e);

        return true;
    }

    /// <summary>Updates the transformation for the measured data.</summary>
    /// <param name="local">The local transformation matrix.</param>
    /// <param name="ancestral">The ancestral transformation matrix.</param>
    public void RenderPassPlace(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
    {
        this.localTransformation = this.TransformLocalTransformation(local);
        this.fullTransformation = Matrix4x4.Multiply(local, ancestral);

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPlace(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Draws from the measured data.</summary>
    /// <param name="drawListPtr">The target draw list.</param>
    public void RenderPassDraw(ImDrawListPtr drawListPtr)
    {
        var e = SpannableEventArgsPool.Rent<SpannableDrawEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializeDrawEvent(drawListPtr);
        this.OnDraw(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Finds a measurement for the child at the given offset.</summary>
    /// <param name="screenOffset">The screen offset.</param>
    /// <returns>The found child, or <c>null</c> if none was found.</returns>
    public virtual Spannable? FindChildAtPos(Vector2 screenOffset)
    {
        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is not { } c)
                continue;
            if (c.Boundary.Contains(c.PointToClient(screenOffset)))
                return c;
        }

        return null;
    }

    /// <summary>Tests if the given local location belongs in this spannable.</summary>
    /// <param name="localLocation">Local location to test.</param>
    /// <returns><c>true</c> if it is the case.</returns>
    public virtual bool HitTest(Vector2 localLocation) => this.Boundary.Contains(localLocation);

    /// <summary>Disposes this instance of <see cref="Spannable{TOptions}"/>.</summary>
    /// <param name="disposing">Whether it is being called from <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var s in this.GetAllChildSpannables())
            {
                if (s?.SourceTemplate is { } st)
                    st.RecycleSpannable(s);
                else
                    s?.Dispose();
            }
        }
    }

    /// <summary>Determines if <see cref="Measure"/> event should be called from
    /// <see cref="Spannable.RenderPassMeasure"/>.</summary>
    /// <returns><c>true</c> if it is.</returns>
    protected virtual bool ShouldMeasureAgain()
    {
        if (this.measureRequested)
            return true;

        var children = this.GetAllChildSpannables();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i]?.ShouldMeasureAgain() is true)
                return true;
        }

        return false;
    }

    /// <summary>Transforms the local transformation matrix according to extra spannable-specific specifications.
    /// </summary>
    /// <param name="local">Local transformation matrix specified from the parent.</param>
    /// <returns>Transformed local transformation matrix.</returns>
    protected virtual Matrix4x4 TransformLocalTransformation(scoped in Matrix4x4 local) => local;

    /// <summary>Raises the <see cref="GotFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnGotFocus(SpannableEventArgs args) => this.GotFocus?.Invoke(args);

    /// <summary>Raises the <see cref="LostFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnLostFocus(SpannableEventArgs args) => this.LostFocus?.Invoke(args);

    /// <summary>Raises the <see cref="EnabledChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnEnabledChange(PropertyChangeEventArgs<bool> args) => this.EnabledChange?.Invoke(args);

    /// <summary>Raises the <see cref="FocusableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnFocusableChange(PropertyChangeEventArgs<bool> args) => this.FocusableChange?.Invoke(args);

    /// <summary>Raises the <see cref="VisibleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnVisibleChange(PropertyChangeEventArgs<bool> args) => this.VisibleChange?.Invoke(args);

    /// <summary>Raises the <see cref="PropertyChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs"/> that contains the event data.</param>
    protected virtual void OnPropertyChange(PropertyChangeEventArgs args)
    {
        if (args.State == PropertyChangeState.After)
            this.RequestMeasure();
        this.PropertyChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="PreDispatchEvents"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPreDispatchEvents(SpannableEventArgs args) => this.PreDispatchEvents?.Invoke(args);

    /// <summary>Raises the <see cref="PostDispatchEvents"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPostDispatchEvents(SpannableEventArgs args) => this.PostDispatchEvents?.Invoke(args);

    /// <summary>Raises the <see cref="Measure"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnMeasure(SpannableEventArgs args) => this.Measure?.Invoke(args);

    /// <summary>Raises the <see cref="Place"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPlace(SpannableEventArgs args) => this.Place?.Invoke(args);

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(SpannableDrawEventArgs args) => this.Draw?.Invoke(args);

    /// <summary>Compares a new value with the old value, and invokes event handler accordingly.</summary>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="eq">Whether the values are equal.</param>
    /// <param name="eh">The event handler.</param>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    /// <returns><c>true</c> if changed.</returns>
    protected bool HandlePropertyChange<T>(
        string propName,
        ref T storage,
        T newValue,
        bool eq,
        PropertyChangeEventHandler<T> eh)
    {
        if (eq)
            return false;

        var e = SpannableEventArgsPool.Rent<PropertyChangeEventArgs<T>>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializePropertyChangeEvent(propName, PropertyChangeState.Before, storage, newValue);

        this.OnPropertyChange(e);
        eh(e);

        if (e.SuppressHandling)
        {
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializePropertyChangeEvent(propName, PropertyChangeState.Cancelled, storage, newValue);
            this.OnPropertyChange(e);
            eh(e);

            SpannableEventArgsPool.Return(e);
            return false;
        }

        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializePropertyChangeEvent(propName, PropertyChangeState.After, storage, newValue);
        storage = e.NewValue;

        this.OnPropertyChange(e);
        eh(e);

        SpannableEventArgsPool.Return(e);
        return true;
    }

    /// <summary>Called when <see cref="Options"/> has a changed property.</summary>
    /// <param name="args">Change details.</param>
    protected virtual void PropertyOnPropertyChanged(PropertyChangeEventArgs args)
    {
        if (args.State == PropertyChangeState.After)
            this.RequestMeasure();
    }
}
