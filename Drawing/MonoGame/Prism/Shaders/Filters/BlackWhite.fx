float3 ApplyBlackWhite(float3 color, VertexShaderOutput input)
{
    float normalization = 1.0;
    if (FilterOptions0.w > 0.5)
    {
        float sum =
            FilterOptions0.x + FilterOptions0.y + FilterOptions0.z;
        if (sum != 0.0)
        {
            normalization = abs(1.0 / sum);
        }
    }
    return dot(color, FilterOptions0.xyz) * normalization;
}
