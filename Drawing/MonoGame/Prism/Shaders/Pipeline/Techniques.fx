technique CopyComposite
{
    pass Pass0
    {
        PixelShader = compile ps_4_0_level_9_1 CopyCompositePixelShader();
    }
}

technique BackdropCrop
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 BackdropCropPixelShader();
    }
}

technique BackdropColorConversion
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 BackdropColorConversionPixelShader();
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
