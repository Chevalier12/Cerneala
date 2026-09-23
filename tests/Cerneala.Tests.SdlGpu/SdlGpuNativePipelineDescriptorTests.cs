using Cerneala.Platforms.Sdl3;
using SDL3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuNativePipelineDescriptorTests
{
    [Fact]
    public void Pipeline_contract_exposes_explicit_vertex_input_and_depth_state()
    {
        System.Reflection.Assembly assembly = typeof(ISdlApi).Assembly;

        Assert.NotNull(assembly.GetType("Cerneala.Platforms.Sdl3.SdlGpuVertexInputDescription"));
        Assert.NotNull(assembly.GetType("Cerneala.Platforms.Sdl3.SdlGpuVertexAttributeDescription"));
        Assert.NotNull(assembly.GetType("Cerneala.Platforms.Sdl3.SdlGpuVertexFormat"));
        Assert.NotNull(assembly.GetType("Cerneala.Platforms.Sdl3.SdlGpuDepthState"));
        Assert.NotNull(assembly.GetType("Cerneala.Platforms.Sdl3.SdlGpuCompareOperation"));

        Type createInfo = typeof(SdlGpuGraphicsPipelineCreateInfo);
        Assert.NotNull(createInfo.GetProperty("VertexInput"));
        Assert.NotNull(createInfo.GetProperty("DepthState"));
        Assert.Null(createInfo.GetProperty("UsesVertexInput"));
    }

    [Fact]
    public void Drawing_vertex_input_uses_the_existing_32_byte_native_layout()
    {
        SdlGpuNativeGraphicsPipelineDescriptor descriptor = CreateDescriptor(
            SdlGpuStencilMode.Disabled,
            SdlGpuVertexInputDescription.Drawing2D);

        SDL.GPUVertexBufferDescription buffer = Assert.Single(descriptor.VertexBuffers);
        Assert.Equal((uint)0, buffer.Slot);
        Assert.Equal((uint)32, buffer.Pitch);
        Assert.Equal(SDL.GPUVertexInputRate.Vertex, buffer.InputRate);
        Assert.Collection(
            descriptor.Attributes,
            attribute => AssertAttribute(attribute, 0, SDL.GPUVertexElementFormat.Float2, 0),
            attribute => AssertAttribute(attribute, 1, SDL.GPUVertexElementFormat.Float2, 8),
            attribute => AssertAttribute(attribute, 2, SDL.GPUVertexElementFormat.Float4, 16));
    }

    [Fact]
    public void Fullscreen_pipeline_has_no_native_vertex_input()
    {
        SdlGpuNativeGraphicsPipelineDescriptor descriptor = CreateDescriptor(
            SdlGpuStencilMode.Disabled,
            SdlGpuVertexInputDescription.Empty);

        Assert.Empty(descriptor.VertexBuffers);
        Assert.Empty(descriptor.Attributes);
        Assert.False(descriptor.Pipeline.DepthStencilState.EnableDepthTest);
        Assert.False(descriptor.Pipeline.DepthStencilState.EnableDepthWrite);
        Assert.False(descriptor.Pipeline.DepthStencilState.EnableStencilTest);
    }

    [Fact]
    public void Existing_pipeline_conversion_keeps_depth_disabled_and_stencil_modes_distinct()
    {
        SdlGpuNativeGraphicsPipelineDescriptor disabled = CreateDescriptor(
            SdlGpuStencilMode.Disabled,
            SdlGpuVertexInputDescription.Drawing2D);
        Assert.False(disabled.Pipeline.DepthStencilState.EnableDepthTest);
        Assert.False(disabled.Pipeline.DepthStencilState.EnableDepthWrite);
        Assert.Equal(
            SDL.GPUCompareOp.Always,
            disabled.Pipeline.DepthStencilState.CompareOp);
        Assert.False(disabled.Pipeline.DepthStencilState.EnableStencilTest);
        Assert.Equal(
            SDL.GPUCompareOp.Always,
            disabled.Pipeline.DepthStencilState.FrontStencilState.CompareOp);
        Assert.Equal(
            SDL.GPUStencilOp.Keep,
            disabled.Pipeline.DepthStencilState.FrontStencilState.PassOp);

        AssertStencil(
            CreateDescriptor(SdlGpuStencilMode.Test, SdlGpuVertexInputDescription.Drawing2D),
            SDL.GPUStencilOp.Keep,
            writesColor: true);
        AssertStencil(
            CreateDescriptor(SdlGpuStencilMode.Increment, SdlGpuVertexInputDescription.Drawing2D),
            SDL.GPUStencilOp.IncrementAndClamp,
            writesColor: false);
        AssertStencil(
            CreateDescriptor(SdlGpuStencilMode.Decrement, SdlGpuVertexInputDescription.Drawing2D),
            SDL.GPUStencilOp.DecrementAndClamp,
            writesColor: false);
    }

    [Fact]
    public void Different_vertex_layout_and_depth_state_reach_native_descriptor()
    {
        SdlGpuVertexInputDescription vertexInput = new(
            28,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float3, 0),
            new SdlGpuVertexAttributeDescription(1, SdlGpuVertexFormat.Float4, 12));

        SdlGpuNativeGraphicsPipelineDescriptor baseline = CreateDescriptor(
            SdlGpuStencilMode.Disabled,
            SdlGpuVertexInputDescription.Drawing2D,
            SdlGpuDepthState.Disabled);
        SdlGpuNativeGraphicsPipelineDescriptor descriptor = CreateDescriptor(
            SdlGpuStencilMode.Disabled,
            vertexInput,
            SdlGpuDepthState.ReadWriteLessOrEqual);

        Assert.Equal((uint)32, Assert.Single(baseline.VertexBuffers).Pitch);
        Assert.Equal((uint)28, Assert.Single(descriptor.VertexBuffers).Pitch);
        Assert.Collection(
            descriptor.Attributes,
            attribute => AssertAttribute(attribute, 0, SDL.GPUVertexElementFormat.Float3, 0),
            attribute => AssertAttribute(attribute, 1, SDL.GPUVertexElementFormat.Float4, 12));
        Assert.False(baseline.Pipeline.DepthStencilState.EnableDepthTest);
        Assert.False(baseline.Pipeline.DepthStencilState.EnableDepthWrite);
        Assert.True(descriptor.Pipeline.DepthStencilState.EnableDepthTest);
        Assert.True(descriptor.Pipeline.DepthStencilState.EnableDepthWrite);
        Assert.Equal(
            SDL.GPUCompareOp.LessOrEqual,
            descriptor.Pipeline.DepthStencilState.CompareOp);
        Assert.False(descriptor.Pipeline.DepthStencilState.EnableStencilTest);
    }

    [Fact]
    public void Depth_and_stencil_are_independent()
    {
        SdlGpuNativeGraphicsPipelineDescriptor descriptor = CreateDescriptor(
            SdlGpuStencilMode.Test,
            SdlGpuVertexInputDescription.Drawing2D,
            SdlGpuDepthState.ReadWriteLessOrEqual);

        Assert.True(descriptor.Pipeline.DepthStencilState.EnableDepthTest);
        Assert.True(descriptor.Pipeline.DepthStencilState.EnableDepthWrite);
        Assert.Equal(
            SDL.GPUCompareOp.LessOrEqual,
            descriptor.Pipeline.DepthStencilState.CompareOp);
        Assert.True(descriptor.Pipeline.DepthStencilState.EnableStencilTest);
        Assert.Equal(
            SDL.GPUCompareOp.Equal,
            descriptor.Pipeline.DepthStencilState.FrontStencilState.CompareOp);
    }

    [Fact]
    public void Vertex_input_snapshots_attributes_and_rejects_incoherent_layouts()
    {
        SdlGpuVertexAttributeDescription[] callerOwned =
        [
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float3, 0),
            new SdlGpuVertexAttributeDescription(1, SdlGpuVertexFormat.Float4, 12)
        ];
        SdlGpuVertexInputDescription descriptor = new(28, callerOwned);

        callerOwned[0] = new SdlGpuVertexAttributeDescription(7, SdlGpuVertexFormat.Float2, 20);

        Assert.Equal((uint)0, descriptor.Attributes[0].Location);
        Assert.Equal(SdlGpuVertexFormat.Float3, descriptor.Attributes[0].Format);
        IList<SdlGpuVertexAttributeDescription> readOnlyAttributes =
            Assert.IsAssignableFrom<IList<SdlGpuVertexAttributeDescription>>(descriptor.Attributes);
        Assert.Throws<NotSupportedException>(() => readOnlyAttributes[0] =
            new SdlGpuVertexAttributeDescription(7, SdlGpuVertexFormat.Float2, 20));
        Assert.Throws<ArgumentNullException>(() =>
            new SdlGpuVertexInputDescription(0, null!));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SdlGpuVertexInputDescription(
            0,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float2, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SdlGpuVertexInputDescription(
            10,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float2, 0)));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(
            16,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float2, 2)));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(
            uint.MaxValue - 3,
            new SdlGpuVertexAttributeDescription(
                0,
                SdlGpuVertexFormat.Float4,
                uint.MaxValue - 7)));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(
            16,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float4, 4)));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(
            16,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float2, 0),
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float2, 8)));
        Assert.Throws<ArgumentException>(() => new SdlGpuVertexInputDescription(
            16,
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float3, 0),
            new SdlGpuVertexAttributeDescription(1, SdlGpuVertexFormat.Float2, 8)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SdlGpuVertexAttributeDescription(0, (SdlGpuVertexFormat)999, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SdlGpuDepthState(false, false, (SdlGpuCompareOperation)999));
    }

    [Fact]
    public void Native_conversion_rejects_null_vertex_input()
    {
        SdlGpuGraphicsPipelineCreateInfo createInfo = CreateInfo(
            SdlGpuStencilMode.Disabled,
            null!,
            SdlGpuDepthState.Disabled);

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = NativeSdlApi.CreateNativeGraphicsPipelineDescriptor(createInfo);
        });
    }

    [Fact]
    public void Fake_pipeline_snapshot_is_unchanged_after_caller_attribute_array_mutation()
    {
        SdlGpuVertexAttributeDescription[] callerOwned =
        [
            new SdlGpuVertexAttributeDescription(0, SdlGpuVertexFormat.Float3, 0),
            new SdlGpuVertexAttributeDescription(1, SdlGpuVertexFormat.Float4, 12)
        ];
        FakeSdlApi api = new();
        api.GpuShaders.Add(1, default);
        api.GpuShaders.Add(2, default);
        SdlGpuGraphicsPipelineCreateInfo createInfo = CreateInfo(
            SdlGpuStencilMode.Disabled,
            new SdlGpuVertexInputDescription(28, callerOwned),
            SdlGpuDepthState.Disabled);

        nint pipeline = api.CreateGpuGraphicsPipeline(api.DeviceResult, createInfo);
        callerOwned[0] = new SdlGpuVertexAttributeDescription(
            7,
            SdlGpuVertexFormat.Float2,
            20);

        SdlGpuGraphicsPipelineCreateInfo snapshot = api.GpuPipelines[pipeline];
        Assert.Equal((uint)0, snapshot.VertexInput.Attributes[0].Location);
        Assert.Equal(SdlGpuVertexFormat.Float3, snapshot.VertexInput.Attributes[0].Format);
        Assert.Equal((uint)0, snapshot.VertexInput.Attributes[0].Offset);
    }

    private static SdlGpuNativeGraphicsPipelineDescriptor CreateDescriptor(
        SdlGpuStencilMode stencilMode,
        SdlGpuVertexInputDescription vertexInput) => CreateDescriptor(
            stencilMode,
            vertexInput,
            SdlGpuDepthState.Disabled);

    private static SdlGpuNativeGraphicsPipelineDescriptor CreateDescriptor(
        SdlGpuStencilMode stencilMode,
        SdlGpuVertexInputDescription vertexInput,
        SdlGpuDepthState depthState) => NativeSdlApi.CreateNativeGraphicsPipelineDescriptor(
            CreateInfo(stencilMode, vertexInput, depthState));

    private static SdlGpuGraphicsPipelineCreateInfo CreateInfo(
        SdlGpuStencilMode stencilMode,
        SdlGpuVertexInputDescription vertexInput,
        SdlGpuDepthState depthState) =>
            new(
                VertexShader: 1,
                FragmentShader: 2,
                ColorFormat: SdlGpuTextureFormat.R8G8B8A8Unorm,
                DepthStencilFormat: SdlGpuTextureFormat.D24UnormS8Uint,
                SampleCount: SdlGpuSampleCount.One,
                PrimitiveType: SdlGpuPrimitiveType.TriangleList,
                BlendState: SdlGpuBlendState.Opaque,
                StencilMode: stencilMode,
                VertexInput: vertexInput,
                DepthState: depthState,
                ColorWriteMask: SdlGpuColorWriteMask.All);

    private static void AssertAttribute(
        SDL.GPUVertexAttribute attribute,
        uint location,
        SDL.GPUVertexElementFormat format,
        uint offset)
    {
        Assert.Equal(location, attribute.Location);
        Assert.Equal((uint)0, attribute.BufferSlot);
        Assert.Equal(format, attribute.Format);
        Assert.Equal(offset, attribute.Offset);
    }

    private static void AssertStencil(
        SdlGpuNativeGraphicsPipelineDescriptor descriptor,
        SDL.GPUStencilOp passOperation,
        bool writesColor)
    {
        SDL.GPUDepthStencilState state = descriptor.Pipeline.DepthStencilState;
        Assert.False(state.EnableDepthTest);
        Assert.False(state.EnableDepthWrite);
        Assert.True(state.EnableStencilTest);
        Assert.Equal(SDL.GPUCompareOp.Equal, state.FrontStencilState.CompareOp);
        Assert.Equal(passOperation, state.FrontStencilState.PassOp);
        Assert.Equal(state.FrontStencilState, state.BackStencilState);

        SDL.GPUColorTargetBlendState blend = Assert.Single(descriptor.ColorTargets).BlendState;
        SDL.GPUColorComponentFlags expected = writesColor
            ? SDL.GPUColorComponentFlags.R |
              SDL.GPUColorComponentFlags.G |
              SDL.GPUColorComponentFlags.B |
              SDL.GPUColorComponentFlags.A
            : 0;
        Assert.Equal(expected, blend.ColorWriteMask);
    }
}
