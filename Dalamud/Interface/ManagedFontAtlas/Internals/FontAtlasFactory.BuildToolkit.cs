using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Unicode;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using SharpDX.DXGI;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Standalone font atlas.
/// </summary>
internal sealed partial class FontAtlasFactory
{
    private static readonly Dictionary<ulong, List<(char Left, char Right, float Distance)>> PairAdjustmentsCache =
        new();

    /// <summary>
    /// Implementations for <see cref="IFontAtlasBuildToolkitPreBuild"/> and
    /// <see cref="IFontAtlasBuildToolkitPostBuild"/>.
    /// </summary>
    private class BuildToolkit : IFontAtlasBuildToolkitPreBuild, IFontAtlasBuildToolkitPostBuild, IDisposable
    {
        private static readonly ushort FontAwesomeIconMin =
            (ushort)Enum.GetValues<FontAwesomeIcon>().Where(x => x > 0).Min();

        private static readonly ushort FontAwesomeIconMax =
            (ushort)Enum.GetValues<FontAwesomeIcon>().Where(x => x > 0).Max();

        private readonly DisposeSafety.ScopedFinalizer disposeAfterBuild = new();
        private readonly GamePrebakedFontHandle.HandleSubstance gameFontHandleSubstance;
        private readonly FontAtlasFactory factory;
        private readonly FontAtlasBuiltData data;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildToolkit"/> class.
        /// </summary>
        /// <param name="factory">An instance of <see cref="FontAtlasFactory"/>.</param>
        /// <param name="data">New atlas.</param>
        /// <param name="gameFontHandleSubstance">An instance of <see cref="GamePrebakedFontHandle.HandleSubstance"/>.</param>
        /// <param name="isAsync">Specify whether the current build operation is an asynchronous one.</param>
        public BuildToolkit(
            FontAtlasFactory factory,
            FontAtlasBuiltData data,
            GamePrebakedFontHandle.HandleSubstance gameFontHandleSubstance,
            bool isAsync)
        {
            this.data = data;
            this.gameFontHandleSubstance = gameFontHandleSubstance;
            this.IsAsyncBuildOperation = isAsync;
            this.factory = factory;
        }

        /// <inheritdoc cref="IFontAtlasBuildToolkit.Font"/>
        public ImFontPtr Font { get; set; }

        /// <inheritdoc cref="IFontAtlasBuildToolkit.Font"/>
        public float Scale => this.data.Scale;

        /// <inheritdoc/>
        public bool IsAsyncBuildOperation { get; }

        /// <inheritdoc/>
        public FontAtlasBuildStep BuildStep { get; set; }

        /// <inheritdoc/>
        public ImFontAtlasPtr NewImAtlas => this.data.Atlas;

        /// <inheritdoc/>
        public ImVectorWrapper<ImFontPtr> Fonts => this.data.Fonts;

        /// <summary>
        /// Gets the list of fonts to ignore global scale.
        /// </summary>
        public List<ImFontPtr> GlobalScaleExclusions { get; } = new();

        /// <inheritdoc/>
        public void Dispose() => this.disposeAfterBuild.Dispose();

        /// <inheritdoc/>
        public T2 DisposeAfterBuild<T2>(T2 disposable) where T2 : IDisposable =>
            this.disposeAfterBuild.Add(disposable);

        /// <inheritdoc/>
        public GCHandle DisposeAfterBuild(GCHandle gcHandle) => this.disposeAfterBuild.Add(gcHandle);

        /// <inheritdoc/>
        public void DisposeAfterBuild(Action action) => this.disposeAfterBuild.Add(action);

        /// <inheritdoc/>
        public T DisposeWithAtlas<T>(T disposable) where T : IDisposable => this.data.Garbage.Add(disposable);

        /// <inheritdoc/>
        public GCHandle DisposeWithAtlas(GCHandle gcHandle) => this.data.Garbage.Add(gcHandle);

        /// <inheritdoc/>
        public void DisposeWithAtlas(Action action) => this.data.Garbage.Add(action);

        /// <inheritdoc/>
        public ImFontPtr IgnoreGlobalScale(ImFontPtr fontPtr)
        {
            this.GlobalScaleExclusions.Add(fontPtr);
            return fontPtr;
        }

        /// <inheritdoc/>
        public bool IsGlobalScaleIgnored(ImFontPtr fontPtr) =>
            this.GlobalScaleExclusions.Contains(fontPtr);

        /// <inheritdoc/>
        public int StoreTexture(IDalamudTextureWrap textureWrap, bool disposeOnError) =>
            this.data.AddNewTexture(textureWrap, disposeOnError);

