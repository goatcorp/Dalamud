using Serilog;
using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState
{
    /// <summary>
    /// Wrapper around the game keystate buffer, which contains the pressed state for
    /// all keyboard keys, indexed by virtual vkCode
    /// </summary>
    public class KeyState
    {
        private IntPtr bufferBase;

        // The array is accessed in a way that this limit doesn't appear to exist
        // but there is other state data past this point, and keys beyond here aren't
        // generally valid for most things anyway
        private const int MaxKeyCodeIndex = 0xA0;

        public KeyState(ClientStateAddressResolver addressResolver, IntPtr moduleBaseAddress)
        {
            this.bufferBase = moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardState);

            Log.Verbose($"Keyboard state buffer address {this.bufferBase}");
        }

        /// <summary>
        /// Get or set the keypressed state for a given vkCode.
        /// </summary>
        /// <param name="vkCode">The virtual key to change.</param>
        /// <returns>Whether the specified key is currently pressed.</returns>
        public bool this[int vkCode]
        {
            get
            {
                if (vkCode< 0 || vkCode > MaxKeyCodeIndex)
                    throw new ArgumentException($"Keycode state only appears to be valid up to {MaxKeyCodeIndex}");

                return (Marshal.ReadInt32(this.bufferBase + (4 * vkCode)) != 0);
            }

            set
            {
                if (vkCode < 0 || vkCode > MaxKeyCodeIndex)
                    throw new ArgumentException($"Keycode state only appears to be valid up to {MaxKeyCodeIndex}");

                Marshal.WriteInt32(this.bufferBase + (4 * vkCode), value ? 1 : 0);
            }
        }

        /// <summary>
        /// Clears the pressed state for all keys.
        /// </summary>
        public void ClearAll()
        {
            for (var i = 0; i < MaxKeyCodeIndex; i++)
            {
                Marshal.WriteInt32(this.bufferBase + (i * 4), 0);
            }
        }
    }
}
