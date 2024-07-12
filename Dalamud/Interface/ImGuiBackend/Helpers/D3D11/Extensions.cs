using System.Text;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.ImGuiBackend.Helpers.D3D11;

/// <summary>Utility extension methods for D3D11 objects.</summary>
internal static class Extensions
{
    /// <summary>Sets the name for debugging.</summary>
    /// <param name="child">D3D11 object.</param>
    /// <param name="name">Debug name.</param>
    /// <typeparam name="T">Object type.</typeparam>
    public static unsafe void SetDebugName<T>(ref this T child, string name)
        where T : unmanaged, ID3D11DeviceChild.Interface
    {
        var len = Encoding.UTF8.GetByteCount(name);
        var buf = stackalloc byte[len + 1];
        Encoding.UTF8.GetBytes(name, new(buf, len + 1));
        buf[len] = 0;
        fixed (Guid* pId = &DirectX.WKPDID_D3DDebugObjectName)
            child.SetPrivateData(pId, (uint)(len + 1), buf).ThrowOnError();
    }
}
