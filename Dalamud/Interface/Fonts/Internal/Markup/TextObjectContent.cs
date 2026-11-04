using System.Numerics;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>What an inline object of styled text shows.</summary>
/// <param name="Entity">The entity.</param>
/// <param name="SourceOffset">Byte offset of the entity in the text as read.</param>
/// <param name="Font">The font of the style given to the entity.</param>
/// <param name="Size">The size of the entity in pixels, with a height relative to the line resolved.</param>
/// <seealso cref="StyledText.Contents"/>
internal readonly record struct TextObjectContent(TextEntity Entity, int SourceOffset, FontImpl Font, Vector2 Size);
