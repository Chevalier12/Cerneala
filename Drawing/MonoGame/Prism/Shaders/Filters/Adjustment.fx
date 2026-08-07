float AdjustmentOkhslToe(float value)
{
    const float k1 = 0.206;
    const float k2 = 0.03;
    const float k3 = (1.0 + k1) / (1.0 + k2);
    float scaled = (k3 * value) - k1;
    return 0.5 * (scaled + sqrt(
        (scaled * scaled) + (4.0 * k2 * k3 * value)));
}

float AdjustmentOkhslToeInverse(float value)
{
    const float k1 = 0.206;
    const float k2 = 0.03;
    const float k3 = (1.0 + k1) / (1.0 + k2);
    return ((value * value) + (k1 * value)) /
        (k3 * (value + k2));
}

float3 AdjustmentLinearSrgbToOklab(float3 color)
{
    float3 lms = float3(
        dot(color, float3(
            0.4122214708, 0.5363325363, 0.0514459929)),
        dot(color, float3(
            0.2119034982, 0.6806995451, 0.1073969566)),
        dot(color, float3(
            0.0883024619, 0.2817188376, 0.6299787005)));
    lms = pow(max(lms, 0.0), 1.0 / 3.0);
    return float3(
        dot(lms, float3(
            0.2104542553, 0.7936177850, -0.0040720468)),
        dot(lms, float3(
            1.9779984951, -2.4285922050, 0.4505937099)),
        dot(lms, float3(
            0.0259040371, 0.7827717662, -0.8086757660)));
}

float3 AdjustmentOklabToLinearSrgb(float3 lab)
{
    float l = lab.x +
        (0.3963377774 * lab.y) +
        (0.2158037573 * lab.z);
    float m = lab.x -
        (0.1055613458 * lab.y) -
        (0.0638541728 * lab.z);
    float s = lab.x -
        (0.0894841775 * lab.y) -
        (1.2914855480 * lab.z);
    l *= l * l;
    m *= m * m;
    s *= s * s;
    return float3(
        (4.0767416361 * l) -
            (3.3077115913 * m) +
            (0.2309699449 * s),
        (-1.2684380046 * l) +
            (2.6097574011 * m) -
            (0.3413193965 * s),
        (-0.0041960863 * l) -
            (0.7034186145 * m) +
            (1.7076147010 * s));
}

float AdjustmentOkhslMaximumChroma(
    float lightness,
    float hue)
{
    const float tau = 6.28318530717958647692;
    float angle = hue * tau;
    float2 direction = float2(cos(angle), sin(angle));
    float minimum = 0.0;
    float maximum = 0.5;

    [unroll]
    for (int iteration = 0; iteration < 10; iteration++)
    {
        float candidate = (minimum + maximum) * 0.5;
        float3 rgb = AdjustmentOklabToLinearSrgb(
            float3(
                lightness,
                candidate * direction.x,
                candidate * direction.y));
        bool inGamut =
            all(rgb >= 0.0) && all(rgb <= 1.0);
        if (inGamut)
        {
            minimum = candidate;
        }
        else
        {
            maximum = candidate;
        }
    }

    return minimum;
}

float3 AdjustmentLinearSrgbToOkhsl(float3 color)
{
    const float tau = 6.28318530717958647692;
    float3 lab = AdjustmentLinearSrgbToOklab(saturate(color));
    float chroma = length(lab.yz);
    float hue = chroma <= 0.000001
        ? 0.0
        : frac((atan2(lab.z, lab.y) / tau) + 1.0);
    float maximumChroma = chroma <= 0.000001
        ? 0.0
        : AdjustmentOkhslMaximumChroma(lab.x, hue);
    return float3(
        hue,
        maximumChroma <= 0.000001
            ? 0.0
            : saturate(chroma / maximumChroma),
        AdjustmentOkhslToe(lab.x));
}

float3 AdjustmentOkhslToLinearSrgb(float3 hsl)
{
    const float tau = 6.28318530717958647692;
    float hue = frac(hsl.x);
    float saturation = saturate(hsl.y);
    float lightness = saturate(hsl.z);
    float labLightness = AdjustmentOkhslToeInverse(lightness);
    if (saturation <= 0.000001 ||
        labLightness <= 0.000001 ||
        labLightness >= 0.999999)
    {
        return saturate(AdjustmentOklabToLinearSrgb(
            float3(labLightness, 0.0, 0.0)));
    }

    float angle = hue * tau;
    float chroma = saturation *
        AdjustmentOkhslMaximumChroma(labLightness, hue);
    return saturate(AdjustmentOklabToLinearSrgb(float3(
        labLightness,
        chroma * cos(angle),
        chroma * sin(angle))));
}

