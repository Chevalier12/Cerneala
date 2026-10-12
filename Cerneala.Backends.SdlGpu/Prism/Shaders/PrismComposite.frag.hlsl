// SDL_GPU Prism kernels: composition, backdrop, adjustment, mask, clip, blend and color conversion. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 0: return CopyCompositePixelShader(input);
        case 1: return BackdropCropPixelShader(input);
        case 2: return BackdropColorConversionPixelShader(input);
        case 3: return AdjustmentFilterPixelShader(input);
        case 4: return LevelsCdfPixelShader(input);
        case 5: return LevelsRangePixelShader(input);
        case 6: return ThresholdRangePixelShader(input);
        case 40: return MaskAlphaPixelShader(input);
        case 41: return MaskExtractPixelShader(input);
        case 42: return MaskFeatherPixelShader(input);
        case 43: return ClipAlphaPixelShader(input);
        case 44: return NormalBlendPixelShader(input);
        case 45: return DissolveBlendPixelShader(input);
        case 46: return DarkenBlendPixelShader(input);
        case 47: return MultiplyBlendPixelShader(input);
        case 48: return ColorBurnBlendPixelShader(input);
        case 49: return LinearBurnBlendPixelShader(input);
        case 50: return DarkerColorBlendPixelShader(input);
        case 51: return LightenBlendPixelShader(input);
        case 52: return ScreenBlendPixelShader(input);
        case 53: return ColorDodgeBlendPixelShader(input);
        case 54: return LinearDodgeBlendPixelShader(input);
        case 55: return LighterColorBlendPixelShader(input);
        case 56: return OverlayBlendPixelShader(input);
        case 57: return SoftLightBlendPixelShader(input);
        case 58: return HardLightBlendPixelShader(input);
        case 59: return VividLightBlendPixelShader(input);
        case 60: return LinearLightBlendPixelShader(input);
        case 61: return PinLightBlendPixelShader(input);
        case 62: return HardMixBlendPixelShader(input);
        case 63: return DifferenceBlendPixelShader(input);
        case 64: return ExclusionBlendPixelShader(input);
        case 65: return SubtractBlendPixelShader(input);
        case 66: return DivideBlendPixelShader(input);
        case 67: return HueBlendPixelShader(input);
        case 68: return SaturationBlendPixelShader(input);
        case 69: return ColorBlendPixelShader(input);
        case 70: return LuminosityBlendPixelShader(input);
        case 71: return PassThroughBlendPixelShader(input);
        case 72: return InputToLinearSrgbPixelShader(input);
        case 73: return InputToSrgbPixelShader(input);
        case 74: return InputToLinearDisplayP3PixelShader(input);
        case 75: return InputToDisplayP3PixelShader(input);
        case 76: return InputToScRgbPixelShader(input);
        case 77: return LinearSrgbToOutputPixelShader(input);
        case 78: return SrgbToOutputPixelShader(input);
        case 79: return LinearDisplayP3ToOutputPixelShader(input);
        case 80: return DisplayP3ToOutputPixelShader(input);
        case 81: return ScRgbToOutputPixelShader(input);
        case 98: return StyleMaskBoxPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
