using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

[Experimental("Dalamud001")]
public static unsafe partial class ImAnimNative
{
    private const string LibName = "cimanim";

    // ----------------------------------------------------
    // Public API declarations
    // ----------------------------------------------------

    [LibraryImport(LibName, EntryPoint = "c_iam_set_imgui_context")]
    public static partial void SetImGuiContext(ImGuiContext* context);

    [LibraryImport(LibName, EntryPoint = "c_iam_demo_window")]
    public static partial void DemoWindow();

    // Frame management

    [LibraryImport(LibName, EntryPoint = "c_iam_update_begin_frame")]
    public static partial void UpdateBeginFrame();

    [LibraryImport(LibName, EntryPoint = "c_iam_gc")]
    public static partial void Gc(uint maxAgeFrames = 600);

    [LibraryImport(LibName, EntryPoint = "c_iam_reserve")]
    public static partial void Reserve(int capFloat, int capVec2, int capVec4, int capInt, int capColor);

    [LibraryImport(LibName, EntryPoint = "c_iam_set_ease_lut_samples")]
    public static partial void SetEaseLutSamples(int count);

    // Global time scale (for slow-motion / fast-forward debugging)

    [LibraryImport(LibName, EntryPoint = "c_iam_set_global_time_scale")]
    public static partial void SetGlobalTimeScale(float scale);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_global_time_scale")]
    public static partial float GetGlobalTimeScale();

    // Lazy Initialization - defer channel creation until animation is needed

    [LibraryImport(LibName, EntryPoint = "c_iam_set_lazy_init")]
    public static partial void SetLazyInit(byte enable);

    [LibraryImport(LibName, EntryPoint = "c_iam_is_lazy_init_enabled")]
    public static partial byte IsLazyInitEnabled();

    [LibraryImport(LibName, EntryPoint = "c_iam_register_custom_ease")]
    public static partial void RegisterCustomEase(int slot, delegate* unmanaged[Cdecl]<float, float> fn);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_custom_ease")]
    public static partial void GetCustomEase(delegate* unmanaged[Cdecl]<float, float>* pOut, int slot);

    // Debug UI

    [LibraryImport(LibName, EntryPoint = "c_iam_show_unified_inspector")]
    public static partial void ShowUnifiedInspector(byte* pOpen = null);

    [LibraryImport(LibName, EntryPoint = "c_iam_show_debug_timeline")]
    public static partial void ShowDebugTimeline(uint instance_id);

    // Performance Profiler

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_enable")]
    public static partial void ProfilerEnable(byte enable);

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_is_enabled")]
    public static partial byte ProfilerIsEnabled();

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_begin_frame")]
    public static partial void ProfilerBeginFrame();

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_end_frame")]
    public static partial void ProfilerEndFrame();

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_begin")]
    public static partial void ProfilerBegin(byte* name);

    [LibraryImport(LibName, EntryPoint = "c_iam_profiler_end")]
    public static partial void ProfilerEnd();

    // Drag Feedback - animated feedback for drag operations

    [LibraryImport(LibName, EntryPoint = "c_iam_drag_begin")]
    public static partial void DragBegin(ImAnimDragFeedback* pOut, uint id, Vector2* pos);

