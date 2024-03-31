using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern with three icons and a background.</summary>
public class TristateIconPattern : AbstractPattern.AbstractSpannable<TristateIconPattern.IconOptions>
{
    private readonly int backgroundInnerId;
    private readonly int foregroundInnerId;

    private readonly Spannable?[] children = new Spannable?[3];
    private readonly bool[] childrenInvalidated = new bool[3];

    private bool? previousState;

    private ISpannableTemplate? activeState;
    private ISpannableTemplate? disappearingState;

    /// <summary>Initializes a new instance of the <see cref="TristateIconPattern"/> class.</summary>
    /// <param name="options">Icon options.</param>
    /// <param name="sourceTemplate">The source template.</param>
    public TristateIconPattern(IconOptions options, ISpannableTemplate? sourceTemplate = null)
        : base(options, sourceTemplate)
    {
        this.backgroundInnerId = this.InnerIdAvailableSlot++;
        this.foregroundInnerId = this.InnerIdAvailableSlot++;
        this.previousState = options.State;
    }

    private Spannable? BackgroundSpannable
    {
        get => this.children[0];
        set => this.children[0] = value;
    }

    private Spannable? DisappearingSpannable
    {
        get => this.children[1];
        set => this.children[1] = value;
    }

    private Spannable? ActiveSpannable
    {
        get => this.children[2];
        set => this.children[2] = value;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<Spannable?> GetAllChildSpannables() => this.children;

    /// <inheritdoc/>
    protected override bool ShouldMeasureAgain() =>
        base.ShouldMeasureAgain()
        || this.Options.ShowIconAnimation?.IsRunning is true
        || this.Options.HideIconAnimation?.IsRunning is true;

    /// <inheritdoc/>
    protected override void PropertyOnPropertyChanged(PropertyChangeEventArgs args)
    {
        if (args.State == PropertyChangeState.After)
        {
            switch (args.PropertyName)
            {
                case nameof(IconOptions.Background):
                    this.childrenInvalidated[0] = true;
                    break;
                case nameof(IconOptions.State):
                    this.Options.ShowIconAnimation?.Restart();
                    this.Options.HideIconAnimation?.Restart();
                    this.childrenInvalidated[1] = true;
                    this.childrenInvalidated[2] = true;
                    break;
            }
        }

        base.PropertyOnPropertyChanged(args);

        if (args.State == PropertyChangeState.Before
            && !args.SuppressHandling
            && args.PropertyName == nameof(IconOptions.State))
            this.previousState = this.Options.State;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.BackgroundSpannable?.Dispose();
            this.ActiveSpannable?.Dispose();
            this.DisappearingSpannable?.Dispose();
            this.BackgroundSpannable = null;
            this.ActiveSpannable = null;
            this.DisappearingSpannable = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableEventArgs args)
    {
        base.OnMeasure(args);

        this.activeState = this.Options.State switch
        {
            null => this.Options.NullIcon,
            true => this.Options.TrueIcon,
            false => this.Options.FalseIcon,
        };

        if (this.childrenInvalidated[0])
        {
            this.BackgroundSpannable?.SourceTemplate?.RecycleSpannable(this.BackgroundSpannable);
            this.BackgroundSpannable = null;
        }

        if (this.childrenInvalidated[1])
        {
            this.DisappearingSpannable?.SourceTemplate?.RecycleSpannable(this.DisappearingSpannable);
            this.DisappearingSpannable = null;
        }

        if (this.childrenInvalidated[2])
        {
            this.ActiveSpannable?.SourceTemplate?.RecycleSpannable(this.ActiveSpannable);
            this.ActiveSpannable = null;
        }

        this.BackgroundSpannable ??= this.Options.Background?.CreateSpannable();
        if (this.BackgroundSpannable is not null)
        {
            this.BackgroundSpannable.Options.RenderScale = this.Options.RenderScale;
            this.BackgroundSpannable.Options.PreferredSize = this.Boundary.Size;
            this.BackgroundSpannable.Options.VisibleSize = this.Boundary.Size;
            this.BackgroundSpannable.Renderer = this.Renderer;
            this.BackgroundSpannable.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.backgroundInnerId);
            this.BackgroundSpannable.RenderPassMeasure();
        }

        if (this.Options.HideIconAnimation?.IsRunning is true)
        {
            this.Options.HideIconAnimation.Update(this.Boundary);
            this.disappearingState = this.previousState switch
            {
                null => this.Options.NullIcon,
                true => this.Options.TrueIcon,
                false => this.Options.FalseIcon,
            };

            this.DisappearingSpannable ??= this.disappearingState?.CreateSpannable();
            if (this.DisappearingSpannable is not null)
            {
                this.DisappearingSpannable.Options.RenderScale = this.Options.RenderScale;
                this.DisappearingSpannable.Options.PreferredSize = this.Boundary.Size;
                this.DisappearingSpannable.Options.VisibleSize = this.Boundary.Size;
                this.DisappearingSpannable.Renderer = this.Renderer;
                this.DisappearingSpannable.RenderPassMeasure();
            }
        }

        this.Options.ShowIconAnimation?.Update(this.Boundary);

        this.ActiveSpannable ??= this.activeState?.CreateSpannable();
        if (this.ActiveSpannable is not null)
        {
            this.ActiveSpannable.Options.RenderScale = this.Options.RenderScale;
            this.ActiveSpannable.Options.PreferredSize = this.Boundary.Size;
            this.ActiveSpannable.Options.VisibleSize = this.Boundary.Size;
            this.ActiveSpannable.Renderer = this.Renderer;
            this.ActiveSpannable.ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.foregroundInnerId);
            this.ActiveSpannable.RenderPassMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);

