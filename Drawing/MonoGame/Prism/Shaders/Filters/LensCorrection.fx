float4 ApplyLensCorrection(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    int edgeMode = (int)(FilterOptions2.y + 0.5);
    float redCyan = FilterOptions0.y;
    float blueYellow = FilterOptions0.z;
    float vignette = LensVignetteFactor(uv);
    if (redCyan == 0.0 && blueYellow == 0.0)
    {
        float4 sampled = SampleResamplingSource(
            MapLensCorrectionCoordinate(uv, 0.0),
            profile,
            edgeMode,
            0.0);
        return float4(sampled.rgb * vignette, sampled.a);
    }
    float redShift = 0.01 * (redCyan - (blueYellow * 0.5));
    float greenShift = -0.005 * (redCyan + blueYellow);
    float blueShift = 0.01 * (blueYellow - (redCyan * 0.5));
    float4 redSample = SampleResamplingSource(
        MapLensCorrectionCoordinate(uv, redShift), profile, edgeMode, 0.0);
    float4 greenSample = SampleResamplingSource(
        MapLensCorrectionCoordinate(uv, greenShift), profile, edgeMode, 0.0);
    float4 blueSample = SampleResamplingSource(
        MapLensCorrectionCoordinate(uv, blueShift), profile, edgeMode, 0.0);
    return float4(
        float3(redSample.r, greenSample.g, blueSample.b) * vignette,
        max(redSample.a, max(greenSample.a, blueSample.a)));
}
