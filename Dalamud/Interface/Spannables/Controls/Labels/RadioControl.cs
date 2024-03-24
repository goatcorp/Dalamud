using System.Collections.Generic;

using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tristate control that defaults to a radio theme.</summary>
public class RadioControl : BooleanControl
{
    private HashSet<RadioControl>? bindGroup;

    /// <summary>Initializes a new instance of the <see cref="RadioControl"/> class.</summary>
    public RadioControl()
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
        this.TextMargin = new(4);
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
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.FrameBg,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
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
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.FrameBgHovered,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
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
                Type = ShapePattern.Shape.CircleFilled,
                ImGuiColor = ImGuiCol.FrameBgActive,
                Rounding = 4,
                Margin = new(0, 0, 0, 0),
            },
            TrueIcon = new ShapePattern
            {
                Type = ShapePattern.Shape.CircleFilled,
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

    /// <summary>Binds this radio control with <paramref name="other"/>.</summary>
    /// <param name="other">The other radio control.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    public RadioControl WithBound(RadioControl other)
    {
        if (other is null)
            throw new NullReferenceException();
        if (other.bindGroup is not null)
        {
            if (this.bindGroup is not null)
            {
                other.bindGroup.UnionWith(this.bindGroup);
                foreach (var x in this.bindGroup)
                    x.bindGroup = other.bindGroup;
            }
            else
            {
                this.bindGroup = other.bindGroup;
            }
        }
        else
        {
            if (this.bindGroup is not null)
                other.bindGroup = this.bindGroup;
            else
                this.bindGroup = other.bindGroup = new();
            this.bindGroup.Add(other);
        }

        if (this.Checked)
        {
            foreach (var x in this.bindGroup)
                x.Checked = false;
        }

        this.bindGroup.Add(this);
        return this;
    }

    /// <inheritdoc/>
    protected override void OnMouseClick(ControlMouseEventArgs args)
    {
        if (!this.Indeterminate && !this.Checked)
        {
            if (this.bindGroup is null)
            {
                this.Checked = true;
            }
            else
            {
                var anyIndeterminate = false;
                foreach (var x in this.bindGroup)
                    anyIndeterminate |= x.Indeterminate;
                if (!anyIndeterminate)
                {
                    foreach (var x in this.bindGroup)
                        x.Checked = x == this;
                }
            }
        }

        base.OnMouseClick(args);
    }

    /// <inheritdoc/>
    protected override void OnCheckedChange(PropertyChangeEventArgs<ControlSpannable, bool> args)
    {
        if (!this.Indeterminate && this.Checked)
        {
            if (this.bindGroup is null)
            {
                this.Checked = true;
            }
            else
            {
                var anyIndeterminate = false;
                foreach (var x in this.bindGroup)
                    anyIndeterminate |= x.Indeterminate;
                if (!anyIndeterminate)
                {
                    foreach (var x in this.bindGroup)
                        x.Checked = x == this;
                }
            }
        }

        base.OnCheckedChange(args);
    }
}
