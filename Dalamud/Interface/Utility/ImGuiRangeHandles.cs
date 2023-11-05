using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;

using Dalamud.Interface.GameFonts;
using Dalamud.Utility;

namespace Dalamud.Interface.Utility;

/// <summary>
/// Essentially static character ranges for ImGui, but needs to be disposed at some point.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class ImGuiRangeHandles : IDisposable, IServiceType
{
    /// <summary>
    /// Unicode ranges to be used when a Chinese font is desired.
    /// </summary>
    public static readonly UnicodeRange[] ChineseRanges =
    {
        // If adding Chinese characters, those fonts usually have Kanas with them.
        // Mixing Kanas from one font and Chinese character from another looks messy,
        // so we just overwrite Kanas from Chinese font.
        UnicodeRanges.Hiragana,
        UnicodeRanges.Katakana,

        UnicodeRanges.CjkUnifiedIdeographs,
        UnicodeRanges.CjkUnifiedIdeographsExtensionA,
    };

    private readonly DisposeStack disposeStack = new();

    [ServiceManager.ServiceConstructor]
    private ImGuiRangeHandles(GameFontManager gameFontManager)
    {
        this.Dummy = this.disposeStack.Add(GCHandle.Alloc(new ushort[]
        {
            ' ', ' ',
            0,
        }, GCHandleType.Pinned));
        this.Full = this.disposeStack.Add(GCHandle.Alloc(new ushort[]
        {
            0x0001, 0xFFFF,
            0,
        }, GCHandleType.Pinned));
        this.FontAwesome = this.disposeStack.Add(GCHandle.Alloc(new ushort[]
        {
            0xE000, 0xF8FF,
            0,
        }, GCHandleType.Pinned));
        this.Korean = this.disposeStack.Add(GCHandle.Alloc(new ushort[]
        {
            (ushort)UnicodeRanges.HangulJamo.FirstCodePoint,
            (ushort)(UnicodeRanges.HangulJamo.FirstCodePoint +
                     UnicodeRanges.HangulJamo.Length - 1),

            (ushort)UnicodeRanges.HangulSyllables.FirstCodePoint,
            (ushort)(UnicodeRanges.HangulSyllables.FirstCodePoint +
                     UnicodeRanges.HangulSyllables.Length - 1),

            (ushort)UnicodeRanges.HangulCompatibilityJamo.FirstCodePoint,
            (ushort)(UnicodeRanges.HangulCompatibilityJamo.FirstCodePoint +
                     UnicodeRanges.HangulCompatibilityJamo.Length - 1),

            (ushort)UnicodeRanges.HangulJamoExtendedA.FirstCodePoint,
            (ushort)(UnicodeRanges.HangulJamoExtendedA.FirstCodePoint +
                     UnicodeRanges.HangulJamoExtendedA.Length - 1),

            (ushort)UnicodeRanges.HangulJamoExtendedB.FirstCodePoint,
            (ushort)(UnicodeRanges.HangulJamoExtendedB.FirstCodePoint +
                     UnicodeRanges.HangulJamoExtendedB.Length - 1),

            0,
        }, GCHandleType.Pinned));
        this.Chinese = this.disposeStack.Add(
            GCHandle.Alloc(
                ChineseRanges
                    .SelectMany(x => new[] { (ushort)x.FirstCodePoint, (ushort)(x.FirstCodePoint + x.Length - 1) })
                    .Append((ushort)0)
                    .ToArray(),
                GCHandleType.Pinned));
        this.Axis12 =
            this.disposeStack.Add(gameFontManager.ToGlyphRanges(GameFontFamilyAndSize.Axis12));
        this.Axis12WithoutJapanese =
            this.disposeStack.Add(gameFontManager.ToGlyphRanges(GameFontFamilyAndSize.Axis12, ChineseRanges));
    }

    /// <summary>
    /// Gets a rangle handle only containing a single space.
    /// </summary>
    public GCHandle Dummy { get; }

    /// <summary>
    /// Gets a rangle handle containing all UCS-2 characters. 
    /// </summary>
    public GCHandle Full { get; }

    /// <summary>
    /// Gets a rangle handle containing range for FontAwesome.
    /// </summary>
    public GCHandle FontAwesome { get; }

    /// <summary>
    /// Gets a rangle handle containing all Korean characters. 
    /// </summary>
    public GCHandle Korean { get; }

    /// <summary>
    /// Gets a rangle handle containing all Chinese characters and Kana. 
    /// </summary>
    public GCHandle Chinese { get; }

    /// <summary>
    /// Gets a rangle handle containing all default game characters. 
    /// </summary>
    public GCHandle Axis12 { get; }

    /// <summary>
    /// Gets a rangle handle containing all default game characters without Chinese characters and Kana. 
    /// </summary>
    public GCHandle Axis12WithoutJapanese { get; }

    /// <inheritdoc/>
    public void Dispose() => this.disposeStack.Dispose();
}
