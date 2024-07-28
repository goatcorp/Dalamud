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
internal enum UnicodeEastAsianWidthClass : byte
{
    A = 0,
    F = 1,
    H = 2,
    N = 3,
    Na = 4,
    W = 5,
}
