



static const float2 InvalidStyleDistanceSeed = float2(-1.0, -1.0);
static const float StyleDistanceSqrtTwo = 1.41421356237;
static const float StyleDistanceStrokeKind = 9.0;

bool IsValidStyleDistanceSeed(float4 seed)
{
    return seed.w > 0.5 && seed.x >= 0.0 && seed.y >= 0.0;
}

float SampleStyleDistanceSourceAlpha(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    return tex2D(
        StyleMaskSourceSampler,
        saturate(uv)).a * inside;
}

float StyleDistanceSquared(float2 uv, float2 seed)
{
    float2 delta = (uv - seed) / PixelSize;
    return dot(delta, delta);
}

float StyleAntiAliasedEdgeDistance(float2 gradient, float alpha)
{
    float gradientLength = length(gradient);
    float2 direction = abs(gradient) / gradientLength;
    float major = max(direction.x, direction.y);
    float minor = min(direction.x, direction.y);
    float cornerArea = 0.5 * minor / major;
    float coverage = saturate(alpha);

    if (coverage < cornerArea)
    {
        return 0.5 * (major + minor) -
            sqrt(max(2.0 * major * minor * coverage, 0.0));
    }
    if (coverage < 1.0 - cornerArea)
    {
        return (0.5 - coverage) * major;
    }

    return -0.5 * (major + minor) +
        sqrt(max(
            2.0 * major * minor * (1.0 - coverage),
            0.0));
}

float4 SelectNearestStyleDistanceSeed(
    float2 uv,
    float4 current,
    float4 candidate)
{
    if (!IsValidStyleDistanceSeed(candidate))
    {
        return current;
    }
    if (!IsValidStyleDistanceSeed(current))
    {
        return candidate;
    }

    float currentDistance =
        StyleDistanceSquared(uv, current.xy);
    float candidateDistance =
        StyleDistanceSquared(uv, candidate.xy);
    bool nearer = candidateDistance < currentDistance - 0.0001;
    bool tied = abs(candidateDistance - currentDistance) <= 0.0001;
    bool ordered = candidate.x < current.x ||
        (candidate.x == current.x && candidate.y < current.y);
    return nearer || (tied && ordered)
        ? candidate
        : current;
}

float4 StyleDistanceSeedPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float alpha = SampleStyleDistanceSourceAlpha(uv);
    float left = SampleStyleDistanceSourceAlpha(
        uv - float2(PixelSize.x, 0.0));
    float right = SampleStyleDistanceSourceAlpha(
        uv + float2(PixelSize.x, 0.0));
    float top = SampleStyleDistanceSourceAlpha(
        uv - float2(0.0, PixelSize.y));
    float bottom = SampleStyleDistanceSourceAlpha(
        uv + float2(0.0, PixelSize.y));
    float topLeft = SampleStyleDistanceSourceAlpha(
        uv - PixelSize);
    float topRight = SampleStyleDistanceSourceAlpha(
        uv + float2(PixelSize.x, -PixelSize.y));
    float bottomLeft = SampleStyleDistanceSourceAlpha(
        uv + float2(-PixelSize.x, PixelSize.y));
    float bottomRight = SampleStyleDistanceSourceAlpha(
        uv + PixelSize);
    bool useDirectionalCoverage =
        abs(StyleModes0.x - StyleDistanceStrokeKind) < 0.5;
    float minimumAlpha = min(
        alpha,
        min(min(left, right), min(top, bottom)));
    float maximumAlpha = max(
        alpha,
        max(max(left, right), max(top, bottom)));
    if (useDirectionalCoverage)
    {
        minimumAlpha = min(
            minimumAlpha,
            min(
                min(topLeft, topRight),
                min(bottomLeft, bottomRight)));
        maximumAlpha = max(
            maximumAlpha,
            max(
                max(topLeft, topRight),
                max(bottomLeft, bottomRight)));
    }
    bool crossesEdge =
        minimumAlpha <= 0.5 &&
        maximumAlpha >= 0.5 &&
        maximumAlpha - minimumAlpha > 0.0001;
    if (!crossesEdge)
    {
        return float4(InvalidStyleDistanceSeed, 0.0, 0.0);
    }

    float2 gradient = float2(
        right - left,
        bottom - top);
    if (useDirectionalCoverage)
    {
        gradient = float2(
            topRight + StyleDistanceSqrtTwo * right + bottomRight -
                topLeft - StyleDistanceSqrtTwo * left - bottomLeft,
            bottomLeft + StyleDistanceSqrtTwo * bottom + bottomRight -
                topLeft - StyleDistanceSqrtTwo * top - topRight);
    }
    float gradientLength = length(gradient);
    if (gradientLength <= 0.0001)
    {


        return float4(uv, 0.5, 1.0);
    }

    float2 edge;
    if (useDirectionalCoverage)
    {
        float edgeDistance =
            StyleAntiAliasedEdgeDistance(gradient, alpha);
        edge = uv +
            (gradient / gradientLength) *
            edgeDistance * PixelSize;
    }
    else
    {
        float signedOffset =
            (alpha - 0.5) / gradientLength;
        edge = uv -
            (gradient / gradientLength) *
            signedOffset * PixelSize;
    }
    return float4(edge, 0.0, 1.0);
}

float4 SampleStyleDistanceField(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    float4 field = tex2D(
        SpriteTextureSampler,
        saturate(uv));
    return inside > 0.5
        ? field
        : float4(InvalidStyleDistanceSeed, 0.0, 0.0);
}

float4 StyleDistanceFloodPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float4 nearest = SampleStyleDistanceField(uv);

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 sampleUv = uv +
                float2(x, y) * MaskFeatherStep;
            nearest = SelectNearestStyleDistanceSeed(
                uv,
                nearest,
                SampleStyleDistanceField(sampleUv));
        }
    }

    return nearest;
}

float StyleSignedEuclideanDistance(float2 uv, float alpha)
{
    float4 field = tex2D(
        StyleDistanceTextureSampler,
        saturate(uv));
    if (!IsValidStyleDistanceSeed(field))
    {
        return alpha >= 0.5 ? -65504.0 : 65504.0;
    }

    float distance = max(
        sqrt(StyleDistanceSquared(uv, field.xy)) - field.z,
        0.0);
    return alpha >= 0.5 ? -distance : distance;
}
