// kawase-upsample-composite.ps.hlsl
// Dual Kawase blur, pass 6 (upsample + composite)

cbuffer BlurCB : register(b0)
{
    float2 g_texelSize; // (1/rtWidth, 1/rtHeight)  – full-res texel size
    float g_blurStrength; // Kawase spread factor (must match downsample pass)
    float g_noiseOpacity; // noise layer opacity [0, 1]
    float4 g_blurRectUV; // (x1/w, y1/h, x2/w, y2/h) normalised [0, 1]
    float g_rounding; // corner radius in pixels
    float3 _pad1;
    float4 g_tintColor; // RGB = tint colour, A = blend strength [0, 1]
    float4 g_luminosityColor; // RGB = luminosity target, A = blend strength [0, 1]
}

Texture2D t_source : register(t0);
Texture2D t_noise : register(t1);
SamplerState s_clamp : register(s0);
SamplerState s_wrap : register(s1);

float3 RGBtoHSL(float3 c)
{
    float maxC = max(c.r, max(c.g, c.b));
    float minC = min(c.r, min(c.g, c.b));
    float d    = maxC - minC;
    float L    = (maxC + minC) * 0.5f;
    if (d < 1e-5f)
        return float3(0.0f, 0.0f, L);
    float S  = d / (1.0f - abs(2.0f * L - 1.0f));
    float hh = (maxC == c.r) ? fmod((c.g - c.b) / d + 6.0f, 6.0f)
             : (maxC == c.g) ? (c.b - c.r) / d + 2.0f
                             : (c.r - c.g) / d + 4.0f;
    return float3(hh / 6.0f, S, L);
}

float3 HSLtoRGB(float3 hsl)
{
    float H  = hsl.x, S = hsl.y, L = hsl.z;
    float C  = (1.0f - abs(2.0f * L - 1.0f)) * S;
    float hh = H * 6.0f;
    float X  = C * (1.0f - abs(fmod(hh, 2.0f) - 1.0f));
    float m  = L - C * 0.5f;
    float3 rgb = (hh < 1.0f) ? float3(C, X, 0) :
                 (hh < 2.0f) ? float3(X, C, 0) :
                 (hh < 3.0f) ? float3(0, C, X) :
                 (hh < 4.0f) ? float3(0, X, C) :
                 (hh < 5.0f) ? float3(X, 0, C) :
                               float3(C, 0, X);
    return saturate(rgb + m);
}

float3 BlendLuminosity(float3 base, float3 blend)
{
    float3 b = RGBtoHSL(base);
    return HSLtoRGB(float3(b.xy, RGBtoHSL(blend).z));
}

float3 BlendColor(float3 base, float3 blend)
{
    float3 bl = RGBtoHSL(blend);
    return HSLtoRGB(float3(bl.xy, RGBtoHSL(base).z));
}

float roundedBoxSDF(float2 p, float2 halfSize, float radius)
{
    float2 q = abs(p) - halfSize + radius;
    return length(max(q, 0.0f)) + min(max(q.x, q.y), 0.0f) - radius;
}

float4 ps_main(float4 svpos : SV_Position, float2 uv : TEXCOORD0) : SV_TARGET
{
    float2 resolution = 1.0f / g_texelSize;

    float2 rectMin = g_blurRectUV.xy * resolution;
    float2 rectMax = g_blurRectUV.zw * resolution;
    float2 halfSize = (rectMax - rectMin) * 0.5f;
    float2 rectCenter = rectMin + halfSize;
    float sdf = roundedBoxSDF(svpos.xy - rectCenter, halfSize, g_rounding);
    float alpha = 1.0f - saturate(sdf + 0.5f);
    if (alpha <= 0.0f)
        discard;

    float2 hp = g_texelSize; // half a source texel
    float2 fp = hp * 2.0f; // one full source texel
    float ofs = 1.0f + g_blurStrength;

    float4 sum = t_source.SampleLevel(s_clamp, uv + float2(-fp.x,  0.0f) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2(-hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( 0.0f, fp.y) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2( hp.x, hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( fp.x, 0.0f) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2( hp.x, -hp.y) * ofs, 0) * 2.0f;
    sum += t_source.SampleLevel(s_clamp, uv + float2( 0.0f, -fp.y) * ofs, 0);
    sum += t_source.SampleLevel(s_clamp, uv + float2(-hp.x, -hp.y) * ofs, 0) * 2.0f;
    float3 result = sum.rgb * (1.0f / 12.0f);

    if (g_luminosityColor.a > 0.0f)
        result = lerp(result, BlendLuminosity(result, g_luminosityColor.rgb), g_luminosityColor.a);

    if (g_tintColor.a > 0.0f)
        result = lerp(result, BlendColor(result, g_tintColor.rgb), g_tintColor.a);

    if (g_noiseOpacity > 0.0f)
    {
        // Noise uses s_wrap because it's a small tileable texture
        float3 noise = t_noise.SampleLevel(s_wrap, svpos.xy / 256.0f, 0).rgb;
        result = lerp(result, noise, g_noiseOpacity);
    }

    return float4(result, alpha);
}

/*
Compile command:
fxc /T ps_5_0 /E ps_main /Fo kawase-upsample-composite.ps.bytes kawase-upsample-composite.ps.hlsl
*/

