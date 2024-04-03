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

    /// <summary>Occurs when anything about the spannable changes.</summary>
    /// <remarks>Used to determine when to measure again.</remarks>
    public event PropertyChangeEventHandler? PropertyChange;

    /// <summary>Occurs when the spannable needs to be measured.</summary>
    public event SpannableEventHandler? Measure;

    /// <summary>Occurs when the spannable needs to be placed.</summary>
    public event SpannableEventHandler? Place;

    /// <summary>Occurs when the spannable needs to be drawn.</summary>
    public event SpannableDrawEventHandler? Draw;

    /// <summary>Gets the guaranteed starting value of <see cref="InnerIdAvailableSlot"/> when extending directly from
    /// this class.</summary>
    public static int InnerIdAvailableSlotStart => 1;

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

    /// <summary>Measures the spannable according to the parameters specified, and updates the result properties.
    /// </summary>
    public void RenderPassMeasure()
    {
        if (!this.ShouldMeasureAgain())
            return;

        this.measureRequested = false;

        if (!this.occupySpaceWhenHidden && !this.visible)
        {
            this.Boundary = RectVector4.Zero;
            return;
        }

        this.Boundary = RectVector4.InvertedExtrema;

        var e = SpannableEventArgsPool.Rent<SpannableEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        this.OnMeasure(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Updates the transformation for the measured data.</summary>
    /// <param name="local">The local transformation matrix.</param>
    /// <param name="ancestral">The ancestral transformation matrix.</param>
    public void RenderPassPlace(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
    {
        if (!this.occupySpaceWhenHidden && !this.visible)
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
        if (!this.visible)
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

    /// <summary>Raises the <see cref="Measure"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnMeasure(SpannableEventArgs args) => this.Measure?.Invoke(args);

    /// <summary>Raises the <see cref="Place"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnPlace(SpannableEventArgs args) => this.Place?.Invoke(args);

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(SpannableDrawEventArgs args) => this.Draw?.Invoke(args);

    /// <summary>Called when <see cref="Options"/> has a changed property.</summary>
    /// <param name="args">Change details.</param>
    protected virtual void PropertyOnPropertyChanged(PropertyChangeEventArgs args)
    {
        if (args.State == PropertyChangeState.After)
            this.RequestMeasure();
    }
}
