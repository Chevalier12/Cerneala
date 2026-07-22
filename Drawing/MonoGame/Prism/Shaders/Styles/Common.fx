float SampleStyleAlpha(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    return tex2D(
        SecondaryTextureSampler,
        saturate(uv)).a * inside;
}

float StyleBlurAlpha(
    float2 uv,
    float radius)
{
    float value = 4.0 * SampleStyleAlpha(uv);
    float totalWeight = 4.0;
    [unroll]
    for (int ring = 1; ring <= 8; ring++)
    {
        float position = ring / 8.0;
        float weight = exp(-2.0 * position * position);
        float2 stepX = float2(
            PixelSize.x * radius * position,
            0.0);
        float2 stepY = float2(
            0.0,
            PixelSize.y * radius * position);
        float2 diagonal = float2(
            stepX.x * 0.70710678,
            stepY.y * 0.70710678);
        value += weight * (
            SampleStyleAlpha(uv - stepX) +
            SampleStyleAlpha(uv + stepX) +
            SampleStyleAlpha(uv - stepY) +
            SampleStyleAlpha(uv + stepY) +
            SampleStyleAlpha(uv - diagonal) +
            SampleStyleAlpha(uv + diagonal) +
            SampleStyleAlpha(
                uv + float2(diagonal.x, -diagonal.y)) +
            SampleStyleAlpha(
                uv + float2(-diagonal.x, diagonal.y)));
        totalWeight += 8.0 * weight;
    }
    return value / totalWeight;
}

float ApplyStyleContour(
    float value,
    float contour)
{
    value = saturate(value);
    if (contour < 0.5)
    {
        return value;
    }
    if (contour < 1.5)
    {
        return smoothstep(0.0, 1.0, value);
    }
    if (contour < 2.5)
    {
        return 1.0 - abs((2.0 * value) - 1.0);
    }
    return saturate(
        0.5 - (0.5 * cos(value * 6.28318531)));
}

float StyleFlag(float bit)
{
    float flags = StyleModes3.y;
    return fmod(floor(flags / bit), 2.0);
}

float StyleRandom(float2 position)
{
    float value = dot(
        floor(position),
        float2(12.9898, 78.233));
    return frac(
        sin(value + (StyleModes0.x * 17.0)) *
        43758.5453);
}

float GradientCoordinate(
    float2 uv,
    float style,
    float angle,
    float scale,
    float2 offset)
{
    float2 centered =
        (uv - 0.5 - offset) / max(scale, 0.0001);
    float2 direction = float2(cos(angle), -sin(angle));
    float coordinate =
        dot(centered, direction) + 0.5;
    if (style > 0.5 && style < 1.5)
    {
        coordinate =
            length(centered * 1.41421356);
    }
    else if (style > 1.5 && style < 2.5)
    {
        coordinate =
            (atan2(centered.y, centered.x) /
                6.28318531) + 0.5;
    }
    else if (style > 2.5 && style < 3.5)
    {
        coordinate =
            abs((coordinate * 2.0) - 1.0);
    }
    else if (style > 3.5)
    {
        coordinate =
            abs(centered.x) + abs(centered.y);
    }
    return saturate(coordinate);
}

float4 SampleStylePaint(
    VertexShaderOutput input,
    float mask)
{
    float2 uv = ResolveUv(input);
    float linked =
        max(StyleFlag(32.0), StyleFlag(512.0));
    float2 paintUv = lerp(
        input.Position.xy * PixelSize,
        uv,
        linked);
    float paintKind = StyleModes0.w;
    float scale = max(StyleOptions1.x, 0.0001);
    float2 offset = StyleOptions1.zw;
    float4 result = StyleColor;
    if (paintKind > 1.5)
    {
        if (StyleResourceAvailable < 0.5)
        {
            result = float4(0.0, 0.0, 0.0, 0.0);
        }
        else
        {
            float2 patternUv =
                (paintUv / scale) + offset;
            float4 pattern = tex2D(
                StyleTextureSampler,
                patternUv);
            float3 straight =
                pattern.rgb / max(pattern.a, 0.000001);
            result = float4(
                straight * step(0.000001, pattern.a),
                pattern.a);
        }
    }
    else if (paintKind > 0.5)
    {
        float coordinate = GradientCoordinate(
            paintUv,
            StyleModes2.w,
            StyleGeometry1.x,
            scale,
            offset);
        coordinate = lerp(
            smoothstep(0.0, 1.0, coordinate),
            coordinate,
            step(0.5, StyleModes2.z));
        if (StyleResourceAvailable > 0.5)
        {
            float4 resource = tex2D(
                StyleTextureSampler,
                float2(coordinate, 0.5));
            float3 straight =
                resource.rgb /
                max(resource.a, 0.000001);
            result = float4(
                straight *
                    step(0.000001, resource.a),
                resource.a);
        }
        else
        {
            coordinate = lerp(
                coordinate,
                1.0 - coordinate,
                StyleFlag(2.0));
            coordinate = saturate(
                coordinate +
                ((StyleRandom(input.Position.xy) - 0.5) /
                    255.0) *
                StyleFlag(4.0));
            result = lerp(
                StyleColor,
                StyleSecondaryColor,
                coordinate);
        }
    }

    return result;
}

float4 CompositeStyleOver(
    float4 style,
    float4 content,
    int blendMode)
{
    float3 styleStraight = style.a > 0.0
        ? style.rgb / style.a
        : 0.0;
    float3 contentStraight = content.a > 0.0
        ? content.rgb / content.a
        : 0.0;
    return CompositeAssociated(
        style,
        content,
        EvaluateBlendMode(
            blendMode,
            contentStraight,
            styleStraight));
}

