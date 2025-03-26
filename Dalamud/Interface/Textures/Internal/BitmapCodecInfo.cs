using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Utility;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Represents an available bitmap codec.</summary>
internal sealed class BitmapCodecInfo : IBitmapCodecInfo
{
    /// <summary>Initializes a new instance of the <see cref="BitmapCodecInfo"/> class.</summary>
    /// <param name="codecInfo">The source codec info. Ownership is not transferred.</param>
    public unsafe BitmapCodecInfo(ComPtr<IWICBitmapCodecInfo> codecInfo)
    {
        this.Name = ReadStringUsing(
            codecInfo,
            ((IWICBitmapCodecInfo.Vtbl<IWICBitmapCodecInfo>*)codecInfo.Get()->lpVtbl)->GetFriendlyName);
        Guid temp;
        codecInfo.Get()->GetContainerFormat(&temp).ThrowOnError();
        this.ContainerGuid = temp;
        this.Extensions = ReadStringUsing(
                codecInfo,
                ((IWICBitmapCodecInfo.Vtbl<IWICBitmapCodecInfo>*)codecInfo.Get()->lpVtbl)->GetFileExtensions)
            .Split(',');
        this.MimeTypes = ReadStringUsing(
                codecInfo,
                ((IWICBitmapCodecInfo.Vtbl<IWICBitmapCodecInfo>*)codecInfo.Get()->lpVtbl)->GetMimeTypes)
            .Split(',');
    }

    /// <summary>Gets the friendly name for the codec.</summary>
    public string Name { get; }

    /// <summary>Gets the <see cref="Guid"/> representing the container.</summary>
    public Guid ContainerGuid { get; }

    /// <summary>Gets the suggested file extensions.</summary>
    public IReadOnlyCollection<string> Extensions { get; }

    /// <summary>Gets the corresponding mime types.</summary>
    public IReadOnlyCollection<string> MimeTypes { get; }

    private static unsafe string ReadStringUsing(
        IWICBitmapCodecInfo* codecInfo,
        delegate* unmanaged<IWICBitmapCodecInfo*, uint, ushort*, uint*, int> readFuncPtr)
    {
        var cch = 0u;
        _ = readFuncPtr(codecInfo, 0, null, &cch);
        var buf = stackalloc char[(int)cch + 1];
        Marshal.ThrowExceptionForHR(readFuncPtr(codecInfo, cch + 1, (ushort*)buf, &cch));
        return new(buf, 0, (int)cch);
    }
}
