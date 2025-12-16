using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImAnim;

[Experimental("Dalamud001")]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimClip
{
    public uint ClipId;

    public static ImAnimClip Begin(uint clipId)
    {
        return ImAnim.ClipBegin(clipId);
    }

    public ImAnimClip KeyFloat(uint channel, float time, float value, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloat(@this, channel, time, value, easeType, null);
        return this;
    }

    public ImAnimClip KeyFloat(uint channel, float time, float value, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloat(@this, channel, time, value, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec2(uint channel, float time, Vector2 value, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2(@this, channel, time, &value, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec2(uint channel, float time, Vector2 value, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2(@this, channel, time, &value, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec4(uint channel, float time, Vector4 value, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4(@this, channel, time, &value, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec4(uint channel, float time, Vector4 value, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4(@this, channel, time, &value, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyInt(uint channel, float time, int value, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyInt(@this, channel, time, value, easeType);
        return this;
    }

    public ImAnimClip KeyColor(uint channel, float time, Vector4 value, ImAnimColorSpace colorSpace, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColor(@this, channel, time, &value, colorSpace, easeType, null);
        return this;
    }

    public ImAnimClip KeyColor(uint channel, float time, Vector4 value, ImAnimColorSpace colorSpace, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColor(@this, channel, time, &value, colorSpace, easeType, &bezier4);
        return this;
    }

    // Keyframes with repeat variation (value changes per loop iteration)

    public ImAnimClip KeyFloatVar(uint channel, float time, float value, ImAnimVariationFloat var, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloatVar(@this, channel, time, value, &var, easeType, null);
        return this;
    }

    public ImAnimClip KeyFloatVar(uint channel, float time, float value, ImAnimVariationFloat var, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloatVar(@this, channel, time, value, &var, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec2Var(uint channel, float time, Vector2 value, ImAnimVariationVec2 var, ImAnimEaseType easeType)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2Var(@this, channel, time, &value, &var, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec2Var(uint channel, float time, Vector2 value, ImAnimVariationVec2 var, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2Var(@this, channel, time, &value, &var, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec4Var(uint channel, float time, Vector4 value, ImAnimVariationVec4 var, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4Var(@this, channel, time, &value, &var, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec4Var(uint channel, float time, Vector4 value, ImAnimVariationVec4 var, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4Var(@this, channel, time, &value, &var, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyIntVar(uint channel, float time, int value, ImAnimVariationInt var, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyIntVar(@this, channel, time, value, &var, easeType);
        return this;
    }

    public ImAnimClip KeyColorVar(uint channel, float time, Vector4 value, ImAnimVariationColor var, ImAnimColorSpace colorSpace, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColorVar(@this, channel, time, &value, &var, colorSpace, easeType, null);
        return this;
    }

    public ImAnimClip KeyColorVar(uint channel, float time, Vector4 value, ImAnimVariationColor var, ImAnimColorSpace colorSpace, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColorVar(@this, channel, time, &value, &var, colorSpace, easeType, &bezier4);
        return this;
    }

    // Spring-based keyframe (float only)

    public ImAnimClip KeyFloatSpring(uint channel, float time, float target, ImAnimSpringParams spring)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloatSpring(@this, channel, time, target, &spring);
        return this;
    }

    // Anchor-relative keyframes (values resolved relative to window/viewport at get time)

    public ImAnimClip KeyFloatRel(uint channel, float time, float percent, float pxBias, ImAnimAnchorSpace anchorSpace, int axis, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloatRel(@this, channel, time, percent, pxBias, anchorSpace, axis, easeType, null);
        return this;
    }

    public ImAnimClip KeyFloatRel(uint channel, float time, float percent, float pxBias, ImAnimAnchorSpace anchorSpace, int axis, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyFloatRel(@this, channel, time, percent, pxBias, anchorSpace, axis, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec2Rel(uint channel, float time, Vector2 percent, Vector2 pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2Rel(@this, channel, time, &percent, &pxBias, anchorSpace, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec2Rel(uint channel, float time, Vector2 percent, Vector2 pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec2Rel(@this, channel, time, &percent, &pxBias, anchorSpace, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyVec4Rel(uint channel, float time, Vector4 percent, Vector4 pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4Rel(@this, channel, time, &percent, &pxBias, anchorSpace, easeType, null);
        return this;
    }

    public ImAnimClip KeyVec4Rel(uint channel, float time, Vector4 percent, Vector4 pxBias, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyVec4Rel(@this, channel, time, &percent, &pxBias, anchorSpace, easeType, &bezier4);
        return this;
    }

    public ImAnimClip KeyColorRel(uint channel, float time, Vector4 percent, Vector4 pxBias, ImAnimColorSpace colorSpace, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType = ImAnimEaseType.Linear)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColorRel(@this, channel, time, &percent, &pxBias, colorSpace, anchorSpace, easeType, null);
        return this;
    }

    public ImAnimClip KeyColorRel(uint channel, float time, Vector4 percent, Vector4 pxBias, ImAnimColorSpace colorSpace, ImAnimAnchorSpace anchorSpace, ImAnimEaseType easeType, Vector4 bezier4)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipKeyColorRel(@this, channel, time, &percent, &pxBias, colorSpace, anchorSpace, easeType, &bezier4);
        return this;
    }

    // Timeline grouping - sequential and parallel keyframe blocks

    public ImAnimClip SeqBegin()
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSeqBegin(@this);
        return this;
    }

    public ImAnimClip SeqEnd()
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSeqEnd(@this);
        return this;
    }

    public ImAnimClip ParBegin()
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipParBegin(@this);
        return this;
    }

    public ImAnimClip ParEnd()
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipParEnd(@this);
        return this;
    }

    // Timeline markers - callbacks at specific times during playback

    public ImAnimClip MarkerId(float time, uint markerId, ImAnim.MarkerCallback cb, void* userData = null)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipMarkerId(@this, time, markerId, (delegate* unmanaged[Cdecl]<uint, uint, float, void*, void>)Marshal.GetFunctionPointerForDelegate(cb), userData);
        return this;
    }

    public ImAnimClip Marker(float time, ImAnim.MarkerCallback cb, void* userData = null)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipMarker(@this, time, (delegate* unmanaged[Cdecl]<uint, uint, float, void*, void>)Marshal.GetFunctionPointerForDelegate(cb), userData);
        return this;
    }

    // Clip options

    public ImAnimClip SetLoop(bool loop, ImAnimDirection direction, int loopCount = -1)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetLoop(@this, (byte)(loop ? 1 : 0), direction, loopCount);
        return this;
    }

    public ImAnimClip SetDelay(float delaySeconds)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetDelay(@this, delaySeconds);
        return this;
    }

    public ImAnimClip SetStagger(int count, float eachDelay, float fromCenterBias)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetStagger(@this, count, eachDelay, fromCenterBias);
        return this;
    }

    // Timing variation per loop iteration

    public ImAnimClip SetDurationVar(ImAnimVariationFloat var)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetDurationVar(@this, &var);
        return this;
    }

    public ImAnimClip SetDelayVar(ImAnimVariationFloat var)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetDelayVar(@this, &var);
        return this;
    }

    public ImAnimClip SetTimescaleVar(ImAnimVariationFloat var)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipSetTimescaleVar(@this, &var);
        return this;
    }

    // Callbacks

    public ImAnimClip OnBegin(ImAnim.ClipCallback cb, void* userData = null)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipOnBegin(@this, (delegate* unmanaged[Cdecl]<uint, void*, void>)Marshal.GetFunctionPointerForDelegate(cb), userData);
        return this;
    }

    public ImAnimClip OnUpdate(ImAnim.ClipCallback cb, void* userData = null)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipOnUpdate(@this, (delegate* unmanaged[Cdecl]<uint, void*, void>)Marshal.GetFunctionPointerForDelegate(cb), userData);
        return this;
    }

    public ImAnimClip OnComplete(ImAnim.ClipCallback cb, void* userData = null)
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipOnComplete(@this, (delegate* unmanaged[Cdecl]<uint, void*, void>)Marshal.GetFunctionPointerForDelegate(cb), userData);
        return this;
    }

    public void End()
    {
        fixed (ImAnimClip* @this = &this)
            ImAnimNative.ClipEnd(@this);
    }

    public ImAnimResult Save(ImU8String path)
    {
        return ImAnim.ClipSave(this.ClipId, path);
    }
}