        /// <inheritdoc/>
        public unsafe ImFontPtr AddFontFromImGuiHeapAllocatedMemory(
            void* dataPointer,
            int dataSize,
            in SafeFontConfig fontConfig,
            bool freeOnException,
            string debugTag)
        {
            Log.Verbose(
                "[{name}] 0x{atlas:X}: {funcname}(0x{dataPointer:X}, 0x{dataSize:X}, ...) from {tag}",
                this.data.Owner?.Name ?? "(error)",
                (nint)this.NewImAtlas.NativePtr,
                nameof(this.AddFontFromImGuiHeapAllocatedMemory),
                (nint)dataPointer,
                dataSize,
                debugTag);

            try
            {
                fontConfig.ThrowOnInvalidValues();

                var raw = fontConfig.Raw with
                {
                    FontData = dataPointer,
                    FontDataSize = dataSize,
                };

                if (fontConfig.GlyphRanges is not { Length: > 0 } ranges)
                    ranges = new ushort[] { 1, 0xFFFE, 0 };

                raw.GlyphRanges = (ushort*)this.DisposeAfterBuild(
                    GCHandle.Alloc(ranges, GCHandleType.Pinned)).AddrOfPinnedObject();

                TrueTypeUtils.CheckImGuiCompatibleOrThrow(raw);

                var font = this.NewImAtlas.AddFont(&raw);

                var dataHash = default(HashCode);
                dataHash.AddBytes(new(dataPointer, dataSize));
                var hashIdent = (uint)dataHash.ToHashCode() | ((ulong)dataSize << 32);

                List<(char Left, char Right, float Distance)> pairAdjustments;
                lock (PairAdjustmentsCache)
                {
                    if (!PairAdjustmentsCache.TryGetValue(hashIdent, out pairAdjustments))
                    {
                        PairAdjustmentsCache.Add(hashIdent, pairAdjustments = new());
                        try
                        {
                            pairAdjustments.AddRange(TrueTypeUtils.ExtractHorizontalPairAdjustments(raw).ToArray());
                        }
                        catch
                        {
                            // don't care
                        }
                    }
                }

                foreach (var pair in pairAdjustments)
                {
                    if (!ImGuiHelpers.IsCodepointInSuppliedGlyphRangesUnsafe(pair.Left, raw.GlyphRanges))
                        continue;
                    if (!ImGuiHelpers.IsCodepointInSuppliedGlyphRangesUnsafe(pair.Right, raw.GlyphRanges))
                        continue;

                    font.AddKerningPair(pair.Left, pair.Right, pair.Distance * raw.SizePixels);
                }

                return font;
            }
            catch
            {
                if (freeOnException)
                    ImGuiNative.igMemFree(dataPointer);
                throw;
            }
        }

        /// <inheritdoc/>
        public ImFontPtr AddFontFromFile(string path, in SafeFontConfig fontConfig)
        {
            return this.AddFontFromStream(
                File.OpenRead(path),
                fontConfig,
                false,
                $"{nameof(this.AddFontFromFile)}({path})");
        }

        /// <inheritdoc/>
        public unsafe ImFontPtr AddFontFromStream(
            Stream stream,
            in SafeFontConfig fontConfig,
            bool leaveOpen,
            string debugTag)
        {
            using var streamCloser = leaveOpen ? null : stream;
            if (!stream.CanSeek)
            {
                // There is no need to dispose a MemoryStream.
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                stream = ms;
            }

            var length = checked((int)(uint)stream.Length);
            var memory = ImGuiHelpers.AllocateMemory(length);
            try
            {
                stream.ReadExactly(new(memory, length));
                return this.AddFontFromImGuiHeapAllocatedMemory(
                    memory,
                    length,
                    fontConfig,
                    false,
                    $"{nameof(this.AddFontFromStream)}({debugTag})");
            }
            catch
            {
                ImGuiNative.igMemFree(memory);
                throw;
            }
        }

        /// <inheritdoc/>
        public unsafe ImFontPtr AddFontFromMemory(
            ReadOnlySpan<byte> span,
            in SafeFontConfig fontConfig,
            string debugTag)
        {
            var length = span.Length;
            var memory = ImGuiHelpers.AllocateMemory(length);
            try
            {
                span.CopyTo(new(memory, length));
                return this.AddFontFromImGuiHeapAllocatedMemory(
                    memory,
                    length,
                    fontConfig,
                    false,
                    $"{nameof(this.AddFontFromMemory)}({debugTag})");
            }
            catch
            {
                ImGuiNative.igMemFree(memory);
                throw;
            }
        }

