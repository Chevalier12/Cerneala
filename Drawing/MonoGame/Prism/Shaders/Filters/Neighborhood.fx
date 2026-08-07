float2 NeighborhoodUnclampedUv(VertexShaderOutput input)
{
    return
        (input.TextureCoordinates * UvScale) +
        UvOffset;
}

float2 MirrorNeighborhoodUv(float2 uv)
{
    float2 phase = frac(uv * 0.5) * 2.0;
    return 1.0 - abs(phase - 1.0);
}

float4 SampleNeighborhood(
    float2 uv,
    int profile,
    int edgeMode)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    if (edgeMode == 2)
    {
        uv = frac(uv);
        inside = 1.0;
    }
    else if (edgeMode == 3)
    {
        uv = MirrorNeighborhoodUv(uv);
        inside = 1.0;
    }

    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    float4 sample = tex2D(
        SpriteTextureSampler,
        uv);
    if (edgeMode == 1)
    {
        sample *= inside;
    }
    return WorkingAssociatedToLinearSrgb(
        sample,
        profile);
}

#include "OptimizedBilinearGaussian.fx"

float4 SampleNeighborhoodResource(float2 uv)
{
    uv = clamp(
        uv,
        0.5 / max(FilterTextureSize, 1.0),
        1.0 - (0.5 / max(FilterTextureSize, 1.0)));
    return tex2D(
        SecondaryTextureSampler,
        uv);
}

float4 SampleNeighborhoodOriginal(
    float2 uv,
    int profile)
{
    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return WorkingAssociatedToLinearSrgb(
        tex2D(FilterAuxiliaryTextureSampler, uv),
        profile);
}

int NeighborhoodEdgeMode(int operation)
{
    if (operation == 1 ||
        operation == 2 ||
        operation == 3 ||
        operation == 4 ||
        operation == 21)
    {
        return (int)(FilterOptions0.z + 0.5);
    }
    if (operation == 6)
    {
        return (int)(FilterOptions0.w + 0.5);
    }
    if (operation == 8)
    {
        return (int)(FilterOptions0.y + 0.5);
    }
    if (operation == 9 || operation == 10)
    {
        return (int)(FilterOptions1.x + 0.5);
    }
    return 0;
}

