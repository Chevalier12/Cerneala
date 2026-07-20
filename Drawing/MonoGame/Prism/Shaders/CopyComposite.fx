Texture2D SpriteTexture;
Texture2D SecondaryTexture;
Texture2D StyleTexture;
float Opacity;
float2 PixelSize;
float2 UvScale;
float2 UvOffset;
float4 BlendChannels;
float KnockoutMode;
float BlendIfChannel;
float4 ThisLayerRange;
float4 UnderlyingRange;
float DissolveSeed;
float BackgroundAvailable;
float MaskChannel;
float MaskDensity;
float MaskInvert;
float3 MaskUvRowX;
float3 MaskUvRowY;
float2 MaskFeatherStep;
float4 StyleColor;
float4 StyleSecondaryColor;
float4 StyleGeometry0;
float4 StyleGeometry1;
float4 StyleOptions0;
float4 StyleOptions1;
float4 StyleModes0;
float4 StyleModes1;
float4 StyleModes2;
float4 StyleModes3;
float StyleResourceAvailable;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

sampler2D SecondaryTextureSampler = sampler_state
{
    Texture = <SecondaryTexture>;
};

sampler2D StyleTextureSampler = sampler_state
{
    Texture = <StyleTexture>;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float2 ResolveUv(VertexShaderOutput input)
{
    float2 uv = (input.TextureCoordinates * UvScale) + UvOffset;
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 SampleSource(VertexShaderOutput input)
{
    return tex2D(SpriteTextureSampler, ResolveUv(input));
}

float4 SampleSecondary(VertexShaderOutput input)
{
    return tex2D(SecondaryTextureSampler, ResolveUv(input));
}

float4 CopyCompositePixelShader(VertexShaderOutput input) : COLOR0
{
    return SampleSource(input) * input.Color * Opacity;
}

float2 ResolveMaskUv(VertexShaderOutput input)
{
    float3 position = float3(input.Position.xy, 1.0);
    return float2(
        dot(position, MaskUvRowX),
        dot(position, MaskUvRowY));
}

float4 MaskExtractPixelShader(VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveMaskUv(input);
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    float4 sample = tex2D(
        SpriteTextureSampler,
        saturate(uv));
    float3 straight = sample.a > 0.0
        ? sample.rgb / sample.a
        : 0.0;
    float luminance = dot(
        saturate(straight),
        float3(0.2126, 0.7152, 0.0722));
    float value = lerp(
        sample.a,
        luminance,
        step(0.5, MaskChannel));
    value *= inside;
    value = lerp(
        value,
        1.0 - value,
        step(0.5, MaskInvert));
    value = lerp(
        1.0,
        value,
        saturate(MaskDensity));
    return float4(value, value, value, value)
        * input.Color
        * Opacity;
}

float SampleMaskAlpha(float2 uv)
{
    float2 clampedUv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return tex2D(SpriteTextureSampler, clampedUv).a;
}

float4 MaskFeatherPixelShader(VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float value =
        SampleMaskAlpha(uv - MaskFeatherStep) +
        (4.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.75))) +
        (7.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.5))) +
        (10.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.25))) +
        (12.0 * SampleMaskAlpha(uv)) +
        (10.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.25))) +
        (7.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.5))) +
        (4.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.75))) +
        SampleMaskAlpha(uv + MaskFeatherStep);
    value /= 56.0;
    value = lerp(
        1.0,
        value,
        saturate(MaskDensity));
    return float4(value, value, value, value)
        * input.Color
        * Opacity;
}

float4 MaskAlphaPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float maskAlpha = SampleSecondary(input).a;
    return content * maskAlpha * input.Color * Opacity;
}

float4 ClipAlphaPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float clipAlpha = SampleSecondary(input).a;
    return content * clipAlpha * input.Color * Opacity;
}

float BlendLuminosity(float3 color)
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float BlendSaturation(float3 color)
{
    return max(color.r, max(color.g, color.b))
        - min(color.r, min(color.g, color.b));
}

