using System;
using System.Runtime.InteropServices;

using Serilog;

namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// Exposes raw key states read by the game.
/// </summary>
/// <remarks>
/// Unlike <see cref="KeyState"/> this is actually writeable and thus should never be exposed to plugins.
/// </remarks>
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class RawKeyState : IServiceType
{
    private readonly KeyStateIndex mKeyIndex;

    private readonly unsafe KeyStateFlag* mState;
    private readonly int mStateLength;

    [ServiceManager.ServiceConstructor]
    private unsafe RawKeyState(SigScanner sigScanner, ClientState clientState, KeyStateIndex vkIndex)
    {
        this.mKeyIndex = vkIndex;

        var moduleBaseAddress = sigScanner.Module.BaseAddress;
        var addressResolver = clientState.AddressResolver;
        this.mState = (KeyStateFlag*)(moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardState));
        this.mStateLength = vkIndex.MaxValidKeyCode;

        Log.Verbose("KeyState->Buffer@{Address:X8}h", (long)(nint)this.mState);
    }

    /// <summary>
    /// Gets key state for all keys.
    /// </summary>
    // Note that MaxValidCode is inclusive (that is, actual length should be max+1)
    public unsafe Span<KeyStateFlag> RawState => new(this.mState, this.mStateLength + 1);

    /// <summary>
    /// Sets a key state for a given virtual key.
    /// </summary>
    /// <param name="vkCode">The virtual key code to update.</param>
    /// <param name="state">The key state to set.</param>
    /// <remarks>
    /// No-op if <see cref="vkCode"/> is invalid.
    /// </remarks>
    public void SetState(ushort vkCode, KeyStateFlag state)
    {
        unsafe
        {
            if (!this.mKeyIndex.TryGetKeyCode(vkCode, out var keyCode))
            {
                return;
            }

            this.mState[keyCode] = state;
        }
    }

    /// <summary>
    /// Gets state value for the key.
    /// </summary>
    /// <param name="vkCode">
    /// The virtual key code to retrieve.
    /// </param>
    /// <param name="state"> 
    /// If this function returns false, the state of this value is considered undefined.
    /// </param>
    /// <returns>
    /// Returns true if the virtual key is invalid, false otherwise.
    /// </returns>
    public bool TryGetState(ushort vkCode, out KeyStateFlag state)
    {
        unsafe
        {
            state = default;

            if (this.mKeyIndex.TryGetKeyCode(vkCode, out var keyCode))
            {
                return false;
            }

            state = this.mState[keyCode];
            return true;
        }
    }
}
