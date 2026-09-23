using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

// Immutable device objects only. Camera, geometry and render targets belong to a session.
internal sealed class SdlGpuRenderSurface3DDeviceResources : IDisposable
{
    internal static readonly SdlGpuVertexInputDescription VertexInput = new(56,
        new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float3, 0),
        new SdlGpuVertexAttributeDescription(1, SdlGpuVertexFormat.Float3, 12),
        new SdlGpuVertexAttributeDescription(2, SdlGpuVertexFormat.Float4, 24),
        new SdlGpuVertexAttributeDescription(3, SdlGpuVertexFormat.Float2, 40),
        new SdlGpuVertexAttributeDescription(4, SdlGpuVertexFormat.Float2, 48));

    private static readonly SdlGpuBlendState CoverageBlend = new(
        SdlGpuBlendFactor.One, SdlGpuBlendFactor.OneMinusSourceAlpha, SdlGpuBlendOperation.Add,
        SdlGpuBlendFactor.One, SdlGpuBlendFactor.OneMinusSourceAlpha, SdlGpuBlendOperation.Add);

    private readonly ISdlApi api;
    private readonly nint device;
    private readonly SdlGpuShaderFormats formats;
    private readonly Dictionary<PipelineKey, nint> pipelines = [];
    private nint vertexShader;
    private nint fragmentShader;
    private bool disposed;

    internal SdlGpuRenderSurface3DDeviceResources(ISdlApi api, nint device, SdlGpuShaderFormats formats)
    {
        this.api = api;
        this.device = device;
        this.formats = formats;
    }

    internal nint GetPipeline(SdlGpuTextureFormat colorFormat, SdlGpuTextureFormat depthFormat,
        SdlGpuSampleCount sampleCount)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        PipelineKey key = new(colorFormat, depthFormat, sampleCount,
            SdlGpuPrimitiveType.TriangleList, CoverageBlend, SdlGpuStencilMode.Disabled,
            SdlGpuDepthState.ReadWriteLessOrEqual, VertexInput.Stride,
            SdlGpuShaderArtifacts.Surface3DVertex.LogicalName,
            SdlGpuShaderArtifacts.Surface3DFragment.LogicalName);
        if (pipelines.TryGetValue(key, out nint existing)) return existing;
        EnsureShaders();
        SdlGpuGraphicsPipelineCreateInfo createInfo = new(
            vertexShader, fragmentShader, colorFormat, depthFormat, sampleCount,
            key.Topology, key.Blend, key.Stencil, VertexInput, key.Depth,
            SdlGpuColorWriteMask.All);
        nint pipeline = api.CreateGpuGraphicsPipeline(device, createInfo);
        if (pipeline == 0) throw SdlApiError.Create(api, $"SDL GPU 3D pipeline creation ({key})");
        pipelines.Add(key, pipeline);
        return pipeline;
    }

    private void EnsureShaders()
    {
        if (vertexShader != 0) return;
        vertexShader = SdlGpuShaderArtifacts.CreateShader(api, device, formats,
            SdlGpuShaderArtifacts.Surface3DVertex);
        try
        {
            fragmentShader = SdlGpuShaderArtifacts.CreateShader(api, device, formats,
                SdlGpuShaderArtifacts.Surface3DFragment);
        }
        catch
        {
            api.ReleaseGpuShader(device, vertexShader);
            vertexShader = 0;
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (nint pipeline in pipelines.Values) api.ReleaseGpuGraphicsPipeline(device, pipeline);
        pipelines.Clear();
        if (fragmentShader != 0) api.ReleaseGpuShader(device, fragmentShader);
        if (vertexShader != 0) api.ReleaseGpuShader(device, vertexShader);
        fragmentShader = vertexShader = 0;
    }

    private readonly record struct PipelineKey(
        SdlGpuTextureFormat Color, SdlGpuTextureFormat DepthFormat, SdlGpuSampleCount Samples,
        SdlGpuPrimitiveType Topology, SdlGpuBlendState Blend, SdlGpuStencilMode Stencil,
        SdlGpuDepthState Depth, uint VertexStride, string VertexShader, string FragmentShader);
}
