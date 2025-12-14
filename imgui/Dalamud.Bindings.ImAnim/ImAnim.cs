using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

public static unsafe class ImAnim
{
    public delegate void ClipCallback(uint instId, void* userData); // iam_clip_callback
    public delegate void MarkerCallback(uint instId, uint markerId, float markerTime, void* userData); // iam_marker_callback
    public delegate float EaseFn(float t); // iam_ease_fn

    public delegate float FloatResolver(void* userData); // iam_float_resolver
    public delegate Vector2 Vec2Resolver(void* userData); // iam_vec2_resolver
    public delegate Vector4 Vec4Resolver(void* userData); // iam_vec4_resolver
    public delegate int IntResolver(void* userData); // iam_int_resolver

    public delegate float VariationFloatFn(int index, void* userData); // iam_variation_float_fn
    public delegate int VariationIntFn(int index, void* userData); // iam_variation_int_fn
    public delegate Vector2 VariationVec2Fn(int index, void* userData); // iam_variation_vec2_fn
    public delegate Vector4 VariationVec4Fn(int index, void* userData); // iam_variation_vec4_fn

    // ----------------------------------------------------
    // Public API declarations
    // ----------------------------------------------------

    public static void SetImGuiContext(ImGuiContext* context)
    {
        ImAnimNative.SetImGuiContext(context);
    }

    public static void DemoWindow()
    {
        ImAnimNative.DemoWindow();
    }

    // Frame management

    public static void UpdateBeginFrame()
    {
        ImAnimNative.UpdateBeginFrame();
    }

    public static void Gc(uint maxAgeFrames = 600)
    {
        ImAnimNative.Gc(maxAgeFrames);
    }

    public static void Reserve(int capFloat, int capVec2, int capVec4, int capInt, int capColor)
    {
        ImAnimNative.Reserve(capFloat, capVec2, capVec4, capInt, capColor);
    }

    public static void SetEaseLutSamples(int count)
    {
        ImAnimNative.SetEaseLutSamples(count);
    }

    // Global time scale (for slow-motion / fast-forward debugging)

    public static void SetGlobalTimeScale(float scale)
    {
        ImAnimNative.SetGlobalTimeScale(scale);
    }

    public static float GetGlobalTimeScale()
    {
        return ImAnimNative.GetGlobalTimeScale();
    }

    // Lazy Initialization - defer channel creation until animation is needed

    public static void SetLazyInit(bool enable)
    {
        ImAnimNative.SetLazyInit((byte)(enable ? 1 : 0));
    }

    public static byte IsLazyInitEnabled()
    {
        return ImAnimNative.IsLazyInitEnabled();
    }

    public static void RegisterCustomEase(int slot, EaseFn fn)
    {
        ImAnimNative.RegisterCustomEase(slot, (delegate* unmanaged[Cdecl]<float, float>)Marshal.GetFunctionPointerForDelegate(fn));
    }

    public static EaseFn? GetCustomEase(int slot)
    {
        delegate* unmanaged[Cdecl]<float, float> retFn = default;
        ImAnimNative.GetCustomEase(&retFn, slot);
        return retFn == null ? null : Marshal.GetDelegateForFunctionPointer<EaseFn>((nint)retFn);
    }

    // Debug UI

    public static void ShowUnifiedInspector()
    {
        ImAnimNative.ShowUnifiedInspector();
    }

    public static void ShowUnifiedInspector(ref bool pOpen)
    {
        var open = (byte)(pOpen ? 1 : 0);
        ImAnimNative.ShowUnifiedInspector(&open);
        pOpen = open == 1;
    }

    public static void ShowDebugTimeline(uint instanceId)
    {
        ImAnimNative.ShowDebugTimeline(instanceId);
    }

    // Performance Profiler

    public static void ProfilerEnable(bool enable)
    {
        ImAnimNative.ProfilerEnable((byte)(enable ? 1 : 0));
    }

    public static byte ProfilerIsEnabled()
    {
        return ImAnimNative.ProfilerIsEnabled();
    }

    public static void ProfilerBeginFrame()
    {
        ImAnimNative.ProfilerBeginFrame();
    }

    public static void ProfilerEndFrame()
    {
        ImAnimNative.ProfilerEndFrame();
    }

    public static void ProfilerBegin(ImU8String name)
    {
        fixed (byte* namePtr = &name.GetPinnableNullTerminatedReference())
            ImAnimNative.ProfilerBegin(namePtr);
        name.Recycle();
    }

    public static void ProfilerEnd()
    {
        ImAnimNative.ProfilerEnd();
    }

    // Drag Feedback - animated feedback for drag operations

    public static ImAnimDragFeedback DragBegin(uint id, Vector2 pos)
    {
        ImAnimDragFeedback ret = default;
        ImAnimNative.DragBegin(&ret, id, &pos);
        return ret;
    }

    public static ImAnimDragFeedback DragUpdate(uint id, Vector2 pos, float dt)
    {
        ImAnimDragFeedback ret = default;
        ImAnimNative.DragUpdate(&ret, id, &pos, dt);
        return ret;
    }

