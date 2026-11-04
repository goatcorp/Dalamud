using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>Shaping, with DirectWrite's text analyzer.</summary>
internal sealed unsafe partial class DirectWriteFont
{
    private static readonly Lazy<ComPtr<IDWriteTextAnalyzer>> AnalyzerLazy =
        new(CreateAnalyzer, LazyThreadSafetyMode.ExecutionAndPublication);

    // Made on first use, as many fonts never shape.
    private int[] unitCodepointBuffer = [];
    private char[] unitBuffer = [];
    private DirectWriteScriptSink.ScriptRun[] scriptBuffer = [];
    private ushort[] clusterMapBuffer = [];
    private DWRITE_SHAPING_TEXT_PROPERTIES[] textPropsBuffer = [];
    private ushort[] shapeIndexBuffer = [];
    private DWRITE_SHAPING_GLYPH_PROPERTIES[] glyphPropsBuffer = [];
    private float[] advanceBuffer = [];
    private DWRITE_GLYPH_OFFSET[] offsetBuffer = [];

    private static IDWriteTextAnalyzer* Analyzer => AnalyzerLazy.Value.Get();

    /// <inheritdoc/>
    /// <remarks>Uses <see cref="IDWriteTextAnalyzer"/> per script, with the font's feature set and locale.
    /// DirectWrite does not take context, so the text around the run is not looked at.</remarks>
    public override void Shape(ShapeRun run, ShapedGlyphList output)
    {
        var count = run.End - run.Begin;
        if (count <= 0)
            return;

        // UTF-16, with the codepoint index of each code unit.
        var pooledUnits = PooledSpan<char>.FromBuffer(ref this.unitBuffer, count * 2);
        var pooledUnitCodepoints = PooledSpan<int>.FromBuffer(ref this.unitCodepointBuffer, count * 2);
        var units = pooledUnits.Span;
        var unitCodepoints = pooledUnitCodepoints.Span;
        var n = 0;
        for (var i = run.Begin; i < run.End; i++)
        {
            if (!Rune.TryCreate(run.Text[i], out var rune))
                rune = Rune.ReplacementChar;
            var length = rune.EncodeToUtf16(units[n..]);
            unitCodepoints.Slice(n, length).Fill(i);
            n += length;
        }

        var pooledScripts = PooledSpan<DirectWriteScriptSink.ScriptRun>.FromBuffer(ref this.scriptBuffer, n);
        var features = this.nativeFeatures;
        var typographic = new DWRITE_TYPOGRAPHIC_FEATURES
        {
            features = features is null ? null : (DWRITE_FONT_FEATURE*)Unsafe.AsPointer(ref features[0]),
            featureCount = (uint)(features?.Length ?? 0),
        };
        fixed (char* unitsPtr = units, localeName = this.locale)
        {
            fixed (DirectWriteScriptSink.ScriptRun* scripts = pooledScripts.Span)
            {
                var source = new DirectWriteTextSource(unitsPtr, (uint)n, localeName);
                var sink = new DirectWriteScriptSink(scripts, n);
                Analyzer->AnalyzeScript(
                        (IDWriteTextAnalysisSource*)&source,
                        0,
                        (uint)n,
                        (IDWriteTextAnalysisSink*)&sink)
                    .ThrowOnError();
                var scriptCount = sink.Count;
                if (scriptCount == 0)
                    scripts[scriptCount++] = new() { Position = 0, Length = (uint)n };
                new Span<DirectWriteScriptSink.ScriptRun>(scripts, scriptCount)
                    .Sort((a, b) => a.Position.CompareTo(b.Position));

                var size = this.SnapSize(run.Size, run.Density);
                var emSize = this.face.EmSize(size);
                var gdiDensity = this.DrawsBitmaps(size, run.Density) ? run.Density : 0;
                for (var s = 0; s < scriptCount; s++)
                {
                    var script = scripts[s];
                    var (position, length) = ((int)script.Position, (int)script.Length);
                    this.ShapeScript(
                        unitsPtr + position,
                        length,
                        unitCodepoints.Slice(position, length),
                        &script.Analysis,
                        localeName,
                        features is null ? null : &typographic,
                        emSize,
                        gdiDensity,
                        output);
                }
            }
        }

        pooledScripts.Recycle();
        pooledUnits.Recycle();
        pooledUnitCodepoints.Recycle();
    }

