float4 CatalogFacet(
    float2 uv,
    float4 source,
    int profile)
{
    const int radius = 3;
    const int maxSampleRadius = radius * 2;
    const float alpha = 1.0;
    const float zeta = 2.0 / radius;
    const float gamma = 3.0 * 3.14159265358979323846 / 16.0;
    const float diagonal = 0.7071067811865476;
    if (source.a <= 0.000001)
    {
        return 0.0;
    }

    float eta =
        (zeta + cos(gamma)) /
        (sin(gamma) * sin(gamma));
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
    float anisotropy =
        (lambda1 + lambda2) <= 0.000001
            ? 0.0
            : saturate(
                (lambda1 - lambda2) /
                (lambda1 + lambda2));
    float angle =
        (0.5 * atan2(
            2.0 * tensor.y,
            tensor.x - tensor.z)) +
        (0.5 * 3.14159265358979323846);
    float cosine = cos(angle);
    float sine = sin(angle);
    float majorRadius =
        radius * ((alpha + anisotropy) / alpha);
    float minorRadius =
        radius * (alpha / (alpha + anisotropy));

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
    for (int offsetY = -maxSampleRadius;
        offsetY <= maxSampleRadius;
        offsetY++)
    {
        [loop]
        for (int offsetX = -maxSampleRadius;
            offsetX <= maxSampleRadius;
            offsetX++)
        {
            float localX =
                ((cosine * offsetX) +
                    (sine * offsetY)) /
                majorRadius;
            float localY =
                ((-sine * offsetX) +
                    (cosine * offsetY)) /
                minorRadius;
            if ((localX * localX) +
                    (localY * localY) >
                1.0)
            {
                continue;
            }

            float xPolynomial =
                zeta - (eta * localX * localX);
            float yPolynomial =
                zeta - (eta * localY * localY);
            float value = max(0.0, localY + xPolynomial);
            float4 cardinalWeights;
            cardinalWeights.x = value * value;
            value = max(0.0, -localX + yPolynomial);
            cardinalWeights.y = value * value;
            value = max(0.0, -localY + xPolynomial);
            cardinalWeights.z = value * value;
            value = max(0.0, localX + yPolynomial);
            cardinalWeights.w = value * value;

            float rotatedX =
                diagonal * (localX - localY);
            float rotatedY =
                diagonal * (localX + localY);
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

            float gaussian =
                exp(
                    -3.125 *
                    ((localX * localX) +
                        (localY * localY))) /
                sectorTotal;
            float4 sample = CatalogFacetLinearSample(
                uv +
                    float2(offsetX, offsetY) *
                    PixelSize,
                profile);
            if (sample.a <= 0.000001)
            {
                continue;
            }
            float3 straight =
                saturate(Unpremultiply(sample));
            cardinalWeights *= gaussian * sample.a;
            diagonalWeights *= gaussian * sample.a;
            cardinalWeightSum += cardinalWeights;
            cardinalRedSum += straight.r * cardinalWeights;
            cardinalGreenSum += straight.g * cardinalWeights;
            cardinalBlueSum += straight.b * cardinalWeights;
            cardinalRedSquareSum +=
                straight.r * straight.r * cardinalWeights;
            cardinalGreenSquareSum +=
                straight.g * straight.g * cardinalWeights;
            cardinalBlueSquareSum +=
                straight.b * straight.b * cardinalWeights;
            diagonalWeightSum += diagonalWeights;
            diagonalRedSum += straight.r * diagonalWeights;
            diagonalGreenSum += straight.g * diagonalWeights;
            diagonalBlueSum += straight.b * diagonalWeights;
            diagonalRedSquareSum +=
                straight.r * straight.r * diagonalWeights;
            diagonalGreenSquareSum +=
                straight.g * straight.g * diagonalWeights;
            diagonalBlueSquareSum +=
                straight.b * straight.b * diagonalWeights;
        }
    }

    float4 cardinalDivisor =
        max(cardinalWeightSum, 0.000001);
    float4 cardinalMeanRed =
        cardinalRedSum / cardinalDivisor;
    float4 cardinalMeanGreen =
        cardinalGreenSum / cardinalDivisor;
    float4 cardinalMeanBlue =
        cardinalBlueSum / cardinalDivisor;
    float4 cardinalVariance =
        max(
            0.0,
            (cardinalRedSquareSum / cardinalDivisor) -
                (cardinalMeanRed * cardinalMeanRed)) +
        max(
            0.0,
            (cardinalGreenSquareSum / cardinalDivisor) -
                (cardinalMeanGreen * cardinalMeanGreen)) +
        max(
            0.0,
            (cardinalBlueSquareSum / cardinalDivisor) -
                (cardinalMeanBlue * cardinalMeanBlue));
    float4 cardinalConfidence =
        step(0.000001, cardinalWeightSum) /
        (1.0 +
            pow(1000.0 * cardinalVariance, 4.0));

    float4 diagonalDivisor =
        max(diagonalWeightSum, 0.000001);
    float4 diagonalMeanRed =
        diagonalRedSum / diagonalDivisor;
    float4 diagonalMeanGreen =
        diagonalGreenSum / diagonalDivisor;
    float4 diagonalMeanBlue =
        diagonalBlueSum / diagonalDivisor;
    float4 diagonalVariance =
        max(
            0.0,
            (diagonalRedSquareSum / diagonalDivisor) -
                (diagonalMeanRed * diagonalMeanRed)) +
        max(
            0.0,
            (diagonalGreenSquareSum / diagonalDivisor) -
                (diagonalMeanGreen * diagonalMeanGreen)) +
        max(
            0.0,
            (diagonalBlueSquareSum / diagonalDivisor) -
                (diagonalMeanBlue * diagonalMeanBlue));
    float4 diagonalConfidence =
        step(0.000001, diagonalWeightSum) /
        (1.0 +
            pow(1000.0 * diagonalVariance, 4.0));

    float3 result = float3(
        dot(cardinalMeanRed, cardinalConfidence) +
            dot(diagonalMeanRed, diagonalConfidence),
        dot(cardinalMeanGreen, cardinalConfidence) +
            dot(diagonalMeanGreen, diagonalConfidence),
        dot(cardinalMeanBlue, cardinalConfidence) +
            dot(diagonalMeanBlue, diagonalConfidence));
    float resultWeight =
        dot(cardinalConfidence, 1.0) +
        dot(diagonalConfidence, 1.0);
    result = resultWeight <= 0.000001
        ? saturate(Unpremultiply(source))
        : saturate(result / resultWeight);
    return float4(result * source.a, source.a);
}
