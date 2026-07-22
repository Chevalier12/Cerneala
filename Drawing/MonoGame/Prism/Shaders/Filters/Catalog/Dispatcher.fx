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
        return CatalogConvolution(
            uv,
            source,
            profile);
    }
    else
    {
        return CatalogColor(source, filterId);
    }
}

float4 CatalogFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = ApplyCatalogFilter(
        input,
        source,
        profile);
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

