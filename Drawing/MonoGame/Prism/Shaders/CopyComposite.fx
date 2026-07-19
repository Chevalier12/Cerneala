Texture2D SpriteTexture;
Texture2D SecondaryTexture;
float Opacity;
float2 PixelSize;
float2 UvScale;
float2 UvOffset;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

sampler2D SecondaryTextureSampler = sampler_state
{
    Texture = <SecondaryTexture>;
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

float4 NormalCompositePixelShader(VertexShaderOutput input) : COLOR0
{
    float4 foreground = SampleSource(input);
    float4 background = SampleSecondary(input);
    return (foreground + (background * (1.0 - foreground.a))) * input.Color;
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

float3 SrgbToLinear(float3 value)
{
    float3 low = value / 12.92;
    float3 high = pow(max((value + 0.055) / 1.055, 0.0), 2.4);
    return lerp(high, low, step(value, 0.04045));
}

float3 LinearToSrgb(float3 value)
{
    float3 low = value * 12.92;
    float3 high = (1.055 * pow(max(value, 0.0), 1.0 / 2.4)) - 0.055;
    return lerp(high, low, step(value, 0.0031308));
}

float4 SrgbToLinearPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = source.a > 0.00001
        ? source.rgb / source.a
        : 0.0;
    return float4(SrgbToLinear(straight) * source.a, source.a)
        * input.Color
        * Opacity;
}

float4 LinearToSrgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = source.a > 0.00001
        ? source.rgb / source.a
        : 0.0;
    return float4(LinearToSrgb(straight) * source.a, source.a)
        * input.Color
        * Opacity;
}

technique CopyComposite
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 CopyCompositePixelShader();
    }
}

technique NormalComposite
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 NormalCompositePixelShader();
    }
}

technique MaskAlpha
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 MaskAlphaPixelShader();
    }
}

technique ClipAlpha
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 ClipAlphaPixelShader();
    }
}

technique SrgbToLinear
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 SrgbToLinearPixelShader();
    }
}

technique LinearToSrgb
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 LinearToSrgbPixelShader();
    }
}

technique Present
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 CopyCompositePixelShader();
    }
}
