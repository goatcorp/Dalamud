using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

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
        private readonly ImFontPtr?[] fonts = new ImFontPtr?[FontNames.Length];

        private readonly int[] fontUseCounter = new int[FontNames.Length];
        private readonly List<Dictionary<char, Tuple<int, FdtReader.FontTableEntry>>> glyphRectIds = new();

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
        public static string DescribeFont(GameFont font)
        {
            return font switch
            {
                GameFont.Undefined => "-",
                GameFont.Axis96 => "AXIS (9.6pt)",
                GameFont.Axis12 => "AXIS (12pt)",
                GameFont.Axis14 => "AXIS (14pt)",
                GameFont.Axis18 => "AXIS (18pt)",
                GameFont.Axis36 => "AXIS (36pt)",
                GameFont.Jupiter16 => "Jupiter (16pt)",
                GameFont.Jupiter20 => "Jupiter (20pt)",
                GameFont.Jupiter23 => "Jupiter (23pt)",
                GameFont.Jupiter45 => "Jupiter Numeric (45pt)",
                GameFont.Jupiter46 => "Jupiter (46pt)",
                GameFont.Jupiter90 => "Jupiter Numeric (90pt)",
                GameFont.Meidinger16 => "Meidinger Numeric (16pt)",
                GameFont.Meidinger20 => "Meidinger Numeric (20pt)",
                GameFont.Meidinger40 => "Meidinger Numeric (40pt)",
                GameFont.MiedingerMid10 => "MiedingerMid (10pt)",
                GameFont.MiedingerMid12 => "MiedingerMid (12pt)",
                GameFont.MiedingerMid14 => "MiedingerMid (14pt)",
                GameFont.MiedingerMid18 => "MiedingerMid (18pt)",
                GameFont.MiedingerMid36 => "MiedingerMid (36pt)",
                GameFont.TrumpGothic184 => "Trump Gothic (18.4pt)",
                GameFont.TrumpGothic23 => "Trump Gothic (23pt)",
                GameFont.TrumpGothic34 => "Trump Gothic (34pt)",
                GameFont.TrumpGothic68 => "Trump Gothic (68pt)",
                _ => throw new ArgumentOutOfRangeException(nameof(font), font, "Invalid argument"),
            };
        }

        /// <summary>
        /// Determines whether a font should be able to display most of stuff.
        /// </summary>
        /// <param name="font">Font to check.</param>
        /// <returns>True if it can.</returns>
        public static bool IsGenericPurposeFont(GameFont font)
        {
            return font switch
            {
                GameFont.Axis96 => true,
                GameFont.Axis12 => true,
                GameFont.Axis14 => true,
                GameFont.Axis18 => true,
                GameFont.Axis36 => true,
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
        public static void CopyGlyphsAcrossFonts(ImFontPtr? source, ImFontPtr? target, bool missingOnly, bool rebuildLookupTable)
        {
            if (!source.HasValue || !target.HasValue)
                return;

            unsafe
            {
                var glyphs = (ImFontGlyphReal*)source.Value!.Glyphs.Data;
                for (int j = 0, j_ = source.Value!.Glyphs.Size; j < j_; j++)
                {
                    var glyph = &glyphs[j];
                    if (glyph->Codepoint < 32 || glyph->Codepoint >= 0xFFFF)
                        continue;

                    var prevGlyphPtr = (ImFontGlyphReal*)target.Value!.FindGlyphNoFallback((ushort)glyph->Codepoint).NativePtr;
                    if ((IntPtr)prevGlyphPtr == IntPtr.Zero)
                    {
                        target.Value!.AddGlyph(
                            target.Value!.ConfigData,
                            (ushort)glyph->Codepoint,
                            glyph->X0,
                            glyph->Y0,
                            glyph->X0 + ((glyph->X1 - glyph->X0) * target.Value!.FontSize / source.Value!.FontSize),
                            glyph->Y0 + ((glyph->Y1 - glyph->Y0) * target.Value!.FontSize / source.Value!.FontSize),
                            glyph->U0,
                            glyph->V0,
                            glyph->U1,
                            glyph->V1,
                            glyph->AdvanceX * target.Value!.FontSize / source.Value!.FontSize);
                    }
                    else if (!missingOnly)
                    {
                        prevGlyphPtr->X0 = glyph->X0;
                        prevGlyphPtr->Y0 = glyph->Y0;
                        prevGlyphPtr->X1 = glyph->X0 + ((glyph->X1 - glyph->X0) * target.Value!.FontSize / source.Value!.FontSize);
                        prevGlyphPtr->Y1 = glyph->Y0 + ((glyph->Y1 - glyph->Y0) * target.Value!.FontSize / source.Value!.FontSize);
                        prevGlyphPtr->U0 = glyph->U0;
                        prevGlyphPtr->V0 = glyph->V0;
                        prevGlyphPtr->U1 = glyph->U1;
                        prevGlyphPtr->V1 = glyph->V1;
                        prevGlyphPtr->AdvanceX = glyph->AdvanceX * target.Value!.FontSize / source.Value!.FontSize;
                    }
                }
            }

            if (rebuildLookupTable)
                target.Value!.BuildLookupTable();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a new GameFontHandle, and increases internal font reference counter, and if it's first time use, then the font will be loaded on next font building process.
        /// </summary>
        /// <param name="gameFont">Font to use.</param>
        /// <returns>Handle to game font that may or may not be ready yet.</returns>
        public GameFontHandle NewFontRef(GameFont gameFont)
        {
            var fontIndex = (int)gameFont;
            var needRebuild = false;

            lock (this.syncRoot)
            {
                var prev = this.fontUseCounter[fontIndex] == 0;
                this.fontUseCounter[fontIndex] += 1;
                needRebuild = prev != (this.fontUseCounter[fontIndex] == 0);
            }

            if (needRebuild)
                this.interfaceManager.RebuildFonts();

            return new(this, gameFont);
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <param name="gameFont">Font to get.</param>
        /// <returns>Corresponding font or null.</returns>
        public ImFontPtr? GetFont(GameFont gameFont) => this.fonts[(int)gameFont];

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(ImFontPtr? source, GameFont target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(source, this.fonts[(int)target], missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(GameFont source, ImFontPtr? target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(this.fonts[(int)source], target, missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Fills missing glyphs in target font from source font, if both are not null.
        /// </summary>
        /// <param name="source">Source font.</param>
        /// <param name="target">Target font.</param>
        /// <param name="missingOnly">Whether to copy missing glyphs only.</param>
        /// <param name="rebuildLookupTable">Whether to call target.BuildLookupTable().</param>
        public void CopyGlyphsAcrossFonts(GameFont source, GameFont target, bool missingOnly, bool rebuildLookupTable)
        {
            GameFontManager.CopyGlyphsAcrossFonts(this.fonts[(int)source], this.fonts[(int)target], missingOnly, rebuildLookupTable);
        }

        /// <summary>
        /// Build fonts before plugins do something more. To be called from InterfaceManager.
        /// </summary>
        public void BuildFonts()
        {
            this.glyphRectIds.Clear();
            var io = ImGui.GetIO();
            io.Fonts.TexDesiredWidth = 4096;

            for (var i = 0; i < FontNames.Length; i++)
            {
                this.fonts[i] = null;
                this.glyphRectIds.Add(new());

                var fdt = this.fdts[i];
                if (this.fontUseCounter[i] == 0 || fdt == null)
                    continue;

                Log.Information($"GameFontManager BuildFont: {FontNames[i]}");

                var font = io.Fonts.AddFontDefault();
                this.fonts[i] = font;
                foreach (var glyph in fdt.Glyphs)
                {
                    var c = glyph.Char;
                    if (c < 32 || c >= 0xFFFF)
                        continue;

                    this.glyphRectIds[i][c] = Tuple.Create(io.Fonts.AddCustomRectFontGlyph(font, c, glyph.BoundingWidth + 1, glyph.BoundingHeight + 1, glyph.BoundingWidth + glyph.NextOffsetX, new Vector2(0, glyph.CurrentOffsetY)), glyph);
                }
            }
        }

        /// <summary>
        /// Post-build fonts before plugins do something more. To be called from InterfaceManager.
        /// </summary>
        public unsafe void AfterBuildFonts()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels8, out var width, out var height);
            var pixels32 = (uint*)pixels8;

            for (var i = 0; i < this.fonts.Length; i++)
            {
                if (!this.fonts[i].HasValue)
                    continue;

                var font = this.fonts[i]!.Value;
                var fdt = this.fdts[i];
                var fontPtr = font.NativePtr;
                fontPtr->ConfigData->SizePixels = fontPtr->FontSize = fdt.FontHeader.LineHeight;
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

                fixed (char* c = FontNames[i])
                {
                    for (var j = 0; j < 40; j++)
                        fontPtr->ConfigData->Name[j] = 0;
                    Encoding.UTF8.GetBytes(c, FontNames[i].Length, fontPtr->ConfigData->Name, 40);
                }

                foreach (var (c, (rectId, glyph)) in this.glyphRectIds[i])
                {
                    var rc = io.Fonts.GetCustomRectByIndex(rectId);
                    var sourceBuffer = this.texturePixels[glyph.TextureFileIndex];
                    var sourceBufferDelta = glyph.TextureChannelByteIndex;
                    for (var y = 0; y < glyph.BoundingHeight; y++)
                    {
                        for (var x = 0; x < glyph.BoundingWidth; x++)
                        {
                            var a = sourceBuffer[sourceBufferDelta + (4 * (((glyph.TextureOffsetY + y) * fdt.FontHeader.TextureWidth) + glyph.TextureOffsetX + x))];
                            pixels32[((rc.Y + y) * width) + rc.X + x] = (uint)(a << 24) | 0xFFFFFFu;
                        }
                    }
                }
            }

            this.CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, GameFont.Axis96, true, false);
            this.CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, GameFont.Axis12, true, false);
            this.CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, GameFont.Axis14, true, false);
            this.CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, GameFont.Axis18, true, false);
            this.CopyGlyphsAcrossFonts(InterfaceManager.DefaultFont, GameFont.Axis36, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis18, GameFont.Jupiter16, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Jupiter20, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Jupiter23, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Jupiter45, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Jupiter46, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Jupiter90, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis18, GameFont.Meidinger16, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Meidinger20, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.Meidinger40, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis96, GameFont.MiedingerMid10, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis12, GameFont.MiedingerMid12, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis14, GameFont.MiedingerMid14, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis18, GameFont.MiedingerMid18, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.MiedingerMid36, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis18, GameFont.TrumpGothic184, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.TrumpGothic23, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.TrumpGothic34, true, false);
            this.CopyGlyphsAcrossFonts(GameFont.Axis36, GameFont.TrumpGothic68, true, false);

            foreach (var font in this.fonts)
                font?.BuildLookupTable();
        }

        /// <summary>
        /// Decrease font reference counter and release if nobody is using it.
        /// </summary>
        /// <param name="gameFont">Font to release.</param>
        internal void DecreaseFontRef(GameFont gameFont)
        {
            var fontIndex = (int)gameFont;
            var needRebuild = false;

            lock (this.syncRoot)
            {
                var prev = this.fontUseCounter[fontIndex] == 0;
                this.fontUseCounter[fontIndex] -= 1;
                needRebuild = prev != (this.fontUseCounter[fontIndex] == 0);
            }

            if (needRebuild)
                this.interfaceManager.RebuildFonts();
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
