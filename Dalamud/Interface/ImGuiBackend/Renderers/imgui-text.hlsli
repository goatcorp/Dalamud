// ImDrawVert::ext as Dalamud's text uses it, shared by the renderers. ext[0] = 0 leaves ext unused.
//
// ext[0] bits 0-2 are the mode. 0 = none. 1 = grayscale glyph. 2 = subpixel glyph. 3 = dilated glyph. 4 = color glyph.
// ext[0] bit 3 is set for a dilated glyph that is for subpixel rendering.
// ext[0] bit 4 is set for a color glyph, which is in GlyphColors. The others are in GlyphCoverage.
// ext[0] bit 5 is set if a grayscale glyph's coverage is to be corrected for contrast and gamma, as DirectWrite does.
// ext[0] bits 6-15 are the slice of its array.
// ext[0] bits 16-23 are the dilation radius, in 1/16 pixels of the glyph bitmap.
// ext[0] bits 24-31 are the dilation softness, in 1/64 of the radius. See ExtRim.
// ext[1] and ext[2] are the glyph box in texels, (x | y << 16), which dilating samples within.
// ext[3] is the top-left texel of the text color mask of a color glyph, (x | y << 16).
// ext[4] bit 0 is set if a color glyph has a mask. Bits 3-12 are its slice of GlyphCoverage.

#define EXT_MODE_NONE 0
#define EXT_MODE_GLYPH 1
#define EXT_MODE_SUBPIXEL 2
#define EXT_MODE_DILATE 3
#define EXT_MODE_COLOR 4
#define EXT_CORRECT 32

// In whole texels on each side of the pixel.
#define EXT_MAX_REACH 8

// The glyph atlas, bound by the renderers for every draw. Coverage of grayscale and subpixel glyphs and of the masks
// of color glyphs, and color glyphs. A glyph too large for the atlas binds arrays of its own size instead.
Texture2DArray<float> GlyphCoverage : register(t1);
Texture2DArray<float4> GlyphColors : register(t2);

// The size of a texel of GlyphCoverage in texture coordinates.
float2 ExtCoverageTexel()
{
    uint width, height, elements;
    GlyphCoverage.GetDimensions(width, height, elements);
    return 1.0 / float2(width, height);
}

// The size of a texel of GlyphColors in texture coordinates.
float2 ExtColorTexel()
{
    uint width, height, elements;
    GlyphColors.GetDimensions(width, height, elements);
    return 1.0 / float2(width, height);
}

uint ExtMode(uint tag)
{
    return tag & 7;
}

bool ExtIsColor(uint tag)
{
    return (tag & 16) != 0;
}

float3 ExtUv(float2 uv, uint tag)
{
    return float3(uv, (tag >> 6) & 1023);
}

// The red, green and blue coverage of a subpixel glyph at a point. They are the texels left of it, at it and right of
// it. At a pixel center, the point is that of the middle texel of the pixel. dx is the width of a texel in texture
// coordinates.
float3 ExtSubpixelAt(SamplerState samp, float3 p, float dx)
{
    return float3(
        GlyphCoverage.SampleLevel(samp, p - float3(dx, 0, 0), 0),
        GlyphCoverage.SampleLevel(samp, p, 0),
        GlyphCoverage.SampleLevel(samp, p + float3(dx, 0, 0), 0));
}

// A color glyph, premultiplied. It is its own colors, plus the text color where its mask says.
float4 ExtColorGlyph(SamplerState samp, float2 uv, float3 textColor, uint4 ext0, uint ext4)
{
    float4 color = GlyphColors.SampleLevel(samp, ExtUv(uv, ext0.x), 0);
    if (ext4 & 1)
    {
        // The mask is as large as the glyph, at another place.
        float2 shift = float2(ext0.w & 0xFFFF, ext0.w >> 16) - float2(ext0.y & 0xFFFF, ext0.y >> 16);
        float2 texel = uv / ExtColorTexel() + shift;
        float mask = GlyphCoverage.SampleLevel(samp, float3(texel * ExtCoverageTexel(), (ext4 >> 3) & 1023), 0);
        color.rgb += textColor * mask;
    }

    return color;
}

// The weight is 0 from |o| = rim.z on.
float ExtWeight(float3 rim, float2 o)
{
    return saturate(rim.x - length(o) * rim.y);
}

