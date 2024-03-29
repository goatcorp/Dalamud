using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Helpers;

using ImGuiNET;

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
    protected override PatternSpannableMeasurement CreateNewRenderPass() =>
        new TristateIconPatternMeasurement(this, new());

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class TristateIconPatternMeasurement(TristateIconPattern owner, SpannableMeasurementOptions options)
        : PatternSpannableMeasurement(owner, options)
    {
        private ISpannable? activeState;
        private ISpannable? disappearingState;
        private ISpannableMeasurement? backgroundMeasurement;
        private ISpannableMeasurement? activeStateMeasurement;
        private ISpannableMeasurement? disappearingStateRenderPass;

        public override void OnReturnMeasurement()
        {
            base.OnReturnMeasurement();

            owner.Background?.ReturnMeasurement(this.backgroundMeasurement);
            this.backgroundMeasurement = null;

            this.activeState?.ReturnMeasurement(this.activeStateMeasurement);
            this.activeStateMeasurement = null;

            this.disappearingState?.ReturnMeasurement(this.disappearingStateRenderPass);
            this.disappearingStateRenderPass = null;
        }

        public override bool HandleInteraction()
        {
            this.backgroundMeasurement?.HandleInteraction();
            this.activeStateMeasurement?.HandleInteraction();
            return base.HandleInteraction();
        }

        public override bool Measure()
        {
            var changed = base.Measure();

            this.activeState = owner.State switch
            {
                null => owner.NullIcon,
                true => owner.TrueIcon,
                false => owner.FalseIcon,
            };

            this.backgroundMeasurement = owner.Background?.RentMeasurement(this.Renderer);
            if (this.backgroundMeasurement is not null)
            {
                this.backgroundMeasurement.RenderScale = this.RenderScale;
                this.backgroundMeasurement.Options.Size = this.Boundary.Size;
                this.backgroundMeasurement.Options.VisibleSize = this.Boundary.Size;
                this.backgroundMeasurement.ImGuiGlobalId = this.GetGlobalIdFromInnerId(owner.backgroundInnerId);
                changed |= this.backgroundMeasurement.Measure();
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

                this.disappearingStateRenderPass = this.disappearingState?.RentMeasurement(this.Renderer);
                if (this.disappearingStateRenderPass is not null)
                {
                    this.disappearingStateRenderPass.RenderScale = this.RenderScale;
                    this.disappearingStateRenderPass.Options.Size = this.Boundary.Size;
                    this.disappearingStateRenderPass.Options.VisibleSize = this.Boundary.Size;
                    changed |= this.disappearingStateRenderPass.Measure();
                }
            }

            owner.ShowIconAnimation?.Update(this.Boundary);

            this.activeStateMeasurement = this.activeState?.RentMeasurement(this.Renderer);
            if (this.activeStateMeasurement is not null)
            {
                this.activeStateMeasurement.RenderScale = this.RenderScale;
                this.activeStateMeasurement.Options.Size = this.Boundary.Size;
                this.activeStateMeasurement.Options.VisibleSize = this.Boundary.Size;
                this.activeStateMeasurement.ImGuiGlobalId = this.GetGlobalIdFromInnerId(owner.foregroundInnerId);
                changed |= this.activeStateMeasurement.Measure();
            }

            return changed;
        }

        public override void UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
        {
            base.UpdateTransformation(in local, in ancestral);

            this.backgroundMeasurement?.UpdateTransformation(Matrix4x4.Identity, this.FullTransformation);

            this.disappearingStateRenderPass?.UpdateTransformation(
                owner.HideIconAnimation?.IsRunning is true
                    ? owner.HideIconAnimation.AnimatedTransformation
                    : Matrix4x4.Identity,
                this.FullTransformation);

            this.activeStateMeasurement?.UpdateTransformation(
                owner.ShowIconAnimation?.IsRunning is true
                    ? owner.ShowIconAnimation.AnimatedTransformation
                    : Matrix4x4.Identity,
                this.FullTransformation);
        }

        public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
        {
            if (this.backgroundMeasurement is { } bm)
            {
                if (!Matrix4x4.Invert(bm.FullTransformation, out var inv)
                    && bm.Boundary.Contains(Vector2.Transform(screenOffset, inv)))
                    return bm;
            }

            if (this.activeStateMeasurement is { } asm)
            {
                if (!Matrix4x4.Invert(asm.FullTransformation, out var inv)
                    && asm.Boundary.Contains(Vector2.Transform(screenOffset, inv)))
                    return asm;
            }

            return base.FindChildMeasurementAt(screenOffset);
        }

        protected override void DrawUntransformed(ImDrawListPtr drawListPtr)
        {
            this.backgroundMeasurement?.Draw(drawListPtr);
            if (this.disappearingStateRenderPass is not null)
            {
                using var transformer = new ScopedTransformer(
                    drawListPtr,
                    Matrix4x4.Identity,
                    Vector2.One,
                    owner.HideIconAnimation?.IsRunning is true ? owner.HideIconAnimation.AnimatedOpacity : 0f);
                this.disappearingStateRenderPass.Draw(drawListPtr);
            }

            if (this.activeStateMeasurement is not null)
            {
                using var transformer = new ScopedTransformer(
                    drawListPtr,
                    Matrix4x4.Identity,
                    Vector2.One,
                    owner.ShowIconAnimation?.IsRunning is true ? owner.ShowIconAnimation.AnimatedOpacity : 1f);
                this.activeStateMeasurement.Draw(drawListPtr);
            }
        }
    }
}
