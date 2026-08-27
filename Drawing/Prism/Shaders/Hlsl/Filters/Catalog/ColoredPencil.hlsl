#ifndef CERNEALA_SDL_GPU
sampler2D ColoredPencilOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float ColoredPencilHash(float2 pixel)
{
    return frac(
        sin(dot(pixel, float2(12.9898, 78.233))) *
        43758.5453);
}

float2 ColoredPencilGradient(float2 uv, int profile)
{
    float topLeft = CatalogLuminance(
        CatalogLinearSample(uv - PixelSize, profile));
    float top = CatalogLuminance(
        CatalogLinearSample(uv + float2(0.0, -PixelSize.y), profile));
    float topRight = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(PixelSize.x, -PixelSize.y),
            profile));
    float left = CatalogLuminance(
        CatalogLinearSample(uv + float2(-PixelSize.x, 0.0), profile));
    float right = CatalogLuminance(
        CatalogLinearSample(uv + float2(PixelSize.x, 0.0), profile));
    float bottomLeft = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, PixelSize.y),
            profile));
    float bottom = CatalogLuminance(
        CatalogLinearSample(uv + float2(0.0, PixelSize.y), profile));
    float bottomRight = CatalogLuminance(
        CatalogLinearSample(uv + PixelSize, profile));
    return float2(
        -topLeft + topRight -
            (2.0 * left) + (2.0 * right) -
            bottomLeft + bottomRight,
        -topLeft - (2.0 * top) - topRight +
            bottomLeft + (2.0 * bottom) + bottomRight) *
        0.25;
}

float4 ColoredPencilTensor(float2 uv, int profile)
{
    float2 gradient = ColoredPencilGradient(uv, profile);
    return float4(
        saturate(gradient.x * gradient.x),
        saturate((gradient.x * gradient.y * 0.5) + 0.5),
        saturate(gradient.y * gradient.y),
        1.0);
}

float4 ColoredPencilTensorSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5)));
}

float4 ColoredPencilBlur(float2 uv, bool horizontal)
{
    float radius = clamp(
        horizontal ? FilterOptions9.x : FilterOptions9.y,
        1.0,
        4.0);
    float sigma = max(radius * 0.5, 0.75);
    float divisor = 2.0 * sigma * sigma;
    float4 sum = 0.0;
    float total = 0.0;
    for (int offset = -4; offset <= 4; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float weight = exp(
                -(offset * offset) / divisor);
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            sum += ColoredPencilTensorSample(uv + delta) *
                weight;
            total += weight;
        }
    }
    return sum / max(total, 0.000001);
}

float3 ColoredPencilDecodeTensor(float4 encoded)
{
    return float3(
        encoded.x,
        (encoded.y - 0.5) * 2.0,
        encoded.z);
}

float ColoredPencilCoherence(float3 tensor)
{
    float difference = tensor.x - tensor.z;
    float discriminant = sqrt(
        max(
            (difference * difference) +
                (4.0 * tensor.y * tensor.y),
            0.0));
    return saturate(
        discriminant /
        max(tensor.x + tensor.z, 0.000001));
}

float2 ColoredPencilTangent(
    float3 tensor,
    float2 pixel)
{
    if (ColoredPencilCoherence(tensor) < 0.02)
    {
        float flatAngle =
            (ColoredPencilHash(floor(pixel / 8.0)) - 0.5) *
            3.14159265;
        return float2(cos(flatAngle), sin(flatAngle));
    }

    float gradientAngle = 0.5 * atan2(
        2.0 * tensor.y,
        tensor.x - tensor.z);
    return float2(
        -sin(gradientAngle),
        cos(gradientAngle));
}

float4 ColoredPencilOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            ColoredPencilOriginalSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}