    private static ComPtr<IDWriteTextAnalyzer> CreateAnalyzer()
    {
        var analyzer = default(ComPtr<IDWriteTextAnalyzer>);
        DirectWriteUtilities.Factory->CreateTextAnalyzer(analyzer.GetAddressOf()).ThrowOnError();
        return analyzer;
    }

    // Shapes text of one script in a locale, and appends the glyphs. A nonzero gdiDensity takes GDI-compatible
    // advances, rounded to whole device pixels at that density.
    private void ShapeScript(
        char* text, int length, ReadOnlySpan<int> unitCodepoints, DWRITE_SCRIPT_ANALYSIS* script, char* localeName,
        DWRITE_TYPOGRAPHIC_FEATURES* features, float emSize, float gdiDensity, ShapedGlyphList output)
    {
        const int eNotSufficientBuffer = unchecked((int)0x8007007A);
        var pooledClusterMap = PooledSpan<ushort>.FromBuffer(ref this.clusterMapBuffer, length);
        var pooledTextProps = PooledSpan<DWRITE_SHAPING_TEXT_PROPERTIES>.FromBuffer(ref this.textPropsBuffer, length);
        var clusterMap = pooledClusterMap.Span;
        var textProps = pooledTextProps.Span;
        var maxGlyphs = ((length * 3) / 2) + 16;
        PooledSpan<ushort> pooledIndices;
        PooledSpan<DWRITE_SHAPING_GLYPH_PROPERTIES> pooledGlyphProps;
        uint glyphCount;
        while (true)
        {
            pooledIndices = PooledSpan<ushort>.FromBuffer(ref this.shapeIndexBuffer, maxGlyphs);
            pooledGlyphProps =
                PooledSpan<DWRITE_SHAPING_GLYPH_PROPERTIES>.FromBuffer(ref this.glyphPropsBuffer, maxGlyphs);
            var hr = this.GetGlyphs(
                text,
                length,
                script,
                localeName,
                features,
                clusterMap,
                textProps,
                pooledIndices.Span,
                pooledGlyphProps.Span,
                out glyphCount);
            if (hr != eNotSufficientBuffer)
            {
                hr.ThrowOnError();
                break;
            }

            pooledIndices.Recycle();
            pooledGlyphProps.Recycle();
            maxGlyphs *= 2;
        }

        var pooledAdvances = PooledSpan<float>.FromBuffer(ref this.advanceBuffer, (int)glyphCount);
        var pooledOffsets = PooledSpan<DWRITE_GLYPH_OFFSET>.FromBuffer(ref this.offsetBuffer, (int)glyphCount);
        var indices = pooledIndices.Span[..(int)glyphCount];
        var advances = pooledAdvances.Span;
        var offsets = pooledOffsets.Span;
        this.GetGlyphPlacements(
                text,
                length,
                script,
                localeName,
                features,
                emSize,
                gdiDensity,
                clusterMap,
                textProps,
                indices,
                pooledGlyphProps.Span[..(int)glyphCount],
                advances,
                offsets)
            .ThrowOnError();

        // A cluster is the code units mapping to the same first glyph.
        for (var u = 0; u < length;)
        {
            var g0 = (int)clusterMap[u];
            var ue = u + 1;
            while (ue < length && clusterMap[ue] == g0)
                ue++;
            var g1 = ue < length ? clusterMap[ue] : (int)glyphCount;
            var unsafeBefore = u > 0 && textProps[u - 1].canBreakShapingAfter == 0;
            for (var g = g0; g < g1; g++)
            {
                var offset = new Vector2(offsets[g].advanceOffset, -offsets[g].ascenderOffset);
                var advance = gdiDensity > 0 ? MathF.Round(advances[g] * gdiDensity) / gdiDensity : advances[g];
                output.Add(indices[g], unitCodepoints[u], advance, offset, g == g0 && unsafeBefore);
            }

            u = ue;
        }

        pooledClusterMap.Recycle();
        pooledTextProps.Recycle();
        pooledIndices.Recycle();
        pooledGlyphProps.Recycle();
        pooledAdvances.Recycle();
        pooledOffsets.Recycle();
    }

