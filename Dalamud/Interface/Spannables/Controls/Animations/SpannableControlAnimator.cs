using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Animations;

/// <summary>Animator for <see cref="SpannableControl"/>.</summary>
public abstract class SpannableControlAnimator
{
    private Matrix4x4 transformation = Matrix4x4.Identity;

    /// <summary>Gets or sets the easing function to use.</summary>
    public Easing? TransformationEasing { get; set; }

    /// <summary>Gets or sets the easing function to use.</summary>
    public Easing? OpacityEasing { get; set; }

    /// <summary>Gets or sets the box that will be used for the purpose of calculating the animation.</summary>
    public SpannableControlBox AnimateBox { get; set; } = SpannableControlBox.Interactive;

    /// <summary>Gets or sets the opacity at the beginning of the animation.</summary>
    public float BeforeOpacity { get; set; } = 1f;

    /// <summary>Gets or sets the opacity at the end of the animation.</summary>
    public float AfterOpacity { get; set; } = 1f;

    /// <summary>Gets or sets the opacity being animated.</summary>
    public float Opacity { get; protected set; } = 1f;

    /// <inheritdoc cref="Animation.Easing.IsRunning"/>
    public bool IsRunning
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.OpacityEasing?.IsRunning is true || this.TransformationEasing?.IsRunning is true;
    }

    /// <summary>Gets the transformation matrix for the animation.</summary>
    public ref readonly Matrix4x4 Transformation
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
    /// <param name="control">The control being animated.</param>
    public void Update(SpannableControl control)
    {
        if (this.TransformationEasing?.IsRunning is true)
        {
            this.TransformationEasing.Update();
            switch (this.AnimateBox)
            {
                case SpannableControlBox.Extruded:
                    this.CalculateMatrix(
                        (float)this.TransformationEasing.Value,
                        control,
                        control.MeasuredExtrudedBox,
                        out this.transformation);
                    break;
                case SpannableControlBox.Boundary:
                    this.CalculateMatrix(
                        (float)this.TransformationEasing.Value,
                        control,
                        control.Boundary,
                        out this.transformation);
                    break;
                case SpannableControlBox.Interactive:
                default:
                    this.CalculateMatrix(
                        (float)this.TransformationEasing.Value,
                        control,
                        control.MeasuredInteractiveBox,
                        out this.transformation);
                    break;
                case SpannableControlBox.Content:
                    this.CalculateMatrix(
                        (float)this.TransformationEasing.Value,
                        control,
                        control.MeasuredContentBox,
                        out this.transformation);
                    break;
            }
            
            if (this.TransformationEasing.IsDone)
                this.TransformationEasing.Stop();
        }

        if (this.OpacityEasing?.IsRunning is true)
        {
            this.OpacityEasing.Update();
            if (this.OpacityEasing.IsDone)
            {
                this.OpacityEasing.Stop();
                this.Opacity = this.AfterOpacity;
            }
            else
            {
                this.Opacity =
                    float.Lerp(
                        this.BeforeOpacity,
                        this.AfterOpacity,
                        Math.Clamp((float)this.OpacityEasing.Value, 0f, 1f));
            }
        }
    }

    /// <summary>Calculates the transformation matrix, given a progress value during animation.</summary>
    /// <param name="p">The progress value.</param>
    /// <param name="control">The control being animated.</param>
    /// <param name="box">The box to use for calculating animation.</param>
    /// <param name="result">The calculated matrix.</param>
    protected abstract void CalculateMatrix(
        float p,
        SpannableControl control,
        in RectVector4 box,
        out Matrix4x4 result);
}
