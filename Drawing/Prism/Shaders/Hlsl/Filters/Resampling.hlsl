#ifndef CERNEALA_SDL_GPU
sampler2D PolarTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Clamp;
};

sampler2D NeonPyramidSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float ResamplingInside(float2 uv)
{
    return
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
}

float2 ResamplingMirror(float2 uv)
{
    return 1.0 - abs((frac(uv * 0.5) * 2.0) - 1.0);
}

float4 ClampResamplingAssociated(float4 color)
{
    color.a = saturate(color.a);
    color.rgb = clamp(color.rgb, 0.0, color.a);
    return color;
}

float4 SampleResamplingSource(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float inside = ResamplingInside(uv);
    if (edgeMode == 1 && inside < 0.5)
    {
        return 0.0;
    }
    if (edgeMode == 4 && inside < 0.5)
    {
        float4 associatedFill = float4(
            fillColor.rgb * fillColor.a,
            fillColor.a);
        return WorkingAssociatedToLinearSrgb(
            associatedFill,
            profile);
    }
    if (edgeMode == 2)
    {
        uv = frac(uv);
    }
    else if (edgeMode == 3)
    {
        uv = ResamplingMirror(uv);
    }
    else
    {
        uv = clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5));
    }

    return WorkingAssociatedToLinearSrgb(
        tex2D(SpriteTextureSampler, uv),
        profile);
}

float ResamplingCubicWeight(float distance)
{
    const float coefficient = -0.75;
    float absolute = abs(distance);
    if (absolute <= 1.0)
    {
        return
            ((coefficient + 2.0) *
                absolute *
                absolute *
                absolute) -
            ((coefficient + 3.0) *
                absolute *
                absolute) +
            1.0;
    }
    if (absolute < 2.0)
    {
        return
            (coefficient *
                absolute *
                absolute *
                absolute) -
            (5.0 * coefficient *
                absolute *
                absolute) +
            (8.0 * coefficient * absolute) -
            (4.0 * coefficient);
    }
    return 0.0;
}

float4 SampleResamplingCubicTap(
    float2 uv,
    int profile,
    int edgeMode)
{
    if (edgeMode == 2)
    {
        uv = frac(uv);
    }
    else if (edgeMode == 3)
    {
        uv = ResamplingMirror(uv);
    }
    else
    {
        uv = clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5));
    }
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            SpriteTextureSampler,
            float4(uv, 0.0, 0.0)),
        profile);
}

float4 SampleResamplingCubic(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fallbackSample)
{
    float inside = ResamplingInside(uv);
    float useFallback =
        (edgeMode == 1 || edgeMode == 4) &&
        inside < 0.5
            ? 1.0
            : 0.0;

    float2 sourceSize =
        1.0 / max(PixelSize, 0.000001);
    float2 position =
        (uv * sourceSize) - 0.5;
    float2 basePosition = floor(position);
    float2 fraction = position - basePosition;
    float4 total = float4(
        0.0,
        0.0,
        0.0,
        0.0);
    [unroll]
    for (int tapY = 0; tapY < 4; tapY++)
    {
        float offsetY = tapY - 1.0;
        float weightY = ResamplingCubicWeight(
            offsetY - fraction.y);
        float4 row = float4(
            0.0,
            0.0,
            0.0,
            0.0);
        [unroll]
        for (int tapX = 0; tapX < 4; tapX++)
        {
            float offsetX = tapX - 1.0;
            float weightX = ResamplingCubicWeight(
                offsetX - fraction.x);
            float2 tapUv =
                (basePosition +
                    float2(offsetX, offsetY) +
                    0.5) *
                PixelSize;
            row += SampleResamplingCubicTap(
                    tapUv,
                    profile,
                    edgeMode) *
                weightX;
        }
        total += row * weightY;
    }
    return lerp(
        total,
        fallbackSample,
        useFallback);
}

float2 MapLiquifyCoordinate(float2 uv)
{
    float4 mesh = tex2D(
        SecondaryTextureSampler,
        uv);
    float2 displacement =
        (mesh.rg * 2.0) - 1.0;
    float mask = FilterOptions6.x > 0.5
        ? tex2D(
            FilterAuxiliaryTextureSampler,
            uv).a
        : 1.0;
    mask = lerp(
        mask,
        1.0 - mask,
        step(0.5, FilterOptions0.y));
    return
        uv -
        (displacement *
            (1.0 - saturate(FilterOptions0.x)) *
            mask);
}

