float4 ApplyRipple(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    uint seed = (uint)FilterOptions0.z | ((uint)FilterOptions0.w << 16);
    float wavelength = max(FilterOptions0.y, 1.0);
    float pixelY = uv.y / PixelSize.y;
    float basePhase = 6.28318531 * pixelY / wavelength;
    float phaseNoise = OceanSimplex(
        float2(pixelY / (wavelength * 4.0), 0.0),
        seed);
    float phase =
        (((seed & 0xffffu) / 65536.0) * 6.28318531) +
        (phaseNoise * 1.1);
    float displacement =
        (0.75 * sin(basePhase + phase)) +
        (0.25 * sin((basePhase * 2.03) + (phase * 0.55)));
    uv.x += displacement * FilterOptions0.x * PixelSize.x;
    return SampleResamplingSource(
        uv,
        profile,
        (int)(FilterOptions1.x + 0.5),
        0.0);
}
