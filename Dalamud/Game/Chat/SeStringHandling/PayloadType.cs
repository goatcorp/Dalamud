
namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// All parsed types of SeString payloads.
    /// </summary>
    public enum PayloadType
    {
        /// <summary>
        /// An SeString payload representing a player link.
        /// </summary>
        Player,
        /// <summary>
        /// An SeString payload representing an Item link.
        /// </summary>
        Item,
        /// <summary>
        /// An SeString payload representing an Status Effect link.
        /// </summary>
        Status,
        /// <summary>
        /// An SeString payload representing raw, typed text.
        /// </summary>
        RawText,
        /// <summary>
        /// An SeString payload representing a text foreground color.
        /// </summary>
        UIForeground,
        /// <summary>
        /// An SeString payload representing a text glow color.
        /// </summary>
        UIGlow,
        /// <summary>
        /// An SeString payload representing a map position link, such as from &lt;flag&gt; or &lt;pos&gt;.
        /// </summary>
        MapLink,
        /// <summary>
        /// An SeString payload representing an auto-translate dictionary entry.
        /// </summary>
        AutoTranslateText,
        /// <summary>
        /// An SeString payload representing italic emphasis formatting on text.
        /// </summary>
        EmphasisItalic,
        /// <summary>
        /// An SeString payload representing a bitmap icon.
        /// </summary>
        Icon,
        /// <summary>
        /// A SeString payload representing a quest link.
        /// </summary>
        Quest,
        /// <summary>
        /// An SeString payload representing any data we don't handle.
        /// </summary>
        Unknown
    }
}
