// SDL_GPU Prism kernels: dedicated catalog filters 33-39. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 33: return PatchworkFilterPixelShader(input);
        case 34: return WaveNoiseFilterPixelShader(input);
        case 35: return SpatterFilterPixelShader(input);
        case 36: return SprayedStrokesFilterPixelShader(input);
        case 37: return ColorHalftoneFilterPixelShader(input);
        case 38: return FacetFilterPixelShader(input);
        case 39: return LightingEffectsFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
