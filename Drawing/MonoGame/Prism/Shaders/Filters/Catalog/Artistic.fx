float DryBrushHash(int2 position, uint seed)
{
    uint2 bits = (uint2)position;
    uint value =
        (bits.x * 0x9e3779b9u) ^
        (bits.y * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777215.0;
}

float4 CatalogPolynomialAnisotropicKuwahara(
    float2 uv,
    float4 source,
    int profile,
    float radius,
    float sharpness,
    float widthScale,
    float minorScale,
    float roughness,
    float luminancePreference,
    float diagonalBias,
    float diagonalBalance,
    float balanceDiagonalsByLuminance,
    uint jitterSeed)
{
    const int latticeRadius = 4;
    const float zeta = 2.0 / latticeRadius;
    const float gamma =
        3.0 * 3.14159265358979323846 / 16.0;
    const float diagonal = 0.7071067811865476;
    if (source.a <= 0.000001)
    {
        return 0.0;
    }

    radius = max(radius, 1.0);
    sharpness = clamp(sharpness, 0.5, 12.0);

    float3 tensor = CatalogFacetStructureTensor(
        uv,
        profile);
    float discriminant = sqrt(max(
        0.0,
        ((tensor.x - tensor.z) *
            (tensor.x - tensor.z)) +
        (4.0 * tensor.y * tensor.y)));
    float lambda1 =
        0.5 * (tensor.x + tensor.z + discriminant);
    float lambda2 =
        0.5 * (tensor.x + tensor.z - discriminant);
    float tensorEnergy = lambda1 + lambda2;
    float anisotropy = tensorEnergy <= 0.000001
        ? 0.0
        : saturate((lambda1 - lambda2) / tensorEnergy);
    float angle = tensorEnergy <= 0.000001
        ? 0.0
        : (0.5 * atan2(
            2.0 * tensor.y,
            tensor.x - tensor.z)) +
            (0.5 * 3.14159265358979323846);
    if (roughness > 0.0)
    {
        float2 pixel = uv / PixelSize;
        float blockSize = max(radius * 2.0, 1.0);
        float jitter = DryBrushHash(
            (int2)floor(pixel / blockSize),
            jitterSeed) - 0.5;
        angle += jitter * roughness * 3.14159265358979323846;
    }
    if (diagonalBias > 0.0)
    {
        float diagonalAngle;
        if (balanceDiagonalsByLuminance > 0.5)
        {
            float threshold = 1.0 - saturate(diagonalBalance);
            diagonalAngle = CatalogLuminance(source) >= threshold
                ? 0.25 * 3.14159265358979323846
                : -0.25 * 3.14159265358979323846;
        }
        else
        {
            diagonalAngle = sin(2.0 * angle) >= 0.0
                ? 0.25 * 3.14159265358979323846
                : -0.25 * 3.14159265358979323846;
        }
        float2 tangent = float2(cos(angle), sin(angle));
        float2 diagonalTangent = float2(
            cos(diagonalAngle),
            sin(diagonalAngle));
        if (dot(tangent, diagonalTangent) < 0.0)
        {
            diagonalTangent = -diagonalTangent;
        }
        float effectiveBias = saturate(
            diagonalBias * (1.0 - (0.35 * anisotropy)));
        tangent = normalize(lerp(
            tangent,
            diagonalTangent,
            effectiveBias));
        angle = atan2(tangent.y, tangent.x);
    }

    float cosine = cos(angle);
    float sine = sin(angle);
    float majorRadius =
        radius * widthScale * (1.0 + anisotropy);
    float minorRadius =
        radius * minorScale / (1.0 + anisotropy);
    float eta =
        (zeta + cos(gamma)) /
        (sin(gamma) * sin(gamma));

    float4 cardinalWeightSum = 0.0;
    float4 cardinalRedSum = 0.0;
    float4 cardinalGreenSum = 0.0;
    float4 cardinalBlueSum = 0.0;
    float4 cardinalRedSquareSum = 0.0;
    float4 cardinalGreenSquareSum = 0.0;
    float4 cardinalBlueSquareSum = 0.0;
    float4 diagonalWeightSum = 0.0;
    float4 diagonalRedSum = 0.0;
    float4 diagonalGreenSum = 0.0;
    float4 diagonalBlueSum = 0.0;
    float4 diagonalRedSquareSum = 0.0;
    float4 diagonalGreenSquareSum = 0.0;
    float4 diagonalBlueSquareSum = 0.0;

    [loop]
    for (int offsetY = -latticeRadius;
        offsetY <= latticeRadius;
        offsetY++)
    {
        [loop]
        for (int offsetX = -latticeRadius;
            offsetX <= latticeRadius;
            offsetX++)
        {
            float2 local =
                float2(offsetX, offsetY) / latticeRadius;
            float radiusSquared = dot(local, local);
            if (radiusSquared > 1.0)
            {
                continue;
            }

            float xPolynomial =
                zeta - (eta * local.x * local.x);
            float yPolynomial =
                zeta - (eta * local.y * local.y);
            float value = max(
                0.0,
                local.y + xPolynomial);
            float4 cardinalWeights;
            cardinalWeights.x = value * value;
            value = max(0.0, -local.x + yPolynomial);
            cardinalWeights.y = value * value;
            value = max(0.0, -local.y + xPolynomial);
            cardinalWeights.z = value * value;
            value = max(0.0, local.x + yPolynomial);
            cardinalWeights.w = value * value;

            float rotatedX =
                diagonal * (local.x - local.y);
            float rotatedY =
                diagonal * (local.x + local.y);
            xPolynomial =
                zeta - (eta * rotatedX * rotatedX);
            yPolynomial =
                zeta - (eta * rotatedY * rotatedY);
            value = max(0.0, rotatedY + xPolynomial);
            float4 diagonalWeights;
            diagonalWeights.x = value * value;
            value = max(0.0, -rotatedX + yPolynomial);
            diagonalWeights.y = value * value;
            value = max(0.0, -rotatedY + xPolynomial);
            diagonalWeights.z = value * value;
            value = max(0.0, rotatedX + yPolynomial);
            diagonalWeights.w = value * value;

            float sectorTotal =
                dot(cardinalWeights, 1.0) +
                dot(diagonalWeights, 1.0);
            if (sectorTotal <= 0.000001)
            {
                continue;
            }

            float2 sampleOffset;
            sampleOffset.x =
                (cosine * local.x * majorRadius) -
                (sine * local.y * minorRadius);
            sampleOffset.y =
                (sine * local.x * majorRadius) +
                (cosine * local.y * minorRadius);
            float4 sample = CatalogFacetLinearSample(
                uv + (sampleOffset * PixelSize),
                profile);
            if (sample.a <= 0.000001)
            {
                continue;
            }

            float3 straight =
                saturate(Unpremultiply(sample));
            float gaussian =
                exp(-3.125 * radiusSquared) /
                sectorTotal;
            cardinalWeights *= gaussian * sample.a;
            diagonalWeights *= gaussian * sample.a;
            cardinalWeightSum += cardinalWeights;
            cardinalRedSum += cardinalWeights * straight.r;
            cardinalGreenSum += cardinalWeights * straight.g;
            cardinalBlueSum += cardinalWeights * straight.b;
            cardinalRedSquareSum +=
                cardinalWeights * straight.r * straight.r;
            cardinalGreenSquareSum +=
                cardinalWeights * straight.g * straight.g;
            cardinalBlueSquareSum +=
                cardinalWeights * straight.b * straight.b;
            diagonalWeightSum += diagonalWeights;
            diagonalRedSum += diagonalWeights * straight.r;
            diagonalGreenSum += diagonalWeights * straight.g;
            diagonalBlueSum += diagonalWeights * straight.b;
            diagonalRedSquareSum +=
                diagonalWeights * straight.r * straight.r;
            diagonalGreenSquareSum +=
                diagonalWeights * straight.g * straight.g;
            diagonalBlueSquareSum +=
                diagonalWeights * straight.b * straight.b;
        }
    }

    float4 safeCardinalWeight =
        max(cardinalWeightSum, 0.000001);
    float4 safeDiagonalWeight =
        max(diagonalWeightSum, 0.000001);
    float4 cardinalMeanRed =
        cardinalRedSum / safeCardinalWeight;
    float4 cardinalMeanGreen =
        cardinalGreenSum / safeCardinalWeight;
    float4 cardinalMeanBlue =
        cardinalBlueSum / safeCardinalWeight;
    float4 diagonalMeanRed =
        diagonalRedSum / safeDiagonalWeight;
    float4 diagonalMeanGreen =
        diagonalGreenSum / safeDiagonalWeight;
    float4 diagonalMeanBlue =
        diagonalBlueSum / safeDiagonalWeight;
    float4 cardinalVariance = max(
        0.0,
        (cardinalRedSquareSum / safeCardinalWeight) -
            (cardinalMeanRed * cardinalMeanRed) +
        (cardinalGreenSquareSum / safeCardinalWeight) -
            (cardinalMeanGreen * cardinalMeanGreen) +
        (cardinalBlueSquareSum / safeCardinalWeight) -
            (cardinalMeanBlue * cardinalMeanBlue));
    float4 diagonalVariance = max(
        0.0,
        (diagonalRedSquareSum / safeDiagonalWeight) -
            (diagonalMeanRed * diagonalMeanRed) +
        (diagonalGreenSquareSum / safeDiagonalWeight) -
            (diagonalMeanGreen * diagonalMeanGreen) +
        (diagonalBlueSquareSum / safeDiagonalWeight) -
            (diagonalMeanBlue * diagonalMeanBlue));
    float4 cardinalConfidence =
        1.0 /
        (1.0 + pow(
            max(cardinalVariance * 100.0, 0.0),
            sharpness));
    float4 diagonalConfidence =
        1.0 /
        (1.0 + pow(
            max(diagonalVariance * 100.0, 0.0),
            sharpness));
    float4 cardinalLuminance =
        (cardinalMeanRed * 0.2126) +
        (cardinalMeanGreen * 0.7152) +
        (cardinalMeanBlue * 0.0722);
    float4 diagonalLuminance =
        (diagonalMeanRed * 0.2126) +
        (diagonalMeanGreen * 0.7152) +
        (diagonalMeanBlue * 0.0722);
    cardinalConfidence *= max(
        0.05,
        1.0 +
            (luminancePreference *
                (cardinalLuminance - 0.5)));
    diagonalConfidence *= max(
        0.05,
        1.0 +
            (luminancePreference *
                (diagonalLuminance - 0.5)));
    cardinalConfidence *= step(
        0.000001,
        cardinalWeightSum);
    diagonalConfidence *= step(
        0.000001,
        diagonalWeightSum);

    float totalConfidence =
        dot(cardinalConfidence, 1.0) +
        dot(diagonalConfidence, 1.0);
    float3 result = saturate(Unpremultiply(source));
    if (totalConfidence > 0.000001)
    {
        result.r =
            dot(cardinalMeanRed, cardinalConfidence) +
            dot(diagonalMeanRed, diagonalConfidence);
        result.g =
            dot(cardinalMeanGreen, cardinalConfidence) +
            dot(diagonalMeanGreen, diagonalConfidence);
        result.b =
            dot(cardinalMeanBlue, cardinalConfidence) +
            dot(diagonalMeanBlue, diagonalConfidence);
        result /= totalConfidence;
    }
    return float4(saturate(result) * source.a, source.a);
}

float CatalogProceduralTextureHeight(
    float2 pixel,
    int textureCode,
    float scaling,
    uint fineSeed,
    uint coarseSeed)
{
    float2 q = pixel / max(scaling, 0.125);
    float fineNoise = DryBrushHash(
        (int2)floor(q),
        fineSeed);
    if (textureCode == 1)
    {
        float row = floor(q.y / 4.0);
        float2 local = frac(float2(
            (q.x / 8.0) + (fmod(row + 1024.0, 2.0) * 0.5),
            q.y / 4.0));
        float edge = min(
            min(local.x, 1.0 - local.x),
            min(local.y, 1.0 - local.y));
        float mortar = edge < 0.08 ? 1.0 : 0.0;
        return saturate(
            0.25 + (0.5 * fineNoise) + (0.25 * mortar));
    }
    if (textureCode == 2)
    {
        float warp = 0.5 +
            (0.5 * cos(q.x * 3.14159265 * 0.5));
        float weft = 0.5 +
            (0.5 * cos(q.y * 3.14159265 * 0.5));
        return saturate(
            0.25 +
            (0.3 * warp) +
            (0.3 * weft) +
            (0.15 * fineNoise));
    }
    if (textureCode == 3)
    {
        float coarseNoise = DryBrushHash(
            (int2)floor(q / 4.0),
            coarseSeed);
        return (0.6 * coarseNoise) + (0.4 * fineNoise);
    }

    float canvasX = 0.5 +
        (0.5 * cos(q.x * 3.14159265));
    float canvasY = 0.5 +
        (0.5 * cos(q.y * 3.14159265));
    return saturate(
        (0.35 * canvasX) +
        (0.35 * canvasY) +
        (0.3 * fineNoise));
}