        /// <inheritdoc/>
        public ImFontPtr AddDalamudDefaultFont(float sizePx, ushort[]? glyphRanges)
        {
            ImFontPtr font;
            glyphRanges ??= this.factory.DefaultGlyphRanges;
            if (Service<InterfaceManager>.Get().UseAxis)
            {
                font = this.AddGameGlyphs(new(GameFontFamily.Axis, sizePx), glyphRanges, default);
            }
            else
            {
                font = this.AddDalamudAssetFont(
                    DalamudAsset.NotoSansJpMedium,
                    new() { SizePx = sizePx, GlyphRanges = glyphRanges });
                this.AddGameSymbol(new() { SizePx = sizePx, MergeFont = font });
            }

            this.AttachExtraGlyphsForDalamudLanguage(new() { SizePx = sizePx, MergeFont = font });
            if (this.Font.IsNull())
                this.Font = font;
            return font;
        }

        /// <inheritdoc/>
        public ImFontPtr AddDalamudAssetFont(DalamudAsset asset, in SafeFontConfig fontConfig)
        {
            if (asset.GetPurpose() != DalamudAssetPurpose.Font)
                throw new ArgumentOutOfRangeException(nameof(asset), asset, "Must have the purpose of Font.");

            switch (asset)
            {
                case DalamudAsset.LodestoneGameSymbol when this.factory.HasGameSymbolsFontFile:
                    return this.factory.AddFont(
                        this,
                        asset,
                        fontConfig with
                        {
                            FontNo = 0,
                            SizePx = (fontConfig.SizePx * 3) / 2,
                        });

                case DalamudAsset.LodestoneGameSymbol when !this.factory.HasGameSymbolsFontFile:
                {
                    return this.AddGameGlyphs(
                        new(GameFontFamily.Axis, fontConfig.SizePx),
                        fontConfig.GlyphRanges,
                        fontConfig.MergeFont);
                }

                default:
                    return this.factory.AddFont(
                        this,
                        asset,
                        fontConfig with
                        {
                            FontNo = 0,
                        });
            }
        }

        /// <inheritdoc/>
        public ImFontPtr AddFontAwesomeIconFont(in SafeFontConfig fontConfig) => this.AddDalamudAssetFont(
            DalamudAsset.FontAwesomeFreeSolid,
            fontConfig with
            {
                GlyphRanges = new ushort[] { FontAwesomeIconMin, FontAwesomeIconMax, 0 },
            });

        /// <inheritdoc/>
        public ImFontPtr AddGameSymbol(in SafeFontConfig fontConfig) =>
            this.AddDalamudAssetFont(
                DalamudAsset.LodestoneGameSymbol,
                fontConfig with
                {
                    GlyphRanges = new ushort[]
                    {
                        GamePrebakedFontHandle.SeIconCharMin,
                        GamePrebakedFontHandle.SeIconCharMax,
                        0,
                    },
                });

        /// <inheritdoc/>
        public ImFontPtr AddGameGlyphs(GameFontStyle gameFontStyle, ushort[]? glyphRanges, ImFontPtr mergeFont) =>
            this.gameFontHandleSubstance.AttachGameGlyphs(this, mergeFont, gameFontStyle, glyphRanges);

        /// <inheritdoc/>
        public void AttachExtraGlyphsForDalamudLanguage(in SafeFontConfig fontConfig)
        {
            var dalamudConfiguration = Service<DalamudConfiguration>.Get();
            if (dalamudConfiguration.EffectiveLanguage == "ko")
            {
                this.AddDalamudAssetFont(
                    DalamudAsset.NotoSansKrRegular,
                    fontConfig with
                    {
                        GlyphRanges = ImGuiHelpers.CreateImGuiRangesFrom(
                            UnicodeRanges.HangulJamo,
                            UnicodeRanges.HangulCompatibilityJamo,
                            UnicodeRanges.HangulSyllables,
                            UnicodeRanges.HangulJamoExtendedA,
                            UnicodeRanges.HangulJamoExtendedB),
                    });
            }
        }

        public void PreBuildSubstances()
        {
            foreach (var substance in this.data.Substances)
                substance.OnPreBuild(this);
            foreach (var substance in this.data.Substances)
                substance.OnPreBuildCleanup(this);
        }

