


float4 SampleIncrementalGaussian(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float sigma)
{
    int count = max(1, min(sampleCount, 17));
    int halfTapCount = max(0, (count - 1) / 2);
    if (halfTapCount == 0 || sigma <= 0.0)
    {
        return SampleNeighborhood(uv, profile, edgeMode);
    }

    float2 tapStep = radius / halfTapCount;
    float stepSquared = dot(tapStep, tapStep);
    float ratio = exp(-stepSquared / (2.0 * sigma * sigma));
    float ratioStep = ratio * ratio;
    float weight = 1.0;
    float multiplier = ratio;
    float4 total = SampleNeighborhood(uv, profile, edgeMode);
    float totalWeight = 1.0;
    for (int tap = 1; tap <= 8; tap++)
    {
        if (tap <= halfTapCount)
        {
            weight *= multiplier;
            multiplier *= ratioStep;
            float2 offset = tap * tapStep * PixelSize;
            total += (
                SampleNeighborhood(uv + offset, profile, edgeMode) +
                SampleNeighborhood(uv - offset, profile, edgeMode)) * weight;
            totalWeight += weight * 2.0;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleOptimizedBilinearGaussian(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile)
{
    int count = max(1, min(sampleCount, 17));
    float halfTapCount = max(
        1.0,
        floor((count - 1.0) * 0.5));

    float centerWeight = 1.0;
    float4 total =
        SampleNeighborhood(uv, profile, edgeMode) * centerWeight;
    float totalWeight = centerWeight;
    float2 tapStep = radius / halfTapCount;
    for (int pairIndex = 0; pairIndex < 4; pairIndex++)
    {
        int firstTap = (pairIndex * 2) + 1;
        if (count > 1 && firstTap <= halfTapCount)
        {
            int secondTap = firstTap + 1;
            float firstPosition = firstTap / (float)halfTapCount;
            float firstWeight = exp(
                -3.125 * firstPosition * firstPosition);
            float secondWeight = 0.0;
            if (secondTap <= halfTapCount)
            {
                float secondPosition =
                    secondTap / (float)halfTapCount;
                secondWeight = exp(
                    -3.125 * secondPosition * secondPosition);
            }

            float pairWeight = firstWeight + secondWeight;
            float pairOffset = firstTap +
                (secondWeight / max(pairWeight, 0.000001));
            float2 offset =
                tapStep * pairOffset * PixelSize;
            total += (
                SampleNeighborhood(
                    uv + offset,
                    profile,
                    edgeMode) +
                SampleNeighborhood(
                    uv - offset,
                    profile,
                    edgeMode)) * pairWeight;
            totalWeight += pairWeight * 2.0;
        }
    }
    return total / max(totalWeight, 0.000001);
}
