using System.Collections.Generic;
using System.Linq;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.EasyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;
using Dalamud.Utility.Timing;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal;

/// <summary>
/// This class manages interaction with the ImGui interface.
/// </summary>
internal partial class InterfaceManager
{
    /// <summary>
    /// Base font size of Dalamud and plugins' text, in points.
    /// </summary>
    internal const float DefaultFontSizePt = 12.0f;

    /// <summary>
    /// Base font size of Dalamud and plugins' text, in pixels.
    /// </summary>
    internal const float DefaultFontSizePx = DefaultFontSizePt * 4.0f / 3.0f;

    private const ushort Fallback1Codepoint = 0x3013; // Geta mark; FFXIV uses this to indicate that a glyph is missing.
    private const ushort Fallback2Codepoint = '-';    // FFXIV uses dash if Geta mark is unavailable.

    private readonly HashSet<SpecialGlyphRequest> glyphRequests = new();
    private readonly Dictionary<ImFontPtr, TargetFontModification> loadedFontInfo = new();

    private bool isRebuildingFonts = false;

    /// <summary>
    /// Sets up a deferred invocation of font rebuilding, before the next render frame.
    /// </summary>
    public void RebuildFonts()
    {
        if (this.scene == null)
        {
            Log.Verbose("[FONT] RebuildFonts(): scene not ready, doing nothing");
            return;
        }

        Log.Verbose("[FONT] RebuildFonts() called");

        // don't invoke this multiple times per frame, in case multiple plugins call it
        if (!this.isRebuildingFonts)
        {
            Log.Verbose("[FONT] RebuildFonts() trigger");
            this.isRebuildingFonts = true;
            this.scene.OnNewRenderFrame += this.RebuildFontsInternal;
        }
    }

    /// <summary>
    /// Requests a default font of specified size to exist.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="ranges">Ranges of glyphs.</param>
    /// <returns>Requets handle.</returns>
    public unsafe SpecialGlyphRequest NewFontSizeRef(float size, List<Tuple<ushort, ushort>> ranges)
    {
        var allContained = false;
        var fonts = ImGui.GetIO().Fonts.Fonts;
        ImFontPtr foundFont = null;
        foreach (var font in fonts.AsSpan())
        {
            if (this.glyphRequests.All(x => x.FontInternal.NativePtr != font.NativePtr))
                continue;

            allContained = true;
            foreach (var range in ranges)
            {
                if (!allContained)
                    break;

                for (var j = range.Item1; j <= range.Item2 && allContained; j++)
                    allContained &= font.FindGlyphNoFallback(j).NativePtr != null;
            }

            if (allContained)
                foundFont = font;

            break;
        }

        var req = new SpecialGlyphRequest(this, size, ranges);
        req.FontInternal = foundFont;

        if (!allContained)
            this.RebuildFonts();

        return req;
    }

    /// <summary>
    /// Requests a default font of specified size to exist.
    /// </summary>
    /// <param name="size">Font size in pixels.</param>
    /// <param name="text">Text to calculate glyph ranges from.</param>
    /// <returns>Requets handle.</returns>
    public SpecialGlyphRequest NewFontSizeRef(float size, string text)
    {
        List<Tuple<ushort, ushort>> ranges = new();
        foreach (var c in new SortedSet<char>(text.ToHashSet()))
        {
            if (ranges.Any() && ranges[^1].Item2 + 1 == c)
                ranges[^1] = Tuple.Create<ushort, ushort>(ranges[^1].Item1, c);
            else
                ranges.Add(Tuple.Create<ushort, ushort>(c, c));
        }

        return this.NewFontSizeRef(size, ranges);
    }

    // This is intended to only be called as a handler attached to scene.OnNewRenderFrame
    private void RebuildFontsInternal()
    {
        Log.Verbose("[FONT] RebuildFontsInternal() called");
        this.SetupFonts();

        Log.Verbose("[FONT] RebuildFontsInternal() detaching");
        this.scene!.OnNewRenderFrame -= this.RebuildFontsInternal;

        Log.Verbose("[FONT] Calling InvalidateFonts");
        this.scene.InvalidateFonts();

        Log.Verbose("[FONT] Font Rebuild OK!");

        this.isRebuildingFonts = false;
    }

