using System;
using System.Collections.Generic;

using Serilog;

namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// This service emulates key states based on inputs.
/// </summary>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class SimulatedKeyState : IServiceType, IDisposable
{
    private readonly InputDevicePoll mInputDevicePoll;

    private readonly KeyStateIndex mKeyIndex;

    private readonly KeyStateFlag[] mRawState;

    // This is based on the assumption that inputs don't change that much (maybe 1 or 2 per frame) on a single frame.
    private readonly List<byte> mNextClearSet = new();

    [ServiceManager.ServiceConstructor]
    private SimulatedKeyState(InputDevicePoll devicePoll, KeyStateIndex keyIndex)
    {
        this.mInputDevicePoll = devicePoll;
        this.mKeyIndex = keyIndex;
        this.mRawState = new KeyStateFlag[keyIndex.MaxValidKeyCode + 1];

        this.mInputDevicePoll.OnAfterPoll += this.OnAfterInputPoll;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the main window has focus.
    /// </summary>
    /// <remarks>
    /// This value is used to release all keys when the window doesn't have focus.
    /// </remarks>
    public bool HasFocus { get; set; } = true;

    /// <summary>
    /// Gets the simulated key state for all keys.
    /// </summary>
    /// <remarks>
    /// The order of keys is same as the game so that it just could be directly mem-copied into RawKeyState buffer.
    /// </remarks>
    public ReadOnlySpan<KeyStateFlag> RawState => this.mRawState;

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        this.mInputDevicePoll.OnAfterPoll -= this.OnAfterInputPoll;
    }

    /// <summary>
    /// Reports a key stroke.
    /// </summary>
    /// <param name="vkCode">A virtual key code.</param>
    /// <param name="down">True if the key is being held down. otherwise false.</param>
    /// <remarks>
    /// This function will be no-op if <see cref="vkCode"/> is invalid.
    /// </remarks>
    internal void AddKeyEvent(ushort vkCode, bool down)
    {
        if (!this.mKeyIndex.TryGetKeyCode(vkCode, out var keyCode))
        {
            return;
        }

        ref var state = ref this.mRawState[keyCode];

        // Calculates a state value for the key stroke
        if (down)
        {
            // Key is pressed
            if (state.HasFlag(KeyStateFlag.Down))
            {
                // nothing to do if the key is already being pressed
                return;
            }

            // Add Down and JustPressed
            state |= KeyStateFlag.Down | KeyStateFlag.JustPressed;
            this.mNextClearSet.Add(keyCode);
        }
        else
        {
            // Key is released
            if (!state.HasFlag(KeyStateFlag.Down))
            {
                // nothing to do if the key is already released
                return;
            }

            // Remove Down and add JustReleased flag.
            state = (state & ~KeyStateFlag.Down) | KeyStateFlag.JustReleased;
            this.mNextClearSet.Add(keyCode);
        }

        Log.Verbose("Translated key: (vk={VkCode:X2}h, kc={Key:X2})", vkCode, keyCode);
        Log.Verbose("Simulated key state: (vk={VkCode:X2}h, down={Down}, state={State})", vkCode, down, state);
    }

    public bool TryGetState(ushort vkCode, out KeyStateFlag state)
    {
        state = default;

        if (!this.mKeyIndex.TryGetKeyCode(vkCode, out var keyCode))
        {
            return false;
        }

        state = this.mRawState[keyCode];
        return true;
    }

    private void OnAfterInputPoll()
    {
        // Release all keys if the game is out of focus.
        if (!this.HasFocus)
        {
            // Apparently game also does similar thing btw.
            // (rev8555434_2023/02/03_03:27:18 @ 1404A5DF0h)
            // Log.Verbose("no focus, clear");
            this.mRawState.AsSpan().Clear();
        }

        // Clear all JustXXX flags.
        foreach (var keyCode in this.mNextClearSet)
        {
            // Remove JustPressed and JustReleased
            this.mRawState[keyCode] &= ~(KeyStateFlag.JustPressed | KeyStateFlag.JustReleased);

            // Log.Verbose("Remove {KeyCode:X2} flag, now {Flag}", keyCode, this.mRawState[keyCode]);
        }

        this.mNextClearSet.Clear();
    }
}
