Texture2D SpriteTexture;
Texture2D SecondaryTexture;
Texture2D StyleTexture;
Texture2D FilterAuxiliaryTexture;
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
float4 FilterHeader;
float4 FilterOptions0;
float4 FilterOptions1;
float4 FilterOptions2;
float4 FilterOptions3;
float4 FilterOptions4;
float4 FilterOptions5;
float4 FilterOptions6;
float4 FilterOptions7;
float4 FilterOptions8;
float4 FilterOptions9;
float2 FilterTextureSize;

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

sampler2D FilterAuxiliaryTextureSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
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
    return
        (source.rgb / max(source.a, 0.000001)) *
        step(0.000001, source.a);
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

float AdjustmentLuminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 WorkingToLinearSrgb(float3 color, int profile)
{
    if (profile == 174)
    {
        return DecodeSrgb(saturate(color));
    }
    if (profile == 175)
    {
        return LinearDisplayP3ToLinearSrgb(color);
    }
    if (profile == 176)
    {
        return LinearDisplayP3ToLinearSrgb(
            DecodeSrgb(saturate(color)));
    }
    return color;
}

float3 LinearSrgbToWorking(float3 color, int profile)
{
    if (profile == 174)
    {
        return EncodeSrgb(color);
    }
    if (profile == 175)
    {
        return LinearSrgbToLinearDisplayP3(color);
    }
    if (profile == 176)
    {
        return EncodeSrgb(
            LinearSrgbToLinearDisplayP3(color));
    }
    return color;
}

float4 WorkingAssociatedToLinearSrgb(
    float4 color,
    int profile)
{
    float3 straight = WorkingToLinearSrgb(
        Unpremultiply(color),
        profile);
    return float4(
        straight * color.a,
        color.a);
}

float4 LinearSrgbAssociatedToWorking(
    float4 color,
    int profile)
{
    float3 straight = LinearSrgbToWorking(
        Unpremultiply(color),
        profile);
    return float4(
        straight * color.a,
        color.a);
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

float2 NeighborhoodUnclampedUv(VertexShaderOutput input)
{
    return
        (input.TextureCoordinates * UvScale) +
        UvOffset;
}

float2 MirrorNeighborhoodUv(float2 uv)
{
    float2 phase = frac(uv * 0.5) * 2.0;
    return 1.0 - abs(phase - 1.0);
}

float4 SampleNeighborhood(
    float2 uv,
    int profile,
    int edgeMode)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    if (edgeMode == 2)
    {
        uv = frac(uv);
        inside = 1.0;
    }
    else if (edgeMode == 3)
    {
        uv = MirrorNeighborhoodUv(uv);
        inside = 1.0;
    }

    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    float4 sample = tex2D(
        SpriteTextureSampler,
        uv);
    if (edgeMode == 1)
    {
        sample *= inside;
    }
    return WorkingAssociatedToLinearSrgb(
        sample,
        profile);
}

float4 SampleNeighborhoodResource(float2 uv)
{
    uv = clamp(
        uv,
        0.5 / max(FilterTextureSize, 1.0),
        1.0 - (0.5 / max(FilterTextureSize, 1.0)));
    return tex2D(
        SecondaryTextureSampler,
        uv);
}

int NeighborhoodEdgeMode(int operation)
{
    if (operation == 1 ||
        operation == 2 ||
        operation == 3 ||
        operation == 4 ||
        operation == 21)
    {
        return (int)(FilterOptions0.z + 0.5);
    }
    if (operation == 6)
    {
        return (int)(FilterOptions0.w + 0.5);
    }
    if (operation == 8)
    {
        return (int)(FilterOptions0.y + 0.5);
    }
    return 0;
}

