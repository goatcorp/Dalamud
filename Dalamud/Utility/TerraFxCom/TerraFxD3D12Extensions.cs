using TerraFX.Interop.DirectX;

namespace Dalamud.Utility.TerraFxCom;

/// <summary>Extension methods for D3D12 TerraFX objects.</summary>
internal static class TerraFxD3D12Extensions
{
    /// <summary>Updates <see cref="D3D12_RESOURCE_DESC.Alignment"/> to a valid value.</summary>
    /// <param name="device">A D3D12 device.</param>
    /// <param name="desc">Descriptor to fix.</param>
    /// <returns>Fixed descriptor.</returns>
    /// <exception cref="InvalidOperationException">If <paramref name="desc"/> is invalid.</exception>
    public static unsafe D3D12_RESOURCE_DESC FixDescAlignment(this ref ID3D12Device device, D3D12_RESOURCE_DESC desc)
    {
        desc.Alignment = D3D12.D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT;
        var resAlloc = device.GetResourceAllocationInfo(0, 1, &desc);
        if (resAlloc.SizeInBytes == ulong.MaxValue)
        {
            desc.Alignment = D3D12.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;
            resAlloc = device.GetResourceAllocationInfo(0, 1, &desc);
            if (resAlloc.SizeInBytes == ulong.MaxValue)
                throw new InvalidOperationException($"{nameof(ID3D12Device.GetResourceAllocationInfo)}");
        }

        return desc;
    }
}
