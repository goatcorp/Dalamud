using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop.DirectX;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Toolkit for use when the build state is <see cref="FontAtlasBuildStep.PreBuild"/>.<br />
/// Not intended for plugins to implement.<br />
/// <br />
/// After <see cref="FontAtlasBuildStepDelegate"/> returns,
/// either <see cref="IFontAtlasBuildToolkit.Font"/> must be set,
/// or at least one font must have been added to the atlas using one of AddFont... functions.
/// </summary>
public interface IFontAtlasBuildToolkitPreBuild : IFontAtlasBuildToolkit
{
    /// <summary>
    /// Queues an item to be disposed after the whole build process gets complete, successful or not.
    /// </summary>
    /// <typeparam name="T">Disposable type.</typeparam>
    /// <param name="disposable">The disposable.</param>
    /// <returns>The same <paramref name="disposable"/>.</returns>
    T DisposeAfterBuild<T>(T disposable) where T : IDisposable;

    /// <summary>
    /// Queues an item to be disposed after the whole build process gets complete, successful or not.
    /// </summary>
    /// <param name="gcHandle">The gc handle.</param>
    /// <returns>The same <paramref name="gcHandle"/>.</returns>
    GCHandle DisposeAfterBuild(GCHandle gcHandle);

    /// <summary>
    /// Queues an item to be disposed after the whole build process gets complete, successful or not.
    /// </summary>
    /// <param name="action">The action to run on dispose.</param>
    void DisposeAfterBuild(Action action);

    /// <summary>
    /// Excludes given font from global scaling.
    /// </summary>
    /// <param name="fontPtr">The font.</param>
    /// <returns>Same <see cref="ImFontPtr"/> with <paramref name="fontPtr"/>.</returns>
    [Obsolete(
        $"Use {nameof(this.SetFontScaleMode)} with {nameof(FontScaleMode)}.{nameof(FontScaleMode.UndoGlobalScale)}")]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    ImFontPtr IgnoreGlobalScale(ImFontPtr fontPtr) => this.SetFontScaleMode(fontPtr, FontScaleMode.UndoGlobalScale);

