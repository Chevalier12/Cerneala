// SDL_GPU Prism kernels: layer styles. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 82: return LayerStylePixelShader(input);
        case 83: return StyleDilatePixelShader(input);
        case 84: return StyleGaussianPixelShader(input);
        case 85: return StyleDistanceSeedPixelShader(input);
        case 86: return StyleDistanceFloodPixelShader(input);
        case 87: return BevelHeightPixelShader(input);
        case 88: return BevelLightingPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
