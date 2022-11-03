// Font-Awesome - Version 5.0.9

namespace Dalamud.Interface;

/// <summary>
/// Extension methods for <see cref="FontAwesomeIcon"/>.
/// </summary>
public static class FontAwesomeExtensions
{
    /// <summary>
    /// Convert the FontAwesomeIcon to a <see cref="char"/> type.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The converted icon.</returns>
    public static char ToIconChar(this FontAwesomeIcon icon)
    {
        return (char)icon;
    }

    /// <summary>
    /// Conver the FontAwesomeIcon to a <see cref="string"/> type.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The converted icon.</returns>
    public static string ToIconString(this FontAwesomeIcon icon)
    {
        return string.Empty + (char)icon;
    }
}
