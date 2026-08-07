float3 ApplyBrightnessContrast(float3 color, VertexShaderOutput input)
{
    if (FilterOptions0.z > 0.5)
    {
        float legacyFactor = max(0.0, 1.0 + FilterOptions0.y);
        return ((color - 0.5) * legacyFactor) +
            0.5 + FilterOptions0.x;
    }

    const float pivot = 0.18;
    float exposure = pow(2.0, FilterOptions0.x);
    float contrast = max(0.001, pow(2.0, FilterOptions0.y * 2.0));
    if (contrast == 1.0)
    {
        return color * exposure;
    }
    return pow(
        max(color * (exposure / pivot), 0.0),
        contrast) * pivot;
}
