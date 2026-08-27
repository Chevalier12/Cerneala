float SampleStyleMaskSource(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    return tex2Dlod(
        StyleMaskSourceSampler,
        float4(saturate(uv), 0.0, 0.0)).a * inside;
}

float4 StyleDilatePixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float radius = min(max(MaskDensity, 0.0), 32.0);
    float value = 0.0;
    [unroll]
    for (int offset = -32; offset <= 32; offset++)
    {
        if (abs(offset) <= radius)
        {
            value = max(
                value,
                SampleStyleMaskSource(
                    uv + (MaskFeatherStep * offset)));
        }
    }
    return float4(value, value, value, value);
}

float GaussianWeight(float position, float sigma)
{
    return exp(
        -(position * position) /
        (2.0 * sigma * sigma));
}

float4 StyleGaussianPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float radius = min(max(MaskDensity, 1.0), 32.0);
    float sigma = max(radius / 3.0, 0.5);
    float value = SampleStyleMaskSource(uv);
    float totalWeight = 1.0;



    [unroll]
    for (int pair = 0; pair < 16; pair++)
    {
        float firstPosition = 1.0 + (pair * 2.0);
        float secondPosition = firstPosition + 1.0;
        float firstWeight = firstPosition <= radius
            ? GaussianWeight(firstPosition, sigma)
            : 0.0;
        float secondWeight = secondPosition <= radius
            ? GaussianWeight(secondPosition, sigma)
            : 0.0;
        float pairWeight = firstWeight + secondWeight;
        if (pairWeight > 0.0)
        {
            float pairedPosition = firstPosition +
                (secondWeight / pairWeight);
            float2 sampleOffset =
                MaskFeatherStep * pairedPosition;
            value += pairWeight * (
                SampleStyleMaskSource(uv - sampleOffset) +
                SampleStyleMaskSource(uv + sampleOffset));
            totalWeight += 2.0 * pairWeight;
        }
    }

    value /= totalWeight;
    return float4(value, value, value, value);
}


float EvaluateOuterGlowMask(
    float2 uv,
    float alpha,
    float size,
    float spread,
    float techniqueCode)
{
    float distance = max(
        StyleSignedEuclideanDistance(uv, alpha),
        0.0);
    float fadeSize = max(size, 0.0001);
    float coordinate = saturate(
        (distance - spread) / fadeSize);
    float linearFalloff = 1.0 - coordinate;
    float softerFalloff = exp(
        -4.0 * coordinate * coordinate);
    float smoothFalloff = smoothstep(
        1.0,
        0.0,
        coordinate);
    float falloff = softerFalloff;
    if (techniqueCode >= 0.5 && techniqueCode < 1.5)
    {
        falloff = linearFalloff;
    }
    else if (techniqueCode >= 1.5)
    {
        falloff = smoothFalloff;
    }
    float support =
        step(distance, spread + size) *
        step(0.0001, spread + size);
    return saturate(falloff * support) *
        (1.0 - alpha);
}
