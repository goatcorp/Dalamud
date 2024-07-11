using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Utility.TerraFxCom;

/// <summary>Extension methods for D3D11 TerraFX objects.</summary>
internal static class TerraFxD3D11Extensions
{
    /// <summary>Creates a 2D texture with the given descriptor.</summary>
    /// <param name="device">Device to copy from and to.</param>
    /// <param name="desc">Resource descriptor.</param>
    /// <param name="copyFrom">Optional initial data for the texture.</param>
    /// <returns>New copied texture.</returns>
    public static unsafe ComPtr<ID3D11Texture2D> CreateTexture2D(
        this ComPtr<ID3D11Device> device,
        D3D11_TEXTURE2D_DESC desc,
        ComPtr<ID3D11Texture2D> copyFrom = default)
    {
        using var tmpTex = default(ComPtr<ID3D11Texture2D>);
        device.Get()->CreateTexture2D(&desc, null, tmpTex.GetAddressOf()).ThrowOnError();

        if (!copyFrom.IsEmpty())
        {
            using var context = default(ComPtr<ID3D11DeviceContext>);
            device.Get()->GetImmediateContext(context.GetAddressOf());
            context.Get()->CopyResource((ID3D11Resource*)tmpTex.Get(), (ID3D11Resource*)copyFrom.Get());
        }

        return new(tmpTex);
    }

    /// <summary>Creates a shader resource view for a resource.</summary>
    /// <param name="device">Device to create the resource view into.</param>
    /// <param name="resource">Resource to create a view on.</param>
    /// <param name="desc">Resource view descriptor.</param>
    /// <typeparam name="T">Type of the resource.</typeparam>
    /// <returns>New shader resource view.</returns>
    public static unsafe ComPtr<ID3D11ShaderResourceView> CreateShaderResourceView<T>(
        this ComPtr<ID3D11Device> device,
        ComPtr<T> resource,
        in D3D11_SHADER_RESOURCE_VIEW_DESC desc)
        where T : unmanaged, ID3D11Resource.Interface
    {
        fixed (D3D11_SHADER_RESOURCE_VIEW_DESC* pDesc = &desc)
        {
            var srv = default(ComPtr<ID3D11ShaderResourceView>);
            device.Get()->CreateShaderResourceView(
                    (ID3D11Resource*)resource.Get(),
                    pDesc,
                    srv.GetAddressOf())
                .ThrowOnError();
            return srv;
        }
    }
    
    /// <summary>Gets the descriptor for a <see cref="ID3D11Texture2D"/>.</summary>
    /// <param name="texture">Texture.</param>
    /// <returns>Texture descriptor.</returns>
    public static unsafe D3D11_TEXTURE2D_DESC GetDesc(this ComPtr<ID3D11Texture2D> texture)
    {
        var desc = default(D3D11_TEXTURE2D_DESC);
        texture.Get()->GetDesc(&desc);
        return desc;
    }
    
    /// <summary>Gets the descriptor for a <see cref="ID3D11Texture2D"/>.</summary>
    /// <param name="texture">Texture.</param>
    /// <returns>Texture descriptor.</returns>
    public static unsafe D3D11_TEXTURE2D_DESC GetDesc(ref this ID3D11Texture2D texture)
    {
        var desc = default(D3D11_TEXTURE2D_DESC);
        texture.GetDesc(&desc);
        return desc;
    }
}
