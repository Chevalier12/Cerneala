using System.Reflection;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal static class SdlGpuShaderArtifacts
{
    public static SdlGpuShaderArtifact Surface3DVertex { get; } = new(
        "surface-3d-vertex", "Gpu.Shaders", "Surface3D.vert",
        SdlGpuShaderStage.Vertex, 0, 0, 0, 1);

    public static SdlGpuShaderArtifact Surface3DFragment { get; } = new(
        "surface-3d-fragment", "Gpu.Shaders", "Surface3D.frag",
        SdlGpuShaderStage.Fragment, 0, 0, 0, 0);

    public static SdlGpuShaderArtifact DrawingVertex { get; } = new(
        "drawing-vertex",
        "Gpu.Shaders",
        "Drawing.vert",
        SdlGpuShaderStage.Vertex,
        SamplerCount: 0,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    public static SdlGpuShaderArtifact DrawingFragment { get; } = new(
        "drawing-fragment",
        "Gpu.Shaders",
        "Drawing.frag",
        SdlGpuShaderStage.Fragment,
        SamplerCount: 1,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 0);

    public static SdlGpuShaderArtifact DrawingImageDomainVertex { get; } = new(
        "drawing-point-clamp-image-domain-vertex",
        "Gpu.Shaders",
        "DrawingImageDomain.vert",
        SdlGpuShaderStage.Vertex,
        SamplerCount: 0,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    public static SdlGpuShaderArtifact DrawingImageDomainFragment { get; } = new(
        "drawing-point-clamp-image-domain-fragment",
        "Gpu.Shaders",
        "DrawingImageDomain.frag",
        SdlGpuShaderStage.Fragment,
        SamplerCount: 1,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 0);

    public static SdlGpuShaderArtifact PrismVertex { get; } = new(
        "prism-fullscreen-vertex",
        "Prism.Shaders",
        "Prism.vert",
        SdlGpuShaderStage.Vertex,
        SamplerCount: 0,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    public static SdlGpuShaderArtifact PrismCopyFragment { get; } = new(
        "prism-copy-fragment",
        "Prism.Shaders",
        "PrismCopy.frag",
        SdlGpuShaderStage.Fragment,
        SamplerCount: 1,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    // Indexed by SdlGpuPrismFragmentShader. Every kernel shader binds the same 15 sampler
    // slots and uniform block, so one Prism draw path serves all of them.
    public static IReadOnlyList<SdlGpuShaderArtifact> PrismKernelFragments { get; } =
    [
        CreatePrismKernelFragment("prism-composite-fragment", "PrismComposite.frag"),
        CreatePrismKernelFragment("prism-neighborhood-fragment", "PrismNeighborhood.frag"),
        CreatePrismKernelFragment("prism-resampling-fragment", "PrismResampling.frag"),
        CreatePrismKernelFragment("prism-catalog-artistic-fragment", "PrismCatalogArtistic.frag"),
        CreatePrismKernelFragment("prism-catalog-procedural-fragment", "PrismCatalogProcedural.frag"),
        CreatePrismKernelFragment("prism-catalog-spatial-fragment", "PrismCatalogSpatial.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-10-14-fragment", "PrismCatalogFilters10To14.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-15-20-fragment", "PrismCatalogFilters15To20.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-21-26-fragment", "PrismCatalogFilters21To26.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-27-32-fragment", "PrismCatalogFilters27To32.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-33-39-fragment", "PrismCatalogFilters33To39.frag"),
        CreatePrismKernelFragment("prism-style-fragment", "PrismStyle.frag"),
        CreatePrismKernelFragment("prism-catalog-filters-89-97-fragment", "PrismCatalogFilters89To97.frag")
    ];

    public static SdlGpuShaderArtifact PrismPresentationFragment { get; } = new(
        "prism-presentation-fragment",
        "Prism.Shaders",
        "PrismPresentation.frag",
        SdlGpuShaderStage.Fragment,
        SamplerCount: 1,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    public static IReadOnlyList<SdlGpuShaderArtifact> All { get; } =
    [
        DrawingVertex,
        DrawingFragment,
        DrawingImageDomainVertex,
        DrawingImageDomainFragment,
        Surface3DVertex,
        Surface3DFragment,
        PrismVertex,
        PrismCopyFragment,
        .. PrismKernelFragments,
        PrismPresentationFragment
    ];

    public static SdlGpuShaderArtifact PrismKernelFragment(SdlGpuPrismFragmentShader shader) =>
        PrismKernelFragments[(int)shader];

    private static SdlGpuShaderArtifact CreatePrismKernelFragment(
        string logicalName,
        string fileStem) => new(
            logicalName,
            "Prism.Shaders",
            fileStem,
            SdlGpuShaderStage.Fragment,
            SamplerCount: 15,
            StorageTextureCount: 0,
            StorageBufferCount: 0,
            UniformBufferCount: 1);

    public static nint CreateShader(
        ISdlApi api,
        nint device,
        SdlGpuShaderFormats supportedFormats,
        SdlGpuShaderArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(api);
        if (device == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(device));
        }

        SdlGpuShaderFormatSelection selection = SelectFormat(supportedFormats);
        byte[] code = Read(artifact, selection.Extension);
        nint shader = api.CreateGpuShader(
            device,
            new SdlGpuShaderCreateInfo(
                selection.Format,
                artifact.Stage,
                code,
                selection.EntryPoint,
                artifact.SamplerCount,
                artifact.UniformBufferCount,
                artifact.StorageTextureCount,
                artifact.StorageBufferCount));
        return shader != 0
            ? shader
            : throw SdlApiError.Create(
                api,
                $"SDL GPU shader creation ({artifact.LogicalName}/{selection.Format})");
    }

    internal static SdlGpuShaderFormatSelection SelectFormat(
        SdlGpuShaderFormats supportedFormats)
    {
        if ((supportedFormats & SdlGpuShaderFormats.Dxil) != 0)
        {
            return new(SdlGpuShaderFormats.Dxil, "dxil", "main");
        }
        if ((supportedFormats & SdlGpuShaderFormats.SpirV) != 0)
        {
            return new(SdlGpuShaderFormats.SpirV, "spv", "main");
        }
        if ((supportedFormats & SdlGpuShaderFormats.Msl) != 0)
        {
            return new(SdlGpuShaderFormats.Msl, "msl", "main0");
        }

        throw new NotSupportedException(
            $"SDL GPU reported no embedded shader format. Supported formats: {supportedFormats}.");
    }

    private static byte[] Read(SdlGpuShaderArtifact artifact, string extension)
    {
        Assembly assembly = typeof(SdlGpuShaderArtifacts).Assembly;
        string resourceName =
            $"Cerneala.Backends.SdlGpu.{artifact.ResourceFolder}.{artifact.FileStem}.{extension}";
        using Stream stream = assembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException(
                $"Embedded SDL GPU shader '{resourceName}' is missing.");
        using MemoryStream output = new();
        stream.CopyTo(output);
        return output.ToArray();
    }
}

internal readonly record struct SdlGpuShaderArtifact(
    string LogicalName,
    string ResourceFolder,
    string FileStem,
    SdlGpuShaderStage Stage,
    uint SamplerCount,
    uint StorageTextureCount,
    uint StorageBufferCount,
    uint UniformBufferCount);

internal readonly record struct SdlGpuShaderFormatSelection(
    SdlGpuShaderFormats Format,
    string Extension,
    string EntryPoint);
