using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tristate control that defaults to a checkbox theme.</summary>
public class CheckboxControl : BooleanControl
{
    /// <summary>Initializes a new instance of the <see cref="CheckboxControl"/> class.</summary>
    public CheckboxControl()
    {
        var animationDuration = TimeSpan.FromMilliseconds(200);
        var showAnimation = new SpannableSizeAnimator
        {
            BeforeRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
            BeforeOpacity = 0f,
            TransformationEasing = new OutCubic(animationDuration),
            OpacityEasing = new OutCubic(animationDuration),
        };

        var hideAnimation = new SpannableSizeAnimator
        {
            AfterRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
            AfterOpacity = 0f,
            TransformationEasing = new InCubic(animationDuration),
            OpacityEasing = new InCubic(animationDuration),
        };

        const float checkSize = 22;
        const float checkmarkMargin = 4;
        const float circleMargin = 6;
        this.CaptureMouseOnMouseDown = true;
        this.Focusable = true;
        this.TextMargin = new(4);
        this.Alignment = new(0, 0.5f);
        this.Padding = new(4);
        this.NormalIcon = new()
        {
            Size = new(checkSize, checkSize),
            MinSize = new(checkSize / 1.5f, checkSize / 1.5f),
            MaxSize = new(checkSize, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.SquareFilled,
                ImGuiColor = ImGuiCol.FrameBg,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new(checkmarkMargin),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new(circleMargin),
            },
        };
        this.HoveredIcon = new()
        {
            Size = new(checkSize, checkSize),
            MinSize = new(checkSize / 1.5f, checkSize / 1.5f),
            MaxSize = new(checkSize, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.SquareFilled,
                ImGuiColor = ImGuiCol.FrameBgHovered,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new(checkmarkMargin),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new(circleMargin),
            },
        };
        this.ActiveIcon = new()
        {
            Size = new(checkSize, checkSize),
            MinSize = new(checkSize / 1.5f, checkSize / 1.5f),
            MaxSize = new(checkSize, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.SquareFilled,
                ImGuiColor = ImGuiCol.FrameBgActive,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new(checkmarkMargin),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new(circleMargin),
            },
        };
    }

    /// <inheritdoc/>
    protected override void OnClick(SpannableControlEventArgs args)
    {
        if (!this.Indeterminate)
            this.Checked = !this.Checked;

        base.OnClick(args);
    }
}
