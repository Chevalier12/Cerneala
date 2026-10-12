// SDL_GPU Prism kernels: resampling filters. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 8: return ResamplingFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
