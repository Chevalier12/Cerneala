struct FragmentInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
    linear centroid float2 CoveredTextureCoordinate : TEXCOORD2;
    float3 FirstEdges : TEXCOORD3;
    float2 LastEdges : TEXCOORD4;
};

Texture2D DrawingTexture : register(t0, space2);
SamplerState DrawingSampler : register(s0, space2);

bool CenterInImageDomainInterior(float3 firstEdges, float2 lastEdges)
{
    bool firstPositive = firstEdges.x > 0.0f && firstEdges.y > 0.0f;
    bool firstNegative = firstEdges.x < 0.0f && firstEdges.y < 0.0f;
    bool secondPositive = lastEdges.x > 0.0f && lastEdges.y > 0.0f;
    bool secondNegative = lastEdges.x < 0.0f && lastEdges.y < 0.0f;

    // E02 is shared by both triangles. It is an internal edge only when
    // their interiors lie on opposite sides. Same-side triangles overlap,
    // making E02 part of the logical image's external boundary instead.
    bool sharedInternal = firstEdges.z == 0.0f &&
        ((firstPositive && secondPositive) ||
         (firstNegative && secondNegative));
    return (firstEdges.z < 0.0f && firstPositive) ||
        (firstEdges.z > 0.0f && firstNegative) ||
        (firstEdges.z > 0.0f && secondPositive) ||
        (firstEdges.z < 0.0f && secondNegative) ||
        sharedInternal;
}

float4 main(FragmentInput input) : SV_Target0
{
    float2 coordinate = CenterInImageDomainInterior(input.FirstEdges, input.LastEdges)
        ? input.TextureCoordinate
        : input.CoveredTextureCoordinate;
    return DrawingTexture.Sample(DrawingSampler, coordinate) * input.Color;
}
