#include "cimanim.h"

// ----------------------------------------------------
// Public API declarations
// ----------------------------------------------------

CIMGUI_API void   c_iam_set_imgui_context(ImGuiContext* context) { ImGui::SetCurrentContext(context); }
CIMGUI_API void   c_iam_demo_window() { ImAnimDemoWindow(); }

// Frame management
CIMGUI_API void   c_iam_update_begin_frame() { iam_update_begin_frame(); }
CIMGUI_API void   c_iam_gc(unsigned int max_age_frames) { iam_gc(max_age_frames); }
CIMGUI_API void   c_iam_reserve(int cap_float, int cap_vec2, int cap_vec4, int cap_int, int cap_color) { iam_reserve(cap_float, cap_vec2, cap_vec4, cap_int, cap_color); }
CIMGUI_API void   c_iam_set_ease_lut_samples(int count) { iam_set_ease_lut_samples(count); }

// Global time scale (for slow-motion / fast-forward debugging)
CIMGUI_API void   c_iam_set_global_time_scale(float scale) { iam_set_global_time_scale(scale); }
CIMGUI_API float  c_iam_get_global_time_scale() { return iam_get_global_time_scale(); }

// Lazy Initialization - defer channel creation until animation is needed
CIMGUI_API void   c_iam_set_lazy_init(bool enable) { iam_set_lazy_init(enable); }
CIMGUI_API bool   c_iam_is_lazy_init_enabled() { return iam_is_lazy_init_enabled(); }

CIMGUI_API void   c_iam_register_custom_ease(int slot, iam_ease_fn fn) { iam_register_custom_ease(slot, fn); }
CIMGUI_API void   c_iam_get_custom_ease(iam_ease_fn* pOut, int slot) { *pOut = iam_get_custom_ease(slot); }

// Debug UI
CIMGUI_API void   c_iam_show_unified_inspector(bool* p_open) { iam_show_unified_inspector(p_open); }

// Performance Profiler
CIMGUI_API void   c_iam_profiler_enable(bool enable) { iam_profiler_enable(enable); }
CIMGUI_API bool   c_iam_profiler_is_enabled() { return iam_profiler_is_enabled(); }
CIMGUI_API void   c_iam_profiler_begin_frame() { iam_profiler_begin_frame(); }
CIMGUI_API void   c_iam_profiler_end_frame() { iam_profiler_end_frame(); }
CIMGUI_API void   c_iam_profiler_begin(const char* name) { iam_profiler_begin(name); }
CIMGUI_API void   c_iam_profiler_end() { iam_profiler_end(); }

// Drag Feedback - animated feedback for drag operations
CIMGUI_API void   c_iam_drag_begin(iam_drag_feedback* pOut, ImGuiID id, ImVec2* pos) { *pOut = iam_drag_begin(id, *pos); }
CIMGUI_API void   c_iam_drag_update(iam_drag_feedback* pOut, ImGuiID id, ImVec2* pos, float dt) { *pOut = iam_drag_update(id, *pos, dt); }
CIMGUI_API void   c_iam_drag_release(iam_drag_feedback* pOut, ImGuiID id, ImVec2* pos, iam_drag_opts* opts, float dt) { *pOut = iam_drag_release(id, *pos, opts ? *opts : iam_drag_opts(), dt); }
CIMGUI_API void   c_iam_drag_cancel(ImGuiID id) { iam_drag_cancel(id); }

// Oscillators - continuous periodic animations
CIMGUI_API float  c_iam_oscillate(ImGuiID id, float amplitude, float frequency, int wave_type, float phase, float dt) { return iam_oscillate(id, amplitude, frequency, wave_type, phase, dt); }
CIMGUI_API int    c_iam_oscillate_int(ImGuiID id, int amplitude, float frequency, int wave_type, float phase, float dt) { return iam_oscillate_int(id, amplitude, frequency, wave_type, phase, dt); }
CIMGUI_API void   c_iam_oscillate_vec2(ImVec2* pOut, ImGuiID id, ImVec2* amplitude, ImVec2* frequency, int wave_type, ImVec2* phase, float dt) { *pOut = iam_oscillate_vec2(id, *amplitude, *frequency, wave_type, *phase, dt); }
CIMGUI_API void   c_iam_oscillate_vec4(ImVec4* pOut, ImGuiID id, ImVec4* amplitude, ImVec4* frequency, int wave_type, ImVec4* phase, float dt) { *pOut = iam_oscillate_vec4(id, *amplitude, *frequency, wave_type, *phase, dt); }
CIMGUI_API void   c_iam_oscillate_color(ImVec4* pOut, ImGuiID id, ImVec4* base_color, ImVec4* amplitude, float frequency, int wave_type, float phase, int color_space, float dt) { *pOut = iam_oscillate_color(id, *base_color, *amplitude, frequency, wave_type, phase, color_space, dt); }

