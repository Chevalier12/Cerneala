


sampler2D CharcoalOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 CharcoalClampUv(float2 uv)
{
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 CharcoalRaw(float2 uv)
{
    return tex2D(SpriteTextureSampler, CharcoalClampUv(uv));
}

float4 CharcoalLinearSample(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        CharcoalRaw(uv),
        profile);
}

float4 CharcoalOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(CharcoalOriginalSampler, CharcoalClampUv(uv)),
        profile);
}

float2 CharcoalDecodeTangent(float4 field)
{
    float2 tangent = (field.rg * 2.0) - 1.0;
    float lengthSquared = dot(tangent, tangent);
    return lengthSquared > 0.000001
        ? tangent * rsqrt(lengthSquared)
        : float2(1.0, 0.0);
}

float4 CharcoalInitialEtf(float2 uv, int profile)
{
    float topLeft = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(-1.0, -1.0)), profile));
    float top = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(0.0, -1.0)), profile));
    float topRight = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(1.0, -1.0)), profile));
    float left = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(-1.0, 0.0)), profile));
    float right = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(1.0, 0.0)), profile));
    float bottomLeft = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(-1.0, 1.0)), profile));
    float bottom = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(0.0, 1.0)), profile));
    float bottomRight = CatalogLuminance(CharcoalLinearSample(
        uv + (PixelSize * float2(1.0, 1.0)), profile));
    float2 gradient = float2(
        -topLeft + topRight - (2.0 * left) + (2.0 * right) -
            bottomLeft + bottomRight,
        -topLeft - (2.0 * top) - topRight + bottomLeft +
            (2.0 * bottom) + bottomRight);
    float magnitude = length(gradient);
    float2 tangent = magnitude > 0.00001
        ? float2(-gradient.y, gradient.x) / magnitude
        : float2(1.0, 0.0);
    float alpha = CharcoalLinearSample(uv, profile).a;
    return float4((tangent * 0.5) + 0.5, saturate(magnitude * 0.25), alpha);
}

void CharcoalAccumulateEtf(
    float2 uv,
    float2 center,
    float4 centerField,
    float2 pixelOffset,
    float radius,
    inout float2 sum,
    inout float totalWeight)
{
    float distanceSquared = dot(pixelOffset, pixelOffset);
    if (distanceSquared <= radius * radius)
    {
        float4 neighborField = CharcoalRaw(
            uv + (PixelSize * pixelOffset));
        float2 neighbor = CharcoalDecodeTangent(neighborField);
        float alignment = dot(center, neighbor);
        float spatial = exp(
            -distanceSquared / max(2.0 * radius * radius, 1.0));
        float magnitude = 0.5 *
            (1.0 + tanh((neighborField.b - centerField.b) * 4.0));
        float coverage = exp(
            -abs(neighborField.a - centerField.a) * 8.0);
        float weight = spatial * abs(alignment) * magnitude * coverage;
        sum += neighbor * (alignment < 0.0 ? -weight : weight);
        totalWeight += weight;
    }
}

float4 CharcoalRefineEtf(float2 uv)
{
    float4 centerField = CharcoalRaw(uv);
    float2 center = CharcoalDecodeTangent(centerField);
    float radius = FilterHeader.x == 104.0
        ? 3.0
        : clamp(FilterOptions6.x, 2.0, 4.0);
    float2 sum = center;
    float totalWeight = 1.0;
    float distance = radius * 0.5;
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(distance, 0.0), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(-distance, 0.0), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(0.0, distance), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(0.0, -distance), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(distance, distance), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(-distance, distance), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(distance, -distance), radius, sum, totalWeight);
    CharcoalAccumulateEtf(
        uv, center, centerField, float2(-distance, -distance), radius, sum, totalWeight);

    float sumLength = length(sum);
    float2 tangent = sumLength > 0.00001 ? sum / sumLength : center;
    return float4((tangent * 0.5) + 0.5, centerField.ba);
}

float CharcoalGaussian(float offset, float sigma)
{
    return exp(-(offset * offset) / max(2.0 * sigma * sigma, 0.000001));
}

void CharcoalAccumulateDog(
    float2 uv,
    float2 normal,
    float offset,
    float sigma,
    float extendedSigma,
    int profile,
    inout float narrow,
    inout float broad,
    inout float2 totalWeight)
{
    float luminance = CatalogLuminance(CharcoalOriginal(
        uv + (normal * PixelSize * offset),
        profile));
    float narrowWeight = CharcoalGaussian(offset, sigma);
    float broadWeight = CharcoalGaussian(offset, extendedSigma);
    narrow += luminance * narrowWeight;
    broad += luminance * broadWeight;
    totalWeight += float2(narrowWeight, broadWeight);
}

