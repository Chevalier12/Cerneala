using System.Reflection;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal static class SdlGpuShaderArtifacts
{
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

    public static SdlGpuShaderArtifact PrismCatalogFragment { get; } = new(
        "prism-catalog-fragment",
        "Prism.Shaders",
        "PrismCatalog.frag",
        SdlGpuShaderStage.Fragment,
        SamplerCount: 15,
        StorageTextureCount: 0,
        StorageBufferCount: 0,
        UniformBufferCount: 1);

    public static IReadOnlyList<SdlGpuShaderArtifact> All { get; } =
    [
        DrawingVertex,
        DrawingFragment,
        PrismVertex,
        PrismCopyFragment,
        PrismCatalogFragment
    ];

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