    [LibraryImport(LibName, EntryPoint = "c_iam_drag_update")]
    public static partial void DragUpdate(ImAnimDragFeedback* pOut, uint id, Vector2* pos, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_drag_release")]
    public static partial void DragRelease(ImAnimDragFeedback* pOut, uint id, Vector2* pos, ImAnimDragOpts* opts, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_drag_cancel")]
    public static partial void DragCancel(uint id);

    // Oscillators - continuous periodic animations

    [LibraryImport(LibName, EntryPoint = "c_iam_oscillate")]
    public static partial float Oscillate(uint id, float amplitude, float frequency, ImAnimWaveType waveType, float phase, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_oscillate_int")]
    public static partial int OscillateInt(uint id, int amplitude, float frequency, ImAnimWaveType waveType, float phase, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_oscillate_vec2")]
    public static partial void OscillateVec2(Vector2* pOut, uint id, Vector2* amplitude, Vector2* frequency, ImAnimWaveType waveType, Vector2* phase, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_oscillate_vec4")]
    public static partial void OscillateVec4(Vector4* pOut, uint id, Vector4* amplitude, Vector4* frequency, ImAnimWaveType waveType, Vector4* phase, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_oscillate_color")]
    public static partial void OscillateColor(Vector4* pOut, uint id, Vector4* baseColor, Vector4* amplitude, float frequency, ImAnimWaveType waveType, float phase, ImAnimColorSpace colorSpace, float dt);

    // Shake/Wiggle - procedural noise animations

    [LibraryImport(LibName, EntryPoint = "c_iam_shake")]
    public static partial float Shake(uint id, float intensity, float frequency, float decayTime, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_shake_int")]
    public static partial int ShakeInt(uint id, int intensity, float frequency, float decayTime, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_shake_vec2")]
    public static partial void ShakeVec2(Vector2* pOut, uint id, Vector2* intensity, float frequency, float decayTime, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_shake_vec4")]
    public static partial void ShakeVec4(Vector4* pOut, uint id, Vector4* intensity, float frequency, float decayTime, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_shake_color")]
    public static partial void ShakeColor(Vector4* pOut, uint id, Vector4* baseColor, Vector4* intensity, float frequency, float decayTime, ImAnimColorSpace colorSpace, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_wiggle")]
    public static partial float Wiggle(uint id, float amplitude, float frequency, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_wiggle_int")]
    public static partial int WiggleInt(uint id, int amplitude, float frequency, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_wiggle_vec2")]
    public static partial void WiggleVec2(Vector2* pOut, uint id, Vector2* amplitude, float frequency, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_wiggle_vec4")]
    public static partial void WiggleVec4(Vector4* pOut, uint id, Vector4* amplitude, float frequency, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_wiggle_color")]
    public static partial void WiggleColor(Vector4* pOut, uint id, Vector4* baseColor, Vector4* amplitude, float frequency, ImAnimColorSpace colorSpace, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_trigger_shake")]
    public static partial void TriggerShake(uint id);

    // Easing evaluation

    [LibraryImport(LibName, EntryPoint = "c_iam_eval_preset")]
    public static partial float EvalPreset(ImAnimEaseType type, float t);

    // Tween API - smoothly interpolate values over time

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_float")]
    public static partial float TweenFloat(uint id, uint channelId, float target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec2")]
    public static partial void TweenVec2(Vector2* pOut, uint id, uint channelId, Vector2* target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec4")]
    public static partial void TweenVec4(Vector4* pOut, uint id, uint channelId, Vector4* target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_int")]
    public static partial int TweenInt(uint id, uint channelId, int target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_color")]
    public static partial void TweenColor(Vector4* pOut, uint id, uint channelId, Vector4* targetSrgb, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt);

    // Resize-friendly helpers

    [LibraryImport(LibName, EntryPoint = "c_iam_anchor_size")]
    public static partial void GetAnchorSize(Vector2* pOut, ImAnimAnchorSpace space);

    // Relative target tweens (percent of anchor + pixel offset) - survive window resizes

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_float_rel")]
    public static partial float TweenFloatRel(uint id, uint channelId, float percent, float pxBias, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, int axis, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec2_rel")]
    public static partial void TweenVec2Rel(Vector2* pOut, uint id, uint channelId, Vector2* percent, Vector2* pxBias, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec4_rel")]
    public static partial void TweenVec4Rel(Vector4* pOut, uint id, uint channelId, Vector4* percent, Vector4* pxBias, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_color_rel")]
    public static partial void TweenColorRel(Vector4* pOut, uint id, uint channelId, Vector4* percent, Vector4* pxBias, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, ImAnimAnchorSpace anchorSpace, float dt);

    // Rebase functions - change target of in-progress animation without restarting

    [LibraryImport(LibName, EntryPoint = "c_iam_rebase_float")]
    public static partial void RebaseFloat(uint id, uint channelId, float newTarget, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_rebase_vec2")]
    public static partial void RebaseVec2(uint id, uint channelId, Vector2* newTarget, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_rebase_vec4")]
    public static partial void RebaseVec4(uint id, uint channelId, Vector4* newTarget, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_rebase_int")]
    public static partial void RebaseInt(uint id, uint channelId, int newTarget, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_rebase_color")]
    public static partial void RebaseColor(uint id, uint channelId, Vector4* newTarget, float dt);

    // Resolved tweens - target computed dynamically by callback each frame

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_float_resolved")]
    public static partial float TweenFloatResolved(uint id, uint channelId, delegate* unmanaged[Cdecl]<void*, float> fn, void* userData, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec2_resolved")]
    public static partial void TweenVec2Resolved(Vector2* pOut, uint id, uint channelId, delegate* unmanaged[Cdecl]<void*, Vector2> fn, void* userData, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec4_resolved")]
    public static partial void TweenVec4Resolved(Vector4* pOut, uint id, uint channelId, delegate* unmanaged[Cdecl]<void*, Vector4> fn, void* userData, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_color_resolved")]
    public static partial void TweenColorResolved(Vector4* pOut, uint id, uint channelId, delegate* unmanaged[Cdecl]<void*, Vector4> fn, void* userData, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_int_resolved")]
    public static partial int TweenIntResolved(uint id, uint channelId, delegate* unmanaged[Cdecl]<void*, int> fn, void* userData, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt);

    // Color blending utility

    [LibraryImport(LibName, EntryPoint = "c_iam_get_blended_color")]
    public static partial void GetBlendedColor(Vector4* pOut, Vector4* a, Vector4* b, float t, ImAnimColorSpace colorSpace);


    // ----------------------------------------------------
    // Convenience shorthands for common easings
    // ----------------------------------------------------

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_preset")]
    public static partial void EasePreset(ImAnimEaseDesc* pOut, ImAnimEaseType type);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_bezier")]
    public static partial void EaseBezier(ImAnimEaseDesc* pOut, float x1, float y1, float x2, float y2);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_steps_desc")]
    public static partial void EaseStepsDesc(ImAnimEaseDesc* pOut, int steps, ImAnimEaseStepsMode mode);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_back")]
    public static partial void EaseBack(ImAnimEaseDesc* pOut, float overshoot);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_elastic")]
    public static partial void EaseElastic(ImAnimEaseDesc* pOut, float amplitude, float period);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_spring_desc")]
    public static partial void EaseSpring(ImAnimEaseDesc* pOut, float mass, float stiffness, float damping, float v0);

    [LibraryImport(LibName, EntryPoint = "c_iam_ease_custom_fn")]
    public static partial void EaseCustomFn(ImAnimEaseDesc* pOut, int slot);

    // Scroll animation - smooth scrolling for ImGui windows

    [LibraryImport(LibName, EntryPoint = "c_iam_scroll_to_y")]
    public static partial void ScrollToY(float targetY, float duration, ImAnimEaseDesc* ez = null);

    [LibraryImport(LibName, EntryPoint = "c_iam_scroll_to_x")]
    public static partial void ScrollToX(float targetX, float duration, ImAnimEaseDesc* ez = null);

    [LibraryImport(LibName, EntryPoint = "c_iam_scroll_to_top")]
    public static partial void ScrollToTop(float duration = 0.3f, ImAnimEaseDesc* ez = null);

    [LibraryImport(LibName, EntryPoint = "c_iam_scroll_to_bottom")]
    public static partial void ScrollToBottom(float duration = 0.3f, ImAnimEaseDesc* ez = null);


    // ----------------------------------------------------
    // Per-axis easing - different easing per component
    // ----------------------------------------------------

    // Tween with per-axis easing - each component uses its own easing curve

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec2_per_axis")]
    public static partial void TweenVec2PerAxis(Vector2* pOut, uint id, uint channelId, Vector2* target, float dur, ImAnimEasePerAxis* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_vec4_per_axis")]
    public static partial void TweenVec4PerAxis(Vector4* pOut, uint id, uint channelId, Vector4* target, float dur, ImAnimEasePerAxis* ez, ImAnimPolicy policy, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_color_per_axis")]
    public static partial void TweenColorPerAxis(Vector4* pOut, uint id, uint channelId, Vector4* targetSrgb, float dur, ImAnimEasePerAxis* ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt);


    // ----------------------------------------------------
    // Motion Paths - animate along curves and splines
    // ----------------------------------------------------

    // Single-curve evaluation functions (stateless, for direct use)

    [LibraryImport(LibName, EntryPoint = "c_iam_bezier_quadratic")]
    public static partial void BezierQuadratic(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_bezier_cubic")]
    public static partial void BezierCubic(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, Vector2* p3, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_catmull_rom")]
    public static partial void CatmullRom(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, Vector2* p3, float t, float tension);

    // Derivatives (for tangent/velocity)

    [LibraryImport(LibName, EntryPoint = "c_iam_bezier_quadratic_deriv")]
    public static partial void BezierQuadraticDeriv(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_bezier_cubic_deriv")]
    public static partial void BezierCubicDeriv(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, Vector2* p3, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_catmull_rom_deriv")]
    public static partial void CatmullRomDeriv(Vector2* pOut, Vector2* p0, Vector2* p1, Vector2* p2, Vector2* p3, float t, float tension);

    // Query path info

    [LibraryImport(LibName, EntryPoint = "c_iam_path_exists")]
    public static partial byte PathExists(uint pathId);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_length")]
    public static partial float PathLength(uint pathId);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_evaluate")]
    public static partial void PathEvaluate(Vector2* pOut, uint pathId, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_tangent")]
    public static partial void PathTangent(Vector2* pOut, uint pathId, float t);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_angle")]
    public static partial float PathAngle(uint pathId, float t);

    // Tween along a path

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_path")]
    public static partial void TweenPath(Vector2* pOut, uint id, uint channelId, uint pathId, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt = -1f);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_path_angle")]
    public static partial float TweenPathAngle(uint id, uint channelId, uint pathId, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, float dt = -1f);


    // ----------------------------------------------------
    // Arc-length parameterization (for constant-speed animation)
    // ----------------------------------------------------

    // Build arc-length lookup table for a path (call once per path, improves accuracy)

    [LibraryImport(LibName, EntryPoint = "c_iam_path_build_arc_lut")]
    public static partial void PathBuildArcLut(uint pathId, int subdivisions);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_has_arc_lut")]
    public static partial byte PathHasArcLut(uint pathId);

    // Distance-based path evaluation (uses arc-length LUT for constant speed)

    [LibraryImport(LibName, EntryPoint = "c_iam_path_distance_to_t")]
    public static partial float PathDistanceToT(uint pathId, float distance);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_evaluate_at_distance")]
    public static partial void PathEvaluateAtDistance(Vector2* pOut, uint pathId, float distance);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_angle_at_distance")]
    public static partial float PathAngleAtDistance(uint pathId, float distance);

    [LibraryImport(LibName, EntryPoint = "c_iam_path_tangent_at_distance")]
    public static partial void PathTangentAtDistance(Vector2* pOut, uint pathId, float distance);


    // ----------------------------------------------------
    // Path Morphing - interpolate between two paths
    // ----------------------------------------------------

    // Evaluate morphed path at parameter t [0,1] with blend factor [0,1]
    // path_a at blend=0, path_b at blend=1
    // Paths can have different numbers of segments - they are resampled to match
    [LibraryImport(LibName, EntryPoint = "c_iam_path_morph")]
    public static partial void PathMorph(Vector2* pOut, uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts* opts = null);

    // Get tangent of morphed path
    [LibraryImport(LibName, EntryPoint = "c_iam_path_morph_tangent")]
    public static partial void PathMorphTangent(Vector2* pOut, uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts* opts = null);

    // Get angle (radians) of morphed path
    [LibraryImport(LibName, EntryPoint = "c_iam_path_morph_angle")]
    public static partial float PathMorphAngle(uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts* opts = null);

    // Tween along a morphing path - animates both position along path AND the morph blend
    [LibraryImport(LibName, EntryPoint = "c_iam_tween_path_morph")]
    public static partial void TweenPathMorph(Vector2* pOut, uint id, uint channelId, uint pathA, uint pathB, float targetBlend, float dur, ImAnimEaseDesc* pathEase, ImAnimEaseDesc* morphEase, ImAnimPolicy policy, float dt, ImAnimMorphOpts* opts = null);

    // Get current morph blend value from a tween (for querying state)
    [LibraryImport(LibName, EntryPoint = "c_iam_get_morph_blend")]
    public static partial float GetMorphBlend(uint id, uint channelId);


    // ----------------------------------------------------
    // Text along motion paths
    // ----------------------------------------------------

    // Render text along a path (static - no animation)
    [LibraryImport(LibName, EntryPoint = "c_iam_text_path")]
    public static partial void TextPath(uint pathId, byte* text, ImAnimTextPathOpts* opts = null);

    // Animated text along path (characters appear progressively)
    [LibraryImport(LibName, EntryPoint = "c_iam_text_path_animated")]
    public static partial void TextPathAnimated(uint pathId, byte* text, float progress, ImAnimTextPathOpts* opts = null);

    // Helper: Get text width for path layout calculations
    [LibraryImport(LibName, EntryPoint = "c_iam_text_path_width")]
    public static partial float TextPathWidth(byte* text, ImAnimTextPathOpts* opts = null);


    // ----------------------------------------------------
    // Quad transform helpers (for advanced custom rendering)
    // ----------------------------------------------------

    // Transform a quad (4 vertices) by rotation and translation
    [LibraryImport(LibName, EntryPoint = "c_iam_transform_quad")]
    public static partial void TransformQuad(Quaternion* quad, Vector2* center, float angleRad, Vector2* translation);

    // Create a rotated quad for a glyph at a position on the path
    [LibraryImport(LibName, EntryPoint = "c_iam_make_glyph_quad")]
    public static partial void MakeGlyphQuad(Quaternion* quad, Vector2* pos, float angleRad, float glyphWidth, float glyphHeight, float baselineOffset);


    // ----------------------------------------------------
    // Text Stagger - per-character animation effects
    // ----------------------------------------------------

    // Render text with per-character stagger animation
    [LibraryImport(LibName, EntryPoint = "c_iam_text_stagger")]
    public static partial void TextStagger(uint id, byte* text, float progress, ImAnimTextStaggerOpts* opts = null);

    // Get text width for layout calculations
    [LibraryImport(LibName, EntryPoint = "c_iam_text_stagger_width")]
    public static partial float TextStaggerWidth(byte* text, ImAnimTextStaggerOpts* opts = null);

    // Get total animation duration for text (accounts for stagger delays)
    [LibraryImport(LibName, EntryPoint = "c_iam_text_stagger_duration")]
    public static partial float TextStaggerDuration(byte* text, ImAnimTextStaggerOpts* opts = null);


    // ----------------------------------------------------
    // Noise Channels - Perlin/Simplex noise for organic movement
    // ----------------------------------------------------

    // Sample noise at a point (returns value in [-1, 1])

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_2d")]
    public static partial float Noise2D(float x, float y, ImAnimNoiseOpts* opts = null);

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_3d")]
    public static partial float Noise3D(float x, float y, float z, ImAnimNoiseOpts* opts = null);

    // Animated noise channels - continuous noise that evolves over time

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_channel_float")]
    public static partial float NoiseChannelFloat(uint id, float frequency, float amplitude, ImAnimNoiseOpts* opts, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_channel_vec2")]
    public static partial void NoiseChannelVec2(Vector2* pOut, uint id, Vector2* frequency, Vector2* amplitude, ImAnimNoiseOpts* opts, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_channel_vec4")]
    public static partial void NoiseChannelVec4(Vector4* pOut, uint id, Vector4* frequency, Vector4* amplitude, ImAnimNoiseOpts* opts, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_noise_channel_color")]
    public static partial void NoiseChannelColor(Vector4* pOut, uint id, Vector4* baseColor, Vector4* amplitude, float frequency, ImAnimNoiseOpts* opts, ImAnimColorSpace colorSpace, float dt);

    // Convenience: smooth random movement (like wiggle but using noise)

    [LibraryImport(LibName, EntryPoint = "c_iam_smooth_noise_float")]
    public static partial float SmoothNoiseFloat(uint id, float amplitude, float speed, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_smooth_noise_vec2")]
    public static partial void SmoothNoiseVec2(Vector2* pOut, uint id, Vector2* amplitude, float speed, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_smooth_noise_vec4")]
    public static partial void SmoothNoiseVec4(Vector4* pOut, uint id, Vector4* amplitude, float speed, float dt);

    [LibraryImport(LibName, EntryPoint = "c_iam_smooth_noise_color")]
    public static partial void SmoothNoiseColor(Vector4* pOut, uint id, Vector4* baseColor, Vector4* amplitude, float speed, ImAnimColorSpace colorSpace, float dt);


    // ----------------------------------------------------
    // Style Interpolation - animate between ImGuiStyle themes
    // ----------------------------------------------------

    // Register a named style for interpolation
    [LibraryImport(LibName, EntryPoint = "c_iam_style_register")]
    public static partial void StyleRegister(uint styleId, ImGuiStyle* style);

    [LibraryImport(LibName, EntryPoint = "c_iam_style_register_current")]
    public static partial void StyleRegisterCurrent(uint styleId);

    // Blend between two registered styles (result applied to ImGui::GetStyle())
    // Uses iam_color_space for color blending mode (iam_col_oklab recommended)
    [LibraryImport(LibName, EntryPoint = "c_iam_style_blend")]
    public static partial void StyleBlend(uint styleA, uint styleB, float t, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab);

    // Tween between styles over time
    [LibraryImport(LibName, EntryPoint = "c_iam_style_tween")]
    public static partial void StyleTween(uint id, uint targetStyle, float duration, ImAnimEaseDesc* ease, ImAnimColorSpace colorSpace, float dt);

    // Get interpolated style without applying
    [LibraryImport(LibName, EntryPoint = "c_iam_style_blend_to")]
    public static partial void StyleBlendTo(uint styleA, uint styleB, float t, ImGuiStyle* outStyle, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab);

    // Check if a style is registered
    [LibraryImport(LibName, EntryPoint = "c_iam_style_exists")]
    public static partial byte StyleExists(uint styleId);

    // Remove a registered style
    [LibraryImport(LibName, EntryPoint = "c_iam_style_unregister")]
    public static partial void StyleUnregister(uint styleId);


    // ----------------------------------------------------
    // Gradient Interpolation - animate between color gradients
    // ----------------------------------------------------

    // Blend between two gradients
    [LibraryImport(LibName, EntryPoint = "c_iam_gradient_lerp")]
    public static partial void GradientLerp(ImAnimGradient* pOut, ImAnimGradient* a, ImAnimGradient* b, float t, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab);

    [LibraryImport(LibName, EntryPoint = "c_iam_tween_gradient")]
    public static partial void TweenGradient(ImAnimGradient* pOut, uint id, uint channelId, ImAnimGradient* target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt);


    // ----------------------------------------------------
    // Transform Interpolation - animate 2D transforms
    // ----------------------------------------------------

    // Blend between two transforms with rotation interpolation
    [LibraryImport(LibName, EntryPoint = "c_iam_transform_lerp")]
    public static partial void TransformLerp(ImAnimTransform* pOut, ImAnimTransform* a, ImAnimTransform* b, float t, ImAnimRotationMode rotationMode = ImAnimRotationMode.Shortest);

    // Tween between transforms over time
    [LibraryImport(LibName, EntryPoint = "c_iam_tween_transform")]
    public static partial void TweenTransform(ImAnimTransform* pOut, uint id, uint channelId, ImAnimTransform* target, float dur, ImAnimEaseDesc* ez, ImAnimPolicy policy, int rotationMode, float dt);

    // Decompose a 3x2 matrix into transform components
    [LibraryImport(LibName, EntryPoint = "c_iam_transform_from_matrix")]
    public static partial void TransformFromMatrix(ImAnimTransform* pOut, float m00, float m01, float m10, float m11, float tx, float ty);

    // Convert transform to 3x2 matrix (row-major: [m00 m01 tx; m10 m11 ty])
    [LibraryImport(LibName, EntryPoint = "c_iam_transform_to_matrix")]
    public static partial void TransformToMatrix(ImAnimTransform* t, Matrix3x2* outMatrix);


    // ----------------------------------------------------
    // iam_clip - fluent API for authoring animations
    // ----------------------------------------------------

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_begin")]
    public static partial void ClipBegin(ImAnimClip* pOut, uint clipId);

    // Add keyframes for different channel types

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_float")]
    public static partial void ClipKeyFloat(ImAnimClip* self, uint channel, float time, float value, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec2")]
    public static partial void ClipKeyVec2(ImAnimClip* self, uint channel, float time, Vector2* value, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec4")]
    public static partial void ClipKeyVec4(ImAnimClip* self, uint channel, float time, Vector4* value, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_int")]
    public static partial void ClipKeyInt(ImAnimClip* self, uint channel, float time, int value, ImAnimEaseType easeType);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_color")]
    public static partial void ClipKeyColor(ImAnimClip* self, uint channel, float time, Vector4* value, ImAnimColorSpace colorSpace, ImAnimEaseType easeType, Vector4* bezier4);

    // Keyframes with repeat variation (value changes per loop iteration)

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_float_var")]
    public static partial void ClipKeyFloatVar(ImAnimClip* self, uint channel, float time, float value, ImAnimVariationFloat* var, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec2_var")]
    public static partial void ClipKeyVec2Var(ImAnimClip* self, uint channel, float time, Vector2* value, ImAnimVariationVec2* var, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec4_var")]
    public static partial void ClipKeyVec4Var(ImAnimClip* self, uint channel, float time, Vector4* value, ImAnimVariationVec4* var, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_int_var")]
    public static partial void ClipKeyIntVar(ImAnimClip* self, uint channel, float time, int value, ImAnimVariationInt* var, ImAnimEaseType easeType);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_color_var")]
    public static partial void ClipKeyColorVar(ImAnimClip* self, uint channel, float time, Vector4* value, ImAnimVariationColor* var, ImAnimColorSpace colorSpace, ImAnimEaseType easeType, Vector4* bezier4);

    // Spring-based keyframe (float only)

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_float_spring")]
    public static partial void ClipKeyFloatSpring(ImAnimClip* self, uint channel, float time, float target, ImAnimSpringParams* spring);

    // Anchor-relative keyframes (values resolved relative to window/viewport at get time)

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_float_rel")]
    public static partial void ClipKeyFloatRel(ImAnimClip* self, uint channel, float time, float percent, float pxBias, ImAnimAnchorSpace anchorSpace, int axis, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec2_rel")]
    public static partial void ClipKeyVec2Rel(ImAnimClip* self, uint channel, float time, Vector2* percent, Vector2* pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_vec4_rel")]
    public static partial void ClipKeyVec4Rel(ImAnimClip* self, uint channel, float time, Vector4* percent, Vector4* pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4* bezier4);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_key_color_rel")]
    public static partial void ClipKeyColorRel(ImAnimClip* self, uint channel, float time, Vector4* percent, Vector4* pxBias, ImAnimColorSpace colorSpace, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4* bezier4);

    // Timeline grouping - sequential and parallel keyframe blocks

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_seq_begin")]
    public static partial void ClipSeqBegin(ImAnimClip* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_seq_end")]
    public static partial void ClipSeqEnd(ImAnimClip* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_par_begin")]
    public static partial void ClipParBegin(ImAnimClip* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_par_end")]
    public static partial void ClipParEnd(ImAnimClip* self);

    // Timeline markers - callbacks at specific times during playback

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_marker_id")]
    public static partial void ClipMarkerId(ImAnimClip* self, float time, uint markerId, delegate* unmanaged[Cdecl]<uint, uint, float, void*, void> cb, void* userData);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_marker")]
    public static partial void ClipMarker(ImAnimClip* self, float time, delegate* unmanaged[Cdecl]<uint, uint, float, void*, void> cb, void* userData);

    // Clip options

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_loop")]
    public static partial void ClipSetLoop(ImAnimClip* self, byte loop, ImAnimDirection direction, int loopCount);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_delay")]
    public static partial void ClipSetDelay(ImAnimClip* self, float delaySeconds);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_stagger")]
    public static partial void ClipSetStagger(ImAnimClip* self, int count, float eachDelay, float fromCenterBias);

    // Timing variation per loop iteration

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_duration_var")]
    public static partial void ClipSetDurationVar(ImAnimClip* self, ImAnimVariationFloat* var);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_delay_var")]
    public static partial void ClipSetDelayVar(ImAnimClip* self, ImAnimVariationFloat* var);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_set_timescale_var")]
    public static partial void ClipSetTimescaleVar(ImAnimClip* self, ImAnimVariationFloat* var);

    // Callbacks

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_on_begin")]
    public static partial void ClipOnBegin(ImAnimClip* self, delegate* unmanaged[Cdecl]<uint, void*, void> cb, void* userData);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_on_update")]
    public static partial void ClipOnUpdate(ImAnimClip* self, delegate* unmanaged[Cdecl]<uint, void*, void> cb, void* userData);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_on_complete")]
    public static partial void ClipOnComplete(ImAnimClip* self, delegate* unmanaged[Cdecl]<uint, void*, void> cb, void* userData);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_end")]
    public static partial void ClipEnd(ImAnimClip* self);


    // ----------------------------------------------------
    // iam_instance - playback control for a clip
    // ----------------------------------------------------

    // Playback control

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_pause")]
    public static partial void InstancePause(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_resume")]
    public static partial void InstanceResume(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_stop")]
    public static partial void InstanceStop(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_destroy_playback")]
    public static partial void InstanceDestroyPlayback(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_seek")]
    public static partial void InstanceSeek(ImAnimInstance* self, float time);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_set_time_scale")]
    public static partial void InstanceSetTimeScale(ImAnimInstance* self, float scale);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_set_weight")]
    public static partial void InstanceSetWeight(ImAnimInstance* self, float weight);

    // Animation chaining - play another clip when this one completes

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_then")]
    public static partial void InstanceThen(ImAnimInstance* pOut, ImAnimInstance* self, uint nextClipId);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_then_id")]
    public static partial void InstanceThenId(ImAnimInstance* pOut, ImAnimInstance* self, uint nextClipId, uint nextInstanceId);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_then_delay")]
    public static partial void InstanceThenDelay(ImAnimInstance* pOut, ImAnimInstance* self, float delay);

    // Query state

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_time")]
    public static partial float InstanceTime(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_duration")]
    public static partial float InstanceDuration(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_is_playing")]
    public static partial byte InstanceIsPlaying(ImAnimInstance* self);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_is_paused")]
    public static partial byte InstanceIsPaused(ImAnimInstance* self);

    // Get animated values

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_get_float")]
    public static partial byte InstanceGetFloat(ImAnimInstance* self, uint channel, float* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_get_vec2")]
    public static partial byte InstanceGetVec2(ImAnimInstance* self, uint channel, Vector2* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_get_vec4")]
    public static partial byte InstanceGetVec4(ImAnimInstance* self, uint channel, Vector4* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_get_int")]
    public static partial byte InstanceGetInt(ImAnimInstance* self, uint channel, int* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_get_color")]
    public static partial byte InstanceGetColor(ImAnimInstance* self, uint channel, Vector4* outVal, ImAnimColorSpace colorSpace);

    // Check validity

    [LibraryImport(LibName, EntryPoint = "c_iam_instance_valid")]
    public static partial byte InstanceValid(ImAnimInstance* self);


    // ----------------------------------------------------
    // Clip System API
    // ----------------------------------------------------

    // Initialize/shutdown (optional - auto-init on first use)
    [LibraryImport(LibName, EntryPoint = "c_iam_clip_init")]
    public static partial void ClipInit(int initialClipCap, int initialInstCap);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_shutdown")]
    public static partial void ClipShutdown();

    // c_Per-frame update (call after iam_update_begin_frame)
    [LibraryImport(LibName, EntryPoint = "c_iam_clip_update")]
    public static partial void ClipUpdate(float dt);

    // Garbage collection for instances
    [LibraryImport(LibName, EntryPoint = "c_iam_clip_gc")]
    public static partial void ClipGc(uint maxAgeFrames);

    // Play a clip on an instance (creates or reuses instance)
    [LibraryImport(LibName, EntryPoint = "c_iam_play")]
    public static partial void Play(ImAnimInstance* pOut, uint clipId, uint instanceId);

    // c_Get an existing instance (returns invalid iam_instance if not found)
    [LibraryImport(LibName, EntryPoint = "c_iam_get_instance")]
    public static partial void GetInstance(ImAnimInstance* pOut, uint instanceId);

    // Query clip info
    [LibraryImport(LibName, EntryPoint = "c_iam_clip_duration")]
    public static partial float ClipDuration(uint clipId);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_exists")]
    public static partial byte ClipExists(uint clipId);

    // Stagger helpers - compute delay for indexed instances
    [LibraryImport(LibName, EntryPoint = "c_iam_stagger_delay")]
    public static partial float StaggerDelay(uint clipId, int index);

    [LibraryImport(LibName, EntryPoint = "c_iam_play_stagger")]
    public static partial void PlayStagger(ImAnimInstance* pOut, uint clipId, uint instanceId, int index);

    // Layering support - blend multiple animation instances
    [LibraryImport(LibName, EntryPoint = "c_iam_layer_begin")]
    public static partial void LayerBegin(uint instanceId);

    [LibraryImport(LibName, EntryPoint = "c_iam_layer_add")]
    public static partial void LayerAdd(ImAnimInstance* inst, float weight);

    [LibraryImport(LibName, EntryPoint = "c_iam_layer_end")]
    public static partial void LayerEnd(uint instanceId);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_blended_float")]
    public static partial byte GetBlendedFloat(uint instanceId, uint channel, float* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_blended_vec2")]
    public static partial byte GetBlendedVec2(uint instanceId, uint channel, Vector2* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_blended_vec4")]
    public static partial byte GetBlendedVec4(uint instanceId, uint channel, Vector4* outVal);

    [LibraryImport(LibName, EntryPoint = "c_iam_get_blended_int")]
    public static partial byte GetBlendedInt(uint instanceId, uint channel, int* outVal);

    // Persistence (optional)
    [LibraryImport(LibName, EntryPoint = "c_iam_clip_save")]
    public static partial ImAnimResult ClipSave(uint clipId, byte* path);

    [LibraryImport(LibName, EntryPoint = "c_iam_clip_load")]
    public static partial ImAnimResult ClipLoad(byte* path, uint* outClipId);
}
