using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Class responsible for drawing a notifier on screen that gamepad mode is active.
/// </summary>
internal class GamepadModeNotifier : IDisposable
{
    private const float TitleFontSizePt = 36f;
    private const float TitleFontSizePx = TitleFontSizePt * 4 / 3;

    private const uint DimColor = 0x661A1A1A;
    private const uint TextColor = 0xFFFFFFFF;

    private const float TopMargin = 60f;
    private const float GlowSpeed = 3f;
    private const float GlowMaxAlpha = 0.35f;
    private const float GlowRingWidth = 1.5f;
    private const int GlowRings = 3;
    private const int GlowSteps = 12;

    private static readonly Vector4 GlowColor = new(0.35f, 0.75f, 1f, 1f);

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly IFontHandle titleFont;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamepadModeNotifier"/> class.
    /// </summary>
    /// <param name="fontAtlasFactory">Font factory.</param>
    public GamepadModeNotifier(FontAtlasFactory fontAtlasFactory)
    {
        var atlas = this.scopedFinalizer.Add(
            fontAtlasFactory.CreateFontAtlas(nameof(GamepadModeNotifier), FontAtlasAutoRebuildMode.Async));
        this.titleFont = this.scopedFinalizer.Add(
            atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(TitleFontSizePx))));
    }

    /// <inheritdoc/>
    public void Dispose() => this.scopedFinalizer.Dispose();

    /// <summary>
    /// Draws a light grey-ish, main-viewport-big filled rect in the background draw list alongside a text indicating gamepad mode.
    /// </summary>
    public void Draw()
    {
        var viewport = ImGuiHelpers.MainViewport;
        var drawList = ImGui.GetBackgroundDrawList(viewport);
        var viewportMax = viewport.Pos + viewport.Size;

        drawList.PushClipRect(viewport.Pos, viewportMax);
        drawList.AddRectFilled(viewport.Pos, viewportMax, DimColor);

        var centerX = viewport.WorkPos.X + (viewport.WorkSize.X / 2);
        var y = viewport.WorkPos.Y + (TopMargin * ImGuiHelpers.GlobalScale);

        using (this.titleFont.Push())
        {
            var title = Loc.Localize("DalamudGamepadModeNotifierTitle", "Gamepad Mode On");
            var titlePos = new Vector2(centerX - (ImGui.CalcTextSize(title).X / 2), y);
            DrawGlow(drawList, titlePos, title);
            drawList.AddText(titlePos, TextColor, title);
            y += ImGui.GetTextLineHeightWithSpacing();
        }

        string IconMacro(BitmapFontIcon icon) => $"<icon({(int)icon})>";

        var description = string.Format(
            Loc.Localize(
                "DalamudGamepadModeNotifierText",
                "Dalamud is sending gamepad inputs to plugin windows.\nPress {0}+{1} to deactivate or press {2} to toggle the plugin installer.\nHold {3} to select windows."),
            IconMacro(BitmapFontIcon.ControllerShoulderLeft),
            IconMacro(BitmapFontIcon.ControllerAnalogLeftStickIn),
            IconMacro(BitmapFontIcon.ControllerAnalogRightStickIn),
            IconMacro(BitmapFontIcon.ControllerButton2));

        var renderer = Service<SeStringRenderer>.Get();
        var lineParams = new SeStringDrawParams
        {
            TargetDrawList = drawList,
            WrapWidth = viewport.WorkSize.X,
            Font = ImGui.GetFont(),
            FontSize = ImGui.GetFontSize(),
            Color = TextColor,
        };

        foreach (var line in description.Split('\n'))
        {
            var size = ImGuiHelpers.CenteredSeString(
                renderer.CompileAndCache(line).AsSpan(),
                lineParams with { ScreenOffset = viewport.WorkPos with { Y = y } }).Size;
            y += size.Y + ImGui.GetStyle().ItemSpacing.Y;
        }

        drawList.PopClipRect();
    }

    private static void DrawGlow(ImDrawListPtr drawList, Vector2 pos, string text)
    {
        var pulse = (MathF.Sin((float)ImGui.GetTime() * GlowSpeed) + 1) / 2;

        for (var ring = 1; ring <= GlowRings; ring++)
        {
            var alpha = pulse * GlowMaxAlpha / ring;
            var color = ImGui.ColorConvertFloat4ToU32(GlowColor with { W = alpha });
            var radius = ring * GlowRingWidth * ImGuiHelpers.GlobalScale;

            for (var step = 0; step < GlowSteps; step++)
            {
                var angle = MathF.Tau * step / GlowSteps;
                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                drawList.AddText(pos + offset, color, text);
            }
        }
    }
}
