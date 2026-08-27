float4 ApplyRadialBlur(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    float2 centerUv = FilterOptions0.zw;
    float amount = FilterOptions0.y;
    float4 total = 0.0;
    int count = max(1, min((int)(FilterOptions9.z + 0.5), 17));
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float position = count <= 1
                ? 0.0
                : (index / (count - 1.0)) - 0.5;
            float2 sampleUv;
            if (FilterOptions0.x < 0.5)
            {
                float angle = position * amount;
                float2 delta = (uv - centerUv) / PixelSize;
                float cosine = cos(angle);
                float sine = sin(angle);
                sampleUv = centerUv + (float2(
                    (delta.x * cosine) - (delta.y * sine),
                    (delta.x * sine) + (delta.y * cosine)) * PixelSize);
            }
            else
            {
                sampleUv = lerp(uv, centerUv, position * amount);
            }
            total += SampleNeighborhood(sampleUv, profile, 0);
        }
    }
    return total / count;
}
