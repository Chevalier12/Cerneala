float3 ApplyAdjustment(
    float3 color,
    VertexShaderOutput input)
{
    int operation = (int)(FilterHeader.x + 0.5);
    if (operation == 0) return ApplyBrightnessContrast(color, input);
    if (operation == 1) return ApplyLevels(color, input);
    if (operation == 2) return ApplyCurves(color, input);
    if (operation == 3) return ApplyExposure(color, input);
    if (operation == 4) return ApplyVibrance(color, input);
    if (operation == 5) return ApplyHueSaturation(color, input);
    if (operation == 6) return ApplyColorBalance(color, input);
    if (operation == 7) return ApplyBlackWhite(color, input);
    if (operation == 8) return ApplyPhotoFilter(color, input);
    if (operation == 9) return ApplyChannelMixer(color, input);
    if (operation == 10) return ApplyColorLookup(color, input);
    if (operation == 11) return ApplyInvert(color, input);
    if (operation == 12) return ApplyPosterize(color, input);
    if (operation == 13) return ApplyThreshold(color, input);
    if (operation == 14) return ApplyGradientMap(color, input);
    return ApplySelectiveColor(color, input);
}

float4 AdjustmentFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode = (int)(FilterHeader.z + 0.5);
    float4 linearSource = WorkingAssociatedToLinearSrgb(source, profile);
    float3 linearColor = Unpremultiply(linearSource);
    float3 adjusted = saturate(ApplyAdjustment(linearColor, input));
    float3 blended = EvaluateBlendMode(
        blendMode,
        saturate(linearColor),
        adjusted);
    float3 result = lerp(
        linearColor,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(
        float4(result * source.a, source.a),
        profile) * input.Color;
}
