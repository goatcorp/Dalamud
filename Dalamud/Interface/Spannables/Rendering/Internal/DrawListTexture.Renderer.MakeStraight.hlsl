RWTexture2D<unorm float4> s_output : register(u1);

float4 vs_main(const float2 position : POSITION) : SV_POSITION {
    return float4(position, 0, 1);
}

float4 ps_main(const float4 position : SV_POSITION) : SV_TARGET {
    const float4 src = s_output[position.xy];
    s_output[position.xy] =
        src.a > 0
            ? float4(src.rgb / src.a, src.a)
            : float4(0, 0, 0, 0);

    return float4(0, 0, 0, 0); // unused
}

/*

fxc /Zi /T vs_5_0 /E vs_main /Fo DrawListTexture.Renderer.MakeStraight.vs.bin DrawListTexture.Renderer.MakeStraight.hlsl
fxc /Zi /T ps_5_0 /E ps_main /Fo DrawListTexture.Renderer.MakeStraight.ps.bin DrawListTexture.Renderer.MakeStraight.hlsl

*/