float3 ClipBlendColor(float3 color)
{
    float luminosity = BlendLuminosity(color);
    float minimum = min(color.r, min(color.g, color.b));
    float maximum = max(color.r, max(color.g, color.b));
    if (minimum < 0.0)
    {
        color = luminosity +
            ((color - luminosity) * luminosity)
            / (luminosity - minimum);
    }
    if (maximum > 1.0)
    {
        color = luminosity +
            ((color - luminosity) * (1.0 - luminosity))
            / (maximum - luminosity);
    }
    return color;
}

float3 SetBlendLuminosity(float3 color, float luminosity)
{
    return ClipBlendColor(
        color + (luminosity - BlendLuminosity(color)));
}

float3 SetBlendSaturation(float3 color, float saturation)
{
    float red = color.r;
    float green = color.g;
    float blue = color.b;
    if (max(red, max(green, blue)) == min(red, min(green, blue)))
    {
        return 0.0;
    }

    if (red <= green)
    {
        if (green <= blue)
        {
            return float3(
                0.0,
                ((green - red) * saturation) / (blue - red),
                saturation);
        }
        if (red <= blue)
        {
            return float3(
                0.0,
                saturation,
                ((blue - red) * saturation) / (green - red));
        }
        return float3(
            ((red - blue) * saturation) / (green - blue),
            saturation,
            0.0);
    }

    if (red <= blue)
    {
        return float3(
            ((red - green) * saturation) / (blue - green),
            0.0,
            saturation);
    }
    if (green <= blue)
    {
        return float3(
            saturation,
            0.0,
            ((blue - green) * saturation) / (red - green));
    }
    return float3(
        saturation,
        ((green - blue) * saturation) / (red - blue),
        0.0);
}

float3 BlendColorBurn(float3 backdrop, float3 source)
{
    float3 denominator = max(source, 0.000001);
    float3 result = 1.0 - min(1.0, (1.0 - backdrop) / denominator);
    return lerp(result, 0.0, step(source, 0.0));
}

float3 BlendColorDodge(float3 backdrop, float3 source)
{
    float3 denominator = max(1.0 - source, 0.000001);
    float3 result = min(1.0, backdrop / denominator);
    return lerp(result, 1.0, step(1.0, source));
}

float3 BlendOverlay(float3 backdrop, float3 source)
{
    float3 low = 2.0 * backdrop * source;
    float3 high =
        1.0 - (2.0 * (1.0 - backdrop) * (1.0 - source));
    return lerp(low, high, step(0.5, backdrop));
}

float3 BlendSoftLight(float3 backdrop, float3 source)
{
    float3 low = backdrop -
        ((1.0 - (2.0 * source)) * backdrop * (1.0 - backdrop));
    float3 polynomial =
        (((16.0 * backdrop) - 12.0) * backdrop + 4.0) * backdrop;
    float3 curve = lerp(
        polynomial,
        sqrt(max(backdrop, 0.0)),
        step(0.25, backdrop));
    float3 high = backdrop +
        (((2.0 * source) - 1.0) * (curve - backdrop));
    return lerp(low, high, step(0.5, source));
}

float3 BlendVividLight(float3 backdrop, float3 source)
{
    float3 low = BlendColorBurn(backdrop, 2.0 * source);
    float3 high = BlendColorDodge(
        backdrop,
        2.0 * (source - 0.5));
    return lerp(low, high, step(0.5, source));
}

float3 BlendPinLight(float3 backdrop, float3 source)
{
    float3 low = min(backdrop, 2.0 * source);
    float3 high = max(backdrop, (2.0 * source) - 1.0);
    return lerp(low, high, step(0.5, source));
}