float4 SampleNeighborhoodLine(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    bool gaussian)
{
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float position = count <= 1
                ? 0.0
                : ((index / (count - 1.0)) * 2.0) - 1.0;
            float weight = gaussian
                ? exp(-3.125 * position * position)
                : 1.0;
            total += SampleNeighborhood(
                uv + (radius * PixelSize * position),
                profile,
                edgeMode) * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodDisk(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float threshold,
    bool edgeAware)
{
    float4 center = SampleNeighborhood(
        uv,
        profile,
        edgeMode);
    float3 centerStraight = Unpremultiply(center);
    float4 total = center;
    float totalWeight = 1.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 1; index < 17; index++)
    {
        if (index < count)
        {
            float fraction = index / max(count - 1.0, 1.0);
            float angle = index * 2.39996323;
            float2 offset = float2(
                cos(angle),
                sin(angle)) *
                sqrt(fraction) *
                radius *
                PixelSize;
            float4 sample = SampleNeighborhood(
                uv + offset,
                profile,
                edgeMode);
            float difference = abs(
                dot(
                    Unpremultiply(sample) - centerStraight,
                    float3(0.2126, 0.7152, 0.0722)));
            float weight = !edgeAware ||
                difference <= threshold
                ? 1.0
                : 0.0;
            total += sample * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodWhole(
    int profile,
    int sampleCount)
{
    float4 total = 0.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float2 uv = float2(
                frac((index + 0.5) * 0.618033989),
                frac((index + 0.5) * 0.414213562));
            total += SampleNeighborhood(
                uv,
                profile,
                0);
        }
    }
    return total / count;
}

float NeighborhoodHash(float2 position, float seed)
{
    float value = dot(
        floor(position),
        float2(127.1, 311.7));
    return frac(
        sin(value + (seed * 0.00006103515625)) *
        43758.5453123);
}

float4 NeighborhoodMedian3x3(
    float2 uv,
    int profile)
{
    float4 values[9];
    values[0] = SampleNeighborhood(
        uv + (float2(-1.0, -1.0) * PixelSize),
        profile,
        0);
    values[1] = SampleNeighborhood(
        uv + (float2(0.0, -1.0) * PixelSize),
        profile,
        0);
    values[2] = SampleNeighborhood(
        uv + (float2(1.0, -1.0) * PixelSize),
        profile,
        0);
    values[3] = SampleNeighborhood(
        uv + (float2(-1.0, 0.0) * PixelSize),
        profile,
        0);
    values[4] = SampleNeighborhood(uv, profile, 0);
    values[5] = SampleNeighborhood(
        uv + (float2(1.0, 0.0) * PixelSize),
        profile,
        0);
    values[6] = SampleNeighborhood(
        uv + (float2(-1.0, 1.0) * PixelSize),
        profile,
        0);
    values[7] = SampleNeighborhood(
        uv + (float2(0.0, 1.0) * PixelSize),
        profile,
        0);
    values[8] = SampleNeighborhood(
        uv + (float2(1.0, 1.0) * PixelSize),
        profile,
        0);

    for (int outer = 0; outer < 8; outer++)
    {
        for (int inner = outer + 1; inner < 9; inner++)
        {
            float outerValue = dot(
                values[outer].rgb,
                float3(0.2126, 0.7152, 0.0722));
            float innerValue = dot(
                values[inner].rgb,
                float3(0.2126, 0.7152, 0.0722));
            if (innerValue < outerValue)
            {
                float4 swap = values[outer];
                values[outer] = values[inner];
                values[inner] = swap;
            }
        }
    }
    return values[4];
}

float4 NeighborhoodSharpen(
    float4 center,
    float4 blurred,
    float amount,
    float threshold)
{
    float difference = abs(
        dot(
            Unpremultiply(center) -
                Unpremultiply(blurred),
            float3(0.2126, 0.7152, 0.0722)));
    return difference < threshold
        ? center
        : center + ((center - blurred) * amount);
}

float4 ApplyNeighborhood(
    VertexShaderOutput input,
    float4 center,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    int sampleCount =
        (int)(FilterOptions9.z + 0.5);
    int edgeMode =
        NeighborhoodEdgeMode(operation);
    float2 uv = NeighborhoodUnclampedUv(input);
    float2 radius = FilterOptions9.xy;

    if (operation == 0)
    {
        return center;
    }
    if (operation == 1 || operation == 2)
    {
        return SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            0.0,
            false);
    }
    if (operation == 3 || operation == 4)
    {
        return SampleNeighborhoodLine(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            operation == 4);
    }
    if (operation == 5)
    {
        float depth = FilterOptions1.w;
        if (FilterHeader.w > 0.5)
        {
            float4 depthSample =
                SampleNeighborhoodResource(uv);
            int depthChannel =
                (int)(FilterOptions1.z + 0.5);
            if (depthChannel == 0)
            {
                depth = dot(
                    depthSample.rgb,
                    float3(0.2126, 0.7152, 0.0722));
            }
            else if (depthChannel == 1)
            {
                depth = depthSample.r;
            }
            else if (depthChannel == 2)
            {
                depth = depthSample.g;
            }
            else if (depthChannel == 3)
            {
                depth = depthSample.b;
            }
            else
            {
                depth = depthSample.a;
            }
        }
        if (FilterOptions2.x > 0.5)
        {
            depth = 1.0 - depth;
        }
        float focus = saturate(
            abs(depth - FilterOptions1.w));
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius * max(focus, 0.05),
            sampleCount,
            0,
            profile,
            0.0,
            false);
        float highlight = step(
            FilterOptions1.y,
            dot(
                Unpremultiply(blurred),
                float3(0.2126, 0.7152, 0.0722)));
        blurred.rgb *=
            1.0 + (highlight * FilterOptions1.x);
        return blurred;
    }
    if (operation == 6)
    {
        float angle = FilterOptions0.y;
        float2 direction = float2(
            cos(angle),
            -sin(angle)) * FilterOptions0.x;
        return SampleNeighborhoodLine(
            uv,
            direction,
            sampleCount,
            edgeMode,
            profile,
            false);
    }
    if (operation == 7)
    {
        float2 centerUv = FilterOptions0.zw;
        float amount = FilterOptions0.y;
        float4 total = 0.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 0; index < 17; index++)
        {
            if (index < count)
            {
                float position = count <= 1
                    ? 0.0
                    : (index / (count - 1.0)) - 0.5;
                float2 sampleUv;
                if (FilterOptions0.x < 0.5)
                {
                    float angle = position * amount;
                    float2 delta = uv - centerUv;
                    float cosine = cos(angle);
                    float sine = sin(angle);
                    sampleUv = centerUv + float2(
                        (delta.x * cosine) -
                            (delta.y * sine),
                        (delta.x * sine) +
                            (delta.y * cosine));
                }
                else
                {
                    sampleUv = lerp(
                        uv,
                        centerUv,
                        position * amount);
                }
                total += SampleNeighborhood(
                    sampleUv,
                    profile,
                    0);
            }
        }
        return total / count;
    }
    if (operation == 8)
    {
        float4 total = center;
        float totalWeight = 1.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 1; index < 17; index++)
        {
            if (index < count)
            {
                float position =
                    (index / max(count - 1.0, 1.0));
                float angle = index * 2.39996323;
                float2 unit = float2(
                    cos(angle),
                    sin(angle));
                float weight = FilterHeader.w > 0.5
                    ? SampleNeighborhoodResource(
                        (unit * 0.5) + 0.5).a
                    : 1.0;
                total += SampleNeighborhood(
                    uv +
                        (unit * position *
                            FilterOptions0.x * PixelSize),
                    profile,
                    edgeMode) * weight;
                totalWeight += weight;
            }
        }
        return total / max(totalWeight, 0.000001);
    }
    if (operation == 9 || operation == 10)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            0,
            profile,
            FilterOptions0.y,
            true);
        if (operation == 9 &&
            FilterOptions0.w > 0.5)
        {
            float edge = saturate(
                length(center.rgb - blurred.rgb) * 4.0);
            float4 edgeColor = float4(
                edge,
                edge,
                edge,
                center.a);
            return FilterOptions0.w > 1.5
                ? lerp(center, edgeColor, edge)
                : edgeColor;
        }
        return blurred;
    }
    if (operation == 11)
    {
        float amount = FilterHeader.w > 0.5
            ? SampleNeighborhoodResource(uv).r
            : 0.0;
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            amount * 24.0,
            sampleCount,
            0,
            profile,
            0.0,
            false);
        return lerp(center, blurred, saturate(amount));
    }
    if (operation == 12)
    {
        float2 delta =
            (uv - FilterOptions0.xy) /
            max(FilterOptions0.zw, 0.000001);
        float angle = -FilterOptions1.y;
        float2 rotated = float2(
            (delta.x * cos(angle)) -
                (delta.y * sin(angle)),
            (delta.x * sin(angle)) +
                (delta.y * cos(angle)));
        float amount = smoothstep(
            1.0,
            1.0 + max(FilterOptions1.x, 0.000001),
            length(rotated));
        return lerp(
            center,
            SampleNeighborhoodDisk(
                uv,
                radius,
                sampleCount,
                0,
                profile,
                0.0,
                false),
            amount);
    }
    if (operation == 13)
    {
        float2 direction = float2(
            -sin(FilterOptions0.z),
            cos(FilterOptions0.z));
        float distance = abs(
            dot(
                uv - FilterOptions0.xy,
                direction));
        float amount = smoothstep(
            FilterOptions0.w,
            FilterOptions0.w +
                max(FilterOptions1.x, 0.000001),
            distance);
        return lerp(
            center,
            SampleNeighborhoodDisk(
                uv,
                radius,
                sampleCount,
                0,
                profile,
                0.0,
                false),
            amount);
    }
    if (operation == 14)
    {
        float4 path = FilterHeader.w > 0.5
            ? SampleNeighborhoodResource(uv)
            : float4(0.5, 0.5, 0.0, 1.0);
        float2 direction =
            (path.rg * 2.0) - 1.0;
        direction /= max(length(direction), 0.000001);
        float speed = lerp(
            FilterOptions0.x,
            FilterOptions0.w,
            saturate(path.b));
        return SampleNeighborhoodLine(
            uv,
            direction * speed,
            sampleCount,
            0,
            profile,
            false);
    }
    if (operation == 15)
    {
        float2 centerUv = FilterOptions0.xy;
        float2 delta = uv - centerUv;
        float2 normalized =
            delta / max(FilterOptions0.zw, 0.000001);
        float inside = step(
            length(normalized),
            1.0);
        float4 total = 0.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 0; index < 17; index++)
        {
            if (index < count)
            {
                float position = count <= 1
                    ? 0.0
                    : (index / (count - 1.0)) - 0.5;
                float angle =
                    position * FilterOptions1.x;
                float cosine = cos(angle);
                float sine = sin(angle);
                float2 sampleUv = centerUv + float2(
                    (delta.x * cosine) -
                        (delta.y * sine),
                    (delta.x * sine) +
                        (delta.y * cosine));
                total += SampleNeighborhood(
                    sampleUv,
                    profile,
                    0);
            }
        }
        return lerp(
            center,
            total / count,
            inside);
    }
    if (operation == 16 || operation == 17)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            1.0,
            9,
            0,
            profile,
            0.0,
            false);
        float amount = FilterOptions0.x *
            (operation == 17 ? 2.0 : 1.0);
        return NeighborhoodSharpen(
            center,
            blurred,
            amount,
            0.0);
    }
    if (operation == 18)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            1.0,
            9,
            0,
            profile,
            0.0,
            false);
        return NeighborhoodSharpen(
            center,
            blurred,
            FilterOptions0.x,
            FilterOptions0.y);
    }
    if (operation == 19 || operation == 20)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            max(FilterOptions0.y, 0.000001),
            9,
            0,
            profile,
            0.0,
            false);
        float amount = FilterOptions0.x;
        float threshold = operation == 19
            ? FilterOptions0.z
            : 0.0;
        float4 sharpened = NeighborhoodSharpen(
            center,
            blurred,
            amount,
            threshold);
        if (operation == 20)
        {
            sharpened = lerp(
                sharpened,
                blurred,
                saturate(FilterOptions0.z));
        }
        return sharpened;
    }
    if (operation == 21)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            0.0,
            false);
        return float4(
            saturate(
                0.5 * center.a +
                center.rgb -
                blurred.rgb),
            center.a);
    }
    if (operation == 22)
    {
        float seed =
            FilterOptions0.w +
            (FilterOptions1.x * 65536.0);
        float redNoise =
            NeighborhoodHash(input.Position.xy, seed);
        float greenNoise = FilterOptions0.z > 0.5
            ? redNoise
            : NeighborhoodHash(
                input.Position.xy + 19.0,
                seed);
        float blueNoise = FilterOptions0.z > 0.5
            ? redNoise
            : NeighborhoodHash(
                input.Position.xy + 47.0,
                seed);
        float3 noise =
            float3(redNoise, greenNoise, blueNoise) *
            2.0 - 1.0;
        if (FilterOptions0.y > 0.5)
        {
            noise = (
                noise +
                float3(
                    NeighborhoodHash(
                        input.Position.xy + 71.0,
                        seed),
                    NeighborhoodHash(
                        input.Position.xy + 89.0,
                        seed),
                    NeighborhoodHash(
                        input.Position.xy + 107.0,
                        seed)) *
                    2.0 - 1.0) * 0.5;
        }
        float3 straight = saturate(
            Unpremultiply(center) +
            (noise * FilterOptions0.x));
        return float4(
            straight * center.a,
            center.a);
    }
    if (operation == 23 ||
        operation == 24 ||
        operation == 25)
    {
        float4 median = NeighborhoodMedian3x3(
            uv,
            profile);
        if (operation == 25)
        {
            return median;
        }
        float difference = abs(
            dot(
                Unpremultiply(center) -
                    Unpremultiply(median),
                float3(0.2126, 0.7152, 0.0722)));
        float threshold = operation == 23
            ? FilterOptions0.x
            : FilterOptions0.y;
        return difference > threshold
            ? median
            : center;
    }

    float4 denoised = SampleNeighborhoodDisk(
        uv,
        1.0,
        9,
        0,
        profile,
        0.0,
        false);
    float detail = saturate(FilterOptions0.y);
    float4 reduced = lerp(
        denoised,
        center,
        detail);
    reduced = NeighborhoodSharpen(
        reduced,
        denoised,
        FilterOptions0.w,
        0.0);
    return lerp(
        center,
        reduced,
        saturate(FilterOptions0.x));
}

