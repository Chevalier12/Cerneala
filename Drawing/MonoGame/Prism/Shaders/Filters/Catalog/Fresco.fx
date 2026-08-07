


sampler2D FrescoOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float3 FrescoStraightSample(
    float2 uv,
    int profile)
{
    return saturate(Unpremultiply(
        CatalogLinearSample(uv, profile)));
}

float4 FrescoStructureTensor(
    float2 uv,
    int profile)
{
    float3 topLeft = FrescoStraightSample(
        uv - PixelSize,
        profile);
    float3 top = FrescoStraightSample(
        uv + float2(0.0, -PixelSize.y),
        profile);
    float3 topRight = FrescoStraightSample(
        uv + float2(PixelSize.x, -PixelSize.y),
        profile);
    float3 left = FrescoStraightSample(
        uv + float2(-PixelSize.x, 0.0),
        profile);
    float3 right = FrescoStraightSample(
        uv + float2(PixelSize.x, 0.0),
        profile);
    float3 bottomLeft = FrescoStraightSample(
        uv + float2(-PixelSize.x, PixelSize.y),
        profile);
    float3 bottom = FrescoStraightSample(
        uv + float2(0.0, PixelSize.y),
        profile);
    float3 bottomRight = FrescoStraightSample(
        uv + PixelSize,
        profile);
    float3 horizontal = (
        -topLeft + topRight -
        (2.0 * left) + (2.0 * right) -
        bottomLeft + bottomRight) * 0.25;
    float3 vertical = (
        -topLeft - (2.0 * top) - topRight +
        bottomLeft + (2.0 * bottom) + bottomRight) * 0.25;
    float horizontalEnergy =
        dot(horizontal, horizontal) / 3.0;
    float verticalEnergy =
        dot(vertical, vertical) / 3.0;
    float crossEnergy =
        dot(horizontal, vertical) / 3.0;
    return float4(
        saturate(horizontalEnergy),
        saturate((crossEnergy * 0.5) + 0.5),
        saturate(verticalEnergy),
        1.0);
}

float4 FrescoTensorSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5)));
}

float4 FrescoBlurTensor(
    float2 uv,
    bool horizontal)
{
    float radius = clamp(
        horizontal ? FilterOptions9.x : FilterOptions9.y,
        1.0,
        4.0);
    float sigma = max(radius * 0.5, 0.75);
    float divisor = 2.0 * sigma * sigma;
    float4 total = 0.0;
    float totalWeight = 0.0;
    [loop]
    for (int offset = -4; offset <= 4; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float weight = exp(
                -(offset * offset) / divisor);
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            total += FrescoTensorSample(uv + delta) *
                weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float3 FrescoDecodeTensor(float4 encoded)
{
    return float3(
        encoded.x,
        (encoded.y - 0.5) * 2.0,
        encoded.z);
}

float4 FrescoOriginal(
    float2 uv,
    int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            FrescoOriginalSampler,
            float4(
                clamp(
                    uv,
                    PixelSize * 0.5,
                    1.0 - (PixelSize * 0.5)),
                0.0,
                0.0)),
        profile);
}