float3 EvaluateBlendMode(
    int mode,
    float3 backdrop,
    float3 source)
{
    backdrop = saturate(backdrop);
    source = saturate(source);
    float3 result = source;
    if (mode == 2)
    {
        result = min(backdrop, source);
    }
    else if (mode == 3)
    {
        result = backdrop * source;
    }
    else if (mode == 4)
    {
        result = BlendColorBurn(backdrop, source);
    }
    else if (mode == 5)
    {
        result = backdrop + source - 1.0;
    }
    else if (mode == 6)
    {
        result = BlendLuminosity(backdrop)
                <= BlendLuminosity(source)
            ? backdrop
            : source;
    }
    else if (mode == 7)
    {
        result = max(backdrop, source);
    }
    else if (mode == 8)
    {
        result = backdrop + source - (backdrop * source);
    }
    else if (mode == 9)
    {
        result = BlendColorDodge(backdrop, source);
    }
    else if (mode == 10)
    {
        result = backdrop + source;
    }
    else if (mode == 11)
    {
        result = BlendLuminosity(backdrop)
                >= BlendLuminosity(source)
            ? backdrop
            : source;
    }
    else if (mode == 12)
    {
        result = BlendOverlay(backdrop, source);
    }
    else if (mode == 13)
    {
        result = BlendSoftLight(backdrop, source);
    }
    else if (mode == 14)
    {
        result = BlendOverlay(source, backdrop);
    }
    else if (mode == 15)
    {
        result = BlendVividLight(backdrop, source);
    }
    else if (mode == 16)
    {
        result = backdrop + (2.0 * source) - 1.0;
    }
    else if (mode == 17)
    {
        result = BlendPinLight(backdrop, source);
    }
    else if (mode == 18)
    {
        result = step(
            0.5,
            BlendVividLight(backdrop, source));
    }
    else if (mode == 19)
    {
        result = abs(backdrop - source);
    }
    else if (mode == 20)
    {
        result = backdrop + source -
            (2.0 * backdrop * source);
    }
    else if (mode == 21)
    {
        result = backdrop - source;
    }
    else if (mode == 22)
    {
        result = lerp(
            backdrop / max(source, 0.000001),
            1.0,
            step(source, 0.0));
    }
    else if (mode == 23)
    {
        result = SetBlendLuminosity(
            SetBlendSaturation(
                source,
                BlendSaturation(backdrop)),
            BlendLuminosity(backdrop));
    }
    else if (mode == 24)
    {
        result = SetBlendLuminosity(
            SetBlendSaturation(
                backdrop,
                BlendSaturation(source)),
            BlendLuminosity(backdrop));
    }
    else if (mode == 25)
    {
        result = SetBlendLuminosity(
            source,
            BlendLuminosity(backdrop));
    }
    else if (mode == 26)
    {
        result = SetBlendLuminosity(
            backdrop,
            BlendLuminosity(source));
    }
    return saturate(result);
}

float ResolveBlendIfValue(float3 color)
{
    if (BlendIfChannel < 0.5)
    {
        return BlendLuminosity(color);
    }
    if (BlendIfChannel < 1.5)
    {
        return color.r;
    }
    if (BlendIfChannel < 2.5)
    {
        return color.g;
    }
    return color.b;
}

float EvaluateBlendIfRange(float value, float4 range)
{
    float black = range.y > range.x
        ? saturate((value - range.x) / (range.y - range.x))
        : value >= range.x ? 1.0 : 0.0;
    float white = range.w > range.z
        ? 1.0 -
            saturate((value - range.z) / (range.w - range.z))
        : value <= range.z ? 1.0 : 0.0;
    return black * white;
}

float4 CompositeAssociated(
    float4 source,
    float4 backdrop,
    float3 blended)
{
    float overlap = source.a * backdrop.a;
    return float4(
        (source.rgb * (1.0 - backdrop.a)) +
            (backdrop.rgb * (1.0 - source.a)) +
            (blended * overlap),
        source.a + backdrop.a - overlap);
}

float SampleStyleAlpha(float2 uv)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    return tex2D(
        SecondaryTextureSampler,
        saturate(uv)).a * inside;
}

float StyleBlurAlpha(
    float2 uv,
    float radius)
{
    float2 stepX = float2(PixelSize.x * radius, 0.0);
    float2 stepY = float2(0.0, PixelSize.y * radius);
    float2 diagonal = float2(
        stepX.x * 0.70710678,
        stepY.y * 0.70710678);
    float value =
        4.0 * SampleStyleAlpha(uv) +
        2.0 * SampleStyleAlpha(uv - stepX) +
        2.0 * SampleStyleAlpha(uv + stepX) +
        2.0 * SampleStyleAlpha(uv - stepY) +
        2.0 * SampleStyleAlpha(uv + stepY) +
        SampleStyleAlpha(uv - diagonal) +
        SampleStyleAlpha(uv + diagonal) +
        SampleStyleAlpha(
            uv + float2(diagonal.x, -diagonal.y)) +
        SampleStyleAlpha(
            uv + float2(-diagonal.x, diagonal.y));
    return value / 16.0;
}

