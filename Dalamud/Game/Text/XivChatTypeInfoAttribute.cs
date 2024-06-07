namespace Dalamud.Game.Text;

/// <summary>
/// Storage for relevant information associated with the chat type.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class XivChatTypeInfoAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XivChatTypeInfoAttribute"/> class.
    /// </summary>
    /// <param name="fancyName">The fancy name.</param>
    /// <param name="slug">The name slug.</param>
    /// <param name="defaultColor">The default color.</param>
    internal XivChatTypeInfoAttribute(string fancyName, string slug, uint defaultColor)
    {
        this.FancyName = fancyName;
        this.Slug = slug;
        this.DefaultColor = defaultColor;
    }

    /// <summary>
    /// Gets the "fancy" name of the type.
    /// </summary>
    public string FancyName { get; }

    /// <summary>
    /// Gets the type name slug or short-form.
    /// </summary>
    public string Slug { get; }

    /// <summary>
    /// Gets the type default color.
    /// </summary>
    public uint DefaultColor { get; }
}
