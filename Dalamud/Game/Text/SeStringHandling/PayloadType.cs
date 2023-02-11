namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// All parsed types of SeString payloads.
/// </summary>
public enum PayloadType
{
    /// <summary>
    /// An unknown SeString.
    /// </summary>
    Unknown,

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
    /// A SeString payload representing a custom clickable link for dalamud plugins.
    /// </summary>
    DalamudLink,

    /// <summary>
    /// An SeString payload representing a newline character.
    /// </summary>
    NewLine,

    /// <summary>
    /// An SeString payload representing a doublewide SE hypen.
    /// </summary>
    SeHyphen,

    /// <summary>
    /// An SeString payload representing a party finder link.
    /// </summary>
    PartyFinder,
}
