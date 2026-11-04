using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Utility.Raii;

using static Dalamud.Interface.Fonts.Internal.FontProfiler;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>Shows the state of the font service.</summary>
internal class FontServiceWidget : IDataWindowWidget
{
    private static readonly string[] PlaneNames = ["Coverage", "Color"];

    private int plane;
    private float zoom = 0.125f;

    private (long Requests, long Misses, long Bytes) lastCounters;
    private (long Requests, long Misses, long Bytes)? perFrame;
    private int lastCountersFrame = -1;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["fontservice", "fonts"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font: Service";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load() => this.Ready = true;

    /// <inheritdoc/>
    public void Draw()
    {
        if (ImGui.CollapsingHeader("Profile"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            BeginProfile();
            DrawProfile();
        }

        if (ImGui.CollapsingHeader("Fonts"u8, ImGuiTreeNodeFlags.DefaultOpen))
            DrawFonts();
        if (ImGui.CollapsingHeader("Layouts"u8, ImGuiTreeNodeFlags.DefaultOpen))
            this.DrawLayouts();
        if (ImGui.CollapsingHeader("Glyph atlas"u8, ImGuiTreeNodeFlags.DefaultOpen))
            this.DrawAtlas();
    }

    private static void DrawProfile()
    {
        ImGui.Text($"Last {FrameCount} frames: {ToMs(AverageFrameTicks()):0.00}ms/f");

        var budget = GlyphRasterBudget.FrameBudget;
        var overridden = GlyphRasterBudget.FrameBudgetOverride is not null;
        if (ImGui.Checkbox("Override the rasterizing budget"u8, ref overridden))
        {
            var start = budget == TimeSpan.MaxValue ? TimeSpan.FromMilliseconds(1) : budget;
            GlyphRasterBudget.FrameBudgetOverride = overridden ? start : null;
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!overridden))
        {
            var ms = budget == TimeSpan.MaxValue ? float.PositiveInfinity : (float)budget.TotalMilliseconds;
            ImGui.SetNextItemWidth(ImGui.GetFontSize() * 12);
            if (ImGui.SliderFloat("ms/f (16=unlimited)"u8, ref ms, 0, 16, "%.2f"))
            {
                GlyphRasterBudget.FrameBudgetOverride =
                    ms >= 16 ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(Math.Max(ms, 0));
            }
        }

        ImGui.Text($"{ConcurrentGlyphQueue.TotalPending} glyphs being rasterized or waiting to be added");

        using var table = ImRaii.Table("##profile"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Section"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Last frame"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Average"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Worst frame"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        for (var section = default(Section); section < Section.Count; section++)
        {
            var (average, worst, worstFrameTicks) = Summarize(section);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(
                section switch
                {
                    Section.Render => "Drawing text",
                    Section.RenderSetup => "- Setup",
                    Section.RenderImages => "- Glyph images",
                    Section.RenderDecorations => "- Decorations",
                    Section.Layout => "Making layouts",
                    Section.Rasterize => "Rasterizing",
                    Section.Deferred => "Deferred",
                    Section.Pack => "Packing",
                    Section.Upload => "Uploading",
                    Section.LayoutAnalyze => "- Analyzing",
                    Section.LayoutShape => "- Shaping",
                    Section.LayoutShapeByFonts => "  - By fonts",
                    Section.LayoutFinish => "- Lines",
                    _ => section.ToString(),
                });
            ImGui.TableNextColumn();
            ImGui.Text(Describe(section, Last(section)));
            ImGui.TableNextColumn();
            ImGui.Text(Describe(section, average));
            ImGui.TableNextColumn();
            ImGui.Text($"{Describe(section, worst)} of {ToMs(worstFrameTicks):0.00}ms");
        }

        return;

        static string Describe(Section section, Stat s)
        {
            var text = $"{ToMs(s.Ticks):0.000}ms, {s.Calls} ";
            return section switch
            {
                Section.Render => $"{text}calls, {s.Bytes} quads",
                Section.RenderSetup or Section.RenderDecorations => $"{text}calls",
                Section.Layout => $"{text}layouts, {s.Bytes / 1024f:0.#} KiB",
                Section.LayoutAnalyze => $"{text}layouts, {s.Bytes} B",
                Section.LayoutShape or Section.LayoutShapeByFonts or Section.LayoutFinish =>
                    $"{text}layouts, {s.Bytes} codepoints",
                Section.Upload => $"{text}uploads, {s.Bytes / 1024f:0.#} KiB",
                _ => $"{text}glyphs",
            };
        }
    }

    private static void DrawFonts()
    {
        var resolver = Service<FontResolver>.Get();
        var entries = resolver.Snapshot();
        ImGui.Text($"{entries.Length} fonts; frame {FontClock.Frame}");
        using var table = ImRaii.Table(
            "##fonts"u8,
            5,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY,
            new(0, ImGui.GetTextLineHeightWithSpacing() * 16));
        if (!table)
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("Family"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("State"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Styles"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Held"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Unused for"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(entries.Length, ImGui.GetTextLineHeightWithSpacing());
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var e = entries[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(e.Family.ToString());
                if (ImGui.IsItemHovered())
                {
                    using var tooltip = ImRaii.Tooltip();
                    DrawFamilyDetails(e.Family);
                }

                ImGui.TableNextColumn();
                ImGui.Text(e.State);
                ImGui.TableNextColumn();
                ImGui.Text($"{e.Styles}");
                ImGui.TableNextColumn();
                ImGui.Text(e.Held ? "Yes" : "No");
                ImGui.TableNextColumn();
                ImGui.Text($"{e.FramesSinceUse} frames");
            }
        }

        clipper.Destroy();
    }

    /// <summary>Draws every property of a family, and of the elements of a chain.</summary>
    /// <param name="family">The family.</param>
    private static void DrawFamilyDetails(IFontFamilyId family)
    {
        if (family is not ChainFamilyId chain)
        {
            ImGui.Text(family.ToString());
            return;
        }

        ImGui.Text($"Chain of {chain.Elements.Length}");
        foreach (var e in chain.Elements)
        {
            ImGui.Bullet();
            using var group = ImRaii.Group();
            DrawFamilyDetails(e.Family);
            var ranges = e.CodepointRanges;
            if (ranges.IsDefaultOrEmpty)
            {
                ImGui.Text("Codepoints: all"u8);
            }
            else
            {
                var sb = new StringBuilder("Codepoints:");
                for (var i = 0; i + 1 < ranges.Length; i += 2)
                    sb.Append(ranges[i] == ranges[i + 1] ? $" U+{ranges[i]:X4}" : $" U+{ranges[i]:X4}-U+{ranges[i + 1]:X4}");
                ImGui.TextWrapped(sb.ToString());
            }

            var offsetUnit = e.GlyphOffsetUnit == StyleUnit.Px ? "px" : "em";
            ImGui.Text($"Glyph offset: ({e.GlyphOffset.X}, {e.GlyphOffset.Y}){offsetUnit}");
            ImGui.Text($"Letter spacing: {e.LetterSpacing}");
            ImGui.Text($"Advance: {e.Advance}; min {e.GlyphMinAdvanceX}; max {e.GlyphMaxAdvanceX}");
            ImGui.Text($"Fit to square: {e.FitToSquare}");
            ImGui.Text($"Size scale: {e.SizeScale}");
            ImGui.Text(
                e.Features.IsDefaultOrEmpty
                    ? "Features: none"
                    : $"Features: {string.Join(' ', e.Features.Select(static f => $"{f.TagName}={f.Value}"))}");
        }
    }

    private void DrawLayouts()
    {
        var now = (TextLayouter.Requests, TextLayouter.Misses, TextLayouter.RequestedBytes);
        var frame = ImGui.GetFrameCount();
        if (frame != this.lastCountersFrame)
        {
            this.perFrame = this.lastCountersFrame == frame - 1
                                ? (now.Requests - this.lastCounters.Requests,
                                      now.Misses - this.lastCounters.Misses,
                                      now.RequestedBytes - this.lastCounters.Bytes)
                                : null;
            (this.lastCounters, this.lastCountersFrame) = (now, frame);
        }

        ImGui.Text($"{TextLayouter.CacheCount} cached, {TextLayouter.ReusedCount} of them used across frames");
        if (this.perFrame is var (requests, misses, bytes))
            ImGui.Text($"Last frame: {requests} asked for, {misses} made, {bytes / 1024f:0.#} KiB of text");
        else
            ImGui.Text("Last frame: -"u8);
        ImGui.Text($"In all: {now.Requests} asked for, {now.Misses} made");
    }

    private void DrawAtlas()
    {
        var atlas = GlyphAtlas.Shared;
        ImGui.Text($"Slices of {GlyphAtlas.SliceSize}x{GlyphAtlas.SliceSize}; {atlas.RestoreCount} glyphs restored");
        ImGui.Text($"Large: {atlas.Large.Count} images, {atlas.Large.Bytes / 1048576f:0.#} MiB on the GPU");
        foreach (var p in (ReadOnlySpan<GlyphAtlas.Plane>)[atlas.Coverage, atlas.Color])
        {
            using var id = ImRaii.PushId((int)p.Kind);
            ImGui.Text(
                $"{p.Kind}: {p.SliceCount} in use, {p.RetiredCount} retired, {p.SlotCount} slots, " +
                $"{p.Capacity} slices on the GPU; " +
                $"{p.GlyphCount} glyph images; {p.EvictionCount} evictions");
            ImGui.SameLine();
            var maxSlices = p.MaxSlices;
            ImGui.SetNextItemWidth(ImGui.GetFontSize() * 8);
            if (ImGui.InputInt("Max slices"u8, ref maxSlices, flags: ImGuiInputTextFlags.EnterReturnsTrue))
                p.MaxSlices = Math.Clamp(maxSlices, 1, GlyphAtlas.SliceLimit);
        }

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 6);
        ImGui.Combo("Plane"u8, ref this.plane, PlaneNames);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 8);
        ImGui.SliderFloat("Zoom"u8, ref this.zoom, 0.03125f, 1f, "%.3fx");

        var kind = (GlyphAtlas.PlaneKind)this.plane;
        var shown = kind == GlyphAtlas.PlaneKind.Color ? atlas.Color : atlas.Coverage;
        var size = new Vector2(GlyphAtlas.SliceSize * this.zoom);
        var drawList = ImGui.GetWindowDrawList();
        for (var i = 0; i < shown.SlotCount; i++)
        {
            ImGui.Text($"Slot {i}: {shown.StateOf(i)}");
            var p0 = ImGui.GetCursorScreenPos();
            var p1 = p0 + size;
            if (ImGui.IsRectVisible(p0, p1))
            {
                drawList.AddRectFilled(p0, p1, 0xFF000000);
                GlyphAtlas.DrawSlice(drawList, kind, i, p0, p1, 0xFFFFFFFF);
            }

            ImGui.Dummy(size);
        }
    }
}
