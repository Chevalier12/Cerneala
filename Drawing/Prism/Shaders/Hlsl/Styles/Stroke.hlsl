float EvaluateStrokeMask(
    float2 uv,
    float alpha,
    float size)
{
    float signedDistance =
        StyleSignedEuclideanDistance(uv, alpha);
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
