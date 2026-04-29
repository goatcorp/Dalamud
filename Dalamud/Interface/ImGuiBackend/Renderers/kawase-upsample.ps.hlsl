// kawase-upsample.ps.hlsl
// Dual Kawase blur, pass 4,5 (only upsample

cbuffer BlurCB : register(b0)
{
    float2 g_texelSize; // half-pixel of the source texture in UV space
    float  g_blurStrength; // Kawase spread factor (must match other passes)
    float  _pad0;
    float4 _pad1;
    float  _pad2;
    float3 _pad3;
    float4 _pad4;
    float4 _pad5;
}

Texture2D    t_source : register(t0);
SamplerState s_clamp  : register(s0);

float4 ps_main(float4 svpos : SV_Position, float2 uv : TEXCOORD0) : SV_TARGET
{
    float2 hp = g_texelSize; // half a source texel
    float2 fp = hp * 2.0f; // one full source texel
    float ofs = 1.0f + g_blurStrength;

    float4 sum = t_source.SampleLevel(s_clamp, uv + float2(-fp.x, 0.0f) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2(-hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( 0.0f, fp.y) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2( hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( fp.x, 0.0f) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2( hp.x, -hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( 0.0f, -fp.y) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2(-hp.x, -hp.y) * ofs, 0) * 2.0f;

    return float4(sum.rgb * (1.0f / 12.0f), 1.0f);
}

/*
Compile command:
fxc /T ps_5_0 /E ps_main /Fo kawase-upsample.ps.bytes kawase-upsample.ps.hlsl
*/

