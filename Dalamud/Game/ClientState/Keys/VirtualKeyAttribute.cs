namespace Dalamud.Game.ClientState.Keys;

/// <summary>
/// Attribute describing a VirtualKey.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class VirtualKeyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualKeyAttribute"/> class.
    /// </summary>
    /// <param name="fancyName">Fancy name of this key.</param>
    public VirtualKeyAttribute(string fancyName)
    {
        this.FancyName = fancyName;
    }

    /// <summary>
    /// Gets the fancy name of this virtual key.
    /// </summary>
    public string FancyName { get; init; }
}
