float3 ApplyGradientMap(float3 color, VertexShaderOutput input)
{
    float coordinate = AdjustmentLuminance(color);
    if (FilterOptions0.z > 0.5)
    {
        int x = (int)fmod(floor(input.Position.x), 4.0);
        int y = (int)fmod(floor(input.Position.y), 4.0);
        float ordered;
        if (y == 0) ordered = x == 0 ? 0.0 : (x == 1 ? 8.0 : (x == 2 ? 2.0 : 10.0));
        else if (y == 1) ordered = x == 0 ? 12.0 : (x == 1 ? 4.0 : (x == 2 ? 14.0 : 6.0));
        else if (y == 2) ordered = x == 0 ? 3.0 : (x == 1 ? 11.0 : (x == 2 ? 1.0 : 9.0));
        else ordered = x == 0 ? 15.0 : (x == 1 ? 7.0 : (x == 2 ? 13.0 : 5.0));
        coordinate = saturate(
            coordinate + ((ordered - 7.5) / (16.0 * 255.0)));
    }
    coordinate = FilterOptions0.y > 0.5
        ? 1.0 - coordinate
        : coordinate;
    float width = max(FilterTextureSize.x, 1.0);
    float textureCoordinate =
        ((saturate(coordinate) * (width - 1.0)) + 0.5) / width;
    float4 mapped = tex2D(
        SecondaryTextureSampler,
        float2(textureCoordinate, 0.5));
    return mapped.a > 0.0 ? mapped.rgb / mapped.a : mapped.rgb;
}
