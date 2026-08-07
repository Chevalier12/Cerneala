float4 ApplyCatalogFilter(
    VertexShaderOutput input,
    float4 source,
    int profile)
{
    float2 uv = ResolveUv(input);
    int filterId =
        (int)(FilterHeader.x + 0.5);
    int primitive =
        (int)(FilterHeader.z + 0.5);
    if (primitive == 0)
    {
        return CatalogMorphology(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 1)
    {
        return CatalogQuantization(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 2)
    {
        return CatalogProcedural(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 3)
    {
        return CatalogVideo(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 4)
    {
        return CatalogArtistic(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 5)
    {
        return CatalogEdge(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 6)
    {
        return CatalogTiling(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 7)
    {
        return CatalogTexture(
            uv,
            source,
            filterId,
            profile);
    }
    else if (primitive == 8)
    {
        return CatalogCustomConvolution(
            uv,
            source,
            profile);
    }
    else if (primitive == 10)
    {
        return CatalogExtrude(
            uv,
            source,
            filterId,
            profile);
    }
    else
    {
        return CatalogColor(source, filterId);
    }
}

float4 FinalizeCatalogFilter(
    VertexShaderOutput input,
    float4 source,
    float4 filtered,
    int profile)
{
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    int filterId =
        (int)(FilterHeader.x + 0.5);
    bool preserveExtendedRange =
        (filterId == 131 && FilterOptions0.x < 0.5) ||
        (filterId == 132 && FilterOptions1.x < 0.5);
    if (preserveExtendedRange && blendMode == 145)
    {
        float4 extendedResult = lerp(
            source,
            filtered,
            saturate(Opacity));
        return LinearSrgbAssociatedToWorking(
            extendedResult,
            profile) * input.Color;
    }
    filtered.a = saturate(filtered.a);
    filtered.rgb = clamp(
        filtered.rgb,
        0.0,
        filtered.a);
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        saturate(Unpremultiply(source)),
        saturate(Unpremultiply(filtered)));
    float4 blended = float4(
        blendedStraight * filtered.a,
        filtered.a);
    float4 result = lerp(
        source,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(
        result,
        profile) * input.Color;
}

float4 CatalogFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        ApplyCatalogFilter(
            input,
            source,
            profile),
        profile);
}

float4 DryBrushFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogDryBrush(
            uv,
            source,
            profile),
        profile);
}

float4 UnderpaintingFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogUnderpainting(
            uv,
            source,
            profile),
        profile);
}

float4 WatercolorFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex <= 1)
    {
        return LinearSrgbAssociatedToWorking(
            WatercolorMeanShift(uv, profile),
            profile) * input.Color;
    }
    if (passIndex <= 5)
    {
        return LinearSrgbAssociatedToWorking(
            WatercolorMorphology(
                uv,
                profile,
                passIndex == 2 || passIndex == 5),
            profile) * input.Color;
    }

    float4 original = WatercolorOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        WatercolorComposite(uv, profile),
        profile);
}

float4 WaterPaperFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return LinearSrgbAssociatedToWorking(
            WaterPaperPreparePigment(uv, profile),
            profile) * input.Color;
    }

    float4 original = WaterPaperOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        WaterPaperComposite(uv, profile),
        profile);
}

float4 WindFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return LinearSrgbAssociatedToWorking(
            WindLineIntegral(uv, profile),
            profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return LinearSrgbAssociatedToWorking(
            WindEnhanceContrast(uv, profile),
            profile) * input.Color;
    }

    float4 original = WindOriginalSample(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        WindLineIntegral(uv, profile),
        profile);
}

float4 SumiEFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return LinearSrgbAssociatedToWorking(
            SumiEDirectionalWash(uv, profile),
            profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return SumiEHorizontalXDog(uv, profile) * input.Color;
    }

    float4 original = SumiEOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        SumiEComposite(uv, profile),
        profile);
}

float4 ColoredPencilFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return ColoredPencilTensor(uv, profile) *
            input.Color;
    }
    if (passIndex == 1)
    {
        return ColoredPencilBlur(uv, true) *
            input.Color;
    }
    if (passIndex == 2)
    {
        return ColoredPencilBlur(uv, false) *
            input.Color;
    }

    float4 original = ColoredPencilOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        ColoredPencilComposite(uv, profile),
        profile);
}

float4 FrescoFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return FrescoStructureTensor(uv, profile) *
            input.Color;
    }
    if (passIndex == 1)
    {
        return FrescoBlurTensor(uv, true) *
            input.Color;
    }
    if (passIndex == 2)
    {
        return FrescoBlurTensor(uv, false) *
            input.Color;
    }

    float4 original = FrescoOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        FrescoKuwahara(uv, profile),
        profile);
}

float4 CutoutFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passKind = (int)fmod(FilterOptions9.z, 4.0);
    float2 uv = ResolveUv(input);
    if (passKind == 3)
    {
        return CutoutMeanShift(uv, profile) *
            input.Color;
    }

    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        CutoutOriginal(uv, profile),
        CutoutQuantize(source),
        profile);
}