float4 SampleNeighborhoodLine(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    bool gaussian)
{
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, sampleCount);
    [loop]
    for (int index = 0; index < count; index++)
    {
        float position = count <= 1
            ? 0.0
            : ((index / (count - 1.0)) * 2.0) - 1.0;
        float weight = gaussian
            ? exp(-3.125 * position * position)
            : 1.0;
        total += SampleNeighborhood(
            uv + (radius * PixelSize * position),
            profile,
            edgeMode) * weight;
        totalWeight += weight;
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodDisk(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float threshold,
    bool edgeAware)
{
    float4 center = SampleNeighborhood(
        uv,
        profile,
        edgeMode);
    float3 centerStraight = Unpremultiply(center);
    float4 total = center;
    float totalWeight = 1.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 1; index < 17; index++)
    {
        if (index < count)
        {
            float fraction = index / max(count - 1.0, 1.0);
            float angle = index * 2.39996323;
            float2 offset = float2(
                cos(angle),
                sin(angle)) *
                sqrt(fraction) *
                radius *
                PixelSize;
            float4 sample = SampleNeighborhood(
                uv + offset,
                profile,
                edgeMode);
            float difference = abs(
                dot(
                    Unpremultiply(sample) - centerStraight,
                    float3(0.2126, 0.7152, 0.0722)));
            float weight = !edgeAware ||
                difference <= threshold
                ? 1.0
                : 0.0;
            total += sample * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleSmartBilateral(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float rangeSigma)
{
    float4 center = SampleNeighborhood(uv, profile, edgeMode);
    float3 centerStraight = Unpremultiply(center);
    float spatialSigma = max(radius.x / 3.0, 0.000001);
    float inverseSpatialVariance = 0.5 / (spatialSigma * spatialSigma);
    float inverseRangeVariance = rangeSigma > 0.0
        ? 0.5 / (rangeSigma * rangeSigma)
        : 0.0;
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    int half = count / 2;
    float stepSize = half > 0 ? radius.x / half : 0.0;
    for (int sampleY = 0; sampleY < 17; sampleY++)
    {
        if (sampleY < count)
        {
            for (int sampleX = 0; sampleX < 17; sampleX++)
            {
                if (sampleX < count)
                {
                    float2 pixelOffset = float2(
                        sampleX - half,
                        sampleY - half) * stepSize;
                    float distanceSquared = dot(pixelOffset, pixelOffset);
                    if (distanceSquared <= radius.x * radius.x)
                    {
                        float4 sample = SampleNeighborhood(
                            uv + (pixelOffset * PixelSize),
                            profile,
                            edgeMode);
                        float3 colorDelta =
                            Unpremultiply(sample) - centerStraight;
                        float rangeDistanceSquared =
                            dot(colorDelta, colorDelta) / 3.0;
                        float rangeWeight = rangeSigma > 0.0
                            ? exp(-rangeDistanceSquared *
                                inverseRangeVariance)
                            : (rangeDistanceSquared <= 0.0000001
                                ? 1.0
                                : 0.0);
                        float spatialWeight = exp(
                            -distanceSquared * inverseSpatialVariance);
                        float weight = spatialWeight * rangeWeight;
                        total += sample * weight;
                        totalWeight += weight;
                    }
                }
            }
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleSurfaceBilateral(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float rangeSigma)
{
    float4 center = SampleNeighborhood(uv, profile, edgeMode);
    float centerLuminance = dot(
        Unpremultiply(center),
        float3(0.2126, 0.7152, 0.0722));
    float spatialSigma = max(radius.x / 3.0, 0.000001);
    float inverseSpatialVariance = 0.5 / (spatialSigma * spatialSigma);
    float inverseRangeVariance = rangeSigma > 0.0
        ? 0.5 / (rangeSigma * rangeSigma)
        : 0.0;
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    int half = count / 2;
    float stepSize = half > 0 ? radius.x / half : 0.0;
    for (int sampleY = 0; sampleY < 17; sampleY++)
    {
        if (sampleY < count)
        {
            for (int sampleX = 0; sampleX < 17; sampleX++)
            {
                if (sampleX < count)
                {
                    float2 pixelOffset = float2(
                        sampleX - half,
                        sampleY - half) * stepSize;
                    float distanceSquared = dot(pixelOffset, pixelOffset);
                    if (distanceSquared <= radius.x * radius.x)
                    {
                        float4 sample = SampleNeighborhood(
                            uv + (pixelOffset * PixelSize),
                            profile,
                            edgeMode);
                        float rangeDistance = dot(
                            Unpremultiply(sample),
                            float3(0.2126, 0.7152, 0.0722)) -
                            centerLuminance;
                        float rangeDistanceSquared =
                            rangeDistance * rangeDistance;
                        float rangeWeight = rangeSigma > 0.0
                            ? exp(-rangeDistanceSquared *
                                inverseRangeVariance)
                            : (rangeDistanceSquared <= 0.0000001
                                ? 1.0
                                : 0.0);
                        float spatialWeight = exp(
                            -distanceSquared * inverseSpatialVariance);
                        float weight = spatialWeight * rangeWeight;
                        total += sample * weight;
                        totalWeight += weight;
                    }
                }
            }
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleLensAperture(
    float2 uv,
    float radius,
    int sampleCount,
    int profile)
{
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    int blades = max(3, (int)(FilterOptions0.y + 0.5));
    float sector = 6.28318530718 / blades;
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float fraction = count <= 1
                ? 0.0
                : index / (count - 1.0);
            float angle = index * 2.39996323;
            float local = angle - FilterOptions0.w;
            local -= floor((local + sector * 0.5) / sector) * sector;
            float polygonRadius = cos(3.14159265359 / blades) /
                max(cos(local), 0.000001);
            float apertureRadius = lerp(
                polygonRadius,
                1.0,
                saturate(FilterOptions0.z));
            float2 offset = float2(cos(angle), sin(angle)) *
                sqrt(fraction) * apertureRadius * radius * PixelSize;
            float4 sample = SampleNeighborhood(uv + offset, profile, 0);
            float luminance = dot(
                Unpremultiply(sample),
                float3(0.2126, 0.7152, 0.0722));
            float boost = step(FilterOptions1.y, luminance) *
                max(FilterOptions1.x, 0.0);
            sample.rgb *= 1.0 + boost;
            total += sample;
            totalWeight += 1.0;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleFieldBlur(
    float2 uv,
    float2 radius,
    int sampleCount,
    int profile)
{
    float depth = saturate(SampleNeighborhoodResource(uv).r);
    if (FilterOptions0.y > 0.5)
    {
        depth = 1.0 - depth;
    }

    float focalDistance = saturate(FilterOptions0.x);
    float range = max(
        max(focalDistance, 1.0 - focalDistance),
        0.000001);
    float coc = saturate(abs(depth - focalDistance) / range);
    if (coc <= 0.000001)
    {
        return SampleNeighborhood(uv, profile, 0);
    }

    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    [loop]
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float fraction = count <= 1
                ? 0.0
                : index / (count - 1.0);
            float angle = index * 2.39996323;
            float2 offset = float2(cos(angle), sin(angle)) *
                sqrt(fraction) * radius * coc * PixelSize;
            float4 sample = SampleNeighborhood(
                uv + offset,
                profile,
                0);
            float luminance = dot(
                Unpremultiply(sample),
                float3(0.2126, 0.7152, 0.0722));
            float weight = 1.0 +
                (max(FilterOptions0.z, 0.0) * saturate(luminance));
            total += sample * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodAverage3x3(
    float2 uv,
    int profile)
{
    float4 total = 0.0;
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            total += SampleNeighborhood(
                uv + (float2(offsetX, offsetY) * PixelSize),
                profile,
                0);
        }
    }
    return total / 9.0;
}

float NeighborhoodHash(float2 position, float seed)
{
    float value = dot(
        floor(position),
        float2(127.1, 311.7));
    return frac(
        sin(value + (seed * 0.00006103515625)) *
        43758.5453123);
}

float AddNoiseUniform(
    uint2 position,
    uint seedLow,
    uint seedHigh,
    uint channel)
{
    uint input =
        (position.x * 0x9e3779b9u) ^
        (position.y * 0x85ebca6bu) ^
        seedLow ^
        (seedHigh * 0x27d4eb2du) ^
        (channel * 0xc2b2ae35u);
    uint state = (input * 747796405u) + 2891336453u;
    uint word =
        ((state >> ((state >> 28u) + 4u)) ^ state) *
        277803737u;
    uint value = (word >> 22u) ^ word;
    return (value + 0.5) / 4294967296.0;
}

float AddNoiseSample(
    uint2 position,
    uint seedLow,
    uint seedHigh,
    uint channel,
    bool gaussian)
{
    if (!gaussian)
    {
        return (
            AddNoiseUniform(
                position,
                seedLow,
                seedHigh,
                channel) *
            2.0) - 1.0;
    }

    uint pair = channel >> 1u;
    float first = max(
        AddNoiseUniform(
            position,
            seedLow,
            seedHigh,
            pair * 2u),
        1.0 / 4294967296.0);
    float second = AddNoiseUniform(
        position,
        seedLow,
        seedHigh,
        (pair * 2u) + 1u);
    float radius = sqrt(-2.0 * log(first));
    float angle = 6.28318530718 * second;
    return radius * ((channel & 1u) == 0u
        ? cos(angle)
        : sin(angle));
}

void MedianCompareExchange(
    inout float4 left,
    inout float leftRank,
    inout float4 right,
    inout float rightRank)
{
    bool exchange = rightRank < leftRank;
    float4 originalLeft = left;
    float originalLeftRank = leftRank;
    left = exchange ? right : left;
    leftRank = exchange ? rightRank : leftRank;
    right = exchange ? originalLeft : right;
    rightRank = exchange ? originalLeftRank : rightRank;
}

float4 NeighborhoodMedian3x3(
    float2 uv,
    int profile)
{
    float4 values[9];
    values[0] = SampleNeighborhood(
        uv + (float2(-1.0, -1.0) * PixelSize),
        profile,
        0);
    values[1] = SampleNeighborhood(
        uv + (float2(0.0, -1.0) * PixelSize),
        profile,
        0);
    values[2] = SampleNeighborhood(
        uv + (float2(1.0, -1.0) * PixelSize),
        profile,
        0);
    values[3] = SampleNeighborhood(
        uv + (float2(-1.0, 0.0) * PixelSize),
        profile,
        0);
    values[4] = SampleNeighborhood(uv, profile, 0);
    values[5] = SampleNeighborhood(
        uv + (float2(1.0, 0.0) * PixelSize),
        profile,
        0);
    values[6] = SampleNeighborhood(
        uv + (float2(-1.0, 1.0) * PixelSize),
        profile,
        0);
    values[7] = SampleNeighborhood(
        uv + (float2(0.0, 1.0) * PixelSize),
        profile,
        0);
    values[8] = SampleNeighborhood(
        uv + (float2(1.0, 1.0) * PixelSize),
        profile,
        0);

    float3 luminanceWeights =
        float3(0.2126, 0.7152, 0.0722);
    float ranks[9];
    ranks[0] = dot(Unpremultiply(values[0]), luminanceWeights);
    ranks[1] = dot(Unpremultiply(values[1]), luminanceWeights);
    ranks[2] = dot(Unpremultiply(values[2]), luminanceWeights);
    ranks[3] = dot(Unpremultiply(values[3]), luminanceWeights);
    ranks[4] = dot(Unpremultiply(values[4]), luminanceWeights);
    ranks[5] = dot(Unpremultiply(values[5]), luminanceWeights);
    ranks[6] = dot(Unpremultiply(values[6]), luminanceWeights);
    ranks[7] = dot(Unpremultiply(values[7]), luminanceWeights);
    ranks[8] = dot(Unpremultiply(values[8]), luminanceWeights);

    MedianCompareExchange(values[0], ranks[0], values[1], ranks[1]);
    MedianCompareExchange(values[2], ranks[2], values[3], ranks[3]);
    MedianCompareExchange(values[4], ranks[4], values[5], ranks[5]);
    MedianCompareExchange(values[6], ranks[6], values[7], ranks[7]);

    MedianCompareExchange(values[1], ranks[1], values[2], ranks[2]);
    MedianCompareExchange(values[3], ranks[3], values[4], ranks[4]);
    MedianCompareExchange(values[5], ranks[5], values[6], ranks[6]);
    MedianCompareExchange(values[7], ranks[7], values[8], ranks[8]);

    MedianCompareExchange(values[0], ranks[0], values[1], ranks[1]);
    MedianCompareExchange(values[2], ranks[2], values[3], ranks[3]);
    MedianCompareExchange(values[4], ranks[4], values[5], ranks[5]);
    MedianCompareExchange(values[6], ranks[6], values[7], ranks[7]);

    MedianCompareExchange(values[1], ranks[1], values[2], ranks[2]);
    MedianCompareExchange(values[3], ranks[3], values[4], ranks[4]);
    MedianCompareExchange(values[5], ranks[5], values[6], ranks[6]);
    MedianCompareExchange(values[7], ranks[7], values[8], ranks[8]);

    MedianCompareExchange(values[0], ranks[0], values[1], ranks[1]);
    MedianCompareExchange(values[2], ranks[2], values[3], ranks[3]);
    MedianCompareExchange(values[4], ranks[4], values[5], ranks[5]);
    MedianCompareExchange(values[6], ranks[6], values[7], ranks[7]);

    MedianCompareExchange(values[1], ranks[1], values[2], ranks[2]);
    MedianCompareExchange(values[3], ranks[3], values[4], ranks[4]);
    MedianCompareExchange(values[5], ranks[5], values[6], ranks[6]);
    MedianCompareExchange(values[7], ranks[7], values[8], ranks[8]);

    MedianCompareExchange(values[0], ranks[0], values[1], ranks[1]);
    MedianCompareExchange(values[2], ranks[2], values[3], ranks[3]);
    MedianCompareExchange(values[4], ranks[4], values[5], ranks[5]);
    MedianCompareExchange(values[6], ranks[6], values[7], ranks[7]);

    MedianCompareExchange(values[1], ranks[1], values[2], ranks[2]);
    MedianCompareExchange(values[3], ranks[3], values[4], ranks[4]);
    MedianCompareExchange(values[5], ranks[5], values[6], ranks[6]);
    MedianCompareExchange(values[7], ranks[7], values[8], ranks[8]);

    MedianCompareExchange(values[0], ranks[0], values[1], ranks[1]);
    MedianCompareExchange(values[2], ranks[2], values[3], ranks[3]);
    MedianCompareExchange(values[4], ranks[4], values[5], ranks[5]);
    MedianCompareExchange(values[6], ranks[6], values[7], ranks[7]);
    return values[4];
}

void NeighborhoodMedianStats(
    float2 uv,
    int profile,
    int radius,
    out float4 median,
    out float minimum,
    out float maximum)
{
    float4 values[49];
    int count = 0;
    [loop]
    for (int offsetY = -3; offsetY <= 3; offsetY++)
    {
        [loop]
        for (int offsetX = -3; offsetX <= 3; offsetX++)
        {
            if (abs(offsetX) <= radius &&
                abs(offsetY) <= radius)
            {
                values[count++] = SampleNeighborhood(
                    uv + (float2(offsetX, offsetY) * PixelSize),
                    profile,
                    0);
            }
        }
    }

    [loop]
    for (int outer = 0; outer < 48; outer++)
    {
        if (outer < count - 1)
        {
            [loop]
            for (int inner = outer + 1; inner < 49; inner++)
            {
                if (inner < count)
                {
                    float outerValue = dot(
                        Unpremultiply(values[outer]),
                        float3(0.2126, 0.7152, 0.0722));
                    float innerValue = dot(
                        Unpremultiply(values[inner]),
                        float3(0.2126, 0.7152, 0.0722));
                    if (innerValue < outerValue)
                    {
                        float4 swap = values[outer];
                        values[outer] = values[inner];
                        values[inner] = swap;
                    }
                }
            }
        }
    }

    median = values[count / 2];
    minimum = dot(
        Unpremultiply(values[0]),
        float3(0.2126, 0.7152, 0.0722));
    maximum = dot(
        Unpremultiply(values[count - 1]),
        float3(0.2126, 0.7152, 0.0722));
}

float4 NeighborhoodAdaptiveThresholdedMedian(
    float2 uv,
    int profile,
    int maximumRadius,
    float threshold)
{
    float4 center = SampleNeighborhood(uv, profile, 0);
    float centerLuminance = dot(
        Unpremultiply(center),
        float3(0.2126, 0.7152, 0.0722));
    float4 fallback = center;
    int boundedRadius = min(3, max(1, maximumRadius));
    [loop]
    for (int radius = 1; radius <= 3; radius++)
    {
        if (radius <= boundedRadius)
        {
            float minimum;
            float maximum;
            float4 median;
            NeighborhoodMedianStats(
                uv,
                profile,
                radius,
                median,
                minimum,
                maximum);
            fallback = median;
            float medianLuminance = dot(
                Unpremultiply(median),
                float3(0.2126, 0.7152, 0.0722));
            if (medianLuminance > minimum &&
                medianLuminance < maximum)
            {
                bool centerIsImpulse =
                    centerLuminance <= minimum ||
                    centerLuminance >= maximum;
                if (centerIsImpulse &&
                    abs(centerLuminance - medianLuminance) > threshold)
                {
                    return float4(
                        saturate(Unpremultiply(median)) * center.a,
                        center.a);
                }
                return center;
            }
        }
    }

    float fallbackLuminance = dot(
        Unpremultiply(fallback),
        float3(0.2126, 0.7152, 0.0722));
    return abs(centerLuminance - fallbackLuminance) > threshold
        ? float4(
            saturate(Unpremultiply(fallback)) * center.a,
            center.a)
        : center;
}

float2 DespeckleKernelOffset(int index, float radius)
{
    float2 offset = 0.0;
    if (index == 1) offset = float2(-1.0, -1.0);
    else if (index == 2) offset = float2(0.0, -1.0);
    else if (index == 3) offset = float2(1.0, -1.0);
    else if (index == 4) offset = float2(-1.0, 0.0);
    else if (index == 5) offset = float2(1.0, 0.0);
    else if (index == 6) offset = float2(-1.0, 1.0);
    else if (index == 7) offset = float2(0.0, 1.0);
    else if (index == 8) offset = float2(1.0, 1.0);
    else if (index == 9) offset = float2(0.0, -2.0);
    else if (index == 10) offset = float2(2.0, 0.0);
    else if (index == 11) offset = float2(0.0, 2.0);
    else if (index == 12) offset = float2(-2.0, 0.0);
    else if (index == 13) offset = float2(-1.0, -2.0);
    else if (index == 14) offset = float2(1.0, -2.0);
    else if (index == 15) offset = float2(2.0, -1.0);
    else if (index == 16) offset = float2(2.0, 1.0);
    else if (index == 17) offset = float2(1.0, 2.0);
    else if (index == 18) offset = float2(-1.0, 2.0);
    else if (index == 19) offset = float2(-2.0, 1.0);
    else if (index == 20) offset = float2(-2.0, -1.0);

    float scale = radius <= 1.5 ? 1.0 : radius * 0.5;
    return round(offset * scale);
}

float4 SampleDespeckleState(float2 uv)
{
    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return tex2D(SpriteTextureSampler, uv);
}

float3 SampleDespeckleOriginalStraight(
    float2 uv,
    int profile)
{
    return saturate(Unpremultiply(
        SampleNeighborhoodOriginal(uv, profile)));
}

float3 DespeckleMedianStraight(
    float2 uv,
    float radius,
    int profile,
    bool encoded,
    bool goodOnly,
    bool useOriginalColor,
    out bool found)
{
    float3 values[21];
    float luminances[21];
    int valueCount = 0;
    int kernelCount = radius <= 1.5 ? 9 : 21;
    for (int index = 0; index < 21; index++)
    {
        values[index] = 0.0;
        luminances[index] = 0.0;
        if (index < kernelCount)
        {
            float2 sampleUv = uv +
                (DespeckleKernelOffset(index, radius) * PixelSize);
            float4 state = encoded
                ? SampleDespeckleState(sampleUv)
                : SampleNeighborhood(sampleUv, profile, 0);
            if (!goodOnly || state.a < 0.5)
            {
                float3 straight = useOriginalColor
                    ? SampleDespeckleOriginalStraight(sampleUv, profile)
                    : (encoded
                        ? saturate(state.rgb)
                        : saturate(Unpremultiply(state)));
                values[valueCount] = straight;
                luminances[valueCount] = dot(
                    straight,
                    float3(0.2126, 0.7152, 0.0722));
                valueCount++;
            }
        }
    }

    for (int outer = 0; outer < 20; outer++)
    {
        for (int inner = outer + 1; inner < 21; inner++)
        {
            if (inner < valueCount &&
                luminances[inner] < luminances[outer])
            {
                float luminanceSwap = luminances[outer];
                luminances[outer] = luminances[inner];
                luminances[inner] = luminanceSwap;
                float3 valueSwap = values[outer];
                values[outer] = values[inner];
                values[inner] = valueSwap;
            }
        }
    }

    found = valueCount > 0;
    int medianIndex = max(0, (valueCount - 1) / 2);
    return found ? values[medianIndex] : 0.0;
}

float4 ApplyDespeckleState(
    float2 uv,
    int profile,
    int passKind,
    int iteration)
{
    float radius = max(FilterOptions9.x, 0.0);
    float threshold = max(FilterOptions0.x, 0.0);
    if (radius <= 0.0)
    {
        if (passKind == 8 && iteration == 0)
        {
            return float4(
                saturate(Unpremultiply(
                    SampleNeighborhood(uv, profile, 0))),
                0.0);
        }
        float4 state = SampleDespeckleState(uv);
        return float4(saturate(state.rgb), 0.0);
    }
    if (passKind == 8)
    {
        bool encoded = iteration > 0;
        float4 center = encoded
            ? SampleDespeckleState(uv)
            : SampleNeighborhood(uv, profile, 0);
        float3 centerStraight = encoded
            ? saturate(center.rgb)
            : saturate(Unpremultiply(center));
        bool found = false;
        float3 median = DespeckleMedianStraight(
            uv,
            radius,
            profile,
            encoded,
            false,
            false,
            found);
        bool detected = found && abs(dot(
            centerStraight - median,
            float3(0.2126, 0.7152, 0.0722))) > threshold;
        float flag = max(encoded ? center.a : 0.0, detected ? 1.0 : 0.0);
        return float4(detected ? median : centerStraight, flag);
    }

    bool firstFilteringPass = iteration == 0;
    float4 centerState = SampleDespeckleState(uv);
    float3 centerStraight = firstFilteringPass
        ? SampleDespeckleOriginalStraight(uv, profile)
        : saturate(centerState.rgb);
    if (centerState.a < 0.5)
    {
        return float4(centerStraight, 0.0);
    }

    bool foundGood = false;
    float3 goodMedian = DespeckleMedianStraight(
        uv,
        radius,
        profile,
        true,
        true,
        firstFilteringPass,
        foundGood);
    return foundGood
        ? float4(goodMedian, 0.0)
        : float4(centerStraight, 1.0);
}

float4 DecodeDespeckle(
    float2 uv,
    int profile)
{
    float4 state = SampleDespeckleState(uv);
    float3 straight = saturate(state.rgb);
    if (state.a >= 0.5)
    {
        bool found = false;
        straight = DespeckleMedianStraight(
            uv,
            max(FilterOptions9.x, 0.0),
            profile,
            true,
            false,
            false,
            found);
        if (!found)
        {
            straight = SampleDespeckleOriginalStraight(uv, profile);
        }
    }
    float alpha = SampleNeighborhoodOriginal(uv, profile).a;
    return float4(straight * alpha, alpha);
}

float4 NeighborhoodSharpen(
    float4 center,
    float4 blurred,
    float amount,
    float threshold)
{
    float difference = abs(
        dot(
            Unpremultiply(center) -
                Unpremultiply(blurred),
            float3(0.2126, 0.7152, 0.0722)));
    return difference < threshold
        ? center
        : center + ((center - blurred) * amount);
}

float4 NeighborhoodUnsharpHighBoost(
    float4 original,
    float4 blurred,
    float amount,
    float threshold)
{
    if (original.a <= 0.000001)
    {
        return original;
    }

    float3 originalStraight =
        saturate(Unpremultiply(original));
    float3 detail =
        originalStraight - Unpremultiply(blurred);
    float difference = abs(
        dot(detail, float3(0.2126, 0.7152, 0.0722)));
    float thresholdCenter = saturate(threshold);
    float knee = max(thresholdCenter * 0.5, 1.0 / 255.0);
    float gate = smoothstep(
        max(0.0, thresholdCenter - knee),
        min(1.0, thresholdCenter + knee),
        difference);
    float3 straight = saturate(
        originalStraight + (detail * amount * gate));
    return float4(straight * original.a, original.a);
}

float3 NeighborhoodSharpenStraight(
    float4 sample,
    float3 fallback)
{
    return sample.a > 0.000001
        ? saturate(sample.rgb / sample.a)
        : fallback;
}

float4 NeighborhoodBinomialHighBoost(
    float2 uv,
    float4 center,
    float amount,
    int profile)
{
    if (center.a <= 0.000001)
    {
        return center;
    }

    float3 centerStraight = NeighborhoodSharpenStraight(
        center,
        0.0);
    float3 blurred =
        NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + (float2(-1.0, -1.0) * PixelSize),
                profile,
                0),
            centerStraight) +
        (NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + float2(0.0, -PixelSize.y),
                profile,
                0),
            centerStraight) * 2.0) +
        NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + (float2(1.0, -1.0) * PixelSize),
                profile,
                0),
            centerStraight) +
        (NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + float2(-PixelSize.x, 0.0),
                profile,
                0),
            centerStraight) * 2.0) +
        (centerStraight * 4.0) +
        (NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + float2(PixelSize.x, 0.0),
                profile,
                0),
            centerStraight) * 2.0) +
        NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + (float2(-1.0, 1.0) * PixelSize),
                profile,
                0),
            centerStraight) +
        (NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + float2(0.0, PixelSize.y),
                profile,
                0),
            centerStraight) * 2.0) +
        NeighborhoodSharpenStraight(
            SampleNeighborhood(
                uv + (float2(1.0, 1.0) * PixelSize),
                profile,
                0),
            centerStraight);
    blurred /= 16.0;
    float strength = saturate(amount) * 2.0;
    float3 straight = saturate(
        centerStraight + ((centerStraight - blurred) * strength));
    return float4(straight * center.a, center.a);
}