    public static ImAnimDragFeedback DragRelease(uint id, Vector2 pos, ImAnimDragOpts opts, float dt)
    {
        ImAnimDragFeedback ret = default;
        ImAnimNative.DragRelease(&ret, id, &pos, &opts, dt);
        return ret;
    }

    public static void DragCancel(uint id)
    {
        ImAnimNative.DragCancel(id);
    }

    // Oscillators - continuous periodic animations

    public static float Oscillate(uint id, float amplitude, float frequency, ImAnimWaveType waveType, float phase, float dt)
    {
        return ImAnimNative.Oscillate(id, amplitude, frequency, waveType, phase, dt);
    }

    public static int OscillateInt(uint id, int amplitude, float frequency, ImAnimWaveType waveType, float phase, float dt)
    {
        return ImAnimNative.OscillateInt(id, amplitude, frequency, waveType, phase, dt);
    }

    public static Vector2 OscillateVec2(uint id, Vector2 amplitude, Vector2 frequency, ImAnimWaveType waveType, Vector2 phase, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.OscillateVec2(&ret, id, &amplitude, &frequency, waveType, &phase, dt);
        return ret;
    }

    public static Vector4 OscillateVec4(uint id, Vector4 amplitude, Vector4 frequency, ImAnimWaveType waveType, Vector4 phase, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.OscillateVec4(&ret, id, &amplitude, &frequency, waveType, &phase, dt);
        return ret;
    }

    public static Vector4 OscillateColor(uint id, Vector4 baseColor, Vector4 amplitude, float frequency, ImAnimWaveType waveType, float phase, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.OscillateColor(&ret, id, &baseColor, &amplitude, frequency, waveType, phase, colorSpace, dt);
        return ret;
    }

    // Shake/Wiggle - procedural noise animations

    public static float Shake(uint id, float intensity, float frequency, float decayTime, float dt)
    {
        return ImAnimNative.Shake(id, intensity, frequency, decayTime, dt);
    }

    public static int ShakeInt(uint id, int intensity, float frequency, float decayTime, float dt)
    {
        return ImAnimNative.ShakeInt(id, intensity, frequency, decayTime, dt);
    }

    public static Vector2 ShakeVec2(uint id, Vector2 intensity, float frequency, float decayTime, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.ShakeVec2(&ret, id, &intensity, frequency, decayTime, dt);
        return ret;
    }

    public static Vector4 ShakeVec4(uint id, Vector4 intensity, float frequency, float decayTime, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.ShakeVec4(&ret, id, &intensity, frequency, decayTime, dt);
        return ret;
    }

    public static Vector4 ShakeColor(uint id, Vector4 baseColor, Vector4 intensity, float frequency, float decayTime, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.ShakeColor(&ret, id, &baseColor, &intensity, frequency, decayTime, colorSpace, dt);
        return ret;
    }

    public static float Wiggle(uint id, float amplitude, float frequency, float dt)
    {
        return ImAnimNative.Wiggle(id, amplitude, frequency, dt);
    }

    public static int WiggleInt(uint id, int amplitude, float frequency, float dt)
    {
        return ImAnimNative.WiggleInt(id, amplitude, frequency, dt);
    }

    public static Vector2 WiggleVec2(uint id, Vector2 amplitude, float frequency, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.WiggleVec2(&ret, id, &amplitude, frequency, dt);
        return ret;
    }

    public static Vector4 WiggleVec4(uint id, Vector4 amplitude, float frequency, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.WiggleVec4(&ret, id, &amplitude, frequency, dt);
        return ret;
    }

    public static Vector4 WiggleColor(uint id, Vector4 baseColor, Vector4 amplitude, float frequency, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.WiggleColor(&ret, id, &baseColor, &amplitude, frequency, colorSpace, dt);
        return ret;
    }

    public static void TriggerShake(uint id)
    {
        ImAnimNative.TriggerShake(id);
    }

    // Easing evaluation

    public static float EvalPreset(ImAnimEaseType type, float t)
    {
        return ImAnimNative.EvalPreset(type, t);
    }

    // Tween API - smoothly interpolate values over time

    public static float TweenFloat(uint id, uint channelId, float target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        return ImAnimNative.TweenFloat(id, channelId, target, dur, &ez, policy, dt);
    }

