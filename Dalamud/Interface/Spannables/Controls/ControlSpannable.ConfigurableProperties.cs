using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class ControlSpannable
{
    private string name = string.Empty;
    private bool clipChildren;
    private string? text;

    private TextStyle textStyle = new()
    {
        ForeColor = 0xFFFFFFFF,
        TextDecorationColor = 0xFFFFFFFF,
        TextDecorationThickness = 1 / 16f,
        VerticalAlignment = -1,
    };

    private float scale = 1f;
    private Vector2 size = new(WrapContent);
    private BorderVector4 extendOutside = BorderVector4.Zero;
    private BorderVector4 margin = BorderVector4.Zero;
    private BorderVector4 padding = BorderVector4.Zero;
    private Matrix4x4 transformation = Matrix4x4.Identity;
    private ISpannableTemplate? normalBackground;
    private ISpannableTemplate? hoveredBackground;
    private ISpannableTemplate? activeBackground;
    private ISpannableTemplate? disabledBackground;
    private SpannableAnimator? showAnimation;
    private SpannableAnimator? hideAnimation;
    private SpannableAnimator? moveAnimation;
    private SpannableAnimator? transformationChangeAnimation;
    private float disabledTextOpacity = 0.5f;

    /// <summary>Gets or sets a name, for internal identification purpose.</summary>
    public string Name
    {
        get => this.name;
        set
        {
            if (value is null)
                throw new NullReferenceException();
            this.HandlePropertyChange(
                nameof(this.Name),
                ref this.name,
                value,
                string.Equals(this.name, value, StringComparison.Ordinal),
                this.OnNameChange);
        }
    }

    /// <summary>Gets or sets a value indicating whether to clip the children.</summary>
    public bool ClipChildren
    {
        get => this.clipChildren;
        set => this.HandlePropertyChange(
            nameof(this.ClipChildren),
            ref this.clipChildren,
            value,
            this.clipChildren == value,
            this.OnClipChildrenChange);
    }

    /// <summary>Gets or sets a text.</summary>
    /// <remarks>Default implementation does nothing with it, else than to display it in <see cref="ToString"/>.
    /// </remarks>
    public string? Text
    {
        get => this.text;
        set => this.HandlePropertyChange(
            nameof(this.Text),
            ref this.text,
            value,
            string.Equals(this.text, value, StringComparison.Ordinal),
            this.OnTextChange);
    }

    /// <summary>Gets or sets the text state options.</summary>
    public TextStyle TextStyle
    {
        get => this.textStyle;
        set => this.HandlePropertyChange(
            nameof(this.TextStyle),
            ref this.textStyle,
            value,
            TextStyle.PropertyReferenceEquals(this.textStyle, value),
            this.OnTextStyleChange);
    }

    /// <summary>Gets or sets the scale, applicable for this and all the descendant spannables.</summary>
    /// <remarks>Effective scale is <see cref="EffectiveRenderScale"/>, which takes this and the render scale specified from
    /// <see cref="RenderContext.RenderScale"/> into consideration.</remarks>
    public float Scale
    {
        get => this.scale;
        set => this.HandlePropertyChange(
            nameof(this.Scale),
            ref this.scale,
            value,
            this.scale - value == 0f,
            this.OnScaleChange);
    }

    /// <summary>Gets or sets the size.</summary>
    /// <remarks>
    /// <para><see cref="MatchParent"/> and <see cref="WrapContent"/> can be used.</para>
    /// <para>The value will be scaled by <see cref="EffectiveRenderScale"/>.</para>
    /// <para>The value includes the margin and padding.</para>
    /// </remarks>
    public Vector2 Size
    {
        get => this.size;
        set => this.HandlePropertyChange(
            nameof(this.Size),
            ref this.size,
            value,
            this.size == value,
            this.OnSizeChange);
    }

    /// <summary>Gets or sets the extrusion.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 ExtendOutside
    {
        get => this.extendOutside;
        set => this.HandlePropertyChange(
            nameof(this.ExtendOutside),
            ref this.extendOutside,
            value,
            this.extendOutside == value,
            this.OnExtendOutsideChange);
    }

    /// <summary>Gets or sets the margin.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 Margin
    {
        get => this.margin;
        set => this.HandlePropertyChange(
            nameof(this.Margin),
            ref this.margin,
            value,
            this.margin == value,
            this.OnMarginChange);
    }

    /// <summary>Gets or sets the padding.</summary>
    /// <remarks>The value will be scaled by <see cref="EffectiveRenderScale"/>.</remarks>
    public BorderVector4 Padding
    {
        get => this.padding;
        set => this.HandlePropertyChange(
            nameof(this.Padding),
            ref this.padding,
            value,
            this.padding == value,
            this.OnPaddingChange);
    }

    /// <summary>Gets or sets the transformation.</summary>
    /// <remarks>This value does not count when calculating <see cref="EffectiveRenderScale"/>.</remarks>
    public Matrix4x4 Transformation
    {
        get => this.transformation;
        set => this.HandlePropertyChange(
            nameof(this.Transformation),
            ref this.transformation,
            value,
            this.transformation == value,
            this.OnTransformationChange);
    }

    /// <summary>Gets or sets the normal background spannable.</summary>
    public ISpannableTemplate? NormalBackground
    {
        get => this.normalBackground;
        set => this.HandlePropertyChange(
            nameof(this.NormalBackground),
            ref this.normalBackground,
            value,
            ReferenceEquals(this.normalBackground, value),
            this.OnNormalBackgroundChange);
    }

    /// <summary>Gets or sets the hovered background spannable.</summary>
    public ISpannableTemplate? HoveredBackground
    {
        get => this.hoveredBackground;
        set => this.HandlePropertyChange(
            nameof(this.HoveredBackground),
            ref this.hoveredBackground,
            value,
            ReferenceEquals(this.hoveredBackground, value),
            this.OnHoveredBackgroundChange);
    }

    /// <summary>Gets or sets the active background spannable.</summary>
    public ISpannableTemplate? ActiveBackground
    {
        get => this.activeBackground;
        set => this.HandlePropertyChange(
            nameof(this.ActiveBackground),
            ref this.activeBackground,
            value,
            ReferenceEquals(this.activeBackground, value),
            this.OnActiveBackgroundChange);
    }

    /// <summary>Gets or sets the disabled background spannable.</summary>
    public ISpannableTemplate? DisabledBackground
    {
        get => this.disabledBackground;
        set => this.HandlePropertyChange(
            nameof(this.DisabledBackground),
            ref this.disabledBackground,
            value,
            ReferenceEquals(this.disabledBackground, value),
            this.OnDisabledBackgroundChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Spannable.Visible"/> changes to
    /// <c>true</c>.</summary>
    public SpannableAnimator? ShowAnimation
    {
        get => this.showAnimation;
        set => this.HandlePropertyChange(
            nameof(this.ShowAnimation),
            ref this.showAnimation,
            value,
            this.showAnimation == value,
            this.OnShowAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Spannable.Visible"/> changes to
    /// <c>false</c>.</summary>
    public SpannableAnimator? HideAnimation
    {
        get => this.hideAnimation;
        set => this.HandlePropertyChange(
            nameof(this.HideAnimation),
            ref this.hideAnimation,
            value,
            this.hideAnimation == value,
            this.OnHideAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when the control effectively moves for any reason, with respect to
    /// its parent.</summary>
    public SpannableAnimator? MoveAnimation
    {
        get => this.moveAnimation;
        set => this.HandlePropertyChange(
            nameof(this.MoveAnimation),
            ref this.moveAnimation,
            value,
            this.moveAnimation == value,
            this.OnMoveAnimationChange);
    }

    /// <summary>Gets or sets the animation to play when <see cref="Transformation"/> changes.</summary>
    public SpannableAnimator? TransformationChangeAnimation
    {
        get => this.transformationChangeAnimation;
        set => this.HandlePropertyChange(
            nameof(this.TransformationChangeAnimation),
            ref this.transformationChangeAnimation,
            value,
            this.transformationChangeAnimation == value,
            this.OnTransformationChangeAnimationChange);
    }

    /// <summary>Gets or sets the opacity of the body when the control is disabled.</summary>
    public float DisabledTextOpacity
    {
        get => this.disabledTextOpacity;
        set => this.HandlePropertyChange(
            nameof(this.DisabledTextOpacity),
            ref this.disabledTextOpacity,
            value,
            this.disabledTextOpacity - value == 0f,
            this.OnDisabledTextOpacityChange);
    }
}
