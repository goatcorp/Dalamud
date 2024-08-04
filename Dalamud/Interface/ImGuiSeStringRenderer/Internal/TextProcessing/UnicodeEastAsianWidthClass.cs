using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;

/// <summary><a href="https://www.unicode.org/reports/tr11/">Unicode east asian width</a>.</summary>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unicode Data")]
internal enum UnicodeEastAsianWidthClass : byte
{
    /// <summary>East Asian Ambiguous.</summary>
    A,

    /// <summary>East Asian Fullwidth.</summary>
    F,

    /// <summary>East Asian Halfwidth.</summary>
    H,

    /// <summary>Neutral.</summary>
    N,

    /// <summary>East Asian Narrow.</summary>
    Na,

    /// <summary>East Asian Wide.</summary>
    W,
}