    public static Vector2 TweenVec2(uint id, uint channelId, Vector2 target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.TweenVec2(&ret, id, channelId, &target, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenVec4(uint id, uint channelId, Vector4 target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenVec4(&ret, id, channelId, &target, dur, &ez, policy, dt);
        return ret;
    }

    public static int TweenInt(uint id, uint channelId, int target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        return ImAnimNative.TweenInt(id, channelId, target, dur, &ez, policy, dt);
    }

    public static Vector4 TweenColor(uint id, uint channelId, Vector4 targetSrgb, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenColor(&ret, id, channelId, &targetSrgb, dur, &ez, policy, colorSpace, dt);
        return ret;
    }

    // Resize-friendly helpers

    public static Vector2 GetAnchorSize(ImAnimAnchorSpace space)
    {
        Vector2 ret = default;
        ImAnimNative.GetAnchorSize(&ret, space);
        return ret;
    }

    // Relative target tweens (percent of anchor + pixel offset) - survive window resizes

    public static float TweenFloatRel(uint id, uint channelId, float percent, float pxBias, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, int axis, float dt)
    {
        return ImAnimNative.TweenFloatRel(id, channelId, percent, pxBias, dur, &ez, policy, anchorSpace, axis, dt);
    }

    public static Vector2 TweenVec2Rel(uint id, uint channelId, Vector2 percent, Vector2 pxBias, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.TweenVec2Rel(&ret, id, channelId, &percent, &pxBias, dur, &ez, policy, anchorSpace, dt);
        return ret;
    }

    public static Vector4 TweenVec4Rel(uint id, uint channelId, Vector4 percent, Vector4 pxBias, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimAnchorSpace anchorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenVec4Rel(&ret, id, channelId, &percent, &pxBias, dur, &ez, policy, anchorSpace, dt);
        return ret;
    }

    public static Vector4 TweenColorRel(uint id, uint channelId, Vector4 percent, Vector4 pxBias, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, ImAnimAnchorSpace anchorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenColorRel(&ret, id, channelId, &percent, &pxBias, dur, &ez, policy, colorSpace, anchorSpace, dt);
        return ret;
    }

    // Rebase functions - change target of in-progress animation without restarting

    public static void RebaseFloat(uint id, uint channelId, float newTarget, float dt)
    {
        ImAnimNative.RebaseFloat(id, channelId, newTarget, dt);
    }

    public static void RebaseVec2(uint id, uint channelId, Vector2 newTarget, float dt)
    {
        ImAnimNative.RebaseVec2(id, channelId, &newTarget, dt);
    }

    public static void RebaseVec4(uint id, uint channelId, Vector4 newTarget, float dt)
    {
        ImAnimNative.RebaseVec4(id, channelId, &newTarget, dt);
    }

    public static void RebaseInt(uint id, uint channelId, int newTarget, float dt)
    {
        ImAnimNative.RebaseInt(id, channelId, newTarget, dt);
    }

    public static void RebaseColor(uint id, uint channelId, Vector4 newTarget, float dt)
    {
        ImAnimNative.RebaseColor(id, channelId, &newTarget, dt);
    }

    // Resolved tweens - target computed dynamically by callback each frame

    public static float TweenFloatResolved(uint id, uint channelId, Func<float> fn, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        float callback(void* userData) => fn();
        return ImAnimNative.TweenFloatResolved(id, channelId, (delegate* unmanaged[Cdecl]<void*, float>)Marshal.GetFunctionPointerForDelegate(callback), null, dur, &ez, policy, dt);
    }

    public static float TweenFloatResolved(uint id, uint channelId, FloatResolver fn, void* userData, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        return ImAnimNative.TweenFloatResolved(id, channelId, (delegate* unmanaged[Cdecl]<void*, float>)Marshal.GetFunctionPointerForDelegate(fn), userData, dur, &ez, policy, dt);
    }

    public static Vector2 TweenVec2Resolved(uint id, uint channelId, Func<Vector2> fn, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector2 callback(void* userData) => fn();
        Vector2 ret = default;
        ImAnimNative.TweenVec2Resolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector2>)Marshal.GetFunctionPointerForDelegate(callback), null, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector2 TweenVec2Resolved(uint id, uint channelId, Vec2Resolver fn, void* userData, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.TweenVec2Resolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector2>)Marshal.GetFunctionPointerForDelegate(fn), userData, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenVec4Resolved(uint id, uint channelId, Func<Vector4> fn, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector4 callback(void* userData) => fn();
        Vector4 ret = default;
        ImAnimNative.TweenVec4Resolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector4>)Marshal.GetFunctionPointerForDelegate(callback), null, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenVec4Resolved(uint id, uint channelId, Vec4Resolver fn, void* userData, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenVec4Resolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector4>)Marshal.GetFunctionPointerForDelegate(fn), userData, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenColorResolved(uint id, uint channelId, Func<Vector4> fn, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 callback(void* userData) => fn();
        Vector4 ret = default;
        ImAnimNative.TweenColorResolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector4>)Marshal.GetFunctionPointerForDelegate(callback), null, dur, &ez, policy, colorSpace, dt);
        return ret;
    }

    public static Vector4 TweenColorResolved(uint id, uint channelId, Vec4Resolver fn, void* userData, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenColorResolved(&ret, id, channelId, (delegate* unmanaged[Cdecl]<void*, Vector4>)Marshal.GetFunctionPointerForDelegate(fn), userData, dur, &ez, policy, colorSpace, dt);
        return ret;
    }

    public static int TweenIntResolved(uint id, uint channelId, Func<int> fn, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        int callback(void* userData) => fn();
        return ImAnimNative.TweenIntResolved(id, channelId, (delegate* unmanaged[Cdecl]<void*, int>)Marshal.GetFunctionPointerForDelegate(callback), null, dur, &ez, policy, dt);
    }

    public static int TweenIntResolved(uint id, uint channelId, IntResolver fn, void* userData, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt)
    {
        return ImAnimNative.TweenIntResolved(id, channelId, (delegate* unmanaged[Cdecl]<void*, int>)Marshal.GetFunctionPointerForDelegate(fn), userData, dur, &ez, policy, dt);
    }

