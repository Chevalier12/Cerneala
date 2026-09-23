using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.SdlGpu;

public sealed class RenderSurface3DBackendRouteTests
{
    [Fact]
    public void Surface_route_uses_an_isolated_depth_target_and_restores_parent_for_later_2d()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-3d-route", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 8, 8), Color.Red));
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(), new DrawRect(4, 3, 32, 24), Color.White, 1));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(48, 0, 8, 8), Color.Blue));

        session.BeginFrame(Color.Transparent);
        DrawingFrameContext frame = new(
            new Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer().Analyze(commands));
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);

        nint surfaceColor = Assert.Single(api.GpuTextures.Where(texture =>
            texture.Value.CreateInfo.Width == 32 &&
            texture.Value.CreateInfo.Usage == SdlGpuTextureUsage.ColorTarget)).Key;
        nint surfaceSample = Assert.Single(api.GpuTextures.Where(texture =>
            texture.Value.CreateInfo.Width == 32 &&
            texture.Value.CreateInfo.Usage.HasFlag(SdlGpuTextureUsage.Sampler))).Key;
        nint parentColor = api.RenderTargets[0].Texture;
        string[] actions = api.GpuActions.ToArray();
        int surfaceBegin = Array.FindIndex(actions, action =>
            action.StartsWith("begin-render:", StringComparison.Ordinal) &&
            action.EndsWith($":{surfaceColor}", StringComparison.Ordinal));
        Assert.True(surfaceBegin > 0);
        int first2dDraw = Array.FindIndex(actions, action =>
            action.StartsWith("draw-indexed:", StringComparison.Ordinal));
        Assert.InRange(first2dDraw, 0, surfaceBegin - 1);
        nint depthPipeline = Assert.Single(api.GpuPipelines.Where(pipeline =>
            pipeline.Value.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual)).Key;
        int bind3d = Array.FindIndex(actions, surfaceBegin, action =>
            action.StartsWith("bind-pipeline:", StringComparison.Ordinal) &&
            action.EndsWith($":{depthPipeline}", StringComparison.Ordinal));
        Assert.True(bind3d > surfaceBegin);
        string surfacePass = actions[bind3d].Split(':')[1];
        int threeDDraw = Array.FindIndex(actions, bind3d + 1, action =>
            action.StartsWith($"draw-indexed:{surfacePass}:", StringComparison.Ordinal));
        Assert.True(threeDDraw > bind3d);
        Assert.Contains(actions[bind3d..threeDDraw], action =>
            action.StartsWith($"push-uniform:", StringComparison.Ordinal) &&
            action.EndsWith(":208", StringComparison.Ordinal));
        int compositeBind = Array.FindIndex(actions, threeDDraw + 1, action =>
            action.StartsWith("bind-sampler:", StringComparison.Ordinal) &&
            action.Contains($":0:{surfaceSample}:", StringComparison.Ordinal));
        Assert.True(compositeBind > threeDDraw);
        string parentPass = actions[compositeBind].Split(':')[1];
        int restoredParent = Array.FindIndex(actions, threeDDraw + 1, action =>
            action.StartsWith("begin-render:", StringComparison.Ordinal) &&
            action.Contains($":{parentPass}:{parentColor}", StringComparison.Ordinal));
        Assert.InRange(restoredParent, threeDDraw + 1, compositeBind - 1);
        int restoredOrdinal = actions.Take(restoredParent + 1).Count(action =>
            action.StartsWith("begin-render:", StringComparison.Ordinal)) - 1;
        Assert.Equal(SdlGpuLoadOp.Load, api.RenderTargets[restoredOrdinal].LoadOp);
        int compositeDraw = Array.FindIndex(actions, compositeBind + 1, action =>
            action.StartsWith($"draw-indexed:{parentPass}:", StringComparison.Ordinal));
        Assert.True(compositeDraw > compositeBind);
        int following2dBind = Array.FindIndex(actions, compositeDraw + 1, action =>
            action.StartsWith($"bind-sampler:{parentPass}:0:", StringComparison.Ordinal) &&
            !action.StartsWith($"bind-sampler:{parentPass}:0:{surfaceSample}:", StringComparison.Ordinal));
        Assert.True(following2dBind > compositeDraw);
        int following2dDraw = Array.FindIndex(actions, following2dBind + 1, action =>
            action.StartsWith($"draw-indexed:{parentPass}:", StringComparison.Ordinal));
        Assert.True(following2dDraw > following2dBind,
            "The 2D texture binding was not restored after the 3D upload/pass.");

        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual);
        Assert.Contains(api.RenderTargets, target =>
            api.GpuTextures[target.Texture].CreateInfo.Width == 32 &&
            api.GpuTextures[target.Texture].CreateInfo.Height == 24);
        Assert.True(api.GpuActions.Count(action => action.StartsWith("draw-indexed:", StringComparison.Ordinal)) >= 4);
        Assert.Contains(api.RenderTargets, target => target.LoadOp == SdlGpuLoadOp.Load);
        Assert.Contains(api.DepthStencilTargets, target => target.DepthLoadOp == SdlGpuLoadOp.Clear);
    }

    [Fact]
    public void Surface_vertex_layout_and_asymmetric_uniform_bytes_match_the_shader_contract()
    {
        Assert.Equal(56, Marshal.SizeOf<SdlGpuDrawingBackend.Surface3DVertex>());
        Assert.Equal(208, Marshal.SizeOf<SdlGpuDrawingBackend.Surface3DUniforms>());
        SdlGpuVertexInputDescription layout = SdlGpuRenderSurface3DDeviceResources.VertexInput;
        Assert.Equal((uint)56, layout.Stride);
        Assert.Equal(new uint[] { 0, 12, 24, 40, 48 },
            layout.Attributes.Select(attribute => attribute.Offset));

        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureVertexUniformWrites = true };
        nint window = api.CreateWindow("surface-3d-uniform", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        Matrix4x4 model = Matrix4x4.CreateTranslation(2, 3, 4);
        Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(3, 2, 7), new Vector3(.5f, -.25f, 0), Vector3.UnitY);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3, 4f / 3, .01f, 100);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(model, view, projection), new DrawRect(4, 3, 32, 24), Color.White, 1));

        Render(session, commands);

        byte[] bytes = Assert.Single(api.VertexUniformWrites.Where(write => write.Length == 208));
        SdlGpuDrawingBackend.Surface3DUniforms uploaded =
            MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(bytes);
        Assert.Equal(model, uploaded.Model);
        Assert.Equal(view, uploaded.View);
        Assert.Equal(projection, uploaded.Projection);
        Assert.Equal(new Vector4(32, 24, 4, 0), uploaded.Viewport);
    }

    [Fact]
    public void Surface_sample_count_requires_both_color_and_depth_support()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureVertexUniformWrites = true };
        api.UnsupportedFormatSampleCounts.Add((SdlGpuTextureFormat.D24UnormS8Uint, SdlGpuSampleCount.Four));
        nint window = api.CreateWindow("surface-3d-samples", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(), new DrawRect(4, 3, 32, 24), Color.White, 1));

        Render(session, commands);

        Assert.Contains(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 32 &&
            texture.CreateInfo.Format == SdlGpuTextureFormat.D24UnormS8Uint &&
            texture.CreateInfo.SampleCount == SdlGpuSampleCount.Two);
        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual &&
            pipeline.SampleCount == SdlGpuSampleCount.Two);
        SdlGpuDrawingBackend.Surface3DUniforms uploaded =
            MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(
                Assert.Single(api.VertexUniformWrites.Where(write => write.Length == 208)));
        Assert.Equal(2, uploaded.Viewport.Z);
    }

    [Fact]
    public void Surface_reports_when_no_common_color_depth_sample_count_exists()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        foreach (SdlGpuSampleCount count in Enum.GetValues<SdlGpuSampleCount>())
            api.UnsupportedFormatSampleCounts.Add((SdlGpuTextureFormat.D24UnormS8Uint, count));
        nint window = api.CreateWindow("surface-3d-no-samples", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(), new DrawRect(4, 3, 32, 24), Color.White, 1));
        DrawingFrameContext frame = new(
            new Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer().Analyze(commands));

        session.BeginFrame(Color.Transparent);
        try
        {
            NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
                session.DrawingBackend.Render(commands, in frame));
            Assert.Contains("at least two common color/depth samples", exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Surface_rejects_one_x_only_common_color_depth_samples_without_rejecting_parent_2d(int limitation)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        if (limitation == 0)
        {
            api.SupportedSampleCounts.Clear();
            api.SupportedSampleCounts.Add(SdlGpuSampleCount.One);
        }
        else
        {
            foreach (SdlGpuSampleCount count in new[] { SdlGpuSampleCount.Two, SdlGpuSampleCount.Four })
            {
                if (limitation == 1)
                {
                    api.UnsupportedFormatSampleCounts.Add((SdlGpuTextureFormat.D24UnormS8Uint, count));
                }
                else
                {
                    api.UnsupportedFormatSampleCounts.Add((SdlGpuTextureFormat.R8G8B8A8Unorm, count));
                    api.UnsupportedFormatSampleCounts.Add((SdlGpuTextureFormat.B8G8R8A8Unorm, count));
                }
            }
        }
        nint window = api.CreateWindow($"surface-3d-only-one-{limitation}", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 8, 8), Color.Red));
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(), new DrawRect(4, 3, 32, 24), Color.White, 1));
        DrawingFrameContext frame = new(
            new Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer().Analyze(commands));

        session.BeginFrame(Color.Transparent);
        try
        {
            NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
                session.DrawingBackend.Render(commands, in frame));
            Assert.Contains("at least two common color/depth samples", exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
        Assert.Single(api.GpuActions.Where(action =>
            action.StartsWith("draw-indexed:", StringComparison.Ordinal)));
        Assert.DoesNotContain(api.GpuTextures.Values, texture => texture.CreateInfo.Width == 32);
        Assert.DoesNotContain(api.GpuPipelines.Values, pipeline => pipeline.DepthState.TestEnabled);
    }

    [Fact]
    public void Two_dimensional_surface_keeps_its_one_x_fallback_on_the_same_limited_device()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedSampleCounts.Clear();
        api.SupportedSampleCounts.Add(SdlGpuSampleCount.One);
        nint window = api.CreateWindow("surface-2d-one-sample", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
            frame.FillRectangle(new DrawRect(0, 0, 32, 24), Color.Blue);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface, new DrawRect(4, 3, 32, 24), Color.White));

        Render(session, commands);

        Assert.Contains(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 32 &&
            texture.CreateInfo.SampleCount == SdlGpuSampleCount.One);
        Assert.True(api.GpuActions.Count(action =>
            action.StartsWith("draw-indexed:", StringComparison.Ordinal)) >= 2);
    }

    [Fact]
    public void Surface_uniform_uses_eight_when_both_fake_attachment_formats_support_eight_samples()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureVertexUniformWrites = true };
        api.SupportedSampleCounts.Add(SdlGpuSampleCount.Eight);
        nint window = api.CreateWindow("surface-3d-eight-samples", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            new TestSource(), new DrawRect(4, 3, 32, 24), Color.White, 1));

        Render(session, commands);

        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual &&
            pipeline.SampleCount == SdlGpuSampleCount.Eight);
        SdlGpuDrawingBackend.Surface3DUniforms uploaded =
            MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(
                Assert.Single(api.VertexUniformWrites.Where(write => write.Length == 208)));
        Assert.Equal(8, uploaded.Viewport.Z);
    }

    [Fact]
    public void Device_3d_shaders_and_pipelines_are_lazy_keyed_by_format_and_samples_and_released()
    {
        FakeSdlApi api = new();
        using (SdlGpuDrawingResources resources = new(api, api.DeviceResult, SdlGpuShaderFormats.Dxil))
        {
            Assert.Empty(api.GpuShaders);
            Assert.Empty(api.GpuPipelines);
            SdlGpuRenderSurface3DDeviceResources threeD = resources.Surface3DResources;
            Assert.Empty(api.GpuShaders);
            nint first = threeD.GetPipeline(SdlGpuTextureFormat.R8G8B8A8Unorm,
                SdlGpuTextureFormat.D24UnormS8Uint, SdlGpuSampleCount.One);
            Assert.Equal(2, api.GpuShaders.Count);
            Assert.Equal(first, threeD.GetPipeline(SdlGpuTextureFormat.R8G8B8A8Unorm,
                SdlGpuTextureFormat.D24UnormS8Uint, SdlGpuSampleCount.One));
            nint otherFormat = threeD.GetPipeline(SdlGpuTextureFormat.B8G8R8A8Unorm,
                SdlGpuTextureFormat.D24UnormS8Uint, SdlGpuSampleCount.One);
            nint otherSamples = threeD.GetPipeline(SdlGpuTextureFormat.R8G8B8A8Unorm,
                SdlGpuTextureFormat.D24UnormS8Uint, SdlGpuSampleCount.Two);
            Assert.NotEqual(first, otherFormat);
            Assert.NotEqual(first, otherSamples);
            Assert.Equal(3, api.GpuPipelines.Count);
        }
        Assert.Empty(api.GpuPipelines);
        Assert.Empty(api.GpuShaders);
    }

    [Fact]
    public void Generated_shader_reflection_matches_the_native_3d_vertex_descriptor()
    {
        string metadataPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../Cerneala.Backends.SdlGpu/Shaders/artifacts.json"));
        using JsonDocument metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
        JsonElement vertex = metadata.RootElement.GetProperty("shaders").EnumerateArray()
            .Single(shader => shader.GetProperty("logicalName").GetString() == "surface-3d-vertex");
        JsonElement fragment = metadata.RootElement.GetProperty("shaders").EnumerateArray()
            .Single(shader => shader.GetProperty("logicalName").GetString() == "surface-3d-fragment");
        JsonElement[] attributes = vertex.GetProperty("layout").GetProperty("vertexInputs")
            .EnumerateArray().ToArray();
        SdlGpuVertexInputDescription descriptor = SdlGpuRenderSurface3DDeviceResources.VertexInput;
        Assert.Equal(descriptor.Attributes.Count, attributes.Length);
        for (int index = 0; index < attributes.Length; index++)
        {
            Assert.Equal((uint)index, attributes[index].GetProperty("location").GetUInt32());
            Assert.Equal($"TEXCOORD{index}", attributes[index].GetProperty("semantic").GetString());
            Assert.Equal(descriptor.Attributes[index].Format.ToString(),
                attributes[index].GetProperty("format").GetString());
        }
        Assert.Equal("Surface3DUniforms", vertex.GetProperty("layout")
            .GetProperty("uniformBuffers")[0].GetProperty("name").GetString());
        Assert.Equal(1, vertex.GetProperty("bindings").GetProperty("uniformBuffers").GetInt32());
        Assert.Equal(0, fragment.GetProperty("bindings").GetProperty("uniformBuffers").GetInt32());
    }

    [Fact]
    public void A_window_without_3d_never_creates_3d_shaders_or_depth_pipelines()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("no-3d", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 8, 8), Color.Red));

        Render(session, commands);

        Assert.Equal(2, api.GpuShaders.Count);
        Assert.DoesNotContain(api.GpuPipelines.Values, pipeline => pipeline.DepthState.TestEnabled);
    }

    [Fact]
    public void Zero_generation_still_records_and_renders_the_first_frame()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-3d-zero", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        TestSource source = new(generation: 0);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            source, new DrawRect(4, 3, 32, 24), Color.White, 1));

        Render(session, commands);

        Assert.Equal(1, source.RecordCount);
        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.DepthState == SdlGpuDepthState.ReadWriteLessOrEqual);
    }

    [Fact]
    public void Callback_invalidation_keeps_the_recorded_camera_snapshot_and_requests_the_next_frame()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureVertexUniformWrites = true };
        nint window = api.CreateWindow("surface-3d-callback-invalidation", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                80, 60, coordinateScale: 1));
        RenderSurface3D surface = new()
        {
            ViewMatrix = Matrix4x4.Identity,
            Projection = RenderProjection3D.Perspective(MathF.PI / 3, .01f, 100)
        };
        int callbacks = 0;
        Matrix4x4 nextView = Matrix4x4.CreateTranslation(0, 0, -1);
        surface.Draw += (_, frame) =>
        {
            callbacks++;
            frame.DrawMarker(new Vector3(0, 0, -2), Color.Red, 6);
            if (callbacks == 1)
            {
                surface.InvalidateFrame();
                surface.ViewMatrix = nextView;
            }
        };
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface3DCommand(
            surface, new DrawRect(4, 3, 32, 24), Color.White, 1));

        Render(session, commands);
        Assert.Equal(1, callbacks);
        SdlGpuDrawingBackend.Surface3DUniforms first = MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(
            Assert.Single(api.VertexUniformWrites.Where(write => write.Length == 208)));
        Assert.Equal(Matrix4x4.Identity, first.View);

        api.VertexUniformWrites.Clear();
        Render(session, commands);
        Assert.Equal(2, callbacks);
        SdlGpuDrawingBackend.Surface3DUniforms second =
            MemoryMarshal.Read<SdlGpuDrawingBackend.Surface3DUniforms>(
                Assert.Single(api.VertexUniformWrites.Where(write => write.Length == 208)));
        Assert.Equal(nextView, second.View);
    }

    private static void Render(SdlGpuWindowGraphicsSession session, DrawCommandList commands)
    {
        DrawingFrameContext frame = new(
            new Cerneala.Drawing.Prism.Graph.PrismFrameAnalyzer().Analyze(commands));
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);
    }

    private sealed class TestSource : IRenderSurface3DSource
    {
        private readonly Matrix4x4 model;
        private readonly Matrix4x4 view;
        private readonly Matrix4x4 projection;
        private readonly long generation;

        public TestSource(Matrix4x4? model = null, Matrix4x4? view = null, Matrix4x4? projection = null,
            long generation = 1)
        {
            this.model = model ?? Matrix4x4.Identity;
            this.view = view ?? Matrix4x4.Identity;
            this.projection = projection ?? Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3, 4f / 3, .01f, 100);
            this.generation = generation;
        }

        public long FrameVersion => generation;

        public bool HasDrawSubscribers => true;

        public long ResourceEpoch => 0;

        public int RecordCount { get; private set; }

        public void SetBackendState(object owner, IRenderSurface3DBackendState? state) { }

        public RenderSurface3DRecording RecordFrame(DrawRect bounds, float rasterScale)
        {
            RecordCount++;
            return new(generation, Color.Transparent, bounds, (int)bounds.Width, (int)bounds.Height,
                rasterScale, TimeSpan.Zero, view, projection,
                [new DrawPrimitive3D(DrawPrimitive3DKind.Marker,
                    new Vector3(0, 0, -2), default, Color.White, 6, model)]);
        }
    }
}
