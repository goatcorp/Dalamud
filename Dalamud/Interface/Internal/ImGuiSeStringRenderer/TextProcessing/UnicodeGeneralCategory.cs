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
internal enum UnicodeGeneralCategory : byte
{
    Cn = 0,
    Lu = 1,
    Ll = 2,
    Lt = 3,
    Lm = 4,
    Lo = 5,
    Mn = 6,
    Me = 7,
    Mc = 8,
    Nd = 9,
    Nl = 10,
    No = 11,
    Zs = 12,
    Zl = 13,
    Zp = 14,
    Cc = 15,
    Cf = 16,
    Co = 17,
    Cs = 18,
    Pd = 19,
    Ps = 20,
    Pe = 21,
    Pc = 22,
    Po = 23,
    Sm = 24,
    Sc = 25,
    Sk = 26,
    So = 27,
    Pi = 28,
    Pf = 29,
}