// The weight is 1 out to (1 - k) * radius and then falls linearly to 0. The fall is at least 1 texel long and ends
// within EXT_MAX_REACH.
float3 ExtRim(float radius, float k)
{
    k = k > 1 ? min(k, max(EXT_MAX_REACH / radius, 1)) : k;
    float inner = max(1 - k, 0) * radius;
    float outer = max(k, 1) * radius;
    float fade = max(outer - inner, 1);
    float middle = (inner + outer) * 0.5 + 0.5;
    float slope = fade > 1 ? 1 / fade : 1;
    return float3(0.5 + middle * slope, slope, middle + 0.5 * fade);
}

// The largest coverage within the radius, weighted by distance. Samples outside the glyph box belong to other glyphs.
// The radius is in pixels. For subpixel glyphs, it steps three texels across and takes the largest of the three. Color
// glyphs give their alpha.
float ExtDilate(SamplerState samp, float2 uv, uint4 ext)
{
    bool subpixel = (ext.x & 8) != 0;
    bool color = ExtIsColor(ext.x);
    float2 texel = color ? ExtColorTexel() : ExtCoverageTexel();
    float2 step = subpixel ? float2(texel.x * 3, texel.y) : texel;

    // Radius 0 takes the texel alone.
    uint tag = ext.x;
    float radius = ((tag >> 16) & 255) / 16.0;
    float3 rim = radius > 0 ? ExtRim(radius, ((tag >> 24) & 255) / 64.0) : float3(1, 1, 0);
    int reach = min((int)ceil(rim.z), EXT_MAX_REACH);
    float2 rectMin = float2(ext.y & 0xFFFF, ext.y >> 16) * texel;
    float2 rectMax = float2(ext.z & 0xFFFF, ext.z >> 16) * texel;
    float slice = (tag >> 6) & 1023;
    float alpha = 0;
    [loop]
    for (int y = -reach; y <= reach && alpha < 1; y++)
    {
        [loop]
        for (int x = -reach; x <= reach && alpha < 1; x++)
        {
            float weight = ExtWeight(rim, float2(x, y));
            float2 p = uv + float2(x, y) * step;
            if (weight > alpha && all(p >= rectMin) && all(p <= rectMax))
            {
                float coverage;
                if (subpixel)
                {
                    float3 rgb = ExtSubpixelAt(samp, float3(p, slice), texel.x);
                    coverage = max(max(rgb.r, rgb.g), rgb.b);
                }
                else if (color)
                {
                    coverage = GlyphColors.SampleLevel(samp, float3(p, slice), 0).a;
                }
                else
                {
                    coverage = GlyphCoverage.SampleLevel(samp, float3(p, slice), 0);
                }

                alpha = max(alpha, coverage * weight);
            }
        }
    }

    return alpha;
}

// As DWrite_ApplyLightOnDarkContrastAdjustment. Light text gets no contrast, and text from a lightness of 0.5 down
// gets it all.
float ExtContrastLightOnDark(float contrast, float3 color)
{
    return contrast * saturate(dot(color, float3(0.30f, 0.59f, 0.11f) * -4.0f) + 3.0f);
}

// As DWrite_ApplyAlphaCorrection. Blending the encoded values then approximates blending in linear light.
float3 ExtCorrectAlpha(float3 a, float3 f, float4 g)
{
    return a + a * (1 - a) * ((g.x * f + g.y) * a + (g.z * f + g.w));
}

// Per-channel coverage of a subpixel glyph, corrected as DirectWrite does.
// subpixel is false for grayscale coverage, such as when texels do not map 1:1 to pixels. Grayscale coverage gets
// grayscaleContrast instead of contrast.
float3 ExtSubpixelCoverage(
    float3 coverage, float3 color, bool subpixel, float level, float contrast, float grayscaleContrast,
    float4 gammaRatios)
{
    float gray = dot(coverage, 1.0 / 3);
    coverage = subpixel ? lerp(gray.xxx, coverage, level) : gray.xxx;
    float3 corrected = subpixel ? color : dot(color, float3(0.25f, 0.5f, 0.25f)).xxx;
    float k = ExtContrastLightOnDark(subpixel ? contrast : grayscaleContrast, color);
    coverage = coverage * (k + 1) / (coverage * k + 1);
    return saturate(ExtCorrectAlpha(coverage, corrected, gammaRatios));
}

// Coverage of a grayscale glyph, corrected as DirectWrite does.
float ExtGrayscaleCoverage(float coverage, float3 color, float contrast, float4 gammaRatios)
{
    return ExtSubpixelCoverage(coverage.xxx, color, false, 0, 0, contrast, gammaRatios).x;
}
