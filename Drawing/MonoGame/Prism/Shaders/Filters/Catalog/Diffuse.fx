void CatalogDiffuseAccumulateTensor(
    float2 uv,
    float2 offset,
    float2 stepSize,
    float weight,
    int profile,
    inout float3 tensor)
{
    float gradientX = 0.5 * (
        CatalogLuminance(CatalogLinearSample(
            uv + offset + float2(stepSize.x, 0.0),
            profile)) -
        CatalogLuminance(CatalogLinearSample(
            uv + offset - float2(stepSize.x, 0.0),
            profile)));
    float gradientY = 0.5 * (
        CatalogLuminance(CatalogLinearSample(
            uv + offset + float2(0.0, stepSize.y),
            profile)) -
        CatalogLuminance(CatalogLinearSample(
            uv + offset - float2(0.0, stepSize.y),
            profile)));
    tensor += weight * float3(
        gradientX * gradientX,
        gradientX * gradientY,
        gradientY * gradientY);
}

float2 CatalogDiffusePrincipalDirection(
    float3 tensor,
    out float coherence)
{
    float discriminant = sqrt(max(
        ((tensor.x - tensor.z) * (tensor.x - tensor.z)) +
        (4.0 * tensor.y * tensor.y),
        0.0));
    float largest = 0.5 * (
        tensor.x + tensor.z + discriminant);
    float smallest = 0.5 * (
        tensor.x + tensor.z - discriminant);
    coherence = saturate(
        (largest - smallest) /
        max(largest + smallest, 0.000001));
    if (largest <= 0.000001)
    {
        return 0.0;
    }
    if (abs(tensor.y) > 0.000001)
    {
        return normalize(float2(
            largest - tensor.z,
            tensor.y));
    }
    return tensor.x >= tensor.z
        ? float2(1.0, 0.0)
        : float2(0.0, 1.0);
}

uint CatalogDiffuseMode()
{
    uint low = (uint)FilterOptions0.x;
    uint high = (uint)FilterOptions0.y;
    return (low & 0xffffu) | (high << 16);
}

float4 CatalogDiffuse(
    float2 uv,
    float4 center,
    int profile,
    float noise)
{
    const uint darkenOnlyMode = 3227452876u;
    const uint lightenOnlyMode = 1153015394u;
    const uint anisotropicMode = 3481264234u;
    const float epsilon = 0.000001;
    if (center.a <= 0.0)
    {
        return 0.0;
    }

    float2 stepSize = max(
        FilterOptions9.xy * PixelSize,
        PixelSize * epsilon);
    float3 tensor = 0.0;
    CatalogDiffuseAccumulateTensor(
        uv, float2(-stepSize.x, -stepSize.y),
        stepSize, 1.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(0.0, -stepSize.y),
        stepSize, 2.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(stepSize.x, -stepSize.y),
        stepSize, 1.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(-stepSize.x, 0.0),
        stepSize, 2.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, 0.0,
        stepSize, 4.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(stepSize.x, 0.0),
        stepSize, 2.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(-stepSize.x, stepSize.y),
        stepSize, 1.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, float2(0.0, stepSize.y),
        stepSize, 2.0, profile, tensor);
    CatalogDiffuseAccumulateTensor(
        uv, stepSize,
        stepSize, 1.0, profile, tensor);

    float coherence;
    float2 tensorNormal = CatalogDiffusePrincipalDirection(
        tensor,
        coherence);
    float gradientX = 0.5 * (
        CatalogLuminance(CatalogLinearSample(
            uv + float2(stepSize.x, 0.0), profile)) -
        CatalogLuminance(CatalogLinearSample(
            uv - float2(stepSize.x, 0.0), profile)));
    float gradientY = 0.5 * (
        CatalogLuminance(CatalogLinearSample(
            uv + float2(0.0, stepSize.y), profile)) -
        CatalogLuminance(CatalogLinearSample(
            uv - float2(0.0, stepSize.y), profile)));
    uint mode = CatalogDiffuseMode();
    bool anisotropic = mode == anisotropicMode;
    float2 direction = anisotropic
        ? tensorNormal
        : float2(gradientX, gradientY);
    float directionLength = length(direction);
    if (directionLength <= epsilon)
    {
        direction = tensorNormal;
        directionLength = length(direction);
    }
    if (directionLength <= epsilon)
    {
        float fallbackAngle = noise * 6.28318530718;
        direction = float2(
            cos(fallbackAngle),
            sin(fallbackAngle));
    }
    else
    {
        direction /= directionLength;
        if (!anisotropic)
        {
            float jitter =
                (noise - 0.5) *
                (1.0 - coherence) *
                1.57079632679;
            float cosine = cos(jitter);
            float sine = sin(jitter);
            direction = float2(
                (cosine * direction.x) -
                    (sine * direction.y),
                (sine * direction.x) +
                    (cosine * direction.y));
        }
    }

    float4 negative = CatalogLinearSample(
        uv - (direction * stepSize),
        profile);
    float4 positive = CatalogLinearSample(
        uv + (direction * stepSize),
        profile);
    float centerLuminance = CatalogLuminance(center);
    float negativeLuminance = CatalogLuminance(negative);
    float positiveLuminance = CatalogLuminance(positive);
    float secondDerivative =
        negativeLuminance -
        (2.0 * centerLuminance) +
        positiveLuminance;

    float4 darker = center;
    float darkerLuminance = centerLuminance;
    if (negativeLuminance < darkerLuminance)
    {
        darker = negative;
        darkerLuminance = negativeLuminance;
    }
    if (positiveLuminance < darkerLuminance)
    {
        darker = positive;
    }
    float4 lighter = center;
    float lighterLuminance = centerLuminance;
    if (negativeLuminance > lighterLuminance)
    {
        lighter = negative;
        lighterLuminance = negativeLuminance;
    }
    if (positiveLuminance > lighterLuminance)
    {
        lighter = positive;
    }

    float4 target = center;
    if (mode == darkenOnlyMode ||
        (mode != lightenOnlyMode &&
            secondDerivative > epsilon))
    {
        target = darker;
    }
    else if (mode == lightenOnlyMode ||
        secondDerivative < -epsilon)
    {
        target = lighter;
    }
    float timeStep = anisotropic
        ? 0.45 * (0.25 + (0.75 * coherence))
        : 0.45;
    float3 straight = saturate(lerp(
        Unpremultiply(center),
        Unpremultiply(target),
        timeStep));
    return float4(straight * center.a, center.a);
}
