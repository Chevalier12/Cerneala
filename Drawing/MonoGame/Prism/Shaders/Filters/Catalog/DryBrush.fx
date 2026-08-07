float4 CatalogDryBrush(
    float2 uv,
    float4 source,
    int profile)
{
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float detail = clamp(FilterOptions0.x, 0.0, 32.0);
    float textureStrength = saturate(
        FilterOptions2.x / 4.0);
    uint seed = 79u * 0x9e3779b9u;
    float2 pixel = uv / PixelSize;
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
    float2 tangent;
    if (tensorEnergy > 0.000001)
    {
        float tensorAngle =
            (0.5 * atan2(
                2.0 * tensor.y,
                tensor.x - tensor.z)) +
            (0.5 * 3.14159265358979323846);
        tangent = float2(
            cos(tensorAngle),
            sin(tensorAngle));
    }
    else
    {
        float blockSize = max(radius * 2.0, 1.0);
        float angle = DryBrushHash(
            (int2)floor(pixel / blockSize),
            seed) * 6.2831853;
        tangent = float2(cos(angle), sin(angle));
    }

    float2 normal = float2(-tangent.y, tangent.x);
    float coherence =
        tensorEnergy <= 0.000001
            ? 0.0
            : saturate(
                (lambda1 - lambda2) /
                tensorEnergy);
    float majorScale = 1.0 + (1.25 * coherence);
    float minorScale = 1.0 - (0.5 * coherence);
    float sharpness =
        1.0 + (5.0 * saturate(detail / 16.0));
    float3 accumulated = 0.0;
    float totalConfidence = 0.0;
    float3 centerColor = saturate(Unpremultiply(source));
    [loop]
    for (int sector = 0; sector < 8; sector++)
    {
        float angle = sector * 0.78539816;
        float2 direction = normalize(
            (tangent * cos(angle) * majorScale) +
            (normal * sin(angle) * minorScale));
        float3 sum = centerColor;
        float3 squareSum = centerColor * centerColor;
        float totalWeight = 1.0;
        [loop]
        for (int stepIndex = 1;
            stepIndex <= 3;
            stepIndex++)
        {
            float fraction = stepIndex / 3.0;
            float spatialWeight = exp(
                -2.0 * fraction * fraction);
            float2 offset =
                direction * radius * fraction;
            float4 sample = CatalogLinearSample(
                uv + (offset * PixelSize),
                profile);
            float weight =
                spatialWeight *
                exp(-abs(sample.a - source.a) * 8.0) *
                step(0.000001, sample.a);
            float3 color =
                saturate(Unpremultiply(sample));
            sum += color * weight;
            squareSum += color * color * weight;
            totalWeight += weight;
        }

        float3 mean = sum /
            max(totalWeight, 0.000001);
        float3 colorVariance = max(
            (squareSum /
                max(totalWeight, 0.000001)) -
                (mean * mean),
            0.0);
        float variance = dot(
            colorVariance,
            1.0 / 3.0);
        float confidence = 1.0 /
            (1.0 + pow(
                max(variance * 24.0, 0.0),
                sharpness));
        accumulated += mean * confidence;
        totalConfidence += confidence;
    }

    float3 filtered = accumulated /
        max(totalConfidence, 0.000001);
    float tangentCoordinate = dot(pixel, tangent);
    float normalCoordinate = dot(pixel, normal);
    float phase = DryBrushHash(
        (int2)floor(float2(
            tangentCoordinate / max(radius * 4.0, 1.0),
            normalCoordinate / max(radius * 2.0, 1.0))),
        seed ^ 0x68bc21ebu);
    float fiberCoordinate =
        normalCoordinate /
        max(radius * 0.32, 0.75);
    float fiber =
        0.5 +
        (0.5 * cos(
            (fiberCoordinate * 6.2831853) +
            (phase * 6.2831853)));
    float grain = DryBrushHash(
        (int2)floor(pixel),
        seed ^ 0x02e5be93u);
    float dryPattern = pow(
        saturate(
            (fiber * 0.82) +
            (grain * 0.18)),
        1.4);
    float3 paperGap = lerp(
        filtered,
        1.0,
        0.3);
    float3 result = lerp(
        filtered,
        paperGap,
        textureStrength * dryPattern);
    return float4(saturate(result) * source.a, source.a);
}