float LiquifyCubicConfidence(float2 uv)
{
    float2 safePixelSize =
        max(PixelSize, 0.000001);
    float2 leftUv = clamp(
        uv - float2(safePixelSize.x, 0.0),
        0.0,
        1.0);
    float2 rightUv = clamp(
        uv + float2(safePixelSize.x, 0.0),
        0.0,
        1.0);
    float2 topUv = clamp(
        uv - float2(0.0, safePixelSize.y),
        0.0,
        1.0);
    float2 bottomUv = clamp(
        uv + float2(0.0, safePixelSize.y),
        0.0,
        1.0);
    float spanX = max(
        (rightUv.x - leftUv.x) /
            safePixelSize.x,
        0.000001);
    float spanY = max(
        (bottomUv.y - topUv.y) /
            safePixelSize.y,
        0.000001);
    float2 derivativeX =
        ((MapLiquifyCoordinate(rightUv) -
            MapLiquifyCoordinate(leftUv)) /
            safePixelSize) /
        spanX;
    float2 derivativeY =
        ((MapLiquifyCoordinate(bottomUv) -
            MapLiquifyCoordinate(topUv)) /
            safePixelSize) /
        spanY;
    float determinant =
        (derivativeX.x * derivativeY.y) -
        (derivativeX.y * derivativeY.x);
    float maximumAxis = max(
        length(derivativeX),
        length(derivativeY));
    float orientationConfidence =
        smoothstep(0.05, 0.25, determinant);
    float footprintConfidence =
        1.0 -
        smoothstep(2.0, 4.0, maximumAxis);
    return saturate(
        orientationConfidence *
        footprintConfidence);
}

float2 MapPolarCoordinate(float2 uv)
{
    float2 center = FilterOptions0.yz;
    float2 sourceSize = 1.0 / max(PixelSize, 0.000001);
    float2 centerPixels = center * sourceSize;
    float2 cornerDistance = max(
        centerPixels,
        sourceSize - centerPixels);
    float maximumRadius = max(
        length(cornerDistance),
        0.000001);
    if (FilterOptions0.x < 0.5)
    {
        float angle =
            (uv.x - center.x) * 6.28318531;
        float radius =
            (uv.y - center.y + 0.5) *
            maximumRadius;
        float2 mappedPixels =
            centerPixels + float2(
                cos(angle),
                sin(angle)) * radius;
        return mappedPixels / sourceSize;
    }

    float2 deltaPixels =
        (uv - center) * sourceSize;
    return float2(
        center.x +
            (atan2(deltaPixels.y, deltaPixels.x) /
                6.28318531),
        center.y - 0.5 +
            (length(deltaPixels) / maximumRadius));
}

float4 SamplePolarSource(
    float2 uv,
    int profile)
{
    float4 result = 0.0;
    if (FilterOptions0.x < 0.5)
    {
        result = SampleResamplingSource(
            uv,
            profile,
            1,
            0.0);
    }
    else if (uv.y >= 0.0 && uv.y <= 1.0)
    {
        uv.x = frac(uv.x);
        result = WorkingAssociatedToLinearSrgb(
            tex2D(PolarTextureSampler, uv),
            profile);
    }
    return result;
}

void PolarJacobian(
    float2 uv,
    out float2 derivativeX,
    out float2 derivativeY)
{
    float2 center = FilterOptions0.yz;
    float2 sourceSize = 1.0 / max(PixelSize, 0.000001);
    float2 centerPixels = center * sourceSize;
    float2 cornerDistance = max(
        centerPixels,
        sourceSize - centerPixels);
    float maximumRadius = max(
        length(cornerDistance),
        0.000001);
    if (FilterOptions0.x < 0.5)
    {
        float angle =
            (uv.x - center.x) * 6.28318531;
        float radius =
            (uv.y - center.y + 0.5) *
            maximumRadius;
        float2 direction = float2(
            cos(angle),
            sin(angle));
        float2 tangent = float2(
            -direction.y,
            direction.x);
        derivativeX =
            tangent * radius * 6.28318531 *
            PixelSize.x;
        derivativeY =
            direction * maximumRadius *
            PixelSize.y;
        return;
    }

    float2 deltaPixels =
        (uv - center) * sourceSize;
    float radiusSquared =
        dot(deltaPixels, deltaPixels);
    float radialScale =
        sourceSize.y / maximumRadius;
    if (radiusSquared < 0.000001)
    {
        derivativeX = float2(0.0, radialScale);
        derivativeY = float2(
            sourceSize.x * 0.25,
            radialScale);
        return;
    }

    float radius = sqrt(radiusSquared);
    float angularScale =
        sourceSize.x /
        (6.28318531 * radiusSquared);
    derivativeX = float2(
        -deltaPixels.y * angularScale,
        deltaPixels.x * radialScale / radius);
    derivativeY = float2(
        deltaPixels.x * angularScale,
        deltaPixels.y * radialScale / radius);
}

