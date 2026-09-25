struct FragmentInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
    linear centroid float2 CoveredTextureCoordinate : TEXCOORD2;
    float4 Domain : TEXCOORD3;
};

Texture2D DrawingTexture : register(t0, space2);
SamplerState DrawingSampler : register(s0, space2);

bool CenterInsideImageDomain(float4 domain)
{
    // Both constituent triangles use this exact diagonal quantity. There is
    // no triangle-local centroid decision along an internal image edge.
    float diagonal = domain.x - domain.y;
    if (diagonal >= 0.0f && domain.y >= 0.0f && domain.x <= 1.0f)
    {
        return true;
    }

    float signedArea = domain.w - domain.z;
    float opposite = (domain.x * domain.w) - (domain.y * domain.z);
    float fourth = -diagonal;
    float origin = signedArea - opposite - fourth;
    if (signedArea > 0.0f)
    {
        return origin >= 0.0f && opposite >= 0.0f && fourth >= 0.0f;
    }
    if (signedArea < 0.0f)
    {
        return origin <= 0.0f && opposite <= 0.0f && fourth <= 0.0f;
    }
    // A zero-area constituent triangle emits no covered fragments.
    return false;
}

float4 main(FragmentInput input) : SV_Target0
{
    float2 coordinate = CenterInsideImageDomain(input.Domain)
        ? input.TextureCoordinate
        : input.CoveredTextureCoordinate;
    return DrawingTexture.Sample(DrawingSampler, coordinate) * input.Color;
}
