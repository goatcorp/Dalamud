using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Dalamud.Interface.Fonts;

/// <summary>Sizes and families of the game's fonts.</summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101", Justification = "Extension receivers are not members")]
public static class GameFontExtensions
{
    /// <summary>Extensions for game fonts at a size.</summary>
    /// <param name="familyAndSize">The font.</param>
    extension(GameFontFamilyAndSize familyAndSize)
    {
        /// <summary>Gets the family.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If not a defined value.</exception>
        /// <remarks><see cref="GameFontFamilyAndSize.Undefined"/> is of <see cref="GameFontFamily.Undefined"/>.
        /// </remarks>
        public GameFontFamily Family => familyAndSize switch
        {
            GameFontFamilyAndSize.Undefined => GameFontFamily.Undefined,
            GameFontFamilyAndSize.Axis96 or GameFontFamilyAndSize.Axis12 or GameFontFamilyAndSize.Axis14
                or GameFontFamilyAndSize.Axis18 or GameFontFamilyAndSize.Axis36 => GameFontFamily.Axis,
            GameFontFamilyAndSize.Jupiter16 or GameFontFamilyAndSize.Jupiter20 or GameFontFamilyAndSize.Jupiter23
                or GameFontFamilyAndSize.Jupiter46 => GameFontFamily.Jupiter,
            GameFontFamilyAndSize.Jupiter45 or GameFontFamilyAndSize.Jupiter90 => GameFontFamily.JupiterNumeric,
            GameFontFamilyAndSize.Meidinger16 or GameFontFamilyAndSize.Meidinger20
                or GameFontFamilyAndSize.Meidinger40 => GameFontFamily.Meidinger,
            GameFontFamilyAndSize.MiedingerMid10 or GameFontFamilyAndSize.MiedingerMid12
                or GameFontFamilyAndSize.MiedingerMid14 or GameFontFamilyAndSize.MiedingerMid18
                or GameFontFamilyAndSize.MiedingerMid36 => GameFontFamily.MiedingerMid,
            GameFontFamilyAndSize.TrumpGothic184 or GameFontFamilyAndSize.TrumpGothic23
                or GameFontFamilyAndSize.TrumpGothic34 or GameFontFamilyAndSize.TrumpGothic68 =>
                GameFontFamily.TrumpGothic,
            _ => throw new ArgumentOutOfRangeException(nameof(familyAndSize), familyAndSize, null),
        };

        /// <summary>Gets the size in points.</summary>
        /// <value>The size, or 0 for <see cref="GameFontFamilyAndSize.Undefined"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException">If not a defined value.</exception>
        public float BaseSizePt => familyAndSize switch
        {
            GameFontFamilyAndSize.Undefined => 0,
            GameFontFamilyAndSize.Axis96 => 9.6f,
            GameFontFamilyAndSize.Axis12 or GameFontFamilyAndSize.MiedingerMid12 => 12,
            GameFontFamilyAndSize.Axis14 or GameFontFamilyAndSize.MiedingerMid14 => 14,
            GameFontFamilyAndSize.Axis18 or GameFontFamilyAndSize.MiedingerMid18 => 18,
            GameFontFamilyAndSize.Axis36 or GameFontFamilyAndSize.MiedingerMid36 => 36,
            GameFontFamilyAndSize.Jupiter16 or GameFontFamilyAndSize.Meidinger16 => 16,
            GameFontFamilyAndSize.Jupiter20 or GameFontFamilyAndSize.Meidinger20 => 20,
            GameFontFamilyAndSize.Jupiter23 or GameFontFamilyAndSize.TrumpGothic23 => 23,
            GameFontFamilyAndSize.Jupiter45 => 45,
            GameFontFamilyAndSize.Jupiter46 => 46,
            GameFontFamilyAndSize.Jupiter90 => 90,
            GameFontFamilyAndSize.Meidinger40 => 40,
            GameFontFamilyAndSize.MiedingerMid10 => 10,
            GameFontFamilyAndSize.TrumpGothic184 => 18.4f,
            GameFontFamilyAndSize.TrumpGothic34 => 34,
            GameFontFamilyAndSize.TrumpGothic68 => 68,
            _ => throw new ArgumentOutOfRangeException(nameof(familyAndSize), familyAndSize, null),
        };

        /// <summary>Gets the size in pixels.</summary>
        /// <value>The size, or 0 for <see cref="GameFontFamilyAndSize.Undefined"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException">If not a defined value.</exception>
        public float BaseSizePx => (familyAndSize.BaseSizePt * 4) / 3;
    }

    /// <summary>Extensions for game font families.</summary>
    /// <param name="family">The family.</param>
    extension(GameFontFamily family)
    {
        /// <summary>Gets the smallest size of the family.</summary>
        /// <value>The font, or <see cref="GameFontFamilyAndSize.Undefined"/> for
        /// <see cref="GameFontFamily.Undefined"/>.</value>
        public GameFontFamilyAndSize FamilyWithMinimumSize =>
            Sizes.TryGetValue(family, out var sizes) ? sizes[0] : GameFontFamilyAndSize.Undefined;

        /// <summary>Gets the size of the family that suits a font size best.</summary>
        /// <param name="sizePt">The font size in points.</param>
        /// <returns>The font, or <see cref="GameFontFamilyAndSize.Undefined"/> for
        /// <see cref="GameFontFamily.Undefined"/> or sizes that are not positive.</returns>
        /// <remarks>The smallest size not smaller in whole pixels, or the largest size.</remarks>
        public GameFontFamilyAndSize GetRecommendedFamilyAndSize(float sizePt) =>
            sizePt > 0 && Sizes.TryGetValue(family, out var sizes)
                ? sizes.FirstOrDefault(x => sizePt <= (((int)(x.BaseSizePx + 0.5f) * 3f) / 4f) + 0.001f, sizes[^1])
                : GameFontFamilyAndSize.Undefined;
    }

    // The sizes of each family, smallest first.
    private static readonly Dictionary<GameFontFamily, GameFontFamilyAndSize[]> Sizes =
        Enum.GetValues<GameFontFamilyAndSize>()
            .Where(x => x != GameFontFamilyAndSize.Undefined)
            .GroupBy(x => x.Family)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.BaseSizePt).ToArray());
}
