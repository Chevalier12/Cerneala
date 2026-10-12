namespace Cerneala.Backends.SdlGpu;

/// <summary>
/// The SDL_GPU Prism kernel fragment shaders. Each one holds a subset of the kernel ids,
/// because the D3D12 WARP JIT crashes on one shader holding every kernel (issue #72).
/// The values index <see cref="SdlGpuShaderArtifacts.PrismKernelFragments"/>.
/// </summary>
internal enum SdlGpuPrismFragmentShader
{
    Composite,
    Neighborhood,
    Resampling,
    CatalogArtistic,
    CatalogProcedural,
    CatalogSpatial,
    CatalogFilters10To14,
    CatalogFilters15To20,
    CatalogFilters21To26,
    CatalogFilters27To32,
    CatalogFilters33To39,
    Style,
    CatalogFilters89To97
}
