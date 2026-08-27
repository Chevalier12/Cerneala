float4 CatalogColorMatrix(float4 source)
{
    float4 straight = float4(
        Unpremultiply(source),
        source.a);
    float4 transformed = float4(
        dot(straight, FilterOptions2),
        dot(straight, FilterOptions3),
        dot(straight, FilterOptions4),
        dot(straight, FilterOptions5)) +
        FilterOptions6;
    if (FilterOptions0.x >= 0.5)
    {
        transformed = saturate(transformed);
    }
    else
    {
        transformed.rgb = clamp(
            transformed.rgb,
            -65504.0,
            65504.0);
        transformed.a = saturate(transformed.a);
    }
    return float4(
        transformed.rgb * transformed.a,
        transformed.a);
}
