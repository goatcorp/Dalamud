namespace Dalamud.Interface;

/// <summary>
/// Set search terms associated with a font awesome icon.
/// </summary>
public class FontAwesomeSearchTermsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontAwesomeSearchTermsAttribute"/> class.
    /// </summary>
    /// <param name="searchTerms">search terms for enum member.</param>
    public FontAwesomeSearchTermsAttribute(string[] searchTerms) => this.SearchTerms = searchTerms;

    /// <summary>
    /// Gets or sets search terms.
    /// </summary>
    public string[] SearchTerms { get; set; }
}
