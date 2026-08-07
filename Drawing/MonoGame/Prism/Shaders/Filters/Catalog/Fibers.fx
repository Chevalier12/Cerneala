float2 CatalogFibersSeed()
{
    return FilterOptions4.xy;
}

float CatalogFibersHash(
    int2 cell,
    float2 seed)
{
    float value =
        dot((float2)cell, float2(127.1, 311.7)) +
        dot(seed, float2(0.1031, 0.11369));
    return frac(sin(value) * 43758.5453123);
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
    float2 seed)
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
    float variance = max(FilterOptions2.x, 0.0001);
    float strength = max(FilterOptions3.x, 0.0);
    float2 position = float2(
        pixel.x / variance,
        (pixel.y / variance) * longitudinalScale);
    float2 seed = CatalogFibersSeed();
    float warp = CatalogFibersPerlin(
        position * float2(0.25, 0.5),
        seed + float2(107.0, 227.0));
    position.x += warp * warpScale;

    float total = 0.0;
    float amplitude = 1.0;
    float amplitudeSum = 0.0;
    float frequency = 1.0;
    [loop]
    for (int octave = 0; octave < 5; octave++)
    {
        float angle;
        if (octave == 0)
        {
            angle = 0.0;
        }
        else if (octave == 1)
        {
            angle = 0.067;
        }
        else if (octave == 2)
        {
            angle = -0.049;
        }
        else if (octave == 3)
        {
            angle = 0.103;
        }
        else
        {
            angle = -0.083;
        }
        float cosine = cos(angle);
        float sine = sin(angle);
        float2 octavePosition = float2(
            (cosine * position.x) -
                (sine * position.y),
            (sine * position.x) +
                (cosine * position.y)) *
            frequency;
        total += CatalogFibersPerlin(
                octavePosition,
                seed +
                    float2(
                        octave * 19.0,
                        octave * 47.0)) *
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