float3 NeighborhoodContrastAdaptiveSharpenStraight(
    float3 center,
    float3 north,
    float3 west,
    float3 east,
    float3 south,
    float amount)
{
    float3 minimumValue = min(
        min(min(north, west), center),
        min(east, south));
    float3 maximumValue = max(
        max(max(north, west), center),
        max(east, south));
    float3 amplitude = sqrt(
        saturate(
            min(minimumValue, 1.0 - maximumValue) /
            max(maximumValue, 0.000001)));
    float strength = saturate(amount);
    float peak = -strength / lerp(8.0, 5.0, strength);
    float3 weight = amplitude * peak;
    return saturate(
        (center +
            ((north + west + east + south) * weight)) /
        (1.0 + (4.0 * weight)));
}

float4 NeighborhoodContrastAdaptiveSharpen(
    float2 uv,
    float4 center,
    float amount,
    int profile)
{
    if (center.a <= 0.000001)
    {
        return center;
    }

    float3 centerStraight = saturate(Unpremultiply(center));
    float3 north = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(0.0, -PixelSize.y),
            profile,
            0),
        centerStraight);
    float3 west = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(-PixelSize.x, 0.0),
            profile,
            0),
        centerStraight);
    float3 east = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(PixelSize.x, 0.0),
            profile,
            0),
        centerStraight);
    float3 south = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(0.0, PixelSize.y),
            profile,
            0),
        centerStraight);

    float3 straight = NeighborhoodContrastAdaptiveSharpenStraight(
        centerStraight,
        north,
        west,
        east,
        south,
        amount);
    return float4(straight * center.a, center.a);
}

