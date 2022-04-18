using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Interface.Internal;
using ImGuiNET;
using Lumina.Data.Files;
using Serilog;

namespace Dalamud.Interface.GameFonts
{
    /// <summary>
    /// Loads game font for use in ImGui.
    /// </summary>
    internal class GameFontManager : IDisposable
    {
        private static readonly string[] FontNames =
        {
            null,
            "AXIS_96", "AXIS_12", "AXIS_14", "AXIS_18", "AXIS_36",
            "Jupiter_16", "Jupiter_20", "Jupiter_23", "Jupiter_45", "Jupiter_46", "Jupiter_90",
            "Meidinger_16", "Meidinger_20", "Meidinger_40",
            "MiedingerMid_10", "MiedingerMid_12", "MiedingerMid_14", "MiedingerMid_18", "MiedingerMid_36",
            "TrumpGothic_184", "TrumpGothic_23", "TrumpGothic_34", "TrumpGothic_68",
        };

        private readonly object syncRoot = new();

        private readonly InterfaceManager interfaceManager;

        private readonly FdtReader?[] fdts;
        private readonly List<byte[]> texturePixels;
        private readonly Dictionary<GameFontStyle, ImFontPtr> fonts = new();
        private readonly Dictionary<GameFontStyle, int> fontUseCounter = new();
        private readonly Dictionary<GameFontStyle, Dictionary<char, Tuple<int, FdtReader.FontTableEntry>>> glyphRectIds = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GameFontManager"/> class.
        /// </summary>
        public GameFontManager()
        {
            var dataManager = Service<DataManager>.Get();

            this.fdts = FontNames.Select(fontName =>
            {
                var file = fontName == null ? null : dataManager.GetFile($"common/font/{fontName}.fdt");
                return file == null ? null : new FdtReader(file!.Data);
            }).ToArray();
            this.texturePixels = Enumerable.Range(1, 1 + this.fdts.Where(x => x != null).Select(x => x.Glyphs.Select(x => x.TextureFileIndex).Max()).Max()).Select(x => dataManager.GameData.GetFile<TexFile>($"common/font/font{x}.tex").ImageData).ToList();

            this.interfaceManager = Service<InterfaceManager>.Get();
        }

        /// <summary>
        /// Describe font into a string.
        /// </summary>
        /// <param name="font">Font to describe.</param>
        /// <returns>A string in a form of "FontName (NNNpt)".</returns>
        public static string DescribeFont(GameFontFamilyAndSize font)
        {
            return font switch
            {
                GameFontFamilyAndSize.Undefined => "-",
                GameFontFamilyAndSize.Axis96 => "AXIS (9.6pt)",
                GameFontFamilyAndSize.Axis12 => "AXIS (12pt)",
                GameFontFamilyAndSize.Axis14 => "AXIS (14pt)",
                GameFontFamilyAndSize.Axis18 => "AXIS (18pt)",
                GameFontFamilyAndSize.Axis36 => "AXIS (36pt)",
                GameFontFamilyAndSize.Jupiter16 => "Jupiter (16pt)",
                GameFontFamilyAndSize.Jupiter20 => "Jupiter (20pt)",
                GameFontFamilyAndSize.Jupiter23 => "Jupiter (23pt)",
                GameFontFamilyAndSize.Jupiter45 => "Jupiter Numeric (45pt)",
                GameFontFamilyAndSize.Jupiter46 => "Jupiter (46pt)",
                GameFontFamilyAndSize.Jupiter90 => "Jupiter Numeric (90pt)",
                GameFontFamilyAndSize.Meidinger16 => "Meidinger Numeric (16pt)",
                GameFontFamilyAndSize.Meidinger20 => "Meidinger Numeric (20pt)",
                GameFontFamilyAndSize.Meidinger40 => "Meidinger Numeric (40pt)",
                GameFontFamilyAndSize.MiedingerMid10 => "MiedingerMid (10pt)",
                GameFontFamilyAndSize.MiedingerMid12 => "MiedingerMid (12pt)",
                GameFontFamilyAndSize.MiedingerMid14 => "MiedingerMid (14pt)",
                GameFontFamilyAndSize.MiedingerMid18 => "MiedingerMid (18pt)",
                GameFontFamilyAndSize.MiedingerMid36 => "MiedingerMid (36pt)",
                GameFontFamilyAndSize.TrumpGothic184 => "Trump Gothic (18.4pt)",
                GameFontFamilyAndSize.TrumpGothic23 => "Trump Gothic (23pt)",
                GameFontFamilyAndSize.TrumpGothic34 => "Trump Gothic (34pt)",
                GameFontFamilyAndSize.TrumpGothic68 => "Trump Gothic (68pt)",
                _ => throw new ArgumentOutOfRangeException(nameof(font), font, "Invalid argument"),
            };
        }