float4 SamplePolarEwa(
    float2 uv,
    float2 mapped,
    int profile)
{
    float2 sourceSize = 1.0 / max(PixelSize, 0.000001);
    float2 derivativeX;
    float2 derivativeY;
    PolarJacobian(
        uv,
        derivativeX,
        derivativeY);
    float covarianceX =
        (derivativeX.x * derivativeX.x) +
        (derivativeY.x * derivativeY.x);
    float covarianceY =
        (derivativeX.y * derivativeX.y) +
        (derivativeY.y * derivativeY.y);
    float covarianceCross =
        (derivativeX.x * derivativeX.y) +
        (derivativeY.x * derivativeY.y);
    float trace = covarianceX + covarianceY;
    float difference = covarianceX - covarianceY;
    float discriminant = sqrt(max(
        (difference * difference) +
            (4.0 * covarianceCross * covarianceCross),
        0.0));
    float majorEigenvalue = max(
        (trace + discriminant) * 0.5,
        0.0);
    float minorEigenvalue = max(
        (trace - discriminant) * 0.5,
        0.0);
    float majorLength = sqrt(majorEigenvalue);
    float minorLength = max(
        sqrt(minorEigenvalue),
        1.0);
    float4 result = SamplePolarSource(
        mapped,
        profile);
    if (majorLength <= 1.0)
    {
        return result;
    }

    minorLength = max(
        minorLength,
        majorLength / 8.0);
    float2 majorDirection;
    if (abs(covarianceCross) > 0.000001)
    {
        majorDirection = normalize(float2(
            covarianceCross,
            majorEigenvalue - covarianceX));
    }
    else
    {
        majorDirection = covarianceX >= covarianceY
            ? float2(1.0, 0.0)
            : float2(0.0, 1.0);
    }
    float2 minorDirection = float2(
        -majorDirection.y,
        majorDirection.x);
    float2 majorAxis =
        majorDirection * majorLength * PixelSize;
    float2 minorAxis =
        minorDirection * minorLength * PixelSize;
    const float innerRadius = 0.2;
    const float outerComponent = 0.31819805;
    const float innerWeight = 0.92311635;
    const float outerWeight = 0.66697681;
    const float totalWeight =
        4.0 * (innerWeight + outerWeight);
    float4 total =
        SamplePolarSource(
            mapped + (majorAxis * innerRadius),
            profile) *
        innerWeight;
    total += SamplePolarSource(
        mapped - (majorAxis * innerRadius),
        profile) * innerWeight;
    total += SamplePolarSource(
        mapped + (minorAxis * innerRadius),
        profile) * innerWeight;
    total += SamplePolarSource(
        mapped - (minorAxis * innerRadius),
        profile) * innerWeight;
    total += SamplePolarSource(
        mapped +
            ((majorAxis + minorAxis) * outerComponent),
        profile) * outerWeight;
    total += SamplePolarSource(
        mapped +
            ((majorAxis - minorAxis) * outerComponent),
        profile) * outerWeight;
    total += SamplePolarSource(
        mapped +
            ((-majorAxis + minorAxis) * outerComponent),
        profile) * outerWeight;
    total += SamplePolarSource(
        mapped -
            ((majorAxis + minorAxis) * outerComponent),
        profile) * outerWeight;
    result = total / totalWeight;
    return result;
}

float ResamplingChannel(float4 sample, int channel)
{
    if (channel == 0)
    {
        return sample.r;
    }
    if (channel == 1)
    {
        return sample.g;
    }
    if (channel == 2)
    {
        return sample.b;
    }
    if (channel == 3)
    {
        return sample.a;
    }
    return dot(
        sample.rgb,
        float3(0.2126, 0.7152, 0.0722));
}

float GlassValueNoise(float2 coordinate)
{
    float2 cell = floor(coordinate);
    float2 fraction = frac(coordinate);
    float2 blend =
        fraction * fraction * (3.0 - (2.0 * fraction));
    float top = lerp(
        NeighborhoodHash(cell, 1779033703.0),
        NeighborhoodHash(cell + float2(1.0, 0.0), 1779033703.0),
        blend.x);
    float bottom = lerp(
        NeighborhoodHash(cell + float2(0.0, 1.0), 1779033703.0),
        NeighborhoodHash(cell + 1.0, 1779033703.0),
        blend.x);
    return lerp(top, bottom, blend.y);
}

uint OceanHash(int2 cell, uint seed)
{
    uint hash =
        (asuint(cell.x) * 0x8da6b343u) ^
        (asuint(cell.y) * 0xd8163841u) ^
        seed;
    hash ^= hash >> 16;
    hash *= 0x7feb352du;
    hash ^= hash >> 15;
    hash *= 0x846ca68bu;
    return hash ^ (hash >> 16);
}

float2 OceanGradient(uint hash)
{
    uint index = hash & 7u;
    if (index == 0u)
    {
        return float2(1.0, 0.0);
    }
    if (index == 1u)
    {
        return float2(-1.0, 0.0);
    }
    if (index == 2u)
    {
        return float2(0.0, 1.0);
    }
    if (index == 3u)
    {
        return float2(0.0, -1.0);
    }
    if (index == 4u)
    {
        return float2(0.70710678, 0.70710678);
    }
    if (index == 5u)
    {
        return float2(-0.70710678, 0.70710678);
    }
    if (index == 6u)
    {
        return float2(0.70710678, -0.70710678);
    }
    return float2(-0.70710678, -0.70710678);
}

float OceanSimplexCorner(
    float2 offset,
    int2 cell,
    uint seed)
{
    float attenuation = 0.5 - dot(offset, offset);
    if (attenuation <= 0.0)
    {
        return 0.0;
    }

    attenuation *= attenuation;
    return attenuation * attenuation *
        dot(OceanGradient(OceanHash(cell, seed)), offset);
}