float ApplyStyleContour(
    float value,
    float contour)
{
    value = saturate(value);
    if (contour < 0.5)
    {
        return value;
    }
    if (contour < 1.5)
    {
        return smoothstep(0.0, 1.0, value);
    }
    if (contour < 2.5)
    {
        return 1.0 - abs((2.0 * value) - 1.0);
    }
    return saturate(
        0.5 - (0.5 * cos(value * 6.28318531)));
}

float StyleFlag(float bit)
{
    float flags = StyleModes3.y;
    return fmod(floor(flags / bit), 2.0);
}

float StyleRandom(float2 position)
{
    float value = dot(
        floor(position),
        float2(12.9898, 78.233));
    return frac(
        sin(value + (StyleModes0.x * 17.0)) *
        43758.5453);
}

float GradientCoordinate(
    float2 uv,
    float style,
    float angle,
    float scale,
    float2 offset)
{
    float2 centered =
        (uv - 0.5 - offset) / max(scale, 0.0001);
    float2 direction = float2(cos(angle), -sin(angle));
    float coordinate =
        dot(centered, direction) + 0.5;
    if (style > 0.5 && style < 1.5)
    {
        coordinate =
            length(centered * 1.41421356);
    }
    else if (style > 1.5 && style < 2.5)
    {
        coordinate =
            (atan2(centered.y, centered.x) /
                6.28318531) + 0.5;
    }
    else if (style > 2.5 && style < 3.5)
    {
        coordinate =
            abs((coordinate * 2.0) - 1.0);
    }
    else if (style > 3.5)
    {
        coordinate =
            abs(centered.x) + abs(centered.y);
    }
    return saturate(coordinate);
}

float4 SampleStylePaint(
    VertexShaderOutput input,
    float mask)
{
    float2 uv = ResolveUv(input);
    float linked =
        max(StyleFlag(32.0), StyleFlag(512.0));
    float2 paintUv = lerp(
        input.Position.xy * PixelSize,
        uv,
        linked);
    float paintKind = StyleModes0.w;
    float scale = max(StyleOptions1.x, 0.0001);
    float2 offset = StyleOptions1.zw;
    float4 result = StyleColor;
    if (paintKind > 1.5)
    {
        if (StyleResourceAvailable < 0.5)
        {
            result = float4(0.0, 0.0, 0.0, 0.0);
        }
        else
        {
            float2 patternUv =
                (paintUv / scale) + offset;
            float4 pattern = tex2D(
                StyleTextureSampler,
                patternUv);
            float3 straight =
                pattern.rgb / max(pattern.a, 0.000001);
            result = float4(
                straight * step(0.000001, pattern.a),
                pattern.a);
        }
    }
    else if (paintKind > 0.5)
    {
        float coordinate = GradientCoordinate(
            paintUv,
            StyleModes2.w,
            StyleGeometry1.x,
            scale,
            offset);
        coordinate = lerp(
            smoothstep(0.0, 1.0, coordinate),
            coordinate,
            step(0.5, StyleModes2.z));
        if (StyleResourceAvailable > 0.5)
        {
            float4 resource = tex2D(
                StyleTextureSampler,
                float2(coordinate, 0.5));
            float3 straight =
                resource.rgb /
                max(resource.a, 0.000001);
            result = float4(
                straight *
                    step(0.000001, resource.a),
                resource.a);
        }
        else
        {
            coordinate = lerp(
                coordinate,
                1.0 - coordinate,
                StyleFlag(2.0));
            coordinate = saturate(
                coordinate +
                ((StyleRandom(input.Position.xy) - 0.5) /
                    255.0) *
                StyleFlag(4.0));
            result = lerp(
                StyleColor,
                StyleSecondaryColor,
                coordinate);
        }
    }

    return result;
}

