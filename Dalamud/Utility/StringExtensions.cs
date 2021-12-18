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

        /// <summary>
        /// Indicates whether the specified string is null or an empty string ("").
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>true if the value parameter is null or an empty string (""); otherwise, false.</returns>
        public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

        /// <summary>
        /// Indicates whether a specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns>true if the value parameter is null or an empty string (""), or if value consists exclusively of white-space characters.</returns>
        public static bool IsNullOrWhitespace(this string? value) => string.IsNullOrWhiteSpace(value);
    }
}
