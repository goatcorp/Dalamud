using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using SharpDX.DXGI;

using TerraFX.Interop.DirectX;

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
    private class BuildToolkit : IFontAtlasBuildToolkit.IApi9Compat, IFontAtlasBuildToolkitPreBuild, IFontAtlasBuildToolkitPostBuild, IDisposable
    {
        private static readonly ushort FontAwesomeIconMin =
            (ushort)Enum.GetValues<FontAwesomeIcon>().Where(x => x > 0).Min();

        private static readonly ushort FontAwesomeIconMax =
            (ushort)Enum.GetValues<FontAwesomeIcon>().Where(x => x > 0).Max();

        private readonly DisposeSafety.ScopedFinalizer disposeAfterBuild = new();
        private readonly GamePrebakedFontHandle.HandleSubstance gameFontHandleSubstance;
        private readonly FontAtlasFactory factory;
        private readonly FontAtlasBuiltData data;
        private readonly List<Action> registeredPostBuildActions = new();

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
        /// Gets the font scale modes.
        /// </summary>
        private Dictionary<ImFontPtr, FontScaleMode> FontScaleModes { get; } = new();

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
        [Api10ToDo(Api10ToDoAttribute.DeleteCompatBehavior)]
        public void FromUiBuilderObsoleteEventHandlers(Action action)
        {
            var previousSubstances = new IFontHandleSubstance[this.data.Substances.Count];
            for (var i = 0; i < previousSubstances.Length; i++)
            {
                previousSubstances[i] = this.data.Substances[i].Manager.Substance;
                this.data.Substances[i].Manager.Substance = this.data.Substances[i];
                this.data.Substances[i].CreateFontOnAccess = true;
                this.data.Substances[i].PreBuildToolkitForApi9Compat = this;
            }

            try
            {
                action();
            }
            finally
            {
                for (var i = 0; i < previousSubstances.Length; i++)
                {
                    this.data.Substances[i].Manager.Substance = previousSubstances[i];
                    this.data.Substances[i].CreateFontOnAccess = false;
                    this.data.Substances[i].PreBuildToolkitForApi9Compat = null;
                }
            }
        }

        /// <inheritdoc/>
        public ImFontPtr GetFont(IFontHandle fontHandle)
        {
            foreach (var s in this.data.Substances)
            {
                var f = s.GetFontPtr(fontHandle);
                if (!f.IsNull())
                    return f;
            }

            return default;
        }

        /// <inheritdoc/>
        public ImFontPtr SetFontScaleMode(ImFontPtr fontPtr, FontScaleMode scaleMode)
        {
            this.FontScaleModes[fontPtr] = scaleMode;
            return fontPtr;
        }

        /// <inheritdoc cref="IFontAtlasBuildToolkitPreBuild.GetFontScaleMode"/>
        public FontScaleMode GetFontScaleMode(ImFontPtr fontPtr) =>
            this.FontScaleModes.GetValueOrDefault(fontPtr, FontScaleMode.Default);

        /// <inheritdoc/>
        public int StoreTexture(IDalamudTextureWrap textureWrap, bool disposeOnError) =>
            this.data.AddNewTexture(textureWrap, disposeOnError);

        /// <inheritdoc/>
        public void RegisterPostBuild(Action action) => this.registeredPostBuildActions.Add(action);

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

            var font = default(ImFontPtr);
            try
            {
                fontConfig.ThrowOnInvalidValues();

                var raw = fontConfig.Raw with
                {
                    FontData = dataPointer,
                    FontDataOwnedByAtlas = 1,
                    FontDataSize = dataSize,
                };

                if (fontConfig.GlyphRanges is not { Length: > 0 } ranges)
                    ranges = new ushort[] { 1, 0xFFFE, 0 };

                raw.GlyphRanges = (ushort*)this.DisposeAfterBuild(
                    GCHandle.Alloc(ranges, GCHandleType.Pinned)).AddrOfPinnedObject();

                TrueTypeUtils.CheckImGuiCompatibleOrThrow(raw);

                font = this.NewImAtlas.AddFont(&raw);

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
                if (!font.IsNull())
                {
                    // Note that for both RemoveAt calls, corresponding destructors will be called.

                    var configIndex = this.data.ConfigData.FindIndex(x => x.DstFont == font.NativePtr);
                    if (configIndex >= 0)
                        this.data.ConfigData.RemoveAt(configIndex);

                    var index = this.Fonts.IndexOf(font);
                    if (index >= 0)
                        this.Fonts.RemoveAt(index);
                }

                // ImFontConfig has no destructor, and does not free the data.
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
            ImFontPtr font = default;
            glyphRanges ??= this.factory.DefaultGlyphRanges;

            var dfid = this.factory.DefaultFontSpec;
            if (sizePx < 0f)
                sizePx *= -dfid.SizePx;

            if (dfid is SingleFontSpec sfs)
            {
                if (sfs.FontId is DalamudDefaultFontAndFamilyId)
                {
                    // invalid; calling sfs.AddToBuildToolkit calls this function, causing infinite recursion
                }
                else
                {
                    sfs = sfs with { SizePx = sizePx };
                    font = sfs.AddToBuildToolkit(this);
                    if (sfs.FontId is not GameFontAndFamilyId { GameFontFamily: GameFontFamily.Axis })
                        this.AddGameSymbol(new() { SizePx = sizePx, MergeFont = font });
                }
            }

            if (font.IsNull())
            {
                // fall back to AXIS fonts
                font = this.AddGameGlyphs(new(GameFontFamily.Axis, sizePx), glyphRanges, default);
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
                    return this.AddGameGlyphs(
                        new(GameFontFamily.Axis, fontConfig.SizePx),
                        fontConfig.GlyphRanges,
                        fontConfig.MergeFont);

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
        public void AttachWindowsDefaultFont(
            CultureInfo cultureInfo,
            in SafeFontConfig fontConfig,
            int weight = (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
            int stretch = (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
            int style = (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL)
        {
            var targetFont = fontConfig.MergeFont;
            if (targetFont.IsNull())
                targetFont = this.Font;
            if (targetFont.IsNull())
                return;

            // https://learn.microsoft.com/en-us/windows/apps/design/globalizing/loc-international-fonts
            var splitTag = cultureInfo.IetfLanguageTag.Split("-");
            foreach (var test in new[]
                     {
                         cultureInfo.IetfLanguageTag,
                         $"{splitTag[0]}-{splitTag[^1]}",
                     })
            {
                var familyName = test switch
                {
                    "af-ZA" => "Segoe UI",
                    "am-ET" => "Ebrima",
                    "ar-SA" => "Segoe UI",
                    "as-IN" => "Nirmala UI",
                    "az-Latn-AZ" => "Segoe UI",
                    "be-BY" => "Segoe UI",
                    "bg-BG" => "Segoe UI",
                    "bn-BD" => "Nirmala UI",
                    "bn-IN" => "Nirmala UI",
                    "bs-Latn-BA" => "Segoe UI",
                    "ca-ES" => "Segoe UI",
                    "ca-ES-valencia" => "Segoe UI",
                    "chr-CHER-US" => "Gadugi",
                    "cs-CZ" => "Segoe UI",
                    "cy-GB" => "Segoe UI",
                    "da-DK" => "Segoe UI",
                    "de-DE" => "Segoe UI",
                    "el-GR" => "Segoe UI",
                    "en-GB" => "Segoe UI",
                    "es-ES" => "Segoe UI",
                    "et-EE" => "Segoe UI",
                    "eu-ES" => "Segoe UI",
                    "fa-IR" => "Segoe UI",
                    "fi-FI" => "Segoe UI",
                    "fil-PH" => "Segoe UI",
                    "fr-FR" => "Segoe UI",
                    "ga-IE" => "Segoe UI",
                    "gd-GB" => "Segoe UI",
                    "gl-ES" => "Segoe UI",
                    "gu-IN" => "Nirmala UI",
                    "ha-Latn-NG" => "Segoe UI",
                    "he-IL" => "Segoe UI",
                    "hi-IN" => "Nirmala UI",
                    "hr-HR" => "Segoe UI",
                    "hu-HU" => "Segoe UI",
                    "hy-AM" => "Segoe UI",
                    "id-ID" => "Segoe UI",
                    "ig-NG" => "Segoe UI",
                    "is-IS" => "Segoe UI",
                    "it-IT" => "Segoe UI",
                    "ja-JP" => "Yu Gothic UI",
                    "ka-GE" => "Segoe UI",
                    "kk-KZ" => "Segoe UI",
                    "km-KH" => "Leelawadee UI",
                    "kn-IN" => "Nirmala UI",
                    "ko-KR" => "Malgun Gothic",
                    "kok-IN" => "Nirmala UI",
                    "ku-ARAB-IQ" => "Segoe UI",
                    "ky-KG" => "Segoe UI",
                    "lb-LU" => "Segoe UI",
                    "lt-LT" => "Segoe UI",
                    "lv-LV" => "Segoe UI",
                    "mi-NZ" => "Segoe UI",
                    "mk-MK" => "Segoe UI",
                    "ml-IN" => "Nirmala UI",
                    "mn-MN" => "Segoe UI",
                    "mr-IN" => "Nirmala UI",
                    "ms-MY" => "Segoe UI",
                    "mt-MT" => "Segoe UI",
                    "nb-NO" => "Segoe UI",
                    "ne-NP" => "Nirmala UI",
                    "nl-NL" => "Segoe UI",
                    "nn-NO" => "Segoe UI",
                    "nso-ZA" => "Segoe UI",
                    "or-IN" => "Nirmala UI",
                    "pa-Arab-PK" => "Segoe UI",
                    "pa-IN" => "Nirmala UI",
                    "pl-PL" => "Segoe UI",
                    "prs-AF" => "Segoe UI",
                    "pt-BR" => "Segoe UI",
                    "pt-PT" => "Segoe UI",
                    "qut-GT" => "Segoe UI",
                    "quz-PE" => "Segoe UI",
                    "ro-RO" => "Segoe UI",
                    "ru-RU" => "Segoe UI",
                    "rw-RW" => "Segoe UI",
                    "sd-Arab-PK" => "Segoe UI",
                    "si-LK" => "Nirmala UI",
                    "sk-SK" => "Segoe UI",
                    "sl-SI" => "Segoe UI",
                    "sq-AL" => "Segoe UI",
                    "sr-Cyrl-BA" => "Segoe UI",
                    "sr-Cyrl-CS" => "Segoe UI",
                    "sr-Latn-CS" => "Segoe UI",
                    "sv-SE" => "Segoe UI",
                    "sw-KE" => "Segoe UI",
                    "ta-IN" => "Nirmala UI",
                    "te-IN" => "Nirmala UI",
                    "tg-Cyrl-TJ" => "Segoe UI",
                    "th-TH" => "Leelawadee UI",
                    "ti-ET" => "Ebrima",
                    "tk-TM" => "Segoe UI",
                    "tn-ZA" => "Segoe UI",
                    "tr-TR" => "Segoe UI",
                    "tt-RU" => "Segoe UI",
                    "ug-CN" => "Segoe UI",
                    "uk-UA" => "Segoe UI",
                    "ur-PK" => "Segoe UI",
                    "uz-Latn-UZ" => "Segoe UI",
                    "vi-VN" => "Segoe UI",
                    "wo-SN" => "Segoe UI",
                    "xh-ZA" => "Segoe UI",
                    "yo-NG" => "Segoe UI",
                    "zh-CN" => "Microsoft YaHei UI",
                    "zh-HK" => "Microsoft JhengHei UI",
                    "zh-TW" => "Microsoft JhengHei UI",
                    "zh-Hans" => "Microsoft YaHei UI",
                    "zh-Hant" => "Microsoft YaHei UI",
                    "zu-ZA" => "Segoe UI",
                    _ => null,
                };
                if (familyName is null)
                    continue;
                var family = IFontFamilyId
                             .ListSystemFonts(false)
                             .FirstOrDefault(
                                 x => x.EnglishName.Equals(familyName, StringComparison.InvariantCultureIgnoreCase));
                if (family?.Fonts[family.FindBestMatch(weight, stretch, style)] is not { } font)
                    return;
                font.AddToBuildToolkit(this, fontConfig with { MergeFont = targetFont });
                return;
            }
        }

        /// <inheritdoc/>
        public void AttachExtraGlyphsForDalamudLanguage(in SafeFontConfig fontConfig)
        {
            var targetFont = fontConfig.MergeFont;
            if (targetFont.IsNull())
                targetFont = this.Font;
            if (targetFont.IsNull())
                return;

            var dalamudConfiguration = Service<DalamudConfiguration>.Get();
            if (dalamudConfiguration.EffectiveLanguage == "ko"
                || Service<DalamudIme>.GetNullable()?.EncounteredHangul is true)
            {
                this.AddDalamudAssetFont(
                    DalamudAsset.NotoSansKrRegular,
                    fontConfig with
                    {
                        MergeFont = targetFont,
                        GlyphRanges = default(FluentGlyphRangeBuilder).WithLanguage("ko-kr").BuildExact(),
                    });
            }

            if (Service<DalamudConfiguration>.Get().EffectiveLanguage == "tw")
            {
                this.AttachWindowsDefaultFont(CultureInfo.GetCultureInfo("zh-hant"), fontConfig with
                {
                    GlyphRanges = default(FluentGlyphRangeBuilder).WithLanguage("zh-hant").BuildExact(),
                });
            }
            else if (Service<DalamudConfiguration>.Get().EffectiveLanguage == "zh"
                     || Service<DalamudIme>.GetNullable()?.EncounteredHan is true)
            {
                this.AttachWindowsDefaultFont(CultureInfo.GetCultureInfo("zh-hans"), fontConfig with
                {
                    GlyphRanges = default(FluentGlyphRangeBuilder).WithLanguage("zh-hans").BuildExact(),
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
                if (this.GetFontScaleMode(config.DstFont) != FontScaleMode.Default)
                    continue;

                config.SizePixels *= this.Scale;

                config.GlyphMaxAdvanceX *= this.Scale;
                if (float.IsInfinity(config.GlyphMaxAdvanceX) || float.IsNaN(config.GlyphMaxAdvanceX))
                    config.GlyphMaxAdvanceX = config.GlyphMaxAdvanceX > 0 ? float.MaxValue : -float.MaxValue;

                config.GlyphMinAdvanceX *= this.Scale;
                if (float.IsInfinity(config.GlyphMinAdvanceX) || float.IsNaN(config.GlyphMinAdvanceX))
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
                if (this.GetFontScaleMode(font) != FontScaleMode.SkipHandling)
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

        public void PostBuildCallbacks()
        {
            foreach (var ac in this.registeredPostBuildActions)
                ac.InvokeSafely();
            this.registeredPostBuildActions.Clear();
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

        /// <inheritdoc/>
        public void FitRatio(ImFontPtr font, bool rebuildLookupTable = true)
        {
            var nsize = font.FontSize;
            var glyphs = font.GlyphsWrapped();
            foreach (ref var glyph in glyphs.DataSpan)
            {
                var ratio = 1f;
                if (glyph.X1 - glyph.X0 > nsize)
                    ratio = Math.Max(ratio, (glyph.X1 - glyph.X0) / nsize);
                if (glyph.Y1 - glyph.Y0 > nsize)
                    ratio = Math.Max(ratio, (glyph.Y1 - glyph.Y0) / nsize);
                var w = MathF.Round((glyph.X1 - glyph.X0) / ratio, MidpointRounding.ToZero);
                var h = MathF.Round((glyph.Y1 - glyph.Y0) / ratio, MidpointRounding.AwayFromZero);
                glyph.X0 = MathF.Round((nsize - w) / 2f, MidpointRounding.ToZero);
                glyph.Y0 = MathF.Round((nsize - h) / 2f, MidpointRounding.AwayFromZero);
                glyph.X1 = glyph.X0 + w;
                glyph.Y1 = glyph.Y0 + h;
                glyph.AdvanceX = nsize;
            }

            if (rebuildLookupTable)
                this.BuildLookupTable(font);
        }
    }
}