float4 NeighborhoodSobelGatedContrastAdaptiveSharpen(
    float2 uv,
    float4 center,
    float amount,
    float threshold,
    int profile)
{
    if (center.a <= 0.000001)
    {
        return center;
    }

    float3 centerStraight = saturate(Unpremultiply(center));
    float3 northWest = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + (float2(-1.0, -1.0) * PixelSize),
            profile,
            0),
        centerStraight);
    float3 north = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(0.0, -PixelSize.y),
            profile,
            0),
        centerStraight);
    float3 northEast = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + (float2(1.0, -1.0) * PixelSize),
            profile,
            0),
        centerStraight);
    float3 west = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(-PixelSize.x, 0.0),
            profile,
            0),
        centerStraight);
    float3 east = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(PixelSize.x, 0.0),
            profile,
            0),
        centerStraight);
    float3 southWest = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + (float2(-1.0, 1.0) * PixelSize),
            profile,
            0),
        centerStraight);
    float3 south = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + float2(0.0, PixelSize.y),
            profile,
            0),
        centerStraight);
    float3 southEast = NeighborhoodSharpenStraight(
        SampleNeighborhood(
            uv + (float2(1.0, 1.0) * PixelSize),
            profile,
            0),
        centerStraight);

    const float3 luminanceWeights = float3(0.2126, 0.7152, 0.0722);
    float northWestLuma = dot(northWest, luminanceWeights);
    float northLuma = dot(north, luminanceWeights);
    float northEastLuma = dot(northEast, luminanceWeights);
    float westLuma = dot(west, luminanceWeights);
    float eastLuma = dot(east, luminanceWeights);
    float southWestLuma = dot(southWest, luminanceWeights);
    float southLuma = dot(south, luminanceWeights);
    float southEastLuma = dot(southEast, luminanceWeights);
    float gradientX =
        (northEastLuma + (2.0 * eastLuma) + southEastLuma) -
        (northWestLuma + (2.0 * westLuma) + southWestLuma);
    float gradientY =
        (southWestLuma + (2.0 * southLuma) + southEastLuma) -
        (northWestLuma + (2.0 * northLuma) + northEastLuma);
    float edgeMagnitude = saturate(
        length(float2(gradientX, gradientY)) * 0.25);

    float edgeThreshold = saturate(threshold);
    float knee = max(edgeThreshold * 0.5, 1.0 / 255.0);
    float gate = smoothstep(
        max(0.0, edgeThreshold - knee),
        min(1.0, edgeThreshold + knee),
        edgeMagnitude);
    float3 sharpened = NeighborhoodContrastAdaptiveSharpenStraight(
        centerStraight,
        north,
        west,
        east,
        south,
        amount);
    float3 straight = lerp(centerStraight, sharpened, gate);
    return float4(straight * center.a, center.a);
}

