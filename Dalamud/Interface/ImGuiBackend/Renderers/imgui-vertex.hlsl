// fxc /nologo /O3 /T vs_5_0 /E main /Fo imgui-vertex.hlsl.bytes imgui-vertex.hlsl
//
// Passes ImDrawVert::ext through untouched, so that pixel shaders set by draw callbacks may use it.

cbuffer ProjectionMatrixBuffer : register(b0)
{
    row_major float4x4 ProjectionMatrix;
};

struct VS_INPUT
{
    float2 pos : POSITION;
    float2 uv : TEXCOORD0;
    float4 col : COLOR0;
    uint4 ext0 : TEXCOORD1;
    uint4 ext1 : TEXCOORD2;
    uint3 ext2 : TEXCOORD3;
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

PS_INPUT main(VS_INPUT input)
{
    PS_INPUT output;
    output.pos = mul(float4(input.pos.xy, 0, 1), ProjectionMatrix);
    output.col = input.col;
    output.uv = input.uv;
    output.ext0 = input.ext0;
    output.ext1 = input.ext1;
    output.ext2 = input.ext2;
    return output;
}