float OceanSimplex(float2 position, uint seed)
{
    const float skew = 0.3660254037844386;
    const float unskew = 0.2113248654051871;
    float skewed = (position.x + position.y) * skew;
    int2 cell = (int2)floor(position + skewed);
    float cellOrigin = (cell.x + cell.y) * unskew;
    float2 first = position -
        (float2(cell) - cellOrigin);
    int2 middleCell =
        first.x > first.y ? int2(1, 0) : int2(0, 1);
    float2 middle =
        first - float2(middleCell) + unskew;
    float2 last = first - 1.0 + (2.0 * unskew);

    return 70.0 * (
        OceanSimplexCorner(first, cell, seed) +
        OceanSimplexCorner(
            middle,
            cell + middleCell,
            seed) +
        OceanSimplexCorner(last, cell + 1, seed));
}

float2 OceanWarpVector(float2 position, uint seed)
{
    return float2(
        OceanSimplex(position, seed),
        OceanSimplex(
            float2(
                position.y + 19.19,
                -position.x + 7.73),
            seed ^ 0x9e3779b9u));
}

float GlassHeight(float2 pixelPosition, int textureKind)
{
    float scaling = max(abs(FilterOptions0.w), 0.05);
    if (textureKind == 4)
    {
        if (FilterHeader.w <= 0.5)
        {
            return 0.5;
        }

        float2 mapUv =
            (((pixelPosition * PixelSize) - 0.5) / scaling) +
            0.5;
        float4 sample = tex2D(
            SecondaryTextureSampler,
            mapUv);
        return saturate(dot(
            sample.rgb,
            float3(0.2126, 0.7152, 0.0722)));
    }

    float featureSize = textureKind == 1
        ? 5.0
        : (textureKind == 2
            ? 18.0
            : (textureKind == 3 ? 8.0 : 7.0));
    float2 coordinate =
        pixelPosition / (featureSize * scaling);
    float2 local = frac(coordinate) - 0.5;
    if (textureKind == 1)
    {
        return sqrt(saturate(
            1.0 - (4.0 * dot(local, local))));
    }
    if (textureKind == 2)
    {
        return saturate(
            1.0 -
            (2.0 * max(abs(local.x), abs(local.y))));
    }
    if (textureKind == 3)
    {
        return saturate(
            0.5 +
            (0.25 * sin(coordinate.x * 6.28318531)) +
            (0.25 * sin(coordinate.y * 6.28318531)));
    }
    return GlassValueNoise(coordinate);
}

float ResamplingWaveSinc(float value)
{
    return abs(value) < 0.0001
        ? 1.0
        : sin(value) / value;
}

float ResamplingWaveHash(
    uint seed,
    int generator,
    int channel)
{
    uint value =
        seed ^
        ((uint)(generator + 1) * 0x9e3779b9u) ^
        ((uint)(channel + 1) * 0x85ebca6bu);
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (float)(value & 0x00ffffffu) /
        16777216.0;
}

float ResamplingBandLimitedWave(
    float phase,
    float2 phaseWidth,
    int kind,
    out float derivative)
{
    float wrapped = frac(phase);
    float maximumWidth = max(
        abs(phaseWidth.x),
        abs(phaseWidth.y));
    derivative = 0.0;
    if (kind == 0)
    {
        if (maximumWidth > 0.5)
        {
            return 0.0;
        }

        float attenuation =
            ResamplingWaveSinc(
                3.14159265 * phaseWidth.x) *
            ResamplingWaveSinc(
                3.14159265 * phaseWidth.y);
        float angle = wrapped * 6.28318531;
        derivative =
            6.28318531 *
            cos(angle) *
            attenuation;
        return sin(angle) * attenuation;
    }

    float value = 0.0;
    [unroll]
    for (int term = 0; term < 8; term++)
    {
        int harmonic = (term * 2) + 1;
        float harmonicWidth =
            harmonic * maximumWidth;
        if (harmonicWidth > 0.5)
        {
            break;
        }

        float attenuation =
            ResamplingWaveSinc(
                3.14159265 *
                harmonic *
                phaseWidth.x) *
            ResamplingWaveSinc(
                3.14159265 *
                harmonic *
                phaseWidth.y);
        float angle =
            wrapped *
            harmonic *
            6.28318531;
        if (kind == 1)
        {
            float coefficient =
                8.0 /
                (3.14159265 *
                    3.14159265 *
                    harmonic *
                    harmonic);
            value +=
                coefficient *
                cos(angle) *
                attenuation;
            derivative -=
                coefficient *
                6.28318531 *
                harmonic *
                sin(angle) *
                attenuation;
        }
        else
        {
            float coefficient =
                -4.0 /
                (3.14159265 * harmonic);
            value +=
                coefficient *
                sin(angle) *
                attenuation;
            derivative +=
                coefficient *
                6.28318531 *
                harmonic *
                cos(angle) *
                attenuation;
        }
    }
    return value;
}

