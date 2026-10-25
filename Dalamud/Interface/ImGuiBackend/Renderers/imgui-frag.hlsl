// fxc /nologo /O3 /T ps_5_0 /E main /Fo imgui-frag.hlsl.bytes imgui-frag.hlsl
//
// Outputs premultiplied color, and per-channel coverage for the ONE / INV_SRC1_COLOR blend.

#include "imgui-text.hlsli"

cbuffer TextBuffer : register(b0)
{
    float4 GammaRatios;
    float EnhancedContrast;
    float ClearTypeLevel;

    // 0 if the render target does not take subpixel text, such as one cleared to transparent, or one on a monitor that
    // does not show it.
    float SubpixelAllowed;
    float GrayscaleEnhancedContrast;

    // 1 if the monitor's subpixels are in blue, green, red order. Glyphs are rasterized red first.
    float Bgr;
    float3 Padding;
};

struct PS_INPUT
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
    float2 uv : TEXCOORD0;
    nointerpolation uint4 ext0 : TEXCOORD1;
    nointerpolation uint4 ext1 : TEXCOORD2;
    nointerpolation uint3 ext2 : TEXCOORD3;
};

struct PS_OUTPUT
{
    float4 Color : SV_Target0;
    float4 Blend : SV_Target1;
};

Texture2D FontTexture : register(t0);
SamplerState FontSampler : register(s0);

PS_OUTPUT main(PS_INPUT input)
{
    PS_OUTPUT output;

    // Before the branches. Derivatives need the whole quad.
    float2 uvDx = ddx(input.uv);
    float2 uvDy = ddy(input.uv);

    float4 color = input.col;
    uint tag = input.ext0.x;
    uint mode = ExtMode(tag);
    [branch]
    if (mode == EXT_MODE_SUBPIXEL)
    {
        // A subpixel glyph has three texels across per pixel, left to right. Subpixel text is snapped at layout, so
        // this only fails for vertices scaled, mirrored or turned after it.
        float2 coverageTexel = ExtCoverageTexel();
        float2 perPixelX = uvDx / coverageTexel;
        float2 perPixelY = uvDy / coverageTexel;
        float2 perTexelTriple = float2(1.0 / 3, 1);
        float2 gx = perPixelX * perTexelTriple;
        float2 gy = perPixelY * perTexelTriple;
        bool unscaled = all(abs(gx - float2(1, 0)) < 1.0 / 64) && all(abs(abs(gy) - float2(0, 1)) < 1.0 / 64);
        float3 rgb = ExtSubpixelAt(FontSampler, ExtUv(input.uv, tag), coverageTexel.x);
        float3 coverage = ExtSubpixelCoverage(
            Bgr > 0 ? rgb.bgr : rgb,
            color.rgb,
            unscaled && SubpixelAllowed > 0,
            ClearTypeLevel,
            EnhancedContrast,
            GrayscaleEnhancedContrast,
            GammaRatios) * color.a;
        output.Color = float4(color.rgb * coverage, max(max(coverage.r, coverage.g), coverage.b));
        output.Blend = float4(coverage, 1);
        return output;
    }

    [branch]
    if (mode == EXT_MODE_COLOR)
    {
        // The text color only adds through the mask, and its alpha fades the whole glyph.
        float4 glyph = ExtColorGlyph(FontSampler, input.uv, color.rgb, input.ext0, input.ext1.x) * color.a;
        output.Color = glyph;
        output.Blend = glyph.aaaa;
        return output;
    }

    [branch]
    if (mode == EXT_MODE_GLYPH)
    {
        float coverage = GlyphCoverage.SampleLevel(FontSampler, ExtUv(input.uv, tag), 0);
        if (tag & EXT_CORRECT)
            coverage = ExtGrayscaleCoverage(coverage, color.rgb, GrayscaleEnhancedContrast, GammaRatios);
        color.a *= coverage;
    }
    else if (mode == EXT_MODE_DILATE)
        color.a *= ExtDilate(FontSampler, input.uv, input.ext0);
    else
        color *= FontTexture.SampleGrad(FontSampler, input.uv, uvDx, uvDy);
    output.Color = float4(color.rgb * color.a, color.a);
    output.Blend = color.aaaa;
    return output;
}
