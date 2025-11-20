namespace Dalamud.Utility;

/// <summary>
/// Specify fuzzy match mode.
/// </summary>
internal enum FuzzyMatcherMode
{
    /// <summary>
    /// The matcher only considers whether the haystack contains the needle (case-insensitive.)
    /// </summary>
    Simple,

    /// <summary>
    /// The string is considered for fuzzy matching as a whole.
    /// </summary>
    Fuzzy,

    /// <summary>
    /// Each part of the string, separated by whitespace, is considered for fuzzy matching; each part must match in a fuzzy way.
    /// </summary>
    FuzzyParts,
}