float4 PosterEdgesFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return PosterEdgesBoxBlur(
            uv,
            profile,
            true,
            true) * input.Color;
    }
    if (passIndex == 1)
    {
        return PosterEdgesBoxBlur(
            uv,
            profile,
            false,
            false) * input.Color;
    }
    if (passIndex == 2)
    {
        return PosterEdgesCoefficients(uv, profile) * input.Color;
    }
    if (passIndex == 3)
    {
        return PosterEdgesBoxBlur(
            uv,
            profile,
            true,
            false) * input.Color;
    }
    if (passIndex == 4)
    {
        return PosterEdgesGuidedColor(uv, profile) * input.Color;
    }

    int filterId = (int)(FilterHeader.x + 0.5);
    float4 original = PosterEdgesOriginal(uv, profile);
    float4 filtered = filterId == 100
        ? BasReliefComposite(uv)
        : PosterEdgesComposite(uv);
    return FinalizeCatalogFilter(
        input,
        original,
        filtered,
        profile);
}

float4 AccentedEdgesFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return AccentedEdgesHorizontal(uv, profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return AccentedEdgesVertical(uv) * input.Color;
    }

    float4 original = AccentedEdgesOriginal(uv, profile);
    int filterId = (int)(FilterHeader.x + 0.5);
    float4 filtered = filterId == 95
        ? DarkStrokesComposite(uv, original)
        : filterId == 96
            ? InkOutlinesComposite(uv, original)
            : AccentedEdgesComposite(uv, original);
    return FinalizeCatalogFilter(
        input,
        original,
        filtered,
        profile);
}

float4 GlowingEdgesFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return GlowingEdgesExtract(uv, profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return GlowingEdgesHorizontal(uv) * input.Color;
    }

    float4 original = GlowingEdgesOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        GlowingEdgesVerticalComposite(uv, profile, original),
        profile);
}

float4 TraceContourFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogTraceContour(uv, source, profile),
        profile);
}

float4 ChalkCharcoalFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return ChalkCharcoalHorizontal(uv, profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return ChalkCharcoalVertical(uv) * input.Color;
    }

    float4 original = ChalkCharcoalOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        ChalkCharcoalComposite(uv, original),
        profile);
}

float4 ChromeFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return LinearSrgbAssociatedToWorking(
            ChromeBlurLuminance(uv, profile, true),
            profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return LinearSrgbAssociatedToWorking(
            ChromeBlurLuminance(uv, profile, false),
            profile) * input.Color;
    }

    float4 original = ChromeOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        ChromeComposite(uv, profile, original),
        profile);
}

float4 NotePaperFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return LinearSrgbAssociatedToWorking(
            NotePaperBlurLuminance(uv, profile, true),
            profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return LinearSrgbAssociatedToWorking(
            NotePaperBuildHeight(uv, profile),
            profile) * input.Color;
    }

    float4 original = NotePaperOriginal(uv, profile);
    return FinalizeCatalogFilter(
        input,
        original,
        NotePaperComposite(uv, profile, original),
        profile);
}

float4 PhotocopyFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return PhotocopyHorizontal(uv, profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return PhotocopyVertical(uv) * input.Color;
    }

    float4 original = PhotocopyOriginal(uv, profile);
    int filterId = (int)(FilterHeader.x + 0.5);
    float4 filtered = filterId == 112
        ? TornEdgesComposite(uv, original)
        : filterId == 111
            ? StampComposite(uv, original)
            : PhotocopyComposite(uv, original);
    return FinalizeCatalogFilter(
        input,
        original,
        filtered,
        profile);
}

float4 ReticulationFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 pixel = ResolveUv(input) / PixelSize;
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogReticulation(pixel, source),
        profile);
}

float4 StainedGlassFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passKind = (int)fmod(FilterOptions9.z, 4.0);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return StainedGlassSeedPass(uv) * input.Color;
    }
    if (passKind == 3)
    {
        return StainedGlassFloodPass(uv) * input.Color;
    }

    float4 original = WorkingAssociatedToLinearSrgb(
        tex2D(StainedGlassOriginalSampler, uv),
        profile);
    return FinalizeCatalogFilter(
        input,
        original,
        StainedGlassComposite(uv, profile, original),
        profile);
}

float4 CraquelureFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 pixel = ResolveUv(input) / PixelSize;
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogCraquelure(pixel, source),
        profile);
}

float4 TexturizerFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogTexturizer(ResolveUv(input), source),
        profile);
}

float4 GrainFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 pixel = ResolveUv(input) / PixelSize;
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogGrain(pixel, source),
        profile);
}

float4 MosaicTilesFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogMosaicTiles(uv, profile),
        profile);
}

float4 PatchworkFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogPatchwork(uv, profile),
        profile);
}

float4 WaveNoiseFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int filterId = (int)(FilterHeader.x + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 pixel = ResolveUv(input) / PixelSize;
    float scale = max(FilterOptions2.x, 0.0001);
    float noise = CatalogWaveNoise(pixel / scale);
    float4 filtered = filterId == 71
        ? CatalogDifferenceClouds(source, noise)
        : CatalogClouds(source, noise);
    return FinalizeCatalogFilter(
        input,
        source,
        filtered,
        profile);
}

float4 SpatterFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogSpatter(uv, source, profile),
        profile);
}

float4 SprayedStrokesFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogSprayedStrokes(uv, source, profile),
        profile);
}


float4 ColorHalftoneFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 pixel = ResolveUv(input) / PixelSize;
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogColorHalftone(pixel, source),
        profile);
}

float4 FacetFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float2 uv = ResolveUv(input);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogFacet(uv, source, profile),
        profile);
}

float4 LightingEffectsFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return FinalizeCatalogFilter(
        input,
        source,
        CatalogLightingEffects(
            ResolveUv(input),
            source),
        profile);
}