float FrescoHash(
    uint2 position,
    uint seed)
{
    uint value =
        (position.x * 0x9e3779b9u) ^
        (position.y * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777215.0;
}

float4 FrescoKuwahara(
    float2 uv,
    int profile)
{
    float4 center = FrescoOriginal(uv, profile);
    if (center.a <= 0.0)
    {
        return 0.0;
    }

    const float diagonal = 0.7071067811865476;
    const float gamma = 0.5890486225480862;
    float radius = clamp(FilterOptions9.x, 1.0, 6.0);
    float3 tensor = FrescoDecodeTensor(
        FrescoTensorSample(uv));
    float difference = tensor.x - tensor.z;
    float discriminant = sqrt(max(
        (difference * difference) +
            (4.0 * tensor.y * tensor.y),
        0.0));
    float lambda1 =
        0.5 * (tensor.x + tensor.z + discriminant);
    float lambda2 =
        0.5 * (tensor.x + tensor.z - discriminant);
    float tensorEnergy = lambda1 + lambda2;
    float anisotropy = tensorEnergy <= 0.000001
        ? 0.0
        : saturate((lambda1 - lambda2) / tensorEnergy);
    float angle =
        (0.5 * atan2(
            2.0 * tensor.y,
            difference)) +
        1.5707963267948966;
    float cosine = cos(angle);
    float sine = sin(angle);
    float majorRadius = radius * (1.0 + anisotropy);
    float minorRadius = radius / (1.0 + anisotropy);
    int sampleRadius = min(
        (int)ceil(majorRadius),
        12);
    float zeta = 2.0 / radius;
    float eta =
        (zeta + cos(gamma)) /
        max(sin(gamma) * sin(gamma), 0.000001);

    float4 moments[8];
    float3 squareSums[8];
    [unroll]
    for (int sector = 0; sector < 8; sector++)
    {
        moments[sector] = 0.0;
        squareSums[sector] = 0.0;
    }

    [loop]
    for (int offsetY = -12; offsetY <= 12; offsetY++)
    {
        [loop]
        for (int offsetX = -12; offsetX <= 12; offsetX++)
        {
            if (abs(offsetX) <= sampleRadius &&
                abs(offsetY) <= sampleRadius)
            {
                float localX =
                    ((cosine * offsetX) +
                        (sine * offsetY)) /
                    majorRadius;
                float localY =
                    ((-sine * offsetX) +
                        (cosine * offsetY)) /
                    minorRadius;
                float distanceSquared =
                    (localX * localX) +
                    (localY * localY);
                if (distanceSquared <= 1.0)
                {
                    float weights[8];
                    float vxx =
                        zeta - (eta * localX * localX);
                    float vyy =
                        zeta - (eta * localY * localY);
                    float value = max(0.0, localY + vxx);
                    weights[0] = value * value;
                    value = max(0.0, -localX + vyy);
                    weights[2] = value * value;
                    value = max(0.0, -localY + vxx);
                    weights[4] = value * value;
                    value = max(0.0, localX + vyy);
                    weights[6] = value * value;

                    float rotatedX =
                        diagonal * (localX - localY);
                    float rotatedY =
                        diagonal * (localX + localY);
                    vxx = zeta -
                        (eta * rotatedX * rotatedX);
                    vyy = zeta -
                        (eta * rotatedY * rotatedY);
                    value = max(0.0, rotatedY + vxx);
                    weights[1] = value * value;
                    value = max(0.0, -rotatedX + vyy);
                    weights[3] = value * value;
                    value = max(0.0, -rotatedY + vxx);
                    weights[5] = value * value;
                    value = max(0.0, rotatedX + vyy);
                    weights[7] = value * value;

                    float sectorTotal = 0.0;
                    [unroll]
                    for (int index = 0; index < 8; index++)
                    {
                        sectorTotal += weights[index];
                    }
                    if (sectorTotal > 0.000001)
                    {
                        float4 sample = FrescoOriginal(
                            uv +
                                (float2(offsetX, offsetY) *
                                    PixelSize),
                            profile);
                        if (sample.a > 0.000001)
                        {
                            float3 color = saturate(
                                Unpremultiply(sample));
                            float gaussian = exp(
                                -3.125 * distanceSquared) /
                                sectorTotal;
                            float alphaStop =
                                sample.a *
                                exp(-abs(sample.a - center.a) * 8.0);
                            [unroll]
                            for (int index = 0;
                                index < 8;
                                index++)
                            {
                                float weight =
                                    weights[index] *
                                    gaussian *
                                    alphaStop;
                                moments[index] +=
                                    float4(color * weight, weight);
                                squareSums[index] +=
                                    color * color * weight;
                            }
                        }
                    }
                }
            }
        }
    }

    float detail = clamp(FilterOptions1.x, 0.0, 16.0);
    float hardness = 250.0 + (93.75 * detail);
    float exponent = max(0.5, detail * 0.5);
    float3 result = 0.0;
    float resultWeight = 0.0;
    [unroll]
    for (int selectedSector = 0;
        selectedSector < 8;
        selectedSector++)
    {
        if (moments[selectedSector].w > 0.000001)
        {
            float3 mean =
                moments[selectedSector].rgb /
                moments[selectedSector].w;
            float3 variance = max(
                (squareSums[selectedSector] /
                    moments[selectedSector].w) -
                    (mean * mean),
                0.0);
            float varianceSum =
                variance.r + variance.g + variance.b;
            float confidence = 1.0 /
                (1.0 + pow(
                    max(hardness * varianceSum, 0.0),
                    exponent));
            result += mean * confidence;
            resultWeight += confidence;
        }
    }

    float3 centerColor = saturate(Unpremultiply(center));
    result = resultWeight <= 0.000001
        ? centerColor
        : result / resultWeight;
    float textureStrength =
        clamp(FilterOptions2.x, 0.0, 8.0) *
        0.02;
    if (textureStrength > 0.0)
    {
        uint2 pixel = (uint2)floor(uv / PixelSize);
        float coarse = FrescoHash(
            pixel >> 1,
            0x51ed270bu);
        float fine = FrescoHash(
            pixel,
            0x68bc21ebu);
        float roughness =
            (((coarse * 0.65) + (fine * 0.35)) - 0.5) *
            textureStrength;
        float luminance = dot(
            result,
            float3(0.2126, 0.7152, 0.0722));
        result = saturate(
            result +
            (roughness *
                (0.4 + (0.6 * (1.0 - luminance)))));
    }
    return float4(result * center.a, center.a);
}