    private HRESULT GetGlyphs(
        char* text,
        int length,
        DWRITE_SCRIPT_ANALYSIS* script,
        char* localeName,
        DWRITE_TYPOGRAPHIC_FEATURES* features,
        Span<ushort> clusterMap,
        Span<DWRITE_SHAPING_TEXT_PROPERTIES> textProps,
        Span<ushort> indices,
        Span<DWRITE_SHAPING_GLYPH_PROPERTIES> glyphProps,
        out uint glyphCount)
    {
        HRESULT hr;
        uint count;
        var rangeLength = (uint)length;
        fixed (ushort* clusterMapPtr = clusterMap, indicesPtr = indices)
        {
            fixed (DWRITE_SHAPING_TEXT_PROPERTIES* textPropsPtr = textProps)
            {
                fixed (DWRITE_SHAPING_GLYPH_PROPERTIES* glyphPropsPtr = glyphProps)
                {
                    hr = Analyzer->GetGlyphs(
                        text,
                        (uint)length,
                        this.face.Native,
                        false,
                        false,
                        script,
                        localeName,
                        null,
                        features is null ? null : &features,
                        features is null ? null : &rangeLength,
                        features is null ? 0u : 1u,
                        (uint)indices.Length,
                        clusterMapPtr,
                        textPropsPtr,
                        indicesPtr,
                        glyphPropsPtr,
                        &count);
                }
            }
        }

        glyphCount = count;
        return hr;
    }

    private HRESULT GetGlyphPlacements(
        char* text,
        int length,
        DWRITE_SCRIPT_ANALYSIS* script,
        char* localeName,
        DWRITE_TYPOGRAPHIC_FEATURES* features,
        float emSize,
        float gdiDensity,
        Span<ushort> clusterMap,
        Span<DWRITE_SHAPING_TEXT_PROPERTIES> textProps,
        Span<ushort> indices,
        Span<DWRITE_SHAPING_GLYPH_PROPERTIES> glyphProps,
        Span<float> advances,
        Span<DWRITE_GLYPH_OFFSET> offsets)
    {
        var rangeLength = (uint)length;
        fixed (ushort* clusterMapPtr = clusterMap, indicesPtr = indices)
        {
            fixed (DWRITE_SHAPING_TEXT_PROPERTIES* textPropsPtr = textProps)
            {
                fixed (DWRITE_SHAPING_GLYPH_PROPERTIES* glyphPropsPtr = glyphProps)
                {
                    fixed (float* advancesPtr = advances)
                    {
                        fixed (DWRITE_GLYPH_OFFSET* offsetsPtr = offsets)
                        {
                            if (gdiDensity > 0)
                            {
                                return Analyzer->GetGdiCompatibleGlyphPlacements(
                                    text,
                                    clusterMapPtr,
                                    textPropsPtr,
                                    (uint)length,
                                    indicesPtr,
                                    glyphPropsPtr,
                                    (uint)indices.Length,
                                    this.face.Native,
                                    emSize,
                                    gdiDensity,
                                    null,
                                    false,
                                    false,
                                    false,
                                    script,
                                    localeName,
                                    features is null ? null : &features,
                                    features is null ? null : &rangeLength,
                                    features is null ? 0u : 1u,
                                    advancesPtr,
                                    offsetsPtr);
                            }

                            return Analyzer->GetGlyphPlacements(
                                text,
                                clusterMapPtr,
                                textPropsPtr,
                                (uint)length,
                                indicesPtr,
                                glyphPropsPtr,
                                (uint)indices.Length,
                                this.face.Native,
                                emSize,
                                false,
                                false,
                                script,
                                localeName,
                                features is null ? null : &features,
                                features is null ? null : &rangeLength,
                                features is null ? 0u : 1u,
                                advancesPtr,
                                offsetsPtr);
                        }
                    }
                }
            }
        }
    }
}
