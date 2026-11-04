#include "Renderer.Common.hlsl"
#include "../../../../ImGuiBackend/Renderers/imgui-text.hlsli"

struct ImDrawVert {
    float2 position : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
    uint4 ext0 : TEXCOORD1;
    uint4 ext1 : TEXCOORD2;
};

struct VsData {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
    nointerpolation uint4 ext0 : TEXCOORD1;
    nointerpolation uint4 ext1 : TEXCOORD2;
};

struct PsData {
    float4 color : COLOR0;
};

Texture2D s_texture : register(t0);
SamplerState s_sampler : register(s0);

VsData vs_main(const ImDrawVert idv) {
    VsData result;
    result.position = mul(g_view, float4(idv.position, 0, 1));
    result.uv = idv.uv;
    result.color = idv.color;
    result.ext0 = idv.ext0;
    result.ext1 = idv.ext1;
    return result;
}

// The target is transparent, so subpixel text is drawn in grayscale. Outputs straight alpha, as ImGui's colors are.
float4 ps_main(const VsData vd) : SV_TARGET {
    float4 texel = s_texture.Sample(s_sampler, vd.uv);
    uint tag = vd.ext0.x;
    uint mode = ExtMode(tag);
    [branch]
    if (mode == EXT_MODE_SUBPIXEL) {
        float3 rgb = ExtSubpixelAt(s_sampler, ExtUv(vd.uv, tag), ExtCoverageTexel().x);
        return float4(vd.color.rgb, vd.color.a * dot(rgb, 1.0 / 3));
    }

    [branch]
    if (mode == EXT_MODE_GLYPH)
        return float4(vd.color.rgb, vd.color.a * GlyphCoverage.SampleLevel(s_sampler, ExtUv(vd.uv, tag), 0));

    [branch]
    if (mode == EXT_MODE_DILATE)
        return float4(vd.color.rgb, vd.color.a * ExtDilate(s_sampler, vd.uv, vd.ext0));

    [branch]
    if (mode == EXT_MODE_COLOR) {
        float4 glyph = ExtColorGlyph(s_sampler, vd.uv, vd.color.rgb, vd.ext0, vd.ext1.x);
        return float4(glyph.a > 0 ? glyph.rgb / glyph.a : 0, glyph.a * vd.color.a);
    }

    return texel * vd.color;
}

/*

fxc /nologo /O3 /T vs_5_0 /E vs_main /Fo Renderer.DrawToPremul.vs.bin Renderer.DrawToPremul.hlsl
fxc /nologo /O3 /T ps_5_0 /E ps_main /Fo Renderer.DrawToPremul.ps.bin Renderer.DrawToPremul.hlsl

*/
