using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
/// <remarks>If <see cref="SpannableMeasureArgs.MaxSize"/> is not bound, then nothing will be drawn.</remarks>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public abstract class PatternSpannable : ISpannable
{
    private readonly PatternRenderPass?[] statePool = new PatternRenderPass?[4];

    /// <summary>Gets or sets the size.</summary>
    public Vector2 Size { get; set; } = new(float.PositiveInfinity);

    /// <summary>Gets or sets the minimum size.</summary>
    public Vector2 MinSize { get; set; } = Vector2.Zero;

    /// <summary>Gets or sets the maximum size.</summary>
    public Vector2 MaxSize { get; set; } = new(float.PositiveInfinity);

    /// <inheritdoc/>
    public int StateGeneration { get; protected set; }

    /// <summary>Gets the list of all children contained within this control, including decorative ones.</summary>
    protected List<ISpannable?> AllChildren { get; } = [];

    /// <summary>Gets the available slot index in <see cref="AllChildren"/> for use by inheritors.</summary>
    protected int AllChildrenAvailableSlot { get; init; }

    /// <summary>Gets the available slot index for inner ID, for use with
    /// <see cref="SpannableExtensions.GetGlobalIdFromInnerId"/>.</summary>
    protected int InnerIdAvailableSlot { get; init; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<ISpannable?> GetAllChildSpannables() => this.AllChildren;

    /// <inheritdoc/>
    public ISpannableRenderPass RentRenderPass(ISpannableRenderer renderer)
    {
        PatternRenderPass? res = null;
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is not null)
            {
                res = s;
                break;
            }
        }

        res ??= this.CreateNewRenderPass();
        res.OnRentState(renderer);
        return res;
    }

    /// <inheritdoc/>
    public void ReturnRenderPass(ISpannableRenderPass? pass)
    {
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is null)
            {
                s = pass as PatternRenderPass;
                s?.OnReturnState();
                return;
            }
        }
    }

    /// <summary>Disposes this instance of <see cref="PatternSpannable"/>.</summary>
    /// <param name="disposing">Whether it is being called from <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (ref var s in CollectionsMarshal.AsSpan(this.AllChildren))
            {
                s?.Dispose();
                s = null;
            }
        }
    }

    /// <summary>Creates a new state.</summary>
    /// <returns>The new state.</returns>
    protected virtual PatternRenderPass CreateNewRenderPass() => new(this);

    /// <summary>A state for <see cref="PatternSpannable"/>.</summary>
    protected class PatternRenderPass(PatternSpannable owner) : ISpannableRenderPass
    {
        private TextState activeTextState;
        private RectVector4 boundary;
        private Matrix4x4 transformationFromParent;
        private Matrix4x4 transformationFromAncestors;

        /// <inheritdoc/>
        public ISpannable RenderPassCreator => owner;

        /// <inheritdoc/>
        public ref TextState ActiveTextState => ref this.activeTextState;

        /// <summary>Gets the active render scale.</summary>
        public float Scale { get; private set; }

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; private set; }

        /// <inheritdoc/>
        public ref readonly RectVector4 Boundary => ref this.boundary;

        /// <inheritdoc/>
        public Vector2 InnerOrigin { get; private set; }

        /// <inheritdoc/>
        public ref readonly Matrix4x4 TransformationFromParent => ref this.transformationFromParent;

        /// <inheritdoc/>
        public ref readonly Matrix4x4 TransformationFromAncestors => ref this.transformationFromAncestors;

        /// <inheritdoc/>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Called when <see cref="ISpannable.RentRenderPass"/> has been called.</summary>
        /// <param name="renderer">The renderer.</param>
        public virtual void OnRentState(ISpannableRenderer renderer) => this.Renderer = renderer;

        /// <summary>Called when <see cref="ISpannable.ReturnRenderPass"/> has been called.</summary>
        public virtual void OnReturnState()
        {
        }

        /// <inheritdoc/>
        public virtual void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            this.Scale = args.Scale;
            this.ImGuiGlobalId = args.ImGuiGlobalId;
            this.activeTextState = args.TextState;

            var ps = (PatternSpannable)owner;

            var size = Vector2.Clamp(ps.Size, ps.MinSize, ps.MaxSize);
            size = Vector2.Clamp(size, args.MinSize, args.MaxSize);

            if (size.X >= float.PositiveInfinity)
                size.X = 0;
            if (size.Y >= float.PositiveInfinity)
                size.Y = 0;

            this.boundary = new(Vector2.Zero, size);
        }

        /// <inheritdoc/>
        public virtual void CommitSpannableMeasurement(scoped in SpannableCommitMeasurementArgs args)
        {
            this.InnerOrigin = args.InnerOrigin;
            this.transformationFromParent = args.TransformationFromParent;
            this.transformationFromAncestors = args.TransformationFromAncestors;
        }

        /// <inheritdoc/>
        public void DrawSpannable(SpannableDrawArgs args)
        {
            using var st = ScopedTransformer.From(args, Vector2.One, 1f);
            this.DrawUntransformed(args);
        }

        /// <inheritdoc/>
        public virtual void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args,
            out SpannableLinkInteracted link)
        {
            link = default;
        }

        /// <summary>Draws the spannable without regarding to <see cref="TransformationFromParent"/>.</summary>
        /// <param name="args">The drawing arguments.</param>
        protected virtual void DrawUntransformed(SpannableDrawArgs args)
        {
        }
    }
}