void WaveJacobian(
    float2 uv,
    out float2 mapped,
    out float2 derivativeX,
    out float2 derivativeY)
{
    uint seed =
        (uint)(FilterOptions2.y + 0.5) |
        ((uint)(FilterOptions2.z + 0.5) << 16);
    int generators = clamp(
        (int)(FilterOptions0.x + 0.5),
        1,
        32);
    int kind = (int)(FilterOptions0.w + 0.5);
    float2 sourceSize =
        1.0 / max(PixelSize, 0.000001);
    float2 displacement = 0.0;
    float2 displacementDerivativeX = 0.0;
    float2 displacementDerivativeY = 0.0;
    float2 pixelPosition = uv * sourceSize;
    [loop]
    for (int generator = 0; generator < 32; generator++)
    {
        if (generator >= generators)
        {
            break;
        }

        float directionAngle =
            ResamplingWaveHash(
                seed,
                generator,
                0) *
            6.28318531;
        float2 direction = float2(
            cos(directionAngle),
            sin(directionAngle));
        float wavelength = lerp(
            FilterOptions0.y,
            FilterOptions0.z,
            ResamplingWaveHash(seed, generator, 1));
        float amplitude = lerp(
            FilterOptions1.x,
            FilterOptions1.y,
            ResamplingWaveHash(seed, generator, 2));
        float waveDerivative;
        float wave = ResamplingBandLimitedWave(
            (dot(pixelPosition, direction) /
                wavelength) +
                ResamplingWaveHash(
                    seed,
                    generator,
                    3),
            direction / wavelength,
            kind,
            waveDerivative);
        float2 displacementDirection =
            direction * amplitude;
        displacement +=
            displacementDirection * wave;
        displacementDerivativeX +=
            displacementDirection *
            waveDerivative *
            direction.x /
            wavelength;
        displacementDerivativeY +=
            displacementDirection *
            waveDerivative *
            direction.y /
            wavelength;
    }

    float normalization =
        rsqrt((float)generators);
    float scaleX =
        FilterOptions1.z * normalization;
    float scaleY =
        FilterOptions1.w * normalization;
    mapped = uv + float2(
        displacement.x * scaleX * PixelSize.x,
        displacement.y * scaleY * PixelSize.y);
    derivativeX = float2(
        1.0 +
            (displacementDerivativeX.x * scaleX),
        displacementDerivativeX.y * scaleY);
    derivativeY = float2(
        displacementDerivativeY.x * scaleX,
        1.0 +
            (displacementDerivativeY.y * scaleY));
}

float ShearHermite(
    float start,
    float end,
    float startTangent,
    float endTangent,
    float position)
{
    float position2 = position * position;
    float position3 = position2 * position;
    return
        (((2.0 * position3) - (3.0 * position2)) + 1.0) *
            start +
        (position3 - (2.0 * position2) + position) *
            startTangent +
        ((-2.0 * position3) + (3.0 * position2)) *
            end +
        (position3 - position2) * endTangent;
}

float ShearCurve(float y, int curve)
{
    float3 slopes = float3(1.0, 1.0, 1.0);
    if (curve == 1)
    {
        slopes = float3(0.0, 1.0, 2.0);
    }
    else if (curve == 2)
    {
        slopes = float3(2.0, 1.0, 0.0);
    }
    else if (curve == 3)
    {
        slopes = float3(0.0, 1.0, 0.0);
    }
    else if (curve >= 4)
    {
        slopes = float3(0.0, 2.0, 0.0);
    }

    return y <= 0.5
        ? ShearHermite(
            0.0,
            0.5,
            slopes.x * 0.5,
            slopes.y * 0.5,
            y * 2.0)
        : ShearHermite(
            0.5,
            1.0,
            slopes.y * 0.5,
            slopes.z * 0.5,
            (y - 0.5) * 2.0);
}

float LensSafeScale(float value)
{
    return abs(value) < 0.0001
        ? (value < 0.0 ? -0.0001 : 0.0001)
        : value;
}

float2 TiltLensCoordinate(
    float2 coordinate,
    float vertical,
    float horizontal)
{
    float clampedHorizontal = clamp(horizontal, -64.0, 64.0);
    float horizontalScale = rsqrt(
        1.0 +
        (clampedHorizontal * clampedHorizontal));
    float x =
        (coordinate.x + clampedHorizontal) *
        horizontalScale;
    float z =
        (1.0 -
            (clampedHorizontal * coordinate.x)) *
        horizontalScale;
    float clampedVertical = clamp(vertical, -64.0, 64.0);
    float verticalScale = rsqrt(
        1.0 +
        (clampedVertical * clampedVertical));
    float y =
        (coordinate.y -
            (clampedVertical * z)) *
        verticalScale;
    z =
        ((clampedVertical * coordinate.y) + z) *
        verticalScale;
    float safeZ = abs(z) < 0.000001
        ? (z < 0.0 ? -0.000001 : 0.000001)
        : z;
    return float2(x / safeZ, y / safeZ);
}

float2 MapLensCorrectionCoordinate(
    float2 uv,
    float chromaticShift)
{
    float aspect = PixelSize.y / max(PixelSize.x, 0.000001);
    float2 centered =
        (uv - 0.5) *
        float2(aspect, 1.0);
    float angle = -FilterOptions1.w;
    float cosine = cos(angle);
    float sine = sin(angle);
    centered = float2(
        (centered.x * cosine) -
            (centered.y * sine),
        (centered.x * sine) +
            (centered.y * cosine));
    centered /= LensSafeScale(FilterOptions2.x);
    centered = TiltLensCoordinate(
        centered,
        FilterOptions1.y,
        FilterOptions1.z);
    float radiusSquared = dot(centered, centered);
    float radial = 1.0 +
        (clamp(
            FilterOptions0.x + chromaticShift,
            -4.0,
            4.0) *
            radiusSquared);
    centered *= radial;
    return float2(
        (centered.x / aspect) + 0.5,
        centered.y + 0.5);
}

