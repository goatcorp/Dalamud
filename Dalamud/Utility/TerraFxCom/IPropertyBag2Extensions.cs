using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Utility.TerraFxCom;

/// <summary>Utilities for <see cref="IUnknown"/> and its derivatives.</summary>
internal static unsafe partial class TerraFxComInterfaceExtensions
{
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
                var option = new PROPBAG2 { pstrName = (ushort*)pName };
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
                    return obj.SetMetadataByName((ushort*)pName, &propVarValue);
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
            return obj.RemoveMetadataByName((ushort*)pName);
    }

    [LibraryImport("propsys.dll")]
    private static partial int VariantToPropVariant(
        void* pVarIn,
        void* pPropVarOut);
}