// Shake/Wiggle - procedural noise animations
CIMGUI_API float  c_iam_shake(ImGuiID id, float intensity, float frequency, float decay_time, float dt) { return iam_shake(id, intensity, frequency, decay_time, dt); }
CIMGUI_API int    c_iam_shake_int(ImGuiID id, int intensity, float frequency, float decay_time, float dt) { return iam_shake_int(id, intensity, frequency, decay_time, dt); }
CIMGUI_API void   c_iam_shake_vec2(ImVec2* pOut, ImGuiID id, ImVec2* intensity, float frequency, float decay_time, float dt) { *pOut = iam_shake_vec2(id, *intensity, frequency, decay_time, dt); }
CIMGUI_API void   c_iam_shake_vec4(ImVec4* pOut, ImGuiID id, ImVec4* intensity, float frequency, float decay_time, float dt) { *pOut = iam_shake_vec4(id, *intensity, frequency, decay_time, dt); }
CIMGUI_API void   c_iam_shake_color(ImVec4* pOut, ImGuiID id, ImVec4* base_color, ImVec4* intensity, float frequency, float decay_time, int color_space, float dt) { *pOut = iam_shake_color(id, *base_color, *intensity, frequency, decay_time, color_space, dt); }
CIMGUI_API float  c_iam_wiggle(ImGuiID id, float amplitude, float frequency, float dt) { return iam_wiggle(id, amplitude, frequency, dt); }
CIMGUI_API int    c_iam_wiggle_int(ImGuiID id, int amplitude, float frequency, float dt) { return iam_wiggle_int(id, amplitude, frequency, dt); }
CIMGUI_API void   c_iam_wiggle_vec2(ImVec2* pOut, ImGuiID id, ImVec2* amplitude, float frequency, float dt) { *pOut = iam_wiggle_vec2(id, *amplitude, frequency, dt); }
CIMGUI_API void   c_iam_wiggle_vec4(ImVec4* pOut, ImGuiID id, ImVec4* amplitude, float frequency, float dt) { *pOut = iam_wiggle_vec4(id, *amplitude, frequency, dt); }
CIMGUI_API void   c_iam_wiggle_color(ImVec4* pOut, ImGuiID id, ImVec4* base_color, ImVec4* amplitude, float frequency, int color_space, float dt) { *pOut = iam_wiggle_color(id, *base_color, *amplitude, frequency, color_space, dt); }
CIMGUI_API void   c_iam_trigger_shake(ImGuiID id) { iam_trigger_shake(id); }

// Easing evaluation
CIMGUI_API float  c_iam_eval_preset(int type, float t) { return iam_eval_preset(type, t); }

