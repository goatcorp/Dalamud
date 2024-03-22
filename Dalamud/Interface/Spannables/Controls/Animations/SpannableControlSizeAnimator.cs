using System.Numerics;

using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Animations;

/// <summary>An animator that animates the size of a control.</summary>
public sealed class SpannableControlSizeAnimator : SpannableControlAnimator
{
    /// <summary>Gets or sets the size ratio at the beginning of the animation.</summary>
    public RectVector4 BeforeRatio { get; set; } = new(-1, -1, 1, 1);

    /// <summary>Gets or sets the size ratio at the end of the animation.</summary>
    public RectVector4 AfterRatio { get; set; } = new(-1, -1, 1, 1);

    /// <inheritdoc/>
    protected override void CalculateMatrix(float p, SpannableControl control, in RectVector4 box, out Matrix4x4 result)
    {
        var lt = Vector2.Lerp(this.BeforeRatio.LeftTop, this.AfterRatio.LeftTop, p);
        var rb = Vector2.Lerp(this.BeforeRatio.RightBottom, this.AfterRatio.RightBottom, p);
        var scale = new Vector3((rb.X - lt.X) / 2, (rb.Y - lt.Y) / 2, 1);
        result = Matrix4x4.CreateScale(scale);

        var translation = new Vector3(box.Size / 2, 0) * (Vector3.One - scale);
        translation += new Vector3(lt + rb, 0);
        result = Matrix4x4.Multiply(result, Matrix4x4.CreateTranslation(translation));
    }
}

/// <summary>A box of a <see cref="SpannableControl"/>.</summary>
public enum SpannableControlBox
{
    /// <summary>Use the extruded box.</summary>
    Extruded,

    /// <summary>Use the layout boundary box.</summary>
    Boundary,

    /// <summary>Use the interactive box.</summary>
    Interactive,

    /// <summary>Use the content box.</summary>
    Content,
}
