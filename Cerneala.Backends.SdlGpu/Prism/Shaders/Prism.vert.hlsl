struct VertexInput
{
    float2 Position : TEXCOORD0;
    float2 TextureCoordinates : TEXCOORD1;
    float4 Color : TEXCOORD2;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
    float2 TextureCoordinates : TEXCOORD1;
};

cbuffer PrismVertexUniforms : register(b0, space1)
{
    float2 ViewportSize;
    float2 Padding;
};

VertexShaderOutput main(VertexInput input)
{
    VertexShaderOutput output;
    float2 normalized = input.Position / ViewportSize;
    output.Position = float4(
        (normalized.x * 2.0f) - 1.0f,
        1.0f - (normalized.y * 2.0f),
        0.0f,
        1.0f);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}
