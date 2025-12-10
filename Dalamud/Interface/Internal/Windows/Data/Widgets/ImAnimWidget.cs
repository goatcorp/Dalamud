using System.Numerics;

using Dalamud.Bindings.ImAnim;
using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying addon inspector.
/// </summary>
internal class ImAnimWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ImAnim";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    private static uint CLIP_BOUNCE;
    private static uint CLIP_CH_OFFSET;
    private static uint CLIP_CH_SCALE;
    private static uint CLIP_CH_ALPHA;

    private static uint CLIP_WITH_CALLBACKS;

    private static int s_callback_begin_count;
    private static int s_callback_update_count;
    private static int s_callback_complete_count;

    /// <inheritdoc/>
    public unsafe void Load()
    {
        var spring = new ImAnimSpringParams
        {
            Mass = 1.0f,
            Stiffness = 180.0f,
            Damping = 22.0f, // Higher damping to prevent excessive scale overshoot
            InitialVelocity = 0.0f,
        };

        CLIP_BOUNCE = ImGui.GetID("DALAMUD_CLIP_BOUNCE");
        CLIP_CH_OFFSET = ImGui.GetID("DALAMUD_CLIP_CH_OFFSET");
        CLIP_CH_SCALE = ImGui.GetID("DALAMUD_CLIP_CH_SCALE");
        CLIP_CH_ALPHA = ImGui.GetID("DALAMUD_CLIP_CH_ALPHA");

        ImAnimClip.Begin(CLIP_BOUNCE)
            .KeyVec2(CLIP_CH_OFFSET, 0.0f, new Vector2(0, -50), ImAnimEaseType.Linear)
            .KeyFloat(CLIP_CH_SCALE, 0.0f, 0.6f, ImAnimEaseType.Linear)
            .KeyFloat(CLIP_CH_ALPHA, 0.0f, 0.3f, ImAnimEaseType.Linear)
            .KeyVec2(CLIP_CH_OFFSET, 0.3f, new Vector2(0, 10), ImAnimEaseType.OutQuad)
            .KeyFloat(CLIP_CH_ALPHA, 0.3f, 1.0f, ImAnimEaseType.OutQuad)
            .KeyVec2(CLIP_CH_OFFSET, 0.5f, new Vector2(0, -15), ImAnimEaseType.OutQuad)
            .KeyVec2(CLIP_CH_OFFSET, 0.7f, new Vector2(0, 5), ImAnimEaseType.OutQuad)
            .KeyVec2(CLIP_CH_OFFSET, 0.9f, new Vector2(0, 0), ImAnimEaseType.OutBounce)
            .KeyFloatSpring(CLIP_CH_SCALE, 0.3f, 1.0f, spring)
            .End();

        CLIP_WITH_CALLBACKS = ImGui.GetID("DALAMUD_CLIP_WITH_CALLBACKS");

        ImAnimClip.Begin(CLIP_WITH_CALLBACKS)
            .KeyFloat(CLIP_CH_SCALE, 0.0f, 0.5f, ImAnimEaseType.OutCubic)
            .KeyFloat(CLIP_CH_SCALE, 0.5f, 1.2f, ImAnimEaseType.OutBack)
            .KeyFloat(CLIP_CH_SCALE, 1.0f, 1.0f, ImAnimEaseType.InOutSine)
            .OnBegin((id, user_data) => s_callback_begin_count++)
            .OnUpdate((id, user_data) => s_callback_update_count++)
            .OnComplete((id, user_data) => s_callback_complete_count++)
            .End();

        this.Ready = true;
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        var inst_id = ImGui.GetID("DALAMUD_BOUNCE_INST");

        if (ImGui.Button("Play Bounce"))
        {
            ImAnim.Play(CLIP_BOUNCE, inst_id);
        }

        ImGui.SameLine();

        var inst = ImAnim.GetInstance(inst_id);
        var offset = Vector2.Zero;
        var scale = 1.0f;
        var alpha = 1.0f;

        if (inst.Valid())
        {
            inst.GetVec2(CLIP_CH_OFFSET, out offset);
            inst.GetFloat(CLIP_CH_SCALE, out scale);
            inst.GetFloat(CLIP_CH_ALPHA, out alpha);
        }

        // Clamp scale to valid range for SetWindowFontScale
        if (scale < 0.1f) scale = 0.1f;
        if (scale > 10.0f) scale = 10.0f;

        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPos(cur + offset);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
        ImGui.SetWindowFontScale(scale);
        ImGui.Text("Bouncing!");
        ImGui.SetWindowFontScale(1.0f);
        ImGui.PopStyleVar();

        ImGui.Separator();

        inst_id = ImGui.GetID("DALAMUD_CALLBACK_INST");

        if (ImGui.Button("Play with Callbacks"))
        {
            ImAnim.Play(CLIP_WITH_CALLBACKS, inst_id);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset Counters"))
        {
            s_callback_begin_count = 0;
            s_callback_update_count = 0;
            s_callback_complete_count = 0;
        }

        inst = ImAnim.GetInstance(inst_id);
        scale = 1.0f;

        if (inst.Valid())
        {
            inst.GetFloat(CLIP_CH_SCALE, out scale);
        }

        if (scale < 0.1f) scale = 0.1f;
        if (scale > 10.0f) scale = 10.0f;

        ImGui.SameLine();
        ImGui.SetWindowFontScale(scale);
        ImGui.Text("Scaling");
        ImGui.SetWindowFontScale(1.0f);

        ImGui.Text($"on_begin called:    {s_callback_begin_count} times");
        ImGui.Text($"on_update called:   {s_callback_update_count} times");
        ImGui.Text($"on_complete called: {s_callback_complete_count} times");

        ImGui.Separator();

        ImGui.Text("viewport: GetWindowViewport()->Size");
        var vp_size = ImGui.GetIO().DisplaySize;

        var display_size = new Vector2(MathF.Min(vp_size.X * 0.3f, 400.0f), 60);
        var origin = ImGui.GetCursorScreenPos();

        var draw_list = ImGui.GetWindowDrawList();
        draw_list.AddRectFilled(origin, new Vector2(origin.X + display_size.X, origin.Y + display_size.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(50, 40, 40, 255)));
        draw_list.AddRect(origin, new Vector2(origin.X + display_size.X, origin.Y + display_size.Y), ImGui.ColorConvertFloat4ToU32(new Vector4(120, 80, 80, 255)));

        var id = ImGui.GetID("anchor_viewport");
        // var pos = ImAnim.TweenVec4Rel(id, 0, new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Vector4(0, 0, 0, 0), 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImAnimAnchorSpace.Viewport, ImGui.GetIO().DeltaTime);
        // var pos = ImAnim.TweenVec4Resolved(id, 0, (_) => new Vector4(0.5f, 0.5f, 0.5f, 0.5f), null, 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImGui.GetIO().DeltaTime);
        var pos = ImAnim.TweenVec4Resolved(id, 0, () => new Vector4(0.5f, 0.5f, 0.5f, 0.5f), 0.5f, ImAnim.EasePreset(ImAnimEaseType.OutCubic), ImAnimPolicy.Crossfade, ImGui.GetIO().DeltaTime);

        // Scale position to display size
        var scale_x = display_size.X / vp_size.X;
        var scale_y = display_size.Y / vp_size.Y;
        var draw_x = Math.Clamp(pos.X * scale_x, 10.0f, display_size.X - 10.0f);
        var draw_y = Math.Clamp(pos.Y * scale_y, 10.0f, display_size.Y - 10.0f);
        draw_list.AddCircleFilled(new Vector2(origin.X + draw_x, origin.Y + draw_y), 8.0f, ImGui.ColorConvertFloat4ToU32(new Vector4(255, 100, 100, 255)));
        draw_list.AddText(new Vector2(origin.X + 5, origin.Y + 5), ImGui.ColorConvertFloat4ToU32(new Vector4(255, 180, 180, 255)), "Viewport Size (scaled preview)");

        ImGui.Dummy(display_size);
        ImGui.Text($"Actual viewport size: ({vp_size.X}, {vp_size.X}), Center pos: ({pos.X}, {pos.Y})");
    }
}
