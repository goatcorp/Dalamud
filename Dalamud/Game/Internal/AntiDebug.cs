using System;
using System.Collections.Generic;

using Serilog;

namespace Dalamud.Game.Internal
{
    public class AntiDebug : IDisposable
    {
        private IntPtr DebugCheckAddress { get; set; }

        public bool IsEnabled { get; private set; }

        public AntiDebug(SigScanner scanner)
        {
            try
            {
                this.DebugCheckAddress = scanner.ScanText("FF 15 ?? ?? ?? ?? 85 C0 74 11 41");
            }
            catch (KeyNotFoundException)
            {
                this.DebugCheckAddress = IntPtr.Zero;
            }

            Log.Verbose("DebugCheck address {DebugCheckAddress}", this.DebugCheckAddress);
        }

        private readonly byte[] nop = new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 };
        private byte[] original;

        public void Enable()
        {
            this.original = new byte[this.nop.Length];
            if (this.DebugCheckAddress != IntPtr.Zero && !this.IsEnabled)
            {
                Log.Information($"Overwriting Debug Check @ 0x{this.DebugCheckAddress.ToInt64():X}");
                SafeMemory.ReadBytes(this.DebugCheckAddress, this.nop.Length, out this.original);
                SafeMemory.WriteBytes(this.DebugCheckAddress, this.nop);
            }
            else
            {
                Log.Information("DebugCheck already overwritten?");
            }

            this.IsEnabled = true;
        }

        public void Dispose()
        {
            // if (this.DebugCheckAddress != IntPtr.Zero && this.original != null)
            //     Marshal.Copy(this.original, 0, DebugCheckAddress, this.nop.Length);
        }
    }
}
