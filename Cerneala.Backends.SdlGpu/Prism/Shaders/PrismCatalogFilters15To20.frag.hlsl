// SDL_GPU Prism kernels: dedicated catalog filters 15-20. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 15: return SumiEFilterPixelShader(input);
        case 16: return ChalkCharcoalFilterPixelShader(input);
        case 17: return ColoredPencilFilterPixelShader(input);
        case 18: return FrescoFilterPixelShader(input);
        case 19: return CutoutFilterPixelShader(input);
        case 20: return PosterEdgesFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
