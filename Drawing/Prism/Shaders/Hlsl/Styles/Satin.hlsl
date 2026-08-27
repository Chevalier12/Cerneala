float EvaluateSatinMask(
    float2 uv,
    float alpha,
    float2 offset,
    float size)
{
    float first = StyleBlurAlpha(
        uv - offset,
        size);
    float second = StyleBlurAlpha(
        uv + offset,
        size);
    float mask = alpha * abs(first - second);
    return lerp(
        mask,
        alpha - mask,
        StyleFlag(8.0));
}
