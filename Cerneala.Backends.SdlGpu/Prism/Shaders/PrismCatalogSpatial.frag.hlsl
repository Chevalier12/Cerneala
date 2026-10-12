// SDL_GPU Prism kernels: catalog filters of the EdgeDetection, Tiling, Texture, Convolution, Extrude and Color primitives. See PrismKernels.hlsl.
#include "PrismKernels.hlsl"

// Catalog primitives 5 EdgeDetection, 6 Tiling, 7 Texture, 8 Convolution and 10 Extrude.
// Every other primitive takes the Color branch, as in ApplyCatalogFilter.
float4 SpatialCatalogFilterPixelShader(VertexShaderOutput input)
{
    int primitive = (int)(FilterHeader.z + 0.5);
    if (primitive == 5)
    {
        return CatalogFilterPixelShader(input, 5);
    }
    if (primitive == 6)
    {
        return CatalogFilterPixelShader(input, 6);
    }
    if (primitive == 7)
    {
        return CatalogFilterPixelShader(input, 7);
    }
    if (primitive == 8)
    {
        return CatalogFilterPixelShader(input, 8);
    }
    if (primitive == 10)
    {
        return CatalogFilterPixelShader(input, 10);
    }
    return CatalogFilterPixelShader(input, 9);
}

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 9: return SpatialCatalogFilterPixelShader(input);
        default: return CopyCompositePixelShader(input);
    }
}
