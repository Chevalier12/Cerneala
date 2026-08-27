float ResolveBlendIfValue(float3 color)
{
    if (BlendIfChannel < 0.5)
    {
        return BlendLuminosity(color);
    }
    if (BlendIfChannel < 1.5)
    {
        return color.r;
    }
    if (BlendIfChannel < 2.5)
    {
        return color.g;
    }
    return color.b;
}

float EvaluateBlendIfRange(float value, float4 range)
{
    float black = range.y > range.x
        ? saturate((value - range.x) / (range.y - range.x))
        : value >= range.x ? 1.0 : 0.0;
    float white = range.w > range.z
        ? 1.0 -
            saturate((value - range.z) / (range.w - range.z))
        : value <= range.z ? 1.0 : 0.0;
    return black * white;
}
