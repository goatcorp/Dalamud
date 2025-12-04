using System.IO;
using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Utility.TerraFxCom;

/// <summary>Utilities for <see cref="IUnknown"/> and its derivatives.</summary>
internal static unsafe partial class TerraFxComInterfaceExtensions
{
    /// <summary>Creates a new instance of <see cref="IStream"/> from a file path.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="mode">The file open mode.</param>
    /// <param name="access">The file access mode.</param>
    /// <param name="share">The file share mode.</param>
    /// <param name="attributes">The file attributes.</param>
    /// <returns>The new instance of <see cref="IStream"/>.</returns>
    public static ComPtr<IStream> CreateIStreamFromFile(
        string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        FileAttributes attributes = FileAttributes.Normal)
    {
        var grfMode = 0u;
        bool fCreate;
        switch (mode)
        {
            case FileMode.CreateNew:
                fCreate = true;
                grfMode |= STGM.STGM_FAILIFTHERE;
                break;
            case FileMode.Create:
                fCreate = true;
                grfMode |= STGM.STGM_CREATE;
                break;
            case FileMode.Open:
                fCreate = false;
                grfMode |= STGM.STGM_FAILIFTHERE; // yes
                break;
            case FileMode.OpenOrCreate:
                throw new NotSupportedException(
                    $"${FileMode.OpenOrCreate} is not supported. It might be, but it needs testing.");
            case FileMode.Append:
                throw new NotSupportedException($"${FileMode.Append} is not supported.");
            case FileMode.Truncate:
                throw new NotSupportedException($"${FileMode.Truncate} is not supported.");
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        switch (access)
        {
            case FileAccess.Read:
                grfMode |= STGM.STGM_READ;
                break;
            case FileAccess.Write:
                grfMode |= STGM.STGM_WRITE;
                break;
            case FileAccess.ReadWrite:
                grfMode |= STGM.STGM_READWRITE;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(access), access, null);
        }

        switch (share)
        {
            case FileShare.None:
                grfMode |= STGM.STGM_SHARE_EXCLUSIVE;
                break;
            case FileShare.Read:
                grfMode |= STGM.STGM_SHARE_DENY_WRITE;
                break;
            case FileShare.Write:
                grfMode |= STGM.STGM_SHARE_DENY_READ;
                break;
            case FileShare.ReadWrite:
                grfMode |= STGM.STGM_SHARE_DENY_NONE;
                break;
            default:
                throw new NotSupportedException($"Only ${FileShare.Read} and ${FileShare.Write} are supported.");
        }

        using var stream = default(ComPtr<IStream>);
        fixed (char* pPath = path)
        {
            SHCreateStreamOnFileEx(
                pPath,
                grfMode,
                (uint)attributes,
                fCreate,
                null,
                stream.GetAddressOf()).ThrowOnError();
        }

        var res = default(ComPtr<IStream>);
        stream.As(ref res).ThrowOnError();
        return res;
    }

    /// <summary>Calls <see cref="IPropertyBag2.Write"/>.</summary>
    /// <param name="obj">The property bag.</param>
    /// <param name="name">The name of the item to be interpreted as a VARIANT.</param>
    /// <param name="value">The new value, to be interpreted as a <see cref="VARIANT"/>.</param>
    /// <returns>Return value from <inheritdoc cref="IPropertyBag2.Write"/>.</returns>
    public static HRESULT Write(ref this IPropertyBag2 obj, string name, object? value)
    {
        VARIANT varValue;
        // Marshal calls VariantInit.
        Marshal.GetNativeVariantForObject(value, (nint)(&varValue));
        try
        {
            fixed (char* pName = name)
            {
                var option = new PROPBAG2 { pstrName = pName };
                return obj.Write(1, &option, &varValue);
            }
        }
        finally
        {
            VariantClear(&varValue);
        }
    }

    /// <summary>Calls <inheritdoc cref="IWICMetadataQueryWriter.SetMetadataByName"/>.</summary>
    /// <param name="obj">The object.</param>
    /// <param name="name">The name of the metadata.</param>
    /// <param name="value">The new value, to be interpreted as a <see cref="PROPVARIANT"/>.</param>
    /// <returns>Return value from <inheritdoc cref="IWICMetadataQueryWriter.SetMetadataByName"/>.</returns>
    public static HRESULT SetMetadataByName(ref this IWICMetadataQueryWriter obj, string name, object? value)
    {
        VARIANT varValue;
        // Marshal calls VariantInit.
        Marshal.GetNativeVariantForObject(value, (nint)(&varValue));
        try
        {
            PROPVARIANT propVarValue;
            var propVarRes = VariantToPropVariant(&varValue, &propVarValue);
            if (propVarRes < 0)
                return propVarRes;

            try
            {
                fixed (char* pName = name)
                    return obj.SetMetadataByName(pName, &propVarValue);
            }
            finally
            {
                _ = PropVariantClear(&propVarValue);
            }
        }
        finally
        {
            _ = VariantClear(&varValue);
        }
    }

    /// <summary>Calls <inheritdoc cref="IWICMetadataQueryWriter.SetMetadataByName"/>.</summary>
    /// <param name="obj">The object.</param>
    /// <param name="name">The name of the metadata.</param>
    /// <returns>Return value from <inheritdoc cref="IWICMetadataQueryWriter.SetMetadataByName"/>.</returns>
    public static HRESULT RemoveMetadataByName(ref this IWICMetadataQueryWriter obj, string name)
    {
        fixed (char* pName = name)
            return obj.RemoveMetadataByName(pName);
    }

    [LibraryImport("propsys.dll")]
    private static partial int VariantToPropVariant(
        void* pVarIn,
        void* pPropVarOut);
}
