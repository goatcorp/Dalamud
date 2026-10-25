namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>A link of styled text, in offsets into the text as read.</summary>
/// <param name="Offset">The byte offset of what defines the link, such as a SeString link payload.</param>
/// <param name="Length">The length of what defines the link.</param>
/// <param name="TargetOffset">The byte offset of the link target.</param>
/// <param name="TargetLength">The length of the link target.</param>
/// <seealso cref="StyledText.Links"/>
internal readonly record struct TextLink(int Offset, int Length, int TargetOffset, int TargetLength);
