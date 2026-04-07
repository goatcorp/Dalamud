// blur-fullscreen.vs.hlsl
// Generates a full-screen CCW triangle entirely from SV_VertexID

void vs_main(
    uint vid : SV_VertexID,
    out float4 pos : SV_Position,
    out float2 uv  : TEXCOORD0)
{
    // Produces UVs: (0,0) (2,0) (0,2)
    uv  = float2((vid << 1) & 2, vid & 2);
    pos = float4(uv * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), 0.0f, 1.0f);
}

/*
Compile command:
fxc /T vs_5_0 /E vs_main /Fo blur-fullscreen.vs.bytes blur-fullscreen.vs.hlsl
*/

