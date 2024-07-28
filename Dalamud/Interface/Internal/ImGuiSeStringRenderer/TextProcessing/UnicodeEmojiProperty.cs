using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Internal.ImGuiSeStringRenderer.TextProcessing;

/// <summary><a href="https://www.unicode.org/reports/tr51/#Emoji_Characters">Unicode emoji property</a>.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
[Flags]
internal enum UnicodeEmojiProperty : byte
{
    /// <summary>Characters that are emoji.</summary>
    Emoji = 1 << 0,

    /// <summary>Characters that have emoji presentation by default.</summary>
    Emoji_Presentation = 1 << 1,

    /// <summary>Characters that are emoji modifiers.</summary>
    Emoji_Modifier = 1 << 2,

    /// <summary>Characters that can serve as a base for emoji modifiers.</summary>
    Emoji_Modifier_Base = 1 << 3,

    /// <summary>Characters used in emoji sequences that normally do not appear on emoji keyboards as separate choices,
    /// such as keycap base characters or Regional_Indicator characters.</summary>
    Emoji_Component = 1 << 4,

    /// <summary>Characters that are used to future-proof segmentation.</summary>
    Extended_Pictographic = 1 << 5,
}
