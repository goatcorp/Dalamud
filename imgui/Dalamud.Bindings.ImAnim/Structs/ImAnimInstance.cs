using System.Numerics;
using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimInstance
{
    public uint InstId;

    // Playback control

    public static ImAnimInstance Create()
    {
        return new ImAnimInstance();
    }

    public static ImAnimInstance Create(uint instId)
    {
        return new ImAnimInstance() { InstId = instId };
    }

    public void Pause()
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstancePause(@this);
    }

    public void Resume()
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceResume(@this);
    }

    public void Stop()
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceStop(@this);
    }

    public void Seek(float time)
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceSeek(@this, time);
    }

    public void SetTimeScale(float scale)
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceSetTimeScale(@this, scale);
    }

    public void SetWeight(float weight)
    {
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceSetWeight(@this, weight);
    }

    // Animation chaining - play another clip when this one completes

    public ImAnimInstance Then(uint nextClipId)
    {
        ImAnimInstance ret = default;
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceThen(&ret, @this, nextClipId);
        return ret;
    }

    public ImAnimInstance Then(uint nextClipId, uint nextInstanceId)
    {
        ImAnimInstance ret = default;
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceThenId(&ret, @this, nextClipId, nextInstanceId);
        return ret;
    }

    public ImAnimInstance ThenDelay(float delay)
    {
        ImAnimInstance ret = default;
        fixed (ImAnimInstance* @this = &this)
            ImAnimNative.InstanceThenDelay(&ret, @this, delay);
        return ret;
    }

    // Query state

    public float Time()
    {
        fixed (ImAnimInstance* @this = &this)
            return ImAnimNative.InstanceTime(@this);
    }

    public float Duration()
    {
        fixed (ImAnimInstance* @this = &this)
            return ImAnimNative.InstanceDuration(@this);
    }

    public bool IsPlaying()
    {
        fixed (ImAnimInstance* @this = &this)
            return ImAnimNative.InstanceIsPlaying(@this) == 1;
    }

    public bool IsPaused()
    {
        fixed (ImAnimInstance* @this = &this)
            return ImAnimNative.InstanceIsPaused(@this) == 1;
    }

    // Get animated values

    public bool GetFloat(uint channel, out float value)
    {
        fixed (ImAnimInstance* @this = &this)
        fixed (float* outVal = &value)
            return ImAnimNative.InstanceGetFloat(@this, channel, outVal) == 1;
    }

    public bool GetVec2(uint channel, out Vector2 value)
    {
        fixed (ImAnimInstance* @this = &this)
        fixed (Vector2* outVal = &value)
            return ImAnimNative.InstanceGetVec2(@this, channel, outVal) == 1;
    }

    public bool GetVec4(uint channel, out Vector4 value)
    {
        fixed (ImAnimInstance* @this = &this)
        fixed (Vector4* outVal = &value)
            return ImAnimNative.InstanceGetVec4(@this, channel, outVal) == 1;
    }

    public bool GetInt(uint channel, out int value)
    {
        fixed (ImAnimInstance* @this = &this)
        fixed (int* outVal = &value)
            return ImAnimNative.InstanceGetInt(@this, channel, outVal) == 1;
    }

    public bool GetColor(uint channel, out Vector4 value, ImAnimColorSpace colorSpace = ImAnimColorSpace.Oklab)
    {
        fixed (ImAnimInstance* @this = &this)
        fixed (Vector4* outVal = &value)
            return ImAnimNative.InstanceGetColor(@this, channel, outVal, colorSpace) == 1;
    }

    // Check validity

    public bool Valid()
    {
        fixed (ImAnimInstance* @this = &this)
            return ImAnimNative.InstanceValid(@this) == 1;
    }
}
