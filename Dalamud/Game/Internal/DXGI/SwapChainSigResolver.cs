using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Dalamud.Game.Internal.DXGI
{
    public sealed class SwapChainSigResolver : BaseAddressResolver, ISwapChainAddressResolver
    {
        public IntPtr Present { get; set; }
        public IntPtr ResizeBuffers { get; set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(m => m.ModuleName == "dxgi.dll");

            Log.Debug($"Found DXGI: {module.BaseAddress.ToInt64():X}");

            var scanner = new SigScanner(module);

            // This(code after the function head - offset of it) was picked to avoid running into issues with other hooks being installed into this function.
            Present = scanner.ScanModule("41 8B F0 8B FA 89 54 24 ?? 48 8B D9 48 89 4D ?? C6 44 24 ?? 00") - 0x37;

            ResizeBuffers = scanner.ScanModule("48 8B C4 55 41 54 41 55 41 56 41 57 48 8D 68 ?? 48 81 EC C0 00 00 00");
        }
    }
}