float LensVignetteFactor(float2 uv)
{
    float amount = clamp(FilterOptions0.w, -4.0, 4.0);
    if (amount == 0.0)
    {
        return 1.0;
    }

    float aspect = PixelSize.y / max(PixelSize.x, 0.000001);
    float2 centered =
        (uv - 0.5) *
        float2(aspect, 1.0);
    float cornerRadius = sqrt(
        ((aspect * aspect) + 1.0) * 0.25);
    float radius = saturate(length(centered) / cornerRadius);
    float midpoint = saturate(FilterOptions1.x);
    float edge = smoothstep(midpoint, 1.0, radius);
    return max(0.0, 1.0 - (amount * edge));
}

float2 MapSpherizeCoordinate(float2 uv)
{
    float2 center = FilterOptions0.zw;
    float2 delta = uv - center;
    float2 normalized = delta * 2.0;
    int mode = (int)(FilterOptions0.y + 0.5);
    if (mode == 1)
    {
        normalized.y = 0.0;
    }
    else if (mode == 2)
    {
        normalized.x = 0.0;
    }

    float radius = length(normalized);
    float amount = clamp(FilterOptions0.x, -1.0, 1.0);
    if (radius <= 0.000001 ||
        radius >= 1.0 ||
        amount == 0.0)
    {
        return uv;
    }


    float mappedRadius = amount > 0.0
        ? lerp(
            radius,
            asin(radius) * 0.6366197723675814,
            amount)
        : lerp(
            radius,
            sin(radius * 1.5707963267948966),
            -amount);
    float scale = mappedRadius / radius;
    if (mode == 1)
    {
        delta.x *= scale;
    }
    else if (mode == 2)
    {
        delta.y *= scale;
    }
    else
    {
        delta *= scale;
    }
    return center + delta;
}

void TwirlJacobian(
    float2 uv,
    out float2 mapped,
    out float2 derivativeX,
    out float2 derivativeY)
{
    float2 center = FilterOptions0.yz;
    float2 delta = uv - center;
    float deltaLength = length(delta);
    float radius = deltaLength / 0.70710678;
    float angle =
        -FilterOptions0.x *
        saturate(1.0 - radius);
    float cosine = cos(angle);
    float sine = sin(angle);
    mapped = center + float2(
        (delta.x * cosine) -
            (delta.y * sine),
        (delta.x * sine) +
            (delta.y * cosine));
    if (radius >= 1.0)
    {
        derivativeX = float2(1.0, 0.0);
        derivativeY = float2(0.0, 1.0);
        return;
    }

    float2 sourceSize =
        1.0 / max(PixelSize, 0.000001);
    float2 tangent = float2(
        -(mapped.y - center.y),
        mapped.x - center.x);
    float2 radialDirection = deltaLength > 0.000001
        ? delta / deltaLength
        : 0.0;
    float boundedAngle = clamp(
        FilterOptions0.x,
        -65536.0,
        65536.0);
    float2 angleGradient =
        radialDirection *
        (boundedAngle / 0.70710678);
    float2 rotatedStepX = float2(
        PixelSize.x * cosine,
        PixelSize.x * sine);
    float2 rotatedStepY = float2(
        -PixelSize.y * sine,
        PixelSize.y * cosine);
    derivativeX =
        (rotatedStepX +
            (tangent *
                dot(
                    angleGradient,
                    float2(PixelSize.x, 0.0)))) *
        sourceSize;
    derivativeY =
        (rotatedStepY +
            (tangent *
                dot(
                    angleGradient,
                    float2(0.0, PixelSize.y)))) *
        sourceSize;
}

float4 SampleTwirlTap(
    float2 mapped,
    float2 axis,
    float position,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float weight = exp(
        -2.0 * position * position);
    return SampleResamplingSource(
        mapped + (axis * position),
        profile,
        edgeMode,
        fillColor) * weight;
}