float AdjustmentShortestHueDelta(float from, float to)
{
    float delta = frac(to - from + 0.5) - 0.5;
    return delta == -0.5 ? 0.5 : delta;
}

float3 AdjustmentRgbToHsv(float3 color)
{
    float maximum = max(color.r, max(color.g, color.b));
    float minimum = min(color.r, min(color.g, color.b));
    float delta = maximum - minimum;
    float hue = 0.0;
    if (delta > 0.000001)
    {
        if (maximum == color.r)
        {
            hue = fmod(
                (color.g - color.b) / delta,
                6.0);
        }
        else if (maximum == color.g)
        {
            hue =
                ((color.b - color.r) / delta) + 2.0;
        }
        else
        {
            hue =
                ((color.r - color.g) / delta) + 4.0;
        }
        hue = frac((hue / 6.0) + 1.0);
    }
    float saturation =
        maximum <= 0.0 ? 0.0 : delta / maximum;
    return float3(hue, saturation, maximum);
}

float3 AdjustmentHsvToRgb(float3 hsv)
{
    float hue = frac(hsv.x) * 6.0;
    float chroma = hsv.z * hsv.y;
    float x = chroma *
        (1.0 - abs(fmod(hue, 2.0) - 1.0));
    int sector = (int)floor(hue);
    float3 color;
    if (sector == 0)
    {
        color = float3(chroma, x, 0.0);
    }
    else if (sector == 1)
    {
        color = float3(x, chroma, 0.0);
    }
    else if (sector == 2)
    {
        color = float3(0.0, chroma, x);
    }
    else if (sector == 3)
    {
        color = float3(0.0, x, chroma);
    }
    else if (sector == 4)
    {
        color = float3(x, 0.0, chroma);
    }
    else
    {
        color = float3(chroma, 0.0, x);
    }
    return color + (hsv.z - chroma);
}

float AdjustmentSkinToneMask(float3 color)
{
    float3 hsv = AdjustmentRgbToHsv(color);
    float hueDistance = abs(hsv.x - 0.075);
    hueDistance = min(
        hueDistance,
        1.0 - hueDistance);
    float hueWeight = 1.0 -
        smoothstep(0.035, 0.16, hueDistance);
    float saturationWeight =
        smoothstep(0.1, 0.3, hsv.y) *
        (1.0 - smoothstep(0.92, 1.0, hsv.y));
    float valueWeight =
        smoothstep(0.08, 0.25, hsv.z) *
        (1.0 - smoothstep(0.98, 1.0, hsv.z));
    return hueWeight *
        saturationWeight *
        valueWeight;
}

float3 AdjustmentScaleChroma(
    float3 color,
    float3 grayTransform,
    float scale)
{
    float gray = dot(color, grayTransform);
    return gray + ((color - gray) * scale);
}

float3 AdjustmentClipChromaToUnit(
    float3 color,
    float gray)
{
    gray = saturate(gray);
    float maximum =
        max(color.r, max(color.g, color.b));
    float minimum =
        min(color.r, min(color.g, color.b));
    float scale = 1.0;
    if (minimum < 0.0 && gray > minimum)
    {
        scale = min(
            scale,
            gray / (gray - minimum));
    }
    if (maximum > 1.0 && maximum > gray)
    {
        scale = min(
            scale,
            (1.0 - gray) / (maximum - gray));
    }
    return saturate(
        gray + ((color - gray) * scale));
}

