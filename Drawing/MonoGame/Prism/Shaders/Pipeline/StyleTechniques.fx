technique LayerStyle
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 LayerStylePixelShader();
    }
}

technique StyleDilate
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 StyleDilatePixelShader();
    }
}

technique StyleGaussian
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 StyleGaussianPixelShader();
    }
}

technique StrokeDistanceSeed
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 StyleDistanceSeedPixelShader();
    }
}

technique StrokeDistanceFlood
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 StyleDistanceFloodPixelShader();
    }
}

technique BevelHeight
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 BevelHeightPixelShader();
    }
}

technique BevelLighting
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 BevelLightingPixelShader();
    }
}
