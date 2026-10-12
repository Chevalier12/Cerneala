// SDL_GPU Prism kernels: neighborhood filters. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 7: return NeighborhoodFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
