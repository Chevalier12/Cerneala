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
    float4 Destination;
};

VertexShaderOutput main(uint vertexId : SV_VertexID)
{
    VertexShaderOutput output;
    float2 corner = vertexId == 0
        ? float2(0.0f, 0.0f)
        : vertexId == 1
            ? float2(2.0f, 0.0f)
            : float2(0.0f, 2.0f);
    float2 position = Destination.xy + (corner * Destination.zw);
    float2 normalized = position / ViewportSize;
    output.Position = float4(
        (normalized.x * 2.0f) - 1.0f,
        1.0f - (normalized.y * 2.0f),
        0.0f,
        1.0f);
    output.Color = float4(1.0f, 1.0f, 1.0f, 1.0f);
    output.TextureCoordinates = corner;
    return output;
}
