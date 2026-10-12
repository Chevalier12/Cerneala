// SDL_GPU Prism kernels: dedicated catalog filters 10-14. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 10: return DryBrushFilterPixelShader(input);
        case 11: return UnderpaintingFilterPixelShader(input);
        case 12: return WatercolorFilterPixelShader(input);
        case 13: return WaterPaperFilterPixelShader(input);
        case 14: return WindFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
