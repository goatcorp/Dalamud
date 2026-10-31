namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>A link of styled text, in offsets into the text as read.</summary>
/// <param name="Offset">The byte offset of what defines the link, such as a SeString link payload.</param>
/// <param name="Length">The length of what defines the link.</param>
/// <param name="Target">The link target.</param>
/// <param name="Detected">Whether a callback found it with <see cref="ITextMarkupCallback.FindLinks"/>.</param>
/// <seealso cref="StyledText.Links"/>
internal readonly record struct TextLink(int Offset, int Length, ReadOnlyMemory<byte> Target, bool Detected = false);
