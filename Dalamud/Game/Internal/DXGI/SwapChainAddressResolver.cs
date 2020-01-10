using System;
using System.Diagnostics;
using System.Linq;
using Serilog;

namespace Dalamud.Game.Internal.DXGI
{
    public sealed class SwapChainAddressResolver : BaseAddressResolver
    {
        public IntPtr Present { get; private set; }
        //public IntPtr ResizeBuffers { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(m => m.ModuleName == "dxgi.dll");

            Log.Debug($"Found DXGI: {module.BaseAddress.ToInt64():X}");

            var scanner = new SigScanner(module);
            Present = scanner.ScanModule("48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 56 48 8D 6C 24 ??");
            // seems unnecessary for now, but we may need to handle it
            //ResizeBuffers = scanner.ScanModule("48 8B C4 55 41 54 41 55 41 56 41 57 48 8D 68 ?? 48 81 EC C0 00 00 00");
        }
    }
}