float4 SampleTwirlFeline(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float2 mapped = uv;
    float2 derivativeX = float2(1.0, 0.0);
    float2 derivativeY = float2(0.0, 1.0);
    TwirlJacobian(
        uv,
        mapped,
        derivativeX,
        derivativeY);
    float covarianceX =
        (derivativeX.x * derivativeX.x) +
        (derivativeY.x * derivativeY.x);
    float covarianceY =
        (derivativeX.y * derivativeX.y) +
        (derivativeY.y * derivativeY.y);
    float covarianceCross =
        (derivativeX.x * derivativeX.y) +
        (derivativeY.x * derivativeY.y);
    float difference = covarianceX - covarianceY;
    float discriminant = sqrt(max(
        (difference * difference) +
            (4.0 * covarianceCross * covarianceCross),
        0.0));
    float majorEigenvalue = max(
        (covarianceX + covarianceY + discriminant) * 0.5,
        0.0);
    float majorLength = sqrt(majorEigenvalue);
    float4 result = SampleResamplingSource(
        mapped,
        profile,
        edgeMode,
        fillColor);
    if (majorLength > 1.0)
    {
        float2 majorDirection = covarianceX >= covarianceY
            ? float2(1.0, 0.0)
            : float2(0.0, 1.0);
        if (abs(covarianceCross) > 0.000001)
        {
            majorDirection = normalize(float2(
                covarianceCross,
                majorEigenvalue - covarianceX));
        }
        int tapCount = majorLength <= 4.0 ? 4 : 8;
        float2 axis =
            majorDirection *
            min(majorLength, 8.0) *
            PixelSize;
        if (tapCount == 4)
        {
            const float innerPosition = 0.125;
            const float outerPosition = 0.375;
            const float innerWeight = 0.96923323;
            const float outerWeight = 0.75483960;
            const float totalWeight =
                2.0 * (innerWeight + outerWeight);
            result = (
                SampleTwirlTap(
                    mapped,
                    axis,
                    -outerPosition,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    -innerPosition,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    innerPosition,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    outerPosition,
                    profile,
                    edgeMode,
                    fillColor)) /
                totalWeight;
        }
        else
        {
            const float position0 = 0.0625;
            const float position1 = 0.1875;
            const float position2 = 0.3125;
            const float position3 = 0.4375;
            const float weight0 = 0.99221794;
            const float weight1 = 0.93210249;
            const float weight2 = 0.82257756;
            const float weight3 = 0.68194075;
            const float totalWeight =
                2.0 * (weight0 + weight1 + weight2 + weight3);
            result = (
                SampleTwirlTap(
                    mapped,
                    axis,
                    -position3,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    -position2,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    -position1,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    -position0,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    position0,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    position1,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    position2,
                    profile,
                    edgeMode,
                    fillColor) +
                SampleTwirlTap(
                    mapped,
                    axis,
                    position3,
                    profile,
                    edgeMode,
                    fillColor)) /
                totalWeight;
        }
    }
    return result;
}

float4 SampleWaveFeline(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float2 mapped = uv;
    float2 derivativeX = float2(1.0, 0.0);
    float2 derivativeY = float2(0.0, 1.0);
    WaveJacobian(
        uv,
        mapped,
        derivativeX,
        derivativeY);
    float covarianceX =
        (derivativeX.x * derivativeX.x) +
        (derivativeY.x * derivativeY.x);
    float covarianceY =
        (derivativeX.y * derivativeX.y) +
        (derivativeY.y * derivativeY.y);
    float covarianceCross =
        (derivativeX.x * derivativeX.y) +
        (derivativeY.x * derivativeY.y);
    float difference = covarianceX - covarianceY;
    float discriminant = sqrt(max(
        (difference * difference) +
            (4.0 * covarianceCross * covarianceCross),
        0.0));
    float majorEigenvalue = max(
        (covarianceX + covarianceY + discriminant) * 0.5,
        0.0);
    float majorLength = sqrt(majorEigenvalue);
    float4 centerSample = SampleResamplingSource(
        mapped,
        profile,
        edgeMode,
        fillColor);
    if (majorLength <= 1.0)
    {
        return centerSample;
    }

    float2 majorDirection = covarianceX >= covarianceY
        ? float2(1.0, 0.0)
        : float2(0.0, 1.0);
    if (abs(covarianceCross) > 0.000001)
    {
        majorDirection = normalize(float2(
            covarianceCross,
            majorEigenvalue - covarianceX));
    }
    int tapCount = majorLength <= 4.0 ? 4 : 8;
    float2 axis =
        majorDirection *
        min(majorLength, 8.0) *
        PixelSize;
    float4 total = 0.0;
    float totalWeight = 0.0;
    [loop]
    for (int tap = 0; tap < 8; tap++)
    {
        if (tap >= tapCount)
        {
            break;
        }

        float position =
            ((tap + 0.5) / tapCount) -
            0.5;
        float weight = exp(
            -2.0 * position * position);
        total +=
            SampleResamplingSource(
                mapped + (axis * position),
                profile,
                edgeMode,
                fillColor) *
            weight;
        totalWeight += weight;
    }
    return totalWeight > 0.0
        ? total / totalWeight
        : centerSample;
}

float4 DiffuseGlowBrightPass(float4 sample)
{
    float luminance = dot(
        Unpremultiply(sample),
        float3(0.2126, 0.7152, 0.0722));
    return luminance < FilterOptions0.z
        ? 0.0
        : sample;
}

float4 DiffuseGlowHorizontal(
    float2 uv,
    int profile,
    float4 fillColor)
{
    float2 stepSize =
        PixelSize *
        max(FilterOptions0.w, 0.5);
    return
        (DiffuseGlowBrightPass(
            SampleResamplingSource(
                uv,
                profile,
                0,
                fillColor)) * 0.38774) +
        (DiffuseGlowBrightPass(
            SampleResamplingSource(
                uv - float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor)) * 0.24477) +
        (DiffuseGlowBrightPass(
            SampleResamplingSource(
                uv + float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor)) * 0.24477) +
        (DiffuseGlowBrightPass(
            SampleResamplingSource(
                uv - float2(stepSize.x * 2.0, 0.0),
                profile,
                0,
                fillColor)) * 0.06136) +
        (DiffuseGlowBrightPass(
            SampleResamplingSource(
                uv + float2(stepSize.x * 2.0, 0.0),
                profile,
                0,
                fillColor)) * 0.06136);
}