        /// <summary>
        /// Determines whether a font should be able to display most of stuff.
        /// </summary>
        /// <param name="font">Font to check.</param>
        /// <returns>True if it can.</returns>
        public static bool IsGenericPurposeFont(GameFontFamilyAndSize font)
        {
            return font switch
            {
                GameFontFamilyAndSize.Axis96 => true,
                GameFontFamilyAndSize.Axis12 => true,
                GameFontFamilyAndSize.Axis14 => true,
                GameFontFamilyAndSize.Axis18 => true,
                GameFontFamilyAndSize.Axis36 => true,
                _ => false,
            };
        }

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        /// <param name="rangeLow">Low codepoint range to copy.</param>
        /// <param name="rangeHigh">High codepoing range to copy.</param>
        public static void CopyGlyphsAcrossFonts(ImFontPtr? source, ImFontPtr? target, bool missingOnly, bool rebuildLookupTable, int rangeLow = 32, int rangeHigh = 0xFFFE)
        {
            if (!source.HasValue || !target.HasValue)
                return;

            var scale = target.Value!.FontSize / source.Value!.FontSize;
            unsafe
            {
                var glyphs = (ImFontGlyphReal*)source.Value!.Glyphs.Data;
                for (int j = 0, j_ = source.Value!.Glyphs.Size; j < j_; j++)
                {
                    var glyph = &glyphs[j];
                    if (glyph->Codepoint < rangeLow || glyph->Codepoint > rangeHigh)
                        continue;

                    var prevGlyphPtr = (ImFontGlyphReal*)target.Value!.FindGlyphNoFallback((ushort)glyph->Codepoint).NativePtr;
                    if ((IntPtr)prevGlyphPtr == IntPtr.Zero)
                    {
                        target.Value!.AddGlyph(
                            target.Value!.ConfigData,
                            (ushort)glyph->Codepoint,
                            glyph->X0 * scale,
                            ((glyph->Y0 - source.Value!.Ascent) * scale) + target.Value!.Ascent,
                            glyph->X1 * scale,
                            ((glyph->Y1 - source.Value!.Ascent) * scale) + target.Value!.Ascent,
                            glyph->U0,
                            glyph->V0,
                            glyph->U1,
                            glyph->V1,
                            glyph->AdvanceX * scale);
                    }
                    else if (!missingOnly)
                    {
                        prevGlyphPtr->X0 = glyph->X0 * scale;
                        prevGlyphPtr->Y0 = ((glyph->Y0 - source.Value!.Ascent) * scale) + target.Value!.Ascent;
                        prevGlyphPtr->X1 = glyph->X1 * scale;
                        prevGlyphPtr->Y1 = ((glyph->Y1 - source.Value!.Ascent) * scale) + target.Value!.Ascent;
                        prevGlyphPtr->U0 = glyph->U0;
                        prevGlyphPtr->V0 = glyph->V0;
                        prevGlyphPtr->U1 = glyph->U1;
                        prevGlyphPtr->V1 = glyph->V1;
                        prevGlyphPtr->AdvanceX = glyph->AdvanceX * scale;
                    }
                }
            }

            if (rebuildLookupTable)
                target.Value!.BuildLookupTable();
        }