// Tween API - smoothly interpolate values over time
CIMGUI_API float  c_iam_tween_float(ImGuiID id, ImGuiID channel_id, float target, float dur, iam_ease_desc* ez, int policy, float dt) { return iam_tween_float(id, channel_id, target, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_vec2(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, ImVec2* target, float dur, iam_ease_desc* ez, int policy, float dt) { *pOut = iam_tween_vec2(id, channel_id, *target, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_vec4(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* target, float dur, iam_ease_desc* ez, int policy, float dt) { *pOut = iam_tween_vec4(id, channel_id, *target, dur, *ez, policy, dt); }
CIMGUI_API int    c_iam_tween_int(ImGuiID id, ImGuiID channel_id, int target, float dur, iam_ease_desc* ez, int policy, float dt) { return iam_tween_int(id, channel_id, target, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_color(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* target_srgb, float dur, iam_ease_desc* ez, int policy, int color_space, float dt) { *pOut = iam_tween_color(id, channel_id, *target_srgb, dur, *ez, policy, color_space, dt); }

// Resize-friendly helpers
CIMGUI_API void   c_iam_anchor_size(ImVec2* pOut, int space) { *pOut = iam_anchor_size(space); }

// Relative target tweens (percent of anchor + pixel offset) - survive window resizes
CIMGUI_API float  c_iam_tween_float_rel(ImGuiID id, ImGuiID channel_id, float percent, float px_bias, float dur, iam_ease_desc* ez, int policy, int anchor_space, int axis, float dt) { return iam_tween_float_rel(id, channel_id, percent, px_bias, dur, *ez, policy, anchor_space, axis, dt); }
CIMGUI_API void   c_iam_tween_vec2_rel(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, ImVec2* percent, ImVec2* px_bias, float dur, iam_ease_desc* ez, int policy, int anchor_space, float dt) { *pOut = iam_tween_vec2_rel(id, channel_id, *percent, *px_bias, dur, *ez, policy, anchor_space, dt); }
CIMGUI_API void   c_iam_tween_vec4_rel(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* percent, ImVec4* px_bias, float dur, iam_ease_desc* ez, int policy, int anchor_space, float dt) { *pOut = iam_tween_vec4_rel(id, channel_id, *percent, *px_bias, dur, *ez, policy, anchor_space, dt); }
CIMGUI_API void   c_iam_tween_color_rel(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* percent, ImVec4* px_bias, float dur, iam_ease_desc* ez, int policy, int color_space, int anchor_space, float dt) { *pOut = iam_tween_color_rel(id, channel_id, *percent, *px_bias, dur, *ez, policy, color_space, anchor_space, dt); }

// Rebase functions - change target of in-progress animation without restarting
CIMGUI_API void   c_iam_rebase_float(ImGuiID id, ImGuiID channel_id, float new_target, float dt) { iam_rebase_float(id, channel_id, new_target, dt); }
CIMGUI_API void   c_iam_rebase_vec2(ImGuiID id, ImGuiID channel_id, ImVec2* new_target, float dt) { iam_rebase_vec2(id, channel_id, *new_target, dt); }
CIMGUI_API void   c_iam_rebase_vec4(ImGuiID id, ImGuiID channel_id, ImVec4* new_target, float dt) { iam_rebase_vec4(id, channel_id, *new_target, dt); }
CIMGUI_API void   c_iam_rebase_color(ImGuiID id, ImGuiID channel_id, ImVec4* new_target, float dt) { iam_rebase_color(id, channel_id, *new_target, dt); }
CIMGUI_API void   c_iam_rebase_int(ImGuiID id, ImGuiID channel_id, int new_target, float dt) { iam_rebase_int(id, channel_id, new_target, dt); }

// Resolved tweens - target computed dynamically by callback each frame
CIMGUI_API float  c_iam_tween_float_resolved(ImGuiID id, ImGuiID channel_id, iam_float_resolver fn, void* user, float dur, iam_ease_desc* ez, int policy, float dt) { return iam_tween_float_resolved(id, channel_id, fn, user, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_vec2_resolved(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, iam_vec2_resolver fn, void* user, float dur, iam_ease_desc* ez, int policy, float dt) { *pOut = iam_tween_vec2_resolved(id, channel_id, fn, user, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_vec4_resolved(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, iam_vec4_resolver fn, void* user, float dur, iam_ease_desc* ez, int policy, float dt) { *pOut = iam_tween_vec4_resolved(id, channel_id, fn, user, dur, *ez, policy, dt); }
CIMGUI_API void   c_iam_tween_color_resolved(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, iam_vec4_resolver fn, void* user, float dur, iam_ease_desc* ez, int policy, int color_space, float dt) { *pOut = iam_tween_color_resolved(id, channel_id, fn, user, dur, *ez, policy, color_space, dt); }
CIMGUI_API int    c_iam_tween_int_resolved(ImGuiID id, ImGuiID channel_id, iam_int_resolver fn, void* user, float dur, iam_ease_desc* ez, int policy, float dt) { return iam_tween_int_resolved(id, channel_id, fn, user, dur, *ez, policy, dt); }

// Color blending utility
CIMGUI_API void   c_iam_get_blended_color(ImVec4* pOut, ImVec4* a_srgb, ImVec4* b_srgb, float t, int color_space) { *pOut = iam_get_blended_color(*a_srgb, *b_srgb, t, color_space); }


// ----------------------------------------------------
// Convenience shorthands for common easings
// ----------------------------------------------------

CIMGUI_API void   c_iam_ease_preset(iam_ease_desc* pOut, int type) { *pOut = iam_ease_preset(type); }
CIMGUI_API void   c_iam_ease_bezier(iam_ease_desc* pOut, float x1, float y1, float x2, float y2) { *pOut = iam_ease_bezier(x1, y1, x2, y2); }
CIMGUI_API void   c_iam_ease_steps_desc(iam_ease_desc* pOut, int steps, int mode) { *pOut = iam_ease_steps_desc(steps, mode); }
CIMGUI_API void   c_iam_ease_back(iam_ease_desc* pOut, float overshoot) { *pOut = iam_ease_back(overshoot); }
CIMGUI_API void   c_iam_ease_elastic(iam_ease_desc* pOut, float amplitude, float period) { *pOut = iam_ease_elastic(amplitude, period); }
CIMGUI_API void   c_iam_ease_spring_desc(iam_ease_desc* pOut, float mass, float stiffness, float damping, float v0) { *pOut = iam_ease_spring_desc(mass, stiffness, damping, v0); }
CIMGUI_API void   c_iam_ease_custom_fn(iam_ease_desc* pOut, int slot) { *pOut = iam_ease_custom_fn(slot); }

// Scroll animation - smooth scrolling for ImGui windows
CIMGUI_API void   c_iam_scroll_to_y(float target_y, float duration, iam_ease_desc* ez) { iam_scroll_to_y(target_y, duration, *ez); }
CIMGUI_API void   c_iam_scroll_to_x(float target_x, float duration, iam_ease_desc* ez) { iam_scroll_to_x(target_x, duration, *ez); }
CIMGUI_API void   c_iam_scroll_to_top(float duration, iam_ease_desc* ez) { iam_scroll_to_top(duration, *ez); }
CIMGUI_API void   c_iam_scroll_to_bottom(float duration, iam_ease_desc* ez) { iam_scroll_to_bottom(duration, *ez); }


// ----------------------------------------------------
// Per-axis easing - different easing per component
// ----------------------------------------------------

// Tween with per-axis easing - each component uses its own easing curve
CIMGUI_API void   c_iam_tween_vec2_per_axis(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, ImVec2* target, float dur, iam_ease_per_axis* ez, int policy, float dt) { *pOut = iam_tween_vec2_per_axis(id, channel_id, *target, dur, ez ? *ez : iam_ease_per_axis(), policy, dt); }
CIMGUI_API void   c_iam_tween_vec4_per_axis(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* target, float dur, iam_ease_per_axis* ez, int policy, float dt) { *pOut = iam_tween_vec4_per_axis(id, channel_id, *target, dur, ez ? *ez : iam_ease_per_axis(), policy, dt); }
CIMGUI_API void   c_iam_tween_color_per_axis(ImVec4* pOut, ImGuiID id, ImGuiID channel_id, ImVec4* target_srgb, float dur, iam_ease_per_axis* ez, int policy, int color_space, float dt) { *pOut = iam_tween_color_per_axis(id, channel_id, *target_srgb, dur, ez ? *ez : iam_ease_per_axis(), policy, color_space, dt); }


// ----------------------------------------------------
// Motion Paths - animate along curves and splines
// ----------------------------------------------------

// Single-curve evaluation functions (stateless, for direct use)
CIMGUI_API void   c_iam_bezier_quadratic(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, float t) { *pOut = iam_bezier_quadratic(*p0, *p1, *p2, t); }
CIMGUI_API void   c_iam_bezier_cubic(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, ImVec2* p3, float t) { *pOut = iam_bezier_cubic(*p0, *p1, *p2, *p3, t); }
CIMGUI_API void   c_iam_catmull_rom(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, ImVec2* p3, float t, float tension) { *pOut = iam_catmull_rom(*p0, *p1, *p2, *p3, t, tension); }

// Derivatives (for tangent/velocity)
CIMGUI_API void   c_iam_bezier_quadratic_deriv(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, float t) { *pOut = iam_bezier_quadratic_deriv(*p0, *p1, *p2, t); }
CIMGUI_API void   c_iam_bezier_cubic_deriv(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, ImVec2* p3, float t) { *pOut = iam_bezier_cubic_deriv(*p0, *p1, *p2, *p3, t); }
CIMGUI_API void   c_iam_catmull_rom_deriv(ImVec2* pOut, ImVec2* p0, ImVec2* p1, ImVec2* p2, ImVec2* p3, float t, float tension) { *pOut = iam_catmull_rom_deriv(*p0, *p1, *p2, *p3, t, tension); }

// Query path info
CIMGUI_API bool   c_iam_path_exists(ImGuiID path_id) { return iam_path_exists(path_id); }
CIMGUI_API float  c_iam_path_length(ImGuiID path_id) { return iam_path_length(path_id); }
CIMGUI_API void   c_iam_path_evaluate(ImVec2* pOut, ImGuiID path_id, float t) { *pOut = iam_path_evaluate(path_id, t); }
CIMGUI_API void   c_iam_path_tangent(ImVec2* pOut, ImGuiID path_id, float t) { *pOut = iam_path_tangent(path_id, t); }
CIMGUI_API float  c_iam_path_angle(ImGuiID path_id, float t) { return iam_path_angle(path_id, t); }

// Tween along a path
CIMGUI_API void   c_iam_tween_path(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, ImGuiID path_id, float dur, iam_ease_desc* ez, int policy, float dt) { *pOut = iam_tween_path(id, channel_id, path_id, dur, *ez, policy, dt); }
CIMGUI_API float  c_iam_tween_path_angle(ImGuiID id, ImGuiID channel_id, ImGuiID path_id, float dur, iam_ease_desc* ez, int policy, float dt) { return iam_tween_path_angle(id, channel_id, path_id, dur, *ez, policy, dt); }


// ----------------------------------------------------
// Arc-length parameterization (for constant-speed animation)
// ----------------------------------------------------

// Build arc-length lookup table for a path (call once per path, improves accuracy)
CIMGUI_API void   c_iam_path_build_arc_lut(ImGuiID path_id, int subdivisions) { iam_path_build_arc_lut(path_id, subdivisions); }
CIMGUI_API bool   c_iam_path_has_arc_lut(ImGuiID path_id) { return iam_path_has_arc_lut(path_id); }

// Distance-based path evaluation (uses arc-length LUT for constant speed)
CIMGUI_API float  c_iam_path_distance_to_t(ImGuiID path_id, float distance) { return iam_path_distance_to_t(path_id, distance); }
CIMGUI_API void   c_iam_path_evaluate_at_distance(ImVec2* pOut, ImGuiID path_id, float distance) { *pOut = iam_path_evaluate_at_distance(path_id, distance); }
CIMGUI_API float  c_iam_path_angle_at_distance(ImGuiID path_id, float distance) { return iam_path_angle_at_distance(path_id, distance); }
CIMGUI_API void   c_iam_path_tangent_at_distance(ImVec2* pOut, ImGuiID path_id, float distance) { *pOut = iam_path_tangent_at_distance(path_id, distance); }


// ----------------------------------------------------
// Path Morphing - interpolate between two paths
// ----------------------------------------------------

// Evaluate morphed path at parameter t [0,1] with blend factor [0,1]
// path_a at blend=0, path_b at blend=1
// Paths can have different numbers of segments - they are resampled to match
CIMGUI_API void   c_iam_path_morph(ImVec2* pOut, ImGuiID path_a, ImGuiID path_b, float t, float blend, iam_morph_opts* opts) { *pOut = iam_path_morph(path_a, path_b, t, blend, opts ? *opts : iam_morph_opts()); }

// Get tangent of morphed path
CIMGUI_API void   c_iam_path_morph_tangent(ImVec2* pOut, ImGuiID path_a, ImGuiID path_b, float t, float blend, iam_morph_opts* opts) { *pOut = iam_path_morph_tangent(path_a, path_b, t, blend, opts ? *opts : iam_morph_opts()); }

// Get angle (radians) of morphed path
CIMGUI_API float  c_iam_path_morph_angle(ImGuiID path_a, ImGuiID path_b, float t, float blend, iam_morph_opts* opts) { return iam_path_morph_angle(path_a, path_b, t, blend, opts ? *opts : iam_morph_opts()); }

// Tween along a morphing path - animates both position along path AND the morph blend
CIMGUI_API void   c_iam_tween_path_morph(ImVec2* pOut, ImGuiID id, ImGuiID channel_id, ImGuiID path_a, ImGuiID path_b, float target_blend, float dur, iam_ease_desc* path_ease, iam_ease_desc* morph_ease, int policy, float dt, iam_morph_opts* opts) {
    *pOut = iam_tween_path_morph(id, channel_id, path_a, path_b, target_blend, dur, *path_ease, *morph_ease, policy, dt, opts ? *opts : iam_morph_opts());
}

// Get current morph blend value from a tween (for querying state)
CIMGUI_API float  c_iam_get_morph_blend(ImGuiID id, ImGuiID channel_id) { return iam_get_morph_blend(id, channel_id); }


// ----------------------------------------------------
// Text along motion paths
// ----------------------------------------------------

// Render text along a path (static - no animation)
CIMGUI_API void   c_iam_text_path(ImGuiID path_id, const char* text, iam_text_path_opts* opts) { iam_text_path(path_id, text, opts ? *opts : iam_text_path_opts()); }

// Animated text along path (characters appear progressively)
CIMGUI_API void   c_iam_text_path_animated(ImGuiID path_id, const char* text, float progress, iam_text_path_opts* opts) { return iam_text_path_animated(path_id, text, progress, opts ? *opts : iam_text_path_opts()); }

// Helper: Get text width for path layout calculations
CIMGUI_API float  c_iam_text_path_width(const char* text, iam_text_path_opts* opts) { return iam_text_path_width(text, opts ? *opts : iam_text_path_opts()); }


// ----------------------------------------------------
// Quad transform helpers (for advanced custom rendering)
// ----------------------------------------------------

// Transform a quad (4 vertices) by rotation and translation
CIMGUI_API void   c_iam_transform_quad(ImVec2* quad, ImVec2* center, float angle_rad, ImVec2* translation) { iam_transform_quad(quad, *center, angle_rad, *translation); }

// Create a rotated quad for a glyph at a position on the path
CIMGUI_API void   c_iam_make_glyph_quad(ImVec2* quad, ImVec2* pos, float angle_rad, float glyph_width, float glyph_height, float baseline_offset) { iam_make_glyph_quad(quad, *pos, angle_rad, glyph_width, glyph_height, baseline_offset); }


// ----------------------------------------------------
// Text Stagger - per-character animation effects
// ----------------------------------------------------

// Render text with per-character stagger animation
CIMGUI_API void   c_iam_text_stagger(ImGuiID id, const char* text, float progress, iam_text_stagger_opts* opts) { iam_text_stagger(id, text, progress, opts ? *opts : iam_text_stagger_opts()); }

// Get text width for layout calculations
CIMGUI_API float  c_iam_text_stagger_width(const char* text, iam_text_stagger_opts* opts) { return iam_text_stagger_width(text, opts ? *opts : iam_text_stagger_opts()); }

// Get total animation duration for text (accounts for stagger delays)
CIMGUI_API float  c_iam_text_stagger_duration(const char* text, iam_text_stagger_opts* opts) { return iam_text_stagger_duration(text, opts ? *opts : iam_text_stagger_opts()); }


// ----------------------------------------------------
// Noise Channels - Perlin/Simplex noise for organic movement
// ----------------------------------------------------

// Sample noise at a point (returns value in [-1, 1])
CIMGUI_API float  c_iam_noise_2d(float x, float y, iam_noise_opts* opts) { return iam_noise_2d(x, y, opts ? *opts : iam_noise_opts()); }
CIMGUI_API float  c_iam_noise_3d(float x, float y, float z, iam_noise_opts* opts) { return iam_noise_3d(x, y, z, opts ? *opts : iam_noise_opts()); }

// Animated noise channels - continuous noise that evolves over time
CIMGUI_API float  c_iam_noise_channel_float(ImGuiID id, float frequency, float amplitude, iam_noise_opts* opts, float dt) { return iam_noise_channel_float(id, frequency, amplitude, opts ? *opts : iam_noise_opts(), dt); }
CIMGUI_API void   c_iam_noise_channel_vec2(ImVec2* pOut, ImGuiID id, ImVec2* frequency, ImVec2* amplitude, iam_noise_opts* opts, float dt) { *pOut = iam_noise_channel_vec2(id, *frequency, *amplitude, opts ? *opts : iam_noise_opts(), dt); }
CIMGUI_API void   c_iam_noise_channel_vec4(ImVec4* pOut, ImGuiID id, ImVec4* frequency, ImVec4* amplitude, iam_noise_opts* opts, float dt) { *pOut = iam_noise_channel_vec4(id, *frequency, *amplitude, opts ? *opts : iam_noise_opts(), dt); }
CIMGUI_API void   c_iam_noise_channel_color(ImVec4* pOut, ImGuiID id, ImVec4* base_color, ImVec4* amplitude, float frequency, iam_noise_opts* opts, int color_space, float dt) { *pOut = iam_noise_channel_color(id, *base_color, *amplitude, frequency, opts ? *opts : iam_noise_opts(), color_space, dt); }

// Convenience: smooth random movement (like wiggle but using noise)
CIMGUI_API float  c_iam_smooth_noise_float(ImGuiID id, float amplitude, float speed, float dt) { return iam_smooth_noise_float(id, amplitude, speed, dt); }
CIMGUI_API void   c_iam_smooth_noise_vec2(ImVec2* pOut, ImGuiID id, ImVec2* amplitude, float speed, float dt) { *pOut = iam_smooth_noise_vec2(id, *amplitude, speed, dt); }
CIMGUI_API void   c_iam_smooth_noise_vec4(ImVec4* pOut, ImGuiID id, ImVec4* amplitude, float speed, float dt) { *pOut = iam_smooth_noise_vec4(id, *amplitude, speed, dt); }
CIMGUI_API void   c_iam_smooth_noise_color(ImVec4* pOut, ImGuiID id, ImVec4* base_color, ImVec4* amplitude, float speed, int color_space, float dt) { *pOut = iam_smooth_noise_color(id, *base_color, *amplitude, speed, color_space, dt); }


// ----------------------------------------------------
// Style Interpolation - animate between ImGuiStyle themes
// ----------------------------------------------------

// Register a named style for interpolation
CIMGUI_API void   c_iam_style_register(ImGuiID style_id, ImGuiStyle* style) { iam_style_register(style_id, *style); }
CIMGUI_API void   c_iam_style_register_current(ImGuiID style_id) { return iam_style_register_current(style_id); }

// Blend between two registered styles (result applied to ImGui::GetStyle())
CIMGUI_API void   c_iam_style_blend(ImGuiID style_a, ImGuiID style_b, float t, int color_space) { iam_style_blend(style_a, style_b, t, color_space); }

// Tween between styles over time
CIMGUI_API void   c_iam_style_tween(ImGuiID id, ImGuiID target_style, float duration, iam_ease_desc* ease, int color_space, float dt) { iam_style_tween(id, target_style, duration, ease ? *ease : iam_ease_desc(), color_space, dt); }

// Get interpolated style without applying
CIMGUI_API void   c_iam_style_blend_to(ImGuiID style_a, ImGuiID style_b, float t, ImGuiStyle* out_style, int color_space) { iam_style_blend_to(style_a, style_b, t, out_style, color_space); }

// Check if a style is registered
CIMGUI_API bool   c_iam_style_exists(ImGuiID style_id) { return iam_style_exists(style_id); }

// Remove a registered style
CIMGUI_API void   c_iam_style_unregister(ImGuiID style_id) { iam_style_unregister(style_id); }


// ----------------------------------------------------
// Gradient Interpolation - animate between color gradients
// ----------------------------------------------------

// Blend between two gradients
CIMGUI_API void   c_iam_gradient_lerp(iam_gradient* pOut, iam_gradient* a, iam_gradient* b, float t, int color_space) { *pOut = iam_gradient_lerp(*a, *b, t, color_space); }

// Tween between gradients over time
CIMGUI_API void   c_iam_tween_gradient(iam_gradient* pOut, ImGuiID id, ImGuiID channel_id, iam_gradient* target, float dur, iam_ease_desc* ez, int policy, int color_space, float dt) { *pOut = iam_tween_gradient(id, channel_id, *target, dur, ez ? *ez : iam_ease_desc(), policy, color_space, dt); }


// ----------------------------------------------------
// Transform Interpolation - animate 2D transforms
// ----------------------------------------------------

// Blend between two transforms with rotation interpolation
CIMGUI_API void   c_iam_transform_lerp(iam_transform* pOut, iam_transform* a, iam_transform* b, float t, int rotation_mode) { *pOut = iam_transform_lerp(*a, *b, t, rotation_mode); }

// Tween between transforms over time
CIMGUI_API void   c_iam_tween_transform(iam_transform* pOut, ImGuiID id, ImGuiID channel_id, iam_transform* target, float dur, iam_ease_desc* ez, int policy, int rotation_mode, float dt) { *pOut = iam_tween_transform(id, channel_id, *target, dur, ez ? *ez : iam_ease_desc(), policy, rotation_mode, dt); }

// Decompose a 3x2 matrix into transform components
CIMGUI_API void   c_iam_transform_from_matrix(iam_transform* pOut, float m00, float m01, float m10, float m11, float tx, float ty) { *pOut = iam_transform_from_matrix(m00, m01, m10, m11, tx, ty); }

// Convert transform to 3x2 matrix (row-major: [m00 m01 tx; m10 m11 ty])
CIMGUI_API void   c_iam_transform_to_matrix(iam_transform* t, float* out_matrix) { iam_transform_to_matrix(*t, out_matrix); }


// ----------------------------------------------------
// iam_clip - fluent API for authoring animations
// ----------------------------------------------------

CIMGUI_API void   c_iam_clip_begin(iam_clip* pOut, ImGuiID clip_id) { *pOut = iam_clip::begin(clip_id); }

// Add keyframes for different channel types
CIMGUI_API void   c_iam_clip_key_float(iam_clip* self, ImGuiID channel, float time, float value, int ease_type, float const* bezier4) { self->key_float(channel, time, value, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec2(iam_clip* self, ImGuiID channel, float time, ImVec2* value, int ease_type, float const* bezier4) { self->key_vec2(channel, time, *value, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec4(iam_clip* self, ImGuiID channel, float time, ImVec4* value, int ease_type, float const* bezier4) { self->key_vec4(channel, time, *value, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_int(iam_clip* self, ImGuiID channel, float time, int value, int ease_type) { self->key_int(channel, time, value, ease_type); }
CIMGUI_API void   c_iam_clip_key_color(iam_clip* self, ImGuiID channel, float time, ImVec4* value, int color_space, int ease_type, float const* bezier4) { self->key_color(channel, time, *value, color_space, ease_type, bezier4); }

// Keyframes with repeat variation (value changes per loop iteration)
CIMGUI_API void   c_iam_clip_key_float_var(iam_clip* self, ImGuiID channel, float time, float value, iam_variation_float* var, int ease_type, float const* bezier4) { self->key_float_var(channel, time, value, *var, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec2_var(iam_clip* self, ImGuiID channel, float time, ImVec2* value, iam_variation_vec2* var, int ease_type, float const* bezier4) { self->key_vec2_var(channel, time, *value, *var, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec4_var(iam_clip* self, ImGuiID channel, float time, ImVec4* value, iam_variation_vec4* var, int ease_type, float const* bezier4) { self->key_vec4_var(channel, time, *value, *var, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_int_var(iam_clip* self, ImGuiID channel, float time, int value, iam_variation_int* var, int ease_type) { self->key_int_var(channel, time, value, *var, ease_type); }
CIMGUI_API void   c_iam_clip_key_color_var(iam_clip* self, ImGuiID channel, float time, ImVec4* value, iam_variation_color* var, int color_space, int ease_type, float const* bezier4) { self->key_color_var(channel, time, *value, *var, color_space, ease_type, bezier4); }

// Spring-based keyframe (float only)
CIMGUI_API void   c_iam_clip_key_float_spring(iam_clip* self, ImGuiID channel, float time, float target, iam_spring_params* spring) { self->key_float_spring(channel, time, target, *spring); }

// Anchor-relative keyframes (values resolved relative to window/viewport at get time)
CIMGUI_API void   c_iam_clip_key_float_rel(iam_clip* self, ImGuiID channel, float time, float percent, float px_bias, int anchor_space, int axis, int ease_type, float const* bezier4) { self->key_float_rel(channel, time, percent, px_bias, anchor_space, axis, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec2_rel(iam_clip* self, ImGuiID channel, float time, ImVec2* percent, ImVec2* px_bias, int anchor_space, int ease_type, float const* bezier4) { self->key_vec2_rel(channel, time, *percent, *px_bias, anchor_space, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_vec4_rel(iam_clip* self, ImGuiID channel, float time, ImVec4* percent, ImVec4* px_bias, int anchor_space, int ease_type, float const* bezier4) { self->key_vec4_rel(channel, time, *percent, *px_bias, anchor_space, ease_type, bezier4); }
CIMGUI_API void   c_iam_clip_key_color_rel(iam_clip* self, ImGuiID channel, float time, ImVec4* percent, ImVec4* px_bias, int color_space, int anchor_space, int ease_type, float const* bezier4) { self->key_color_rel(channel, time, *percent, *px_bias, color_space, anchor_space, ease_type, bezier4); }

// Timeline grouping - sequential and parallel keyframe blocks
CIMGUI_API void   c_iam_clip_seq_begin(iam_clip* self) { self->seq_begin(); }
CIMGUI_API void   c_iam_clip_seq_end(iam_clip* self) { self->seq_end(); }
CIMGUI_API void   c_iam_clip_par_begin(iam_clip* self) { self->par_begin(); }
CIMGUI_API void   c_iam_clip_par_end(iam_clip* self) { self->par_end(); }

// Timeline markers - callbacks at specific times during playback
CIMGUI_API void   c_iam_clip_marker_id(iam_clip* self, float time, ImGuiID marker_id, iam_marker_callback cb, void* user) { self->marker(time, marker_id, cb, user); }
CIMGUI_API void   c_iam_clip_marker(iam_clip* self, float time, iam_marker_callback cb, void* user) { self->marker(time, cb, user); }

// Clip options
CIMGUI_API void   c_iam_clip_set_loop(iam_clip* self, bool loop, int direction, int loop_count) { self->set_loop(loop, direction, loop_count); }
CIMGUI_API void   c_iam_clip_set_delay(iam_clip* self, float delay_seconds) { self->set_delay(delay_seconds); }
CIMGUI_API void   c_iam_clip_set_stagger(iam_clip* self, int count, float each_delay, float from_center_bias) { self->set_stagger(count, each_delay, from_center_bias); }

// Timing variation per loop iteration
CIMGUI_API void   c_iam_clip_set_duration_var(iam_clip* self, iam_variation_float* var) { self->set_duration_var(*var); }
CIMGUI_API void   c_iam_clip_set_delay_var(iam_clip* self, iam_variation_float* var) { self->set_delay_var(*var); }
CIMGUI_API void   c_iam_clip_set_timescale_var(iam_clip* self, iam_variation_float* var) { self->set_timescale_var(*var); }

// Callbacks
CIMGUI_API void   c_iam_clip_on_begin(iam_clip* self, iam_clip_callback cb, void* user) { self->on_begin(cb, user); }
CIMGUI_API void   c_iam_clip_on_update(iam_clip* self, iam_clip_callback cb, void* user) { self->on_update(cb, user); }
CIMGUI_API void   c_iam_clip_on_complete(iam_clip* self, iam_clip_callback cb, void* user) { self->on_complete(cb, user); }

CIMGUI_API void   c_iam_clip_end(iam_clip* self) { self->end(); }


// ----------------------------------------------------
// iam_instance - playback control for a clip
// ----------------------------------------------------

// Playback control
CIMGUI_API void   c_iam_instance_pause(iam_instance* self) { self->pause(); }
CIMGUI_API void   c_iam_instance_resume(iam_instance* self) { self->resume(); }
CIMGUI_API void   c_iam_instance_stop(iam_instance* self) { self->stop(); }
CIMGUI_API void   c_iam_instance_destroy_playback(iam_instance* self) { self->destroy(); }
CIMGUI_API void   c_iam_instance_seek(iam_instance* self, float time) { self->seek(time); }
CIMGUI_API void   c_iam_instance_set_time_scale(iam_instance* self, float scale) { self->set_time_scale(scale); }
CIMGUI_API void   c_iam_instance_set_weight(iam_instance* self, float weight) { self->set_weight(weight); }

// Animation chaining - play another clip when this one completes
CIMGUI_API void   c_iam_instance_then(iam_instance* pOut, iam_instance* self, ImGuiID next_clip_id) { *pOut = self->then(next_clip_id); }
CIMGUI_API void   c_iam_instance_then_id(iam_instance* pOut, iam_instance* self, ImGuiID next_clip_id, ImGuiID next_instance_id) { *pOut = self->then(next_clip_id, next_instance_id); }
CIMGUI_API void   c_iam_instance_then_delay(iam_instance* pOut, iam_instance* self, float delay) { *pOut = self->then_delay(delay); }

// Query state
CIMGUI_API float  c_iam_instance_time(iam_instance* self) { return self->time(); }
CIMGUI_API float  c_iam_instance_duration(iam_instance* self) { return self->duration(); }
CIMGUI_API bool   c_iam_instance_is_playing(iam_instance* self) { return self->is_playing(); }
CIMGUI_API bool   c_iam_instance_is_paused(iam_instance* self) { return self->is_paused(); }

// Get animated values
CIMGUI_API bool   c_iam_instance_get_float(iam_instance* self, ImGuiID channel, float* out) { return self->get_float(channel, out); }
CIMGUI_API bool   c_iam_instance_get_vec2(iam_instance* self, ImGuiID channel, ImVec2* out) { return self->get_vec2(channel, out); }
CIMGUI_API bool   c_iam_instance_get_vec4(iam_instance* self, ImGuiID channel, ImVec4* out) { return self->get_vec4(channel, out); }
CIMGUI_API bool   c_iam_instance_get_int(iam_instance* self, ImGuiID channel, int* out) { return self->get_int(channel, out); }
CIMGUI_API bool   c_iam_instance_get_color(iam_instance* self, ImGuiID channel, ImVec4* out, int color_space) { return self->get_color(channel, out, color_space); }

// Check validity
CIMGUI_API bool   c_iam_instance_valid(iam_instance* self) { return self->valid(); }


// ----------------------------------------------------
// Clip System API
// ----------------------------------------------------

// Initialize/shutdown (optional - auto-init on first use)
CIMGUI_API void   c_iam_clip_init(int initial_clip_cap, int initial_inst_cap) { iam_clip_init(initial_clip_cap, initial_inst_cap); }
CIMGUI_API void   c_iam_clip_shutdown() { iam_clip_shutdown(); }

// c_Per-frame update (call after iam_update_begin_frame)
CIMGUI_API void   c_iam_clip_update(float dt) { iam_clip_update(dt); }

// Garbage collection for instances
CIMGUI_API void   c_iam_clip_gc(unsigned int max_age_frames) { iam_clip_gc(max_age_frames); }

// Play a clip on an instance (creates or reuses instance)
CIMGUI_API void   c_iam_play(iam_instance* pOut, ImGuiID clip_id, ImGuiID instance_id) { *pOut = iam_play(clip_id, instance_id); }

// c_Get an existing instance (returns invalid iam_instance if not found)
CIMGUI_API void   c_iam_get_instance(iam_instance* pOut, ImGuiID instance_id) { *pOut = iam_get_instance(instance_id); }

// Query clip info
CIMGUI_API float  c_iam_clip_duration(ImGuiID clip_id) { return iam_clip_duration(clip_id); }
CIMGUI_API bool   c_iam_clip_exists(ImGuiID clip_id) { return iam_clip_exists(clip_id); }

// Stagger helpers - compute delay for indexed instances
CIMGUI_API float  c_iam_stagger_delay(ImGuiID clip_id, int index) { return iam_stagger_delay(clip_id, index); }
CIMGUI_API void   c_iam_play_stagger(iam_instance* pOut, ImGuiID clip_id, ImGuiID instance_id, int index) { *pOut = iam_play_stagger(clip_id, instance_id, index); }

// Layering support - blend multiple animation instances
CIMGUI_API void   c_iam_layer_begin(ImGuiID instance_id) { iam_layer_begin(instance_id); }
CIMGUI_API void   c_iam_layer_add(iam_instance* inst, float weight) { iam_layer_add(*inst, weight); }
CIMGUI_API void   c_iam_layer_end(ImGuiID instance_id) { iam_layer_end(instance_id); }
CIMGUI_API bool   c_iam_get_blended_float(ImGuiID instance_id, ImGuiID channel, float* out) { return iam_get_blended_float(instance_id, channel, out); }
CIMGUI_API bool   c_iam_get_blended_vec2(ImGuiID instance_id, ImGuiID channel, ImVec2* out) { return iam_get_blended_vec2(instance_id, channel, out); }
CIMGUI_API bool   c_iam_get_blended_vec4(ImGuiID instance_id, ImGuiID channel, ImVec4* out) { return iam_get_blended_vec4(instance_id, channel, out); }
CIMGUI_API bool   c_iam_get_blended_int(ImGuiID instance_id, ImGuiID channel, int* out) { return iam_get_blended_int(instance_id, channel, out); }

// Persistence (optional)
CIMGUI_API iam_result c_iam_clip_save(ImGuiID clip_id, char const* path) { return iam_clip_save(clip_id, path); }
CIMGUI_API iam_result c_iam_clip_load(char const* path, ImGuiID* out_clip_id) { return iam_clip_load(path, out_clip_id); }
