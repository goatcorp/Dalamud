using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Timing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

public class ProfilerWindow : Window
{
    private double min;
    private double max;

    public ProfilerWindow() : base("Profiler", forceMainWindow: true) { }

    public override void OnOpen()
    {
        this.min = Timings.AllTimings.Min(x => x.StartTime);
        this.max = Timings.AllTimings.Max(x => x.EndTime);
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var width = ImGui.GetWindowWidth();
        var actualMin = Timings.AllTimings.Min(x => x.StartTime);
        var actualMax = Timings.AllTimings.Max(x => x.EndTime);

        ImGui.Text("Timings");

        const int childHeight = 300;

        if (ImGui.BeginChild("Timings", new Vector2(0, childHeight), true))
        {
            var pos = ImGui.GetCursorScreenPos();

            for (var i = 0; i < width; i += 80)
            {
                ImGui.PushFont(InterfaceManager.MonoFont);

                var lineEnd = childHeight - 20;

                ImGui.GetWindowDrawList().AddLine(
                    pos + new Vector2(i, 0),
                    pos + new Vector2(i, lineEnd - 10),
                    ImGui.GetColorU32(ImGuiColors.ParsedGrey.WithW(0x40)));

                // Draw ms label for line
                var ms = ((i / width) * (this.max - this.min)) + this.min;
                var msStr = (ms / 1000).ToString("F2") + "s";
                var msSize = ImGui.CalcTextSize(msStr);
                var labelPos = pos + new Vector2(i - (msSize.X / 2), (-msSize.Y / 2) + lineEnd);

                // nudge label to the side if it's the first, so we're not cut off
                if (i == 0)
                    labelPos.X += msSize.X / 2;

                ImGui.GetWindowDrawList().AddText(
                    labelPos,
                    ImGui.GetColorU32(ImGuiColors.ParsedGrey.WithW(0x40)),
                    msStr);

                ImGui.PopFont();
            }

            uint maxRectDept = 0;

            foreach (var timingHandle in Timings.AllTimings)
            {
                var startX = (timingHandle.StartTime - this.min) / (this.max - this.min) * width;
                var endX = (timingHandle.EndTime - this.min) / (this.max - this.min) * width;

                startX = Math.Max(startX, 0);
                endX = Math.Max(endX, 0);

                var rectColor = timingHandle.IsMainThread ? ImGuiColors.ParsedBlue : ImGuiColors.ParsedPurple;
                rectColor.X -= timingHandle.Depth * 0.12f;
                rectColor.Y -= timingHandle.Depth * 0.12f;
                rectColor.Z -= timingHandle.Depth * 0.12f;

                if (maxRectDept < timingHandle.Depth)
                    maxRectDept = timingHandle.Depth;

                if (startX == endX)
                {
                    continue;
                }

                var minPos = pos + new Vector2((uint)startX, 20 * timingHandle.Depth);
                var maxPos = pos + new Vector2((uint)endX, 20 * (timingHandle.Depth + 1));

                ImGui.GetWindowDrawList().AddRectFilled(
                    minPos,
                    maxPos,
                    ImGui.GetColorU32(rectColor));

                ImGui.GetWindowDrawList().AddTextClippedEx(minPos, maxPos, timingHandle.Name, null, Vector2.Zero, null);

                // Show tooltip when hovered
                var mousePos = ImGui.GetMousePos();
                if (mousePos.X > pos.X + startX && mousePos.X < pos.X + endX &&
                    mousePos.Y > pos.Y + (20 * timingHandle.Depth) &&
                    mousePos.Y < pos.Y + (20 * (timingHandle.Depth + 1)))
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(timingHandle.Name);
                    ImGui.Text(timingHandle.MemberName);
                    ImGui.Text($"{timingHandle.FileName}:{timingHandle.LineNumber}");
                    ImGui.Text($"Duration: {timingHandle.Duration}ms");
                    ImGui.EndTooltip();
                }
            }

            uint eventTextDepth = maxRectDept + 2;

            foreach (var timingEvent in Timings.Events)
            {
                var startX = (timingEvent.StartTime - this.min) / (this.max - this.min) * width;

                if (startX < 0 || startX > width)
                {
                    continue;
                }

                ImGui.GetWindowDrawList().AddLine(
                    pos + new Vector2((uint)startX, 0),
                    pos + new Vector2((uint)startX, childHeight),
                    ImGui.GetColorU32(ImGuiColors.ParsedOrange),
                    1.5f);

                const uint padding = 5;

                var textSize = ImGui.CalcTextSize(timingEvent.Name);
                var textPos = pos + new Vector2((uint)startX + padding, eventTextDepth * 20);

                if (textPos.X + textSize.X > pos.X + width - 20)
                {
                    textPos.X = pos.X + (uint)startX - textSize.X - padding;
                }

                ImGui.GetWindowDrawList().AddText(
                    textPos,
                    ImGui.GetColorU32(ImGuiColors.DalamudWhite),
                    timingEvent.Name);
            }
        }

        ImGui.EndChild();

        var sliderMin = (float)this.min / 1000f;
        if (ImGui.SliderFloat("Start", ref sliderMin, (float)actualMin / 1000f, (float)this.max / 1000f, "%.1fs"))
        {
            this.min = sliderMin * 1000f;
        }

        var sliderMax = (float)this.max / 1000f;
        if (ImGui.SliderFloat("End", ref sliderMax, (float)this.min / 1000f, (float)actualMax / 1000f, "%.1fs"))
        {
            this.max = sliderMax * 1000f;
        }

        var sizeShown = (float)(this.max - this.min);
        var sizeActual = (float)(actualMax - actualMin);
        if (ImGui.SliderFloat("Size", ref sizeShown, sizeActual / 10f, sizeActual, "%.1fs"))
        {
            this.max = this.min + sizeShown;
        }

        ImGui.Text("Min: " + actualMin.ToString("0.000"));
        ImGui.Text("Max: " + actualMax.ToString("0.000"));
        ImGui.Text("Timings: " + Timings.AllTimings.Count);
    }
}
