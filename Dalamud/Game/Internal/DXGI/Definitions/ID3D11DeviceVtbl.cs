namespace Dalamud.Game.Internal.DXGI.Definitions;

/// <summary>
/// Contains a full list of ID3D11Device functions to be used as an indexer into the DirectX Virtual Function Table entries.
/// </summary>
internal enum ID3D11DeviceVtbl
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

    // ID3D11Device

    /// <summary>
    /// ID3D11Device::CreateBuffer method (d3d11.h).
    /// </summary>
    CreateBuffer = 3,

    /// <summary>
    /// ID3D11Device::CreateTexture1D method (d3d11.h).
    /// </summary>
    CreateTexture1D = 4,

    /// <summary>
    /// ID3D11Device::CreateTexture2D method (d3d11.h).
    /// </summary>
    CreateTexture2D = 5,

    /// <summary>
    /// ID3D11Device::CreateTexture3D method (d3d11.h).
    /// </summary>
    CreateTexture3D = 6,

    /// <summary>
    /// ID3D11Device::CreateShaderResourceView method (d3d11.h).
    /// </summary>
    CreateShaderResourceView = 7,

    /// <summary>
    /// ID3D11Device::CreateUnorderedAccessView method (d3d11.h).
    /// </summary>
    CreateUnorderedAccessView = 8,

    /// <summary>
    /// ID3D11Device::CreateRenderTargetView method (d3d11.h).
    /// </summary>
    CreateRenderTargetView = 9,

    /// <summary>
    /// ID3D11Device::CreateDepthStencilView method (d3d11.h).
    /// </summary>
    CreateDepthStencilView = 10,

    /// <summary>
    /// ID3D11Device::CreateInputLayout method (d3d11.h).
    /// </summary>
    CreateInputLayout = 11,

    /// <summary>
    /// ID3D11Device::CreateVertexShader method (d3d11.h).
    /// </summary>
    CreateVertexShader = 12,

    /// <summary>
    /// ID3D11Device::CreateGeometryShader method (d3d11.h).
    /// </summary>
    CreateGeometryShader = 13,

    /// <summary>
    /// ID3D11Device::CreateGeometryShaderWithStreamOutput method (d3d11.h).
    /// </summary>
    CreateGeometryShaderWithStreamOutput = 14,

    /// <summary>
    /// ID3D11Device::CreatePixelShader method (d3d11.h).
    /// </summary>
    CreatePixelShader = 15,

    /// <summary>
    /// ID3D11Device::CreateHullShader method (d3d11.h).
    /// </summary>
    CreateHullShader = 16,

    /// <summary>
    /// ID3D11Device::CreateDomainShader method (d3d11.h).
    /// </summary>
    CreateDomainShader = 17,

    /// <summary>
    /// ID3D11Device::CreateComputeShader method (d3d11.h).
    /// </summary>
    CreateComputeShader = 18,

    /// <summary>
    /// ID3D11Device::CreateClassLinkage method (d3d11.h).
    /// </summary>
    CreateClassLinkage = 19,

    /// <summary>
    /// ID3D11Device::CreateBlendState method (d3d11.h).
    /// </summary>
    CreateBlendState = 20,

    /// <summary>
    /// ID3D11Device::CreateDepthStencilState method (d3d11.h).
    /// </summary>
    CreateDepthStencilState = 21,

    /// <summary>
    /// ID3D11Device::CreateRasterizerState method (d3d11.h).
    /// </summary>
    CreateRasterizerState = 22,

    /// <summary>
    /// ID3D11Device::CreateSamplerState method (d3d11.h).
    /// </summary>
    CreateSamplerState = 23,

    /// <summary>
    /// ID3D11Device::CreateQuery method (d3d11.h).
    /// </summary>
    CreateQuery = 24,

    /// <summary>
    /// ID3D11Device::CreatePredicate method (d3d11.h).
    /// </summary>
    CreatePredicate = 25,

    /// <summary>
    /// ID3D11Device::CreateCounter method (d3d11.h).
    /// </summary>
    CreateCounter = 26,

    /// <summary>
    /// ID3D11Device::CreateDeferredContext method (d3d11.h).
    /// </summary>
    CreateDeferredContext = 27,

    /// <summary>
    /// ID3D11Device::OpenSharedResource method (d3d11.h).
    /// </summary>
    OpenSharedResource = 28,

    /// <summary>
    /// ID3D11Device::CheckFormatSupport method (d3d11.h).
    /// </summary>
    CheckFormatSupport = 29,

    /// <summary>
    /// ID3D11Device::CheckMultisampleQualityLevels method (d3d11.h).
    /// </summary>
    CheckMultisampleQualityLevels = 30,

    /// <summary>
    /// ID3D11Device::CheckCounterInfo method (d3d11.h).
    /// </summary>
    CheckCounterInfo = 31,

    /// <summary>
    /// ID3D11Device::CheckCounter method (d3d11.h).
    /// </summary>
    CheckCounter = 32,

    /// <summary>
    /// ID3D11Device::CheckFeatureSupport method (d3d11.h).
    /// </summary>
    CheckFeatureSupport = 33,

    /// <summary>
    /// ID3D11Device::GetPrivateData method (d3d11.h).
    /// </summary>
    GetPrivateData = 34,

    /// <summary>
    /// ID3D11Device::SetPrivateData method (d3d11.h).
    /// </summary>
    SetPrivateData = 35,

    /// <summary>
    /// ID3D11Device::SetPrivateDataInterface method (d3d11.h).
    /// </summary>
    SetPrivateDataInterface = 36,

    /// <summary>
    /// ID3D11Device::GetFeatureLevel method (d3d11.h).
    /// </summary>
    GetFeatureLevel = 37,

    /// <summary>
    /// ID3D11Device::GetCreationFlags method (d3d11.h).
    /// </summary>
    GetCreationFlags = 38,

    /// <summary>
    /// ID3D11Device::GetDeviceRemovedReason method (d3d11.h).
    /// </summary>
    GetDeviceRemovedReason = 39,

    /// <summary>
    /// ID3D11Device::GetImmediateContext method (d3d11.h).
    /// </summary>
    GetImmediateContext = 40,

    /// <summary>
    /// ID3D11Device::SetExceptionMode method (d3d11.h).
    /// </summary>
    SetExceptionMode = 41,

    /// <summary>
    /// ID3D11Device::GetExceptionMode method (d3d11.h).
    /// </summary>
    GetExceptionMode = 42,
}
