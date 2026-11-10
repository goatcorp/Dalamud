#include "Renderer.Common.hlsl"

struct ImDrawVert {
    float2 position : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
};

struct VsData {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR0;
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
    return result;
}

float4 ps_main(const VsData vd) : SV_TARGET {
    return s_texture.Sample(s_sampler, vd.uv) * vd.color;
}

/*

fxc /Zi /T vs_5_0 /E vs_main /Fo Renderer.DrawToPremul.vs.bin Renderer.DrawToPremul.hlsl
fxc /Zi /T ps_5_0 /E ps_main /Fo Renderer.DrawToPremul.ps.bin Renderer.DrawToPremul.hlsl

*/