float4 SampleShapePsf(
    float2 uv,
    float radius,
    int sampleCount,
    int edgeMode,
    int profile)
{
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    for (int kernelY = 0; kernelY < 17; kernelY++)
    {
        if (kernelY < count)
        {
            float v = (kernelY + 0.5) / count;
            float offsetY = count <= 1
                ? 0.0
                : ((kernelY / (count - 1.0)) * 2.0) - 1.0;
            for (int kernelX = 0; kernelX < 17; kernelX++)
            {
                if (kernelX < count)
                {
                    float u = (kernelX + 0.5) / count;
                    float offsetX = count <= 1
                        ? 0.0
                        : ((kernelX / (count - 1.0)) * 2.0) - 1.0;
                    float weight = max(
                        SampleNeighborhoodResource(float2(u, v)).a,
                        0.0);
                    total += SampleNeighborhood(
                        uv - (float2(offsetX, offsetY) * radius * PixelSize),
                        profile,
                        edgeMode) * weight;
                    totalWeight += weight;
                }
            }
        }
    }
    return totalWeight > 0.000001
        ? total / totalWeight
        : SampleNeighborhood(uv, profile, edgeMode);
}

bool PathDerivativeFromField(
    float4 field,
    int intervalCount,
    float directionSign,
    out float2 derivative,
    out float2 tangent,
    out float validity)
{
    validity = saturate(field.a);
    float2 direction = (field.rg * 2.0) - 1.0;
    float directionLength = length(direction);
    if (validity <= 0.000001 || directionLength <= 0.000001)
    {
        derivative = 0.0;
        tangent = 0.0;
        return false;
    }

    direction /= directionLength;
    float speed = lerp(
        FilterOptions0.x,
        FilterOptions0.w,
        saturate(field.b));
    tangent = direction * directionSign;
    derivative =
        tangent *
        PixelSize *
        (speed / max(intervalCount, 1));
    return true;
}

bool PathDerivative(
    float2 position,
    int intervalCount,
    float directionSign,
    out float2 derivative,
    out float2 tangent,
    out float validity)
{
    return PathDerivativeFromField(
        SampleNeighborhoodResource(position),
        intervalCount,
        directionSign,
        derivative,
        tangent,
        validity);
}

