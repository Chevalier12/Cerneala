static const float CatalogColorEpsilon = 0.000001;

float CatalogColorSignedCbrt(float value)
{
    return sign(value) * pow(abs(value), 1.0 / 3.0);
}

float3 CatalogColorToOklab(float3 rgb)
{
    float3 lms = float3(
        dot(rgb, float3(0.4122214708, 0.5363325363, 0.0514459929)),
        dot(rgb, float3(0.2119034982, 0.6806995451, 0.1073969566)),
        dot(rgb, float3(0.0883024619, 0.2817188376, 0.6299787005)));
    float3 roots = float3(
        CatalogColorSignedCbrt(lms.x),
        CatalogColorSignedCbrt(lms.y),
        CatalogColorSignedCbrt(lms.z));
    return float3(
        dot(roots, float3(0.2104542553, 0.7936177850, -0.0040720468)),
        dot(roots, float3(1.9779984951, -2.4285922050, 0.4505937099)),
        dot(roots, float3(0.0259040371, 0.7827717662, -0.8086757660)));
}

float3 CatalogColorFromOklab(float3 oklab)
{
    float3 roots = float3(
        dot(oklab, float3(1.0, 0.3963377774, 0.2158037573)),
        dot(oklab, float3(1.0, -0.1055613458, -0.0638541728)),
        dot(oklab, float3(1.0, -0.0894841775, -1.2914855480)));
    float3 lms = roots * roots * roots;
    return float3(
        dot(lms, float3(4.0767416621, -3.3077115913, 0.2309699292)),
        dot(lms, float3(-1.2684380046, 2.6097574011, -0.3413193965)),
        dot(lms, float3(-0.0041960863, -0.7034186147, 1.7076147010)));
}

float3 CatalogColorLinearSrgbToXyz(float3 rgb)
{
    return float3(
        dot(rgb, float3(0.4124564, 0.3575761, 0.1804375)),
        dot(rgb, float3(0.2126729, 0.7151522, 0.0721750)),
        dot(rgb, float3(0.0193339, 0.1191920, 0.9503041)));
}

float3 CatalogColorXyzToLinearSrgb(float3 xyz)
{
    return float3(
        dot(xyz, float3(3.2404542, -1.5371385, -0.4985314)),
        dot(xyz, float3(-0.9692660, 1.8760108, 0.0415560)),
        dot(xyz, float3(0.0556434, -0.2040259, 1.0572252)));
}

float3 CatalogColorXyzToCat16(float3 xyz)
{
    return float3(
        dot(xyz, float3(0.401288, 0.650173, -0.051461)),
        dot(xyz, float3(-0.250268, 1.204414, 0.045854)),
        dot(xyz, float3(-0.002079, 0.048952, 0.953127)));
}

float3 CatalogColorCat16ToXyz(float3 lms)
{
    return float3(
        dot(lms, float3(1.86206786, -1.01125463, 0.14918677)),
        dot(lms, float3(0.38752654, 0.62144744, -0.00897398)),
        dot(lms, float3(-0.01584150, -0.03412294, 1.04996444)));
}

float3 CatalogColorXyToXyz(float2 xy)
{
    float denominator = max(xy.y, CatalogColorEpsilon);
    return float3(
        xy.x / denominator,
        1.0,
        max(0.0, 1.0 - xy.x - xy.y) / denominator);
}

float2 CatalogColorTemperatureWhitePoint(float temperature)
{
    float kelvin = clamp(
        6504.0 * exp2(-clamp(temperature, -2.0, 2.0)),
        1667.0,
        25000.0);
    float kelvin2 = kelvin * kelvin;
    float kelvin3 = kelvin2 * kelvin;
    float x = kelvin <= 4000.0
        ? (-0.2661239e9 / kelvin3) -
            (0.2343580e6 / kelvin2) +
            (0.8776956e3 / kelvin) +
            0.179910
        : (-3.0258469e9 / kelvin3) +
            (2.1070379e6 / kelvin2) +
            (0.2226347e3 / kelvin) +
            0.240390;
    float y = kelvin <= 2222.0
        ? (-1.1063814 * x * x * x) -
            (1.34811020 * x * x) +
            (2.18555832 * x) -
            0.20219683
        : kelvin <= 4000.0
            ? (-0.9549476 * x * x * x) -
                (1.37418593 * x * x) +
                (2.09137015 * x) -
                0.16748867
            : (3.0817580 * x * x * x) -
                (5.87338670 * x * x) +
                (3.75112997 * x) -
                0.37001483;
    return float2(x, y);
}

