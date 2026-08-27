uint CatalogFibersSeed()
{
    uint low = (uint)FilterOptions4.x;
    uint high = (uint)FilterOptions4.y;
    return (low & 0xffffu) | (high << 16);
}

float CatalogFibersHash(
    int2 cell,
    uint seed)
{
    return CatalogIntegerHash(cell.x, cell.y, seed);
}

float CatalogFibersGradient(
    float hash,
    float2 offset)
{
    const float diagonal = 0.70710678118;
    float2 gradient = float2(
        hash < 0.5 ? 1.0 : -1.0,
        frac(hash * 2.0) < 0.5 ? 1.0 : -1.0);
    return diagonal * dot(gradient, offset);
}

float CatalogFibersFade(float value)
{
    return value *
        value *
        value *
        ((value * ((value * 6.0) - 15.0)) + 10.0);
}

float CatalogFibersPerlin(
    float2 position,
    uint seed)
{
    int2 cell = (int2)floor(position);
    float2 local = position - cell;
    float2 fade = float2(
        CatalogFibersFade(local.x),
        CatalogFibersFade(local.y));
    float topLeft = CatalogFibersGradient(
        CatalogFibersHash(cell, seed),
        local);
    float topRight = CatalogFibersGradient(
        CatalogFibersHash(cell + int2(1, 0), seed),
        local - float2(1.0, 0.0));
    float bottomLeft = CatalogFibersGradient(
        CatalogFibersHash(cell + int2(0, 1), seed),
        local - float2(0.0, 1.0));
    float bottomRight = CatalogFibersGradient(
        CatalogFibersHash(cell + int2(1, 1), seed),
        local - 1.0);
    return lerp(
        lerp(topLeft, topRight, fade.x),
        lerp(bottomLeft, bottomRight, fade.x),
        fade.y);
}

float CatalogFibersNoise(float2 pixel)
{
    const float longitudinalScale = 0.125;
    const float warpScale = 0.35;
    const float gradientNormalization = 1.41421356237;
    pixel = floor(pixel + 0.0001) + 0.5;
    float variance = max(FilterOptions2.x, 0.0001);
    float strength = max(FilterOptions3.x, 0.0);
    float2 position = float2(
        pixel.x / variance,
        (pixel.y / variance) * longitudinalScale);
    uint seed = CatalogFibersSeed();
    float warp = CatalogFibersPerlin(
        position * float2(0.25, 0.5),
        seed + 0x6be3a8e3u);
    position.x += warp * warpScale;

    float total = 0.0;
    float amplitude = 1.0;
    float amplitudeSum = 0.0;
    float frequency = 1.0;
    [loop]
    for (int octave = 0; octave < 5; octave++)
    {
        float2 rotation;
        if (octave == 0)
        {
            rotation = float2(1.0, 0.0);
        }
        else if (octave == 1)
        {
            rotation = float2(0.99775634, 0.06694988);
        }
        else if (octave == 2)
        {
            rotation = float2(0.99879974, -0.04898039);
        }
        else if (octave == 3)
        {
            rotation = float2(0.99470066, 0.10281728);
        }
        else
        {
            rotation = float2(0.99655754, -0.08290404);
        }
        float2 octavePosition = float2(
            (rotation.x * position.x) -
                (rotation.y * position.y),
            (rotation.y * position.x) +
                (rotation.x * position.y)) *
            frequency;
        total += CatalogFibersPerlin(
                octavePosition,
                seed + ((uint)octave * 0x9e3779b9u)) *
            amplitude;
        amplitudeSum += amplitude;
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    float normalized = clamp(
        (total / amplitudeSum) * gradientNormalization,
        -1.0,
        1.0);
    return saturate(
        0.5 + (normalized * 0.5 * strength));
}

float4 CatalogFibers(float2 pixel, float4 source)
{
    float fibers = CatalogFibersNoise(pixel);
    return float4(
        lerp(FilterOptions0.rgb, FilterOptions1.rgb, fibers) * source.a,
        source.a);
}