        public unsafe void PreBuild()
        {
            var configData = this.data.ConfigData;
            foreach (ref var config in configData.DataSpan)
            {
                if (this.GlobalScaleExclusions.Contains(new(config.DstFont)))
                    continue;

                config.SizePixels *= this.Scale;

                config.GlyphMaxAdvanceX *= this.Scale;
                if (float.IsInfinity(config.GlyphMaxAdvanceX))
                    config.GlyphMaxAdvanceX = config.GlyphMaxAdvanceX > 0 ? float.MaxValue : -float.MaxValue;

                config.GlyphMinAdvanceX *= this.Scale;
                if (float.IsInfinity(config.GlyphMinAdvanceX))
                    config.GlyphMinAdvanceX = config.GlyphMinAdvanceX > 0 ? float.MaxValue : -float.MaxValue;

                config.GlyphOffset *= this.Scale;
            }
        }

        public void DoBuild()
        {
            // ImGui will call AddFontDefault() on Build() call.
            // AddFontDefault() will reliably crash, when invoked multithreaded.
            // We add a dummy font to prevent that.
            if (this.data.ConfigData.Length == 0)
            {
                this.AddDalamudAssetFont(
                    DalamudAsset.NotoSansJpMedium,
                    new() { GlyphRanges = new ushort[] { ' ', ' ', '\0' }, SizePx = 1 });
            }

            if (!this.NewImAtlas.Build())
                throw new InvalidOperationException("ImFontAtlas.Build failed");

            this.BuildStep = FontAtlasBuildStep.PostBuild;
        }

        public unsafe void PostBuild()
        {
            var scale = this.Scale;
            foreach (ref var font in this.Fonts.DataSpan)
            {
                if (!this.GlobalScaleExclusions.Contains(font))
                    font.AdjustGlyphMetrics(1 / scale, 1 / scale);

                foreach (var c in FallbackCodepoints)
                {
                    var g = font.FindGlyphNoFallback(c);
                    if (g.NativePtr == null)
                        continue;

                    font.UpdateFallbackChar(c);
                    break;
                }

                foreach (var c in EllipsisCodepoints)
                {
                    var g = font.FindGlyphNoFallback(c);
                    if (g.NativePtr == null)
                        continue;

                    font.EllipsisChar = c;
                    break;
                }
            }
        }

        public void PostBuildSubstances()
        {
            foreach (var substance in this.data.Substances)
                substance.OnPostBuild(this);
        }

