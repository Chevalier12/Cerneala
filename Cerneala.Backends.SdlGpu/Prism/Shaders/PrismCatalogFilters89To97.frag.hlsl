// SDL_GPU Prism kernels: dedicated catalog filters 89-97. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 89: return CharcoalInitialEtfSdl(input);
        case 90: return CharcoalRefineEtfSdl(input);
        case 91: return CharcoalNormalDogSdl(input);
        case 92: return CharcoalFlowDogSdl(input);
        case 93: return CharcoalSdl(input);
        case 94: return ConteCrayonSdl(input);
        case 95: return GraphicPenSdl(input);
        case 96: return PlasterSdl(input);
        case 97: return DeinterlaceSdl(input);
        default: return CopyCompositePixelShader(input);
    }
}