float4 DiffuseGlowVertical(
    float2 uv,
    int profile,
    float4 fillColor)
{
    float2 stepSize =
        PixelSize *
        max(FilterOptions0.w, 0.5);
    return
        (SampleResamplingSource(
            uv,
            profile,
            0,
            fillColor) * 0.38774) +
        (SampleResamplingSource(
            uv - float2(0.0, stepSize.y),
            profile,
            0,
            fillColor) * 0.24477) +
        (SampleResamplingSource(
            uv + float2(0.0, stepSize.y),
            profile,
            0,
            fillColor) * 0.24477) +
        (SampleResamplingSource(
            uv - float2(0.0, stepSize.y * 2.0),
            profile,
            0,
            fillColor) * 0.06136) +
        (SampleResamplingSource(
            uv + float2(0.0, stepSize.y * 2.0),
            profile,
            0,
            fillColor) * 0.06136);
}

float NeonGlowSignal(float4 sample)
{
    float luminance = dot(
        sample.rgb,
        float3(0.2126, 0.7152, 0.0722));
    return max(luminance, sample.a * 0.25);
}

float4 NeonGlowEdge(
    float2 uv,
    int profile,
    float4 fillColor)
{
    float2 pixel = PixelSize;
    float topLeft = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(-1.0, -1.0) * pixel),
        profile,
        0,
        fillColor));
    float top = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(0.0, -1.0) * pixel),
        profile,
        0,
        fillColor));
    float topRight = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(1.0, -1.0) * pixel),
        profile,
        0,
        fillColor));
    float left = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(-1.0, 0.0) * pixel),
        profile,
        0,
        fillColor));
    float right = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(1.0, 0.0) * pixel),
        profile,
        0,
        fillColor));
    float bottomLeft = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(-1.0, 1.0) * pixel),
        profile,
        0,
        fillColor));
    float bottom = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(0.0, 1.0) * pixel),
        profile,
        0,
        fillColor));
    float bottomRight = NeonGlowSignal(SampleResamplingSource(
        uv + (float2(1.0, 1.0) * pixel),
        profile,
        0,
        fillColor));
    float gradientX =
        -topLeft + topRight -
        (2.0 * left) + (2.0 * right) -
        bottomLeft + bottomRight;
    float gradientY =
        -topLeft - (2.0 * top) - topRight +
        bottomLeft + (2.0 * bottom) + bottomRight;
    float edge = saturate(length(float2(
        gradientX,
        gradientY)) * 0.25);
    return float4(edge, edge, edge, edge);
}

float4 NeonGlowGaussian(
    float2 uv,
    float2 axis,
    int profile,
    float4 fillColor)
{
    float2 stepSize =
        axis *
        PixelSize *
        max(FilterOptions0.y, 0.5);
    return
        (SampleResamplingSource(
            uv,
            profile,
            0,
            fillColor) * 0.38774) +
        (SampleResamplingSource(
            uv - stepSize,
            profile,
            0,
            fillColor) * 0.24477) +
        (SampleResamplingSource(
            uv + stepSize,
            profile,
            0,
            fillColor) * 0.24477) +
        (SampleResamplingSource(
            uv - (stepSize * 2.0),
            profile,
            0,
            fillColor) * 0.06136) +
        (SampleResamplingSource(
            uv + (stepSize * 2.0),
            profile,
            0,
            fillColor) * 0.06136);
}

float NeonGlowMipMask(
    float2 uv,
    float lod,
    int profile)
{
    float4 sample = tex2Dlod(
        NeonPyramidSampler,
        float4(uv, 0.0, lod));
    return WorkingAssociatedToLinearSrgb(
        sample,
        profile).r;
}

float4 NeonGlowPyramidComposite(
    float2 uv,
    int profile)
{
    float maximumLod = max(FilterOptions0.z, 0.0);
    float mask =
        (NeonGlowMipMask(uv, 0.0, profile) * 0.32) +
        (NeonGlowMipMask(
            uv,
            maximumLod * 0.25,
            profile) * 0.25) +
        (NeonGlowMipMask(
            uv,
            maximumLod * 0.5,
            profile) * 0.19) +
        (NeonGlowMipMask(
            uv,
            maximumLod * 0.75,
            profile) * 0.14) +
        (NeonGlowMipMask(
            uv,
            maximumLod,
            profile) * 0.10);
    float4 original = WorkingAssociatedToLinearSrgb(
        tex2D(FilterAuxiliaryTextureSampler, uv),
        profile);
    float strength =
        clamp(FilterOptions0.w, 0.0, 8.0) *
        saturate(FilterOptions1.a) *
        mask;
    float3 combined = min(
        original.rgb +
            (FilterOptions1.rgb * strength),
        float3(original.a, original.a, original.a));
    return float4(combined, original.a);
}