float3 AdjustmentVibrance(float3 color)
{
    float vibrance = FilterOptions0.x;
    float saturation = FilterOptions0.y;
    if (vibrance == 0.0 && saturation == 0.0)
    {
        return color;
    }

    float3 perceptual = EncodeSrgb(max(color, 0.0));
    float3 grayTransform = FilterOptions1.rgb;
    if (vibrance > 0.0)
    {
        float maximum = max(
            perceptual.r,
            max(perceptual.g, perceptual.b));
        float minimum = min(
            perceptual.r,
            min(perceptual.g, perceptual.b));
        float chroma = maximum > 0.0
            ? (maximum - minimum) / maximum
            : 0.0;
        float chromaSquared = chroma * chroma;
        float vibranceSquared = vibrance * vibrance;
        float vibranceCubed =
            vibranceSquared * vibrance;
        float response =
            (3.0 * vibrance) +
            ((-4.5 * vibranceSquared -
                1.5 * vibrance) * chroma) +
            ((4.5 * vibranceCubed -
                0.5 * vibrance) * chromaSquared) +
            ((-4.5 * vibranceCubed +
                4.5 * vibranceSquared -
                vibrance) *
                chromaSquared * chroma);
        if (FilterOptions0.z > 0.5)
        {
            response *= 1.0 -
                (0.75 * AdjustmentSkinToneMask(
                    perceptual));
        }
        perceptual = AdjustmentScaleChroma(
            perceptual,
            grayTransform,
            1.0 + max(0.0, response));
    }
    else
    {
        perceptual = AdjustmentScaleChroma(
            perceptual,
            grayTransform,
            1.0 + vibrance);
    }

    perceptual = AdjustmentScaleChroma(
        perceptual,
        grayTransform,
        1.0 + saturation);
    float gray = dot(perceptual, grayTransform);
    return DecodeSrgb(
        AdjustmentClipChromaToUnit(
            perceptual,
            gray));
}

float AdjustmentCurveLut(float value, int channel)
{
    float width = max(FilterTextureSize.x, 1.0);
    float coordinate =
        ((saturate(value) * (width - 1.0)) + 0.5) /
        width;
    float4 mapped = tex2D(
        SecondaryTextureSampler,
        float2(coordinate, 0.5));
    if (channel == 0)
    {
        return mapped.r;
    }
    if (channel == 1)
    {
        return mapped.g;
    }
    return mapped.b;
}

float3 AdjustmentCurves(float3 color)
{
    return float3(
        AdjustmentCurveLut(color.r, 0),
        AdjustmentCurveLut(color.g, 1),
        AdjustmentCurveLut(color.b, 2));
}

float AdjustmentLevel(
    float value,
    float inputBlack,
    float inputWhite,
    float gamma,
    float outputBlack,
    float outputWhite)
{
    float normalized = saturate(
        (value - inputBlack) /
        max(inputWhite - inputBlack, 0.000001));
    return outputBlack +
        (pow(
            normalized,
            1.0 / max(gamma, 0.000001)) *
        (outputWhite - outputBlack));
}

