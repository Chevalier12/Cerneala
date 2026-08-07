float4 ApplyGlass(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    int textureKind = (int)(FilterOptions0.z + 0.5);
    float radius = 1.0 + (saturate(FilterOptions0.y) * 3.0);
    float2 pixelPosition = uv / PixelSize;
    float2 displacement = float2(
        GlassHeight(pixelPosition + float2(radius, 0.0), textureKind) -
            GlassHeight(pixelPosition - float2(radius, 0.0), textureKind),
        GlassHeight(pixelPosition + float2(0.0, radius), textureKind) -
            GlassHeight(pixelPosition - float2(0.0, radius), textureKind)) *
        0.5;
    displacement = clamp(displacement, -0.5, 0.5);
    displacement = lerp(
        displacement,
        -displacement,
        step(0.5, FilterOptions1.x));
    return SampleResamplingSource(
        uv - (displacement * FilterOptions0.x * PixelSize),
        profile,
        0,
        0.0);
}