    /// <summary>
    /// Loads font for use in ImGui text functions.
    /// </summary>
    private void SetupFonts()
    {
        foreach (var ignoreCustomDefaultFont in new[] { false, true })
        {
            using var b = new SetupFontsClass(this, this.Font.CustomDefaultFontLoadFailed = ignoreCustomDefaultFont);
            using var setupFontsTimings = Timings.Start("IM SetupFonts");

            Log.Verbose("[FONT] SetupFonts - Clear Previous");
            b.ClearOldData();

            Log.Verbose("[FONT] SetupFonts - Default Font");
            DefaultFont = b.AddDefaultFont();

            Log.Verbose("[FONT] SetupFonts - FontAwesome icon font");
            IconFont = b.AddFontAwesomeFont();

            Log.Verbose("[FONT] SetupFonts - Mono Font");
            MonoFont = b.AddMonoFont();

            Log.Verbose("[FONT] SetupFonts - Default font but in requested size for requested glyphs");
            b.AddRequestedExtraFonts();

            if (!b.BuildFonts())
            {
                b.ClearOldData();
                continue;
            }

            b.AfterBuildFonts();
            return;
        }

        throw new InvalidOperationException("BuildFonts failure");
    }

    /// <summary>
    /// Collection of font-related properties.
    /// </summary>
    public class FontProperties
    {
        /// <summary>
        /// Fallback font chain to use, if font chain is effectively empty.
        /// </summary>
        public static readonly FontChain FallbackFontChain = new(
            new FontChainEntry(new(GameFontFamily.Axis), DefaultFontSizePx));

        /// <summary>
        /// Gets or sets a value indicating whether the last attempt at loading custom default font has failed.
        /// </summary>
        public bool CustomDefaultFontLoadFailed { get; internal set; } = false;

        /// <summary>
        /// Gets or sets the overrided font family and variant chain, instead of using the value from configuration.
        /// </summary>
        public FontChain? FontChainOverride { get; set; } = null;

        /// <summary>
        /// Gets the font family and variant chain to use.
        /// </summary>
        public FontChain FontChain
        {
            get
            {
                var r = this.FontChainOverride ?? Configuration.DefaultFontChain;
                return r.Fonts.Any(x => x.Ident != default) ? r : FallbackFontChain;
            }
        }

        /// <summary>
        /// Gets or sets the overrided font gamma value, instead of using the value from configuration.
        /// </summary>
        public float? GammaOverride { get; set; } = null;

        /// <summary>
        /// Gets the font gamma value to use.
        /// </summary>
        public float Gamma => Math.Max(0.1f, this.GammaOverride.GetValueOrDefault(Configuration.FontGammaLevel));

        /// <summary>
        /// Gets a value indicating whether the values set into this class is different from Dalamud Configuration.
        /// </summary>
        public bool HasDifferentConfigurationValues
        {
            get
            {
                var conf = Configuration;
                return Math.Abs(this.Gamma - conf.FontGammaLevel) > float.Epsilon ||
                       !this.FontChain.Equals(conf.DefaultFontChain);
            }
        }

        private static DalamudConfiguration Configuration => Service<DalamudConfiguration>.Get();

        /// <summary>
        /// Remove override values.
        /// </summary>
        public void ResetOverrides()
        {
            this.GammaOverride = null;
            this.FontChainOverride = null;
        }
    }

    /// <summary>
    /// Represents a glyph request.
    /// </summary>
    public class SpecialGlyphRequest : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecialGlyphRequest"/> class.
        /// </summary>
        /// <param name="manager">InterfaceManager to associate.</param>
        /// <param name="size">Font size in pixels.</param>
        /// <param name="ranges">Codepoint ranges.</param>
        internal SpecialGlyphRequest(InterfaceManager manager, float size, List<Tuple<ushort, ushort>> ranges)
        {
            this.Manager = manager;
            this.Size = size;
            this.CodepointRanges = ranges;
            this.Manager.glyphRequests.Add(this);
        }

