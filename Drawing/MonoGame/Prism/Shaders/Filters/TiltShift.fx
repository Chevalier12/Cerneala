float4 ApplyTiltShift(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    float2 direction = float2(
        -sin(FilterOptions0.z),
        cos(FilterOptions0.z));
    float distance = abs(dot(uv - FilterOptions0.xy, direction));
    float amount = smoothstep(
        FilterOptions0.w,
        FilterOptions0.w + max(FilterOptions1.x, 0.000001),
        distance);
    return lerp(
        center,
        SampleNeighborhoodDisk(
            uv,
            FilterOptions9.xy,
            (int)(FilterOptions9.z + 0.5),
            0,
            profile,
            0.0,
            false),
        amount);
}
