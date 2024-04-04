using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

#pragma warning disable SA1010

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable : IDisposable
{
    private static uint imGuiGlobalIdGeneratorCounter = 0x822FC8AB;

    private Vector2 lastMeasurePreferredSize;
    private Matrix4x4 fullTransformation = Matrix4x4.Identity;
    private Matrix4x4 localTransformation = Matrix4x4.Identity;

    /// <summary>Initializes a new instance of the <see cref="Spannable"/> class.</summary>
    protected Spannable() => this.imGuiGlobalId = imGuiGlobalIdGeneratorCounter++;

    /// <summary>Occurs when anything about the spannable changes.</summary>
    /// <remarks>Used to determine when to measure again.</remarks>
    public event PropertyChangeEventHandler? PropertyChange;

    /// <summary>Occurs when the spannable needs to be measured.</summary>
    public event SpannableMeasureEventHandler? Measure;

    /// <summary>Occurs when the spannable needs to be placed.</summary>
    public event SpannableEventHandler? Place;

    /// <summary>Occurs when the spannable needs to be drawn.</summary>
    public event SpannableDrawEventHandler? Draw;

    /// <summary>Gets or sets the measured boundary.</summary>
    /// <remarks>Boundary may extend leftward or upward past zero.</remarks>
    public RectVector4 Boundary { get; protected set; }

    /// <summary>Gets the effective scale from the current (or last, if outside) render cycle.</summary>
    public virtual float EffectiveRenderScale => this.RenderScale * (this.Parent?.EffectiveRenderScale ?? 1f);

    /// <summary>Gets an immutable reference to the full transformation matrix.</summary>
    public ref readonly Matrix4x4 FullTransformation => ref this.fullTransformation;

    /// <summary>Gets an immutable reference to the local transformation returned from
    /// <see cref="TransformLocalTransformation"/> during <see cref="Place"/>.</summary>
    protected ref readonly Matrix4x4 LocalTransformation => ref this.localTransformation;

    /// <summary>Gets a value indicating whether <see cref="IDisposable.Dispose"/> has been called.</summary>
    protected bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the control display state.</summary>
    /// <returns>The display state.</returns>
    public DisplayedStatePattern.DisplayedState GetDisplayedState()
    {
        if (!this.EffectivelyVisible)
            return DisplayedStatePattern.DisplayedState.Hidden;
        if (!this.Enabled)
            return DisplayedStatePattern.DisplayedState.Disabled;
        if (this.IsMouseHoveredInsideBoundary && this.IsAnyMouseButtonDown)
            return DisplayedStatePattern.DisplayedState.Active;
        if (this.IsMouseHoveredInsideBoundary && this.ImGuiIsHoverable)
            return DisplayedStatePattern.DisplayedState.Hovered;
        return DisplayedStatePattern.DisplayedState.Normal;
    }

    /// <summary>Requests the spannable to process next <see cref="RenderPassMeasure"/> again.</summary>
    public void RequestMeasure()
    {
        this.lastMeasurePreferredSize = new(float.NaN);
        this.Parent?.RequestMeasure();
    }

    /// <summary>Measures the spannable according to the parameters specified, and updates the result properties.
    /// </summary>
    /// <param name="preferredSize">The preferred size.</param>
    public void RenderPassMeasure(Vector2 preferredSize)
    {
        if (this.lastMeasurePreferredSize == preferredSize)
            return;

        this.lastMeasurePreferredSize = preferredSize;

        if (!this.occupySpaceWhenHidden && !this.EffectivelyVisible)
        {
            this.Boundary = RectVector4.Zero;
            return;
        }

        this.Boundary = RectVector4.InvertedExtrema;

        var e = SpannableEventArgsPool.Rent<SpannableMeasureEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializeMeasureEvent(preferredSize);
        this.OnMeasure(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Updates the transformation for the measured data.</summary>
    /// <param name="local">The local transformation matrix.</param>
    /// <param name="ancestral">The ancestral transformation matrix.</param>
    public void RenderPassPlace(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
    {
        if (!this.occupySpaceWhenHidden && !this.EffectivelyVisible)
            return;

        this.localTransformation = this.TransformLocalTransformation(local);
        this.fullTransformation = Matrix4x4.Multiply(this.localTransformation, ancestral);

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnPlace(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Draws from the measured data.</summary>
    /// <param name="drawListPtr">The target draw list.</param>
    public void RenderPassDraw(ImDrawListPtr drawListPtr)
    {
        if (!this.EffectivelyVisible)
            return;

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
        foreach (var child in this.EnumerateChildren(false))
        {
            if (child.Boundary.Contains(child.PointToClient(screenOffset)))
                return child;
        }

        return null;
    }

    /// <summary>Tests if the given local location belongs in this spannable.</summary>
    /// <param name="localLocation">Local location to test.</param>
    /// <returns><c>true</c> if it is the case.</returns>
    public virtual bool HitTest(Vector2 localLocation) => this.Boundary.Contains(localLocation);

    /// <summary>Disposes this instance of <see cref="Spannable"/>.</summary>
    /// <param name="disposing">Whether it is being called from <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            this.ClearChildren();
    }

    /// <summary>Transforms the local transformation matrix according to extra spannable-specific specifications.
    /// </summary>
    /// <param name="local">Local transformation matrix specified from the parent.</param>
    /// <returns>Transformed local transformation matrix.</returns>
    protected virtual Matrix4x4 TransformLocalTransformation(scoped in Matrix4x4 local) => local;

    /// <summary>Raises the <see cref="Measure"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnMeasure(SpannableMeasureEventArgs args) => this.Measure?.Invoke(args);

    /// <summary>Raises the <see cref="Place"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPlace(SpannableEventArgs args) => this.Place?.Invoke(args);

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(SpannableDrawEventArgs args) => this.Draw?.Invoke(args);
}
