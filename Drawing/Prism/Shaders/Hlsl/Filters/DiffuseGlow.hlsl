float4 ApplyDiffuseGlow(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    int passKind = (int)(FilterHeader.z + 0.5);
    if (passKind == 1)
    {
        return DiffuseGlowHorizontal(uv, profile, 0.0);
    }
    if (passKind == 2)
    {
        float4 bloom = DiffuseGlowVertical(uv, profile, 0.0);
        float4 original = WorkingAssociatedToLinearSrgb(
            tex2D(FilterAuxiliaryTextureSampler, uv),
            profile);
        float strength =
            saturate(FilterOptions0.y) * saturate(FilterOptions1.a);
        float3 combined = min(
            original.rgb + (bloom.rgb * FilterOptions1.rgb * strength),
            original.a.xxx);
        return float4(combined, original.a);
    }
    float noise = NeighborhoodHash(input.Position.xy, 9173.0);
    float3 straight = saturate(
        Unpremultiply(source) +
        ((noise - 0.5) * saturate(FilterOptions0.x)));
    return float4(straight * source.a, source.a);
}