float3 CatalogColorApplyCat16(
    float3 rgb,
    float temperature,
    float4 tint)
{
    float2 destinationXy = CatalogColorTemperatureWhitePoint(temperature);
    float3 tintXyz = CatalogColorLinearSrgbToXyz(max(tint.rgb, 0.0));
    float tintSum = tintXyz.x + tintXyz.y + tintXyz.z;
    if (tint.a > 0.0 && tintSum > CatalogColorEpsilon)
    {
        float2 tintXy = clamp(
            tintXyz.xy / tintSum,
            float2(0.01, 0.01),
            float2(0.85, 0.85));
        float excess = max(0.0, tintXy.x + tintXy.y - 0.98);
        tintXy -= excess * 0.5;
        destinationXy = lerp(destinationXy, tintXy, saturate(tint.a));
    }

    float3 sourceWhite = CatalogColorXyToXyz(float2(0.3127, 0.3290));
    float3 destinationWhite = CatalogColorXyToXyz(destinationXy);
    float3 sourceLms = CatalogColorXyzToCat16(sourceWhite);
    float3 destinationLms = CatalogColorXyzToCat16(destinationWhite);
    float3 lms = CatalogColorXyzToCat16(
        CatalogColorLinearSrgbToXyz(rgb));
    lms *= destinationLms / sourceLms;
    return CatalogColorXyzToLinearSrgb(
        CatalogColorCat16ToXyz(lms));
}

bool CatalogColorIsInGamut(float3 rgb)
{
    return all(rgb >= 0.0) && all(rgb <= 1.0);
}

float3 CatalogColorCompressToGamut(float3 rgb)
{
    if (CatalogColorIsInGamut(rgb))
    {
        return rgb;
    }

    float3 oklab = CatalogColorToOklab(rgb);
    oklab.x = saturate(oklab.x);
    float chroma = length(oklab.yz);
    if (chroma <= CatalogColorEpsilon)
    {
        return saturate(CatalogColorFromOklab(float3(oklab.x, 0.0, 0.0)));
    }

    float2 direction = oklab.yz / chroma;
    float low = 0.0;
    float high = chroma;
    [unroll]
    for (int iteration = 0; iteration < 8; iteration++)
    {
        float candidate = (low + high) * 0.5;
        float3 candidateRgb = CatalogColorFromOklab(
            float3(oklab.x, direction * candidate));
        if (CatalogColorIsInGamut(candidateRgb))
        {
            low = candidate;
        }
        else
        {
            high = candidate;
        }
    }

    return saturate(CatalogColorFromOklab(
        float3(oklab.x, direction * low)));
}

float4 CatalogApplyColor(float4 source)
{
    float3 straight = Unpremultiply(source);
    bool neutral =
        FilterOptions0.x == 0.0 &&
        FilterOptions2.x == 1.0 &&
        FilterOptions3.x == 0.0 &&
        FilterOptions6.x == 1.0 &&
        FilterOptions4.x == 0.0 &&
        FilterOptions7.x == 0.0 &&
        FilterOptions8.a <= 0.0;
    bool clampOutput = FilterOptions1.x >= 0.5;
    if (neutral)
    {
        straight = clampOutput ? saturate(straight) : straight;
        return float4(straight * source.a, source.a);
    }

    straight *= exp2(clamp(FilterOptions3.x, -16.0, 16.0));
    straight += FilterOptions0.x;
    straight = ((straight - 0.18) *
        clamp(FilterOptions2.x, -16.0, 16.0)) + 0.18;

    if (FilterOptions7.x != 0.0 || FilterOptions8.a > 0.0)
    {
        straight = CatalogColorApplyCat16(
            straight,
            FilterOptions7.x,
            FilterOptions8);
    }

    if (FilterOptions6.x != 1.0 || FilterOptions4.x != 0.0)
    {
        float3 oklab = CatalogColorToOklab(straight);
        float angle = FilterOptions4.x * 0.01745329252;
        float sine = sin(angle);
        float cosine = cos(angle);
        float2 chroma = oklab.yz * clamp(FilterOptions6.x, -8.0, 8.0);
        oklab.yz = float2(
            (chroma.x * cosine) - (chroma.y * sine),
            (chroma.x * sine) + (chroma.y * cosine));
        straight = CatalogColorFromOklab(oklab);
    }

    straight = clampOutput
        ? CatalogColorCompressToGamut(straight)
        : clamp(straight, -65504.0, 65504.0);
    return float4(straight * source.a, source.a);
}
