using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tristate control that defaults to a checkbox theme.</summary>
public class CheckboxControl : TristateControl
{
    /// <summary>Initializes a new instance of the <see cref="CheckboxControl"/> class.</summary>
    public CheckboxControl()
    {
        this.CaptureMouseOnMouseDown = true;

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

        // SpannableSizeAnimator? hideAnimation = null, showAnimation = null;

        const float checkSize = 22;
        const float gap = 4;
        const float checkMargin = 4;
        const float circleMargin = 6;
        this.NormalIcon = new()
        {
            Size = new(checkSize + gap, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.FrameBg,
                Rounding = 4,
                Margin = new(0, 0, gap, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new RectVector4(checkMargin) + new RectVector4(0, 0, gap, 0),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new RectVector4(circleMargin) + new RectVector4(0, 0, gap, 0),
            },
        };
        this.HoveredIcon = new()
        {
            Size = new(checkSize + gap, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.FrameBgHovered,
                Rounding = 4,
                Margin = new(0, 0, gap, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new RectVector4(checkMargin) + new RectVector4(0, 0, gap, 0),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new RectVector4(circleMargin) + new RectVector4(0, 0, gap, 0),
            },
        };
        this.ActiveIcon = new()
        {
            Size = new(checkSize + gap, checkSize),
            ShowIconAnimation = showAnimation,
            HideIconAnimation = hideAnimation,
            Background = new ShapePattern
            {
                Type = ShapePattern.Shape.RectFilled,
                ImGuiColor = ImGuiCol.FrameBgActive,
                Rounding = 4,
                Margin = new(0, 0, gap, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.Checkmark,
                ImGuiColor = ImGuiCol.CheckMark,
                Margin = new RectVector4(checkMargin) + new RectVector4(0, 0, gap, 0),
            },
            NullIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.CheckMark,
                ColorMultiplier = new(1, 1, 1, 0.6f),
                Margin = new RectVector4(circleMargin) + new RectVector4(0, 0, gap, 0),
            },
        };
        this.Margin = new(gap);
    }
}
