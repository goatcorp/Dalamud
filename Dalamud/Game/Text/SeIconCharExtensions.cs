namespace Dalamud.Game.Text;

/// <summary>
/// Extension methods for <see cref="SeIconChar"/>.
/// </summary>
public static class SeIconCharExtensions
{
    /// <summary>
    /// Convert the SeIconChar to a <see cref="char"/> type.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The converted icon.</returns>
    public static char ToIconChar(this SeIconChar icon)
    {
        return (char)icon;
    }

    /// <summary>
    /// Conver the SeIconChar to a <see cref="string"/> type.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The converted icon.</returns>
    public static string ToIconString(this SeIconChar icon)
    {
        return string.Empty + (char)icon;
    }
}