float3 AdjustmentLevels(float3 color)
{
    int channel = (int)(FilterOptions0.x + 0.5);
    float inputBlack = FilterOptions0.y;
    float inputWhite = FilterOptions0.z;
    if (FilterOptions1.z > 0.5)
    {
        float4 automaticRange = tex2D(
            SecondaryTextureSampler,
            float2(0.5, 0.5));
        inputBlack = automaticRange.r;
        inputWhite = automaticRange.g;
    }
    if (channel == 0 || channel == 1)
    {
        color.r = AdjustmentLevel(
            color.r,
            inputBlack,
            inputWhite,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    if (channel == 0 || channel == 2)
    {
        color.g = AdjustmentLevel(
            color.g,
            inputBlack,
            inputWhite,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    if (channel == 0 || channel == 3)
    {
        color.b = AdjustmentLevel(
            color.b,
            inputBlack,
            inputWhite,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    return color;
}

float LevelsAnalysisValue(float3 color, int channel)
{
    if (channel == 1)
    {
        return color.r;
    }
    if (channel == 2)
    {
        return color.g;
    }
    if (channel == 3)
    {
        return color.b;
    }
    return AdjustmentLuminance(color);
}

float4 LevelsCdfPixelShader(
    VertexShaderOutput input) : COLOR0
{
    const int sampleSide = 32;
    const int sampleCount = sampleSide * sampleSide;
    int channel = (int)(FilterHeader.x + 0.5);
    int profile = (int)(FilterHeader.y + 0.5);
    float threshold =
        (input.Position.x - 0.5) / 255.0;
    float accepted = 0.0;
    float valid = 0.0;

    [loop]
    for (int index = 0; index < sampleCount; index++)
    {
        float2 uv = float2(
            (fmod(index, sampleSide) + 0.5) / sampleSide,
            (floor(index / sampleSide) + 0.5) / sampleSide);
        float4 source = tex2D(SpriteTextureSampler, uv);
        if (source.a > 0.0)
        {
            float4 linearSample = WorkingAssociatedToLinearSrgb(
                source,
                profile);
            float value = LevelsAnalysisValue(
                Unpremultiply(linearSample),
                channel);
            accepted += value <= threshold ? 1.0 : 0.0;
            valid += 1.0;
        }
    }

    float cdf = valid > 0.0 ? accepted / valid : 0.0;
    return float4(cdf, 0.0, 0.0, 1.0);
}

float4 LevelsRangePixelShader(
    VertexShaderOutput input) : COLOR0
{
    const float clippedFraction = 0.001;
    float inputBlack = 0.0;
    float inputWhite = 1.0;
    bool foundBlack = false;
    bool foundWhite = false;

    [loop]
    for (int bin = 0; bin < 256; bin++)
    {
        float cdf = tex2D(
            SpriteTextureSampler,
            float2((bin + 0.5) / 256.0, 0.5)).r;
        if (!foundBlack && cdf > clippedFraction)
        {
            inputBlack = bin / 255.0;
            foundBlack = true;
        }
        if (!foundWhite &&
            cdf >= 1.0 - clippedFraction)
        {
            inputWhite = bin / 255.0;
            foundWhite = true;
        }
    }

    if (!foundBlack ||
        !foundWhite ||
        inputBlack >= inputWhite)
    {
        inputBlack = 0.0;
        inputWhite = 1.0;
    }
    return float4(inputBlack, inputWhite, 0.0, 1.0);
}

float4 ThresholdRangePixelShader(
    VertexShaderOutput input) : COLOR0
{
    float weightedTotal = 0.0;
    float previousCdf = 0.0;
    int nonemptyBinCount = 0;
    int onlyNonemptyBin = 0;
    [loop]
    for (int bin = 0; bin < 256; bin++)
    {
        float cdf = tex2D(
            SpriteTextureSampler,
            float2((bin + 0.5) / 256.0, 0.5)).r;
        float probability = max(0.0, cdf - previousCdf);
        if (probability > 0.0)
        {
            nonemptyBinCount++;
            onlyNonemptyBin = bin;
        }
        weightedTotal += bin * probability;
        previousCdf = cdf;
    }

    float backgroundWeight = 0.0;
    float backgroundWeighted = 0.0;
    float previous = 0.0;
    float bestVariance = 0.0;
    float bestThreshold = FilterHeader.x;
    [loop]
    for (int threshold = 0; threshold < 255; threshold++)
    {
        float cdf = tex2D(
            SpriteTextureSampler,
            float2((threshold + 0.5) / 256.0, 0.5)).r;
        float probability = max(0.0, cdf - previous);
        backgroundWeight += probability;
        backgroundWeighted += threshold * probability;
        previous = cdf;
        float foregroundWeight = 1.0 - backgroundWeight;
        if (backgroundWeight > 0.0 && foregroundWeight > 0.0)
        {
            float backgroundMean =
                backgroundWeighted / backgroundWeight;
            float foregroundMean =
                (weightedTotal - backgroundWeighted) /
                foregroundWeight;
            float difference = backgroundMean - foregroundMean;
            float variance = backgroundWeight * foregroundWeight *
                difference * difference;
            if (variance > bestVariance)
            {
                bestVariance = variance;
                bestThreshold = threshold / 255.0;
            }
        }
    }
    if (nonemptyBinCount == 1)
    {
        bestThreshold = onlyNonemptyBin / 255.0;
    }
    return float4(bestThreshold, 0.0, 0.0, 1.0);
}

float3 PreserveAdjustmentLuminance(
    float3 color,
    float luminance)
{
    return color +
        (luminance - AdjustmentLuminance(color));
}

float3 PreserveAdjustmentLightness(
    float3 source,
    float3 adjusted)
{
    float3 sourceHsl = AdjustmentLinearSrgbToOkhsl(source);
    float3 adjustedHsl = AdjustmentLinearSrgbToOkhsl(adjusted);
    adjustedHsl.z = sourceHsl.z;
    return AdjustmentOkhslToLinearSrgb(adjustedHsl);
}

float AdjustmentHueWeight(float hue, int channel)
{
    if (channel == 0)
    {
        return 1.0;
    }
    float center = (channel - 1.0) / 6.0;
    float distance = abs(hue - center);
    distance = min(distance, 1.0 - distance);
    return saturate(1.0 - (distance * 6.0));
}

float3 SampleAdjustmentLutPoint(float3 coordinate)
{
    float size = max(FilterHeader.w, 2.0);
    coordinate = clamp(
        coordinate,
        0.0,
        size - 1.0);
    float linearIndex = coordinate.x +
        (size * (coordinate.y + (size * coordinate.z)));
    float2 uv = float2(
        (fmod(linearIndex, FilterTextureSize.x) + 0.5) /
            FilterTextureSize.x,
        (floor(linearIndex / FilterTextureSize.x) + 0.5) /
            FilterTextureSize.y);
    float4 sample = tex2D(
        SecondaryTextureSampler,
        uv);
    return sample.a > 0.0
        ? sample.rgb / sample.a
        : sample.rgb;
}

float3 SampleAdjustmentLutTrilinear(
    float3 baseCoordinate,
    float3 fraction)
{
    float3 c000 =
        SampleAdjustmentLutPoint(baseCoordinate);
    float3 c100 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(1.0, 0.0, 0.0));
    float3 c010 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(0.0, 1.0, 0.0));
    float3 c110 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(1.0, 1.0, 0.0));
    float3 c001 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(0.0, 0.0, 1.0));
    float3 c101 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(1.0, 0.0, 1.0));
    float3 c011 = SampleAdjustmentLutPoint(
        baseCoordinate + float3(0.0, 1.0, 1.0));
    float3 c111 =
        SampleAdjustmentLutPoint(baseCoordinate + 1.0);
    float3 low = lerp(
        lerp(c000, c100, fraction.x),
        lerp(c010, c110, fraction.x),
        fraction.y);
    float3 high = lerp(
        lerp(c001, c101, fraction.x),
        lerp(c011, c111, fraction.x),
        fraction.y);
    return lerp(low, high, fraction.z);
}

float3 SampleAdjustmentLut(float3 color)
{
    float size = max(FilterHeader.w, 2.0);
    float3 coordinate =
        saturate(color) * (size - 1.0);
    float3 baseCoordinate = floor(coordinate);
    float3 fraction = coordinate - baseCoordinate;
    float3 result;
    if (true)
    {
        result = SampleAdjustmentLutTrilinear(
            baseCoordinate,
            fraction);
    }
    else
    {
    float3 c000 =
        SampleAdjustmentLutPoint(baseCoordinate);
    float3 c111 =
        SampleAdjustmentLutPoint(
            baseCoordinate + 1.0);
    result = c000;
    if (fraction.x >= fraction.y)
    {
        if (fraction.y >= fraction.z)
        {
            float3 c100 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(1.0, 0.0, 0.0));
            float3 c110 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(1.0, 1.0, 0.0));
            result = c000 +
                (fraction.x * (c100 - c000)) +
                (fraction.y * (c110 - c100)) +
                (fraction.z * (c111 - c110));
        }
        else if (fraction.x >= fraction.z)
        {
            float3 c100 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(1.0, 0.0, 0.0));
            float3 c101 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(1.0, 0.0, 1.0));
            result = c000 +
                (fraction.x * (c100 - c000)) +
                (fraction.z * (c101 - c100)) +
                (fraction.y * (c111 - c101));
        }
        else
        {
            float3 c001 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(0.0, 0.0, 1.0));
            float3 c101 = SampleAdjustmentLutPoint(
                baseCoordinate + float3(1.0, 0.0, 1.0));
            result = c000 +
                (fraction.z * (c001 - c000)) +
                (fraction.x * (c101 - c001)) +
                (fraction.y * (c111 - c101));
        }
    }
    else if (fraction.x >= fraction.z)
    {
        float3 c010 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(0.0, 1.0, 0.0));
        float3 c110 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(1.0, 1.0, 0.0));
        result = c000 +
            (fraction.y * (c010 - c000)) +
            (fraction.x * (c110 - c010)) +
            (fraction.z * (c111 - c110));
    }
    else if (fraction.y >= fraction.z)
    {
        float3 c010 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(0.0, 1.0, 0.0));
        float3 c011 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(0.0, 1.0, 1.0));
        result = c000 +
            (fraction.y * (c010 - c000)) +
            (fraction.z * (c011 - c010)) +
            (fraction.x * (c111 - c011));
    }
    else
    {
        float3 c001 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(0.0, 0.0, 1.0));
        float3 c011 = SampleAdjustmentLutPoint(
            baseCoordinate + float3(0.0, 1.0, 1.0));
        result = c000 +
            (fraction.z * (c001 - c000)) +
            (fraction.y * (c011 - c001)) +
            (fraction.x * (c111 - c011));
    }
    }
    return result;
}
