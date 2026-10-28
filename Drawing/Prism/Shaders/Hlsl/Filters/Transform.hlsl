float4 ApplyTransform(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 origin = FilterOptions2.xy;
    // uv spans the execution extent, whose pixel size is PixelSize. Rotation and skew are
    // rigid in pixels, as the CPU reference and the bounds assume; in uv a non-square
    // extent would shear them.
    float2 position = ((uv - origin) / PixelSize) - FilterOptions0.xy;
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
        origin + (position * PixelSize),
        profile,
        (int)(FilterOptions2.z + 0.5),
        0.0);
}
