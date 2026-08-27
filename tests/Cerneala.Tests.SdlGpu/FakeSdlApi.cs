using Cerneala.Platforms.Sdl3;
using System.Runtime.InteropServices;

namespace Cerneala.Tests.SdlGpu;

internal sealed class FakeSdlApi : ISdlApi
{
    private readonly Queue<SdlEvent> events = [];
    private int nextWindow = 100;
    private int nextCursor = 1000;
    private int nextTexture = 2000;
    private int nextCommandBuffer = 3000;
    private int nextRenderPass = 4000;
    private int nextSwapchainTexture = 5000;
    private int nextCopyPass = 6000;
    private int nextTransferBuffer = 7000;
    private int nextFence = 8000;
    private int nextShader = 9000;
    private int nextPipeline = 10000;
    private int nextBuffer = 11000;
    private int nextSampler = 12000;
    private readonly Dictionary<nint, SdlGpuColorTargetInfo> colorTargetsByRenderPass = [];
    private readonly Dictionary<nint, nint> swapchainTexturesByCommandBuffer = [];

    public bool InitializeResult { get; set; } = true;

    public string Error { get; set; } = "fake SDL error";

    public nint DeviceResult { get; set; } = (nint)0x1234;

    public SdlGpuShaderFormats SupportedShaderFormats { get; set; } =
        SdlGpuShaderFormats.SpirV | SdlGpuShaderFormats.Dxil;

    public int InitializeCount { get; private set; }

    public int QuitCount { get; private set; }

    public int CreateDeviceCount { get; private set; }

    public int DestroyDeviceCount { get; private set; }

    public int InitializeThreadId { get; private set; }

    public int QuitThreadId { get; private set; }

    public SdlGpuShaderFormats RequestedShaderFormats { get; private set; }

    public bool RequestedDebugMode { get; private set; }

    public bool ClaimWindowResult { get; set; } = true;

    public bool SetSwapchainParametersResult { get; set; } = true;

    public bool SubmitResult { get; set; } = true;

    public bool WaitFenceResult { get; set; } = true;

    public int FailSwapchainAcquireCount { get; set; }

    public int NullSwapchainTextureCount { get; set; }

    public int FailTextureCreationAt { get; set; }

    public SdlGpuTextureFormat SwapchainTextureFormat { get; set; } =
        SdlGpuTextureFormat.R8G8B8A8Unorm;

    public HashSet<SdlGpuSwapchainComposition> SupportedCompositions { get; } =
        [SdlGpuSwapchainComposition.Sdr];

    public HashSet<SdlGpuPresentMode> SupportedPresentModes { get; } =
        [SdlGpuPresentMode.VSync];

    public HashSet<SdlGpuTextureFormat> SupportedTextureFormats { get; } =
        [
            SdlGpuTextureFormat.R8G8B8A8Unorm,
            SdlGpuTextureFormat.B8G8R8A8Unorm,
            SdlGpuTextureFormat.R8G8B8A8UnormSrgb,
            SdlGpuTextureFormat.B8G8R8A8UnormSrgb,
            SdlGpuTextureFormat.D24UnormS8Uint
        ];

    public HashSet<SdlGpuSampleCount> SupportedSampleCounts { get; } =
        [SdlGpuSampleCount.One, SdlGpuSampleCount.Two, SdlGpuSampleCount.Four];

    public List<string> DebugLabelCalls { get; } = [];

    public List<string> GpuActions { get; } = [];

    public Dictionary<nint, FakeGpuTexture> GpuTextures { get; } = [];

    public Dictionary<nint, FakeTransferBuffer> TransferBuffers { get; } = [];

    public Dictionary<nint, SdlGpuShaderCreateInfo> GpuShaders { get; } = [];

    public Dictionary<nint, SdlGpuGraphicsPipelineCreateInfo> GpuPipelines { get; } = [];

    public Dictionary<nint, FakeGpuBuffer> GpuBuffers { get; } = [];

