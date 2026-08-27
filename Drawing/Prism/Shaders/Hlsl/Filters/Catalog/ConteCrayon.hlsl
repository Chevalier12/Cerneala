



#ifndef CERNEALA_SDL_GPU
sampler2D ConteCrayonOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float2 ConteCrayonClampUv(float2 uv)
{
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 ConteCrayonRaw(float2 uv)
{
    return tex2Dlod(
        SpriteTextureSampler,
        float4(ConteCrayonClampUv(uv), 0.0, 0.0));
}

float4 ConteCrayonOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            ConteCrayonOriginalSampler,
            float4(ConteCrayonClampUv(uv), 0.0, 0.0)),
        profile);
}

float2 ConteCrayonDecodeTangent(float4 field)
{
    float2 tangent = (field.rg * 2.0) - 1.0;
    float lengthSquared = dot(tangent, tangent);
    tangent = lengthSquared > 0.000001
        ? tangent * rsqrt(lengthSquared)
        : float2(1.0, 0.0);
    if (tangent.y < -0.0001 ||
        (abs(tangent.y) <= 0.0001 && tangent.x < 0.0))
    {
        tangent = -tangent;
    }
    return tangent;
}

float ConteCrayonHash(float2 coordinate, float seed)
{
    int2 cell = (int2)floor(coordinate);
    return CatalogIntegerHash(
        cell.x,
        cell.y,
        (uint)seed * 0x9e3779b9u);
}

float ConteCrayonWave(float value)
{
    return 0.5 + (0.5 * cos(value));
}

float ConteCrayonPaperHeight(
    float2 pixel,
    float textureScale,
    int textureKind)
{
    float2 coordinate = pixel / max(textureScale, 0.125);
    float fine = ConteCrayonHash(floor(coordinate * 0.45), 13.0);
    float coarse = ConteCrayonHash(floor(coordinate * 0.09), 29.0);
    if (textureKind == 1)
    {
        float row = floor(coordinate.y / 5.0);
        float shiftedX = coordinate.x +
            (fmod(abs(row), 2.0) < 1.0 ? 0.0 : 4.0);
        float verticalMortar = abs(frac(shiftedX / 8.0) - 0.5) * 2.0;
        float horizontalMortar = abs(frac(coordinate.y / 5.0) - 0.5) * 2.0;
        float mortar = 1.0 - min(verticalMortar, horizontalMortar);
        return saturate(
            (0.5 * fine) + (0.35 * coarse) + (0.15 * mortar));
    }
    if (textureKind == 2)
    {
        float horizontal = ConteCrayonWave(
            (coordinate.y * 2.2) + (coarse * 1.4));
        float vertical = ConteCrayonWave(
            (coordinate.x * 2.05) - (fine * 1.2));
        return saturate(
            (0.28 * fine) +
            (0.18 * coarse) +
            (0.27 * horizontal) +
            (0.27 * vertical));
    }
    if (textureKind == 3)
    {
        return saturate(
            (0.35 * fine) +
            (0.5 * coarse) +
            (0.15 * ConteCrayonHash(
                floor(coordinate * float2(0.9, 0.18)),
                47.0)));
    }

    return saturate(
        (0.45 * fine) +
        (0.25 * coarse) +
        (0.15 * ConteCrayonWave(coordinate.x * 1.7)) +
        (0.15 * ConteCrayonWave(coordinate.y * 1.9)));
}

float ConteCrayonHatchLayer(
    float2 pixel,
    float2 tangent,
    float angle,
    float spacing,
    float width,
    float seed)
{
    float cosine = cos(angle);
    float sine = sin(angle);
    float2 direction = float2(
        (tangent.x * cosine) - (tangent.y * sine),
        (tangent.x * sine) + (tangent.y * cosine));
    float2 normal = float2(-direction.y, direction.x);
    float phase = ConteCrayonHash(
        floor(pixel / (spacing * 3.0)),
        seed) * 0.45;
    float coordinate = (dot(pixel, normal) / spacing) + phase;
    float distance = abs(frac(coordinate) - 0.5) * 2.0;
    return 1.0 - smoothstep(width, width + 0.22, distance);
}

