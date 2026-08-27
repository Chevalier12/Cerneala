float4 ApplyTransform(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 origin = FilterOptions2.xy;
    float2 size = max(FilterOptions3.xy, 1.0);
    float2 position = uv - origin - (FilterOptions0.xy / size);
    float cosine = cos(-FilterOptions1.x);
    float sine = sin(-FilterOptions1.x);
    float2 unrotated = float2(
        (position.x * cosine) - (position.y * sine),
        (position.x * sine) + (position.y * cosine));
    float determinant = max(
        1.0 - (FilterOptions1.y * FilterOptions1.z),
        0.000001);
    position = float2(
        unrotated.x - (FilterOptions1.y * unrotated.y),
        unrotated.y - (FilterOptions1.z * unrotated.x)) /
        determinant;
    float2 scale = FilterOptions0.zw;
    float2 safeScale = float2(
        scale.x < 0.0 ? min(scale.x, -0.000001) : max(scale.x, 0.000001),
        scale.y < 0.0 ? min(scale.y, -0.000001) : max(scale.y, 0.000001));
    position /= safeScale;
    return SampleResamplingSource(
        origin + position,
        profile,
        (int)(FilterOptions2.z + 0.5),
        0.0);
}
