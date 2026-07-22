float EvaluateInnerShadowMask(
    float alpha,
    float shifted,
    float spreadRatio)
{
    return alpha * saturate(
        1.0 - shifted + spreadRatio);
}
