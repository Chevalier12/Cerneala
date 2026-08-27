




#ifndef CERNEALA_SDL_GPU
sampler2D TexturizerTextureSampler = sampler_state
{
    Texture = <SecondaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};
#endif

uint TexturizerHash(int2 cell, uint seed)
{
    uint value =
        (uint(cell.x) * 0x9e3779b9u) ^
        (uint(cell.y) * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float TexturizerRandom(int2 cell, uint seed)
{
    return float(TexturizerHash(cell, seed) & 0x00ffffffu) / 16777215.0;
}

float TexturizerValueNoise(float2 position, uint seed)
{
    int2 cell = (int2)floor(position);
    float2 local = position - float2(cell);
    local = local * local * (3.0 - (2.0 * local));
    float upper = lerp(
        TexturizerRandom(cell, seed),
        TexturizerRandom(cell + int2(1, 0), seed),
        local.x);
    float lower = lerp(
        TexturizerRandom(cell + int2(0, 1), seed),
        TexturizerRandom(cell + int2(1, 1), seed),
        local.x);
    return lerp(upper, lower, local.y);
}

float TexturizerWave(float value)
{
    return 0.5 + (0.5 * cos(value));
}

float TexturizerSurfaceHeight(float2 pixel, float scale, int textureKind)
{
    float2 coordinate = pixel / max(scale, 0.125);
    float fine = TexturizerValueNoise(coordinate * 0.45, 0x51ed270bu);
    float coarse = TexturizerValueNoise(coordinate * 0.09, 0x8321ca5du);
    if (textureKind == 1)
    {
        float row = floor(coordinate.y / 5.0);
        float shiftedX = coordinate.x +
            ((((int)row & 1) == 0) ? 0.0 : 4.0);
        float verticalMortar = abs(frac(shiftedX / 8.0) - 0.5) * 2.0;
        float horizontalMortar = abs(frac(coordinate.y / 5.0) - 0.5) * 2.0;
        float mortar = 1.0 - min(verticalMortar, horizontalMortar);
        return saturate((0.5 * fine) + (0.35 * coarse) + (0.15 * mortar));
    }
    if (textureKind == 2)
    {
        float horizontal = TexturizerWave(
            (coordinate.y * 2.2) + (coarse * 1.4));
        float vertical = TexturizerWave(
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
            (0.15 * TexturizerValueNoise(
                coordinate * float2(0.9, 0.18),
                0x31a42f19u)));
    }

    return saturate(
        (0.45 * fine) +
        (0.25 * coarse) +
        (0.15 * TexturizerWave(coordinate.x * 1.7)) +
        (0.15 * TexturizerWave(coordinate.y * 1.9)));
}

float TexturizerHeight(float2 uv, float2 pixel)
{
    float scaling = clamp(abs(FilterOptions3.x), 0.125, 16.0);
    float height;
    if (FilterHeader.w >= 1.0)
    {
        float2 textureUv = frac(((uv - 0.5) / scaling) + 0.5);
        float4 sample = tex2D(TexturizerTextureSampler, textureUv);
        float3 straight = sample.a > 0.000001
            ? saturate(sample.rgb / sample.a)
            : 0.0;
        height = dot(straight, float3(0.2126, 0.7152, 0.0722));
    }
    else
    {
        int textureKind = (int)(FilterOptions4.x + 0.5);
        height = TexturizerSurfaceHeight(
            pixel,
            scaling * max(FilterOptions6.x, 0.125),
            textureKind);
    }
    return FilterOptions0.x >= 0.5 ? 1.0 - height : height;
}

float3 TexturizerLightVector(int direction)
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

float4 CatalogTexturizer(float2 uv, float4 source)
{
    if (source.a <= 0.000001)
    {
        return 0.0;
    }

    float2 pixel = floor(uv / PixelSize) + 0.5;
    float2 dx = float2(PixelSize.x, 0.0);
    float2 dy = float2(0.0, PixelSize.y);
    float topLeft = TexturizerHeight(uv - dx - dy, pixel + float2(-1.0, -1.0));
    float top = TexturizerHeight(uv - dy, pixel + float2(0.0, -1.0));
    float topRight = TexturizerHeight(uv + dx - dy, pixel + float2(1.0, -1.0));
    float left = TexturizerHeight(uv - dx, pixel + float2(-1.0, 0.0));
    float right = TexturizerHeight(uv + dx, pixel + float2(1.0, 0.0));
    float bottomLeft = TexturizerHeight(uv - dx + dy, pixel + float2(-1.0, 1.0));
    float bottom = TexturizerHeight(uv + dy, pixel + float2(0.0, 1.0));
    float bottomRight = TexturizerHeight(uv + dx + dy, pixel + float2(1.0, 1.0));
    float horizontal =
        (3.0 * (topRight - topLeft)) +
        (10.0 * (right - left)) +
        (3.0 * (bottomRight - bottomLeft));
    float vertical =
        (3.0 * (bottomLeft - topLeft)) +
        (10.0 * (bottom - top)) +
        (3.0 * (bottomRight - topRight));
    horizontal /= 16.0;
    vertical /= 16.0;

    float relief = clamp(abs(FilterOptions2.x), 0.0, 1.0);
    float3 normal = normalize(float3(
        -horizontal * relief * 24.0,
        -vertical * relief * 24.0,
        1.0));
    float3 light = TexturizerLightVector((int)(FilterOptions1.x + 0.5));
    float shade = clamp(
        1.0 + ((dot(normal, light) - light.z) * 1.6),
        0.25,
        1.75);
    float3 straight = saturate((source.rgb / source.a) * shade);
    return float4(straight * source.a, source.a);
}
