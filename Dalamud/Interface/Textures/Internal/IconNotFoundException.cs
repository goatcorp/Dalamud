namespace Dalamud.Interface.Textures.Internal;

/// <summary>
/// Exception thrown when an icon could not be found.
/// </summary>
public class IconNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IconNotFoundException"/> class.
    /// </summary>
    /// <param name="lookup">The lookup that was used to find the icon.</param>
    internal IconNotFoundException(GameIconLookup lookup)
        : base($"The icon with the ID {lookup.IconId} {(lookup.HiRes ? "HiRes" : string.Empty)} {(lookup.ItemHq ? "ItemHq" : string.Empty)} " +
               $"with language {lookup.Language} was not found.")
    { 
    }
}
