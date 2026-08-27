float EvaluateInnerGlowMask(
    float alpha,
    float local,
    float innerEdge)
{
    return StyleModes2.x < 0.5
        ? innerEdge
        : alpha * saturate(local - innerEdge);
}
