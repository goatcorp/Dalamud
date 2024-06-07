namespace Dalamud.Game.Internal.DXGI;

/// <summary>
/// An interface binding for the address resolvers that attempt to find native D3D11 methods.
/// </summary>
public interface ISwapChainAddressResolver
{
    /// <summary>
    /// Gets or sets the address of the native D3D11.Present method.
    /// </summary>
    IntPtr Present { get; set; }

    /// <summary>
    /// Gets or sets the address of the native D3D11.ResizeBuffers method.
    /// </summary>
    IntPtr ResizeBuffers { get; set; }
}
