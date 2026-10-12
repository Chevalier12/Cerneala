// SDL_GPU Prism kernels: catalog filters of the Morphology, Quantization, Procedural and Video primitives. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

// Catalog primitives 0 Morphology, 1 Quantization, 2 Procedural and 3 Video.
float4 ProceduralCatalogFilterPixelShader(VertexShaderOutput input)
{
    int primitive = (int)(FilterHeader.z + 0.5);
    if (primitive == 0)
    {
        return CatalogFilterPixelShader(input, 0);
    }
    if (primitive == 1)
    {
        return CatalogFilterPixelShader(input, 1);
    }
    if (primitive == 2)
    {
        return CatalogFilterPixelShader(input, 2);
    }
    return CatalogFilterPixelShader(input, 3);
}

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 9: return ProceduralCatalogFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