        /// <summary>
        /// Unscales fonts after they have been rendered onto atlas.
        /// </summary>
        /// <param name="fontPtr">Font to unscale.</param>
        /// <param name="fontScale">Scale factor.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public static void UnscaleFont(ImFontPtr fontPtr, float fontScale, bool rebuildLookupTable = true)
        {
            if (fontScale == 1)
                return;

            unsafe
            {
                var font = fontPtr.NativePtr;
                for (int i = 0, i_ = font->IndexAdvanceX.Size; i < i_; ++i)
                    ((float*)font->IndexAdvanceX.Data)[i] /= fontScale;
                font->FallbackAdvanceX /= fontScale;
                font->FontSize /= fontScale;
                font->Ascent /= fontScale;
                font->Descent /= fontScale;
                if (font->ConfigData != null)
                    font->ConfigData->SizePixels /= fontScale;
                var glyphs = (ImFontGlyphReal*)font->Glyphs.Data;
                for (int i = 0, i_ = font->Glyphs.Size; i < i_; i++)
                {
                    var glyph = &glyphs[i];
                    glyph->X0 /= fontScale;
                    glyph->X1 /= fontScale;
                    glyph->Y0 /= fontScale;
                    glyph->Y1 /= fontScale;
                    glyph->AdvanceX /= fontScale;
                }
            }

            if (rebuildLookupTable)
                fontPtr.BuildLookupTable();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a new GameFontHandle, and increases internal font reference counter, and if it's first time use, then the font will be loaded on next font building process.
        /// </summary>
        /// <param name="style">Font to use.</param>
        /// <returns>Handle to game font that may or may not be ready yet.</returns>
        public GameFontHandle NewFontRef(GameFontStyle style)
        {
            var needRebuild = false;

            lock (this.syncRoot)
            {
                var prevValue = this.fontUseCounter.GetValueOrDefault(style, 0);
                var newValue = this.fontUseCounter[style] = prevValue + 1;
                needRebuild = (prevValue == 0) != (newValue == 0) && !this.fonts.ContainsKey(style);
            }

            if (needRebuild)
            {
                Log.Information("[GameFontManager] Calling RebuildFonts because {0} has been requested.", style.ToString());
                this.interfaceManager.RebuildFonts();
            }

            return new(this, style);
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <param name="style">Font to get.</param>
        /// <returns>Corresponding font or null.</returns>
        public ImFontPtr? GetFont(GameFontStyle style) => this.fonts.GetValueOrDefault(style, null);

        /// <summary>
        /// Gets the corresponding FdtReader.
        /// </summary>
        /// <param name="family">Font to get.</param>
        /// <returns>Corresponding FdtReader or null.</returns>
        public FdtReader? GetFdtReader(GameFontFamilyAndSize family) => this.fdts[(int)family];

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(ImFontPtr? source, GameFontStyle target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(source, this.fonts[target], missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(GameFontStyle source, ImFontPtr? target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(this.fonts[source], target, missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(GameFontStyle source, GameFontStyle target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(this.fonts[source], this.fonts[target], missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Build fonts before plugins do something more. To be called from InterfaceManager.
        /// </summary>
        /// <param name="forceMinSize">Whether to load fonts in minimum sizes.</param>
        public void BuildFonts(bool forceMinSize)
        {
            unsafe
            {
                ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                fontConfig.OversampleH = 1;
                fontConfig.OversampleV = 1;
                fontConfig.PixelSnapH = false;

                var io = ImGui.GetIO();

                this.glyphRectIds.Clear();
                this.fonts.Clear();

                foreach (var style in this.fontUseCounter.Keys)
                {
                    var rectIds = this.glyphRectIds[style] = new();

                    var fdt = this.fdts[(int)(forceMinSize ? style.FamilyWithMinimumSize : style.FamilyAndSize)];
                    if (fdt == null)
                        continue;

                    var font = io.Fonts.AddFontDefault(fontConfig);

                    this.fonts[style] = font;
                    foreach (var glyph in fdt.Glyphs)
                    {
                        var c = glyph.Char;
                        if (c < 32 || c >= 0xFFFF)
                            continue;

                        var widthAdjustment = style.CalculateBaseWidthAdjustment(fdt, glyph);
                        rectIds[c] = Tuple.Create(
                            io.Fonts.AddCustomRectFontGlyph(
                                font,
                                c,
                                glyph.BoundingWidth + widthAdjustment + 1,
                                glyph.BoundingHeight + 1,
                                glyph.AdvanceWidth,
                                new Vector2(0, glyph.CurrentOffsetY)),
                            glyph);
                    }
                }

                fontConfig.Destroy();
            }
        }

        /// <summary>
        /// Post-build fonts before plugins do something more. To be called from InterfaceManager.
        /// </summary>
        /// <param name="forceMinSize">Whether to load fonts in minimum sizes.</param>
        public unsafe void AfterBuildFonts(bool forceMinSize)
        {
            var ioFonts = ImGui.GetIO().Fonts;
            ioFonts.GetTexDataAsRGBA32(out byte* pixels8, out var width, out var height);
            var pixels32 = (uint*)pixels8;
            var fontGamma = this.interfaceManager.FontGamma;

            foreach (var (style, font) in this.fonts)
            {
                var fdt = this.fdts[(int)(forceMinSize ? style.FamilyWithMinimumSize : style.FamilyAndSize)];
                var scale = style.SizePt / fdt.FontHeader.Size;
                var fontPtr = font.NativePtr;
                fontPtr->FontSize = fdt.FontHeader.Size * 4 / 3;
                if (fontPtr->ConfigData != null)
                    fontPtr->ConfigData->SizePixels = fontPtr->FontSize;
                fontPtr->Ascent = fdt.FontHeader.Ascent;
                fontPtr->Descent = fdt.FontHeader.Descent;
                fontPtr->EllipsisChar = '…';
                foreach (var fallbackCharCandidate in "〓?!")
                {
                    var glyph = font.FindGlyphNoFallback(fallbackCharCandidate);
                    if ((IntPtr)glyph.NativePtr != IntPtr.Zero)
                    {
                        font.SetFallbackChar(fallbackCharCandidate);
                        break;
                    }
                }

                // I have no idea what's causing NPE, so just to be safe
                try
                {
                    if (font.NativePtr != null && font.NativePtr->ConfigData != null)
                    {
                        var nameBytes = Encoding.UTF8.GetBytes(style.ToString() + "\0");
                        Marshal.Copy(nameBytes, 0, (IntPtr)font.ConfigData.Name.Data, Math.Min(nameBytes.Length, font.ConfigData.Name.Count));
                    }
                }
                catch (NullReferenceException)
                {
                    // do nothing
                }

                foreach (var (c, (rectId, glyph)) in this.glyphRectIds[style])
                {
                    var rc = ioFonts.GetCustomRectByIndex(rectId);
                    var sourceBuffer = this.texturePixels[glyph.TextureFileIndex];
                    var sourceBufferDelta = glyph.TextureChannelByteIndex;
                    var widthAdjustment = style.CalculateBaseWidthAdjustment(fdt, glyph);
                    if (widthAdjustment == 0)
                    {
                        for (var y = 0; y < glyph.BoundingHeight; y++)
                        {
                            for (var x = 0; x < glyph.BoundingWidth; x++)
                            {
                                var a = sourceBuffer[sourceBufferDelta + (4 * (((glyph.TextureOffsetY + y) * fdt.FontHeader.TextureWidth) + glyph.TextureOffsetX + x))];
                                pixels32[((rc.Y + y) * width) + rc.X + x] = (uint)(a << 24) | 0xFFFFFFu;
                            }
                        }
                    }
                    else
                    {
                        for (var y = 0; y < glyph.BoundingHeight; y++)
                        {
                            for (var x = 0; x < glyph.BoundingWidth + widthAdjustment; x++)
                                pixels32[((rc.Y + y) * width) + rc.X + x] = 0xFFFFFFu;
                        }

                        for (int xbold = 0, xbold_ = Math.Max(1, (int)Math.Ceiling(style.Weight + 1)); xbold < xbold_; xbold++)
                        {
                            var boldStrength = Math.Min(1f, style.Weight + 1 - xbold);
                            for (var y = 0; y < glyph.BoundingHeight; y++)
                            {
                                float xDelta = xbold;
                                if (style.BaseSkewStrength > 0)
                                    xDelta += style.BaseSkewStrength * (fdt.FontHeader.LineHeight - glyph.CurrentOffsetY - y) / fdt.FontHeader.LineHeight;
                                else if (style.BaseSkewStrength < 0)
                                    xDelta -= style.BaseSkewStrength * (glyph.CurrentOffsetY + y) / fdt.FontHeader.LineHeight;
                                var xDeltaInt = (int)Math.Floor(xDelta);
                                var xness = xDelta - xDeltaInt;
                                for (var x = 0; x < glyph.BoundingWidth; x++)
                                {
                                    var sourcePixelIndex = ((glyph.TextureOffsetY + y) * fdt.FontHeader.TextureWidth) + glyph.TextureOffsetX + x;
                                    var a1 = sourceBuffer[sourceBufferDelta + (4 * sourcePixelIndex)];
                                    var a2 = x == glyph.BoundingWidth - 1 ? 0 : sourceBuffer[sourceBufferDelta + (4 * (sourcePixelIndex + 1))];
                                    var n = (a1 * xness) + (a2 * (1 - xness));
                                    var targetOffset = ((rc.Y + y) * width) + rc.X + x + xDeltaInt;
                                    pixels8[(targetOffset * 4) + 3] = Math.Max(pixels8[(targetOffset * 4) + 3], (byte)(boldStrength * n));
                                }
                            }
                        }
                    }

                    if (Math.Abs(fontGamma - 1.4f) >= 0.001)
                    {
                        // Gamma correction (stbtt/FreeType would output in linear space whereas most real world usages will apply 1.4 or 1.8 gamma; Windows/XIV prebaked uses 1.4)
                        for (int y = rc.Y, y_ = rc.Y + rc.Height; y < y_; y++)
                        {
                            for (int x = rc.X, x_ = rc.X + rc.Width; x < x_; x++)
                            {
                                var i = (((y * width) + x) * 4) + 3;
                                pixels8[i] = (byte)(Math.Pow(pixels8[i] / 255.0f, 1.4f / fontGamma) * 255.0f);
                            }
                        }
                    }
                }

                CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, font, true, false);
                UnscaleFont(font, 1 / scale, false);
                font.BuildLookupTable();
            }
        }

        /// <summary>
        /// Decrease font reference counter.
        /// </summary>
        /// <param name="style">Font to release.</param>
        internal void DecreaseFontRef(GameFontStyle style)
        {
            lock (this.syncRoot)
            {
                if (!this.fontUseCounter.ContainsKey(style))
                    return;

                if ((this.fontUseCounter[style] -= 1) == 0)
                    this.fontUseCounter.Remove(style);
            }
        }

        private struct ImFontGlyphReal
        {
            public uint ColoredVisibleCodepoint;
            public float AdvanceX;
            public float X0;
            public float Y0;
            public float X1;
            public float Y1;
            public float U0;
            public float V0;
            public float U1;
            public float V1;

            public bool Colored
            {
                get => ((this.ColoredVisibleCodepoint >> 0) & 1) != 0;
                set => this.ColoredVisibleCodepoint = (this.ColoredVisibleCodepoint & 0xFFFFFFFEu) | (value ? 1u : 0u);
            }

            public bool Visible
            {
                get => ((this.ColoredVisibleCodepoint >> 1) & 1) != 0;
                set => this.ColoredVisibleCodepoint = (this.ColoredVisibleCodepoint & 0xFFFFFFFDu) | (value ? 2u : 0u);
            }

            public int Codepoint
            {
                get => (int)(this.ColoredVisibleCodepoint >> 2);
                set => this.ColoredVisibleCodepoint = (this.ColoredVisibleCodepoint & 3u) | ((uint)this.Codepoint << 2);
            }
        }
    }
}
