namespace Dalamud.Game.Internal.DXGI.Definitions;

/// <summary>
/// Contains a full list of IDXGISwapChain functions to be used as an indexer into the SwapChain Virtual Function Table
/// entries.
/// </summary>
internal enum IDXGISwapChainVtbl
{
    // IUnknown

    /// <summary>
    /// IUnknown::QueryInterface method (unknwn.h).
    /// </summary>
    QueryInterface = 0,

    /// <summary>
    /// IUnknown::AddRef method (unknwn.h).
    /// </summary>
    AddRef = 1,

    /// <summary>
    /// IUnknown::Release method (unknwn.h).
    /// </summary>
    Release = 2,

    // IDXGIObject

    /// <summary>
    /// IDXGIObject::SetPrivateData method (dxgi.h).
    /// </summary>
    SetPrivateData = 3,

    /// <summary>
    /// IDXGIObject::SetPrivateDataInterface method (dxgi.h).
    /// </summary>
    SetPrivateDataInterface = 4,

    /// <summary>
    /// IDXGIObject::GetPrivateData method (dxgi.h).
    /// </summary>
    GetPrivateData = 5,

    /// <summary>
    /// IDXGIObject::GetParent method (dxgi.h).
    /// </summary>
    GetParent = 6,

    // IDXGIDeviceSubObject

    /// <summary>
    /// IDXGIDeviceSubObject::GetDevice method (dxgi.h).
    /// </summary>
    GetDevice = 7,

    // IDXGISwapChain

    /// <summary>
    /// IDXGISwapChain::Present method (dxgi.h).
    /// </summary>
    Present = 8,

    /// <summary>
    /// IUnknIDXGISwapChainown::GetBuffer method (dxgi.h).
    /// </summary>
    GetBuffer = 9,

    /// <summary>
    /// IDXGISwapChain::SetFullscreenState method (dxgi.h).
    /// </summary>
    SetFullscreenState = 10,

    /// <summary>
    /// IDXGISwapChain::GetFullscreenState method (dxgi.h).
    /// </summary>
    GetFullscreenState = 11,

    /// <summary>
    /// IDXGISwapChain::GetDesc method (dxgi.h).
    /// </summary>
    GetDesc = 12,

    /// <summary>
    /// IDXGISwapChain::ResizeBuffers method (dxgi.h).
    /// </summary>
    ResizeBuffers = 13,

    /// <summary>
    /// IDXGISwapChain::ResizeTarget method (dxgi.h).
    /// </summary>
    ResizeTarget = 14,

    /// <summary>
    /// IDXGISwapChain::GetContainingOutput method (dxgi.h).
    /// </summary>
    GetContainingOutput = 15,

    /// <summary>
    /// IDXGISwapChain::GetFrameStatistics method (dxgi.h).
    /// </summary>
    GetFrameStatistics = 16,

    /// <summary>
    /// IDXGISwapChain::GetLastPresentCount method (dxgi.h).
    /// </summary>
    GetLastPresentCount = 17,
}
