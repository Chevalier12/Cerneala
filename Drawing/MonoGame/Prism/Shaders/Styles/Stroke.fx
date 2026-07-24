static const float2 InvalidStrokeSeed = float2(-1.0, -1.0);

bool IsValidStrokeSeed(float2 seed)
{
    return seed.x >= 0.0 && seed.y >= 0.0;
}

float StrokeSeedDistanceSquared(float2 uv, float2 seed)
{
    float2 delta = (uv - seed) / PixelSize;
    return dot(delta, delta);
}

float2 SelectNearestStrokeSeed(
    float2 uv,
    float2 current,
    float2 candidate)
{
    if (!IsValidStrokeSeed(candidate))
    {
        return current;
    }
    if (!IsValidStrokeSeed(current) ||
        StrokeSeedDistanceSquared(uv, candidate) <
            StrokeSeedDistanceSquared(uv, current))
    {
        return candidate;
    }
    return current;
}

float4 StrokeDistanceSeedPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float alpha = SampleSource(input).a;
    bool inside = alpha >= 0.5;
    return float4(
        inside ? uv : InvalidStrokeSeed,
        inside ? InvalidStrokeSeed : uv);
}

float4 SampleStrokeDistanceField(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    float4 field = tex2D(
        SpriteTextureSampler,
        saturate(uv));
    return lerp(
        float4(
            InvalidStrokeSeed,
            InvalidStrokeSeed),
        field,
        inside);
}

float4 StrokeDistanceFloodPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float4 nearest = SampleStrokeDistanceField(uv);

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 sampleUv = uv +
                float2(x, y) * MaskFeatherStep;
            float4 candidate =
                SampleStrokeDistanceField(sampleUv);
            nearest.xy = SelectNearestStrokeSeed(
                uv,
                nearest.xy,
                candidate.xy);
            nearest.zw = SelectNearestStrokeSeed(
                uv,
                nearest.zw,
                candidate.zw);
        }
    }

    return nearest;
}

float StrokeSignedEuclideanDistance(float2 uv, float alpha)
{
    float4 field = tex2D(
        StyleMaskTextureSampler,
        saturate(uv));
    bool inside = alpha >= 0.5;
    float2 seed = inside ? field.zw : field.xy;
    if (!IsValidStrokeSeed(seed))
    {
        return inside ? -65504.0 : 65504.0;
    }

    float distance = sqrt(
        StrokeSeedDistanceSquared(uv, seed));
    float edgeDistance = max(distance - 0.5, 0.0);
    return inside ? -edgeDistance : edgeDistance;
}

float EvaluateStrokeMask(
    float2 uv,
    float alpha,
    float size)
{
    float signedDistance =
        StrokeSignedEuclideanDistance(uv, alpha);
    float position = StyleModes1.w;
    float outsideSize = position < 0.5
        ? size
        : position < 1.5 ? size * 0.5 : 0.0;
    float insideSize = position < 0.5
        ? 0.0
        : position < 1.5 ? size * 0.5 : size;
    float outsideMask =
        saturate(outsideSize + 0.5 - signedDistance) *
        (1.0 - alpha) *
        step(0.0001, outsideSize);
    float insideMask =
        saturate(insideSize + 0.5 + signedDistance) *
        alpha *
        step(0.0001, insideSize);
    return saturate(outsideMask + insideMask);
}
