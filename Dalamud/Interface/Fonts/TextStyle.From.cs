using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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
        new()
        {
            FamilyId = fallback && family is not DalamudDefaultFontId
                           ? FallbackChains.Of(family)
                           : family ?? throw new ArgumentNullException(nameof(family)),
            Size = size,
        };

    /// <summary>Gets a style of the family of a face, in the weight, slant, and stretch of the face.</summary>
    /// <param name="face">The face.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    [OverloadResolutionPriority(1)]
    public static TextStyle From(IFontFaceId face, StyleLength size = default, bool fallback = true) =>
        From(face.Family, size, fallback).WithFace(face.Style);

    /// <summary>Gets a style of an installed font family, in a style.</summary>
    /// <param name="familyName">The family name in any language and case. Names in different languages make
    /// different styles, even for one family.</param>
    /// <param name="style">The weight, slant, and stretch.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    public static TextStyle From(
        string familyName, FontStyle style = default, StyleLength size = default, bool fallback = true) =>
        From(new SystemFontFaceId(new(familyName), style), size, fallback);

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

    /// <summary>Gets a style of a font file (TTF, OTF, TTC).</summary>
    /// <param name="path">Path of the file.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    public static TextStyle FromFile(string path, StyleLength size = default, int fontNo = 0, bool fallback = true) =>
        From(FileFontId.Of(path, fontNo), size, fallback);

    /// <summary>Gets a style of font data in a stream, read now from its current position.</summary>
    /// <param name="stream">The stream. Not closed within.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="name">Name of the font, for display.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <remarks>The style cannot be saved, as in a plugin configuration. Saving it throws.</remarks>
    public static TextStyle From(
        Stream stream, StyleLength size = default, int fontNo = 0, string? name = null, bool fallback = true) =>
        From(new StreamFontId(stream, name, fontNo), size, fallback);

    /// <summary>Gets a style of font data, copied now.</summary>
    /// <param name="data">The data.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="name">Name of the font, for display.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <remarks>The style cannot be saved, as in a plugin configuration. Saving it throws.</remarks>
    public static TextStyle From(
        ReadOnlySpan<byte> data, StyleLength size = default, int fontNo = 0, string? name = null,
        bool fallback = true) =>
        From(new StreamFontId([.. data], name, fontNo), size, fallback);

    /// <summary>Gets a style of a font embedded in an assembly as a manifest resource.</summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="size">The size, as <see cref="Size"/>.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="fallback">
    /// <inheritdoc cref="From(IFontFamilyId, StyleLength, bool)" path="/param[@name='fallback']/node()"/></param>
    /// <returns>The style.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="assembly"/> or <paramref name="resourceName"/> is
    /// <see langword="null"/>.</exception>
    /// <remarks><ul>
    /// <li>The resource is read when the font is made. A resource that does not exist is left out of the font, with
    /// a warning.</li>
    /// <li>The style cannot be saved, as in a plugin configuration. Saving it throws.</li>
    /// </ul></remarks>
    public static TextStyle From(
        Assembly assembly, string resourceName, StyleLength size = default, int fontNo = 0, bool fallback = true) =>
        From(ManifestResourceFontId.Of(assembly, resourceName, fontNo), size, fallback);

    // Keeps the chains of a family and the default font that were made lately. A class of its own, so that it is ready
    // for the static properties made with From.
    private static class FallbackChains
    {
        private static readonly ChainFamilyId?[] Chains = new ChainFamilyId?[8];
        private static int next;

        // Gets a chain of a family and the default font.
        public static ChainFamilyId Of(IFontFamilyId family)
        {
            foreach (var chain in Chains)
            {
                if (chain is not null && Equals(chain.Elements[0].Family, family))
                    return chain;
            }

            var made = new ChainFamilyId([new(family), new(DalamudDefaultFontId.Instance)]);
            Chains[(uint)Interlocked.Increment(ref next) % Chains.Length] = made;
            return made;
        }
    }
}
