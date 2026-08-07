float4 ApplyLensBlur(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    float depth = FilterOptions1.w;
    if (FilterHeader.w > 0.5)
    {
        float4 depthSample = SampleNeighborhoodResource(uv);
        int depthChannel = (int)(FilterOptions1.z + 0.5);
        if (depthChannel == 0)
        {
            depth = dot(
                depthSample.rgb,
                float3(0.2126, 0.7152, 0.0722));
        }
        else if (depthChannel == 1) depth = depthSample.r;
        else if (depthChannel == 2) depth = depthSample.g;
        else if (depthChannel == 3) depth = depthSample.b;
        else depth = depthSample.a;
    }
    if (FilterOptions2.x > 0.5)
    {
        depth = 1.0 - depth;
    }
    float focus = FilterHeader.w > 0.5
        ? saturate(abs(depth - FilterOptions1.w))
        : 1.0;
    if (focus <= 0.000001)
    {
        return center;
    }
    return SampleLensAperture(
        uv,
        FilterOptions0.x * focus,
        (int)(FilterOptions9.z + 0.5),
        profile);
}