bool PathRk4Step(
    float2 position,
    float4 initialField,
    bool useInitialField,
    int intervalCount,
    float directionSign,
    out float2 next,
    out float2 tangent,
    out float validity,
    out float stepLength)
{
    float2 k1;
    float2 k2;
    float2 k3;
    float2 k4;
    float2 ignoredTangent;
    float ignoredValidity;
    next = position;
    tangent = 0.0;
    validity = 0.0;
    stepLength = 0.0;
    bool firstValid = useInitialField
        ? PathDerivativeFromField(
            initialField,
            intervalCount,
            directionSign,
            k1,
            ignoredTangent,
            ignoredValidity)
        : PathDerivative(
            position,
            intervalCount,
            directionSign,
            k1,
            ignoredTangent,
            ignoredValidity);
    if (!firstValid)
    {
        return false;
    }
    if (!PathDerivative(
        position + (k1 * 0.5),
        intervalCount,
        directionSign,
        k2,
        ignoredTangent,
        ignoredValidity))
    {
        return false;
    }
    if (!PathDerivative(
        position + (k2 * 0.5),
        intervalCount,
        directionSign,
        k3,
        ignoredTangent,
        ignoredValidity))
    {
        return false;
    }
    if (!PathDerivative(
        position + k3,
        intervalCount,
        directionSign,
        k4,
        tangent,
        validity))
    {
        return false;
    }

    float2 displacement =
        (k1 + (k2 * 2.0) + (k3 * 2.0) + k4) / 6.0;
    next = position + displacement;
    stepLength = length(displacement / PixelSize);
    return true;
}

float PathProfileWeight(float distanceFraction)
{
    int shape = (int)(FilterOptions1.x + 0.5);
    if (shape == 0)
    {
        return 1.0;
    }
    return 1.0 -
        (saturate(FilterOptions0.y) * saturate(distanceFraction));
}

void AccumulatePathDirection(
    float2 origin,
    float4 originField,
    float2 noisePosition,
    int stepCount,
    int intervalCount,
    float directionSign,
    int profile,
    inout float4 total,
    inout float totalWeight)
{
    float2 position = origin;
    bool active = true;
    for (int stepIndex = 1; stepIndex < 17; stepIndex++)
    {
        if (stepIndex <= stepCount && active)
        {
            float2 next;
            float2 tangent;
            float validity;
            float stepLength;
            active = PathRk4Step(
                position,
                originField,
                stepIndex == 1,
                intervalCount,
                directionSign,
                next,
                tangent,
                validity,
                stepLength);
            if (active)
            {
                position = next;
                float2 hashPosition = noisePosition + float2(
                    stepIndex * 19.0,
                    directionSign < 0.0 ? 37.0 : 73.0);
                float jitter =
                    ((NeighborhoodHash(hashPosition, 2791.0) * 2.0) - 1.0) *
                    saturate(abs(FilterOptions1.z)) *
                    stepLength *
                    0.5;
                float2 samplePosition =
                    position + (tangent * PixelSize * jitter);
                float profileWeight = PathProfileWeight(
                    stepIndex / max(stepCount, 1.0));
                float weight = validity * profileWeight;
                total += SampleNeighborhood(
                    samplePosition,
                    profile,
                    0) * weight;
                totalWeight += weight;
            }
        }
    }
}

float4 SamplePathRk4(
    float2 uv,
    float2 noisePosition,
    int sampleCount,
    int profile)
{
    int count = max(1, min(sampleCount, 17));
    int intervalCount = count - 1;
    float4 center = SampleNeighborhood(uv, profile, 0);
    float4 centerField = SampleNeighborhoodResource(uv);
    float centerWeight = saturate(centerField.a);
    if (intervalCount == 0 || centerWeight <= 0.000001)
    {
        return center;
    }

    bool centered = FilterOptions0.z > 0.5;
    int flashSync = centered
        ? 1
        : (int)(FilterOptions1.y + 0.5);
    int backwardSteps = flashSync == 0
        ? intervalCount
        : (flashSync == 1 ? intervalCount / 2 : 0);
    int forwardSteps = intervalCount - backwardSteps;
    float4 total = center * centerWeight;
    float totalWeight = centerWeight;
    AccumulatePathDirection(
        uv,
        centerField,
        noisePosition,
        backwardSteps,
        intervalCount,
        -1.0,
        profile,
        total,
        totalWeight);
    AccumulatePathDirection(
        uv,
        centerField,
        noisePosition,
        forwardSteps,
        intervalCount,
        1.0,
        profile,
        total,
        totalWeight);
    return total / max(totalWeight, 0.000001);
}

int SpinSampleCount(
    float arcLength,
    int maximumSamples)
{
    if (arcLength <= 0.000001)
    {
        return 1;
    }

    int maximumIntervals = max(2, maximumSamples - 1);
    maximumIntervals = (maximumIntervals / 2) * 2;
    int intervals = arcLength >= maximumIntervals
        ? maximumIntervals
        : max(2, (int)ceil(arcLength));
    intervals = ((intervals + 1) / 2) * 2;
    return min(intervals, maximumIntervals) + 1;
}

float SpinStrobeWeight(
    float position,
    float strength,
    int flashes,
    float duration)
{
    if (strength <= 0.0 || flashes <= 0)
    {
        return 1.0;
    }

    float phase = frac((position * flashes) + 0.5);
    float pulse = step(
        abs(phase - 0.5),
        duration * 0.5);
    return lerp(1.0, pulse, strength);
}

float4 SampleSpinBlur(
    float2 uv,
    float2 noisePosition,
    int maximumSamples,
    int profile)
{
    float4 centerSample = SampleNeighborhood(uv, profile, 0);
    float2 centerUv = FilterOptions0.xy;
    float2 delta = uv - centerUv;
    float2 normalized =
        delta / max(FilterOptions0.zw, 0.000001);
    float distance = length(normalized);
    float feather = saturate(FilterOptions1.y);
    float mask = feather <= 0.000001
        ? step(distance, 1.0)
        : smoothstep(
            0.0,
            1.0,
            saturate((1.0 - distance) / feather));
    if (mask <= 0.000001)
    {
        return centerSample;
    }

    float2 pixelDelta = delta / PixelSize;
    float rotation = FilterOptions1.x;
    int count = SpinSampleCount(
        abs(rotation) * length(pixelDelta),
        maximumSamples);
    if (count <= 1)
    {
        return centerSample;
    }

    int intervals = count - 1;
    float angleStep = rotation / intervals;
    float startAngle = rotation * -0.5;
    float startCosine = cos(startAngle);
    float startSine = sin(startAngle);
    float2 rotatedDelta = float2(
        (pixelDelta.x * startCosine) -
            (pixelDelta.y * startSine),
        (pixelDelta.x * startSine) +
            (pixelDelta.y * startCosine));
    float stepCosine = cos(angleStep);
    float stepSine = sin(angleStep);
    float noise = saturate(FilterOptions2.y);
    float strobeStrength = saturate(FilterOptions1.z);
    int strobeFlashes = max(
        0,
        (int)(FilterOptions1.w + 0.5));
    float strobeDuration = saturate(FilterOptions2.x);
    float4 total = 0.0;
    float totalWeight = 0.0;
    [loop]
    for (int index = 0; index < 65; index++)
    {
        if (index < count)
        {
            float2 sampleDelta = rotatedDelta;
            if (noise > 0.0 &&
                index > 0 &&
                index < intervals &&
                index != intervals / 2)
            {
                float jitter = (
                    NeighborhoodHash(
                        noisePosition + float2(
                            index * 19.0,
                            index * 47.0),
                        51729.0) *
                    2.0 - 1.0) *
                    noise *
                    angleStep *
                    0.5;
                float jitterCosine = cos(jitter);
                float jitterSine = sin(jitter);
                sampleDelta = float2(
                    (sampleDelta.x * jitterCosine) -
                        (sampleDelta.y * jitterSine),
                    (sampleDelta.x * jitterSine) +
                        (sampleDelta.y * jitterCosine));
            }

            float position = index / (float)intervals;
            float weight = SpinStrobeWeight(
                position,
                strobeStrength,
                strobeFlashes,
                strobeDuration);
            total += SampleNeighborhood(
                centerUv + (sampleDelta * PixelSize),
                profile,
                0) * weight;
            totalWeight += weight;
            rotatedDelta = float2(
                (rotatedDelta.x * stepCosine) -
                    (rotatedDelta.y * stepSine),
                (rotatedDelta.x * stepSine) +
                    (rotatedDelta.y * stepCosine));
        }
    }

    float4 blurred = totalWeight > 0.000001
        ? total / totalWeight
        : centerSample;
    return lerp(centerSample, blurred, mask);
}

