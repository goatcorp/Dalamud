using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents an object with localizable names.
/// </summary>
public interface IObjectWithLocalizableName
{
    /// <summary>
    /// Gets the name in English.
    /// </summary>
    string EnglishName { get; }

    /// <summary>
    /// Gets the name in the system language.
    /// </summary>
    string LocalizedName { get; }

    /// <summary>
    /// Gets the localized name, or the first name if unavailable.
    /// </summary>
    /// <param name="fn">The names.</param>
    /// <param name="locales">The locales.</param>
    /// <returns>The string in the first-matching locale, or the first string.</returns>
    internal static unsafe string GetLocalizedNameOrFirst(IDWriteLocalizedStrings* fn, params string[] locales)
    {
        var index = 0u;
        BOOL exists = false;
        foreach (var locale in locales)
        {
            fixed (void* pName = locale)
                fn->FindLocaleName((ushort*)pName, &index, &exists).ThrowOnError();
            if (exists)
                break;
        }

        if (!exists)
            index = 0u;

        var length = 0;
        fn->GetStringLength(index, (uint*)&length).ThrowOnError();
        var name = stackalloc char[length + 1];
        fn->GetString(index, (ushort*)name, (uint)length + 1u).ThrowOnError();
        return new(name, 0, length);
    }
}