        this.BackgroundSpannable?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);

        this.DisappearingSpannable?.RenderPassPlace(
            this.Options.HideIconAnimation?.IsRunning is true
                ? this.Options.HideIconAnimation.AnimatedTransformation
                : Matrix4x4.Identity,
            this.FullTransformation);

        this.ActiveSpannable?.RenderPassPlace(
            this.Options.ShowIconAnimation?.IsRunning is true
                ? this.Options.ShowIconAnimation.AnimatedTransformation
                : Matrix4x4.Identity,
            this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        this.BackgroundSpannable?.RenderPassDraw(args.DrawListPtr);

        if (this.DisappearingSpannable is not null)
        {
            using var transformer = new ScopedTransformer(
                args.DrawListPtr,
                Matrix4x4.Identity,
                Vector2.One,
                this.Options.HideIconAnimation?.IsRunning is true
                    ? this.Options.HideIconAnimation.AnimatedOpacity
                    : 0f);
            this.DisappearingSpannable.RenderPassDraw(args.DrawListPtr);
        }

        if (this.ActiveSpannable is not null)
        {
            using var transformer = new ScopedTransformer(
                args.DrawListPtr,
                Matrix4x4.Identity,
                Vector2.One,
                this.Options.ShowIconAnimation?.IsRunning is true
                    ? this.Options.ShowIconAnimation.AnimatedOpacity
                    : 1f);
            this.ActiveSpannable.RenderPassDraw(args.DrawListPtr);
        }
    }

    /// <summary>Icon options.</summary>
    public class IconOptions : AbstractPattern.PatternOptions
    {
        private SpannableAnimator? showIconAnimation;
        private SpannableAnimator? hideIconAnimation;
        private ISpannableTemplate? trueIcon;
        private ISpannableTemplate? falseIcon;
        private ISpannableTemplate? nullIcon;
        private ISpannableTemplate? background;
        private bool? state;

        /// <summary>Gets or sets the animation to play for the icon being shown, when the icon currently being used is
        /// changed.</summary>
        public SpannableAnimator? ShowIconAnimation
        {
            get => this.showIconAnimation;
            set => this.UpdateProperty(
                nameof(this.ShowIconAnimation),
                ref this.showIconAnimation,
                value,
                this.showIconAnimation == value);
        }

        /// <summary>Gets or sets the animation to play for the icon being hidden, when the icon currently being used is
        /// changed.</summary>
        public SpannableAnimator? HideIconAnimation
        {
            get => this.hideIconAnimation;
            set => this.UpdateProperty(
                nameof(this.HideIconAnimation),
                ref this.hideIconAnimation,
                value,
                this.hideIconAnimation == value);
        }

        /// <summary>Gets or sets the icon to use when checked.</summary>
        public ISpannableTemplate? TrueIcon
        {
            get => this.trueIcon;
            set => this.UpdateProperty(
                nameof(this.TrueIcon),
                ref this.trueIcon,
                value,
                ReferenceEquals(this.trueIcon, value));
        }

        /// <summary>Gets or sets the icon to use when not checked.</summary>
        public ISpannableTemplate? FalseIcon
        {
            get => this.falseIcon;
            set => this.UpdateProperty(
                nameof(this.FalseIcon),
                ref this.falseIcon,
                value,
                ReferenceEquals(this.falseIcon, value));
        }

        /// <summary>Gets or sets the icon to use when indeterminate.</summary>
        public ISpannableTemplate? NullIcon
        {
            get => this.nullIcon;
            set => this.UpdateProperty(
                nameof(this.NullIcon),
                ref this.nullIcon,
                value,
                ReferenceEquals(this.nullIcon, value));
        }

        /// <summary>Gets or sets the background.</summary>
        public ISpannableTemplate? Background
        {
            get => this.background;
            set => this.UpdateProperty(
                nameof(this.Background),
                ref this.background,
                value,
                ReferenceEquals(this.background, value));
        }

        /// <summary>Gets or sets a value indicating whether it is checked.</summary>
        public bool? State
        {
            get => this.state;
            set => this.UpdateProperty(nameof(this.State), ref this.state, value, this.state == value);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.showIconAnimation = this.hideIconAnimation = null;
            this.trueIcon = this.falseIcon = this.nullIcon = this.background = null;
            return base.TryReset();
        }

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            if (source is IconOptions options)
            {
                this.showIconAnimation = (SpannableAnimator)options.showIconAnimation?.Clone();
                this.hideIconAnimation = (SpannableAnimator)options.hideIconAnimation?.Clone();
                this.trueIcon = options.trueIcon;
                this.falseIcon = options.falseIcon;
                this.nullIcon = options.nullIcon;
                this.background = options.background;
                this.state = options.state;
            }

            base.CopyFrom(source);
        }
    }

    /// <summary>A template for <see cref="TristateIconPattern"/>.</summary>
    public class Template(IconOptions options) : AbstractPattern.AbstractTemplate<IconOptions>(options)
    {
        private Template? falseTemplate;
        private Template? trueTemplate;
        private Template? nullTemplate;

        /// <summary>Gets the state.</summary>
        public bool? State => this.Options.State;

        /// <inheritdoc/>
        public override Spannable CreateSpannable() => new TristateIconPattern(this.Options, this);

        /// <summary>Gets a copy of <see cref="Template"/> with the specified state.</summary>
        /// <param name="state">The desired state.</param>
        /// <returns>The template.</returns>
        public Template WithState(bool? state)
        {
            if (state == this.State)
                return this;
            switch (state)
            {
                case null when this.nullTemplate is not null:
                    return this.nullTemplate;
                case false when this.falseTemplate is not null:
                    return this.falseTemplate;
                case true when this.trueTemplate is not null:
                    return this.trueTemplate;
            }

            var topt = new IconOptions();
            topt.CopyFrom(this.Options);
            topt.State = state;

            return state switch
            {
                false => this.falseTemplate = new(topt),
                true => this.trueTemplate = new(topt),
                _ => this.nullTemplate = new(topt),
            };
        }
    }
}
