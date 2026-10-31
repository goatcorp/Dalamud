namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>What happened to a link, kept until asked for.</summary>
/// <param name="Markup">The markup of the text.</param>
/// <param name="Offset">The byte offset of what defines the link, in the text as drawn.</param>
/// <param name="Length">The length of what defines the link, in the text as drawn.</param>
/// <param name="Source">The text as read. Layouts never write to it after it is made.</param>
/// <param name="Link">The link, in offsets into <paramref name="Source"/>.</param>
/// <param name="Flags">What happened.</param>
/// <param name="Held">Whether the link is held down.</param>
internal readonly record struct TextInteractionRecord(
    TextMarkup Markup,
    int Offset,
    int Length,
    byte[] Source,
    TextLink Link,
    TextInteractionFlags Flags,
    bool Held);
