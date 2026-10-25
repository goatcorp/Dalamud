using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.ImGuiBackend.Helpers.D3D11;

/// <summary>Creates the shaders and states shared by the renderers of <see cref="ImDrawVert"/>.</summary>
/// <remarks>All functions throw if the device fails.</remarks>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal static unsafe class D3D11Shaders
{
    /// <summary>Reads a manifest resource of this assembly.</summary>
    /// <param name="name">The name of the resource.</param>
    /// <returns>The bytes.</returns>
    public static byte[] ReadResource(string name)
    {
        using var stream = typeof(D3D11Shaders).Assembly.GetManifestResourceStream(name)!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>Creates a vertex shader from a manifest resource.</summary>
    /// <param name="device">The device.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="shader">The created shader.</param>
    public static void CreateVertexShader(ID3D11Device* device, string name, ID3D11VertexShader** shader)
    {
        var bytes = ReadResource(name);
        fixed (byte* p = bytes)
            device->CreateVertexShader(p, (nuint)bytes.Length, null, shader).ThrowOnError();
    }

    /// <summary>Creates a pixel shader from a manifest resource.</summary>
    /// <param name="device">The device.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="shader">The created shader.</param>
    public static void CreatePixelShader(ID3D11Device* device, string name, ID3D11PixelShader** shader)
    {
        var bytes = ReadResource(name);
        fixed (byte* p = bytes)
            device->CreatePixelShader(p, (nuint)bytes.Length, null, shader).ThrowOnError();
    }

    /// <summary>Creates a vertex shader that reads <see cref="ImDrawVert"/>, and its input layout.</summary>
    /// <param name="device">The device.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="elementCount">The number of leading elements of
    /// <see cref="CreateImDrawVertInputLayout"/> the shader reads.</param>
    /// <param name="shader">The created shader.</param>
    /// <param name="layout">The created input layout.</param>
    public static void CreateImDrawVertShader(
        ID3D11Device* device,
        string name,
        int elementCount,
        ID3D11VertexShader** shader,
        ID3D11InputLayout** layout)
    {
        var bytes = ReadResource(name);
        fixed (byte* p = bytes)
            device->CreateVertexShader(p, (nuint)bytes.Length, null, shader).ThrowOnError();
        CreateImDrawVertInputLayout(device, bytes, elementCount, layout);
    }

    /// <summary>Creates an input layout for <see cref="ImDrawVert"/>.</summary>
    /// <param name="device">The device.</param>
    /// <param name="bytecode">The bytecode of the vertex shader.</param>
    /// <param name="elementCount">The number of leading elements to use, up to 6.</param>
    /// <param name="layout">The created input layout.</param>
    /// <remarks>The elements are <c>POSITION</c>, <c>TEXCOORD0</c>, <c>COLOR</c>, and <c>TEXCOORD1</c> to
    /// <c>TEXCOORD3</c> as unsigned integers.</remarks>
    public static void CreateImDrawVertInputLayout(
        ID3D11Device* device,
        ReadOnlySpan<byte> bytecode,
        int elementCount,
        ID3D11InputLayout** layout)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, 6);
        fixed (byte* pBytecode = bytecode)
        fixed (byte* pszPosition = "POSITION"u8)
        fixed (byte* pszTexCoord = "TEXCOORD"u8)
        fixed (byte* pszColor = "COLOR"u8)
        {
            var ied = stackalloc D3D11_INPUT_ELEMENT_DESC[]
            {
                new()
                {
                    SemanticName = (sbyte*)pszPosition,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszColor,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    SemanticIndex = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    SemanticIndex = 2,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT,
                    AlignedByteOffset = uint.MaxValue,
                },
                new()
                {
                    SemanticName = (sbyte*)pszTexCoord,
                    SemanticIndex = 3,
                    Format = DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT,
                    AlignedByteOffset = uint.MaxValue,
                },
            };
            device->CreateInputLayout(ied, (uint)elementCount, pBytecode, (nuint)bytecode.Length, layout)
                  .ThrowOnError();
        }
    }

    /// <summary>Creates the blend state of the text pixel shader.</summary>
    /// <param name="device">The device.</param>
    /// <param name="state">The created blend state.</param>
    /// <remarks>It uses dual-source blending.</remarks>
    public static void CreateTextBlendState(ID3D11Device* device, ID3D11BlendState** state)
    {
        var desc = new D3D11_BLEND_DESC
        {
            RenderTarget =
            {
                e0 =
                {
                    BlendEnable = true,
                    SrcBlend = D3D11_BLEND.D3D11_BLEND_ONE,
                    DestBlend = D3D11_BLEND.D3D11_BLEND_INV_SRC1_COLOR,
                    BlendOp = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                    SrcBlendAlpha = D3D11_BLEND.D3D11_BLEND_INV_DEST_ALPHA,
                    DestBlendAlpha = D3D11_BLEND.D3D11_BLEND_ONE,
                    BlendOpAlpha = D3D11_BLEND_OP.D3D11_BLEND_OP_ADD,
                    RenderTargetWriteMask = (byte)D3D11_COLOR_WRITE_ENABLE.D3D11_COLOR_WRITE_ENABLE_ALL,
                },
            },
        };
        device->CreateBlendState(&desc, state).ThrowOnError();
    }
}
