// SDL_GPU Prism kernels: dedicated catalog filters 21-26. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 21: return AccentedEdgesFilterPixelShader(input);
        case 22: return GlowingEdgesFilterPixelShader(input);
        case 23: return TraceContourFilterPixelShader(input);
        case 24: return ChromeFilterPixelShader(input);
        case 25: return NotePaperFilterPixelShader(input);
        case 26: return PhotocopyFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
