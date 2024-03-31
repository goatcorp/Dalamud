using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;

namespace Dalamud.Interface.Spannables.Patterns;

#pragma warning disable SA1010

/// <summary>A spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
public static class AbstractPattern
{
    /// <summary>Base options for pattern spannables.</summary>
    public class PatternOptions : SpannableOptions
    {
        private Vector2 minSize = Vector2.Zero;
        private Vector2 size = new(float.PositiveInfinity);
        private Vector2 maxSize = new(float.PositiveInfinity);

        /// <summary>Gets or sets the size.</summary>
        /// <value><see cref="float.PositiveInfinity"/> for a dimension will use the size from the parent.</value>
        /// <remarks>This is not a hard limiting value.</remarks>
        public Vector2 Size
        {
            get => this.size;
            set => this.UpdateProperty(nameof(this.Size), ref this.size, value, this.size == value);
        }

        /// <summary>Gets or sets the minimum size.</summary>
        public Vector2 MinSize
        {
            get => this.minSize;
            set => this.UpdateProperty(nameof(this.MinSize), ref this.minSize, value, this.minSize == value);
        }

        /// <summary>Gets or sets the maximum size.</summary>
        public Vector2 MaxSize
        {
            get => this.maxSize;
            set => this.UpdateProperty(nameof(this.MaxSize), ref this.maxSize, value, this.maxSize == value);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.minSize = Vector2.Zero;
            this.size = new(float.PositiveInfinity);
            this.maxSize = new(float.PositiveInfinity);
            return base.TryReset();
        }

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            if (source is PatternOptions s)
            {
                this.Size = s.Size;
                this.MinSize = s.MinSize;
                this.MaxSize = s.MaxSize;
            }

            base.CopyFrom(source);
        }
    }

    /// <summary>A template for spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
    /// <typeparam name="TOptions">Type of spannable options.</typeparam>
    /// <remarks>If neither <see cref="PatternOptions.Size"/> nor <see cref="SpannableOptions.PreferredSize"/> is bound,
    /// then nothing will be drawn.</remarks>
    public class AbstractSpannable<TOptions> : Spannable<TOptions>
        where TOptions : PatternOptions, new()
    {
        /// <summary>Initializes a new instance of the <see cref="AbstractSpannable{TOptions}"/> class.</summary>
        /// <param name="options">The options for this render pass.</param>
        /// <param name="sourceTemplate">The source template.</param>
        public AbstractSpannable(TOptions options, ISpannableTemplate? sourceTemplate = null)
            : base(options)
        {
            this.SourceTemplate = sourceTemplate;
        }

        /// <summary>Occurs when the inside area needs to be drawn.</summary>
        public event SpannableDrawEventHandler? DrawInside;

        /// <inheritdoc/>
        protected override void OnMeasure(SpannableEventArgs args)
        {
            var size = this.Options.Size;
            if (size.X >= float.PositiveInfinity)
                size.X = this.Options.PreferredSize.X;
            if (size.Y >= float.PositiveInfinity)
                size.Y = this.Options.PreferredSize.Y;

            size = Vector2.Clamp(size, this.Options.MinSize, this.Options.MaxSize);

            if (size.X >= float.PositiveInfinity)
                size.X = this.Options.VisibleSize.X;
            if (size.Y >= float.PositiveInfinity)
                size.Y = this.Options.VisibleSize.Y;
            if (size.X >= float.PositiveInfinity || size.Y >= double.PositiveInfinity)
                size = Vector2.Zero;

            this.Boundary = new(Vector2.Zero, size);
            base.OnMeasure(args);
        }

        /// <inheritdoc/>
        protected override void OnDraw(SpannableDrawEventArgs args)
        {
            using var st = new ScopedTransformer(args.DrawListPtr, this.LocalTransformation, Vector2.One, 1f);
            var e = SpannableEventArgsPool.Rent<SpannableDrawEventArgs>();
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializeDrawEvent(args.DrawListPtr);
            this.OnDrawInside(e);
            SpannableEventArgsPool.Return(e);
        }

        /// <summary>Raises the <see cref="DrawInside"/> event.</summary>
        /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
        protected virtual void OnDrawInside(SpannableDrawEventArgs args) => this.DrawInside?.Invoke(args);
    }

    /// <summary>A spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
    /// <typeparam name="TOptions">Type of spannable options.</typeparam>
    /// <remarks>If <see cref="SpannableOptions.PreferredSize"/> is not bound, then nothing will be drawn.</remarks>
    public abstract class AbstractTemplate<TOptions>(TOptions options) : ISpannableTemplate
        where TOptions : PatternOptions, new()
    {
        /// <summary>Gets the configurable options.</summary>
        protected TOptions Options { get; } = options;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public virtual Spannable CreateSpannable() => new AbstractSpannable<TOptions>(this.Options, this);

        /// <summary>Disposes this instance of <see cref="AbstractTemplate{TOptions}"/>.</summary>
        /// <param name="disposing">Whether it is being called from <see cref="IDisposable.Dispose"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
