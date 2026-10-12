// SDL_GPU Prism kernels: dedicated catalog filters 27-32. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 27: return ReticulationFilterPixelShader(input);
        case 28: return StainedGlassFilterPixelShader(input);
        case 29: return CraquelureFilterPixelShader(input);
        case 30: return TexturizerFilterPixelShader(input);
        case 31: return GrainFilterPixelShader(input);
        case 32: return MosaicTilesFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
