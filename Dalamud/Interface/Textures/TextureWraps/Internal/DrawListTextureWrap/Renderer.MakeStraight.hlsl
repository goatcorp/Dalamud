Texture2D s_texture : register(t0);

float4 vs_main(const float2 position : POSITION) : SV_POSITION {
    return float4(position, 0, 1);
}

float4 ps_main(const float4 position : SV_POSITION) : SV_TARGET {
    const float4 src = s_texture[position.xy];
    return src.a > 0
            ? float4(src.rgb / src.a, src.a)
            : float4(0, 0, 0, 0);
}

/*

fxc /Zi /T vs_5_0 /E vs_main /Fo Renderer.MakeStraight.vs.bin Renderer.MakeStraight.hlsl
fxc /Zi /T ps_5_0 /E ps_main /Fo Renderer.MakeStraight.ps.bin Renderer.MakeStraight.hlsl

*/
