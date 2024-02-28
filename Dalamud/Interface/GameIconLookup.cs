using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface;

/// <summary>
/// Represents a lookup for a game icon.
/// </summary>
/// <param name="IconId">The icon ID.</param>
/// <param name="ItemHq">Whether the HQ icon is requested, where HQ is in the context of items.</param>
/// <param name="HiRes">Whether the high-resolution icon is requested.</param>
/// <param name="Language">The language of the icon to load.</param>
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1313:Parameter names should begin with lower-case letter",
    Justification = "no")]
public record struct GameIconLookup(
    uint IconId,
    bool ItemHq = false,
    bool HiRes = true,
    ClientLanguage? Language = null);
