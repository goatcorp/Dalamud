using System;
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

        public AntiDebug(SigScanner scanner) {
            DebugCheckAddress = scanner.ScanText("FF 15 ?? ?? ?? ?? 85 C0 74 11");

            Log.Verbose("IsDebuggerPresent address {IsDebuggerPresent}", DebugCheckAddress);
        }

        private IntPtr scanAddress;
        private readonly byte[] nop = new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 };
        private byte[] original;

        public void Enable() {
            this.original = new byte[this.nop.Length];
            if (DebugCheckAddress != IntPtr.Zero) {
                Log.Information($"Overwriting Debug Check @ 0x{scanAddress.ToInt64():X}");
                Marshal.Copy(DebugCheckAddress, this.original, 0, this.nop.Length);
                Marshal.Copy(this.nop, 0, DebugCheckAddress, this.nop.Length);
            }
        }

        public void Dispose() {
            if (this.scanAddress != IntPtr.Zero && this.original != null) 
                Marshal.Copy(this.original, 0, DebugCheckAddress, this.nop.Length);
        }
    }
}
