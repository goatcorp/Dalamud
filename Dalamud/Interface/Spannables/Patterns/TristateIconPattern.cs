using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern with three icons and a background.</summary>
public class TristateIconPattern : PatternSpannable
{
    private readonly int backgroundInnerId;
    private readonly int foregroundInnerId;

    private bool? disappearingState;
    private bool? state;

    /// <summary>Initializes a new instance of the <see cref="TristateIconPattern"/> class.</summary>
    public TristateIconPattern()
    {
        this.backgroundInnerId = this.InnerIdAvailableSlot++;
        this.foregroundInnerId = this.InnerIdAvailableSlot++;
    }

    /// <summary>Gets or sets the animation to play for the icon being shown, when the icon currently being used is
    /// changed.</summary>
    public SpannableAnimator? ShowIconAnimation { get; set; }

    /// <summary>Gets or sets the animation to play for the icon being hidden, when the icon currently being used is
    /// changed.</summary>
    public SpannableAnimator? HideIconAnimation { get; set; }

    /// <summary>Gets or sets the icon to use when checked.</summary>
    public ISpannable? TrueIcon { get; set; }

    /// <summary>Gets or sets the icon to use when not checked.</summary>
    public ISpannable? FalseIcon { get; set; }

    /// <summary>Gets or sets the icon to use when indeterminate.</summary>
    public ISpannable? NullIcon { get; set; }

    /// <summary>Gets or sets the background.</summary>
    public ISpannable? Background { get; set; }

    /// <summary>Gets or sets a value indicating whether it is checked.</summary>
    public bool? State
    {
        get => this.state;
        set
        {
            if (this.state == value)
                return;

            this.disappearingState = this.state;
            this.state = value;
            this.ShowIconAnimation?.Restart();
            this.HideIconAnimation?.Restart();
        }
    }

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new CheckmarkRenderPass(this);

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class CheckmarkRenderPass(TristateIconPattern owner) : PatternRenderPass
    {
        private ISpannable? activeState;
        private ISpannable? disappearingState;
        private ISpannableRenderPass? backgroundRenderPass;
        private ISpannableRenderPass? activeStateRenderPass;
        private ISpannableRenderPass? disappearingStateRenderPass;

        public override void OnReturnState()
        {
            base.OnReturnState();

            owner.Background?.ReturnRenderPass(this.backgroundRenderPass);
            this.backgroundRenderPass = null;

            this.activeState?.ReturnRenderPass(this.activeStateRenderPass);
            this.activeStateRenderPass = null;

            this.disappearingState?.ReturnRenderPass(this.disappearingStateRenderPass);
            this.disappearingStateRenderPass = null;
        }

        public override void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            base.MeasureSpannable(in args);

            this.activeState = owner.State switch
            {
                null => owner.NullIcon,
                true => owner.TrueIcon,
                false => owner.FalseIcon,
            };

            this.backgroundRenderPass = owner.Background?.RentRenderPass(args.RenderPass.Renderer);
            if (this.backgroundRenderPass is not null)
            {
                args.NotifyChild(
                    owner.Background!,
                    this.backgroundRenderPass,
                    owner.backgroundInnerId,
                    args.MinSize,
                    this.Boundary.Size);
            }

            if (owner.HideIconAnimation?.IsRunning is true)
            {
                owner.HideIconAnimation.Update(this.Boundary);
                this.disappearingState = owner.disappearingState switch
                {
                    null => owner.NullIcon,
                    true => owner.TrueIcon,
                    false => owner.FalseIcon,
                };

                this.disappearingStateRenderPass = this.disappearingState?.RentRenderPass(args.RenderPass.Renderer);
                if (this.disappearingStateRenderPass is not null)
                {
                    args.NotifyChild(
                        this.disappearingState!,
                        this.disappearingStateRenderPass,
                        -1,
                        args.MinSize,
                        this.Boundary.Size);
                }
            }

            owner.ShowIconAnimation?.Update(this.Boundary);

            this.activeStateRenderPass = this.activeState?.RentRenderPass(args.RenderPass.Renderer);
            if (this.activeStateRenderPass is not null)
            {
                args.NotifyChild(
                    this.activeState!,
                    this.activeStateRenderPass,
                    owner.foregroundInnerId,
                    args.MinSize,
                    this.Boundary.Size);
            }
        }

        public override void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            base.CommitSpannableMeasurement(in args);

            if (this.backgroundRenderPass is not null)
                args.NotifyChild(owner.Background!, this.backgroundRenderPass, Vector2.Zero, Matrix4x4.Identity);

            if (this.disappearingStateRenderPass is not null)
            {
                args.NotifyChild(
                    this.disappearingState!,
                    this.disappearingStateRenderPass,
                    Vector2.Zero,
                    owner.HideIconAnimation?.IsRunning is true
                        ? owner.HideIconAnimation.AnimatedTransformation
                        : Matrix4x4.Identity);
            }

            if (this.activeStateRenderPass is not null)
            {
                args.NotifyChild(
                    this.activeState!,
                    this.activeStateRenderPass,
                    Vector2.Zero,
                    owner.ShowIconAnimation?.IsRunning is true
                        ? owner.ShowIconAnimation.AnimatedTransformation
                        : Matrix4x4.Identity);
            }
        }

        public override void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args,
            out SpannableLinkInteracted link)
        {
            base.HandleSpannableInteraction(in args, out link);

            if (this.backgroundRenderPass is not null)
            {
                if (link.IsEmpty)
                    args.NotifyChild(owner.Background!, this.backgroundRenderPass, out link);
                else
                    args.NotifyChild(owner.Background!, this.backgroundRenderPass, out _);
            }

            if (this.activeStateRenderPass is not null)
            {
                if (link.IsEmpty)
                    args.NotifyChild(this.activeState!, this.activeStateRenderPass, out link);
                else
                    args.NotifyChild(this.activeState!, this.activeStateRenderPass, out _);
            }
        }

        protected override void DrawUntransformed(SpannableDrawArgs args)
        {
            if (this.backgroundRenderPass is not null)
                args.NotifyChild(owner.Background!, this.backgroundRenderPass);

            if (this.disappearingStateRenderPass is not null)
            {
                using var transformer = new ScopedTransformer(
                    Matrix4x4.Identity,
                    args.DrawListPtr,
                    owner.HideIconAnimation?.IsRunning is true ? owner.HideIconAnimation.AnimatedOpacity : 0f);
                args.NotifyChild(this.disappearingState!, this.disappearingStateRenderPass);
            }

            if (this.activeStateRenderPass is not null)
            {
                using var transformer = new ScopedTransformer(
                    Matrix4x4.Identity,
                    args.DrawListPtr,
                    owner.ShowIconAnimation?.IsRunning is true ? owner.ShowIconAnimation.AnimatedOpacity : 1f);
                args.NotifyChild(this.activeState!, this.activeStateRenderPass);
            }
        }
    }
}
