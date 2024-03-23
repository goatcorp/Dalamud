using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
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

    /// <inheritdoc/>
    public IReadOnlyCollection<ISpannable?> Children => this.AllChildren;

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
    public ISpannableRenderPass RentRenderPass(scoped in SpannableRentRenderPassArgs args)
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
        res.OnRentState(args);
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
                return;
            }
        }
    }

    /// <summary>Disposes this instance of <see cref="PatternSpannable"/>.</summary>
    /// <param name="disposing">Whether it is being called from <see cref="Dispose"/>.</param>
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
    protected virtual PatternRenderPass CreateNewRenderPass() => new();

    /// <summary>A state for <see cref="PatternSpannable"/>.</summary>
    protected class PatternRenderPass : ISpannableRenderPass
    {
        private TextState activeTextState;
        private RectVector4 boundary;
        private Matrix4x4 transformation;

        /// <inheritdoc/>
        public ref TextState TextState => ref this.activeTextState;

        /// <summary>Gets the active render scale.</summary>
        public float Scale { get; private set; }

        /// <inheritdoc/>
        public ref readonly RectVector4 Boundary => ref this.boundary;

        /// <inheritdoc/>
        public Vector2 ScreenOffset { get; private set; }

        /// <inheritdoc/>
        public Vector2 TransformationOrigin { get; private set; }

        /// <inheritdoc/>
        public ref readonly Matrix4x4 Transformation => ref this.transformation;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; private set; }

        /// <inheritdoc/>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Called when <see cref="ISpannable.RentRenderPass"/> has been called.</summary>
        /// <param name="args">The arguments.</param>
        public virtual void OnRentState(SpannableRentRenderPassArgs args)
        {
            this.Renderer = args.Renderer;
        }

        /// <inheritdoc/>
        public virtual void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            this.Scale = args.Scale;
            this.activeTextState = args.TextState;
            if (args.MaxSize.X >= float.MaxValue || args.MaxSize.Y >= float.MaxValue)
                this.boundary = new(Vector2.Zero);
            else
                this.boundary = new(Vector2.Zero, args.MaxSize);
        }

        /// <inheritdoc/>
        public virtual void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            this.ScreenOffset = args.ScreenOffset;
            this.TransformationOrigin = args.TransformationOrigin;
            this.transformation = args.Transformation;
        }

        /// <inheritdoc/>
        public virtual void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args,
            out SpannableLinkInteracted link)
        {
            this.ImGuiGlobalId = args.ImGuiGlobalId;
            link = default;
        }

        /// <inheritdoc/>
        public virtual void DrawSpannable(SpannableDrawArgs args)
        {
        }
    }
}
