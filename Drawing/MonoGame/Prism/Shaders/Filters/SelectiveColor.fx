float3 ApplySelectiveColor(float3 color, VertexShaderOutput input)
{
    float maximum = max(color.r, max(color.g, color.b));
    float minimum = min(color.r, min(color.g, color.b));
    float weights[9] =
    {
        max(color.r - max(color.g, color.b), 0.0),
        max(min(color.r, color.g) - color.b, 0.0),
        max(color.g - max(color.r, color.b), 0.0),
        max(min(color.g, color.b) - color.r, 0.0),
        max(color.b - max(color.r, color.g), 0.0),
        max(min(color.r, color.b) - color.g, 0.0),
        max((minimum * 2.0) - 1.0, 0.0),
        max(1.0 - (abs(maximum - 0.5) + abs(minimum - 0.5)), 0.0),
        max(1.0 - (maximum * 2.0), 0.0)
    };
    float4 adjustments[9] =
    {
        FilterOptions0,
        FilterOptions1,
        FilterOptions2,
        FilterOptions3,
        FilterOptions4,
        FilterOptions5,
        FilterOptions6,
        FilterOptions7,
        FilterOptions8
    };
    bool relative = FilterOptions9.x < 0.5;
    float3 delta = 0.0;
    [unroll]
    for (int index = 0; index < 9; index++)
    {
        float4 adjustment = adjustments[index];
        float3 rangeDelta =
            ((-1.0 - adjustment.rgb) * adjustment.a) -
            adjustment.rgb;
        if (relative)
        {
            rangeDelta *= 1.0 - color;
        }
        rangeDelta = clamp(rangeDelta, -color, 1.0 - color);
        delta += weights[index] * rangeDelta;
    }
    return color + delta;
}