float ResamplingInside(float2 uv)
{
    return
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
}

float2 ResamplingMirror(float2 uv)
{
    return 1.0 - abs((frac(uv * 0.5) * 2.0) - 1.0);
}

float4 SampleResamplingSource(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float inside = ResamplingInside(uv);
    if (edgeMode == 1 && inside < 0.5)
    {
        return 0.0;
    }
    if (edgeMode == 4 && inside < 0.5)
    {
        float4 associatedFill = float4(
            fillColor.rgb * fillColor.a,
            fillColor.a);
        return WorkingAssociatedToLinearSrgb(
            associatedFill,
            profile);
    }
    if (edgeMode == 2)
    {
        uv = frac(uv);
    }
    else if (edgeMode == 3)
    {
        uv = ResamplingMirror(uv);
    }
    else
    {
        uv = clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5));
    }

    return WorkingAssociatedToLinearSrgb(
        tex2D(SpriteTextureSampler, uv),
        profile);
}

float ResamplingChannel(float4 sample, int channel)
{
    if (channel == 0)
    {
        return sample.r;
    }
    if (channel == 1)
    {
        return sample.g;
    }
    if (channel == 2)
    {
        return sample.b;
    }
    if (channel == 3)
    {
        return sample.a;
    }
    return dot(
        sample.rgb,
        float3(0.2126, 0.7152, 0.0722));
}

