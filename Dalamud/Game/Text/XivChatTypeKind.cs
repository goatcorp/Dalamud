namespace Dalamud.Game.Text;

/// <summary>
/// The "kind" of the XivChatType (channel or mask).
/// </summary>
public enum XivChatTypeKind : ushort
{
    /// <summary>
    /// The kind is not known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An unmasked chat channel.
    /// </summary>
    Channel,

    /// <summary>
    /// A chat type mask for source.
    /// </summary>
    Source,

    /// <summary>
    /// A chat type mask for target.
    /// </summary>
    Target,
}
