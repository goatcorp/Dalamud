using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Component Demo Window to view custom ImGui components.
/// </summary>
internal sealed class ComponentDemoWindow : Window
{
    private static readonly TimeSpan DefaultEasingTime = new(0, 0, 0, 1700);

    private readonly List<(string Name, Action Demo)> componentDemos;
    private readonly IReadOnlyList<Easing> easings = new Easing[]
    {
        new InSine(DefaultEasingTime), new OutSine(DefaultEasingTime), new InOutSine(DefaultEasingTime),
        new InCubic(DefaultEasingTime), new OutCubic(DefaultEasingTime), new InOutCubic(DefaultEasingTime),
        new InQuint(DefaultEasingTime), new OutQuint(DefaultEasingTime), new InOutQuint(DefaultEasingTime),
        new InCirc(DefaultEasingTime), new OutCirc(DefaultEasingTime), new InOutCirc(DefaultEasingTime),
        new InElastic(DefaultEasingTime), new OutElastic(DefaultEasingTime), new InOutElastic(DefaultEasingTime),
    };

    private int animationTimeMs = (int)DefaultEasingTime.TotalMilliseconds;
    private Vector4 defaultColor = ImGuiColors.DalamudOrange;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentDemoWindow"/> class.
    /// </summary>
    public ComponentDemoWindow()
        : base("Dalamud Components Demo")
    {
        this.Size = new Vector2(600, 500);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;

        this.componentDemos = new()
        {
            ("Test", ImGuiComponents.Test),
            ("HelpMarker", HelpMarkerDemo),
            ("IconButton", IconButtonDemo),
            ("TextWithLabel", TextWithLabelDemo),
            ("ColorPickerWithPalette", this.ColorPickerWithPaletteDemo),
        };
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        foreach (var easing in this.easings)
        {
            easing.Restart();
        }
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        foreach (var easing in this.easings)
        {
            easing.Stop();
        }
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        ImGui.Text("This is a collection of UI components you can use in your plugin.");

        for (var i = 0; i < this.componentDemos.Count; i++)
        {
            var componentDemo = this.componentDemos[i];

            if (ImGui.CollapsingHeader($"{componentDemo.Name}###comp{i}"))
            {
                componentDemo.Demo();
            }
        }

        if (ImGui.CollapsingHeader("Easing animations"))
        {
            this.EasingsDemo();
        }
    }

    private static void HelpMarkerDemo()
    {
        ImGui.Text("Hover over the icon to learn more.");
        ImGuiComponents.HelpMarker("help me!");
    }

    private static void IconButtonDemo()
    {
        ImGui.Text("Click on the icon to use as a button.");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(1, FontAwesomeIcon.Carrot))
        {
            ImGui.OpenPopup("IconButtonDemoPopup");
        }

        if (ImGui.BeginPopup("IconButtonDemoPopup"))
        {
            ImGui.Text("You clicked!");
            ImGui.EndPopup();
        }
    }

    private static void TextWithLabelDemo()
    {
        ImGuiComponents.TextWithLabel("Label", "Hover to see more", "more");
    }

    private void EasingsDemo()
    {
        ImGui.SliderInt("Speed in MS", ref this.animationTimeMs, 200, 5000);

        foreach (var easing in this.easings)
        {
            easing.Duration = new TimeSpan(0, 0, 0, 0, this.animationTimeMs);

            if (!easing.IsRunning)
            {
                easing.Start();
            }

            var cursor = ImGui.GetCursorPos();
            var p1 = new Vector2(cursor.X + 5, cursor.Y);
            var p2 = p1 + new Vector2(45, 0);
            easing.Point1 = p1;
            easing.Point2 = p2;
            easing.Update();

            if (easing.IsDone)
            {
                easing.Restart();
            }

            ImGui.SetCursorPos(easing.EasedPoint);
            ImGui.Bullet();

            ImGui.SetCursorPos(cursor + new Vector2(0, 10));
            ImGui.Text($"{easing.GetType().Name} ({easing.Value})");
            ImGuiHelpers.ScaledDummy(5);
        }
    }

    private void ColorPickerWithPaletteDemo()
    {
        ImGui.Text("Click on the color button to use the picker.");
        ImGui.SameLine();
        this.defaultColor = ImGuiComponents.ColorPickerWithPalette(1, "ColorPickerWithPalette Demo", this.defaultColor);
    }
}
