using System.Runtime.CompilerServices;
using System.Threading;

using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

public partial record struct TextStyle
{
    /// <summary>Gets a style of a font family.</summary>
    /// <param name="family">The family.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">Whether characters the family lacks come from the default font. If so, the family is
    /// a <see cref="ChainFamilyId"/> of it and <see cref="DalamudDefaultFontId"/>.</param>
    /// <returns>The style.</returns>
    public static TextStyle From(IFontFamilyId family, StyleLength size = default, bool fallback = true) =>
        new() { FamilyId = WithFallback(family, fallback), Size = size };

    /// <summary>Gets a style of the family of a face, in the weight, slant, and stretch of the face.</summary>
    /// <param name="face">The face.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    [OverloadResolutionPriority(1)]
    public static TextStyle From(IFontFaceId face, StyleLength size = default, bool fallback = true) =>
        From(face.Family, size, fallback).WithFace(face.Style);

    /// <summary>Gets a style of a font family found by its name.</summary>
    /// <param name="familyName">The name of a Dalamud font, game font, or system font family. Names of an installed
    /// system family in any language canonicalize to the same family. An unrecognized name creates an unresolved
    /// <see cref="SystemFontFamilyId"/>, which falls back to the default font. Plugin registrations are not searched.
    /// Use <see cref="IUiBuilder.FindFontFamily"/> for those.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <remarks>Make the style once and keep it, rather than calling this every frame.</remarks>
    public static TextStyle From(string familyName, StyleLength size = default, bool fallback = true) =>
        From(FindFamily(familyName), size, fallback);

    /// <summary>Gets a style of a font family found by its name, in a style.</summary>
    /// <param name="familyName">
    /// <inheritdoc cref="From(string, StyleLength, bool)" path="/param[@name='familyName']/node()"/></param>
    /// <param name="style">The weight, slant, and stretch. Values left at 0 stay unset, as in
    /// <see cref="From(IFontFamilyId, StyleLength, bool)"/>.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <remarks><inheritdoc cref="From(string, StyleLength, bool)" path="/remarks/node()"/></remarks>
    public static TextStyle From(
        string familyName, FontStyle style, StyleLength size = default, bool fallback = true) =>
        From(FindFamily(familyName), size, fallback).WithFace(style);

    /// <summary>Gets a style of a Dalamud asset font.</summary>
    /// <param name="fontAsset">A font asset.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="fontAsset"/> is not a font.</exception>
    public static TextStyle From(
        DalamudAsset fontAsset, StyleLength size = default, int fontNo = 0, bool fallback = true) =>
        From(new DalamudAssetFontId(fontAsset, fontNo), size, fallback);

    /// <summary>Gets a style of a game font.</summary>
    /// <param name="family">The family.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    public static TextStyle From(GameFontFamily family, StyleLength size = default, bool fallback = true) =>
        From(new GameFontId(family), size, fallback);

    /// <summary>Gets a style of a game font at one of its sizes.</summary>
    /// <param name="familyAndSize">The family and size.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    public static TextStyle From(GameFontFamilyAndSize familyAndSize, bool fallback = true) =>
        From(familyAndSize.Family, StyleLength.Px(familyAndSize.BaseSizePx), fallback);

    /// <summary>Gets a family, or a chain of it and the default font.</summary>
    /// <param name="family">The family.</param>
    /// <param name="fallback">Whether to chain the default font after it.</param>
    /// <returns>The family or the chain. A family that already ends in the default font is returned as it is.
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="family"/> is <see langword="null"/>.</exception>
    internal static IFontFamilyId WithFallback(IFontFamilyId family, bool fallback)
    {
        ArgumentNullException.ThrowIfNull(family);
        return !fallback || family.EndsWithDefault ? family : FallbackChains.Of(family);
    }

    private static IFontFamilyId FindFamily(string familyName) =>
        FontRegistry.FindFamily(familyName, null) ?? new SystemFontFamilyId(familyName);

    // Separate static class so it initializes before TextStyle's static properties that call From.
    private static class FallbackChains
    {
        // Chains by the instance of their family, which callers usually keep. It does not keep the families alive.
        private static readonly ConditionalWeakTable<IFontFamilyId, ChainFamilyId> ByInstance = new();

        // Small ring buffer for equal family instances not yet in ByInstance.
        private static readonly ChainFamilyId?[] Recent = new ChainFamilyId?[8];
        private static int next;

        public static ChainFamilyId Of(IFontFamilyId family)
        {
            if (ByInstance.TryGetValue(family, out var chain) && chain is not null)
                return chain;

            foreach (var recent in Recent)
            {
                if (recent is not null && Equals(recent.Elements[0], family))
                {
                    ByInstance.TryAdd(family, recent);
                    return recent;
                }
            }

            var made = ChainFamilyId.Of(family, DalamudDefaultFontId.Instance);
            Recent[(uint)Interlocked.Increment(ref next) % Recent.Length] = made;
            ByInstance.TryAdd(family, made);
            return made;
        }
    }
}
