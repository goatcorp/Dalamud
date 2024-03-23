using System.Numerics;

using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Animations;

/// <summary>An animator that animates the size of a control.</summary>
public sealed class SpannableSizeAnimator : SpannableAnimator
{
    /// <summary>Gets or sets the size ratio at the beginning of the animation.</summary>
    public RectVector4 BeforeRatio { get; set; } = new(-1, -1, 1, 1);

    /// <summary>Gets or sets the size ratio at the end of the animation.</summary>
    public RectVector4 AfterRatio { get; set; } = new(-1, -1, 1, 1);

    /// <inheritdoc/>
    protected override void CalculateMatrix(float p, in RectVector4 box, out Matrix4x4 result)
    {
        var lt = Vector2.Lerp(this.BeforeRatio.LeftTop, this.AfterRatio.LeftTop, p);
        var rb = Vector2.Lerp(this.BeforeRatio.RightBottom, this.AfterRatio.RightBottom, p);
        var scale = new Vector2((rb.X - lt.X) / 2, (rb.Y - lt.Y) / 2);

        var center = (box.LeftTop + box.RightBottom) / 2;
        result = Matrix4x4.CreateTranslation(new(-center, 0));
        result = Matrix4x4.Multiply(result, Matrix4x4.CreateScale(new Vector3(scale, 1)));
        var translation = (box.Size * (lt + rb)) + center;
        result = Matrix4x4.Multiply(result, Matrix4x4.CreateTranslation(new(translation, 0)));
    }
}
