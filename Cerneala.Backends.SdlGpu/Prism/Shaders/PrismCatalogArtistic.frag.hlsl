// SDL_GPU Prism kernels: catalog filters of the Artistic primitive. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 9: return CatalogFilterPixelShader(input, 4);
        default: return CopyCompositePixelShader(input);
    }
}