    public Dictionary<nint, SdlGpuSamplerCreateInfo> GpuSamplers { get; } = [];

    public HashSet<nint> ClaimedGpuWindows { get; } = [];

    public List<nint> ReleasedGpuWindows { get; } = [];

    public List<nint> ReleasedGpuTextures { get; } = [];

    public List<SdlGpuBlitInfo> Blits { get; } = [];

    public List<nint> GeneratedMipmaps { get; } = [];

    public List<byte[]> FragmentUniformWrites { get; } = [];

    public List<(uint Slot, SdlGpuTextureSamplerBinding Binding)> FragmentSamplerBindings { get; } = [];

    public int SubmitCount { get; private set; }

    public int CancelCount { get; private set; }

    public int SwapchainConfigurationCount { get; private set; }

    public int TextureCreationCount { get; private set; }

    public SdlGpuSwapchainComposition ConfiguredComposition { get; private set; }

    public SdlGpuPresentMode ConfiguredPresentMode { get; private set; }

    public Dictionary<nint, FakeWindow> Windows { get; } = [];

    public List<nint> DestroyedWindows { get; } = [];

    public List<nint> DestroyedCursors { get; } = [];

    public float WindowPixelDensity { get; set; } = 2;

    public SdlRect DisplayBounds { get; set; } = new(0, 0, 1920, 1080);

    public void Enqueue(params SdlEvent[] values)
    {
        foreach (SdlEvent value in values)
        {
            events.Enqueue(value);
        }
    }

    public bool InitializeVideo()
    {
        InitializeCount++;
        InitializeThreadId = Environment.CurrentManagedThreadId;
        return InitializeResult;
    }

    public void Quit()
    {
        QuitCount++;
        QuitThreadId = Environment.CurrentManagedThreadId;
        GpuActions.Add("quit");
    }

    public string GetError() => Error;

    public nint CreateGpuDevice(
        SdlGpuShaderFormats shaderFormats,
        bool debugMode,
        string? preferredDriver)
    {
        CreateDeviceCount++;
        RequestedShaderFormats = shaderFormats;
        RequestedDebugMode = debugMode;
        Assert.Null(preferredDriver);
        return DeviceResult;
    }

    public void DestroyGpuDevice(nint device)
    {
        Assert.Equal(DeviceResult, device);
        DestroyDeviceCount++;
        GpuActions.Add("destroy-device");
    }

    public SdlGpuShaderFormats GetGpuShaderFormats(nint device)
    {
        Assert.Equal(DeviceResult, device);
        return SupportedShaderFormats;
    }

    public bool ClaimWindowForGpuDevice(nint device, nint window)
    {
        Assert.Equal(DeviceResult, device);
        if (!ClaimWindowResult)
        {
            return false;
        }

        Assert.True(ClaimedGpuWindows.Add(window));
        GpuActions.Add($"claim-window:{window}");
        return true;
    }

