


float4 CatalogOilPaint(
    float2 uv,
    float4 source,
    int profile)
{
    if (source.a <= 0.000001)
    {
        return 0.0;
    }

    float stylization = saturate(FilterOptions6.x);
    float cleanliness = saturate(FilterOptions2.x);
    float bristleDetail = saturate(FilterOptions1.x);
    bool lighting = FilterOptions3.x >= 0.5;
    float angle = FilterOptions0.x *
        (3.14159265358979323846 / 180.0);
    float shine = saturate(FilterOptions5.x);
    float radius = clamp(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0,
        12.0);
    float sharpness =
        1.5 +
        (8.0 * stylization) +
        (2.0 * cleanliness);
    float roughness =
        (1.0 - cleanliness) *
        bristleDetail *
        0.65;
    float4 painted = CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        radius,
        sharpness,
        1.1 + (0.5 * stylization),
        1.0 - (0.35 * stylization),
        roughness,
        0.0,
        0.0,
        0.0,
        0.0,
        0x6f696c50u);
    float3 result = lerp(
        saturate(Unpremultiply(source)),
        saturate(Unpremultiply(painted)),
        0.35 + (0.65 * stylization));

    float cosine = cos(angle);
    float sine = sin(angle);
    float2 pixel = uv / PixelSize;
    float along =
        (pixel.x * cosine) +
        (pixel.y * sine);
    float across =
        (-pixel.x * sine) +
        (pixel.y * cosine);
    float ridge = 0.5 +
        (0.5 * cos(
            along * (0.75 + (1.5 * bristleDetail)) +
            (sin(across * 0.35) * 1.2)));
    float grain = DryBrushHash(
        (int2)floor(pixel / max(radius * 0.75, 1.0)),
        0x62726973u);
    float bristle =
        (((0.65 * ridge) + (0.35 * grain)) - 0.5) *
        bristleDetail *
        (1.0 - (0.65 * cleanliness)) *
        0.12;
    result *= 1.0 + bristle;

    if (lighting)
    {
        float sampleOffset = max(1.0, radius * 0.35);
        float left = CatalogLuminance(CatalogLinearSample(
            uv - float2(PixelSize.x * sampleOffset, 0.0),
            profile));
        float right = CatalogLuminance(CatalogLinearSample(
            uv + float2(PixelSize.x * sampleOffset, 0.0),
            profile));
        float up = CatalogLuminance(CatalogLinearSample(
            uv - float2(0.0, PixelSize.y * sampleOffset),
            profile));
        float down = CatalogLuminance(CatalogLinearSample(
            uv + float2(0.0, PixelSize.y * sampleOffset),
            profile));
        float heightScale = 0.8 + (1.6 * stylization);
        float3 normal = normalize(float3(
            (left - right) * heightScale,
            (up - down) * heightScale,
            1.0));
        float3 light = normalize(float3(
            -cosine * 0.55,
            -sine * 0.55,
            0.85));
        float diffuse = max(dot(normal, light), 0.0);
        float3 halfVector = normalize(light + float3(0.0, 0.0, 1.0));
        float specular = pow(
            max(dot(normal, halfVector), 0.0),
            8.0 + (24.0 * (1.0 - shine))) *
            shine *
            0.16;
        result =
            (result * (0.86 + (0.22 * diffuse))) +
            specular;
    }

    return float4(saturate(result) * source.a, source.a);
}