    // Color blending utility

    public static Vector4 GetBlendedColor(Vector4 a, Vector4 b, float t, ImAnimColorSpace colorSpace)
    {
        Vector4 ret = default;
        ImAnimNative.GetBlendedColor(&ret, &a, &b, t, colorSpace);
        return ret;
    }


    // ----------------------------------------------------
    // Convenience shorthands for common easings
    // ----------------------------------------------------

    public static ImAnimEaseDesc EasePreset(ImAnimEaseType type)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EasePreset(&ret, type);
        return ret;
    }

    public static ImAnimEaseDesc EaseBezier(float x1, float y1, float x2, float y2)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseBezier(&ret, x1, y1, x2, y2);
        return ret;
    }

    public static ImAnimEaseDesc EaseStepsDesc(int steps, ImAnimEaseStepsMode mode)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseStepsDesc(&ret, steps, mode);
        return ret;
    }

    public static ImAnimEaseDesc EaseBack(float overshoot)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseBack(&ret, overshoot);
        return ret;
    }

    public static ImAnimEaseDesc EaseElastic(float amplitude, float period)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseElastic(&ret, amplitude, period);
        return ret;
    }

    public static ImAnimEaseDesc EaseSpring(float mass, float stiffness, float damping, float v0)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseSpring(&ret, mass, stiffness, damping, v0);
        return ret;
    }

    public static ImAnimEaseDesc EaseCustomFn(int slot)
    {
        ImAnimEaseDesc ret = default;
        ImAnimNative.EaseCustomFn(&ret, slot);
        return ret;
    }

    // Scroll animation - smooth scrolling for ImGui windows

    public static void ScrollToY(float targetY, float duration)
    {
        ImAnimNative.ScrollToY(targetY, duration, null);
    }

    public static void ScrollToY(float targetY, float duration, ImAnimEaseDesc ez)
    {
        ImAnimNative.ScrollToY(targetY, duration, &ez);
    }

    public static void ScrollToX(float targetX, float duration)
    {
        ImAnimNative.ScrollToX(targetX, duration, null);
    }

    public static void ScrollToX(float targetX, float duration, ImAnimEaseDesc ez)
    {
        ImAnimNative.ScrollToX(targetX, duration, &ez);
    }

    public static void ScrollToTop(float duration = 0.3f)
    {
        ImAnimNative.ScrollToTop(duration, null);
    }

    public static void ScrollToTop(float duration, ImAnimEaseDesc ez)
    {
        ImAnimNative.ScrollToTop(duration, &ez);
    }

    public static void ScrollToBottom(float duration = 0.3f)
    {
        ImAnimNative.ScrollToBottom(duration, null);
    }

    public static void ScrollToBottom(float duration, ImAnimEaseDesc ez)
    {
        ImAnimNative.ScrollToBottom(duration, &ez);
    }


    // ----------------------------------------------------
    // Per-axis easing - different easing per component
    // ----------------------------------------------------

    // Tween with per-axis easing - each component uses its own easing curve

    public static Vector2 TweenVec2PerAxis(uint id, uint channelId, Vector2 target, float dur, ImAnimEasePerAxis ez, ImAnimPolicy policy, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.TweenVec2PerAxis(&ret, id, channelId, &target, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenVec4PerAxis(uint id, uint channelId, Vector4 target, float dur, ImAnimEasePerAxis ez, ImAnimPolicy policy, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenVec4PerAxis(&ret, id, channelId, &target, dur, &ez, policy, dt);
        return ret;
    }

    public static Vector4 TweenColorPerAxis(uint id, uint channelId, Vector4 targetSrgb, float dur, ImAnimEasePerAxis ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.TweenColorPerAxis(&ret, id, channelId, &targetSrgb, dur, &ez, policy, colorSpace, dt);
        return ret;
    }


    // ----------------------------------------------------
    // Motion Paths - animate along curves and splines
    // ----------------------------------------------------

    // Single-curve evaluation functions (stateless, for direct use)

    public static Vector2 BezierQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        Vector2 ret = default;
        ImAnimNative.BezierQuadratic(&ret, &p0, &p1, &p2, t);
        return ret;
    }

    public static Vector2 BezierCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        Vector2 ret = default;
        ImAnimNative.BezierCubic(&ret, &p0, &p1, &p2, &p3, t);
        return ret;
    }

    public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, float tension)
    {
        Vector2 ret = default;
        ImAnimNative.CatmullRom(&ret, &p0, &p1, &p2, &p3, t, tension);
        return ret;
    }

    // Derivatives (for tangent/velocity)

    public static Vector2 BezierQuadraticDeriv(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        Vector2 ret = default;
        ImAnimNative.BezierQuadraticDeriv(&ret, &p0, &p1, &p2, t);
        return ret;
    }

    public static Vector2 BezierCubicDeriv(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        Vector2 ret = default;
        ImAnimNative.BezierCubicDeriv(&ret, &p0, &p1, &p2, &p3, t);
        return ret;
    }

    public static Vector2 CatmullRomDeriv(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, float tension)
    {
        Vector2 ret = default;
        ImAnimNative.CatmullRomDeriv(&ret, &p0, &p1, &p2, &p3, t, tension);
        return ret;
    }

    // Query path info

    public static bool PathExists(uint pathId)
    {
        return ImAnimNative.PathExists(pathId) == 1;
    }

    public static float PathLength(uint pathId)
    {
        return ImAnimNative.PathLength(pathId);
    }

    public static Vector2 PathEvaluate(uint pathId, float t)
    {
        Vector2 ret = default;
        ImAnimNative.PathEvaluate(&ret, pathId, t);
        return ret;
    }

    public static Vector2 PathTangent(uint pathId, float t)
    {
        Vector2 ret = default;
        ImAnimNative.PathTangent(&ret, pathId, t);
        return ret;
    }

    public static float PathAngle(uint pathId, float t)
    {
        return ImAnimNative.PathAngle(pathId, t);
    }

    // Tween along a path

    public static Vector2 TweenPath(uint id, uint channelId, uint pathId, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt = -1f)
    {
        Vector2 ret = default;
        ImAnimNative.TweenPath(&ret, id, channelId, pathId, dur, &ez, policy, dt);
        return ret;
    }

    public static float TweenPathAngle(uint id, uint channelId, uint pathId, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, float dt = -1f)
    {
        return ImAnimNative.TweenPathAngle(id, channelId, pathId, dur, &ez, policy, dt);
    }


    // ----------------------------------------------------
    // Arc-length parameterization (for constant-speed animation)
    // ----------------------------------------------------

    // Build arc-length lookup table for a path (call once per path, improves accuracy)

    public static void PathBuildArcLut(uint pathId, int subdivisions)
    {
        ImAnimNative.PathBuildArcLut(pathId, subdivisions);
    }

    public static bool PathHasArcLut(uint pathId)
    {
        return ImAnimNative.PathHasArcLut(pathId) == 1;
    }

    // Distance-based path evaluation (uses arc-length LUT for constant speed)

    public static float PathDistanceToT(uint pathId, float distance)
    {
        return ImAnimNative.PathDistanceToT(pathId, distance);
    }

    public static Vector2 PathEvaluateAtDistance(uint pathId, float distance)
    {
        Vector2 ret = default;
        ImAnimNative.PathEvaluateAtDistance(&ret, pathId, distance);
        return ret;
    }

    public static float PathAngleAtDistance(uint pathId, float distance)
    {
        return ImAnimNative.PathAngleAtDistance(pathId, distance);
    }

    public static Vector2 PathTangentAtDistance(uint pathId, float distance)
    {
        Vector2 ret = default;
        ImAnimNative.PathTangentAtDistance(&ret, pathId, distance);
        return ret;
    }


    // ----------------------------------------------------
    // Path Morphing - interpolate between two paths
    // ----------------------------------------------------

    // Evaluate morphed path at parameter t [0,1] with blend factor [0,1]
    // path_a at blend=0, path_b at blend=1
    // Paths can have different numbers of segments - they are resampled to match
    public static Vector2 PathMorph(uint pathA, uint pathB, float t, float blend)
    {
        Vector2 ret = default;
        ImAnimNative.PathMorph(&ret, pathA, pathB, t, blend, null);
        return ret;
    }
    public static Vector2 PathMorph(uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts opts)
    {
        Vector2 ret = default;
        ImAnimNative.PathMorph(&ret, pathA, pathB, t, blend, &opts);
        return ret;
    }

    // Get tangent of morphed path
    public static Vector2 PathMorphTangent(uint pathA, uint pathB, float t, float blend)
    {
        Vector2 ret = default;
        ImAnimNative.PathMorphTangent(&ret, pathA, pathB, t, blend, null);
        return ret;
    }
    public static Vector2 PathMorphTangent(uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts opts)
    {
        Vector2 ret = default;
        ImAnimNative.PathMorphTangent(&ret, pathA, pathB, t, blend, &opts);
        return ret;
    }

    // Get angle (radians) of morphed path
    public static float PathMorphAngle(uint pathA, uint pathB, float t, float blend)
    {
        return ImAnimNative.PathMorphAngle(pathA, pathB, t, blend, null);
    }
    public static float PathMorphAngle(uint pathA, uint pathB, float t, float blend, ImAnimMorphOpts opts)
    {
        return ImAnimNative.PathMorphAngle(pathA, pathB, t, blend, &opts);
    }

    // Tween along a morphing path - animates both position along path AND the morph blend
    public static Vector2 TweenPathMorph(uint id, uint channelId, uint pathA, uint pathB, float targetBlend, float dur, ImAnimEaseDesc pathEase, ImAnimEaseDesc morphEase, ImAnimPolicy policy, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.TweenPathMorph(&ret, id, channelId, pathA, pathB, targetBlend, dur, &pathEase, &morphEase, policy, dt, null);
        return ret;
    }
    public static Vector2 TweenPathMorph(uint id, uint channelId, uint pathA, uint pathB, float targetBlend, float dur, ImAnimEaseDesc pathEase, ImAnimEaseDesc morphEase, ImAnimPolicy policy, float dt, ImAnimMorphOpts opts)
    {
        Vector2 ret = default;
        ImAnimNative.TweenPathMorph(&ret, id, channelId, pathA, pathB, targetBlend, dur, &pathEase, &morphEase, policy, dt, &opts);
        return ret;
    }

    // Get current morph blend value from a tween (for querying state)
    public static float GetMorphBlend(uint id, uint channelId)
    {
        return ImAnimNative.GetMorphBlend(id, channelId);
    }


    // ----------------------------------------------------
    // Text along motion paths
    // ----------------------------------------------------

    // Render text along a path (static - no animation)
    public static void TextPath(uint pathId, ImU8String text)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextPath(pathId, textPtr, null);
        text.Recycle();
    }
    public static void TextPath(uint pathId, ImU8String text, ImAnimTextPathOpts opts)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextPath(pathId, textPtr, &opts);
        text.Recycle();
    }

    // Animated text along path (characters appear progressively)
    public static void TextPathAnimated(uint pathId, ImU8String text, float progress)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextPathAnimated(pathId, textPtr, progress, null);
        text.Recycle();
    }
    public static void TextPathAnimated(uint pathId, ImU8String text, float progress, ImAnimTextPathOpts opts)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextPathAnimated(pathId, textPtr, progress, &opts);
        text.Recycle();
    }

    // Helper: Get text width for path layout calculations
    public static float TextPathWidth(ImU8String text)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextPathWidth(textPtr, null);
        text.Recycle();
        return ret;
    }
    public static float TextPathWidth(ImU8String text, ImAnimTextPathOpts opts)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextPathWidth(textPtr, &opts);
        text.Recycle();
        return ret;
    }


    // ----------------------------------------------------
    // Quad transform helpers (for advanced custom rendering)
    // ----------------------------------------------------

    // Transform a quad (4 vertices) by rotation and translation
    public static void TransformQuad(ref Quaternion quad, Vector2 center, float angleRad, Vector2 translation)
    {
        ImAnimNative.TransformQuad((Quaternion*)Unsafe.AsPointer(ref quad), &center, angleRad, &translation);
    }

    // Create a rotated quad for a glyph at a position on the path
    public static void MakeGlyphQuad(ref Quaternion quad, Vector2 pos, float angleRad, float glyphWidth, float glyphHeight, float baselineOffset)
    {
        ImAnimNative.MakeGlyphQuad((Quaternion*)Unsafe.AsPointer(ref quad), &pos, angleRad, glyphWidth, glyphHeight, baselineOffset);
    }


    // ----------------------------------------------------
    // Text Stagger - per-character animation effects
    // ----------------------------------------------------

    // Render text with per-character stagger animation
    public static void TextStagger(uint id, ImU8String text, float progress)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextStagger(id, textPtr, progress, null);
        text.Recycle();
    }
    public static void TextStagger(uint id, ImU8String text, float progress, ImAnimTextStaggerOpts opts)
    {
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ImAnimNative.TextStagger(id, textPtr, progress, &opts);
        text.Recycle();
    }

    // Get text width for layout calculations
    public static float TextStaggerWidth(ImU8String text)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextStaggerWidth(textPtr, null);
        text.Recycle();
        return ret;
    }
    public static float TextStaggerWidth(ImU8String text, ImAnimTextStaggerOpts opts)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextStaggerWidth(textPtr, &opts);
        text.Recycle();
        return ret;
    }

    // Get total animation duration for text (accounts for stagger delays)
    public static float TextStaggerDuration(ImU8String text)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextStaggerDuration(textPtr, null);
        text.Recycle();
        return ret;
    }
    public static float TextStaggerDuration(ImU8String text, ImAnimTextStaggerOpts opts)
    {
        float ret = default;
        fixed (byte* textPtr = &text.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.TextStaggerDuration(textPtr, &opts);
        text.Recycle();
        return ret;
    }


    // ----------------------------------------------------
    // Noise Channels - Perlin/Simplex noise for organic movement
    // ----------------------------------------------------

    // Sample noise at a point (returns value in [-1, 1])

    public static float Noise2D(float x, float y)
    {
        return ImAnimNative.Noise2D(x, y, null);
    }
    public static float Noise2D(float x, float y, ImAnimNoiseOpts opts)
    {
        return ImAnimNative.Noise2D(x, y, &opts);
    }

    public static float Noise3D(float x, float y, float z)
    {
        return ImAnimNative.Noise3D(x, y, z, null);
    }
    public static float Noise3D(float x, float y, float z, ImAnimNoiseOpts opts)
    {
        return ImAnimNative.Noise3D(x, y, z, &opts);
    }

    // Animated noise channels - continuous noise that evolves over time

    public static float NoiseChannelFloat(uint id, float frequency, float amplitude, ImAnimNoiseOpts opts, float dt)
    {
        return ImAnimNative.NoiseChannelFloat(id, frequency, amplitude, &opts, dt);
    }

    public static Vector2 NoiseChannelVec2(uint id, Vector2 frequency, Vector2 amplitude, ImAnimNoiseOpts opts, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.NoiseChannelVec2(&ret, id, &frequency, &amplitude, &opts, dt);
        return ret;
    }

    public static Vector4 NoiseChannelVec4(uint id, Vector4 frequency, Vector4 amplitude, ImAnimNoiseOpts opts, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.NoiseChannelVec4(&ret, id, &frequency, &amplitude, &opts, dt);
        return ret;
    }

    public static Vector4 NoiseChannelColor(uint id, Vector4 baseColor, Vector4 amplitude, float frequency, ImAnimNoiseOpts opts, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.NoiseChannelColor(&ret, id, &baseColor, &amplitude, frequency, &opts, colorSpace, dt);
        return ret;
    }

    // Convenience: smooth random movement (like wiggle but using noise)

    public static float SmoothNoiseFloat(uint id, float amplitude, float speed, float dt)
    {
        return ImAnimNative.SmoothNoiseFloat(id, amplitude, speed, dt);
    }

    public static Vector2 SmoothNoiseVec2(uint id, Vector2 amplitude, float speed, float dt)
    {
        Vector2 ret = default;
        ImAnimNative.SmoothNoiseVec2(&ret, id, &amplitude, speed, dt);
        return ret;
    }

    public static Vector4 SmoothNoiseVec4(uint id, Vector4 amplitude, float speed, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.SmoothNoiseVec4(&ret, id, &amplitude, speed, dt);
        return ret;
    }

    public static Vector4 SmoothNoiseColor(uint id, Vector4 baseColor, Vector4 amplitude, float speed, ImAnimColorSpace colorSpace, float dt)
    {
        Vector4 ret = default;
        ImAnimNative.SmoothNoiseColor(&ret, id, &baseColor, &amplitude, speed, colorSpace, dt);
        return ret;
    }


    // ----------------------------------------------------
    // Style Interpolation - animate between ImGuiStyle themes
    // ----------------------------------------------------

    // Register a named style for interpolation
    public static void StyleRegister(uint styleId, ImGuiStylePtr style)
    {
        ImAnimNative.StyleRegister(styleId, style);
    }

    public static void StyleRegisterCurrent(uint styleId)
    {
        ImAnimNative.StyleRegisterCurrent(styleId);
    }

    // Blend between two registered styles (result applied to ImGui::GetStyle())
    // Uses iam_color_space for color blending mode (iam_col_oklab recommended)
    public static void StyleBlend(uint styleA, uint styleB, float t, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab)
    {
        ImAnimNative.StyleBlend(styleA, styleB, t, colorSpace);
    }

    // Tween between styles over time
    public static void StyleTween(uint id, uint targetStyle, float duration, ImAnimEaseDesc ease, ImAnimColorSpace colorSpace, float dt)
    {
        ImAnimNative.StyleTween(id, targetStyle, duration, &ease, colorSpace, dt);
    }

    // Get interpolated style without applying
    public static ImGuiStyle StyleBlendTo(uint styleA, uint styleB, float t, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab)
    {
        ImGuiStyle ret = default;
        ImAnimNative.StyleBlendTo(styleA, styleB, t, &ret, colorSpace);
        return ret;
    }

    // Check if a style is registered
    public static bool StyleExists(uint styleId)
    {
        return ImAnimNative.StyleExists(styleId) == 1;
    }

    // Remove a registered style
    public static void StyleUnregister(uint styleId)
    {
        ImAnimNative.StyleUnregister(styleId);
    }


    // ----------------------------------------------------
    // Gradient Interpolation - animate between color gradients
    // ----------------------------------------------------

    // Blend between two gradients
    public static ImAnimGradient GradientLerp(ImAnimGradient a, ImAnimGradient b, float t, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab)
    {
        ImAnimGradient ret = default;
        ImAnimNative.GradientLerp(&ret, &a, &b, t, colorSpace);
        return ret;
    }

    public static ImAnimGradient TweenGradient(uint id, uint channelId, ImAnimGradient target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, ImAnimColorSpace colorSpace, float dt)
    {
        ImAnimGradient ret = default;
        ImAnimNative.TweenGradient(&ret, id, channelId, &target, dur, &ez, policy, colorSpace, dt);
        return ret;
    }


    // ----------------------------------------------------
    // Transform Interpolation - animate 2D transforms
    // ----------------------------------------------------

    // Blend between two transforms with rotation interpolation
    public static ImAnimTransform TransformLerp(ImAnimTransform a, ImAnimTransform b, float t, ImAnimRotationMode rotationMode = ImAnimRotationMode.Shortest)
    {
        ImAnimTransform ret = default;
        ImAnimNative.TransformLerp(&ret, &a, &b, t, rotationMode);
        return ret;
    }

    /// Tween between transforms over time
    public static ImAnimTransform TweenTransform(uint id, uint channelId, ImAnimTransform target, float dur, ImAnimEaseDesc ez, ImAnimPolicy policy, int rotationMode, float dt)
    {
        ImAnimTransform ret = default;
        ImAnimNative.TweenTransform(&ret, id, channelId, &target, dur, &ez, policy, rotationMode, dt);
        return ret;
    }

    // Decompose a 3x2 matrix into transform components
    public static ImAnimTransform TransformFromMatrix(Matrix3x2 matrix)
    {
        ImAnimTransform ret = default;
        ImAnimNative.TransformFromMatrix(&ret, matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
        return ret;
    }
    public static ImAnimTransform TransformFromMatrix(float m00, float m01, float m10, float m11, float tx, float ty)
    {
        ImAnimTransform ret = default;
        ImAnimNative.TransformFromMatrix(&ret, m00, m01, m10, m11, tx, ty);
        return ret;
    }

    // Convert transform to 3x2 matrix (row-major: [m00 m01 tx; m10 m11 ty])
    public static Matrix3x2 TransformToMatrix(ImAnimTransform t)
    {
        Matrix3x2 ret = default;
        ImAnimNative.TransformToMatrix(&t, &ret);
        return ret;
    }


    // ----------------------------------------------------
    // iam_clip - fluent API for authoring animations
    // ----------------------------------------------------

    public static ImAnimClip ClipBegin(uint clipId)
    {
        ImAnimClip ret = default;
        ImAnimNative.ClipBegin(&ret, clipId);
        return ret;
    }


    // ----------------------------------------------------
    // Clip System API
    // ----------------------------------------------------

    // Initialize/shutdown (optional - auto-init on first use)
    public static void ClipInit(int initialClipCap, int initialInstCap)
    {
        ImAnimNative.ClipInit(initialClipCap, initialInstCap);
    }

    public static void ClipShutdown()
    {
        ImAnimNative.ClipShutdown();
    }

    // c_Per-frame update (call after iam_update_begin_frame)
    public static void ClipUpdate(float dt)
    {
        ImAnimNative.ClipUpdate(dt);
    }

    // Garbage collection for instances
    public static void ClipGc(uint maxAgeFrames)
    {
        ImAnimNative.ClipGc(maxAgeFrames);
    }

    // Play a clip on an instance (creates or reuses instance)
    public static ImAnimInstance Play(uint clipId, uint instanceId)
    {
        ImAnimInstance ret = default;
        ImAnimNative.Play(&ret, clipId, instanceId);
        return ret;
    }

    // Get an existing instance (returns invalid iam_instance if not found)
    public static ImAnimInstance GetInstance(uint instanceId)
    {
        ImAnimInstance ret = default;
        ImAnimNative.GetInstance(&ret, instanceId);
        return ret;
    }

    // Query clip info
    public static float ClipDuration(uint clipId)
    {
        return ImAnimNative.ClipDuration(clipId);
    }

    public static byte ClipExists(uint clipId)
    {
        return ImAnimNative.ClipExists(clipId);
    }

    // Stagger helpers - compute delay for indexed instances
    public static float StaggerDelay(uint clipId, int index)
    {
        return ImAnimNative.StaggerDelay(clipId, index);
    }

    public static ImAnimInstance PlayStagger(uint clipId, uint instanceId, int index)
    {
        ImAnimInstance ret = default;
        ImAnimNative.PlayStagger(&ret, clipId, instanceId, index);
        return ret;
    }

    // Layering support - blend multiple animation instances
    public static void LayerBegin(uint instanceId)
    {
        ImAnimNative.LayerBegin(instanceId);
    }

    public static void LayerAdd(ImAnimInstance inst, float weight)
    {
        ImAnimNative.LayerAdd(&inst, weight);
    }

    public static void LayerEnd(uint instanceId)
    {
        ImAnimNative.LayerEnd(instanceId);
    }

    public static bool GetBlendedFloat(uint instanceId, uint channel, out float value)
    {
        fixed (float* outVal = &value)
            return ImAnimNative.GetBlendedFloat(instanceId, channel, outVal) == 1;
    }

    public static bool GetBlendedVec2(uint instanceId, uint channel, out Vector2 value)
    {
        fixed (Vector2* outVal = &value)
            return ImAnimNative.GetBlendedVec2(instanceId, channel, outVal) == 1;
    }

    public static bool GetBlendedVec4(uint instanceId, uint channel, out Vector4 value)
    {
        fixed (Vector4* outVal = &value)
            return ImAnimNative.GetBlendedVec4(instanceId, channel, outVal) == 1;
    }

    public static bool GetBlendedInt(uint instanceId, uint channel, out int value)
    {
        fixed (int* outVal = &value)
            return ImAnimNative.GetBlendedInt(instanceId, channel, outVal) == 1;
    }

    // Persistence (optional)
    public static ImAnimResult ClipSave(uint clipId, ImU8String path)
    {
        ImAnimResult ret = default;
        fixed (byte* pathPtr = &path.GetPinnableNullTerminatedReference())
            ret = ImAnimNative.ClipSave(clipId, pathPtr);
        path.Recycle();
        return ret;
    }

    public static ImAnimResult ClipLoad(ImU8String path, out uint clipId)
    {
        ImAnimResult ret = default;
        fixed (byte* pathPtr = &path.GetPinnableNullTerminatedReference())
        fixed (uint* outClipId = &clipId)
            ret = ImAnimNative.ClipLoad(pathPtr, outClipId);
        path.Recycle();
        return ret;
    }
}
