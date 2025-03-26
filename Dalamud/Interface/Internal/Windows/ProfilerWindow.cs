using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Timing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Class used to draw the Dalamud profiler.
/// </summary>
public class ProfilerWindow : Window
{
    private double min;
    private double max;
    private List<List<Tuple<double, double>>> occupied = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilerWindow"/> class.
    /// </summary>
    public ProfilerWindow()
        : base("Profiler")
    {
    }

    /// <inheritdoc cref="Window.OnOpen"/>
    public override void OnOpen()
    {
        this.min = Timings.AllTimings.Keys.Min(x => x.StartTime);
        this.max = Timings.AllTimings.Keys.Max(x => x.EndTime);
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var width = ImGui.GetWindowWidth();
        var actualMin = Timings.AllTimings.Keys.Min(x => x.StartTime);
        var actualMax = Timings.AllTimings.Keys.Max(x => x.EndTime);

        ImGui.Text("Timings");

        var childHeight = Math.Max(300, 20 * (2.5f + this.occupied.Count));

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

            foreach (var l in this.occupied)
                l.Clear();

            var parentDepthDict = new Dictionary<long, int>();
            var rects = new Dictionary<long, RectInfo>();
            var mousePos = ImGui.GetMousePos();
            foreach (var timingHandle in Timings.AllTimings.Keys)
            {
                var startX = (timingHandle.StartTime - this.min) / (this.max - this.min) * width;
                var endX = (timingHandle.EndTime - this.min) / (this.max - this.min) * width;
                var depth = timingHandle.IdChain.Length < 2 ? 0 : parentDepthDict.GetValueOrDefault(timingHandle.IdChain[^2]);
                for (; depth < this.occupied.Count; depth++)
                {
                    var acceptable = true;
                    foreach (var (x1, x2) in this.occupied[depth])
                    {
                        if (x2 <= timingHandle.StartTime || x1 >= timingHandle.EndTime)
                            continue;
                        acceptable = false;
                        break;
                    }

                    if (acceptable)
                        break;
                }

                if (depth == this.occupied.Count)
                    this.occupied.Add(new());
                this.occupied[depth].Add(Tuple.Create(timingHandle.StartTime, timingHandle.EndTime));
                parentDepthDict[timingHandle.Id] = depth;

                startX = Math.Max(startX, 0);
                endX = Math.Max(endX, startX + (ImGuiHelpers.GlobalScale * 16));

                Vector4 rectColor;
                if (this.occupied[depth].Count % 2 == 0)
                    rectColor = timingHandle.IsMainThread ? ImGuiColors.ParsedBlue : ImGuiColors.ParsedOrange;
                else
                    rectColor = timingHandle.IsMainThread ? ImGuiColors.ParsedPurple : ImGuiColors.ParsedGold;
                rectColor.X -= timingHandle.IdChain.Length * 0.12f;
                rectColor.Y -= timingHandle.IdChain.Length * 0.12f;
                rectColor.Z -= timingHandle.IdChain.Length * 0.12f;

                if (maxRectDept < depth)
                    maxRectDept = (uint)depth;

                var minPos = pos + new Vector2((uint)startX, 20 * depth);
                var maxPos = pos + new Vector2((uint)endX, 20 * (depth + 1));

                rects[timingHandle.Id] = new RectInfo
                {
                    Hover = mousePos.X >= minPos.X && mousePos.X < maxPos.X &&
                            mousePos.Y >= minPos.Y && mousePos.Y < maxPos.Y,
                    Timing = timingHandle,
                    MinPos = minPos,
                    MaxPos = maxPos,
                    RectColor = rectColor,
                };
            }

            foreach (var hoveredItem in rects.Values.Where(x => x.Hover))
            {
                foreach (var rectInfo in rects.Values)
                {
                    if (rectInfo == hoveredItem)
                        rectInfo.RectColor = new Vector4(255, 255, 255, 255);
                    else if (rectInfo.Timing.IdChain.Contains(hoveredItem.Timing.Id))
                        rectInfo.RectColor = ImGuiColors.ParsedGreen;
                    else if (hoveredItem.Timing.IdChain.Contains(rectInfo.Timing.Id))
                        rectInfo.RectColor = ImGuiColors.ParsedPink;
                }
            }

            foreach (var rectInfo in rects.Values)
            {
                ImGui.GetWindowDrawList().AddRectFilled(
                    rectInfo.MinPos,
                    rectInfo.MaxPos,
                    ImGui.GetColorU32(rectInfo.RectColor));

                if (rectInfo.Hover)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 255));
                ImGui.GetWindowDrawList().AddTextClippedEx(
                    rectInfo.MinPos,
                    rectInfo.MaxPos,
                    rectInfo.Timing.Name,
                    null,
                    Vector2.Zero,
                    null);
                if (rectInfo.Hover)
                    ImGui.PopStyleColor();

                // Show tooltip when hovered
                if (rectInfo.Hover)
                {
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted(rectInfo.Timing.Name);
                    ImGui.TextUnformatted(rectInfo.Timing.MemberName);
                    ImGui.TextUnformatted($"{rectInfo.Timing.FileName}:{rectInfo.Timing.LineNumber}");
                    ImGui.TextUnformatted($"Duration: {rectInfo.Timing.Duration}ms");
                    if (rectInfo.Timing.Parent != null)
                        ImGui.TextUnformatted($"Parent: {rectInfo.Timing.Parent.Name}");
                    ImGui.EndTooltip();
                }
            }

            uint eventTextDepth = maxRectDept + 2;

            var eventsXPos = new List<float>();
            const float eventsXPosFudge = 5f;
            
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
                
                var numClashes = eventsXPos.Count(x => Math.Abs(x - textPos.X) < textSize.X + eventsXPosFudge);
                if (numClashes > 0)
                {
                    textPos.Y -= numClashes * textSize.Y;
                }

                ImGui.GetWindowDrawList().AddText(
                    textPos,
                    ImGui.GetColorU32(ImGuiColors.DalamudWhite),
                    timingEvent.Name);
                
                eventsXPos.Add(textPos.X);
            }
        }

        ImGui.EndChild();

        var sliderMin = (float)this.min / 1000f;
        if (ImGui.SliderFloat("Start", ref sliderMin, (float)actualMin / 1000f, (float)this.max / 1000f, "%.2fs"))
        {
            this.min = sliderMin * 1000f;
        }

        var sliderMax = (float)this.max / 1000f;
        if (ImGui.SliderFloat("End", ref sliderMax, (float)this.min / 1000f, (float)actualMax / 1000f, "%.2fs"))
        {
            this.max = sliderMax * 1000f;
        }

        var sizeShown = (float)(this.max - this.min) / 1000f;
        var sizeActual = (float)(actualMax - actualMin) / 1000f;
        if (ImGui.SliderFloat("Size", ref sizeShown, sizeActual / 10f, sizeActual, "%.2fs"))
        {
            this.max = this.min + (sizeShown * 1000f);
        }

        ImGui.Text("Min: " + actualMin.ToString("0.000"));
        ImGui.Text("Max: " + actualMax.ToString("0.000"));
        ImGui.Text("Timings: " + Timings.AllTimings.Count);
    }

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internals")]
    private class RectInfo
    {
        // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized <- well you're wrong
        internal TimingHandle Timing;
        internal Vector2 MinPos;
        internal Vector2 MaxPos;
        internal Vector4 RectColor;
        internal bool Hover;
    }
}
