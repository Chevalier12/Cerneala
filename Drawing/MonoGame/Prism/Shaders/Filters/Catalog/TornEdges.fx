uint TornEdgesHash(uint x, uint y)
{
    uint hash =
        (x * 0x8da6b343u) ^
        (y * 0xd8163841u) ^
        0xcb1ab31fu;
    hash ^= hash >> 16;
    hash *= 0x7feb352du;
    hash ^= hash >> 15;
    hash *= 0x846ca68bu;
    hash ^= hash >> 16;
    return hash;
}

float TornEdgesHashValue(int2 cell)
{
    uint hash = TornEdgesHash((uint)cell.x, (uint)cell.y);
    return (((hash & 0x00ffffffu) / 16777215.0) * 2.0) - 1.0;
}

float TornEdgesFade(float value)
{
    return value * value * value *
        ((value * ((value * 6.0) - 15.0)) + 10.0);
}

float TornEdgesValueNoise(float2 position)
{
    int2 cell = (int2)floor(position);
    float2 fraction = position - cell;
    float2 fade = float2(
        TornEdgesFade(fraction.x),
        TornEdgesFade(fraction.y));
    float lower = lerp(
        TornEdgesHashValue(cell),
        TornEdgesHashValue(cell + int2(1, 0)),
        fade.x);
    float upper = lerp(
        TornEdgesHashValue(cell + int2(0, 1)),
        TornEdgesHashValue(cell + int2(1, 1)),
        fade.x);
    return lerp(lower, upper, fade.y);
}

float TornEdgesFbm(float2 position)
{
    float total = 0.0;
    float normalization = 0.0;
    float amplitude = 0.5;
    [unroll]
    for (int octave = 0; octave < 4; octave++)
    {
        total += TornEdgesValueNoise(position) * amplitude;
        normalization += amplitude;
        position = (position * 2.03) + float2(19.1, 7.7);
        amplitude *= 0.5;
    }
    return total / normalization;
}

float4 TornEdgesComposite(float2 uv, float4 original)
{
    const float minimumWeight = 0.000001;
    if (original.a <= minimumWeight)
    {
        return 0.0;
    }

    float4 gaussian = PhotocopyRawSample(uv);
    float narrowLuminance = gaussian.y <= minimumWeight
        ? 0.0
        : gaussian.x / gaussian.y;
    float extendedLuminance = gaussian.w <= minimumWeight
        ? 0.0
        : gaussian.z / gaussian.w;
    float sharpen = clamp(FilterOptions6.x, 8.0, 48.0);
    float response =
        ((sharpen + 1.0) * narrowLuminance) -
        (sharpen * extendedLuminance);
    float threshold = saturate(FilterOptions5.w);
    float transitionWidth = clamp(FilterOptions6.w, 0.05, 0.25);
    float transitionWeight = 1.0 - smoothstep(
        0.0,
        transitionWidth,
        abs(response - threshold));
    float2 pixel = uv / PixelSize;
    float noise = TornEdgesFbm(pixel * clamp(FilterOptions6.z, 0.01, 0.5));
    float perturbedThreshold = threshold +
        (noise * clamp(FilterOptions6.y, 0.0, 0.2) * transitionWeight);
    float3 color = response < perturbedThreshold
        ? FilterOptions2.rgb
        : FilterOptions0.rgb;
    return float4(saturate(color) * original.a, original.a);
}
