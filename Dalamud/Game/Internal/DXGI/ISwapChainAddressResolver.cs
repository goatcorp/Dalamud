using System;

namespace Dalamud.Game.Internal.DXGI {
    public interface ISwapChainAddressResolver {
        IntPtr Present { get; set; }
        IntPtr ResizeBuffers { get; set; }
    }
}
