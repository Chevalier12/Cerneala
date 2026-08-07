float ShapeBevelHeight(
    float coordinate,
    float styleTechnique)
{
    coordinate = saturate(coordinate);
    if (styleTechnique < 0.5)
    {
        return smoothstep(0.0, 1.0, coordinate);
    }
    if (styleTechnique < 1.5)
    {
        return coordinate;
    }
    return saturate(coordinate * 2.0);
}

float SampleBevelTextureHeight(
    VertexShaderOutput input,
    float2 baseUv,
    float2 sampleUv)
{
    if (StyleFlag(64.0) < 0.5 ||
        StyleResourceAvailable < 0.5)
    {
        return 0.0;
    }

    float2 delta = sampleUv - baseUv;
    float2 screenUv =
        (input.Position.xy * PixelSize) + delta;
    float2 textureUv = lerp(
        screenUv,
        sampleUv,
        StyleFlag(512.0));
    float4 sample = tex2D(
        StyleTextureSampler,
        (textureUv / max(StyleOptions1.x, 0.0001)) +
            StyleOptions1.zw);
    float value = lerp(
        sample.a,
        1.0 - sample.a,
        StyleFlag(128.0));
    return ((value * 2.0) - 1.0) *
        StyleOptions1.y * 0.125;
}

float BevelHeightFromDistance(
    float distance,
    float bevelStyle,
    float styleTechnique)
{
    float extent = max(
        StyleGeometry0.z + StyleGeometry1.w,
        0.5);
    float coordinate;
    if (bevelStyle < 0.5)
    {
        coordinate = saturate(-distance / extent);
    }
    else if (bevelStyle < 1.5)
    {
        coordinate = saturate(
            1.0 - (max(distance, 0.0) / extent));
    }
    else if (bevelStyle < 2.5)
    {
        coordinate = saturate(
            0.5 - (distance / (2.0 * extent)));
    }
    else if (bevelStyle < 3.5)
    {
        coordinate = saturate(abs(distance) / extent);
    }
    else
    {
        coordinate = 1.0 - saturate(
            abs(distance) / extent);
    }

    return ShapeBevelHeight(
        coordinate,
        styleTechnique);
}

float3 SobelBevelNormal(
    float2 uv)
{
    float2 dx = float2(PixelSize.x, 0.0);
    float2 dy = float2(0.0, PixelSize.y);
    float topLeft = tex2D(
        SpriteTextureSampler, uv - dx - dy).r;
    float top = tex2D(
        SpriteTextureSampler, uv - dy).r;
    float topRight = tex2D(
        SpriteTextureSampler, uv + dx - dy).r;
    float left = tex2D(
        SpriteTextureSampler, uv - dx).r;
    float right = tex2D(
        SpriteTextureSampler, uv + dx).r;
    float bottomLeft = tex2D(
        SpriteTextureSampler, uv - dx + dy).r;
    float bottom = tex2D(
        SpriteTextureSampler, uv + dy).r;
    float bottomRight = tex2D(
        SpriteTextureSampler, uv + dx + dy).r;
    float gradientX = (
        topRight + (2.0 * right) + bottomRight -
        topLeft - (2.0 * left) - bottomLeft) / 8.0;
    float gradientY = (
        bottomLeft + (2.0 * bottom) + bottomRight -
        topLeft - (2.0 * top) - topRight) / 8.0;

    float extent = max(
        StyleGeometry0.z + StyleGeometry1.w,
        0.5);
    float slopeScale =
        max(StyleGeometry1.z, 0.0) * extent;
    return normalize(float3(
        -gradientX * slopeScale,
        -gradientY * slopeScale,
        1.0));
}

float BevelSupport(
    float distance,
    float alpha,
    float bevelStyle)
{
    float extent = max(
        StyleGeometry0.z + StyleGeometry1.w,
        0.5);
    float inRange = step(abs(distance), extent);
    if (bevelStyle < 0.5)
    {
        return inRange * alpha * step(distance, 0.0);
    }
    if (bevelStyle < 1.5)
    {
        return inRange * (1.0 - alpha) * step(0.0, distance);
    }
    return inRange;
}

float4 BevelHeightPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float alpha = SampleStyleAlpha(uv);
    float distance = StyleSignedEuclideanDistance(
        uv,
        alpha);
    float bevelStyle = StyleModes3.x;
    float height = BevelHeightFromDistance(
        distance,
        bevelStyle,
        StyleModes1.z);
    if (StyleFlag(256.0) > 0.5)
    {
        height = ApplyStyleContour(
            saturate(
                height /
                max(StyleModes3.z, 0.0001)),
            StyleModes1.y);
        height = lerp(
            height,
            smoothstep(0.0, 1.0, height),
            StyleFlag(1024.0));
    }
    float support = BevelSupport(distance, alpha, bevelStyle);
    height = saturate(
        height +
        SampleBevelTextureHeight(input, uv, uv) * support);
    return float4(
        height,
        support,
        alpha,
        1.0);
}

float4 BevelLightingPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float lightAngle = StyleGeometry1.x;
    float altitude = StyleGeometry1.y;
    float4 bevelField = tex2D(
        SpriteTextureSampler,
        uv);
    float3 normal = SobelBevelNormal(uv);
    float cosineAltitude = cos(altitude);
    float3 light = normalize(float3(
        cos(lightAngle) * cosineAltitude,
        -sin(lightAngle) * cosineAltitude,
        max(sin(altitude), 0.0001)));
    float signedLight = dot(normal, light) - light.z;
    signedLight *= lerp(
        1.0,
        -1.0,
        step(0.5, StyleModes2.y));
    float edgeAmount = ApplyStyleContour(
        saturate(abs(signedLight) * 2.0),
        StyleModes1.x);
    edgeAmount = lerp(
        edgeAmount,
        smoothstep(0.0, 1.0, edgeAmount),
        StyleFlag(1.0));
    float signedEdge = sign(signedLight) *
        edgeAmount *
        bevelField.g;
    return float4(
        saturate(signedEdge),
        saturate(-signedEdge),
        bevelField.g,
        1.0);
}

float4 CompositeBevelEmbossStyle(
    VertexShaderOutput input,
    float4 content,
    float2 uv,
    int blendMode,
    int secondaryBlendMode)
{
    float4 bevelLighting = tex2D(
        StyleDistanceTextureSampler,
        uv);
    float highlightAlpha =
        bevelLighting.r *
        StyleOptions0.x *
        StyleColor.a;
    float shadowAlpha =
        bevelLighting.g *
        StyleOptions0.y *
        StyleSecondaryColor.a;
    float4 shadow = float4(
        StyleSecondaryColor.rgb * shadowAlpha,
        shadowAlpha);
    float4 highlight = float4(
        StyleColor.rgb * highlightAlpha,
        highlightAlpha);
    float4 beveled = CompositeStyleOver(
        shadow,
        content,
        secondaryBlendMode);
    return CompositeStyleOver(
        highlight,
        beveled,
        blendMode) *
        input.Color *
        Opacity;
}
