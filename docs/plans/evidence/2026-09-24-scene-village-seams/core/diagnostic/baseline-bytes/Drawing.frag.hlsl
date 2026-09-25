struct FragmentInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

Texture2D DrawingTexture : register(t0, space2);
SamplerState DrawingSampler : register(s0, space2);

float4 main(FragmentInput input) : SV_Target0
{
    return DrawingTexture.Sample(DrawingSampler, input.TextureCoordinate) * input.Color;
}
