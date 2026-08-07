uint CatalogWaveNoiseSeed()
{
    uint low = (uint)FilterOptions3.x;
    uint high = (uint)FilterOptions3.y;
    return (low & 0xffffu) | (high << 16);
}

float CatalogWaveNoiseHash(int x, int y, uint seed)
{
    uint value =
        ((uint)x * 0x9e3779b9u) ^
        ((uint)y * 0x85ebca6bu) ^
        (seed * 0xc2b2ae35u);
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777216.0;
}

float2 CatalogWaveNoiseDirection(float angle)
{
    return float2(cos(angle), sin(angle));
}

float CatalogWaveNoiseSlicePosition(
    int directionIndex,
    int slice,
    uint seed)
{
    return 0.3 +
        (0.4 * CatalogWaveNoiseHash(
            directionIndex,
            slice,
            seed + 0x31u));
}

float2 CatalogWaveNoiseTableSample(int sampleIndex)
{
    sampleIndex = sampleIndex % 64;
    if (sampleIndex < 0)
    {
        sampleIndex += 64;
    }
    int packedIndex = sampleIndex / 2;
    float4 packed = tex2Dlod(
        WaveNoiseTableSampler,
        float4(
            (packedIndex + 0.5) / 32.0,
            0.5,
            0.0,
            0.0));
    return (sampleIndex & 1) == 0
        ? packed.xy
        : packed.zw;
}

float2 CatalogWaveNoiseTableLinear(float coordinate)
{
    float wrapped = frac(coordinate) * 64.0;
    int first = (int)floor(wrapped);
    int second = (first + 1) % 64;
    return lerp(
        CatalogWaveNoiseTableSample(first),
        CatalogWaveNoiseTableSample(second),
        wrapped - first);
}

float2 CatalogWaveNoiseSlice(
    float2 scaledPosition,
    int directionIndex,
    int slice,
    int directionCount,
    float sectorStart,
    float sliceThickness,
    uint seed)
{
    float sectorWidth = 3.14159265359 / directionCount;
    float angle =
        sectorStart +
        (sectorWidth * CatalogWaveNoiseHash(
                directionIndex,
                slice,
                seed + 0x6du));
    float2 direction = CatalogWaveNoiseDirection(
        angle);
    float offset = CatalogWaveNoiseHash(
        directionIndex,
        slice,
        seed + 0xb7u);
    float coordinate =
        (dot(scaledPosition, direction) + offset) /
        (sliceThickness * 32.0);
    return CatalogWaveNoiseTableLinear(coordinate);
}

float CatalogWaveNoiseDirectionWeight(
    float angle,
    float axis,
    float isotropy)
{
    if (isotropy >= 0.999)
    {
        return 1.0;
    }

    float delta = abs(
        frac(
            ((angle - axis) / 3.14159265359) +
            0.5) *
        3.14159265359 -
        1.57079632679);
    float sigma =
        0.04 +
        (isotropy * (1.57079632679 - 0.04));
    float ratio = delta / sigma;
    return exp(-0.5 * ratio * ratio) + 0.001;
}

float CatalogWaveNoise(float2 position)
{
    int directionCount = clamp(
        (int)FilterOptions4.x,
        4,
        32);
    float sliceThickness = clamp(
        FilterOptions5.x,
        0.25,
        16.0);
    float axis =
        FilterOptions7.x * (3.14159265359 / 180.0);
    float isotropy = saturate(FilterOptions7.y);
    float2 scaledPosition = position * sliceThickness;
    uint seed = CatalogWaveNoiseSeed();
    float2 sum = 0.0;
    float weightSum = 0.0;
    float weightSquareSum = 0.0;

    [fastopt]
    for (int directionIndex = 0;
        directionIndex < directionCount;
        directionIndex++)
    {
        float sectorWidth =
            3.14159265359 / directionCount;
        float sectorStart =
            directionIndex * sectorWidth;
        float baseAngle =
            sectorStart +
            (sectorWidth * CatalogWaveNoiseHash(
                directionIndex,
                0,
                seed + 0x19u));
        float2 baseDirection =
            CatalogWaveNoiseDirection(baseAngle);
        float projection = dot(
            scaledPosition,
            baseDirection);
        int cell = (int)floor(projection);
        float center =
            cell +
            CatalogWaveNoiseSlicePosition(
                directionIndex,
                cell,
                seed);
        int leftSlice;
        int rightSlice;
        float leftPosition;
        float rightPosition;
        if (projection < center)
        {
            leftSlice = cell - 1;
            rightSlice = cell;
            leftPosition =
                leftSlice +
                CatalogWaveNoiseSlicePosition(
                    directionIndex,
                    leftSlice,
                    seed);
            rightPosition = center;
        }
        else
        {
            leftSlice = cell;
            rightSlice = cell + 1;
            leftPosition = center;
            rightPosition =
                rightSlice +
                CatalogWaveNoiseSlicePosition(
                    directionIndex,
                    rightSlice,
                    seed);
        }

        float blend = smoothstep(
            0.0,
            1.0,
            saturate(
                (projection - leftPosition) /
                max(
                    rightPosition - leftPosition,
                    0.0001)));
        float2 left = CatalogWaveNoiseSlice(
            scaledPosition,
            directionIndex,
            leftSlice,
            directionCount,
            sectorStart,
            sliceThickness,
            seed);
        float2 right = CatalogWaveNoiseSlice(
            scaledPosition,
            directionIndex,
            rightSlice,
            directionCount,
            sectorStart,
            sliceThickness,
            seed);
        float weight = CatalogWaveNoiseDirectionWeight(
            baseAngle,
            axis,
            isotropy);
        sum += lerp(left, right, blend) * weight;
        weightSum += weight;
        weightSquareSum += weight * weight;
    }

    float effectiveDirectionCount =
        weightSum /
        sqrt(max(weightSquareSum, 0.0001));
    float normalized =
        (sum.x / max(weightSum, 0.0001)) *
        effectiveDirectionCount *
        FilterOptions9.x;
    return saturate(0.5 + (normalized * 0.2));
}
