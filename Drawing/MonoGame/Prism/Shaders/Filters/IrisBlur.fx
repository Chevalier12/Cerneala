float4 ApplyIrisBlur(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    float2 delta = uv - FilterOptions0.xy;
    float angle = -FilterOptions1.y;
    float2 rotated = float2(
        (delta.x * cos(angle)) - (delta.y * sin(angle)),
        (delta.x * sin(angle)) + (delta.y * cos(angle))) /
        max(FilterOptions0.zw, 0.000001);
    float amount = smoothstep(
        1.0,
        1.0 + max(FilterOptions1.x, 0.000001),
        length(rotated));
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