float4 CompositeStyleOver(
    float4 style,
    float4 content,
    int blendMode)
{
    float3 styleStraight = style.a > 0.0
        ? style.rgb / style.a
        : 0.0;
    float3 contentStraight = content.a > 0.0
        ? content.rgb / content.a
        : 0.0;
    return CompositeAssociated(
        style,
        content,
        EvaluateBlendMode(
            blendMode,
            contentStraight,
            styleStraight));
}

float4 LayerStylePixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float2 uv = ResolveUv(input);
    int kind = (int)(StyleModes0.x + 0.5);
    int blendMode = (int)(StyleModes0.y + 0.5);
    int secondaryBlendMode =
        (int)(StyleModes0.z + 0.5);
    float alpha = SampleStyleAlpha(uv);
    float2 offset = StyleGeometry0.xy;
    float styleTechnique = StyleModes1.z;
    float techniqueScale = styleTechnique < 0.5
        ? 1.0
        : styleTechnique < 1.5 ? 0.65 : 0.8;
    float size = max(
        StyleGeometry0.z * techniqueScale,
        0.5);
    float spread = max(StyleGeometry0.w, 0.0);
    float shifted = StyleBlurAlpha(
        uv - offset,
        size);
    float local = StyleBlurAlpha(uv, size);
    float spreadRatio =
        spread / max(size + spread, 0.0001);
    float grown = saturate(
        shifted + (spreadRatio * (1.0 - shifted)));
    float innerEdge = saturate(
        alpha * (1.0 - local));
    float outerEdge = saturate(grown - alpha);
    float mask = alpha;

    if (kind == 0)
    {
        mask = grown *
            lerp(1.0, 1.0 - alpha, StyleFlag(16.0));
    }
    else if (kind == 1)
    {
        mask = alpha * saturate(
            1.0 - shifted + spreadRatio);
    }
    else if (kind == 2)
    {
        mask = outerEdge;
    }
    else if (kind == 3)
    {
        mask = StyleModes2.x < 0.5
            ? innerEdge
            : alpha * saturate(local - innerEdge);
    }
    else if (kind == 5)
    {
        float first = StyleBlurAlpha(
            uv - offset,
            size);
        float second = StyleBlurAlpha(
            uv + offset,
            size);
        mask = alpha * abs(first - second);
        mask = lerp(
            mask,
            alpha - mask,
            StyleFlag(8.0));
    }
    else if (kind == 9)
    {
        if (StyleModes1.w < 0.5)
        {
            mask = outerEdge;
        }
        else if (StyleModes1.w < 1.5)
        {
            mask = saturate(outerEdge + innerEdge);
        }
        else
        {
            mask = innerEdge;
        }
    }

    if (kind == 2 || kind == 3)
    {
        mask = saturate(
            mask / max(StyleModes3.z, 0.0001));
    }
    mask = ApplyStyleContour(
        mask,
        StyleModes1.x);
    mask = lerp(
        mask,
        smoothstep(0.0, 1.0, mask),
        StyleFlag(1.0));
    float noise = saturate(StyleOptions0.z);
    mask *= lerp(
        1.0,
        step(
            StyleRandom(input.Position.xy),
            mask),
        noise);
    mask = saturate(
        mask +
        ((StyleRandom(
            input.Position.xy + 37.0) - 0.5) *
            StyleOptions0.w *
            mask));

    if (kind == 4)
    {
        float lightAngle = StyleGeometry1.x;
        float altitude = StyleGeometry1.y;
        float2 light = float2(
            cos(lightAngle),
            -sin(lightAngle)) *
            PixelSize *
            max(
                StyleGeometry0.z +
                    StyleGeometry1.w,
                1.0);
        float signedEdge =
            SampleStyleAlpha(uv - light) -
            SampleStyleAlpha(uv + light);
        float edgeSign = sign(signedEdge);
        float edgeAmount = ApplyStyleContour(
            abs(signedEdge),
            StyleModes1.x);
        edgeAmount = styleTechnique < 0.5
            ? edgeAmount
            : styleTechnique < 1.5
                ? saturate(edgeAmount * 1.5)
                : step(0.2, edgeAmount);
        if (StyleFlag(256.0) > 0.5)
        {
            edgeAmount = ApplyStyleContour(
                saturate(
                    edgeAmount /
                    max(StyleModes3.z, 0.0001)),
                StyleModes1.y);
            edgeAmount = lerp(
                edgeAmount,
                smoothstep(0.0, 1.0, edgeAmount),
                StyleFlag(1024.0));
        }

        float bevelStyle = StyleModes3.x;
        float edgeSupport = alpha;
        if (bevelStyle > 0.5 && bevelStyle < 1.5)
        {
            edgeSupport = 1.0 - alpha;
        }
        else if (bevelStyle > 1.5 && bevelStyle < 3.5)
        {
            edgeSupport = 1.0;
        }
        edgeSign *= lerp(
            1.0,
            -1.0,
            step(0.5, StyleModes2.y));
        signedEdge =
            edgeSign *
            edgeAmount *
            edgeSupport *
            max(StyleGeometry1.z, 0.0) *
            max(sin(altitude), 0.05);
        float textureValue = 1.0;
        if (StyleFlag(64.0) > 0.5 &&
            StyleResourceAvailable > 0.5)
        {
            float2 textureUv = lerp(
                input.Position.xy * PixelSize,
                uv,
                StyleFlag(512.0));
            float4 sample = tex2D(
                StyleTextureSampler,
                (textureUv / max(StyleOptions1.x, 0.0001)) +
                    StyleOptions1.zw);
            textureValue = lerp(
                sample.a,
                1.0 - sample.a,
                StyleFlag(128.0));
            signedEdge +=
                (((textureValue * 2.0) - 1.0) *
                    StyleOptions1.y *
                    0.25);
        }

        float highlightAlpha =
            saturate(signedEdge) *
            alpha *
            StyleOptions0.x *
            StyleColor.a;
        float shadowAlpha =
            saturate(-signedEdge) *
            alpha *
            StyleOptions0.y *
            StyleSecondaryColor.a;
        float4 shadow = float4(
            StyleSecondaryColor.rgb * shadowAlpha,
            shadowAlpha);
        float4 highlight = float4(
            StyleColor.rgb * highlightAlpha,
            highlightAlpha);
        float4 beveled = CompositeStyleOver(
            shadow,
            content,
            secondaryBlendMode);
        return CompositeStyleOver(
            highlight,
            beveled,
            blendMode) *
            input.Color *
            Opacity;
    }

    float4 paint = SampleStylePaint(input, mask);
    float styleAlpha = saturate(
        mask *
        StyleOptions0.x *
        paint.a);
    float4 style = float4(
        paint.rgb * styleAlpha,
        styleAlpha);
    float4 result;
    if (kind == 0 || kind == 2)
    {
        result = content +
            (style * (1.0 - content.a));
    }
    else
    {
        result = CompositeStyleOver(
            style,
            content,
            blendMode);
    }

    return result * input.Color * Opacity;
}

