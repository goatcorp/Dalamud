using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
/// <remarks>If <see cref="ISpannableMeasurementOptions.Size"/> is not bound, then nothing will be drawn.</remarks>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "No")]
public abstract class PatternSpannable : ISpannable
{
    private readonly PatternSpannableMeasurement?[] statePool = new PatternSpannableMeasurement?[4];
    private Vector2 size = new(float.PositiveInfinity);
    private Vector2 minSize = Vector2.Zero;
    private Vector2 maxSize = new(float.PositiveInfinity);

    /// <inheritdoc/>
    public event Action<ISpannable>? SpannableChange;

    /// <summary>Gets or sets the size.</summary>
    public Vector2 Size
    {
        get => this.size;
        set => this.HandlePropertyChange(nameof(this.Size), ref this.size, value);
    }

    /// <summary>Gets or sets the minimum size.</summary>
    public Vector2 MinSize
    {
        get => this.minSize;
        set => this.HandlePropertyChange(nameof(this.MinSize), ref this.minSize, value);
    }

    /// <summary>Gets or sets the maximum size.</summary>
    public Vector2 MaxSize
    {
        get => this.maxSize;
        set => this.HandlePropertyChange(nameof(this.MaxSize), ref this.maxSize, value);
    }

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
    public virtual ISpannableMeasurement RentMeasurement(ISpannableRenderer renderer)
    {
        PatternSpannableMeasurement? res = null;
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is not null)
            {
                res = s;
                break;
            }
        }

        res ??= this.CreateNewRenderPass();
        res.OnRentMeasurement(renderer);
        return res;
    }

    /// <inheritdoc/>
    public virtual void ReturnMeasurement(ISpannableMeasurement? pass)
    {
        foreach (ref var s in this.statePool.AsSpan())
        {
            if (s is null)
            {
                s = pass as PatternSpannableMeasurement;
                s?.OnReturnMeasurement();
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
    protected virtual PatternSpannableMeasurement CreateNewRenderPass() => new(this, new());

    /// <summary>Assigns a new value to a property..</summary>
    /// <param name="propName">The property name. Use <c>nameof(...)</c>.</param>
    /// <param name="storage">The reference of the stored value.</param>
    /// <param name="newValue">The new value.</param>
    /// <typeparam name="T">Type of the changed value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void HandlePropertyChange<T>(string propName, ref T storage, T newValue)
    {
        if (Equals(storage, newValue))
            return;
        storage = newValue;
        this.SpannableChange?.Invoke(this);
    }

    /// <summary>A state for <see cref="PatternSpannable"/>.</summary>
    protected class PatternSpannableMeasurement : ISpannableMeasurement
    {
        private readonly PatternSpannable owner;
        private readonly SpannableMeasurementOptions options;

        private Matrix4x4 localTransformation;
        private Matrix4x4 fullTransformation;

        /// <summary>Initializes a new instance of the <see cref="PatternSpannableMeasurement"/> class.</summary>
        /// <param name="owner">The owner of this render pass.</param>
        /// <param name="options">The options for this render pass.</param>
        public PatternSpannableMeasurement(PatternSpannable owner, SpannableMeasurementOptions options)
        {
            this.owner = owner;
            this.options = options;
            this.options.PropertyChanged += this.OptionsOnPropertyChanged;
        }

        /// <inheritdoc/>
        public ISpannable? Spannable => this.owner;

        /// <inheritdoc/>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <inheritdoc/>
        public bool IsMeasurementValid { get; private set; }

        /// <inheritdoc/>
        public RectVector4 Boundary { get; private set; }

        /// <inheritdoc/>
        public ISpannableMeasurementOptions Options => this.options;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; set; }

        /// <inheritdoc/>
        public float RenderScale { get; set; }

        /// <summary>Gets a read-only reference to the local transformation matrix.</summary>
        public ref readonly Matrix4x4 LocalTransformation => ref this.localTransformation;

        /// <inheritdoc/>
        public ref readonly Matrix4x4 FullTransformation => ref this.fullTransformation;

        /// <inheritdoc/>
        public virtual bool TryReset()
        {
            this.localTransformation = this.fullTransformation = Matrix4x4.Identity;
            this.Renderer = null!;
            this.IsMeasurementValid = false;
            this.Boundary = RectVector4.InvertedExtrema;
            this.ImGuiGlobalId = 0u;
            this.RenderScale = 1f;
            return true;
        }

        /// <summary>Called when <see cref="ISpannable.RentMeasurement"/> has been called.</summary>
        /// <param name="renderer">The renderer.</param>
        public virtual void OnRentMeasurement(ISpannableRenderer renderer) => this.Renderer = renderer;

        /// <summary>Called when <see cref="ISpannable.ReturnMeasurement"/> has been called.</summary>
        public virtual void OnReturnMeasurement()
        {
        }

        /// <summary>Measures the spannable according to the parameters specified, and updates the result properties.
        /// </summary>
        /// <returns><c>true</c> if a measurement has changed.</returns>
        public virtual bool Measure()
        {
            var size = Vector2.Clamp(this.owner.Size, this.owner.MinSize, this.owner.MaxSize);

            if (size.X >= float.PositiveInfinity)
                size.X = this.Options.Size.X;
            if (size.Y >= float.PositiveInfinity)
                size.Y = this.Options.Size.Y;

            if (this.Boundary.LeftTop == Vector2.Zero && this.Boundary.RightBottom == size)
                return false;
            this.Boundary = new(Vector2.Zero, size);
            return true;
        }

        /// <inheritdoc/>
        public virtual bool HandleInteraction() => true;

        /// <summary>Updates the transformation for the measured data.</summary>
        /// <param name="local">The local transformation matrix.</param>
        /// <param name="ancestral">The ancestral transformation matrix.</param>
        public virtual void UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
        {
            this.localTransformation = local;
            this.fullTransformation = Matrix4x4.Multiply(local, ancestral);
        }

        /// <summary>Draws from the measured data.</summary>
        /// <param name="drawListPtr">The target draw list.</param>
        public void Draw(ImDrawListPtr drawListPtr)
        {
            using var st = new ScopedTransformer(drawListPtr, this.localTransformation, Vector2.One, 1f);
            this.DrawUntransformed(drawListPtr);
        }

        /// <inheritdoc/>
        public void ReturnMeasurementToSpannable() => this.Spannable?.ReturnMeasurement(this);

        /// <summary>Draws the spannable without regarding to <see cref="LocalTransformation"/>.</summary>
        /// <param name="drawListPtr">The target draw list.</param>
        protected virtual void DrawUntransformed(ImDrawListPtr drawListPtr)
        {
        }

        private void OptionsOnPropertyChanged(string obj) => this.IsMeasurementValid = false;
    }
}
