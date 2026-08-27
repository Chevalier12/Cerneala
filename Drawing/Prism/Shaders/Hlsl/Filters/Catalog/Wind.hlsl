


float WindSeed()
{
    return FilterOptions2.x + (FilterOptions2.y * 65536.0);
}

float WindHash(float2 cell, float seed)
{
    return frac(
        sin(
            dot(cell, float2(127.1, 311.7)) +
            (seed * 0.017)) *
        43758.5453);
}

float WindValueNoise(float2 position, float seed)
{
    float2 cell = floor(position);
    float2 blend = frac(position);
    blend = blend * blend * (3.0 - (2.0 * blend));
    float top = lerp(
        WindHash(cell, seed),
        WindHash(cell + float2(1.0, 0.0), seed),
        blend.x);
    float bottom = lerp(
        WindHash(cell + float2(0.0, 1.0), seed),
        WindHash(cell + float2(1.0, 1.0), seed),
        blend.x);
    return lerp(top, bottom, blend.y);
}

float4 WindOriginalSample(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            FilterAuxiliaryTextureSampler,
            float4(
                clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5)),
                0.0,
                0.0)),
        profile);
}

float4 WindSignalSample(float2 uv, int profile)
{
    return CatalogLinearSample(uv, profile);
}

float WindOriginalLuminance(float2 uv, int profile)
{
    return CatalogLuminance(WindOriginalSample(uv, profile));
}

float WindDirectionSign()
{
    return 1.0 - (2.0 * saturate(FilterOptions0.x));
}

float2 WindProceduralDirection(float2 uv)
{
    int method = (int)(FilterOptions1.x + 0.5);
    float2 baseDirection = float2(WindDirectionSign(), 0.0);
    float2 pixel = (uv / PixelSize) - 0.5;
    float scale = method == 2 ? 7.0 : 11.0;
    float noise = WindValueNoise(
            pixel / scale,
            WindSeed() + 19.0) -
        0.5;
    float turbulence = method == 1
        ? 0.16
        : method == 2
            ? 0.68
            : 0.38;
    float angle = noise * turbulence;
    float cosine = cos(angle);
    float sine = sin(angle);
    float2 flow = float2(
        (baseDirection.x * cosine) - (baseDirection.y * sine),
        (baseDirection.x * sine) + (baseDirection.y * cosine));
    return flow;
}

float2 WindFlowDirection(float2 uv, int profile)
{
    float2 baseDirection = float2(WindDirectionSign(), 0.0);
    float2 flow = WindProceduralDirection(uv);

    float horizontal =
        WindOriginalLuminance(
            uv + float2(PixelSize.x, 0.0),
            profile) -
        WindOriginalLuminance(
            uv - float2(PixelSize.x, 0.0),
            profile);
    float vertical =
        WindOriginalLuminance(
            uv + float2(0.0, PixelSize.y),
            profile) -
        WindOriginalLuminance(
            uv - float2(0.0, PixelSize.y),
            profile);
    float2 tangent = float2(-vertical, horizontal);
    float tangentLength = length(tangent);
    float edge = saturate(tangentLength * 1.5);
    if (tangentLength > 0.0001)
    {
        tangent /= tangentLength;
        if (dot(tangent, baseDirection) < 0.0)
        {
            tangent = -tangent;
        }
        flow = lerp(flow, tangent, edge * 0.18);
    }

    float flowLength = length(flow);
    return flowLength <= 0.0001
        ? baseDirection
        : flow / flowLength;
}

float2 WindAdvance(
    float2 uv,
    float signValue,
    float stepLength,
    float2 guidance)
{
    float2 local = WindProceduralDirection(uv);
    float2 flow = lerp(local, guidance, 0.35);
    float flowLength = length(flow);
    flow = flowLength <= 0.0001 ? guidance : flow / flowLength;
    return uv + (flow * PixelSize * (signValue * stepLength));
}

float WindMethodLengthScale(int method)
{
    return method == 1
        ? 5.5
        : method == 2
            ? 4.5
            : 4.0;
}

float WindStaggerWeight(
    int method,
    float2 uv,
    int step)
{
    if (method != 2)
    {
        return 1.0;
    }

    float2 pixel = (uv / PixelSize) - 0.5;
    float lane = WindValueNoise(
        float2(pixel.x / 5.0, pixel.y / 3.0),
        WindSeed() + (step * 37.0));
    return 0.35 + (lane * 0.65);
}

float4 WindLineIntegral(float2 uv, int profile)
{
    const int integrationSteps = 8;
    int method = (int)(FilterOptions1.x + 0.5);
    float lineLength = clamp(
        max(FilterOptions3.x, 0.0) * WindMethodLengthScale(method),
        0.0,
        64.0);
    float stepLength = lineLength / integrationSteps;
    float reverseBias = method == 1
        ? 0.16
        : method == 2
            ? 0.42
            : 0.3;
    float2 forward = uv;
    float2 backward = uv;
    float2 guidance = WindFlowDirection(uv, profile);
    float4 total = WindSignalSample(uv, profile);
    float weightTotal = 1.0;

    [loop]
    for (int step = 1; step <= integrationSteps; step++)
    {
        forward = WindAdvance(
            forward,
            1.0,
            stepLength,
            guidance);
        backward = WindAdvance(
            backward,
            -1.0,
            stepLength,
            guidance);
        float phase = step / (integrationSteps + 1.0);
        float window = 0.5 +
            (0.5 * cos(3.14159265359 * phase));
        float forwardWeight = window *
            WindStaggerWeight(method, forward, step);
        float backwardWeight = window * reverseBias;
        total += WindSignalSample(forward, profile) * forwardWeight;
        total += WindSignalSample(backward, profile) * backwardWeight;
        weightTotal += forwardWeight + backwardWeight;
    }

    float4 result = total / weightTotal;
    float2 rampUv = uv + float2(
        WindDirectionSign() * PixelSize.x * lineLength * 0.5,
        0.0);
    result = lerp(
        result,
        WindSignalSample(rampUv, profile),
        0.18);
    result.a = saturate(WindOriginalSample(uv, profile).a);
    result.rgb = clamp(result.rgb, 0.0, result.a);
    return result;
}

float4 WindEnhanceContrast(float2 uv, int profile)
{
    float4 center = WindSignalSample(uv, profile);
    float4 neighbors =
        WindSignalSample(
            uv - float2(PixelSize.x, 0.0),
            profile) +
        WindSignalSample(
            uv + float2(PixelSize.x, 0.0),
            profile) +
        WindSignalSample(
            uv - float2(0.0, PixelSize.y),
            profile) +
        WindSignalSample(
            uv + float2(0.0, PixelSize.y),
            profile);
    float3 highPass = (center.rgb * 4.0) - neighbors.rgb;
    float alpha = saturate(WindOriginalSample(uv, profile).a);
    return float4(
        clamp(center.rgb + (highPass * 0.32), 0.0, alpha),
        alpha);
}