float4 ApplyBlendChannelMask(
    float4 composite,
    float4 backdrop)
{
    float3 compositeStraight = composite.a > 0.0
        ? composite.rgb / composite.a
        : 0.0;
    float3 backdropStraight = backdrop.a > 0.0
        ? backdrop.rgb / backdrop.a
        : 0.0;
    float alpha = lerp(
        backdrop.a,
        composite.a,
        BlendChannels.a);
    float3 straight = lerp(
        backdropStraight,
        compositeStraight,
        BlendChannels.rgb);
    return float4(straight * alpha, alpha);
}

float DissolveValue(float2 position)
{
    float value = fmod(
        (floor(position.x) * 17.0) +
        (floor(position.y) * 131.0) +
        (DissolveSeed * 13.0),
        256.0);
    return value / 256.0;
}

float4 BlendPixelShader(
    VertexShaderOutput input,
    int mode) : COLOR0
{
    float4 source = SampleSource(input);
    float4 backdrop = BackgroundAvailable > 0.5
        ? SampleSecondary(input)
        : 0.0;
    float3 sourceStraight = source.a > 0.0
        ? source.rgb / source.a
        : 0.0;
    float3 backdropStraight = backdrop.a > 0.0
        ? backdrop.rgb / backdrop.a
        : 0.0;
    float blendIf = EvaluateBlendIfRange(
            ResolveBlendIfValue(sourceStraight),
            ThisLayerRange) *
        EvaluateBlendIfRange(
            ResolveBlendIfValue(backdropStraight),
            UnderlyingRange);
    source *= blendIf;

    float4 composite;
    if (mode == 1)
    {
        float selected = DissolveValue(input.Position.xy) < source.a
            ? 1.0
            : 0.0;
        float4 dissolved = float4(
            sourceStraight * selected,
            selected);
        composite = CompositeAssociated(
            dissolved,
            backdrop,
            sourceStraight);
    }
    else
    {
        float3 blended = KnockoutMode > 0.5
            ? sourceStraight
            : EvaluateBlendMode(
                mode == 27 ? 0 : mode,
                backdropStraight,
                sourceStraight);
        composite = CompositeAssociated(
            source,
            backdrop,
            blended);
    }

    return ApplyBlendChannelMask(composite, backdrop)
        * input.Color
        * Opacity;
}