        public unsafe void UploadTextures()
        {
            var buf = Array.Empty<byte>();
            try
            {
                var use4 = this.factory.InterfaceManager.SupportsDxgiFormat(Format.B4G4R4A4_UNorm);
                var bpp = use4 ? 2 : 4;
                var width = this.NewImAtlas.TexWidth;
                var height = this.NewImAtlas.TexHeight;
                foreach (ref var texture in this.data.ImTextures.DataSpan)
                {
                    if (texture.TexID != 0)
                    {
                        // Nothing to do
                    }
                    else if (texture.TexPixelsRGBA32 is not null)
                    {
                        var wrap = this.factory.InterfaceManager.LoadImageFromDxgiFormat(
                            new(texture.TexPixelsRGBA32, width * height * 4),
                            width * 4,
                            width,
                            height,
                            use4 ? Format.B4G4R4A4_UNorm : Format.R8G8B8A8_UNorm);
                        this.data.AddExistingTexture(wrap);
                        texture.TexID = wrap.ImGuiHandle;
                    }
                    else if (texture.TexPixelsAlpha8 is not null)
                    {
                        var numPixels = width * height;
                        if (buf.Length < numPixels * bpp)
                        {
                            ArrayPool<byte>.Shared.Return(buf);
                            buf = ArrayPool<byte>.Shared.Rent(numPixels * bpp);
                        }

                        fixed (void* pBuf = buf)
                        {
                            var sourcePtr = texture.TexPixelsAlpha8;
                            if (use4)
                            {
                                var target = (ushort*)pBuf;
                                while (numPixels-- > 0)
                                {
                                    *target = (ushort)((*sourcePtr << 8) | 0x0FFF);
                                    target++;
                                    sourcePtr++;
                                }
                            }
                            else
                            {
                                var target = (uint*)pBuf;
                                while (numPixels-- > 0)
                                {
                                    *target = (uint)((*sourcePtr << 24) | 0x00FFFFFF);
                                    target++;
                                    sourcePtr++;
                                }
                            }
                        }

                        var wrap = this.factory.InterfaceManager.LoadImageFromDxgiFormat(
                            buf,
                            width * bpp,
                            width,
                            height,
                            use4 ? Format.B4G4R4A4_UNorm : Format.B8G8R8A8_UNorm);
                        this.data.AddExistingTexture(wrap);
                        texture.TexID = wrap.ImGuiHandle;
                        continue;
                    }
                    else
                    {
                        Log.Warning(
                            "[{name}]: TexID, TexPixelsRGBA32, and TexPixelsAlpha8 are all null",
                            this.data.Owner?.Name ?? "(error)");
                    }

                    if (texture.TexPixelsRGBA32 is not null)
                        ImGuiNative.igMemFree(texture.TexPixelsRGBA32);
                    if (texture.TexPixelsAlpha8 is not null)
                        ImGuiNative.igMemFree(texture.TexPixelsAlpha8);
                    texture.TexPixelsRGBA32 = null;
                    texture.TexPixelsAlpha8 = null;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }

    /// <summary>
    /// Implementations for <see cref="IFontAtlasBuildToolkitPostPromotion"/>.
    /// </summary>
    private class BuildToolkitPostPromotion : IFontAtlasBuildToolkitPostPromotion
    {
        private readonly FontAtlasBuiltData builtData;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildToolkitPostPromotion"/> class.
        /// </summary>
        /// <param name="builtData">The built data.</param>
        public BuildToolkitPostPromotion(FontAtlasBuiltData builtData) => this.builtData = builtData;

        /// <inheritdoc/>
        public ImFontPtr Font { get; set; }

        /// <inheritdoc/>
        public float Scale => this.builtData.Scale;

        /// <inheritdoc/>
        public bool IsAsyncBuildOperation => true;

        /// <inheritdoc/>
        public FontAtlasBuildStep BuildStep => FontAtlasBuildStep.PostPromotion;

        /// <inheritdoc/>
        public ImFontAtlasPtr NewImAtlas => this.builtData.Atlas;

        /// <inheritdoc/>
        public unsafe ImVectorWrapper<ImFontPtr> Fonts => new(
            &this.NewImAtlas.NativePtr->Fonts,
            x => ImGuiNative.ImFont_destroy(x->NativePtr));

        /// <inheritdoc/>
        public T DisposeWithAtlas<T>(T disposable) where T : IDisposable => this.builtData.Garbage.Add(disposable);

        /// <inheritdoc/>
        public GCHandle DisposeWithAtlas(GCHandle gcHandle) => this.builtData.Garbage.Add(gcHandle);

        /// <inheritdoc/>
        public void DisposeWithAtlas(Action action) => this.builtData.Garbage.Add(action);

        /// <inheritdoc/>
        public unsafe void CopyGlyphsAcrossFonts(
            ImFontPtr source,
            ImFontPtr target,
            bool missingOnly,
            bool rebuildLookupTable = true,
            char rangeLow = ' ',
            char rangeHigh = '\uFFFE')
        {
            var sourceFound = false;
            var targetFound = false;
            foreach (var f in this.Fonts)
            {
                sourceFound |= f.NativePtr == source.NativePtr;
                targetFound |= f.NativePtr == target.NativePtr;
            }

            if (sourceFound && targetFound)
            {
                ImGuiHelpers.CopyGlyphsAcrossFonts(
                    source,
                    target,
                    missingOnly,
                    false,
                    rangeLow,
                    rangeHigh);
                if (rebuildLookupTable)
                    this.BuildLookupTable(target);
            }
        }

        /// <inheritdoc/>
        public unsafe void BuildLookupTable(ImFontPtr font)
        {
            // Need to clear previous Fallback pointers before BuildLookupTable, or it may crash
            font.NativePtr->FallbackGlyph = null;
            font.NativePtr->FallbackHotData = null;
            font.BuildLookupTable();

            // Need to fix our custom ImGui, so that imgui_widgets.cpp:3656 stops thinking
            // Codepoint < FallbackHotData.size always means that it's not fallback char.
            // Otherwise, having a fallback character in ImGui.InputText gets strange.
            var indexedHotData = font.IndexedHotDataWrapped();
            var indexLookup = font.IndexLookupWrapped();
            ref var fallbackHotData = ref *(ImGuiHelpers.ImFontGlyphHotDataReal*)font.NativePtr->FallbackHotData;
            for (var codepoint = 0; codepoint < indexedHotData.Length; codepoint++)
            {
                if (indexLookup[codepoint] == ushort.MaxValue)
                {
                    indexedHotData[codepoint].AdvanceX = fallbackHotData.AdvanceX;
                    indexedHotData[codepoint].OccupiedWidth = fallbackHotData.OccupiedWidth;
                }
            }
        }
    }
}