float ResamplingWaveShape(float phase, int kind)
{
    if (kind == 1)
    {
        return (abs(frac(phase) - 0.5) * 4.0) - 1.0;
    }
    if (kind == 2)
    {
        return step(0.5, frac(phase)) * 2.0 - 1.0;
    }
    return sin(phase * 6.28318531);
}

float4 ApplyResampling(
    VertexShaderOutput input,
    float4 source,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    int passKind = (int)(FilterHeader.z + 0.5);
    float2 uv = ResolveUv(input);
    float2 mapped = float2(uv.x, uv.y);
    int edgeMode = 0;
    float4 fillColor =
        float4(0.0, 0.0, 0.0, 0.0);

    if (operation == 3 && passKind == 1)
    {
        float2 stepSize =
            PixelSize *
            max(FilterOptions0.y, 0.5);
        float4 diffuse = (
            SampleResamplingSource(
                uv,
                profile,
                0,
                fillColor) * 4.0 +
            SampleResamplingSource(
                uv + float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv - float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv + float2(0.0, stepSize.y),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv - float2(0.0, stepSize.y),
                profile,
                0,
                fillColor)) / 8.0;
        return lerp(
            source,
            diffuse,
            saturate(FilterOptions0.y));
    }
    else if (operation == 0)
    {
        float2 origin = FilterOptions2.xy;
        float2 size = max(FilterOptions3.xy, 1.0);
        float2 position =
            uv - origin - (FilterOptions0.xy / size);
        float cosine = cos(-FilterOptions1.x);
        float sine = sin(-FilterOptions1.x);
        float2 unrotated = float2(
            (position.x * cosine) -
                (position.y * sine),
            (position.x * sine) +
                (position.y * cosine));
        float determinant = max(
            1.0 -
                (FilterOptions1.y * FilterOptions1.z),
            0.000001);
        position = float2(
            unrotated.x -
                (FilterOptions1.y * unrotated.y),
            unrotated.y -
                (FilterOptions1.z * unrotated.x)) /
            determinant;
        float2 scale = FilterOptions0.zw;
        float2 safeScale = float2(
            scale.x < 0.0
                ? min(scale.x, -0.000001)
                : max(scale.x, 0.000001),
            scale.y < 0.0
                ? min(scale.y, -0.000001)
                : max(scale.y, 0.000001));
        position /= safeScale;
        mapped = origin + position;
        edgeMode = (int)(FilterOptions2.z + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 1)
    {
        float2 constraint = tex2D(
            SecondaryTextureSampler,
            uv).rg * 2.0 - 1.0;
        float2 centered = uv - 0.5;
        float radius = dot(centered, centered);
        float projection =
            1.0 + (FilterOptions0.x * 0.12);
        float focal = FilterOptions0.y > 0.5
            ? 0.75
            : 1.0;
        float radialScale =
            1.0 +
            (radius * projection * focal /
                max(FilterOptions0.z, 0.0001));
        centered = centered * radialScale;
        float angle = -FilterOptions1.x;
        float cosine = cos(angle);
        float sine = sin(angle);
        float2 rotated = float2(
            (centered.x * cosine) -
                (centered.y * sine),
            (centered.x * sine) +
                (centered.y * cosine));
        mapped =
            0.5 +
            (rotated /
                max(FilterOptions0.w, 0.0001)) -
            (FilterOptions1.yz * PixelSize) +
            (constraint * PixelSize);
        edgeMode = 1;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 2)
    {
        float2 centered = uv - 0.5;
        float angle = -FilterOptions1.w;
        float cosine = cos(angle);
        float sine = sin(angle);
        centered = float2(
            (centered.x * cosine) -
                (centered.y * sine),
            (centered.x * sine) +
                (centered.y * cosine));
        centered.x -= FilterOptions1.z * centered.y;
        centered.y -= FilterOptions1.y * centered.x;
        float radius = dot(centered, centered);
        centered *=
            (1.0 +
                (FilterOptions0.x * radius)) /
            max(FilterOptions2.x, 0.0001);
        mapped = centered + 0.5;
        edgeMode = (int)(FilterOptions2.y + 0.5);
        float4 baseSample = SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
        float2 chromaDirection =
            centered * radius * 0.01;
        float4 redSample = SampleResamplingSource(
            mapped +
                (chromaDirection * FilterOptions0.y),
            profile,
            edgeMode,
            fillColor);
        float4 blueSample = SampleResamplingSource(
            mapped +
                (chromaDirection * FilterOptions0.z),
            profile,
            edgeMode,
            fillColor);
        float vignette = 1.0 -
            (saturate(
                (length(centered) -
                    FilterOptions1.x) /
                max(1.0 - FilterOptions1.x, 0.0001)) *
                FilterOptions0.w);
        return float4(
            float3(
                redSample.r,
                baseSample.g,
                blueSample.b) * vignette,
            baseSample.a);
    }
    else if (operation == 3)
    {
        float noise = NeighborhoodHash(
            input.Position.xy,
            9173.0);
        float3 straight = saturate(
            Unpremultiply(source) +
            ((noise - 0.5) *
                FilterOptions0.x) +
            (FilterOptions1.rgb *
                FilterOptions0.z));
        return float4(
            straight * source.a,
            source.a);
    }
    else if (operation == 4)
    {
        float2 mapUv = FilterOptions0.z < 0.5
            ? uv
            : frac(uv);
        float4 map = tex2D(
            SecondaryTextureSampler,
            mapUv);
        float2 displacement = float2(
            ResamplingChannel(
                map,
                (int)(FilterOptions1.x + 0.5)),
            ResamplingChannel(
                map,
                (int)(FilterOptions1.y + 0.5)));
        displacement = (displacement * 2.0) - 1.0;
        mapped =
            uv -
            (displacement *
                FilterOptions0.xy *
                PixelSize);
        edgeMode = (int)(FilterOptions0.w + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 5)
    {
        float4 textureSample = FilterHeader.w > 0.5
            ? tex2D(SecondaryTextureSampler, uv)
            : float4(
                NeighborhoodHash(
                    input.Position.xy,
                    FilterOptions0.z + 31.0),
                NeighborhoodHash(
                    input.Position.xy + 43.0,
                    FilterOptions0.z + 59.0),
                0.5,
                1.0);
        float2 displacement =
            (textureSample.rg * 2.0) - 1.0;
        displacement = lerp(
            displacement,
            -displacement,
            step(0.5, FilterOptions1.x));
        mapped =
            uv -
            (displacement *
                FilterOptions0.x *
                max(FilterOptions0.w, 0.0001) *
                PixelSize);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 6)
    {
        float seed =
            FilterOptions0.z +
            (FilterOptions0.w * 65536.0);
        float2 position = uv /
            max(FilterOptions0.x * PixelSize, 0.000001);
        float noise = NeighborhoodHash(
            floor(position),
            seed);
        mapped = uv + float2(
            sin((position.y + noise) * 6.28318531),
            cos((position.x - noise) * 6.28318531)) *
            FilterOptions0.y *
            PixelSize;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 7)
    {
        float2 center = FilterOptions0.yz;
        float2 delta = uv - center;
        float radius = length(delta) / 0.70710678;
        float power =
            1.0 + FilterOptions0.x;
        mapped = center +
            (delta *
                pow(
                    max(radius, 0.000001),
                    power - 1.0));
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 8)
    {
        float2 center = FilterOptions0.yz;
        if (FilterOptions0.x < 0.5)
        {
            float angle =
                (uv.x - center.x) * 6.28318531;
            float radius =
                (uv.y - center.y + 0.5) *
                0.70710678;
            mapped = center + float2(
                cos(angle),
                sin(angle)) * radius;
        }
        else
        {
            float2 delta = uv - center;
            mapped = float2(
                center.x +
                    (atan2(delta.y, delta.x) /
                        6.28318531),
                center.y - 0.5 +
                    (length(delta) /
                        0.70710678));
        }
        edgeMode = 2;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 9)
    {
        float seed =
            FilterOptions0.z +
            (FilterOptions0.w * 65536.0);
        float size =
            6.0 + (FilterOptions0.y * 8.0);
        float phase =
            (uv.y * size) +
            (NeighborhoodHash(
                floor(input.Position.xy / size),
                seed) * 6.28318531);
        mapped.x +=
            sin(phase) *
            FilterOptions0.x *
            PixelSize.x;
        edgeMode = (int)(FilterOptions1.x + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 10)
    {
        float y = saturate(uv.y);
        float curve = FilterOptions0.x;
        float amount = curve < 0.5
            ? 0.0
            : curve < 1.5
                ? y * y
                : curve < 2.5
                    ? 1.0 -
                        ((1.0 - y) * (1.0 - y))
                    : curve < 3.5
                        ? smoothstep(0.0, 1.0, y)
                        : sin((y - 0.5) * 3.14159265);
        mapped.x -= (amount - 0.5) * 0.5;
        edgeMode = (int)(FilterOptions0.y + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 11)
    {
        float2 center = FilterOptions0.zw;
        float2 delta = uv - center;
        float radius = saturate(
            length(delta) / 0.70710678);
        float amount =
            FilterOptions0.x *
            (1.0 - radius * radius);
        float2 warped = delta /
            max(1.0 + amount, 0.0001);
        if (FilterOptions0.y > 0.5 &&
            FilterOptions0.y < 1.5)
        {
            warped.y = delta.y;
        }
        else if (FilterOptions0.y > 1.5)
        {
            warped.x = delta.x;
        }
        mapped = center + warped;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 12)
    {
        float2 center = FilterOptions0.yz;
        float2 delta = uv - center;
        float radius = length(delta) / 0.70710678;
        float angle =
            -FilterOptions0.x *
            saturate(1.0 - radius);
        float cosine = cos(angle);
        float sine = sin(angle);
        mapped = center + float2(
            (delta.x * cosine) -
                (delta.y * sine),
            (delta.x * sine) +
                (delta.y * cosine));
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 13)
    {
        float seed =
            FilterOptions2.y +
            (FilterOptions2.z * 65536.0);
        int kind =
            (int)(FilterOptions0.w + 0.5);
        float generators =
            max(FilterOptions0.x, 1.0);
        float phase =
            NeighborhoodHash(
                floor(input.Position.xy),
                seed);
        float waveX = ResamplingWaveShape(
            (uv.y * generators /
                max(FilterOptions0.y * PixelSize.y, 0.000001)) +
                phase,
            kind);
        float waveY = ResamplingWaveShape(
            (uv.x * generators /
                max(FilterOptions0.z * PixelSize.x, 0.000001)) -
                phase,
            kind);
        mapped += float2(
            waveX * FilterOptions1.x *
                FilterOptions1.z,
            waveY * FilterOptions1.y *
                FilterOptions1.w) * PixelSize;
        edgeMode = (int)(FilterOptions2.x + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 14)
    {
        float2 center = FilterOptions1.xy;
        float2 delta = uv - center;
        float radius = length(delta);
        float angle = atan2(delta.y, delta.x);
        float ridges = max(FilterOptions0.y, 1.0);
        float oscillation =
            sin((radius * ridges * 40.0) +
                (FilterOptions0.z > 1.5
                    ? angle * ridges
                    : 0.0));
        float amount =
            oscillation *
            FilterOptions0.x *
            PixelSize.x;
        if (FilterOptions0.z < 0.5)
        {
            mapped = center +
                normalize(delta + 0.000001) *
                (radius + amount);
        }
        else
        {
            angle += amount * 10.0;
            mapped = center + float2(
                cos(angle),
                sin(angle)) * radius;
        }
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 15)
    {
        float4 mesh = tex2D(
            SecondaryTextureSampler,
            uv);
        float2 displacement =
            (mesh.rg * 2.0) - 1.0;
        float mask = FilterOptions6.x > 0.5
            ? tex2D(
                FilterAuxiliaryTextureSampler,
                uv).a
            : 1.0;
        mask = lerp(
            mask,
            1.0 - mask,
            step(0.5, FilterOptions0.y));
        mapped =
            uv -
            (displacement *
                (1.0 - saturate(FilterOptions0.x)) *
                mask);
        edgeMode = (int)(FilterOptions0.z + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else
    {
        mapped =
            uv -
            (FilterOptions0.xy * PixelSize);
        edgeMode = (int)(FilterOptions0.z + 0.5);
        fillColor = FilterOptions1;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }

}

float4 CatalogLinearSample(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            SpriteTextureSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}

float CatalogLuminance(float4 color)
{
    return dot(
        saturate(Unpremultiply(color)),
        float3(0.2126, 0.7152, 0.0722));
}

float CatalogParameterMagnitude()
{
    return dot(abs(FilterOptions0), 1.0) +
        dot(abs(FilterOptions1), 1.0) +
        dot(abs(FilterOptions2), 1.0) +
        dot(abs(FilterOptions3), 1.0) +
        dot(abs(FilterOptions4), 1.0) +
        dot(abs(FilterOptions5), 1.0) +
        dot(abs(FilterOptions6), 1.0) +
        dot(abs(FilterOptions7), 1.0) +
        dot(abs(FilterOptions8), 1.0);
}

float CatalogSeed()
{
    return dot(
        float4(
            FilterOptions0.x,
            FilterOptions1.x,
            FilterOptions2.x,
            FilterOptions3.x),
        float4(1.0, 17.0, 257.0, 4099.0)) +
        dot(
            float4(
                FilterOptions4.x,
                FilterOptions5.x,
                FilterOptions6.x,
                FilterOptions7.x),
            float4(65537.0, 31.0, 521.0, 8191.0)) +
        FilterOptions8.x;
}

float CatalogSobel(float2 uv, int profile)
{
    float topLeft = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, -PixelSize.y),
            profile));
    float top = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(0.0, -PixelSize.y),
            profile));
    float topRight = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(PixelSize.x, -PixelSize.y),
            profile));
    float left = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, 0.0),
            profile));
    float right = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(PixelSize.x, 0.0),
            profile));
    float bottomLeft = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, PixelSize.y),
            profile));
    float bottom = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(0.0, PixelSize.y),
            profile));
    float bottomRight = CatalogLuminance(
        CatalogLinearSample(
            uv + PixelSize,
            profile));
    float horizontal =
        -topLeft + topRight -
        (2.0 * left) + (2.0 * right) -
        bottomLeft + bottomRight;
    float vertical =
        -topLeft - (2.0 * top) - topRight +
        bottomLeft + (2.0 * bottom) + bottomRight;
    return saturate(length(float2(horizontal, vertical)));
}