float4 CharcoalNormalDog(float2 uv, int profile)
{
    float4 field = CharcoalRaw(uv);
    float2 tangent = CharcoalDecodeTangent(field);
    float2 normal = float2(-tangent.y, tangent.x);
    float4 settings = FilterHeader.x == 104.0
        ? FilterOptions8
        : FilterOptions5;
    float sigma = clamp(settings.x, 0.5, 4.0);
    float extendedSigma = clamp(settings.y, 0.75, 6.4);
    float radius = clamp(settings.z, 2.0, 8.0);
    float narrow = 0.0;
    float broad = 0.0;
    float2 totalWeight = 0.0;
    CharcoalAccumulateDog(
        uv, normal, 0.0, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    float step = radius * 0.25;
    CharcoalAccumulateDog(
        uv, normal, step, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, -step, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, step * 2.0, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, step * -2.0, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, step * 3.0, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, step * -3.0, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, radius, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);
    CharcoalAccumulateDog(
        uv, normal, -radius, sigma, extendedSigma, profile,
        narrow, broad, totalWeight);

    float response =
        (narrow / max(totalWeight.x, 0.000001)) -
        ((FilterHeader.x == 104.0 ? 0.98 : FilterOptions6.z) *
            broad / max(totalWeight.y, 0.000001));
    return float4(field.rg, saturate((response * 0.5) + 0.5), field.a);
}

void CharcoalFlowStep(
    inout float2 position,
    inout float2 previous,
    float stepDistance,
    float travelled,
    float sigma,
    inout float response,
    inout float totalWeight)
{
    position = CharcoalClampUv(
        position + (previous * PixelSize * stepDistance));
    float4 nextField = CharcoalRaw(position);
    float2 next = CharcoalDecodeTangent(nextField);
    next *= dot(previous, next) < 0.0 ? -1.0 : 1.0;
    previous = next;
    float weight = CharcoalGaussian(travelled, sigma);
    response += ((nextField.b * 2.0) - 1.0) * weight;
    totalWeight += weight;
}

void CharcoalIntegrateDirection(
    float2 uv,
    float2 tangent,
    float direction,
    float radius,
    float sigma,
    inout float response,
    inout float totalWeight)
{
    float2 position = uv;
    float2 previous = tangent * direction;
    float stepDistance = radius * 0.25;
    CharcoalFlowStep(
        position, previous, stepDistance, stepDistance,
        sigma, response, totalWeight);
    CharcoalFlowStep(
        position, previous, stepDistance, stepDistance * 2.0,
        sigma, response, totalWeight);
    CharcoalFlowStep(
        position, previous, stepDistance, stepDistance * 3.0,
        sigma, response, totalWeight);
    CharcoalFlowStep(
        position, previous, stepDistance, radius,
        sigma, response, totalWeight);
}

float4 CharcoalFlowDog(float2 uv)
{
    float4 centerField = CharcoalRaw(uv);
    float2 centerTangent = CharcoalDecodeTangent(centerField);
    float radius = clamp(
        FilterHeader.x == 104.0
            ? FilterOptions8.w
            : FilterOptions5.w,
        3.0,
        8.0);
    float sigma = max(radius * 0.5, 1.0);
    float response = (centerField.b * 2.0) - 1.0;
    float totalWeight = 1.0;
    CharcoalIntegrateDirection(
        uv, centerTangent, -1.0, radius, sigma, response, totalWeight);
    CharcoalIntegrateDirection(
        uv, centerTangent, 1.0, radius, sigma, response, totalWeight);

    response /= max(totalWeight, 0.000001);
    return float4(
        centerField.rg,
        saturate((response * 0.5) + 0.5),
        centerField.a);
}

float CharcoalHash(float2 coordinate)
{
    float3 value = frac(float3(coordinate.xyx) * 0.1031);
    value += dot(value, value.yzx + 33.33);
    return frac((value.x + value.y) * value.z);
}

float4 CharcoalComposite(float2 uv, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float response = (CharcoalRaw(uv).b * 2.0) - 1.0;
    float detail = saturate(FilterOptions2.x / 10.0);
    float balance = saturate(FilterOptions4.x / 100.0);
    float edgeThreshold = lerp(0.045, 0.012, detail);
    float lineMask = smoothstep(
        edgeThreshold * 0.35,
        edgeThreshold,
        abs(response));
    float luminance = CatalogLuminance(original);
    float toneThreshold = lerp(0.22, 0.82, balance);
    float tone = 1.0 - smoothstep(
        toneThreshold - 0.24,
        toneThreshold + 0.24,
        luminance);
    float grain = 0.72 +
        (CharcoalHash(floor(uv / PixelSize) + float2(17.0, 43.0)) * 0.28);
    float charcoal = saturate(max(
        lineMask,
        tone * lerp(0.3, 0.82, balance) * grain));
    float3 straight = lerp(
        saturate(FilterOptions0.rgb),
        saturate(FilterOptions3.rgb),
        charcoal);
    return float4(straight * original.a, original.a);
}
