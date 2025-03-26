using System.Collections.Generic;

using Dalamud.Utility;

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
    /// Convert the FontAwesomeIcon to a <see cref="string"/> type.
    /// </summary>
    /// <param name="icon">The icon to convert.</param>
    /// <returns>The converted icon.</returns>
    public static string ToIconString(this FontAwesomeIcon icon)
    {
        return string.Empty + (char)icon;
    }

    /// <summary>
    /// Get FontAwesome search terms.
    /// </summary>
    /// <param name="icon">The icon to pull search terms from.</param>
    /// <returns>string array of search terms or empty array if none.</returns>
    public static IEnumerable<string> GetSearchTerms(this FontAwesomeIcon icon)
    {
        var searchTermsAttribute = icon.GetAttribute<FontAwesomeSearchTermsAttribute>();
        return searchTermsAttribute == null ? new string[] { } : searchTermsAttribute.SearchTerms;
    }

    /// <summary>
    /// Get FontAwesome categories.
    /// </summary>
    /// <param name="icon">The icon to pull categories from.</param>
    /// <returns>string array of categories or empty array if none.</returns>
    public static IEnumerable<string> GetCategories(this FontAwesomeIcon icon)
    {
        var categoriesAttribute = icon.GetAttribute<FontAwesomeCategoriesAttribute>();
        return categoriesAttribute == null ? new string[] { } : categoriesAttribute.Categories;
    }
}