float4 CatalogMorphology(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 stepSize =
        FilterOptions9.xy * PixelSize;
    float4 negative = CatalogLinearSample(
        uv - stepSize,
        profile);
    float4 positive = CatalogLinearSample(
        uv + stepSize,
        profile);
    return filterId == 55
        ? max(source, max(negative, positive))
        : min(source, min(negative, positive));
}

float4 CatalogQuantization(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 65)
    {
        return (
            (source * 4.0) +
            CatalogLinearSample(
                uv + float2(PixelSize.x, 0.0),
                profile) +
            CatalogLinearSample(
                uv - float2(PixelSize.x, 0.0),
                profile) +
            CatalogLinearSample(
                uv + float2(0.0, PixelSize.y),
                profile) +
            CatalogLinearSample(
                uv - float2(0.0, PixelSize.y),
                profile)) / 8.0;
    }
    else if (filterId == 66)
    {
        float offset = max(FilterOptions0.x, 0.0);
        float2 diagonal = PixelSize * offset;
        return (
            CatalogLinearSample(uv - diagonal, profile) +
            CatalogLinearSample(
                uv + float2(diagonal.x, -diagonal.y),
                profile) +
            CatalogLinearSample(
                uv + float2(-diagonal.x, diagonal.y),
                profile) +
            CatalogLinearSample(uv + diagonal, profile)) /
            4.0;
    }
    else
    {
        float cell = max(
            filterId == 63
                ? FilterOptions1.x
                : FilterOptions0.x,
            1.0);
        float2 cellIndex = floor(pixel / cell);
        float2 center = (cellIndex + 0.5) * cell;
        float seed = CatalogSeed();
        if (filterId == 64 || filterId == 69)
        {
            center =
                (cellIndex +
                    float2(
                        NeighborhoodHash(cellIndex, seed),
                        NeighborhoodHash(cellIndex, seed + 1.0))) *
                cell;
        }
        float4 sampled = CatalogLinearSample(
            center * PixelSize,
            profile);
        if (filterId == 67)
        {
            float noise = NeighborhoodHash(pixel, seed);
            float value =
                step(noise, CatalogLuminance(source));
            return float4(
                value * source.a,
                value * source.a,
                value * source.a,
                source.a);
        }
        if (filterId == 63)
        {
            float dotValue = NeighborhoodHash(cellIndex, 63.0);
            float value =
                step(dotValue, CatalogLuminance(source));
            return float4(
                value * source.a,
                value * source.a,
                value * source.a,
                source.a);
        }
        if (filterId == 69 &&
            distance(pixel, center) > cell * 0.45)
        {
            float4 background = FilterOptions0;
            return float4(
                background.rgb *
                    source.a *
                    background.a,
                source.a * background.a);
        }
        return sampled;
    }
}