float4 NormalBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 0);
}

float4 DissolveBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 1);
}

float4 DarkenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 2);
}

float4 MultiplyBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 3);
}

float4 ColorBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 4);
}

float4 LinearBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 5);
}

float4 DarkerColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 6);
}

float4 LightenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 7);
}

float4 ScreenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 8);
}

float4 ColorDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 9);
}

float4 LinearDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 10);
}

float4 LighterColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 11);
}

float4 OverlayBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 12);
}

float4 SoftLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 13);
}

float4 HardLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 14);
}

float4 VividLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 15);
}

float4 LinearLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 16);
}

float4 PinLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 17);
}

float4 HardMixBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 18);
}

float4 DifferenceBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 19);
}

float4 ExclusionBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 20);
}

float4 SubtractBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 21);
}

float4 DivideBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 22);
}

float4 HueBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 23);
}

float4 SaturationBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 24);
}

float4 ColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 25);
}

float4 LuminosityBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 26);
}

float4 PassThroughBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 27);
}

float3 DecodeSrgb(float3 value)
{
    float3 low = value / 12.92;
    float3 high = pow(max((value + 0.055) / 1.055, 0.0), 2.4);
    return lerp(high, low, step(value, 0.04045));
}

float3 EncodeSrgb(float3 value)
{
    float3 low = value * 12.92;
    float3 high = (1.055 * pow(max(value, 0.0), 1.0 / 2.4)) - 0.055;
    return lerp(high, low, step(value, 0.0031308));
}

float3 LinearSrgbToLinearDisplayP3(float3 value)
{
    return float3(
        dot(value, float3(0.822592735, 0.177533954, 0.000000027)),
        dot(value, float3(0.033199601, 0.966783523, -0.000000002)),
        dot(value, float3(0.017085349, 0.072395741, 0.910301476)));
}

float3 LinearDisplayP3ToLinearSrgb(float3 value)
{
    return float3(
        dot(value, float3(1.224745485, -0.224904439, -0.000000037)),
        dot(value, float3(-0.042058082, 1.042080996, 0.000000003)),
        dot(value, float3(-0.019642260, -0.078654881, 1.098537162)));
}

float3 Unpremultiply(float4 source)
{
    return source.a > 0.0
        ? source.rgb / source.a
        : 0.0;
}

float4 FinishColorConversion(
    VertexShaderOutput input,
    float4 source,
    float3 straight)
{
    float3 associated = source.a > 0.0
        ? straight * source.a
        : 0.0;
    return float4(associated, source.a)
        * input.Color
        * Opacity;
}

float4 InputToLinearSrgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        DecodeSrgb(straight));
}

float4 InputToSrgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(Unpremultiply(source)));
}