float ConteCrayonFourLayerHatch(
    float2 pixel,
    float2 tangent,
    float darkness,
    float level,
    float textureScale)
{
    float bias = (level - 0.35) * 0.2;
    float first = ConteCrayonHatchLayer(
        pixel, tangent, 0.0, textureScale * 4.6, 0.2, 11.0) *
        smoothstep(0.12 - bias, 0.34 - bias, darkness);
    float second = ConteCrayonHatchLayer(
        pixel, tangent, 0.7853982, textureScale * 5.2, 0.19, 23.0) *
        smoothstep(0.3 - bias, 0.5 - bias, darkness);
    float third = ConteCrayonHatchLayer(
        pixel, tangent, 1.5707963, textureScale * 5.8, 0.18, 37.0) *
        smoothstep(0.5 - bias, 0.7 - bias, darkness);
    float fourth = ConteCrayonHatchLayer(
        pixel, tangent, -0.7853982, textureScale * 6.4, 0.17, 53.0) *
        smoothstep(0.68 - bias, 0.88 - bias, darkness);
    return saturate(
        1.0 -
        ((1.0 - first) *
            (1.0 - second) *
            (1.0 - third) *
            (1.0 - fourth)));
}

float3 ConteCrayonLightVector(int direction)
{
    const float diagonal = 0.7071068;
    float2 planar = float2(0.0, -1.0);
    if (direction == 1) planar = float2(diagonal, -diagonal);
    else if (direction == 2) planar = float2(1.0, 0.0);
    else if (direction == 3) planar = float2(diagonal, diagonal);
    else if (direction == 4) planar = float2(0.0, 1.0);
    else if (direction == 5) planar = float2(-diagonal, diagonal);
    else if (direction == 6) planar = float2(-1.0, 0.0);
    else if (direction == 7) planar = float2(-diagonal, -diagonal);
    return normalize(float3(planar, 1.25));
}

float4 ConteCrayonComposite(float2 uv, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float4 flow = ConteCrayonRaw(uv);
    float2 tangent = ConteCrayonDecodeTangent(flow);
    float response = (flow.b * 2.0) - 1.0;
    float foregroundLevel = saturate(FilterOptions3.x / 20.0);
    float backgroundLevel = saturate(FilterOptions1.x / 20.0);
    float textureScale = clamp(FilterOptions6.x, 0.125, 16.0);
    float relief = clamp(FilterOptions5.x, 0.0, 2.0);
    int textureKind = (int)(FilterOptions7.x + 0.5);
    int lightDirection = (int)(FilterOptions4.x + 0.5);
    float luminance = CatalogLuminance(original);
    float darkness = 1.0 - luminance;
    float edgeThreshold = lerp(0.052, 0.011, foregroundLevel);
    float lineMask = smoothstep(
        edgeThreshold * 0.28,
        edgeThreshold,
        abs(response));
    float2 pixel = floor(uv / PixelSize) + 0.5;
    float hatch = ConteCrayonFourLayerHatch(
        pixel,
        tangent,
        darkness,
        backgroundLevel,
        textureScale);
    float paper = ConteCrayonPaperHeight(
        pixel,
        textureScale,
        textureKind);
    float tooth = (paper - 0.5) * 2.0;
    float lineCoverage = lineMask * lerp(0.48, 1.0, foregroundLevel);
    float toneCoverage = hatch * lerp(0.42, 0.95, backgroundLevel);
    float coverage = saturate(
        max(lineCoverage, toneCoverage) *
        (1.0 + (tooth * lerp(0.12, 0.34, backgroundLevel))));

    float horizontal =
        ConteCrayonPaperHeight(
            pixel + float2(1.0, 0.0),
            textureScale,
            textureKind) -
        ConteCrayonPaperHeight(
            pixel - float2(1.0, 0.0),
            textureScale,
            textureKind);
    float vertical =
        ConteCrayonPaperHeight(
            pixel + float2(0.0, 1.0),
            textureScale,
            textureKind) -
        ConteCrayonPaperHeight(
            pixel - float2(0.0, 1.0),
            textureScale,
            textureKind);
    float3 normal = normalize(float3(
        -horizontal * relief * 2.4,
        -vertical * relief * 2.4,
        1.0));
    float illumination = saturate(dot(
        normal,
        ConteCrayonLightVector(lightDirection)));
    float paperShade = lerp(
        1.0,
        0.78 + (0.36 * illumination),
        saturate(relief));
    float3 straight = lerp(
        saturate(FilterOptions0.rgb),
        saturate(FilterOptions2.rgb),
        coverage);
    straight = saturate(straight * paperShade);
    return float4(straight * original.a, original.a);
}
