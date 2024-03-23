using System.Collections.Generic;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.Labels;

/// <summary>A tristate control that defaults to a radio theme.</summary>
public class RadioControl : TristateControl
{
    private HashSet<RadioControl>? bindGroup;

    /// <summary>Initializes a new instance of the <see cref="RadioControl"/> class.</summary>
    public RadioControl()
    {
        this.CaptureMouseOnMouseDown = true;

        const float checkSize = 22;
        const float gap = 4;
        const float checkMargin = 4;
        const float circleMargin = 6;
        this.NormalIcon = new()
        {
            Size = new(checkSize + gap, checkSize),
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

        if (this.Checked is true)
        {
            foreach (var x in this.bindGroup)
                x.Checked = false;
        }

        this.bindGroup.Add(this);
        return this;
    }

    /// <inheritdoc/>
    protected override void OnCheckedChange(PropertyChangeEventArgs<ControlSpannable, bool?> args)
    {
        if (this.bindGroup is not null)
        {
            if (args.NewValue is true)
            {
                foreach (var x in this.bindGroup)
                    x.Checked = x == this;
            }
            else if (args is { NewValue: false, PreviousValue: true })
            {
                var anyTrue = false;
                foreach (var x in this.bindGroup)
                    anyTrue |= x.Checked is true;
                this.Checked = !anyTrue;
            }
        }

        base.OnCheckedChange(args);
    }
}
