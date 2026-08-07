#ifdef PRISM_DEINTERLACE_EFFECT
float CatalogDeinterlacePairCost(
    float4 first,
    float4 second)
{
    const float3 lumaWeights = float3(
        0.2126,
        0.7152,
        0.0722);
    return abs(
            dot(first.rgb, lumaWeights) -
            dot(second.rgb, lumaWeights)) +
        (0.25 * abs(first.a - second.a));
}

float4 CatalogDeinterlace(
    float2 uv,
    float4 source,
    int profile)
{
    float2 pixel = uv / PixelSize;
    float scanline = floor(pixel.y);
    float lineParity = fmod(scanline, 2.0);
    if (abs(lineParity - FilterOptions0.x) > 0.5)
    {
        return source;
    }

    float2 verticalStep = float2(0.0, PixelSize.y);
    bool hasTop = scanline >= 1.0;
    bool hasBottom =
        scanline + 1.0 < FilterTextureSize.y;
    if (FilterOptions1.x > 0.5)
    {
        if (hasTop)
        {
            return CatalogLinearSample(
                uv - verticalStep,
                profile);
        }
        return hasBottom
            ? CatalogLinearSample(
                uv + verticalStep,
                profile)
            : source;
    }
    if (!hasTop)
    {
        return hasBottom
            ? CatalogLinearSample(
                uv + verticalStep,
                profile)
            : source;
    }
    if (!hasBottom)
    {
        return CatalogLinearSample(
            uv - verticalStep,
            profile);
    }

    float4 topCenter = CatalogLinearSample(
        uv - verticalStep,
        profile);
    float4 bottomCenter = CatalogLinearSample(
        uv + verticalStep,
        profile);
    const float3 lumaWeights = float3(
        0.2126,
        0.7152,
        0.0722);
    int bestSlope = 0;
    float bestCost = 1e20;
    for (int slope = -3; slope <= 3; slope++)
    {
        float2 slopeStep =
            float2(slope * PixelSize.x, 0.0);
        float2 neighborStep =
            float2(PixelSize.x, 0.0);
        float4 topCandidate = CatalogLinearSample(
            uv - verticalStep - slopeStep,
            profile);
        float4 bottomCandidate = CatalogLinearSample(
            uv + verticalStep + slopeStep,
            profile);
        float cost =
            CatalogDeinterlacePairCost(
                topCandidate,
                bottomCandidate) +
            (0.5 * CatalogDeinterlacePairCost(
                CatalogLinearSample(
                    uv - verticalStep - slopeStep - neighborStep,
                    profile),
                CatalogLinearSample(
                    uv + verticalStep + slopeStep - neighborStep,
                    profile))) +
            (0.5 * CatalogDeinterlacePairCost(
                CatalogLinearSample(
                    uv - verticalStep - slopeStep + neighborStep,
                    profile),
                CatalogLinearSample(
                    uv + verticalStep + slopeStep + neighborStep,
                    profile))) +
            (0.02 * abs((float)slope));
        if (slope != 0)
        {
            cost -= 0.25 * (
                abs(
                    dot(topCandidate.rgb, lumaWeights) -
                    dot(topCenter.rgb, lumaWeights)) +
                abs(
                    dot(bottomCandidate.rgb, lumaWeights) -
                    dot(bottomCenter.rgb, lumaWeights)));
        }
        if (cost < bestCost)
        {
            bestCost = cost;
            bestSlope = slope;
        }
    }

    float2 bestStep =
        float2(bestSlope * PixelSize.x, 0.0);
    float4 nearTop = CatalogLinearSample(
        uv - verticalStep - bestStep,
        profile);
    float4 nearBottom = CatalogLinearSample(
        uv + verticalStep + bestStep,
        profile);
    float4 linearResult = (nearTop + nearBottom) * 0.5;
    if (scanline < 3.0 ||
        scanline + 3.0 >= FilterTextureSize.y)
    {
        return linearResult;
    }

    float4 farTop = CatalogLinearSample(
        uv - (3.0 * verticalStep) - (3.0 * bestStep),
        profile);
    float4 farBottom = CatalogLinearSample(
        uv + (3.0 * verticalStep) + (3.0 * bestStep),
        profile);
    float4 fourPoint =
        ((9.0 * (nearTop + nearBottom)) -
            farTop -
            farBottom) /
        16.0;
    float4 result = clamp(
        fourPoint,
        min(nearTop, nearBottom),
        max(nearTop, nearBottom));
    result.rgb = min(result.rgb, result.a);
    return result;
}
#endif