    /// <summary>
    /// Gets whether global scaling is ignored for the given font.
    /// </summary>
    /// <param name="fontPtr">The font.</param>
    /// <returns>True if ignored.</returns>
    [Obsolete($"Use {nameof(this.GetFontScaleMode)}")]
    [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
    bool IsGlobalScaleIgnored(ImFontPtr fontPtr) => this.GetFontScaleMode(fontPtr) == FontScaleMode.UndoGlobalScale;

    /// <summary>
    /// Sets the scaling mode for the given font.
    /// </summary>
    /// <param name="fontPtr">The font, returned from <see cref="AddFontFromFile"/> and alike.
    /// Note that <see cref="IFontAtlasBuildToolkit.Font"/> property is not guaranteed to be automatically updated upon
    /// calling font adding functions. Pass the return value from font adding functions, not
    /// <see cref="IFontAtlasBuildToolkit.Font"/> property.</param>
    /// <param name="mode">The scaling mode.</param>
    /// <returns><paramref name="fontPtr"/>.</returns>
    ImFontPtr SetFontScaleMode(ImFontPtr fontPtr, FontScaleMode mode);

    /// <summary>
    /// Gets the scaling mode for the given font.
    /// </summary>
    /// <param name="fontPtr">The font.</param>
    /// <returns>The scaling mode.</returns>
    FontScaleMode GetFontScaleMode(ImFontPtr fontPtr);

    /// <summary>
    /// Registers a function to be run after build.
    /// </summary>
    /// <param name="action">The action to run.</param>
    void RegisterPostBuild(Action action);

    /// <summary>
    /// Adds a font from memory region allocated using <see cref="ImGuiHelpers.AllocateMemory"/>.<br />
    /// <b>It WILL crash if you try to use a memory pointer allocated in some other way.</b><br />
    /// <b>
    /// Do NOT call <see cref="ImGuiNative.igMemFree"/> on the <paramref name="dataPointer"/> once this function has
    /// been called, unless <paramref name="freeOnException"/> is set and the function has thrown an error.
    /// </b>
    /// </summary>
    /// <param name="dataPointer">Memory address for the data allocated using <see cref="ImGuiHelpers.AllocateMemory"/>.</param>
    /// <param name="dataSize">The size of the font file..</param>
    /// <param name="fontConfig">The font config.</param>
    /// <param name="freeOnException">Free <paramref name="dataPointer"/> if an exception happens.</param>
    /// <param name="debugTag">A debug tag.</param>
    /// <returns>The newly added font.</returns>
    unsafe ImFontPtr AddFontFromImGuiHeapAllocatedMemory(
        nint dataPointer,
        int dataSize,
        in SafeFontConfig fontConfig,
        bool freeOnException,
        string debugTag)
        => this.AddFontFromImGuiHeapAllocatedMemory(
            (void*)dataPointer,
            dataSize,
            fontConfig,
            freeOnException,
            debugTag);

    /// <summary>
    /// Adds a font from memory region allocated using <see cref="ImGuiHelpers.AllocateMemory"/>.<br />
    /// <b>It WILL crash if you try to use a memory pointer allocated in some other way.</b><br />
    /// <b>
    /// Do NOT call <see cref="ImGuiNative.igMemFree"/> on the <paramref name="dataPointer"/> once this function has
    /// been called, unless <paramref name="freeOnException"/> is set and the function has thrown an error.
    /// </b>
    /// </summary>
    /// <param name="dataPointer">Memory address for the data allocated using <see cref="ImGuiHelpers.AllocateMemory"/>.</param>
    /// <param name="dataSize">The size of the font file..</param>
    /// <param name="fontConfig">The font config.</param>
    /// <param name="freeOnException">Free <paramref name="dataPointer"/> if an exception happens.</param>
    /// <param name="debugTag">A debug tag.</param>
    /// <returns>The newly added font.</returns>
    unsafe ImFontPtr AddFontFromImGuiHeapAllocatedMemory(
        void* dataPointer,
        int dataSize,
        in SafeFontConfig fontConfig,
        bool freeOnException,
        string debugTag);

    /// <summary>
    /// Adds a font from a file.
    /// </summary>
    /// <param name="path">The file path to create a new font from.</param>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The newly added font.</returns>
    ImFontPtr AddFontFromFile(string path, in SafeFontConfig fontConfig);

    /// <summary>
    /// Adds a font from a stream.
    /// </summary>
    /// <param name="stream">The stream to create a new font from.</param>
    /// <param name="fontConfig">The font config.</param>
    /// <param name="leaveOpen">Dispose when this function returns or throws.</param>
    /// <param name="debugTag">A debug tag.</param>
    /// <returns>The newly added font.</returns>
    ImFontPtr AddFontFromStream(Stream stream, in SafeFontConfig fontConfig, bool leaveOpen, string debugTag);

    /// <summary>
    /// Adds a font from memory.
    /// </summary>
    /// <param name="span">The span to create from.</param>
    /// <param name="fontConfig">The font config.</param>
    /// <param name="debugTag">A debug tag.</param>
    /// <returns>The newly added font.</returns>
    ImFontPtr AddFontFromMemory(ReadOnlySpan<byte> span, in SafeFontConfig fontConfig, string debugTag);

    /// <summary>
    /// Adds the default font known to the current font atlas.<br />
    /// <br />
    /// Includes <see cref="AddFontAwesomeIconFont"/> and <see cref="AttachExtraGlyphsForDalamudLanguage"/>.<br />
    /// As this involves adding multiple fonts, calling this function will set <see cref="IFontAtlasBuildToolkit.Font"/>
    /// as the return value of this function, if it was empty before.
    /// </summary>
    /// <param name="sizePx">
    /// Font size in pixels.
    /// If a negative value is supplied,
    /// (<see cref="UiBuilder.DefaultFontSpec"/>.<see cref="IFontSpec.SizePx"/> * <paramref name="sizePx"/>) will be
    /// used as the font size. Specify -1 to use the default font size.
    /// </param>
    /// <param name="glyphRanges">The glyph ranges. Use <see cref="FontAtlasBuildToolkitUtilities"/>.ToGlyphRange to build.</param>
    /// <returns>A font returned from <see cref="ImFontAtlasPtr.AddFont"/>.</returns>
    ImFontPtr AddDalamudDefaultFont(float sizePx, ushort[]? glyphRanges = null);

    /// <summary>
    /// Adds a font that is shipped with Dalamud.<br />
    /// <br />
    /// Note: if game symbols font file is requested but is unavailable,
    /// then it will take the glyphs from game's built-in fonts, and everything in <paramref name="fontConfig"/>
    /// will be ignored but <see cref="SafeFontConfig.SizePx"/>, <see cref="SafeFontConfig.MergeFont"/>,
    /// and <see cref="SafeFontConfig.GlyphRanges"/>.
    /// </summary>
    /// <param name="asset">The font type.</param>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddDalamudAssetFont(DalamudAsset asset, in SafeFontConfig fontConfig);

    /// <summary>
    /// Same with <see cref="AddDalamudAssetFont"/>(<see cref="DalamudAsset.FontAwesomeFreeSolid"/>, ...),
    /// but using only FontAwesome icon ranges.<br />
    /// <see cref="SafeFontConfig.GlyphRanges"/> will be ignored.
    /// </summary>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddFontAwesomeIconFont(in SafeFontConfig fontConfig);

    /// <summary>
    /// Adds the game's symbols into the provided font.<br />
    /// <see cref="SafeFontConfig.GlyphRanges"/> will be ignored.<br />
    /// If the game symbol font file is unavailable, only <see cref="SafeFontConfig.SizePx"/> will be honored.
    /// </summary>
    /// <param name="fontConfig">The font config.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddGameSymbol(in SafeFontConfig fontConfig);

    /// <summary>
    /// Adds the game glyphs to the font.
    /// </summary>
    /// <param name="gameFontStyle">The font style.</param>
    /// <param name="glyphRanges">The glyph ranges.</param>
    /// <param name="mergeFont">The font to merge to. If empty, then a new font will be created.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddGameGlyphs(GameFontStyle gameFontStyle, ushort[]? glyphRanges, ImFontPtr mergeFont);

    /// <summary>Adds glyphs from the Windows default font for the given culture info into the provided font.</summary>
    /// <param name="cultureInfo">The culture info.</param>
    /// <param name="fontConfig">The font config. If <see cref="SafeFontConfig.MergeFont"/> is not set, then
    /// <see cref="IFontAtlasBuildToolkit.Font"/> will be used as the target. If that is empty too, then it will do
    /// nothing.</param>
    /// <param name="weight">The font weight, in range from <c>1</c> to <c>1000</c>. <c>400</c> is regular(normal).
    /// </param>
    /// <param name="stretch">The font stretch, in range from <c>1</c> to <c>9</c>. <c>5</c> is medium(normal).
    /// </param>
    /// <param name="style">The font style, in range from <c>0</c> to <c>2</c>. <c>0</c> is normal.</param>
    /// <remarks>
    /// <para>May do nothing at all if <paramref name="cultureInfo"/> is unsupported by Dalamud font handler.</para> 
    /// <para>See
    /// <a href="https://learn.microsoft.com/en-us/windows/apps/design/globalizing/loc-international-fonts">Microsoft
    /// Learn</a> for the fonts.</para>
    /// </remarks>
    void AttachWindowsDefaultFont(
        CultureInfo cultureInfo,
        in SafeFontConfig fontConfig,
        int weight = (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
        int stretch = (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
        int style = (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL);

    /// <summary>
    /// Adds glyphs of extra languages into the provided font, depending on Dalamud Configuration.<br />
    /// <see cref="SafeFontConfig.GlyphRanges"/> will be ignored.
    /// </summary>
    /// <param name="fontConfig">The font config. If <see cref="SafeFontConfig.MergeFont"/> is not set, then
    /// <see cref="IFontAtlasBuildToolkit.Font"/> will be used as the target. If that is empty too, then it will do
    /// nothing.</param>
    void AttachExtraGlyphsForDalamudLanguage(in SafeFontConfig fontConfig);
}
