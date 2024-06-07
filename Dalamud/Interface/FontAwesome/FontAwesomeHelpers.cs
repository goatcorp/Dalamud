using System.Collections.Generic;
using System.Linq;

using Dalamud.Utility;

namespace Dalamud.Interface;

/// <summary>
/// Class containing various helper methods for use with Font Awesome inside Dalamud.
/// </summary>
public static class FontAwesomeHelpers
{
    /// <summary>
    /// Get all non-obsolete icons.
    /// </summary>
    /// <returns>list of font awesome icons.</returns>
    public static List<FontAwesomeIcon> GetIcons()
    {
        var icons = new List<FontAwesomeIcon>();
        foreach (var icon in Enum.GetValues(typeof(FontAwesomeIcon)).Cast<FontAwesomeIcon>().ToList())
        {
            if (icon.IsObsolete()) continue;
            icons.Add(icon);
        }

        return icons;
    }

    /// <summary>
    /// Get all categories available on non-obsolete icons.
    /// </summary>
    /// <returns>list of font awesome icons.</returns>
    public static string[] GetCategories()
    {
        var icons = GetIcons();
        var result = new List<string>();
        foreach (var icon in icons)
        {
            var categories = icon.GetCategories();
            foreach (var category in categories)
            {
                if (!result.Contains(category))
                {
                    result.Add(category);
                }
            }
        }

        result.Sort();
        result.Insert(0, string.Empty);
        return result.ToArray();
    }

    /// <summary>
    /// Get icons by search term.
    /// </summary>
    /// <param name="search">search term string.</param>
    /// <param name="category">name of category to filter by.</param>
    /// <returns>list array of font awesome icons matching search term.</returns>
    public static List<FontAwesomeIcon> SearchIcons(string search, string category)
    {
        var icons = GetIcons();
        var result = new List<FontAwesomeIcon>();

        // if no filters
        if (string.IsNullOrEmpty(search) && string.IsNullOrEmpty(category))
        {
            return icons;
        }

        // if search with only search term
        if (!string.IsNullOrEmpty(search) && string.IsNullOrEmpty(category))
        {
            foreach (var icon in icons)
            {
                var name = Enum.GetName(icon)?.ToLower();
                var searchTerms = icon.GetSearchTerms();
                if (name!.Contains(search.ToLower()) || searchTerms.Contains(search.ToLower()))
                {
                    result.Add(icon);
                }
            }

            return result;
        }

        // if search with only category
        if (string.IsNullOrEmpty(search) && !string.IsNullOrEmpty(category))
        {
            foreach (var icon in icons)
            {
                var categories = icon.GetCategories();
                if (categories.Contains(category))
                {
                    result.Add(icon);
                }
            }

            return result;
        }

        // search by both terms and category
        foreach (var icon in icons)
        {
            var name = Enum.GetName(icon)?.ToLower();
            var searchTerms = icon.GetSearchTerms();
            var categories = icon.GetCategories();
            if ((name!.Contains(search.ToLower()) || searchTerms.Contains(search.ToLower())) && categories.Contains(category))
            {
                result.Add(icon);
            }
        }

        return result;
    }
}