float4 CatalogProcedural(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    float packedPass = floor(FilterOptions9.z / 4.0);
    float seed = CatalogSeed() +
        (packedPass * 4099.0);
    float noise = NeighborhoodHash(pixel, seed);
    if (filterId == 70 || filterId == 71)
    {
        float4 background = FilterOptions0;
        float4 foreground = FilterOptions1;
        float3 pattern = lerp(
            background.rgb,
            foreground.rgb,
            noise);
        if (filterId == 71)
        {
            pattern = abs(
                saturate(Unpremultiply(source)) -
                pattern);
        }
        return float4(pattern * source.a, source.a);
    }
    if (filterId == 72)
    {
        float fibers = saturate(
            0.5 +
            ((NeighborhoodHash(
                    float2(
                        pixel.x * 0.25,
                        pixel.y),
                    seed) -
                0.5) *
                max(FilterOptions3.x, 1.0)));
        return float4(
            lerp(
                FilterOptions0.rgb,
                FilterOptions1.rgb,
                fibers) * source.a,
            source.a);
    }
    if (filterId == 73)
    {
        float2 center = FilterOptions1.xy;
        float flare = pow(
            saturate(1.0 - (distance(uv, center) * 3.0)),
            2.0) *
            max(FilterOptions0.x, 0.0);
        float3 straight = saturate(
            Unpremultiply(source) +
            float3(
                flare,
                flare * 0.75,
                flare * 0.35));
        return float4(straight * source.a, source.a);
    }
    if (filterId == 74)
    {
        float4 light = tex2D(
            SecondaryTextureSampler,
            uv);
        float4 textureSample =
            FilterHeader.w >= 2.0
                ? tex2D(
                    FilterAuxiliaryTextureSampler,
                    uv)
                : 1.0;
        float intensity = saturate(
            FilterOptions0.x +
            dot(light.rgb, float3(0.2126, 0.7152, 0.0722)) *
            (0.5 +
                (0.5 *
                    dot(
                        textureSample.rgb,
                        float3(0.2126, 0.7152, 0.0722)))));
        return float4(
            saturate(Unpremultiply(source) * intensity) *
                source.a,
            source.a);
    }

    float angle = noise * 6.28318530718;
    float2 offset = float2(cos(angle), sin(angle)) *
        FilterOptions9.xy *
        PixelSize;
    return CatalogLinearSample(uv + offset, profile);
}

