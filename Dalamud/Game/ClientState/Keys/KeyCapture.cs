using System;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using MonoMod.Utils;

namespace Dalamud.Game.ClientState.Keys;

[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal sealed partial class KeyCapture : IDisposable, IServiceType
{
    private readonly InputDevicePoll mInputDevicePoll;

    private readonly SimulatedKeyState mSimulatedKeyState;

    private readonly RawKeyState mRawKeyState;

    private readonly List<ushort> mNextCaptureSet = new();

    private readonly List<ushort> mNextRestoreSet = new();

    private State mCaptureState;

    [ServiceManager.ServiceConstructor]
    private KeyCapture(InputDevicePoll devicePoll, SimulatedKeyState simulatedKeyState, RawKeyState rawKeyState)
    {
        this.mInputDevicePoll = devicePoll;
        this.mSimulatedKeyState = simulatedKeyState;
        this.mRawKeyState = rawKeyState;

        this.mInputDevicePoll.OnBeforePoll += this.OnBeforeInputPoll;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        this.mInputDevicePoll.OnBeforePoll -= this.OnBeforeInputPoll;
    }

    /// <summary>
    /// Stop delivering inputs to the game indefinitely.
    /// </summary>
    /// <param name="doCapture">
    /// If this is set to true then it will block all inputs.
    /// To stop capturing, set this to false.
    /// </param>
    public void CaptureAll(bool doCapture = true)
    {
        this.mCaptureState = doCapture switch
        {
            true => this.mCaptureState | State.CaptureAll,
            false => this.mCaptureState & ~State.CaptureAll,
        };

        if (!doCapture)
        {
            // Also queue restore so that the game can receive held inputs again.
            this.mCaptureState |= State.RestoreAllOnNextFrame;
        }
    }

    /// <summary>
    /// Captures all keys for a single frame.
    /// </summary>
    public void CaptureAllSingleFrame()
    {
        this.mCaptureState |= State.CaptureAllSingleFrame;
    }

    /// <summary>
    /// Captures a designated key for a single frame.
    /// </summary>
    /// <param name="vkCode">A virtual key code to capture.</param>
    public void CaptureSingleFrame(ushort vkCode)
    {
        this.mNextCaptureSet.Add(vkCode);
    }

    private void OnBeforeInputPoll()
    {
        // Restore all keys
        if (this.mCaptureState.HasFlag(State.RestoreAllOnNextFrame))
        {
            // Remove pending restore flag
            this.mCaptureState &= ~State.RestoreAllOnNextFrame;

            // Copy all simulated key states into actual buffer.
            // Note that simulated key state is laid out very carefully so that
            // it could just be memcpy'd in this situation.
            this.mSimulatedKeyState.RawState.CopyTo(this.mRawKeyState.RawState);

            // If restored all keys, there's no point of restoring individual keys.
            this.mNextRestoreSet.Clear();
        }

        // Restore individual keys
        foreach (var vkCode in this.mNextRestoreSet)
        {
            if (!this.mSimulatedKeyState.TryGetState(vkCode, out var state))
            {
                continue;
            }

            // Set key state to its original value
            // Log.Verbose("restore: {VkCode} = {State}" vkCode, state);
            this.mRawKeyState.SetState(vkCode, state);
        }

        this.mNextRestoreSet.Clear();

        // Capture key states.
        // Note that we process capturing **only after** finished restoring keys.
        // This allows capturing to take higher precedence over restoring if they're queued on the same frame.
        if (this.mCaptureState.HasFlag(State.CaptureAll))
        {
            // We clear game inputs here on every frame to block delivering any new inputs to the game
            // because we didn't at DispatchMessage time.
            //
            // Thankfully zeroing ~1KB contiguous memory is very fast so we don't even need to touch WndProc hook at all.
            this.mRawKeyState.RawState.Clear();

            // If we captured all keys, there's no point of restoring individual keys.
            this.mNextCaptureSet.Clear();
        }

        if (this.mCaptureState.Has(State.CaptureAllSingleFrame))
        {
            // Remove CaptureAllSingleFrame and Add RestoreAllOnNextFrame
            this.mCaptureState = (this.mCaptureState | State.RestoreAllOnNextFrame) &
                                 ~State.CaptureAllSingleFrame;
            this.mRawKeyState.RawState.Clear();
        }

        // Capture individual keys
        foreach (var vkCode in this.mNextCaptureSet)
        {
            if (!VirtualKeyExtensions.IsValidVirtualKey(vkCode))
            {
                continue;
            }

            // Release a key
            this.mRawKeyState.SetState(vkCode, KeyStateFlag.None);
            this.mNextRestoreSet.Add(vkCode);
        }

        this.mNextCaptureSet.Clear();
    }
}
