using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using EasyHook;
using Serilog;

namespace Dalamud.Game.Internal
{
    public class AntiDebug : IDisposable
    {
        private IntPtr DebugCheckAddress { get; set; }

        public bool IsEnabled { get; private set; }

        public AntiDebug(SigScanner scanner) {
            try {
                DebugCheckAddress = scanner.ScanText("FF 15 ?? ?? ?? ?? 85 C0 74 11 41");
            } catch (KeyNotFoundException) {
                DebugCheckAddress = IntPtr.Zero;
            }
            
            Log.Verbose("DebugCheck address {DebugCheckAddress}", DebugCheckAddress);
        }

        private readonly byte[] nop = new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 };
        private byte[] original;

        public void Enable() {
            this.original = new byte[this.nop.Length];
            if (DebugCheckAddress != IntPtr.Zero && !IsEnabled) {
                Log.Information($"Overwriting Debug Check @ 0x{DebugCheckAddress.ToInt64():X}");
                SafeMemory.ReadBytes(DebugCheckAddress, this.nop.Length, out this.original);
                SafeMemory.WriteBytes(DebugCheckAddress, this.nop);
            } else {
                Log.Information("DebugCheck already overwritten?");
            }

            IsEnabled = true;
        }

        public void Dispose() {
            //if (this.DebugCheckAddress != IntPtr.Zero && this.original != null) 
            //    Marshal.Copy(this.original, 0, DebugCheckAddress, this.nop.Length);
        }
    }
}