    public void ReleaseWindowFromGpuDevice(nint device, nint window)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(ClaimedGpuWindows.Remove(window));
        ReleasedGpuWindows.Add(window);
        GpuActions.Add($"release-window:{window}");
    }

    public bool WindowSupportsGpuSwapchainComposition(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(window, Windows.Keys);
        return SupportedCompositions.Contains(composition);
    }

    public bool WindowSupportsGpuPresentMode(
        nint device,
        nint window,
        SdlGpuPresentMode presentMode)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(window, Windows.Keys);
        return SupportedPresentModes.Contains(presentMode);
    }

    public bool SetGpuSwapchainParameters(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition,
        SdlGpuPresentMode presentMode)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(window, Windows.Keys);
        SwapchainConfigurationCount++;
        ConfiguredComposition = composition;
        ConfiguredPresentMode = presentMode;
        GpuActions.Add($"configure-swapchain:{window}:{composition}:{presentMode}");
        return SetSwapchainParametersResult;
    }

    public SdlGpuTextureFormat GetGpuSwapchainTextureFormat(nint device, nint window)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(window, Windows.Keys);
        return SwapchainTextureFormat;
    }

    public bool GpuTextureSupportsFormat(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuTextureUsage usage)
    {
        Assert.Equal(DeviceResult, device);
        Assert.NotEqual(SdlGpuTextureUsage.None, usage);
        return SupportedTextureFormats.Contains(format);
    }

    public bool GpuTextureSupportsSampleCount(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuSampleCount sampleCount)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(format, SupportedTextureFormats);
        return SupportedSampleCounts.Contains(sampleCount);
    }

    public nint CreateGpuTexture(nint device, in SdlGpuTextureCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        TextureCreationCount++;
        if (FailTextureCreationAt == TextureCreationCount)
        {
            return 0;
        }

        nint handle = (nint)nextTexture++;
        GpuTextures.Add(handle, new FakeGpuTexture(createInfo));
        GpuActions.Add($"create-texture:{handle}:{createInfo.Width}x{createInfo.Height}:{createInfo.SampleCount}");
        return handle;
    }

    public void ReleaseGpuTexture(nint device, nint texture)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(GpuTextures.Remove(texture));
        ReleasedGpuTextures.Add(texture);
        GpuActions.Add($"release-texture:{texture}");
    }

    public nint CreateGpuShader(nint device, in SdlGpuShaderCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        nint handle = (nint)nextShader++;
        GpuShaders.Add(handle, createInfo with { Code = createInfo.Code.ToArray() });
        GpuActions.Add($"create-shader:{handle}:{createInfo.Stage}:{createInfo.Format}");
        return handle;
    }

    public void ReleaseGpuShader(nint device, nint shader)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(GpuShaders.Remove(shader));
        GpuActions.Add($"release-shader:{shader}");
    }

    public nint CreateGpuGraphicsPipeline(
        nint device,
        in SdlGpuGraphicsPipelineCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(createInfo.VertexShader, GpuShaders.Keys);
        Assert.Contains(createInfo.FragmentShader, GpuShaders.Keys);
        nint handle = (nint)nextPipeline++;
        GpuPipelines.Add(handle, createInfo);
        GpuActions.Add($"create-pipeline:{handle}:{createInfo.BlendState}:{createInfo.StencilMode}");
        return handle;
    }

    public void ReleaseGpuGraphicsPipeline(nint device, nint pipeline)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(GpuPipelines.Remove(pipeline));
        GpuActions.Add($"release-pipeline:{pipeline}");
    }

    public nint CreateGpuBuffer(nint device, in SdlGpuBufferCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        nint handle = (nint)nextBuffer++;
        GpuBuffers.Add(handle, new FakeGpuBuffer(createInfo));
        GpuActions.Add($"create-buffer:{handle}:{createInfo.Usage}:{createInfo.Size}");
        return handle;
    }

    public void ReleaseGpuBuffer(nint device, nint buffer)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(GpuBuffers.Remove(buffer));
        GpuActions.Add($"release-buffer:{buffer}");
    }

    public nint CreateGpuSampler(nint device, in SdlGpuSamplerCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        nint handle = (nint)nextSampler++;
        GpuSamplers.Add(handle, createInfo);
        GpuActions.Add($"create-sampler:{handle}:{createInfo.Filter}:{createInfo.AddressMode}");
        return handle;
    }

    public void ReleaseGpuSampler(nint device, nint sampler)
    {
        Assert.Equal(DeviceResult, device);
        Assert.True(GpuSamplers.Remove(sampler));
        GpuActions.Add($"release-sampler:{sampler}");
    }

    public nint AcquireGpuCommandBuffer(nint device)
    {
        Assert.Equal(DeviceResult, device);
        nint commandBuffer = (nint)nextCommandBuffer++;
        GpuActions.Add($"acquire-command:{commandBuffer}");
        return commandBuffer;
    }

    public bool WaitAndAcquireGpuSwapchainTexture(
        nint commandBuffer,
        nint window,
        out nint texture,
        out uint width,
        out uint height)
    {
        Assert.Contains(window, ClaimedGpuWindows);
        FakeWindow fakeWindow = Windows[window];
        width = checked((uint)Math.Max(1, MathF.Ceiling(fakeWindow.Width * WindowPixelDensity)));
        height = checked((uint)Math.Max(1, MathF.Ceiling(fakeWindow.Height * WindowPixelDensity)));
        if (FailSwapchainAcquireCount > 0)
        {
            FailSwapchainAcquireCount--;
            texture = 0;
            GpuActions.Add($"acquire-swapchain-failed:{commandBuffer}:{window}");
            return false;
        }

        if (NullSwapchainTextureCount > 0)
        {
            NullSwapchainTextureCount--;
            texture = 0;
            GpuActions.Add($"acquire-swapchain-null:{commandBuffer}:{window}");
            return true;
        }

        texture = (nint)nextSwapchainTexture++;
        GpuTextures.Add(
            texture,
            new FakeGpuTexture(new SdlGpuTextureCreateInfo(
                SwapchainTextureFormat,
                SdlGpuTextureUsage.ColorTarget,
                width,
                height)));
        swapchainTexturesByCommandBuffer.Add(commandBuffer, texture);
        GpuActions.Add($"acquire-swapchain:{commandBuffer}:{window}:{texture}");
        return true;
    }

    public nint BeginGpuRenderPass(nint commandBuffer, in SdlGpuColorTargetInfo target)
    {
        Assert.Contains(target.Texture, GpuTextures.Keys);
        nint renderPass = (nint)nextRenderPass++;
        colorTargetsByRenderPass.Add(renderPass, target);
        GpuActions.Add($"begin-render:{commandBuffer}:{renderPass}:{target.Texture}");
        return renderPass;
    }

    public nint BeginGpuRenderPass(
        nint commandBuffer,
        in SdlGpuColorTargetInfo target,
        in SdlGpuDepthStencilTargetInfo depthStencilTarget)
    {
        Assert.Contains(depthStencilTarget.Texture, GpuTextures.Keys);
        nint renderPass = BeginGpuRenderPass(commandBuffer, target);
        GpuActions.Add($"depth-stencil:{renderPass}:{depthStencilTarget.Texture}:{depthStencilTarget.StencilLoadOp}");
        return renderPass;
    }

    public void EndGpuRenderPass(nint renderPass)
    {
        Assert.True(colorTargetsByRenderPass.Remove(renderPass, out SdlGpuColorTargetInfo colorTarget));
        FakeGpuTexture target = GpuTextures[colorTarget.Texture];
        if (colorTarget.LoadOp == SdlGpuLoadOp.Clear)
        {
            target.Fill(colorTarget.ClearColor);
        }
        if (colorTarget.ResolveTexture != 0)
        {
            GpuTextures[colorTarget.ResolveTexture].CopyFrom(target);
        }

        GpuActions.Add($"end-render:{renderPass}");
    }

    public void GenerateGpuMipmaps(nint commandBuffer, nint texture)
    {
        Assert.Contains(texture, GpuTextures.Keys);
        Assert.True(GpuTextures[texture].CreateInfo.MipLevelCount > 1);
        GeneratedMipmaps.Add(texture);
        GpuActions.Add($"generate-mipmaps:{commandBuffer}:{texture}");
    }

    public void BlitGpuTexture(nint commandBuffer, in SdlGpuBlitInfo blitInfo)
    {
        Blits.Add(blitInfo);
        GpuTextures[blitInfo.DestinationTexture].CopyFrom(GpuTextures[blitInfo.SourceTexture]);
        GpuActions.Add($"blit:{commandBuffer}:{blitInfo.SourceTexture}:{blitInfo.DestinationTexture}");
    }

    public bool SubmitGpuCommandBuffer(nint commandBuffer)
    {
        SubmitCount++;
        GpuActions.Add($"submit:{commandBuffer}");
        ReleaseFakeSwapchainTexture(commandBuffer);
        return SubmitResult;
    }

    public nint SubmitGpuCommandBufferAndAcquireFence(nint commandBuffer)
    {
        SubmitCount++;
        nint fence = SubmitResult ? (nint)nextFence++ : 0;
        GpuActions.Add($"submit-fence:{commandBuffer}:{fence}");
        ReleaseFakeSwapchainTexture(commandBuffer);
        return fence;
    }

    public bool CancelGpuCommandBuffer(nint commandBuffer)
    {
        CancelCount++;
        GpuActions.Add($"cancel:{commandBuffer}");
        return true;
    }

    public nint CreateGpuTransferBuffer(
        nint device,
        in SdlGpuTransferBufferCreateInfo createInfo)
    {
        Assert.Equal(DeviceResult, device);
        nint handle = (nint)nextTransferBuffer++;
        TransferBuffers.Add(handle, new FakeTransferBuffer(checked((int)createInfo.Size)));
        GpuActions.Add($"create-transfer:{handle}:{createInfo.Size}");
        return handle;
    }

    public void ReleaseGpuTransferBuffer(nint device, nint transferBuffer)
    {
        Assert.Equal(DeviceResult, device);
        FakeTransferBuffer buffer = TransferBuffers[transferBuffer];
        TransferBuffers.Remove(transferBuffer);
        buffer.Dispose();
        GpuActions.Add($"release-transfer:{transferBuffer}");
    }

    public nint MapGpuTransferBuffer(nint device, nint transferBuffer, bool cycle)
    {
        Assert.Equal(DeviceResult, device);
        GpuActions.Add($"map-transfer:{transferBuffer}:{cycle}");
        return TransferBuffers[transferBuffer].Pointer;
    }

    public void UnmapGpuTransferBuffer(nint device, nint transferBuffer)
    {
        Assert.Equal(DeviceResult, device);
        Assert.Contains(transferBuffer, TransferBuffers.Keys);
    }

    public nint BeginGpuCopyPass(nint commandBuffer)
    {
        nint copyPass = (nint)nextCopyPass++;
        GpuActions.Add($"begin-copy:{commandBuffer}:{copyPass}");
        return copyPass;
    }

    public void UploadToGpuBuffer(
        nint copyPass,
        nint transferBuffer,
        uint transferOffset,
        nint buffer,
        uint bufferOffset,
        uint size,
        bool cycle)
    {
        FakeTransferBuffer source = TransferBuffers[transferBuffer];
        FakeGpuBuffer destination = GpuBuffers[buffer];
        destination.CopyFrom(
            source.Pointer + checked((int)transferOffset),
            checked((int)bufferOffset),
            checked((int)size));
        GpuActions.Add($"upload-buffer:{copyPass}:{transferBuffer}:{buffer}:{size}:{cycle}");
    }

    public void UploadToGpuTexture(
        nint copyPass,
        in SdlGpuTextureTransferInfo source,
        in SdlGpuTextureRegion destination,
        bool cycle)
    {
        FakeTransferBuffer buffer = TransferBuffers[source.TransferBuffer];
        int pixelsPerRow = checked((int)(source.PixelsPerRow == 0
            ? destination.Width
            : source.PixelsPerRow));
        GpuTextures[destination.Texture].CopyFrom(
            buffer.Pointer + checked((int)source.Offset),
            checked((int)destination.Width),
            checked((int)destination.Height),
            checked(pixelsPerRow * 4));
        GpuActions.Add($"upload-texture:{copyPass}:{source.TransferBuffer}:{destination.Texture}:{cycle}");
    }

    public void DownloadFromGpuTexture(
        nint copyPass,
        in SdlGpuTextureRegion source,
        in SdlGpuTextureTransferInfo destination)
    {
        FakeGpuTexture texture = GpuTextures[source.Texture];
        FakeTransferBuffer buffer = TransferBuffers[destination.TransferBuffer];
        int pixelsPerRow = checked((int)(destination.PixelsPerRow == 0
            ? source.Width
            : destination.PixelsPerRow));
        texture.CopyTo(
            buffer.Pointer + checked((int)destination.Offset),
            checked((int)source.Width),
            checked((int)source.Height),
            checked(pixelsPerRow * 4));
        GpuActions.Add($"download:{copyPass}:{source.Texture}:{destination.TransferBuffer}");
    }

    public void EndGpuCopyPass(nint copyPass) =>
        GpuActions.Add($"end-copy:{copyPass}");

    public void BindGpuGraphicsPipeline(nint renderPass, nint pipeline)
    {
        Assert.Contains(pipeline, GpuPipelines.Keys);
        GpuActions.Add($"bind-pipeline:{renderPass}:{pipeline}");
    }

    public void BindGpuVertexBuffer(
        nint renderPass,
        uint slot,
        in SdlGpuBufferBinding binding)
    {
        Assert.Contains(binding.Buffer, GpuBuffers.Keys);
        GpuActions.Add($"bind-vertex:{renderPass}:{slot}:{binding.Buffer}:{binding.Offset}");
    }

    public void BindGpuIndexBuffer(nint renderPass, in SdlGpuBufferBinding binding)
    {
        Assert.Contains(binding.Buffer, GpuBuffers.Keys);
        GpuActions.Add($"bind-index:{renderPass}:{binding.Buffer}:{binding.Offset}");
    }

    public void BindGpuFragmentSampler(
        nint renderPass,
        uint slot,
        in SdlGpuTextureSamplerBinding binding)
    {
        Assert.Contains(binding.Texture, GpuTextures.Keys);
        Assert.Contains(binding.Sampler, GpuSamplers.Keys);
        FragmentSamplerBindings.Add((slot, binding));
        GpuActions.Add($"bind-sampler:{renderPass}:{slot}:{binding.Texture}:{binding.Sampler}");
    }

    public void PushGpuVertexUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data) =>
        GpuActions.Add($"push-uniform:{commandBuffer}:{slot}:{data.Length}");

    public void PushGpuFragmentUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data)
    {
        FragmentUniformWrites.Add(data.ToArray());
        GpuActions.Add($"push-fragment-uniform:{commandBuffer}:{slot}:{data.Length}");
    }

    public void SetGpuViewport(nint renderPass, in SdlGpuViewport viewport) =>
        GpuActions.Add(
            $"viewport:{renderPass}:{viewport.X},{viewport.Y},{viewport.Width},{viewport.Height}," +
            $"{viewport.MinDepth},{viewport.MaxDepth}");

    public void SetGpuScissor(nint renderPass, in SdlRect scissor) =>
        GpuActions.Add($"scissor:{renderPass}:{scissor.X},{scissor.Y},{scissor.Width},{scissor.Height}");

    public void SetGpuStencilReference(nint renderPass, byte reference) =>
        GpuActions.Add($"stencil-reference:{renderPass}:{reference}");

    public void DrawGpuIndexedPrimitives(
        nint renderPass,
        uint indexCount,
        uint firstIndex,
        int vertexOffset) =>
        GpuActions.Add($"draw-indexed:{renderPass}:{indexCount}:{firstIndex}:{vertexOffset}");

    public bool WaitForGpuFence(nint device, nint fence)
    {
        Assert.Equal(DeviceResult, device);
        GpuActions.Add($"wait-fence:{fence}");
        return WaitFenceResult;
    }

    public void ReleaseGpuFence(nint device, nint fence)
    {
        Assert.Equal(DeviceResult, device);
        GpuActions.Add($"release-fence:{fence}");
    }

    public nint CreateWindow(string title, int width, int height, SdlWindowOptions options)
    {
        nint handle = (nint)nextWindow++;
        Windows.Add(handle, new FakeWindow((uint)handle, title, width, height, options));
        return handle;
    }

    public void DestroyWindow(nint window)
    {
        if (!DestroyedWindows.Contains(window))
        {
            DestroyedWindows.Add(window);
            GpuActions.Add($"destroy-window:{window}");
        }
    }

    public uint GetWindowId(nint window) => Windows[window].Id;

    public bool SetWindowTitle(nint window, string title)
    {
        Windows[window].Title = title;
        return true;
    }

    public bool SetWindowSize(nint window, int width, int height)
    {
        Windows[window].Width = width;
        Windows[window].Height = height;
        return true;
    }

    public bool SetWindowMinimumSize(nint window, int width, int height)
    {
        Windows[window].MinimumWidth = width;
        Windows[window].MinimumHeight = height;
        return true;
    }

    public bool SetWindowMaximumSize(nint window, int width, int height)
    {
        Windows[window].MaximumWidth = width;
        Windows[window].MaximumHeight = height;
        return true;
    }

    public bool SetWindowPosition(nint window, int x, int y)
    {
        Windows[window].X = x;
        Windows[window].Y = y;
        return true;
    }

    public bool SetWindowAlwaysOnTop(nint window, bool alwaysOnTop)
    {
        Windows[window].AlwaysOnTop = alwaysOnTop;
        return true;
    }

    public bool SetWindowBordered(nint window, bool bordered)
    {
        Windows[window].Bordered = bordered;
        return true;
    }

    public bool SetWindowResizable(nint window, bool resizable)
    {
        Windows[window].Resizable = resizable;
        return true;
    }

    public bool ShowWindow(nint window)
    {
        Windows[window].Visible = true;
        return true;
    }

    public bool HideWindow(nint window)
    {
        Windows[window].Visible = false;
        return true;
    }

    public bool RaiseWindow(nint window)
    {
        Windows[window].Raised = true;
        return true;
    }

    public bool MinimizeWindow(nint window) => SetState(window, SdlEventKind.WindowMinimized);

    public bool MaximizeWindow(nint window) => SetState(window, SdlEventKind.WindowMaximized);

    public bool RestoreWindow(nint window) => SetState(window, SdlEventKind.WindowRestored);

    public bool SetWindowParent(nint window, nint parent)
    {
        Windows[window].Parent = parent;
        return true;
    }

    public float GetWindowPixelDensity(nint window) => WindowPixelDensity;

    public bool GetWindowSizeInPixels(nint window, out int width, out int height)
    {
        width = (int)MathF.Ceiling(Windows[window].Width * WindowPixelDensity);
        height = (int)MathF.Ceiling(Windows[window].Height * WindowPixelDensity);
        return true;
    }

    public bool GetWindowPosition(nint window, out int x, out int y)
    {
        x = Windows[window].X;
        y = Windows[window].Y;
        return true;
    }

    public bool GetWindowSize(nint window, out int width, out int height)
    {
        width = Windows[window].Width;
        height = Windows[window].Height;
        return true;
    }

    public uint GetPrimaryDisplay() => 1;

    public bool GetDisplayUsableBounds(uint displayId, out SdlRect bounds)
    {
        bounds = DisplayBounds;
        return displayId == 1;
    }

    public bool PollEvent(out SdlEvent @event)
    {
        if (events.TryDequeue(out @event))
        {
            return true;
        }

        @event = default;
        return false;
    }

    public nint CreateSystemCursor(SdlSystemCursor cursor) => (nint)nextCursor++;

    public bool SetCursor(nint cursor) => true;

    public void DestroyCursor(nint cursor) => DestroyedCursors.Add(cursor);

    public void PushGpuDebugGroup(nint commandBuffer, string label) =>
        DebugLabelCalls.Add($"push:{commandBuffer}:{label}");

    public void PopGpuDebugGroup(nint commandBuffer) =>
        DebugLabelCalls.Add($"pop:{commandBuffer}");

    public void InsertGpuDebugLabel(nint commandBuffer, string label) =>
        DebugLabelCalls.Add($"insert:{commandBuffer}:{label}");

    private bool SetState(nint window, SdlEventKind state)
    {
        Windows[window].State = state;
        return true;
    }

    private void ReleaseFakeSwapchainTexture(nint commandBuffer)
    {
        if (swapchainTexturesByCommandBuffer.Remove(commandBuffer, out nint texture))
        {
            GpuTextures.Remove(texture);
        }
    }

    internal sealed class FakeWindow(
        uint id,
        string title,
        int width,
        int height,
        SdlWindowOptions options)
    {
        public uint Id { get; } = id;
        public string Title { get; set; } = title;
        public int Width { get; set; } = width;
        public int Height { get; set; } = height;
        public SdlWindowOptions Options { get; } = options;
        public int X { get; set; }
        public int Y { get; set; }
        public int MinimumWidth { get; set; }
        public int MinimumHeight { get; set; }
        public int MaximumWidth { get; set; }
        public int MaximumHeight { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool Bordered { get; set; }
        public bool Resizable { get; set; }
        public bool Visible { get; set; }
        public bool Raised { get; set; }
        public nint Parent { get; set; }
        public SdlEventKind State { get; set; }
    }

    internal sealed class FakeGpuTexture
    {
        private byte[] pixels;

        public FakeGpuTexture(SdlGpuTextureCreateInfo createInfo)
        {
            CreateInfo = createInfo;
            pixels = new byte[checked((int)(createInfo.Width * createInfo.Height * 4))];
        }

        public SdlGpuTextureCreateInfo CreateInfo { get; }

        public void Fill(SdlGpuColor color)
        {
            byte r = ToByte(color.R);
            byte g = ToByte(color.G);
            byte b = ToByte(color.B);
            byte a = ToByte(color.A);
            bool bgra = CreateInfo.Format is SdlGpuTextureFormat.B8G8R8A8Unorm or
                SdlGpuTextureFormat.B8G8R8A8UnormSrgb;
            for (int offset = 0; offset < pixels.Length; offset += 4)
            {
                pixels[offset] = bgra ? b : r;
                pixels[offset + 1] = g;
                pixels[offset + 2] = bgra ? r : b;
                pixels[offset + 3] = a;
            }
        }

        public void CopyFrom(FakeGpuTexture source)
        {
            int length = Math.Min(pixels.Length, source.pixels.Length);
            source.pixels.AsSpan(0, length).CopyTo(pixels);
        }

        public void CopyFrom(nint source, int width, int height, int sourceStride)
        {
            int destinationStride = checked((int)CreateInfo.Width * 4);
            int rowLength = checked(width * 4);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(
                    source + y * sourceStride,
                    pixels,
                    y * destinationStride,
                    rowLength);
            }
        }

        public void CopyTo(nint destination, int width, int height, int destinationStride)
        {
            int sourceStride = checked((int)CreateInfo.Width * 4);
            int rowLength = checked(width * 4);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(
                    pixels,
                    y * sourceStride,
                    destination + y * destinationStride,
                    rowLength);
            }
        }

        private static byte ToByte(float value) =>
            (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
    }

    internal sealed class FakeGpuBuffer(SdlGpuBufferCreateInfo createInfo)
    {
        private readonly byte[] data = new byte[checked((int)createInfo.Size)];

        public SdlGpuBufferCreateInfo CreateInfo { get; } = createInfo;

        public ReadOnlyMemory<byte> Data => data;

        public void CopyFrom(nint source, int destinationOffset, int size) =>
            Marshal.Copy(source, data, destinationOffset, size);
    }

    internal sealed class FakeTransferBuffer : IDisposable
    {
        private nint pointer;

        public FakeTransferBuffer(int size)
        {
            Size = size;
            pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(new byte[size], 0, Pointer, size);
        }

        public int Size { get; }

        public nint Pointer => pointer;

        public void Dispose()
        {
            nint value = Interlocked.Exchange(ref pointer, 0);
            if (value != 0)
            {
                Marshal.FreeHGlobal(value);
            }
        }
    }
}
