namespace Dalamud.Interface;

/// <summary>
/// Set categories associated with a font awesome icon.
/// </summary>
public class FontAwesomeCategoriesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontAwesomeCategoriesAttribute"/> class.
    /// </summary>
    /// <param name="categories">categories for enum member.</param>
    public FontAwesomeCategoriesAttribute(string[] categories) => this.Categories = categories;

    /// <summary>
    /// Gets or sets categories.
    /// </summary>
    public string[] Categories { get; set; }
}
