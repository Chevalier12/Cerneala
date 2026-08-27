float2 BasReliefLightDirection(float code)
{
    int direction = (int)round(code);
    const float diagonal = 0.7071067811865476;
    if (direction == 0) return float2(0.0, -1.0);
    if (direction == 1) return float2(diagonal, -diagonal);
    if (direction == 2) return float2(1.0, 0.0);
    if (direction == 3) return float2(diagonal, diagonal);
    if (direction == 4) return float2(0.0, 1.0);
    if (direction == 5) return float2(-diagonal, diagonal);
    if (direction == 6) return float2(-1.0, 0.0);
    if (direction == 7) return float2(-diagonal, -diagonal);
    return float2(-diagonal, diagonal);
}

float4 BasReliefComposite(float2 uv)
{
    float4 guided = PosterEdgesRawSample(uv);
    if (guided.a <= 0.000001)
    {
        return 0.0;
    }

    float detail = clamp(FilterOptions1.x, 0.0, 64.0) * 0.25;
    float2 gradient = GuidedScharrGradient(uv, 1.0);
    float3 normal = normalize(float3(-gradient * detail, 1.0));
    float2 lightDirection = BasReliefLightDirection(FilterOptions3.x);
    float shade = saturate(
        0.5 + (0.5 * dot(normal.xy, lightDirection)));
    float3 color = lerp(
        saturate(FilterOptions2.rgb),
        saturate(FilterOptions0.rgb),
        shade);
    return float4(color * guided.a, guided.a);
}
