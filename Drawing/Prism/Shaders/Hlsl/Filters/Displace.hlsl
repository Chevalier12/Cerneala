float4 ApplyDisplace(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 mapUv = FilterOptions0.z < 0.5
        ? uv
        : frac(uv / max(PixelSize * FilterTextureSize, 0.000001));
    float4 map = tex2D(DisplacementMapSampler, mapUv);
    float2 displacement = float2(
        saturate(ResamplingChannel(map, (int)(FilterOptions1.x + 0.5))),
        saturate(ResamplingChannel(map, (int)(FilterOptions1.y + 0.5)))) -
        0.5;
    float2 mapped = uv -
        (displacement * FilterOptions0.xy * PixelSize);
    return SampleResamplingSource(
        mapped,
        profile,
        (int)(FilterOptions0.w + 0.5),
        0.0);
}
