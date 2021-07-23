namespace Dalamud.Utility
{
    /// <summary>
    /// Extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// An extension method to chain usage of string.Format.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Formatted string.</returns>
        public static string Format(this string format, params object[] args) => string.Format(format, args);
    }
}