float3 DecodeRichardsonLucyCorrection(float3 encoded)
{
    return min(
        encoded / max(1.0 - encoded, 1.0 / 255.0),
        16.0);
}

float4 EncodeRichardsonLucyCorrection(float3 correction)
{
    correction = clamp(correction, 0.0, 16.0);
    return float4(
        correction / (1.0 + correction),
        1.0);
}

float3 SampleRichardsonLucyEncoded(float2 uv)
{
    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return DecodeRichardsonLucyCorrection(
        tex2D(SpriteTextureSampler, uv).rgb);
}

float4 SampleRichardsonLucyPsf(
    float2 uv,
    int sampleCount,
    int profile,
    bool correction)
{
    int count = max(1, min(sampleCount, 17));
    float radius = max(FilterOptions0.y, 0.0);
    int remove = (int)(FilterOptions0.w + 0.5);
    float3 total = 0.0;
    float totalWeight = 0.0;
    if (remove == 2)
    {
        float2 direction = float2(
            cos(FilterOptions1.x),
            -sin(FilterOptions1.x));
        for (int index = 0; index < 17; index++)
        {
            if (index < count)
            {
                float position = count <= 1
                    ? 0.0
                    : ((index / (count - 1.0)) * 2.0) - 1.0;
                float2 sampleUv =
                    uv + (direction * position * radius * PixelSize);
                total += correction
                    ? SampleRichardsonLucyEncoded(sampleUv)
                    : Unpremultiply(
                        SampleNeighborhood(sampleUv, profile, 0));
                totalWeight += 1.0;
            }
        }
    }
    else
    {
        int half = count / 2;
        float stepSize = half > 0 ? radius / half : 0.0;
        float sigma = max(radius / 3.0, 0.000001);
        float inverseVariance = 0.5 / (sigma * sigma);
        for (int sampleY = 0; sampleY < 17; sampleY++)
        {
            if (sampleY < count)
            {
                for (int sampleX = 0; sampleX < 17; sampleX++)
                {
                    if (sampleX < count)
                    {
                        float2 offset = float2(
                            sampleX - half,
                            sampleY - half) * stepSize;
                        float distanceSquared = dot(offset, offset);
                        if (remove != 1 ||
                            distanceSquared <= radius * radius)
                        {
                            float weight = remove == 0
                                ? exp(-distanceSquared * inverseVariance)
                                : 1.0;
                            float2 sampleUv =
                                uv + (offset * PixelSize);
                            total += (correction
                                ? SampleRichardsonLucyEncoded(sampleUv)
                                : Unpremultiply(
                                    SampleNeighborhood(
                                        sampleUv,
                                        profile,
                                        0))) * weight;
                            totalWeight += weight;
                        }
                    }
                }
            }
        }
    }

    float3 filtered = total / max(totalWeight, 0.000001);
    if (correction)
    {
        return EncodeRichardsonLucyCorrection(filtered);
    }
    float alpha = SampleNeighborhood(uv, profile, 0).a;
    return float4(saturate(filtered) * alpha, alpha);
}

float RichardsonLucyLocalLuminance(
    float2 uv,
    float radius,
    int profile)
{
    float total = dot(
        Unpremultiply(SampleNeighborhoodOriginal(uv, profile)),
        float3(0.2126, 0.7152, 0.0722));
    if (radius <= 0.000001)
    {
        return total;
    }

    for (int index = 1; index < 17; index++)
    {
        float fraction = index / 16.0;
        float angle = index * 2.39996323;
        float distance = sqrt(fraction) * radius;
        float2 offset = float2(cos(angle), sin(angle)) *
            distance * PixelSize;
        total += dot(
            Unpremultiply(
                SampleNeighborhoodOriginal(uv + offset, profile)),
            float3(0.2126, 0.7152, 0.0722));
    }
    return total / 17.0;
}

float4 ApplyRichardsonLucy(
    float2 uv,
    float4 center,
    int sampleCount,
    int passKind,
    int profile)
{
    if (passKind == 3)
    {
        return SampleRichardsonLucyPsf(
            uv,
            sampleCount,
            profile,
            false);
    }
    if (passKind == 4)
    {
        float3 observed = Unpremultiply(
            SampleNeighborhoodOriginal(uv, profile));
        float3 blurred = max(
            Unpremultiply(center),
            1.0 / 4096.0);
        float3 ratio = clamp(observed / blurred, 0.0, 16.0);
        float updateStrength =
            1.0 - saturate(FilterOptions0.z);
        return EncodeRichardsonLucyCorrection(
            lerp(1.0, ratio, updateStrength));
    }
    if (passKind == 5)
    {
        return SampleRichardsonLucyPsf(
            uv,
            sampleCount,
            profile,
            true);
    }
    if (passKind == 6)
    {
        float4 estimate =
            SampleNeighborhoodOriginal(uv, profile);
        float3 correction =
            SampleRichardsonLucyEncoded(uv);
        float3 straight = saturate(
            Unpremultiply(estimate) * correction);
        return float4(straight * estimate.a, estimate.a);
    }

    float4 original =
        SampleNeighborhoodOriginal(uv, profile);
    float shadowLuminance = RichardsonLucyLocalLuminance(
        uv,
        FilterOptions1.w,
        profile);
    float highlightLuminance = RichardsonLucyLocalLuminance(
        uv,
        FilterOptions2.z,
        profile);
    float shadowWidth = saturate(FilterOptions1.z);
    float highlightWidth = saturate(FilterOptions2.y);
    float shadowProtection = shadowWidth <= 0.0
        ? 0.0
        : (1.0 - smoothstep(
            0.0,
            shadowWidth,
            shadowLuminance)) *
            saturate(FilterOptions1.y);
    float highlightProtection = highlightWidth <= 0.0
        ? 0.0
        : smoothstep(
            1.0 - highlightWidth,
            1.0,
            highlightLuminance) *
            saturate(FilterOptions2.x);
    float strength = max(FilterOptions0.x, 0.0) *
        (1.0 - max(shadowProtection, highlightProtection));
    float3 straight = saturate(
        Unpremultiply(original) +
        ((Unpremultiply(center) - Unpremultiply(original)) * strength));
    return float4(straight * original.a, original.a);
}

