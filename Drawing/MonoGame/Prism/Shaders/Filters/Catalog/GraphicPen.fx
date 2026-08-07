


float2 GraphicPenStrokeDirection(int direction)
{
    const float diagonal = 0.7071068;
    if (direction == 1)
    {
        return float2(1.0, 0.0);
    }
    if (direction == 2)
    {
        return float2(diagonal, -diagonal);
    }
    if (direction == 3)
    {
        return float2(0.0, 1.0);
    }
    return float2(diagonal, diagonal);
}

float GraphicPenFiniteHatch(
    float2 pixel,
    float2 direction,
    float strokeLength)
{
    const float spacing = 3.25;
    float2 normal = float2(-direction.y, direction.x);
    float along = dot(pixel, direction);
    float across = dot(pixel, normal);
    float row = floor(across / spacing);
    float rowCenter = (row + 0.5) * spacing;
    float acrossDistance = abs(across - rowCenter);
    float widthMask = 1.0 - smoothstep(0.45, 1.05, acrossDistance);

    float gap = max(2.5, strokeLength * 0.3);
    float period = strokeLength + gap;
    float phase = frac(row * 0.6180339) * period;
    float segmentCoordinate = frac((along + phase) / period);
    float alongDistance = abs(segmentCoordinate - 0.5) * period;
    float segmentMask = 1.0 - smoothstep(
        max((strokeLength * 0.5) - 0.75, 0.0),
        (strokeLength * 0.5) + 0.75,
        alongDistance);
    return widthMask * segmentMask;
}

float4 GraphicPenComposite(float2 uv, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float balance = saturate(FilterOptions2.x / 100.0);
    float response = (CharcoalRaw(uv).b * 2.0) - 1.0;
    float edgeThreshold = lerp(0.12, 0.045, balance);
    float edgeLine = smoothstep(
        edgeThreshold * 0.3,
        edgeThreshold,
        max(-response, 0.0));
    float darkness = 1.0 - CatalogLuminance(original);
    float toneThreshold = lerp(0.72, 0.18, balance);
    float tone = smoothstep(
        toneThreshold,
        toneThreshold + 0.22,
        darkness);
    float strokeLength = clamp(FilterOptions4.x, 1.0, 96.0);
    float2 direction = GraphicPenStrokeDirection(
        (int)(FilterOptions3.x + 0.5));
    float2 pixel = uv / PixelSize;
    float hatch = GraphicPenFiniteHatch(
        pixel,
        direction,
        strokeLength);
    float coverage = saturate(max(edgeLine, hatch * tone));
    float3 straight = lerp(
        saturate(FilterOptions0.rgb),
        saturate(FilterOptions1.rgb),
        coverage);
    return float4(straight * original.a, original.a);
}
