// kawase-downsample.ps.hlsl
// Dual Kawase blur pass 1,2,3 (downsample)

cbuffer BlurCB : register(b0)
{
    float2 g_texelSize; // (1/rtWidth, 1/rtHeight)  – full-res source texel size
    float g_blurStrength; // Kawase spread factor; typical range 0.5 – 4
    float g_noiseOpacity; // unused in this pass
    float4 g_blurRectUV; // unused in this pass
    float g_rounding; // unused in this pass
    float3 _pad1;
    float4 g_tintColor; // unused in this pass
    float4 g_luminosityColor; // unused in this pass
}

Texture2D t_source : register(t0);
SamplerState s_clamp : register(s0);

float4 ps_main(float4 svpos : SV_Position, float2 uv : TEXCOORD0) : SV_TARGET
{
    float2 halfPixel = g_texelSize * 0.5f;
    float2 ofs = halfPixel * (1.0f + g_blurStrength);

    float4 sum  = t_source.SampleLevel(s_clamp, uv,                           0) * 4.0f;
    sum += t_source.SampleLevel(s_clamp, uv - ofs,                    0);
    sum += t_source.SampleLevel(s_clamp, uv + ofs,                    0);
    sum += t_source.SampleLevel(s_clamp, uv + float2( ofs.x, -ofs.y), 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2(-ofs.x,  ofs.y), 0);

    // FFXIV backbuffer has depth in alpha-channel for 3d so we don't pass it through
    return float4(sum.rgb * 0.125f, 1.0f);
}

/*
Compile command:
fxc /T ps_5_0 /E ps_main /Fo kawase-downsample.ps.bytes kawase-downsample.ps.hlsl
*/
