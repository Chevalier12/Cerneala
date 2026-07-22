float EvaluateStrokeMask(
    float outerEdge,
    float innerEdge)
{
    if (StyleModes1.w < 0.5)
    {
        return outerEdge;
    }
    if (StyleModes1.w < 1.5)
    {
        return saturate(outerEdge + innerEdge);
    }
    return innerEdge;
}
