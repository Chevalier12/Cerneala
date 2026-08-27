float4 CatalogPlasticWrap(
    float2 uv,
    float4 source,
    int profile)
{
    float highlightStrength = max(FilterOptions0.x, 0.0);
    if (source.a <= 0.0 || highlightStrength <= 0.0)
    {
        return source;
    }

    float detail = saturate(FilterOptions1.x / 15.0);
    float smoothness = saturate(FilterOptions2.x / 15.0);
    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float2 horizontal = float2(PixelSize.x * radius, 0.0);
    float2 vertical = float2(0.0, PixelSize.y * radius);
    float left = CatalogLuminance(
        CatalogLinearSample(uv - horizontal, profile));
    float right = CatalogLuminance(
        CatalogLinearSample(uv + horizontal, profile));
    float top = CatalogLuminance(
        CatalogLinearSample(uv - vertical, profile));
    float bottom = CatalogLuminance(
        CatalogLinearSample(uv + vertical, profile));
    float heightScale = 6.0 * detail;
    float3 normal = normalize(
        float3(
            -(right - left) * heightScale,
            -(bottom - top) * heightScale,
            1.0));
    float3 view = float3(0.0, 0.0, 1.0);
    float3 surfaceToLight = normalize(
        float3(-0.45, -0.55, 1.0));
    float roughness = max(
        0.045,
        0.4 - (0.3 * smoothness));
    float normalDotLight = max(
        dot(normal, surfaceToLight),
        0.0);
    float3 specular = CatalogCookTorranceGgxSpecular(
        0.04,
        normal,
        view,
        surfaceToLight,
        roughness);
    float effectAmount = saturate(highlightStrength / 20.0);
    float diffuseShade = lerp(
        1.0,
        0.55 + (0.45 * normalDotLight),
        effectAmount);
    float specularGain =
        highlightStrength *
        (0.65 + (0.35 * smoothness));
    float3 result =
        (saturate(Unpremultiply(source)) * diffuseShade) +
        (specular * normalDotLight * specularGain);
    return float4(saturate(result) * source.a, source.a);
}
