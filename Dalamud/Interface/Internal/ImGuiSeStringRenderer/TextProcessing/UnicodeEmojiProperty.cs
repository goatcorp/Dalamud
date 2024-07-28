using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Internal.ImGuiSeStringRenderer.TextProcessing;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements should be documented",
    Justification = "Unicode Data")]
[SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1602:Enumeration items should be documented",
    Justification = "Unicode Data")]
[Flags]
internal enum UnicodeEmojiProperty : byte
{
    Emoji = 1 << 0,
    Emoji_Presentation = 1 << 1,
    Emoji_Modifier_Base = 1 << 2,
    Emoji_Modifier = 1 << 3,
    Emoji_Component = 1 << 4,
    Extended_Pictographic = 1 << 5,
}