void ColoredPencilAccumulatePair(
    float2 uv,
    int profile,
    float2 tangent,
    float2 normal,
    float distance,
    float radius,
    float phase,
    float coherence,
    float centerLuminance,
    float centerAlpha,
    float edgeStop,
    inout float3 accumulated,
    inout float totalWeight)
{
    float swing =
        sin((distance + phase) * 1.7) *
        (1.0 - coherence) *
        0.35;
    float2 pathOffset =
        ((tangent * distance) + (normal * swing)) *
        PixelSize;
    float4 forward = ColoredPencilOriginal(
        uv + pathOffset,
        profile);
    float4 backward = ColoredPencilOriginal(
        uv - pathOffset,
        profile);
    float3 forwardColor =
        saturate(Unpremultiply(forward));
    float3 backwardColor =
        saturate(Unpremultiply(backward));
    float forwardDelta = abs(
        dot(
            forwardColor,
            float3(0.2126, 0.7152, 0.0722)) -
        centerLuminance);
    float backwardDelta = abs(
        dot(
            backwardColor,
            float3(0.2126, 0.7152, 0.0722)) -
        centerLuminance);
    float spatial = distance / max(radius, 1.0);
    float spatialWeight = exp(
        -2.0 * spatial * spatial);
    float forwardWeight =
        spatialWeight *
        exp(-forwardDelta / edgeStop) *
        exp(-abs(forward.a - centerAlpha) * 8.0) *
        step(0.000001, forward.a);
    float backwardWeight =
        spatialWeight *
        exp(-backwardDelta / edgeStop) *
        exp(-abs(backward.a - centerAlpha) * 8.0) *
        step(0.000001, backward.a);
    accumulated +=
        (forwardColor * forwardWeight) +
        (backwardColor * backwardWeight);
    totalWeight += forwardWeight + backwardWeight;
}

float4 ColoredPencilComposite(float2 uv, int profile)
{
    float4 center = ColoredPencilOriginal(uv, profile);
    if (center.a <= 0.0)
    {
        return 0.0;
    }

    float2 pixel = uv / PixelSize;
    float3 centerColor = saturate(Unpremultiply(center));
    float centerLuminance = dot(
        centerColor,
        float3(0.2126, 0.7152, 0.0722));
    float3 centerTensor = ColoredPencilDecodeTensor(
        ColoredPencilTensorSample(uv));
    float2 tangent = ColoredPencilTangent(
        centerTensor,
        pixel);
    float radius = clamp(FilterOptions9.x, 0.0, 12.0);
    float pressure = saturate(FilterOptions1.x / 16.0);
    float edgeStop = 0.08 + ((1.0 - pressure) * 0.12);
    float3 accumulated = centerColor;
    float totalWeight = 1.0;

    float coherence = ColoredPencilCoherence(centerTensor);
    float phase = ColoredPencilHash(floor(pixel / 4.0));
    float2 normal = float2(-tangent.y, tangent.x);
    if (radius > 0.0)
    {
        ColoredPencilAccumulatePair(
            uv,
            profile,
            tangent,
            normal,
            radius / 3.0,
            radius,
            phase,
            coherence,
            centerLuminance,
            center.a,
            edgeStop,
            accumulated,
            totalWeight);
        ColoredPencilAccumulatePair(
            uv,
            profile,
            tangent,
            normal,
            radius * (2.0 / 3.0),
            radius,
            phase,
            coherence,
            centerLuminance,
            center.a,
            edgeStop,
            accumulated,
            totalWeight);
        ColoredPencilAccumulatePair(
            uv,
            profile,
            tangent,
            normal,
            radius,
            radius,
            phase,
            coherence,
            centerLuminance,
            center.a,
            edgeStop,
            accumulated,
            totalWeight);
    }

    float3 licColor = accumulated /
        max(totalWeight, 0.000001);
    float licLuminance = dot(
        licColor,
        float3(0.2126, 0.7152, 0.0722));
    float tensorEnergy = saturate(
        sqrt(max(centerTensor.x + centerTensor.z, 0.0)));
    float paperBrightness = saturate(FilterOptions2.x);
    float3 paperColor = saturate(
        FilterOptions3.rgb *
        (0.75 + (0.25 * paperBrightness)));
    float coverage = saturate(
        ((1.0 - licLuminance) *
            (0.45 + (0.9 * pressure))) +
        (tensorEnergy *
            (0.2 + (0.3 * pressure))));
    float pencilWidth = clamp(
        FilterOptions0.x,
        0.0,
        12.0);
    float lineCoordinate =
        dot(pixel, float2(-tangent.y, tangent.x)) /
        max(0.75, pencilWidth * 0.4);
    float strokePattern =
        0.82 +
        (0.18 *
            (0.5 +
                (0.5 * cos(
                    (lineCoordinate * 6.2831853) +
                    (ColoredPencilHash(
                        floor(pixel / 3.0)) *
                        3.14159265)))));
    float grain =
        0.88 + (0.12 * ColoredPencilHash(pixel));
    coverage *= strokePattern * grain;
    float3 pigment = saturate(
        licColor *
        (0.3 + (0.55 * licLuminance)));
    float3 result = lerp(
        paperColor,
        pigment,
        saturate(coverage));
    return float4(result * center.a, center.a);
}