float4 CatalogVideo(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 75)
    {
        float oddLine = fmod(floor(pixel.y), 2.0);
        float4 interpolation = (
            CatalogLinearSample(
                uv - float2(0.0, PixelSize.y),
                profile) +
            CatalogLinearSample(
                uv + float2(0.0, PixelSize.y),
                profile)) * 0.5;
        return lerp(source, interpolation, oddLine);
    }
    else if (filterId == 76)
    {
        float3 straight =
            saturate(Unpremultiply(source));
        float luminance = dot(
            straight,
            float3(0.299, 0.587, 0.114));
        float3 limited = saturate(
            luminance +
            clamp(
                straight - luminance,
                -(1.0 - luminance),
                1.0 - luminance));
        return float4(limited * source.a, source.a);
    }

    float frequency = max(FilterOptions1.x, 1.0);
    float scanlinePosition = frac(
        (uv.y * frequency) +
        FilterOptions4.x);
    float coverage =
        step(scanlinePosition, saturate(FilterOptions5.x)) *
        saturate(FilterOptions2.x);
    float4 lineColor = FilterOptions0;
    float3 result = lerp(
        saturate(Unpremultiply(source)),
        lineColor.rgb,
        coverage * lineColor.a);
    return float4(result * source.a, source.a);
}

float4 CatalogArtistic(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float edge = CatalogSobel(uv, profile);
    float3 straight =
        saturate(Unpremultiply(source));
    float amount = saturate(
        0.05 +
        (CatalogParameterMagnitude() * 0.01));
    float noise = NeighborhoodHash(
        uv / PixelSize,
        CatalogSeed()) - 0.5;
    uint variant = ((uint)filterId - 77u) % 6u;
    float3 result = straight;
    if (variant == 0)
    {
        result =
            floor(straight * 6.0 + 0.5) / 6.0 -
            edge * amount;
    }
    else if (variant == 1)
    {
        result = lerp(
            straight,
            CatalogLuminance(source),
            amount);
    }
    else if (variant == 2)
    {
        result =
            floor(
                saturate(straight + noise * amount) *
                8.0 +
                0.5) / 8.0;
    }
    else if (variant == 3)
    {
        result = lerp(
            straight,
            1.0 - edge,
            amount);
    }
    else if (variant == 4)
    {
        result = lerp(
            straight,
            straight * float3(1.1, 0.95, 0.8),
            amount);
    }
    else
    {
        result += float3(
            edge * amount,
            -edge * amount * 0.5,
            noise * amount);
    }
    return float4(saturate(result) * source.a, source.a);
}

float4 CatalogEdge(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float edge = CatalogSobel(uv, profile);
    if (filterId == 115)
    {
        float angle =
            FilterOptions1.x * 0.01745329252;
        float2 direction =
            float2(cos(angle), sin(angle)) *
            PixelSize *
            max(FilterOptions2.x, 1.0);
        float delta =
            CatalogLuminance(
                CatalogLinearSample(
                    uv + direction,
                    profile)) -
            CatalogLuminance(
                CatalogLinearSample(
                    uv - direction,
                    profile));
        float value = saturate(
            0.5 + (delta * FilterOptions0.x));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }
    else if (filterId == 117)
    {
        float value = 1.0 - saturate(
            (edge - FilterOptions0.x) /
            max(1.0 - FilterOptions0.x, 0.0001));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }
    else if (filterId == 118)
    {
        float3 glow = saturate(
            float3(edge * 0.25, edge * 0.6, edge) *
            max(FilterOptions0.x, 1.0));
        return float4(glow * source.a, source.a);
    }
    else if (filterId == 121)
    {
        float value = step(
            max(edge, 0.02),
            abs(
                CatalogLuminance(source) -
                FilterOptions1.x));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }

    float4 foreground = FilterOptions0;
    float4 background = FilterOptions1;
    float mixValue = saturate(
        CatalogLuminance(source) +
        (edge * 0.5));
    float3 sketch = lerp(
        foreground.rgb,
        background.rgb,
        mixValue);
    float amount = saturate(
        0.35 +
        (CatalogParameterMagnitude() * 0.01));
    return float4(
        lerp(
            saturate(Unpremultiply(source)),
            sketch,
            amount) * source.a,
        source.a);
}

