float3 ApplyPosterize(float3 color, VertexShaderOutput input)
{
    float steps = max(1.0, floor(FilterOptions0.x + 0.5) - 1.0);
    return floor((color * steps) + 0.5) / steps;
}