float3 ReduceNoiseRgbToYCoCg(float3 rgb)
{
    float co = rgb.r - rgb.b;
    float temporary = (rgb.r + rgb.b) * 0.5;
    float cg = rgb.g - temporary;
    return float3(temporary + (cg * 0.5), co, cg);
}

float3 ReduceNoiseYCoCgToRgb(float3 color)
{
    float temporary = color.x - (color.z * 0.5);
    float green = color.z + temporary;
    float blue = temporary - (color.y * 0.5);
    return float3(blue + color.y, green, blue);
}

float ReduceNoiseDomainDistance(float3 first, float3 second)
{
    float3 difference = abs(first - second);
    return difference.x + (0.25 * (difference.y + difference.z));
}

float4 SampleDomainTransform(
    float2 uv,
    float4 center,
    float2 direction,
    int radius,
    int iteration,
    int profile)
{
    if (center.a <= 0.000001)
    {
        return center;
    }

    float3 centerYCoCg = ReduceNoiseRgbToYCoCg(
        saturate(Unpremultiply(center)));
    float3 total = centerYCoCg;
    float totalWeight = 1.0;
    float iterationSigma = iteration == 0
        ? FilterOptions2.x
        : (iteration == 1 ? FilterOptions2.y : FilterOptions2.z);
    float spatialSigma = max(FilterOptions1.y, 0.000001);
    float rangeSigma =
        0.025 + (0.175 * (1.0 - saturate(FilterOptions0.y)));

    for (int side = -1; side <= 1; side += 2)
    {
        float3 previous = centerYCoCg;
        float domainDistance = 0.0;
        for (int step = 1; step <= 8; step++)
        {
            if (step <= radius)
            {
                float2 sampleUv =
                    uv + (direction * PixelSize * step * side);
                float4 sample = SampleNeighborhood(
                    sampleUv,
                    profile,
                    0);
                float3 sampleYCoCg = ReduceNoiseRgbToYCoCg(
                    saturate(Unpremultiply(sample)));
                domainDistance += 1.0 +
                    ((spatialSigma / rangeSigma) *
                        ReduceNoiseDomainDistance(
                            sampleYCoCg,
                            previous));
                previous = sampleYCoCg;
                float alphaWeight = saturate(
                    1.0 - (abs(sample.a - center.a) * 8.0));
                float weight = exp(
                    -sqrt(2.0) *
                    domainDistance /
                    max(iterationSigma, 0.000001)) *
                    alphaWeight;
                total += sampleYCoCg * weight;
                totalWeight += weight;
            }
        }
    }

    float3 filtered = total / totalWeight;
    float lumaMix = saturate(
        max(FilterOptions0.x, FilterOptions0.w) / 3.0);
    float chromaMix = saturate(FilterOptions0.z / 3.0);
    float3 mixed = float3(
        lerp(centerYCoCg.x, filtered.x, lumaMix),
        lerp(centerYCoCg.y, filtered.y, chromaMix),
        lerp(centerYCoCg.z, filtered.z, chromaMix));
    float3 straight = saturate(ReduceNoiseYCoCgToRgb(mixed));
    return float4(straight * center.a, center.a);
}

float4 SampleJpegDeblock(
    float2 uv,
    float2 position,
    float4 center,
    bool horizontal,
    int profile)
{
    if (center.a <= 0.000001)
    {
        return center;
    }

    uint coordinate = horizontal
        ? (uint)floor(position.x)
        : (uint)floor(position.y);
    uint phase = coordinate & 7u;
    if (phase != 0u && phase != 7u)
    {
        return center;
    }

    float direction = phase == 7u ? 1.0 : -1.0;
    float2 axis = horizontal
        ? float2(PixelSize.x, 0.0)
        : float2(0.0, PixelSize.y);
    float4 across = SampleNeighborhood(
        uv + (axis * direction),
        profile,
        0);
    float4 inner = SampleNeighborhood(
        uv - (axis * direction),
        profile,
        0);
    float4 acrossInner = SampleNeighborhood(
        uv + (axis * direction * 2.0),
        profile,
        0);
    float3 centerYCoCg = ReduceNoiseRgbToYCoCg(
        Unpremultiply(center));
    float3 acrossYCoCg = ReduceNoiseRgbToYCoCg(
        Unpremultiply(across));
    float boundary = ReduceNoiseDomainDistance(
        centerYCoCg,
        acrossYCoCg);
    float local = max(
        ReduceNoiseDomainDistance(
            centerYCoCg,
            ReduceNoiseRgbToYCoCg(Unpremultiply(inner))),
        ReduceNoiseDomainDistance(
            acrossYCoCg,
            ReduceNoiseRgbToYCoCg(Unpremultiply(acrossInner))));
    float alphaWeight = saturate(
        1.0 - (abs(across.a - center.a) * 8.0));
    float gate =
        saturate((boundary - local) * 12.0) *
        (1.0 - smoothstep(0.12, 0.35, boundary)) *
        alphaWeight;
    float3 straight = saturate(lerp(
        Unpremultiply(center),
        Unpremultiply(across),
        0.35 * gate));
    return float4(straight * center.a, center.a);
}

float4 RecombineReduceNoise(
    float2 uv,
    float4 filtered,
    int profile)
{
    float4 original = SampleNeighborhoodOriginal(uv, profile);
    if (original.a <= 0.000001)
    {
        return original;
    }

    float3 originalYCoCg = ReduceNoiseRgbToYCoCg(
        saturate(Unpremultiply(original)));
    float3 filteredYCoCg = ReduceNoiseRgbToYCoCg(
        saturate(Unpremultiply(filtered)));
    float strength = saturate(FilterOptions0.x);
    float preserve = saturate(FilterOptions0.y);
    float colorNoise = saturate(FilterOptions0.z);
    float sharpen = saturate(FilterOptions0.w);
    bool removeJpeg = FilterOptions1.x > 0.5;
    float lumaMix = max(strength, removeJpeg ? 0.65 : 0.0);
    float chromaMix = max(colorNoise, removeJpeg ? 0.5 : 0.0);
    float detail = originalYCoCg.x - filteredYCoCg.x;
    float3 combined = float3(
        lerp(originalYCoCg.x, filteredYCoCg.x, lumaMix) +
            (detail * ((preserve * strength) + (sharpen * 0.5))),
        lerp(originalYCoCg.y, filteredYCoCg.y, chromaMix),
        lerp(originalYCoCg.z, filteredYCoCg.z, chromaMix));
    float3 straight = saturate(ReduceNoiseYCoCgToRgb(combined));
    return float4(straight * original.a, original.a);
}