float4 CatalogTiling(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 133)
    {
        float amount = FilterOptions0.x;
        float2 direction = FilterOptions2.xy;
        direction = length(direction) > 0.0001
            ? normalize(direction)
            : float2(1.0, 0.0);
        float2 offset =
            direction * amount * PixelSize;
        float4 red = CatalogLinearSample(
            uv + offset,
            profile);
        float4 blue = CatalogLinearSample(
            uv - offset,
            profile);
        return float4(
            red.r,
            source.g,
            blue.b,
            source.a);
    }
    else if (filterId == 122)
    {
        float strength = max(FilterOptions3.x, 0.0);
        return (
            (source * 2.0) +
            CatalogLinearSample(
                uv + float2(
                    strength * PixelSize.x,
                    0.0),
                profile) +
            CatalogLinearSample(
                uv + float2(
                    strength * 2.0 * PixelSize.x,
                    0.0),
                profile)) / 4.0;
    }
    else
    {
        float size = filterId == 116
            ? max(FilterOptions5.x, 1.0)
            : max(
                1.0,
                max(
                    1.0 / PixelSize.x,
                    1.0 / PixelSize.y) /
                max(FilterOptions4.x, 1.0));
        float2 cell = floor(pixel / size);
        float2 samplePixel =
            (cell + 0.5) * size;
        float randomOffset =
            NeighborhoodHash(
                cell,
                CatalogSeed()) - 0.5;
        samplePixel += randomOffset *
            size *
            max(FilterOptions2.x, 0.1);
        return CatalogLinearSample(
            samplePixel * PixelSize,
            profile);
    }
}

float4 CatalogTexture(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float textureValue =
        FilterHeader.w >= 1.0
            ? dot(
                tex2D(
                    SecondaryTextureSampler,
                    uv).rgb,
                float3(0.2126, 0.7152, 0.0722))
            : NeighborhoodHash(
                uv / PixelSize,
                CatalogSeed());
    float edge = CatalogSobel(uv, profile);
    float relief = saturate(
        0.05 +
        (CatalogParameterMagnitude() * 0.002));
    float3 result =
        saturate(Unpremultiply(source)) +
        float3(
            (textureValue - 0.5) * relief,
            (edge - 0.5) * relief * 0.5,
            (textureValue - edge) * relief * 0.35);
    return float4(saturate(result) * source.a, source.a);
}

float4 CatalogConvolution(
    float2 uv,
    float4 source,
    int profile)
{
    float4 total = 0.0;
    float weightTotal = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 kernelUv =
                (float2(x, y) + 1.5) / 3.0;
            float weight = tex2D(
                SecondaryTextureSampler,
                kernelUv).r;
            total += CatalogLinearSample(
                uv + (float2(x, y) * PixelSize),
                profile) * weight;
            weightTotal += weight;
        }
    }
    float divisor = abs(weightTotal) < 0.000001
        ? 1.0
        : weightTotal;
    float4 result =
        (total / divisor) *
        FilterOptions4.x +
        FilterOptions3.x;
    result.a = lerp(
        source.a,
        result.a,
        step(0.5, FilterOptions0.x));
    return result;
}

float3 CatalogRotateHue(float3 color, float degrees)
{
    float angle = degrees * 0.01745329252;
    float3 axis = normalize(float3(1.0, 1.0, 1.0));
    return
        (color * cos(angle)) +
        (cross(axis, color) * sin(angle)) +
        (axis *
            dot(axis, color) *
            (1.0 - cos(angle)));
}

float4 CatalogColor(
    float4 source,
    int filterId)
{
    float3 straight =
        saturate(Unpremultiply(source));
    if (filterId == 119)
    {
        float3 inverted = 1.0 - straight;
        straight = lerp(
            straight,
            inverted,
            step(FilterOptions0.x, straight));
    }
    else if (filterId == 132)
    {
        straight += FilterOptions0.x;
        straight =
            (straight - 0.5) *
            FilterOptions2.x +
            0.5;
        straight *= exp2(FilterOptions3.x);
        float luminance = dot(
            straight,
            float3(0.2126, 0.7152, 0.0722));
        straight = lerp(
            luminance,
            straight,
            FilterOptions6.x);
        straight = CatalogRotateHue(
            straight,
            FilterOptions4.x);
        straight += float3(
            FilterOptions7.x,
            0.0,
            -FilterOptions7.x);
        straight = lerp(
            straight,
            FilterOptions8.rgb,
            FilterOptions8.a);
    }
    else
    {
        float signature = frac(
            dot(
                FilterOptions1.xy,
                float2(
                    0.00006103515625,
                    0.00390625)));
        straight += float3(
            signature - 0.5,
            0.5 - signature,
            (signature - 0.5) * 0.5) * 0.1;
    }
    return float4(saturate(straight) * source.a, source.a);
}

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

float4 ResamplingFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = ApplyResampling(
        input,
        source,
        profile);
    float3 sourceStraight =
        saturate(Unpremultiply(source));
    float3 filteredStraight =
        saturate(Unpremultiply(filtered));
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        sourceStraight,
        filteredStraight);
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

float4 NeighborhoodFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = source;
    if ((int)(FilterHeader.x + 0.5) == 0)
    {
        filtered = SampleNeighborhoodWhole(
            profile,
            (int)(FilterOptions9.z + 0.5));
    }
    else
    {
        filtered = ApplyNeighborhood(
            input,
            source,
            profile);
    }
    float3 sourceStraight =
        saturate(Unpremultiply(source));
    float3 filteredStraight =
        saturate(Unpremultiply(filtered));
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        sourceStraight,
        filteredStraight);
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

technique CopyComposite
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 CopyCompositePixelShader();
    }
}

technique AdjustmentFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 AdjustmentFilterPixelShader();
    }
}

technique NeighborhoodFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 NeighborhoodFilterPixelShader();
    }
}

technique ResamplingFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ResamplingFilterPixelShader();
    }
}

technique CatalogFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CatalogFilterPixelShader();
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
