float3 ApplyExposure(float3 color, VertexShaderOutput input)
{
    const float minimumContrast = 0.001;
    const float middleGray = 0.18;
    const float videoTransferPower = 0.54644808743169393;
    float exposure = FilterOptions0.x;
    float contrast = max(
        minimumContrast,
        FilterOptions0.y * FilterOptions0.z);
    float pivot = max(minimumContrast, FilterOptions0.w);
    int style = (int)(FilterOptions1.x + 0.5);
    bool inverse = FilterOptions1.y > 0.5;

    if (style == 2)
    {
        float logPivot = max(
            0.0,
            log2(pivot / middleGray) *
                FilterOptions1.z +
                FilterOptions1.w);
        float exposureOffset = exposure * FilterOptions1.z;
        if (inverse)
        {
            float inverseOffset =
                logPivot -
                (logPivot / contrast) -
                exposureOffset;
            return (color / contrast) + inverseOffset;
        }

        float forwardOffset =
            ((exposureOffset - logPivot) * contrast) +
            logPivot;
        return (color * contrast) + forwardOffset;
    }

    float transferPower = style == 1 ? videoTransferPower : 1.0;
    float adjustedPivot = pow(pivot, transferPower);
    float exposureScale = pow(pow(2.0, exposure), transferPower);
    if (inverse)
    {
        if (contrast == 1.0)
        {
            return color / exposureScale;
        }
        return pow(
            max(color / adjustedPivot, 0.0),
            1.0 / contrast) *
            (adjustedPivot / exposureScale);
    }

    if (contrast == 1.0)
    {
        return color * exposureScale;
    }
    return pow(
        max(color * (exposureScale / adjustedPivot), 0.0),
        contrast) * adjustedPivot;
}
