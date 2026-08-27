struct VertexInput
{
    float2 Position : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
    float4 Color : TEXCOORD2;
};

struct VertexOutput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

cbuffer FrameUniforms : register(b0, space1)
{
    float2 ViewportSize;
    float2 Padding;
};

VertexOutput main(VertexInput input)
{
    VertexOutput output;
    float2 normalized = input.Position / ViewportSize;
    output.Position = float4(
        (normalized.x * 2.0f) - 1.0f,
        1.0f - (normalized.y * 2.0f),
        0.0f,
        1.0f);
    output.TextureCoordinate = input.TextureCoordinate;
    output.Color = input.Color;
    return output;
}