float4 InputToLinearDisplayP3PixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(LinearSrgbToLinearDisplayP3(
            DecodeSrgb(straight))));
}

float4 InputToDisplayP3PixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearSrgbToLinearDisplayP3(
                DecodeSrgb(straight)))));
}

float4 InputToScRgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        DecodeSrgb(straight));
}

float4 LinearSrgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(Unpremultiply(source))));
}

float4 SrgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(Unpremultiply(source)));
}

float4 LinearDisplayP3ToOutputPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearDisplayP3ToLinearSrgb(
                Unpremultiply(source)))));
}

float4 DisplayP3ToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearDisplayP3ToLinearSrgb(
                DecodeSrgb(straight)))));
}

float4 ScRgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(Unpremultiply(source))));
}

technique CopyComposite
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 CopyCompositePixelShader();
    }
}

technique NormalBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 NormalBlendPixelShader();
    }
}

technique DissolveBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DissolveBlendPixelShader();
    }
}

technique DarkenBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DarkenBlendPixelShader();
    }
}

technique MultiplyBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 MultiplyBlendPixelShader();
    }
}

technique ColorBurnBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ColorBurnBlendPixelShader();
    }
}

technique LinearBurnBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LinearBurnBlendPixelShader();
    }
}

technique DarkerColorBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DarkerColorBlendPixelShader();
    }
}

technique LightenBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LightenBlendPixelShader();
    }
}

technique ScreenBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ScreenBlendPixelShader();
    }
}

technique ColorDodgeBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ColorDodgeBlendPixelShader();
    }
}

technique LinearDodgeBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LinearDodgeBlendPixelShader();
    }
}

technique LighterColorBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LighterColorBlendPixelShader();
    }
}

technique OverlayBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 OverlayBlendPixelShader();
    }
}

technique SoftLightBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 SoftLightBlendPixelShader();
    }
}

technique HardLightBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 HardLightBlendPixelShader();
    }
}

technique VividLightBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 VividLightBlendPixelShader();
    }
}

technique LinearLightBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LinearLightBlendPixelShader();
    }
}

technique PinLightBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 PinLightBlendPixelShader();
    }
}

technique HardMixBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 HardMixBlendPixelShader();
    }
}

technique DifferenceBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DifferenceBlendPixelShader();
    }
}

technique ExclusionBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ExclusionBlendPixelShader();
    }
}

technique SubtractBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 SubtractBlendPixelShader();
    }
}

technique DivideBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DivideBlendPixelShader();
    }
}

technique HueBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 HueBlendPixelShader();
    }
}

technique SaturationBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 SaturationBlendPixelShader();
    }
}

technique ColorBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ColorBlendPixelShader();
    }
}

technique LuminosityBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LuminosityBlendPixelShader();
    }
}

technique PassThroughBlend
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 PassThroughBlendPixelShader();
    }
}

technique MaskAlpha
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 MaskAlphaPixelShader();
    }
}

technique MaskExtract
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 MaskExtractPixelShader();
    }
}

technique MaskFeather
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 MaskFeatherPixelShader();
    }
}

technique ClipAlpha
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 ClipAlphaPixelShader();
    }
}

technique LayerStyle
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LayerStylePixelShader();
    }
}

technique InputToLinearSrgb
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 InputToLinearSrgbPixelShader();
    }
}

technique InputToSrgb
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 InputToSrgbPixelShader();
    }
}

technique InputToLinearDisplayP3
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 InputToLinearDisplayP3PixelShader();
    }
}

technique InputToDisplayP3
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 InputToDisplayP3PixelShader();
    }
}

technique InputToScRgb
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 InputToScRgbPixelShader();
    }
}

technique LinearSrgbToOutput
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 LinearSrgbToOutputPixelShader();
    }
}

technique SrgbToOutput
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 SrgbToOutputPixelShader();
    }
}

technique LinearDisplayP3ToOutput
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 LinearDisplayP3ToOutputPixelShader();
    }
}

technique DisplayP3ToOutput
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 DisplayP3ToOutputPixelShader();
    }
}

technique ScRgbToOutput
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 ScRgbToOutputPixelShader();
    }
}
