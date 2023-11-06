using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

using SharpDX.DirectWrite;

using Factory = SharpDX.DirectWrite.Factory;
using Font = SharpDX.DirectWrite.Font;
using FontCollection = SharpDX.DirectWrite.FontCollection;
using UnicodeRange = System.Text.Unicode.UnicodeRange;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal partial class InterfaceManager
{
    private sealed unsafe class SetupFontsClass : IDisposable
    {
        private readonly bool ignoreCustomDefaultFont;
        private readonly DisposeStack waste = new();
        private readonly InterfaceManager im;
        private readonly GameFontManager gfm = Service<GameFontManager>.Get();
        private readonly Dalamud dalamud = Service<Dalamud>.Get();
        private readonly DalamudConfiguration conf = Service<DalamudConfiguration>.Get();
        private readonly ImGuiRangeHandles ranges = Service<ImGuiRangeHandles>.Get();
        private readonly ImGuiIOPtr io = ImGui.GetIO();

        private readonly Factory? factory;
        private readonly FontCollection? fontCollection;

        private readonly Dictionary<
            (string Name, FontVariant Variant),
            (Font Font, string Path, int No)> resolvedFonts = new();

        private readonly string fallbackFontPath;

        private ImFontConfigPtr fontConfig;

        public SetupFontsClass(InterfaceManager im, bool ignoreCustomDefaultFont)
        {
            this.im = im;
            this.ignoreCustomDefaultFont = ignoreCustomDefaultFont;
            this.factory = this.waste.Add(Util.DefaultIfError(() => new Factory()));
            this.fontCollection = this.waste.Add(
                Util.DefaultIfError(() => this.factory?.GetSystemFontCollection(false)));
            this.fallbackFontPath = EnsureFontPath(
                Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Regular.otf"),
                Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "NotoSansCJKjp-Medium.otf"));
            this.fontConfig = new(ImGuiNative.ImFontConfig_ImFontConfig())
            {
                OversampleH = 1,
                OversampleV = 1,
                PixelSnapH = true,
                RasterizerGamma = 1f,
            };
        }

        ~SetupFontsClass()
        {
            if (this.fontConfig.NativePtr != null)
            {
                this.fontConfig.Destroy();
                this.fontConfig = default;
            }
        }

        public Dictionary<ImFontPtr, TargetFontModification> LoadedFontInfo => this.im.loadedFontInfo;

        public FontProperties Font => this.im.Font;

        private bool EnsureCharactersK =>
            this.conf.EnsureKoreanCharacters || this.conf.EffectiveLanguage == "ko";

        private bool EnsureCharactersSc =>
            this.conf.EnsureSimplifiedChineseCharacters || this.conf.EffectiveLanguage == "zh";

        private bool EnsureCharactersTc =>
            this.conf.EnsureTraditionalChineseCharacters || this.conf.EffectiveLanguage == "tw";

        private bool EnsureChinese => this.EnsureCharactersSc || this.EnsureCharactersTc;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.waste.Dispose();
            if (this.fontConfig.NativePtr != null)
            {
                this.fontConfig.Destroy();
                this.fontConfig = default;
            }
        }

        public void ClearOldData()
        {
            this.io.Fonts.Clear();
            this.io.Fonts.TexDesiredWidth = 4096;
            this.LoadedFontInfo.Values.DisposeItems();
            this.LoadedFontInfo.Clear();
        }

        public ImFontPtr AddDefaultFont() => this.FontFromFontChain(this.Font.FontChain, "Default");

        public ImFontPtr AddFontAwesomeFont() => this.FontFromFile(
            "Icon",
            EnsureFontPath(Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "FontAwesomeFreeSolid.otf")),
            0,
            DefaultFontSizePx,
            this.ranges.FontAwesome);

        public ImFontPtr AddMonoFont() => this.FontFromFile(
            "Mono",
            EnsureFontPath(Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "Inconsolata-Regular.ttf")),
            0,
            DefaultFontSizePx);

        public void AddRequestedExtraFonts()
        {
            Dictionary<float, List<SpecialGlyphRequest>> extraFontRequests = new();
            foreach (var extraFontRequest in this.im.glyphRequests)
            {
                if (!extraFontRequests.ContainsKey(extraFontRequest.Size))
                    extraFontRequests[extraFontRequest.Size] = new();
                extraFontRequests[extraFontRequest.Size].Add(extraFontRequest);
            }

            var rangeBuilder = new ImGuiRangeBuilder();
            foreach (var (fontSize, requests) in extraFontRequests)
            {
                var customRangeHandle = this.waste.Add(
                    GCHandle.Alloc(
                        rangeBuilder
                            .WithClear()
                            .WithEnsureCapacity(4 + requests.Sum(x => x.CodepointRanges.Count))
                            .With(Fallback1Codepoint, Fallback2Codepoint, 0x2026, 0x0085)
                            .WithRanges(requests.SelectMany(y => y.CodepointRanges.Select(x => (x.Item1, x.Item2))))
                            .Build(), GCHandleType.Pinned));
                var sizedFont = this.FontFromFontChain(
                    new(this.Font.FontChain.Fonts.Select(
                            x => x with { SizePx = x.SizePx * fontSize / DefaultFontSizePx })),
                    $"Extra({fontSize}px)",
                    customRangeHandle);
                foreach (var request in requests)
                    request.FontInternal = sizedFont;
            }
        }

        public bool BuildFonts()
        {
            this.gfm.BuildFonts(this.EnsureChinese ? ImGuiRangeHandles.ChineseRanges : Array.Empty<UnicodeRange>());

            var customFontFirstConfigIndex = this.io.Fonts.ConfigData.Size;

            Log.Verbose("[FONT] Invoke OnBuildFonts");
            this.im.BuildFonts?.InvokeSafely();
            Log.Verbose("[FONT] OnBuildFonts OK!");

            foreach (var config in this.io.Fonts.ConfigData.AsEnumerable().Skip(customFontFirstConfigIndex))
            {
                if (this.gfm.OwnsFont(config.DstFont))
                    continue;

                config.OversampleH = 1;
                config.OversampleV = 1;

                var name = Encoding.UTF8.GetString((byte*)config.Name.Data, config.Name.Count).TrimEnd('\0');
                if (name.IsNullOrEmpty())
                    name = $"{config.SizePixels}px";

                // ImFont information is reflected only if corresponding ImFontConfig has MergeMode not set.
                if (config.MergeMode)
                {
                    if (!this.LoadedFontInfo.ContainsKey(config.DstFont.NativePtr))
                    {
                        Log.Warning("MergeMode specified for {0} but not found in loadedFontInfo. Skipping.", name);
                        continue;
                    }
                }
                else
                {
                    if (this.LoadedFontInfo.ContainsKey(config.DstFont.NativePtr))
                    {
                        Log.Warning("MergeMode not specified for {0} but found in loadedFontInfo. Skipping.", name);
                        continue;
                    }

                    // While the font will be loaded in the scaled size after FontScale is applied, the font will be treated as having the requested size when used from plugins.
                    this.LoadedFontInfo[config.DstFont.NativePtr] = new($"PlReq({name})", config.SizePixels);
                }

                config.SizePixels *= this.io.FontGlobalScale;
            }

            // Multiply gamma late, so that it can be also applied to plugin-requested fonts.
            foreach (var config in this.io.Fonts.ConfigData.AsEnumerable())
                config.RasterizerGamma *= this.Font.Gamma;

            Log.Verbose("[FONT] ImGui.IO.Build will be called.");
            return this.io.Fonts.Build();
        }

        public void AfterBuildFonts()
        {
            this.gfm.AfterIoFontsBuild();
            this.im.ClearStacks();
            Log.Verbose("[FONT] ImGui.IO.Build OK!");

            this.gfm.AfterBuildFonts();

            foreach (var (font, mod) in this.LoadedFontInfo)
            {
                if (font.NativePtr == null)
                {
                    Log.Error("[FONT] {0}: ImFont is null", mod.Name);
                    continue;
                }

                Log.Verbose("[FONT] {0}: Unscale with scale value of {1}", mod.Name, mod.Scale);
                GameFontManager.UnscaleFont(font, mod.Scale, false);

                switch (mod.Axis)
                {
                    case TargetFontModification.AxisMode.Overwrite when mod.SourceAxis is { } sourceAxis:
                    {
                        Log.Verbose("[FONT] {name}({size}): Overwrite from {family}({targetSize}px)",
                                    mod.Name, font.FontSize, mod.GameFontFamily, sourceAxis.ImFont.FontSize);
                        GameFontManager.UnscaleFont(font, font.FontSize / sourceAxis.ImFont.FontSize, false);
                        var ascentDiff = sourceAxis.ImFont.Ascent - font.Ascent;
                        font.Ascent += ascentDiff;
                        font.Descent = ascentDiff;
                        font.FallbackChar = sourceAxis.ImFont.FallbackChar;
                        font.EllipsisChar = sourceAxis.ImFont.EllipsisChar;
                        ImGuiHelpers.CopyGlyphsAcrossFonts(
                            sourceAxis.ImFont,
                            font,
                            false,
                            false,
                            0x20,
                            0xFFFD,
                            mod.AxisOffsetX,
                            mod.AxisOffsetY + ((mod.LineHeightPx - mod.TargetSizePx) / 2),
                            mod.AxisLetterSpacing);
                        break;
                    }

                    case TargetFontModification.AxisMode.GameGlyphsOnly when mod.SourceAxis is { } sourceAxis:
                    {
                        Log.Verbose("[FONT] {name}({size}): Overwrite special glyphs from {family}({targetSize}px)",
                                    mod.Name, font.FontSize, mod.GameFontFamily, sourceAxis.ImFont.FontSize);
                        ImGuiHelpers.CopyGlyphsAcrossFonts(
                            sourceAxis.ImFont,
                            font,
                            true,
                            false,
                            0xE020,
                            0xE0DB,
                            mod.AxisOffsetX,
                            mod.AxisOffsetY + ((mod.LineHeightPx - mod.TargetSizePx) / 2),
                            mod.AxisLetterSpacing);
                        break;
                    }
                }

                Log.Verbose("[FONT] {0}: Resize from {1}px to {2}px", mod.Name, font.FontSize, mod.TargetSizePx);
                GameFontManager.UnscaleFont(font, font.FontSize / mod.TargetSizePx, false);

                // Snap font size = line height to pixels
                if (mod.Axis is not TargetFontModification.AxisMode.Suppress)
                    font.FontSize = MathF.Round(mod.LineHeightPx / this.io.FontGlobalScale) * this.io.FontGlobalScale;
            }

            // Fill missing glyphs in MonoFont from DefaultFont
            ImGuiHelpers.CopyGlyphsAcrossFonts(DefaultFont, MonoFont, true, false);

            foreach (ref var font in this.io.Fonts.Fonts.AsSpan())
            {
                if (font.Glyphs.Size == 0)
                {
                    Log.Warning("[FONT] Font has no glyph: {0}", font.GetDebugName());
                    continue;
                }

                if (font.FindGlyphNoFallback(Fallback1Codepoint).NativePtr != null)
                    font.FallbackChar = Fallback1Codepoint;

                GameFontManager.SnapFontKerningPixels(font, this.io.FontGlobalScale);
                font.BuildLookupTableNonstandard();
            }

            Log.Verbose("[FONT] Invoke OnAfterBuildFonts");
            this.im.AfterBuildFonts?.InvokeSafely();
            Log.Verbose("[FONT] OnAfterBuildFonts OK!");

            if (this.io.Fonts.Fonts[0].NativePtr != DefaultFont.NativePtr)
                Log.Warning("[FONT] First font is not DefaultFont");

            Log.Verbose("[FONT] Fonts built!");

            this.im.FontsReady = true;
        }

        private static string EnsureFontPath(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            Util.Fatal(
                "One or more files required by XIVLauncher were not found.\n" +
                "Please restart and report this error if it occurs again.\n\n" +
                "Following path(s) are attempted:\n" +
                string.Join("\n", paths),
                "Error");
            return null!;
        }

        private ImFontPtr FontFromFile(
            string name,
            string path,
            int fontNo,
            float sizePixelsPriorScale,
            GCHandle glyphRanges = default)
        {
            var nameBytes = Encoding.UTF8.GetBytes($"{name}\0");
            Marshal.Copy(nameBytes, 0, (IntPtr)this.fontConfig.Name.Data,
                         Math.Min(nameBytes.Length, this.fontConfig.Name.Count));
            this.fontConfig.SizePixels = sizePixelsPriorScale * this.io.FontGlobalScale;
            this.fontConfig.MergeMode = false;
            this.fontConfig.GlyphRanges = glyphRanges == default ? 0 : glyphRanges.AddrOfPinnedObject();
            var f = this.AddFontFromFileTtf(path, fontNo);
            this.LoadedFontInfo[f] = new(
                this.gfm,
                name,
                TargetFontModification.AxisMode.GameGlyphsOnly,
                GameFontFamily.Axis,
                DefaultFontSizePx,
                DefaultFontSizePx,
                this.io.FontGlobalScale,
                0,
                0,
                0);
            return f;
        }

        private ImFontPtr FontFromFontChain(FontChain fontChain, string name, GCHandle customRangeHandle = default)
        {
            var nameBytes = Encoding.UTF8.GetBytes($"{name}\0");
            Marshal.Copy(nameBytes, 0, (IntPtr)this.fontConfig.Name.Data,
                         Math.Min(nameBytes.Length, this.fontConfig.Name.Count));

            var axisRangeHandle = this.EnsureChinese ? this.ranges.Axis12WithoutJapanese : this.ranges.Axis12;

            var firstIdentGame = fontChain.Fonts.First().Ident.Game;
            var tfm = new TargetFontModification(
                this.gfm,
                name,
                firstIdentGame is not GameFontFamily.Undefined
                    ? TargetFontModification.AxisMode.Overwrite
                    : TargetFontModification.AxisMode.GameGlyphsOnly,
                firstIdentGame == GameFontFamily.Undefined ? GameFontFamily.Axis : firstIdentGame,
                fontChain.Fonts.First().SizePx,
                fontChain.Fonts.First().SizePx * fontChain.LineHeight,
                this.io.FontGlobalScale,
                firstIdentGame == GameFontFamily.Undefined ? 0 : fontChain.Fonts.First().OffsetX,
                firstIdentGame == GameFontFamily.Undefined ? 0 : fontChain.Fonts.First().OffsetY,
                firstIdentGame == GameFontFamily.Undefined ? 0 : fontChain.Fonts.First().LetterSpacing);
            var result = default(ImFontPtr);

            this.fontConfig.MergeMode = false;

            var glyphAvail = new GlyphAvailability();
            var baseRangeAdded = false;

            foreach (var fav in fontChain.Fonts.Where(x => x != default).Append(default))
            {
                this.fontConfig.SizePixels = fav.SizePx * this.io.FontGlobalScale;
                this.fontConfig.GlyphExtraSpacing = new Vector2(fav.LetterSpacing, 0) * this.io.FontGlobalScale;
                this.fontConfig.GlyphOffset = new Vector2(
                                                  fav.OffsetX,
                                                  fav.OffsetY + (fav.SizePx * (fontChain.LineHeight - 1f) / 2))
                                              * this.io.FontGlobalScale;

                switch (fav.Ident)
                {
                    case { System: { } system }:
                    {
                        this.fontConfig.GlyphRanges = customRangeHandle != default
                                                          ? customRangeHandle.AddrOfPinnedObject()
                                                          : this.ranges.Full.AddrOfPinnedObject();
                        if (!this.AddSystemFont(system.Name, system.Variant, ref result, ref glyphAvail))
                            continue;
                        break;
                    }

                    case { File: { } file }:
                        if (!File.Exists(file.Path))
                        {
                            Log.Error("[FONT] File not found: {path}", file.Path);
                            continue;
                        }

                        result = this.AddFontFromFileTtf(file.Path, file.Index);
                        break;

                    case { NotoSansJ: true }:
                    {
                        this.fontConfig.GlyphRanges = customRangeHandle != default
                                                          ? customRangeHandle.AddrOfPinnedObject()
                                                          : axisRangeHandle.AddrOfPinnedObject();
                        result = this.AddFontFromFileTtf(this.fallbackFontPath);
                        baseRangeAdded = true;
                        break;
                    }

                    // Fallback font mode
                    case var _ when this.ignoreCustomDefaultFont:

                    // Last in the foreach loop; failed to add any font so far; add the default font as a fallback
                    case var _ when fav.Ident == default && !this.fontConfig.MergeMode:

                    case { Game: not GameFontFamily.Undefined }:
                    {
                        this.fontConfig.GlyphRanges = this.ranges.Dummy.AddrOfPinnedObject();
                        this.fontConfig.FontNo = 0;
                        result = this.io.Fonts.AddFontDefault(this.fontConfig);
                        baseRangeAdded = true;
                        break;
                    }
                }

                this.fontConfig.MergeMode = true;
                if (this.ignoreCustomDefaultFont && baseRangeAdded)
                    break;
            }

            this.fontConfig.SizePixels = tfm.TargetSizePx * this.io.FontGlobalScale;
            this.fontConfig.GlyphExtraSpacing = default;
            this.fontConfig.GlyphOffset = default;

            if (!glyphAvail.K && this.EnsureCharactersK)
            {
                this.fontConfig.GlyphRanges = customRangeHandle == default
                                                  ? this.ranges.Korean.AddrOfPinnedObject()
                                                  : customRangeHandle.AddrOfPinnedObject();
                _ = this.AddSystemFont("Source Han Sans K", default, ref result, ref glyphAvail)
                    || this.AddSystemFont("Noto Sans KR", default, ref result, ref glyphAvail)
                    || this.AddSystemFont("Malgun Gothic", default, ref result, ref glyphAvail)
                    || this.AddSystemFont("Gulim", default, ref result, ref glyphAvail);
            }

            if (!glyphAvail.Sc && this.EnsureCharactersSc)
            {
                this.fontConfig.GlyphRanges = customRangeHandle == default
                                                  ? this.ranges.Chinese.AddrOfPinnedObject()
                                                  : customRangeHandle.AddrOfPinnedObject();
                _ = this.AddSystemFont("Microsoft YaHei UI", default, ref result, ref glyphAvail)
                    || this.AddSystemFont("Microsoft YaHei", default, ref result, ref glyphAvail);
            }

            if (!glyphAvail.Tc && this.EnsureCharactersTc)
            {
                this.fontConfig.GlyphRanges = customRangeHandle == default
                                                  ? this.ranges.Chinese.AddrOfPinnedObject()
                                                  : customRangeHandle.AddrOfPinnedObject();
                _ = this.AddSystemFont("Microsoft JhengHei UI", default, ref result, ref glyphAvail)
                    || this.AddSystemFont("Microsoft JhengHei", default, ref result, ref glyphAvail);
            }

            // ensure display of game characters if none of fonts from the chain contain them
            if (!baseRangeAdded && customRangeHandle == default)
            {
                // Current implementation makes it difficult to use AXIS font as the last item in the chain.
                // * We don't keep track of which glyphs are already added.
                // * Loading glyphs from AXIS font will overwrite previously loaded glyphs.
                // * Other ImGui-loaded fonts will skip previously loaded glyphs.
                this.fontConfig.GlyphRanges = this.ranges.Axis12.AddrOfPinnedObject();
                result = this.AddFontFromFileTtf(this.fallbackFontPath);
            }

            this.fontConfig.MergeMode = false;

            Debug.Assert(result.NativePtr != null, "result.NativePtr != null");
            this.LoadedFontInfo[result] = tfm;
            return result;
        }

        private bool AddSystemFont(
            string name, FontVariant variant, ref ImFontPtr font, ref GlyphAvailability glyphAvail)
        {
            if (this.fontCollection is null)
                return false;
            if (!this.resolvedFonts.TryGetValue((name, variant), out var resolved))
            {
                resolved = default;
                try
                {
                    if (!this.fontCollection.FindFamilyName(name, out var fontFamilyIndex))
                        throw new FileNotFoundException($"Corresponding font family not found");

                    using var fontFamily = this.fontCollection.GetFontFamily(fontFamilyIndex);
                    resolved.Font = this.waste.Add(
                        fontFamily.GetFirstMatchingFont(variant.Weight, variant.Stretch, variant.Style));

                    using var fontFace = new FontFace(resolved.Font);
                    resolved.No = fontFace.Index;

                    using var files = fontFace.GetFiles().WrapDisposableElements();
                    var localFontFileLoaderGuid = typeof(LocalFontFileLoader).GUID;

                    var file = files.Single();
                    using var loader = file.Loader;
                    loader.QueryInterface(ref localFontFileLoaderGuid, out var loaderIntPtr).CheckError();
                    using var fontFileLoader = new LocalFontFileLoader(loaderIntPtr);
                    resolved.Path = fontFileLoader.GetFilePath(file.GetReferenceKey());
                    this.resolvedFonts.Add((name, variant), resolved);
                }
                catch (Exception e)
                {
                    Log.Error(e, "[FONT] Failed to load font: {font} ({variant}).", name, variant);
                    resolved.No = -1;
                }
            }

            if (resolved.No == -1)
                return false;

            font = this.AddFontFromFileTtf(resolved.Path, resolved.No);
            for (var p = (ushort*)this.fontConfig.GlyphRanges; p != null && *p != 0; p += 2)
            {
                if (p[0] <= '기' || p[1] >= '기')
                    glyphAvail.K |= resolved.Font.HasCharacter('기');
                if (p[0] <= '气' || p[1] >= '气')
                    glyphAvail.Sc |= resolved.Font.HasCharacter('气');
                if (p[0] <= '氣' || p[1] >= '氣')
                    glyphAvail.Tc |= resolved.Font.HasCharacter('氣');
            }

            return true;
        }

        private ImFontPtr AddFontFromFileTtf(string path, int fontNo = 0)
        {
            this.fontConfig.FontNo = fontNo;
            return this.io.Fonts.AddFontFromFileTTF(path, this.fontConfig.SizePixels, this.fontConfig);
        }

        private record GlyphAvailability
        {
            public bool K { get; set; }

            public bool Sc { get; set; }

            public bool Tc { get; set; }
        }
    }
}
