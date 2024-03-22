using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Brushes;

/// <summary>A spannable that can be used as a brush, for backgrounds and alike.</summary>
/// <remarks>If <see cref="SpannableMeasureArgs.MaxSize"/> is not bound, then nothing will be drawn.</remarks>
public abstract class BrushSpannable : ISpannable
{
    private readonly State?[] statePool = new State?[4];

    /// <inheritdoc/>
    public ISpannableState RentState(scoped in SpannableRentStateArgs args)
    {
        State? res = null;
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is not null)
            {
                res = s;
                break;
            }
        }

        res ??= this.CreateNewState();
        res.OnRentState(args);
        return res;
    }

    /// <inheritdoc/>
    public void ReturnState(ISpannableState? state)
    {
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is null)
            {
                s = state as State;
                return;
            }
        }
    }

    /// <inheritdoc/>
    public void MeasureSpannable(scoped in SpannableMeasureArgs args) =>
        (args.State as State)?.OnMeasure(args);

    /// <inheritdoc/>
    public void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args) =>
        (args.State as State)?.OnCommitMeasurement(args);

    /// <inheritdoc/>
    public virtual void HandleSpannableInteraction(scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link) => link = default;

    /// <inheritdoc/>
    public abstract void DrawSpannable(SpannableDrawArgs args);

    /// <summary>Creates a new state.</summary>
    /// <returns>The new state.</returns>
    protected virtual State CreateNewState() => new();

    /// <summary>A state for <see cref="BrushSpannable"/>.</summary>
    protected class State : ISpannableState
    {
        private TextState activeTextState;
        private RectVector4 boundary;
        private Matrix4x4 transformation;

        /// <inheritdoc/>
        public ref TextState TextState => ref this.activeTextState;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; private set; }

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
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Called when <see cref="ISpannable.RentState"/> has been called.</summary>
        /// <param name="args">The arguments.</param>
        public void OnRentState(SpannableRentStateArgs args)
        {
            this.Renderer = args.Renderer;
            this.ImGuiGlobalId = args.ImGuiGlobalId;
            this.Scale = args.Scale;
            this.activeTextState = args.TextState;
        }

        /// <summary>Called when <see cref="ISpannable.MeasureSpannable"/> has been called.</summary>
        /// <param name="args">The arguments.</param>
        public void OnMeasure(scoped in SpannableMeasureArgs args)
        {
            if (args.MaxSize.X >= float.MaxValue || args.MaxSize.Y >= float.MaxValue)
                this.boundary = new(Vector2.Zero);
            else
                this.boundary = new(Vector2.Zero, args.MaxSize);
        }

        /// <summary>Called when <see cref="ISpannable.CommitSpannableMeasurement"/> has been called.</summary>
        /// <param name="args">The arguments.</param>
        public void OnCommitMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            this.ScreenOffset = args.ScreenOffset;
            this.TransformationOrigin = args.TransformationOrigin;
            this.transformation = args.Transformation;
        }
    }
}
