struct FragmentInput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
    linear centroid float2 CoveredTextureCoordinate : TEXCOORD2;
    nointerpolation float4 FirstCorners : TEXCOORD3;
    nointerpolation float4 LastCorners : TEXCOORD4;
};

Texture2D DrawingTexture : register(t0, space2);
SamplerState DrawingSampler : register(s0, space2);

// The input corners are the physical positions sent to the rasterizer. Scale
// each difference only when its endpoint range would overflow a float product
// or underflow a very small edge. Positive powers of two preserve its sign.
float2 ScaledDifference(float2 origin, float2 end)
{
    const float upper = 1099511627776.0f; // 2^40
    const float lower = 9.09494701772928237915e-13f; // 2^-40
    float magnitude = max(
        max(abs(origin.x), abs(origin.y)),
        max(abs(end.x), abs(end.y)));
    if (magnitude > upper)
    {
        [unroll]
        for (int index = 0; index < 3; index++)
        {
            if (magnitude > upper)
            {
                origin *= lower;
                end *= lower;
                magnitude *= lower;
            }
        }
    }
    else if (magnitude > 0.0f && magnitude < lower)
    {
        [unroll]
        for (int index = 0; index < 3; index++)
        {
            if (magnitude < lower)
            {
                origin *= upper;
                end *= upper;
                magnitude *= upper;
            }
        }
    }
    return end - origin;
}

float Cross(float2 first, float2 second)
{
    precise float left = first.x * second.y;
    precise float right = first.y * second.x;
    return left - right;
}

bool CenterInImageDomainInterior(FragmentInput input)
{
    float2 q0 = input.FirstCorners.xy;
    float2 q1 = input.FirstCorners.zw;
    float2 q2 = input.LastCorners.xy;
    float2 q3 = input.LastCorners.zw;
    float2 center = input.Position.xy;

    float2 d01 = ScaledDifference(q0, q1);
    float2 d12 = ScaledDifference(q1, q2);
    float2 d02 = ScaledDifference(q0, q2);
    float2 d23 = ScaledDifference(q2, q3);
    float2 d30 = ScaledDifference(q3, q0);
    float2 r0 = ScaledDifference(q0, center);
    float2 r1 = ScaledDifference(q1, center);
    float2 r2 = ScaledDifference(q2, center);
    float2 r3 = ScaledDifference(q3, center);

    float e01 = Cross(d01, r0);
    float e12 = Cross(d12, r1);
    float e02 = Cross(d02, r0);
    float e23 = Cross(d23, r2);
    float e30 = Cross(d30, r3);
    bool firstAlive = Cross(d01, d02) != 0.0f;
    bool secondAlive = Cross(d02, -d30) != 0.0f;

    bool firstPositive = e01 > 0.0f && e12 > 0.0f;
    bool firstNegative = e01 < 0.0f && e12 < 0.0f;
    bool secondPositive = e23 > 0.0f && e30 > 0.0f;
    bool secondNegative = e23 < 0.0f && e30 < 0.0f;

    // E02 is shared by both triangles. It is an internal edge only when
    // their interiors lie on opposite sides. Same-side triangles overlap,
    // making E02 part of the logical image's external boundary instead.
    bool sharedInternal = firstAlive && secondAlive && e02 == 0.0f &&
        ((firstPositive && secondPositive) ||
         (firstNegative && secondNegative));
    return (firstAlive && e02 < 0.0f && firstPositive) ||
        (firstAlive && e02 > 0.0f && firstNegative) ||
        (secondAlive && e02 > 0.0f && secondPositive) ||
        (secondAlive && e02 < 0.0f && secondNegative) ||
        sharedInternal;
}

float4 main(FragmentInput input) : SV_Target0
{
    float2 coordinate = CenterInImageDomainInterior(input)
        ? input.TextureCoordinate
        : input.CoveredTextureCoordinate;
    return DrawingTexture.Sample(DrawingSampler, coordinate) * input.Color;
}
