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

float AdjustmentCurve(float value, int curve)
{
    if (curve == 1)
    {
        return sqrt(max(value, 0.0));
    }
    if (curve == 2)
    {
        return value * value;
    }
    if (curve == 3)
    {
        return value * value * (3.0 - (2.0 * value));
    }
    return value;
}

float3 AdjustmentChannelMap(
    float3 color,
    int channel,
    int curve)
{
    if (channel == 0 || channel == 1)
    {
        color.r = AdjustmentCurve(color.r, curve);
    }
    if (channel == 0 || channel == 2)
    {
        color.g = AdjustmentCurve(color.g, curve);
    }
    if (channel == 0 || channel == 3)
    {
        color.b = AdjustmentCurve(color.b, curve);
    }
    return color;
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
    if (channel == 0 || channel == 1)
    {
        color.r = AdjustmentLevel(
            color.r,
            FilterOptions0.y,
            FilterOptions0.z,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    if (channel == 0 || channel == 2)
    {
        color.g = AdjustmentLevel(
            color.g,
            FilterOptions0.y,
            FilterOptions0.z,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    if (channel == 0 || channel == 3)
    {
        color.b = AdjustmentLevel(
            color.b,
            FilterOptions0.y,
            FilterOptions0.z,
            FilterOptions0.w,
            FilterOptions1.x,
            FilterOptions1.y);
    }
    return color;
}

float3 PreserveAdjustmentLuminance(
    float3 color,
    float luminance)
{
    return color +
        (luminance - AdjustmentLuminance(color));
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

float AdjustmentBlackWhiteWeight(int index)
{
    if (index == 0)
    {
        return FilterOptions0.x;
    }
    if (index == 1)
    {
        return FilterOptions0.y;
    }
    if (index == 2)
    {
        return FilterOptions0.z;
    }
    if (index == 3)
    {
        return FilterOptions0.w;
    }
    if (index == 4)
    {
        return FilterOptions1.x;
    }
    return FilterOptions1.y;
}

float4 AdjustmentSelectiveParameter(int index)
{
    if (index == 0)
    {
        return FilterOptions0;
    }
    if (index == 1)
    {
        return FilterOptions1;
    }
    if (index == 2)
    {
        return FilterOptions2;
    }
    if (index == 3)
    {
        return FilterOptions3;
    }
    if (index == 4)
    {
        return FilterOptions4;
    }
    return FilterOptions5;
}

float3 SampleAdjustmentLutPoint(float3 coordinate)
{
    float size = max(FilterTextureSize.y, 2.0);
    coordinate = clamp(
        coordinate,
        0.0,
        size - 1.0);
    float2 uv = float2(
        ((coordinate.z * size) + coordinate.x + 0.5) /
            FilterTextureSize.x,
        (coordinate.y + 0.5) / FilterTextureSize.y);
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
    float size = max(FilterTextureSize.y, 2.0);
    float3 coordinate =
        saturate(color) * (size - 1.0);
    float3 baseCoordinate = floor(coordinate);
    float3 fraction = coordinate - baseCoordinate;
    float3 result;
    if (FilterOptions0.y > 0.5)
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

float3 ApplyAdjustment(
    float3 color,
    VertexShaderOutput input)
{
    int operation = (int)(FilterHeader.x + 0.5);
    if (operation == 0)
    {
        float factor = FilterOptions0.z > 0.5
            ? max(0.0, 1.0 + FilterOptions0.y)
            : pow(2.0, FilterOptions0.y * 2.0);
        return ((color - 0.5) * factor) +
            0.5 + FilterOptions0.x;
    }
    if (operation == 1)
    {
        return AdjustmentLevels(color);
    }
    if (operation == 2)
    {
        return AdjustmentChannelMap(
            color,
            (int)(FilterOptions0.x + 0.5),
            (int)(FilterOptions0.y + 0.5));
    }
    if (operation == 3)
    {
        float3 exposed =
            (color * pow(2.0, FilterOptions0.x)) +
            FilterOptions0.y;
        return pow(
            max(exposed, 0.0),
            1.0 / max(FilterOptions0.z, 0.000001));
    }
    if (operation == 4)
    {
        float maximum =
            max(color.r, max(color.g, color.b));
        float minimum =
            min(color.r, min(color.g, color.b));
        float amount = 1.0 +
            (FilterOptions0.x *
                (1.0 - (maximum - minimum))) +
            FilterOptions0.y;
        float luminance = AdjustmentLuminance(color);
        return luminance +
            ((color - luminance) * amount);
    }
    if (operation == 5)
    {
        float3 hsv = AdjustmentRgbToHsv(color);
        float weight = AdjustmentHueWeight(
            hsv.x,
            (int)(FilterOptions0.x + 0.5));
        if (FilterOptions1.x > 0.5)
        {
            hsv.x = frac(
                (FilterOptions0.y / 360.0) + 1.0);
            hsv.y = saturate(
                0.5 + (FilterOptions0.z * 0.5));
            hsv.z = saturate(
                AdjustmentLuminance(color) +
                FilterOptions0.w);
        }
        else
        {
            hsv.x = frac(
                hsv.x +
                ((FilterOptions0.y / 360.0) * weight) +
                1.0);
            hsv.y = saturate(
                hsv.y *
                (1.0 + (FilterOptions0.z * weight)));
            hsv.z = saturate(
                hsv.z + (FilterOptions0.w * weight));
        }
        return AdjustmentHsvToRgb(hsv);
    }
    if (operation == 6)
    {
        float luminance = AdjustmentLuminance(color);
        float shadows =
            (1.0 - luminance) * (1.0 - luminance);
        float highlights = luminance * luminance;
        float midtones = max(
            0.0,
            1.0 - shadows - highlights);
        float3 adjusted = color +
            (FilterOptions0.rgb * shadows) +
            (FilterOptions1.rgb * midtones) +
            (FilterOptions2.rgb * highlights);
        return FilterOptions3.x > 0.5
            ? PreserveAdjustmentLuminance(
                adjusted,
                luminance)
            : adjusted;
    }
    if (operation == 7)
    {
        float3 hsv = AdjustmentRgbToHsv(color);
        float sector = hsv.x * 6.0;
        int first = min((int)floor(sector), 5);
        int second = first == 5 ? 0 : first + 1;
        float amount = lerp(
            AdjustmentBlackWhiteWeight(first),
            AdjustmentBlackWhiteWeight(second),
            frac(sector));
        float gray =
            AdjustmentLuminance(color) * amount;
        return FilterOptions1.z > 0.5
            ? FilterOptions2.rgb * gray
            : gray;
    }
    if (operation == 8)
    {
        float luminance = AdjustmentLuminance(color);
        float3 filtered = lerp(
            color,
            color * FilterOptions0.rgb,
            FilterOptions1.x);
        return FilterOptions1.y > 0.5
            ? PreserveAdjustmentLuminance(
                filtered,
                luminance)
            : filtered;
    }
    if (operation == 9)
    {
        float3 mixed = float3(
            dot(color, FilterOptions0.rgb) +
                FilterOptions3.x,
            dot(color, FilterOptions1.rgb) +
                FilterOptions3.y,
            dot(color, FilterOptions2.rgb) +
                FilterOptions3.z);
        return FilterOptions4.x > 0.5
            ? mixed.xxx
            : mixed;
    }
    if (operation == 10)
    {
        float3 mapped = SampleAdjustmentLut(color);
        return lerp(
            color,
            mapped,
            FilterOptions0.x);
    }
    if (operation == 11)
    {
        return 1.0 - color;
    }
    if (operation == 12)
    {
        float levels =
            max(1.0, round(FilterOptions0.x) - 1.0);
        return round(color * levels) / levels;
    }
    if (operation == 13)
    {
        float value =
            AdjustmentLuminance(color) >= FilterOptions0.x
                ? 1.0
                : 0.0;
        return value.xxx;
    }
    if (operation == 14)
    {
        float coordinate = AdjustmentLuminance(color);
        if (FilterOptions0.z > 0.5)
        {
            float ordered = fmod(
                floor(input.Position.x) * 5.0 +
                floor(input.Position.y) * 3.0,
                16.0);
            coordinate = saturate(
                coordinate +
                ((ordered - 7.5) / (16.0 * 255.0)));
        }
        coordinate = FilterOptions0.y > 0.5
            ? 1.0 - coordinate
            : coordinate;
        if (FilterOptions0.x > 0.5)
        {
            return coordinate < 0.5
                ? lerp(
                    float3(0.04, 0.08, 0.8),
                    float3(0.85, 0.05, 0.03),
                    coordinate * 2.0)
                : lerp(
                    float3(0.85, 0.05, 0.03),
                    float3(1.0, 0.9, 0.05),
                    (coordinate - 0.5) * 2.0);
        }
        return coordinate.xxx;
    }

    float3 hsv = AdjustmentRgbToHsv(color);
    float sector = hsv.x * 6.0;
    int first = min((int)floor(sector), 5);
    int second = first == 5 ? 0 : first + 1;
    float4 hue = lerp(
        AdjustmentSelectiveParameter(first),
        AdjustmentSelectiveParameter(second),
        frac(sector));
    float maximum = max(color.r, max(color.g, color.b));
    float minimum = min(color.r, min(color.g, color.b));
    float chroma = maximum - minimum;
    float whiteWeight = saturate((minimum - 0.5) * 2.0);
    float blackWeight = saturate((0.5 - maximum) * 2.0);
    float neutralWeight = saturate(
        1.0 - chroma - whiteWeight - blackWeight);
    float4 adjustment =
        (hue * chroma) +
        (FilterOptions6 * whiteWeight) +
        (FilterOptions7 * neutralWeight) +
        (FilterOptions8 * blackWeight);
    float scale = FilterOptions9.x < 0.5
        ? max(0.05, 1.0 - minimum)
        : 1.0;
    float3 cmy = 1.0 - color;
    cmy += adjustment.rgb * scale;
    cmy += adjustment.a * scale;
    return 1.0 - cmy;
}

float4 AdjustmentFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode = (int)(FilterHeader.z + 0.5);
    float4 linearSource =
        WorkingAssociatedToLinearSrgb(
            source,
            profile);
    float3 linearColor =
        Unpremultiply(linearSource);
    float3 adjusted = saturate(
        ApplyAdjustment(linearColor, input));
    float3 blended = EvaluateBlendMode(
        blendMode,
        saturate(linearColor),
        adjusted);
    float3 result = lerp(
        linearColor,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(
        float4(
            result * source.a,
            source.a),
        profile) * input.Color;
}

