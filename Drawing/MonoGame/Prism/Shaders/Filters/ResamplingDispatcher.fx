float4 ApplyResampling(
    VertexShaderOutput input,
    float4 source,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    if (operation == 0) return ApplyTransform(input, source, profile);
    if (operation == 1) return ApplyAdaptiveWideAngle(input, source, profile);
    if (operation == 2) return ApplyLensCorrection(input, source, profile);
    if (operation == 3) return ApplyDiffuseGlow(input, source, profile);
    if (operation == 4) return ApplyDisplace(input, source, profile);
    if (operation == 5) return ApplyGlass(input, source, profile);
    if (operation == 6) return ApplyOceanRipple(input, source, profile);
    if (operation == 7) return ApplyPinch(input, source, profile);
    if (operation == 8) return ApplyPolarCoordinates(input, source, profile);
    if (operation == 9) return ApplyRipple(input, source, profile);
    if (operation == 10) return ApplyShear(input, source, profile);
    if (operation == 11) return ApplySpherize(input, source, profile);
    if (operation == 12) return ApplyTwirl(input, source, profile);
    if (operation == 13) return ApplyWave(input, source, profile);
    if (operation == 14) return ApplyZigZag(input, source, profile);
    if (operation == 15) return ApplyLiquify(input, source, profile);
    if (operation == 16) return ApplyOffset(input, source, profile);
    return ApplyNeonGlow(input, source, profile);
}

float4 ResamplingFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode = (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = ApplyResampling(input, source, profile);
    int operation = (int)(FilterHeader.x + 0.5);
    int passKind = (int)(FilterHeader.z + 0.5);
    float4 blendSource = source;
    if (operation == 17 && passKind == 7)
    {
        blendSource = WorkingAssociatedToLinearSrgb(
            tex2D(FilterAuxiliaryTextureSampler, ResolveUv(input)),
            profile);
    }
    float3 sourceStraight = saturate(Unpremultiply(blendSource));
    float3 filteredStraight = saturate(Unpremultiply(filtered));
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        sourceStraight,
        filteredStraight);
    float4 blended = float4(
        blendedStraight * filtered.a,
        filtered.a);
    float4 result = lerp(
        blendSource,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(result, profile) * input.Color;
}
