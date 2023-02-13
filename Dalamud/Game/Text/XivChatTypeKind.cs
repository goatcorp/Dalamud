namespace Dalamud.Game.Text;

/// <summary>
/// The "kind" of the XivChatType (channel or mask).
/// </summary>
public enum XivChatTypeKind : ushort
{
    /// <summary>
    /// An unmasked chat channel.
    /// </summary>
    Channel = 0,

    /// <summary>
    /// A chat type mask for source.
    /// </summary>
    Source = 1,

    /// <summary>
    /// A chat type mask for target.
    /// </summary>
    Target = 2,
}