        /// <summary>
        /// Gets the font of specified size, or DefaultFont if it's not ready yet.
        /// </summary>
        public ImFontPtr Font
        {
            get
            {
                unsafe
                {
                    return this.FontInternal.NativePtr == null ? DefaultFont : this.FontInternal;
                }
            }
        }

        /// <summary>
        /// Gets or sets the associated ImFont.
        /// </summary>
        internal ImFontPtr FontInternal { get; set; }

        /// <summary>
        /// Gets associated InterfaceManager.
        /// </summary>
        internal InterfaceManager Manager { get; init; }

        /// <summary>
        /// Gets font size.
        /// </summary>
        internal float Size { get; init; }

        /// <summary>
        /// Gets codepoint ranges.
        /// </summary>
        internal List<Tuple<ushort, ushort>> CodepointRanges { get; init; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Manager.glyphRequests.Remove(this);
        }
    }

    private class TargetFontModification : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFontModification"/> class.
        /// Constructs new target font modification information, assuming that AXIS fonts will not be applied.
        /// </summary>
        /// <param name="name">Name of the font to write to ImGui font information.</param>
        /// <param name="sizePx">Target font size in pixels, which will not be considered for further scaling.</param>
        internal TargetFontModification(string name, float sizePx)
        {
            this.Name = name;
            this.Axis = AxisMode.Suppress;
            this.TargetSizePx = sizePx;
            this.Scale = 1;
            this.SourceAxis = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFontModification"/> class.
        /// Constructs new target font modification information.
        /// </summary>
        /// <param name="gfm">An instance of <see cref="GameFontManager"/>.</param>
        /// <param name="name">Name of the font to write to ImGui font information.</param>
        /// <param name="axis">Whether and how to use AXIS fonts.</param>
        /// <param name="gameFontFamily">Game font family to use.</param>
        /// <param name="sizePx">Target font size in pixels, which will not be considered for further scaling.</param>
        /// <param name="lineHeightPx">Target font line height in pixels.</param>
        /// <param name="globalFontScale">Font scale to be referred for loading AXIS font of appropriate size.</param>
        /// <param name="axisOffsetX">Target font offset X.</param>
        /// <param name="axisOffsetY">Target font offset Y.</param>
        /// <param name="axisLetterSpacing">Target font letter spacing.</param>
        internal TargetFontModification(
            GameFontManager gfm,
            string name,
            AxisMode axis,
            GameFontFamily gameFontFamily,
            float sizePx,
            float lineHeightPx,
            float globalFontScale,
            float axisOffsetX,
            float axisOffsetY,
            float axisLetterSpacing)
        {
            if (!Enum.IsDefined(gameFontFamily) || gameFontFamily == GameFontFamily.Undefined)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameFontFamily),
                    gameFontFamily,
                    $"{nameof(GameFontFamily.Undefined)} is not a valid parameter value.");
            }

            this.Name = name;
            this.Axis = axis;
            this.GameFontFamily = gameFontFamily;
            this.TargetSizePx = sizePx;
            this.LineHeightPx = lineHeightPx;
            this.Scale = globalFontScale;
            this.AxisOffsetX = axisOffsetX;
            this.AxisOffsetY = axisOffsetY;
            this.AxisLetterSpacing = axisLetterSpacing;
            this.SourceAxis = gfm.NewFontRef(new(gameFontFamily, this.TargetSizePx * this.Scale));
        }

        internal enum AxisMode
        {
            Suppress,
            GameGlyphsOnly,
            Overwrite,
        }

        internal string Name { get; }

        internal AxisMode Axis { get; }

        internal GameFontFamily GameFontFamily { get; }

        internal float TargetSizePx { get; }

        internal float LineHeightPx { get; }

        internal float AxisOffsetX { get; }

        internal float AxisOffsetY { get; }

        internal float AxisLetterSpacing { get; }

        internal float Scale { get; }

        internal GameFontHandle? SourceAxis { get; }

        public void Dispose() => this.SourceAxis?.Dispose();
    }
}
