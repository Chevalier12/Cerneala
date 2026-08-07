float CatalogLightingHeight(float2 uv)
{
    float4 sample = tex2D(
        FilterAuxiliaryTextureSampler,
        clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5)));
    float3 straight = sample.a > 0.0
        ? sample.rgb / sample.a
        : 0.0;
    return saturate(
        dot(
            straight,
            float3(0.2126, 0.7152, 0.0722)));
}

float3 CatalogLightingNormal(float2 uv)
{
    float textureHeight = max(FilterOptions6.x, 0.0);
    if (FilterHeader.w < 2.0 || textureHeight <= 0.0)
    {
        return float3(0.0, 0.0, 1.0);
    }

    float left = CatalogLightingHeight(
        uv - float2(PixelSize.x, 0.0));
    float right = CatalogLightingHeight(
        uv + float2(PixelSize.x, 0.0));
    float top = CatalogLightingHeight(
        uv - float2(0.0, PixelSize.y));
    float bottom = CatalogLightingHeight(
        uv + float2(0.0, PixelSize.y));
    return normalize(
        float3(
            -(right - left) * textureHeight * 0.5,
            -(bottom - top) * textureHeight * 0.5,
            1.0));
}

float4 CatalogLightingEffects(
    float2 uv,
    float4 source)
{
    const float minimumRoughness = 0.045;
    const float minimumDenominator = 0.00001;
    float3 baseColor = saturate(Unpremultiply(source));
    float metallic = saturate(FilterOptions2.x);
    float gloss = saturate(FilterOptions3.x);
    float roughness = max(
        minimumRoughness,
        (1.0 - gloss) * (1.0 - gloss));
    float3 normal = CatalogLightingNormal(uv);
    float3 view = float3(0.0, 0.0, 1.0);
    float3 f0 = lerp(
        0.04,
        baseColor,
        metallic);
    float3 diffuseColor =
        baseColor * (1.0 - metallic) / 3.14159265359;
    float3 result =
        baseColor * max(FilterOptions1.x, 0.0);
    float3 surfacePosition = float3(uv, 0.0);
    int lightCount = clamp(
        (int)(FilterLightCount + 0.5),
        0,
        8);

    [loop]
    for (int lightIndex = 0;
        lightIndex < lightCount;
        lightIndex++)
    {
        int baseIndex = lightIndex * 3;
        float4 metadata = FilterLights[baseIndex];
        float3 directionOrPosition =
            FilterLights[baseIndex + 1].xyz;
        float3 lightColor =
            FilterLights[baseIndex + 2].rgb;
        float3 surfaceToLight;
        float attenuation;
        if (metadata.x < 0.5)
        {
            surfaceToLight = directionOrPosition;
            attenuation = metadata.y;
        }
        else
        {
            float3 delta =
                directionOrPosition - surfacePosition;
            float distanceSquared = max(
                dot(delta, delta),
                minimumDenominator);
            surfaceToLight =
                delta / sqrt(distanceSquared);
            attenuation =
                metadata.y / distanceSquared;
        }

        float normalDotLight = max(
            dot(normal, surfaceToLight),
            0.0);
        if (normalDotLight <= 0.0 ||
            attenuation <= 0.0)
        {
            continue;
        }

        float3 radiance =
            lightColor * attenuation;
        result +=
            (diffuseColor +
                CatalogCookTorranceGgxSpecular(
                    f0,
                    normal,
                    view,
                    surfaceToLight,
                    roughness)) *
            radiance *
            normalDotLight;
    }

    float exposure = exp2(FilterOptions4.x);
    return float4(
        saturate(result * exposure) * source.a,
        source.a);
}
