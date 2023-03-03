using System;
using System.Runtime.InteropServices;

using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game.ClientState.Keys;

// TODO: this is probably safe to expose to plugins without major concerns
// [PluginInterface]
[ServiceManager.BlockingEarlyLoadedService]
internal sealed class KeyStateIndex : IServiceType
{
    private const int InvalidKeyCode = 0;

    private readonly unsafe byte* mVkIndex;

    [ServiceManager.ServiceConstructor]
    private unsafe KeyStateIndex(SigScanner sigScanner, ClientState clientState)
    {
        var moduleBaseAddress = sigScanner.Module.BaseAddress;
        var addressResolver = clientState.AddressResolver;

        this.mVkIndex = (byte*)(moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardStateIndexArray));
        this.MaxValidKeyCode = this.AsSpan().Max();

        Log.Verbose("VkIndex@{Address:X8}h (max={Max:X2}h)", (long)(nint)this.mVkIndex, this.MaxValidKeyCode);
    }

    /// <summary>
    /// Gets the upper bound for the key code.
    /// </summary>
    public byte MaxValidKeyCode { get; }

    /// <summary>
    /// Translates a virtual key code from Windows into a key code which the game internally uses.
    /// </summary>
    /// <param name="vkCode">A virtual key to translate.</param>
    /// <param name="keyCode">A reference to keyCode to receive the translated key code.</param>
    /// <returns>
    /// Returns true if this function successfully reads the key code, false otherwise.
    /// </returns>
    /// <remarks>
    /// If this function returns false then the state of <see cref="keyCode"/> will be unspecified.
    /// </remarks>
    public bool TryGetKeyCode(ushort vkCode, out byte keyCode)
    {
        unsafe
        {
            keyCode = default;

            if (!VirtualKeyExtensions.IsValidVirtualKey(vkCode))
            {
                return false;
            }

            keyCode = this.mVkIndex[vkCode];
            if (keyCode == InvalidKeyCode)
            {
                return false;
            }

            return true;
        }
    }

    private unsafe ReadOnlySpan<byte> AsSpan() => new(this.mVkIndex, VirtualKeyExtensions.MaxValidCode + 1);
}
