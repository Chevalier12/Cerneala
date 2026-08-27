float4 ApplyNeighborhood(
    VertexShaderOutput input,
    float4 center,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    if (operation == 0) return ApplyAverage(input, center, profile);
    if (operation == 1) return ApplyBlur(input, center, profile);
    if (operation == 2) return ApplyBlurMore(input, center, profile);
    if (operation == 3) return ApplyBoxBlur(input, center, profile);
    if (operation == 4) return ApplyGaussianBlur(input, center, profile);
    if (operation == 5) return ApplyLensBlur(input, center, profile);
    if (operation == 6) return ApplyMotionBlur(input, center, profile);
    if (operation == 7) return ApplyRadialBlur(input, center, profile);
    if (operation == 8) return ApplyShapeBlur(input, center, profile);
    if (operation == 9) return ApplySmartBlur(input, center, profile);
    if (operation == 10) return ApplySurfaceBlur(input, center, profile);
    if (operation == 11) return ApplyFieldBlur(input, center, profile);
    if (operation == 12) return ApplyIrisBlur(input, center, profile);
    if (operation == 13) return ApplyTiltShift(input, center, profile);
    if (operation == 14) return ApplyPathBlur(input, center, profile);
    if (operation == 15) return ApplySpinBlur(input, center, profile);
    if (operation == 16) return ApplySharpen(input, center, profile);
    if (operation == 17) return ApplySharpenMore(input, center, profile);
    if (operation == 18) return ApplySharpenEdges(input, center, profile);
    if (operation == 19) return ApplyUnsharpMask(input, center, profile);
    if (operation == 20) return ApplySmartSharpen(input, center, profile);
    if (operation == 21) return ApplyHighPass(input, center, profile);
    if (operation == 22) return ApplyAddNoise(input, center, profile);
    if (operation == 24) return ApplyDustScratches(input, center, profile);
    if (operation == 25) return ApplyMedian(input, center, profile);
    if (operation == 26) return ApplyReduceNoise(input, center, profile);
    return center;
}

float4 NeighborhoodFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int operation = (int)(FilterHeader.x + 0.5);
    int passKind = (int)(FilterHeader.z + 0.5);
    int blendMode = (int)(FilterOptions9.w + 0.5);
    if (operation == 23 && passKind != 10)
    {
        return ApplyDespeckleStatePass(input, profile, passKind)
            * input.Color;
    }

    float4 source;
    float4 filtered;
    if (operation == 23)
    {
        source = ReadDespeckleSource(input, profile);
        filtered = ReadDespeckleResult(input, profile);
    }
    else
    {
        source = WorkingAssociatedToLinearSrgb(
            SampleSource(input),
            profile);
        filtered = ApplyNeighborhood(input, source, profile);
    }
    if (operation == 20 && (passKind == 4 || passKind == 5))
    {
        return filtered * input.Color;
    }
    float4 blendSource =
        ((operation == 19 || operation == 20 || operation == 26) &&
            passKind == 7)
            ? SampleNeighborhoodOriginal(
                NeighborhoodUnclampedUv(input),
                profile)
            : source;
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
