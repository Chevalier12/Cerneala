using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuShaderArtifactTests
{
    [Fact]
    public void Presentation_profiles_do_not_merge_with_each_other_or_ordinary_drawing()
    {
        CerberusBatchKey ordinary = new(
            DrawPrimitiveTopology.TriangleList, 42,
            DrawSamplingMode.Linear, DrawAddressMode.Clamp, DrawBlendMode.Normal,
            SdlGpuStencilMode.Disabled, 0, new SdlRect(0, 0, 16, 16),
            SdlGpuColorWriteMask.All);
        Assert.True(ordinary.CanMerge(ordinary));
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        {
            CerberusBatchKey presentation = ordinary with { PrismWorkingColorProfile = profile };
            Assert.False(ordinary.CanMerge(presentation));
            Assert.False(presentation.CanMerge(ordinary));
            foreach (PrismColorProfile other in Enum.GetValues<PrismColorProfile>())
            {
                Assert.Equal(profile == other, presentation.CanMerge(
                    ordinary with { PrismWorkingColorProfile = other }));
            }
        }
    }

    [Fact]
    public void Presentation_and_ordinary_pipelines_are_separately_cached_and_owned()
    {
        FakeSdlApi api = new();
        using SdlGpuDrawingResources resources = new(api, api.DeviceResult, SdlGpuShaderFormats.Dxil);
        nint Pipeline(bool presentation) => resources.GetPipeline(
            SdlGpuTextureFormat.R8G8B8A8Unorm, SdlGpuSampleCount.One,
            DrawPrimitiveTopology.TriangleList, DrawBlendMode.Normal,
            SdlGpuStencilMode.Disabled, prismPresentation: presentation);
        nint ordinary = Pipeline(false);
        nint converted = Pipeline(true);
        Assert.NotEqual(ordinary, converted);
        Assert.Equal(ordinary, Pipeline(false));
        Assert.Equal(converted, Pipeline(true));
        Assert.Equal(2, resources.PipelineCount);
        Assert.Equal(3, api.GpuShaders.Count);
    }

    [Theory]
    [InlineData((uint)SdlGpuShaderFormats.Dxil, "main")]
    [InlineData((uint)SdlGpuShaderFormats.SpirV, "main")]
    [InlineData((uint)SdlGpuShaderFormats.Msl, "main0")]
    public void Every_manifest_shader_loads_for_each_embedded_format(
        uint formatValue,
        string expectedEntryPoint)
    {
        SdlGpuShaderFormats format = (SdlGpuShaderFormats)formatValue;
        FakeSdlApi api = new();
        List<nint> handles = [];
        try
        {
            foreach (SdlGpuShaderArtifact artifact in SdlGpuShaderArtifacts.All)
            {
                handles.Add(SdlGpuShaderArtifacts.CreateShader(
                    api,
                    api.DeviceResult,
                    format,
                    artifact));
            }

            Assert.Equal(SdlGpuShaderArtifacts.All.Count, api.GpuShaders.Count);
            foreach ((nint _, SdlGpuShaderCreateInfo shader) in api.GpuShaders)
            {
                Assert.Equal(format, shader.Format);
                Assert.Equal(expectedEntryPoint, shader.EntryPoint);
                Assert.True(shader.Code.Length > 32,
                    $"Shader '{shader.Stage}' did not contain compiled {format} code.");
                if (format == SdlGpuShaderFormats.Msl)
                {
                    string source = System.Text.Encoding.UTF8.GetString(shader.Code.Span);
                    Assert.Equal(source.TrimEnd('\r', '\n') + "\n", source);
                }
            }
        }
        finally
        {
            foreach (nint handle in handles)
            {
                api.ReleaseGpuShader(api.DeviceResult, handle);
            }
        }
    }

    [Fact]
    public void Format_selection_is_deterministic_and_fail_closed()
    {
        SdlGpuShaderFormatSelection selected = SdlGpuShaderArtifacts.SelectFormat(
            SdlGpuShaderFormats.SpirV |
            SdlGpuShaderFormats.Dxil |
            SdlGpuShaderFormats.Msl);

        Assert.Equal(SdlGpuShaderFormats.Dxil, selected.Format);
        Assert.Throws<NotSupportedException>(
            () => SdlGpuShaderArtifacts.SelectFormat(
                SdlGpuShaderFormats.None |
                SdlGpuShaderFormats.Dxbc |
                SdlGpuShaderFormats.MetalLib));
    }

    [SdlNativeFact]
    public void Native_device_creates_drawing_and_prism_pipelines_from_offline_artifacts()
    {
        NativeSdlApi api = new();
        Assert.True(api.InitializeVideo(), api.GetError());
        nint device = 0;
        List<nint> shaders = [];
        List<nint> pipelines = [];
        try
        {
            device = api.CreateGpuDevice(
                SdlGpuDeviceOwner.RequestedShaderFormats,
                debugMode: true,
                preferredDriver: null);
            Assert.NotEqual(0, device);
            SdlGpuShaderFormats formats = api.GetGpuShaderFormats(device);
            Dictionary<string, nint> handles = [];
            foreach (SdlGpuShaderArtifact artifact in SdlGpuShaderArtifacts.All)
            {
                nint shader = SdlGpuShaderArtifacts.CreateShader(
                    api,
                    device,
                    formats,
                    artifact);
                shaders.Add(shader);
                handles.Add(artifact.LogicalName, shader);
            }

            pipelines.Add(CreatePipeline(
                api,
                device,
                handles["drawing-vertex"],
                handles["drawing-fragment"]));
            pipelines.Add(CreatePipeline(
                api,
                device,
                handles["drawing-vertex"],
                handles["prism-presentation-fragment"]));
            pipelines.Add(CreatePipeline(
                api,
                device,
                handles["prism-fullscreen-vertex"],
                handles["prism-copy-fragment"]));
            Assert.All(pipelines, static pipeline => Assert.NotEqual(0, pipeline));
        }
        finally
        {
            if (device != 0)
            {
                foreach (nint pipeline in pipelines)
                {
                    if (pipeline != 0)
                    {
                        api.ReleaseGpuGraphicsPipeline(device, pipeline);
                    }
                }
                foreach (nint shader in shaders)
                {
                    api.ReleaseGpuShader(device, shader);
                }
                api.DestroyGpuDevice(device);
            }
            api.Quit();
        }
    }

    private static nint CreatePipeline(
        ISdlApi api,
        nint device,
        nint vertexShader,
        nint fragmentShader) => api.CreateGpuGraphicsPipeline(
            device,
            new SdlGpuGraphicsPipelineCreateInfo(
                vertexShader,
                fragmentShader,
                SdlGpuTextureFormat.R8G8B8A8Unorm,
                SdlGpuTextureFormat.D24UnormS8Uint,
                SdlGpuSampleCount.One,
                SdlGpuPrimitiveType.TriangleList,
                SdlGpuBlendState.Opaque,
                SdlGpuStencilMode.Disabled));
}
