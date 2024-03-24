using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Animation;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Animations;

/// <summary>Animator for <see cref="ISpannable"/>.</summary>
public abstract class SpannableAnimator
{
    private Matrix4x4 beforeMatrix = Matrix4x4.Identity;
    private Matrix4x4 afterMatrix = Matrix4x4.Identity;
    private Matrix4x4 transformation = Matrix4x4.Identity;
    private BorderVector4 boundaryAdjustment = BorderVector4.Zero;

    /// <summary>Gets or sets the easing function to use.</summary>
    public Easing? TransformationEasing { get; set; }

    /// <summary>Gets or sets the easing function to use.</summary>
    public Easing? OpacityEasing { get; set; }

    /// <summary>Gets or sets the box that will be used for the purpose of calculating the animation.</summary>
    public ControlSpannableBox AnimateBox { get; set; } = ControlSpannableBox.Boundary;

    /// <summary>Gets or sets the opacity at the beginning of the animation.</summary>
    public float BeforeOpacity { get; set; } = 1f;

    /// <summary>Gets or sets the opacity at the end of the animation.</summary>
    public float AfterOpacity { get; set; } = 1f;

    /// <summary>Gets or sets the opacity being animated.</summary>
    public float AnimatedOpacity { get; protected set; } = 1f;

    /// <summary>Gets the mutable reference of transformation matrix at the beginning of the animation.</summary>
    public ref Matrix4x4 BeforeMatrix => ref this.beforeMatrix;
    
    /// <summary>Gets the mutable reference of transformation matrix at the end of the animation.</summary>
    public ref Matrix4x4 AfterMatrix => ref this.afterMatrix;

    /// <summary>Gets the boundary adjustment.</summary>
    /// <remarks>Each component of the returned value should be added to all boxes, normalizing the addition results
    /// as needed.</remarks>
    // TODO: is this useful?
    public ref readonly BorderVector4 AnimatedBoundaryAdjustment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this.boundaryAdjustment;
    }

    /// <inheritdoc cref="Animation.Easing.IsRunning"/>
    public bool IsRunning
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.OpacityEasing?.IsRunning is true || this.TransformationEasing?.IsRunning is true;
    }

    /// <summary>Gets the transformation matrix for the animation.</summary>
    public ref readonly Matrix4x4 AnimatedTransformation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this.transformation;
    }

    /// <inheritdoc cref="Animation.Easing.Start"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start()
    {
        this.TransformationEasing?.Start();
        this.OpacityEasing?.Start();
    }

    /// <inheritdoc cref="Animation.Easing.Stop"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Stop()
    {
        this.TransformationEasing?.Stop();
        this.OpacityEasing?.Stop();
    }

    /// <inheritdoc cref="Animation.Easing.Restart"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Restart()
    {
        this.TransformationEasing?.Restart();
        this.OpacityEasing?.Restart();
    }

    /// <inheritdoc cref="Animation.Easing.Reset"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        this.TransformationEasing?.Reset();
        this.OpacityEasing?.Reset();
    }

    /// <summary>Updates the animation.</summary>
    /// <param name="control">Control being animated.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(ControlSpannable control)
    {
        switch (this.AnimateBox)
        {
            case ControlSpannableBox.Extruded:
                this.Update(control.MeasuredExtrudedBox);
                break;
            case ControlSpannableBox.Boundary:
                this.Update(control.Boundary);
                break;
            case ControlSpannableBox.Interactive:
            default:
                this.Update(control.MeasuredInteractiveBox);
                break;
            case ControlSpannableBox.Content:
                this.Update(control.MeasuredContentBox);
                break;
        }
    }

    /// <summary>Updates the animation.</summary>
    /// <param name="renderPass">Render pass of the spannable being animated.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(ISpannableRenderPass renderPass) => this.Update(renderPass.Boundary);

    /// <summary>Updates the animation.</summary>
    /// <param name="boundary">Boundary to animate.</param>
    public void Update(scoped in RectVector4 boundary)
    {
        if (this.TransformationEasing?.IsRunning is true)
        {
            this.TransformationEasing.Update();
            this.CalculateMatrix((float)this.TransformationEasing.Value, boundary, out this.transformation);
            if (this.TransformationEasing.IsDone)
                this.TransformationEasing.Stop();
        }

        if (this.OpacityEasing?.IsRunning is true)
        {
            this.OpacityEasing.Update();
            if (this.OpacityEasing.IsDone)
            {
                this.OpacityEasing.Stop();
                this.AnimatedOpacity = this.AfterOpacity;
            }
            else
            {
                this.AnimatedOpacity =
                    float.Lerp(
                        this.BeforeOpacity,
                        this.AfterOpacity,
                        Math.Clamp((float)this.OpacityEasing.Value, 0f, 1f));
            }
        }
    }

    /// <summary>Calculates the transformation matrix, given a progress value during animation.</summary>
    /// <param name="p">The progress value.</param>
    /// <param name="box">The box to use for calculating animation.</param>
    /// <param name="result">The calculated matrix.</param>
    protected abstract void CalculateMatrix(
        float p,
        scoped in RectVector4 box,
        out Matrix4x4 result);
}
