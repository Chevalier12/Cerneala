namespace Cerneala.Platforms.Sdl3;

[Flags]
internal enum SdlGpuShaderFormats : uint
{
    None = 0,
    SpirV = 1 << 1,
    Dxbc = 1 << 2,
    Dxil = 1 << 3,
    Msl = 1 << 4,
    MetalLib = 1 << 5
}

[Flags]
internal enum SdlWindowOptions : ulong
{
    None = 0,
    Hidden = 1 << 3,
    Borderless = 1 << 4,
    Resizable = 1 << 5,
    Minimized = 1 << 6,
    Maximized = 1 << 7,
    HighPixelDensity = 1 << 13,
    AlwaysOnTop = 1 << 16,
    Utility = 1 << 17
}

internal enum SdlEventKind
{
    None,
    Quit,
    WindowShown,
    WindowHidden,
    WindowExposed,
    WindowMoved,
    WindowResized,
    WindowPixelSizeChanged,
    WindowMinimized,
    WindowMaximized,
    WindowRestored,
    WindowMouseEnter,
    WindowMouseLeave,
    WindowFocusGained,
    WindowFocusLost,
    WindowCloseRequested,
    WindowDisplayChanged,
    WindowDisplayScaleChanged,
    WindowDestroyed,
    KeyDown,
    KeyUp,
    TextEditing,
    TextInput,
    MouseMotion,
    MouseButtonDown,
    MouseButtonUp,
    MouseWheel
}

internal enum SdlSystemCursor
{
    Default,
    Text,
    Crosshair,
    ResizeHorizontal,
    ResizeVertical,
    Pointer
}

internal enum SdlGpuSwapchainComposition
{
    Sdr,
    SdrLinear,
    HdrExtendedLinear,
    Hdr10St2084
}

internal enum SdlGpuPresentMode
{
    VSync,
    Immediate,
    Mailbox
}

internal enum SdlGpuTextureFormat
{
    Invalid,
    R8G8B8A8Unorm = 4,
    B8G8R8A8Unorm = 12,
    R16G16B16A16Float = 29,
    R32Float = 30,
    R32G32B32A32Float = 32,
    R8G8B8A8UnormSrgb = 52,
    B8G8R8A8UnormSrgb = 53,
    D24UnormS8Uint = 61
}

internal enum SdlGpuSampleCount
{
    One,
    Two,
    Four,
    Eight
}

[Flags]
internal enum SdlGpuTextureUsage : uint
{
    None = 0,
    Sampler = 1,
    ColorTarget = 2,
    DepthStencilTarget = 4
}

[Flags]
internal enum SdlGpuBufferUsage : uint
{
    None = 0,
    Vertex = 1,
    Index = 2
}

internal enum SdlGpuShaderStage
{
    Vertex,
    Fragment
}

internal enum SdlGpuPrimitiveType
{
    TriangleList,
    TriangleStrip
}

internal enum SdlGpuFilter
{
    Nearest,
    Linear
}

internal enum SdlGpuSamplerAddressMode
{
    Repeat,
    MirroredRepeat,
    ClampToEdge
}

internal enum SdlGpuSamplerMipmapMode
{
    Nearest,
    Linear
}

internal enum SdlGpuBlendFactor
{
    Invalid,
    Zero,
    One,
    SourceColor,
    OneMinusSourceColor,
    DestinationColor,
    OneMinusDestinationColor,
    SourceAlpha,
    OneMinusSourceAlpha,
    DestinationAlpha,
    OneMinusDestinationAlpha
}

internal enum SdlGpuBlendOperation
{
    Invalid,
    Add,
    Subtract,
    ReverseSubtract,
    Minimum,
    Maximum
}

internal enum SdlGpuStencilMode
{
    Disabled,
    Test,
    Increment,
    Decrement
}

[Flags]
internal enum SdlGpuColorWriteMask
{
    None = 0,
    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Alpha = 1 << 3,
    All = Red | Green | Blue | Alpha
}

internal enum SdlGpuLoadOp
{
    Load,
    Clear,
    DontCare
}

internal enum SdlGpuStoreOp
{
    Store,
    DontCare,
    Resolve,
    ResolveAndStore
}

internal enum SdlGpuTransferBufferUsage
{
    Upload,
    Download
}

internal readonly record struct SdlRect(int X, int Y, int Width, int Height);

internal readonly record struct SdlGpuViewport(
    float X,
    float Y,
    float Width,
    float Height,
    float MinDepth = 0,
    float MaxDepth = 1);

internal readonly record struct SdlGpuColor(float R, float G, float B, float A);

internal readonly record struct SdlGpuTextureCreateInfo(
    SdlGpuTextureFormat Format,
    SdlGpuTextureUsage Usage,
    uint Width,
    uint Height,
    SdlGpuSampleCount SampleCount = SdlGpuSampleCount.One,
    uint MipLevelCount = 1);

internal readonly record struct SdlGpuColorTargetInfo(
    nint Texture,
    SdlGpuColor ClearColor,
    SdlGpuLoadOp LoadOp,
    SdlGpuStoreOp StoreOp,
    nint ResolveTexture = 0,
    bool Cycle = false,
    bool CycleResolveTexture = false);

internal readonly record struct SdlGpuDepthStencilTargetInfo(
    nint Texture,
    SdlGpuLoadOp DepthLoadOp,
    SdlGpuStoreOp DepthStoreOp,
    SdlGpuLoadOp StencilLoadOp,
    SdlGpuStoreOp StencilStoreOp,
    byte ClearStencil = 0,
    bool Cycle = false);

internal readonly record struct SdlGpuShaderCreateInfo(
    SdlGpuShaderFormats Format,
    SdlGpuShaderStage Stage,
    ReadOnlyMemory<byte> Code,
    string EntryPoint,
    uint SamplerCount,
    uint UniformBufferCount,
    uint StorageTextureCount = 0,
    uint StorageBufferCount = 0);

internal readonly record struct SdlGpuBlendState(
    SdlGpuBlendFactor SourceColor,
    SdlGpuBlendFactor DestinationColor,
    SdlGpuBlendOperation ColorOperation,
    SdlGpuBlendFactor SourceAlpha,
    SdlGpuBlendFactor DestinationAlpha,
    SdlGpuBlendOperation AlphaOperation,
    bool Enabled = true)
{
    public static SdlGpuBlendState Opaque { get; } = new(
        SdlGpuBlendFactor.One,
        SdlGpuBlendFactor.Zero,
        SdlGpuBlendOperation.Add,
        SdlGpuBlendFactor.One,
        SdlGpuBlendFactor.Zero,
        SdlGpuBlendOperation.Add,
        Enabled: false);
}

internal readonly record struct SdlGpuGraphicsPipelineCreateInfo(
    nint VertexShader,
    nint FragmentShader,
    SdlGpuTextureFormat ColorFormat,
    SdlGpuTextureFormat DepthStencilFormat,
    SdlGpuSampleCount SampleCount,
    SdlGpuPrimitiveType PrimitiveType,
    SdlGpuBlendState BlendState,
    SdlGpuStencilMode StencilMode,
    SdlGpuColorWriteMask ColorWriteMask = SdlGpuColorWriteMask.All,
    bool UsesVertexInput = true);

internal readonly record struct SdlGpuBufferCreateInfo(
    SdlGpuBufferUsage Usage,
    uint Size);

internal readonly record struct SdlGpuSamplerCreateInfo(
    SdlGpuFilter Filter,
    SdlGpuSamplerAddressMode AddressMode,
    SdlGpuSamplerMipmapMode MipmapMode = SdlGpuSamplerMipmapMode.Nearest,
    float MinLod = 0,
    float MaxLod = 1000);

internal readonly record struct SdlGpuBufferBinding(
    nint Buffer,
    uint Offset = 0);

internal readonly record struct SdlGpuTextureSamplerBinding(
    nint Texture,
    nint Sampler);

internal readonly record struct SdlGpuBlitInfo(
    nint SourceTexture,
    uint SourceWidth,
    uint SourceHeight,
    nint DestinationTexture,
    uint DestinationWidth,
    uint DestinationHeight,
    bool LinearFilter);

internal readonly record struct SdlGpuTextureRegion(
    nint Texture,
    uint Width,
    uint Height,
    uint X = 0,
    uint Y = 0);

internal readonly record struct SdlGpuTextureTransferInfo(
    nint TransferBuffer,
    uint Offset,
    uint PixelsPerRow,
    uint RowsPerLayer);

internal readonly record struct SdlGpuTransferBufferCreateInfo(
    SdlGpuTransferBufferUsage Usage,
    uint Size);

internal readonly record struct SdlEvent(
    SdlEventKind Kind,
    uint WindowId = 0,
    int Data1 = 0,
    int Data2 = 0,
    float X = 0,
    float Y = 0,
    int Scancode = 0,
    byte MouseButton = 0,
    bool Repeat = false,
    bool WheelFlipped = false,
    string? Text = null);

internal delegate void SdlEventWatch(SdlEvent @event);

internal interface ISdlApi
{
    bool InitializeVideo();

    void Quit();

    string GetError();

    nint CreateGpuDevice(
        SdlGpuShaderFormats shaderFormats,
        bool debugMode,
        string? preferredDriver);

    void DestroyGpuDevice(nint device);

    SdlGpuShaderFormats GetGpuShaderFormats(nint device);

    bool ClaimWindowForGpuDevice(nint device, nint window);

    void ReleaseWindowFromGpuDevice(nint device, nint window);

    bool WindowSupportsGpuSwapchainComposition(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition);

    bool WindowSupportsGpuPresentMode(
        nint device,
        nint window,
        SdlGpuPresentMode presentMode);

    bool SetGpuSwapchainParameters(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition,
        SdlGpuPresentMode presentMode);

    SdlGpuTextureFormat GetGpuSwapchainTextureFormat(nint device, nint window);

    bool GpuTextureSupportsFormat(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuTextureUsage usage);

    bool GpuTextureSupportsSampleCount(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuSampleCount sampleCount);

    nint CreateGpuTexture(nint device, in SdlGpuTextureCreateInfo createInfo);

    void ReleaseGpuTexture(nint device, nint texture);

    nint CreateGpuShader(nint device, in SdlGpuShaderCreateInfo createInfo);

    void ReleaseGpuShader(nint device, nint shader);

    nint CreateGpuGraphicsPipeline(
        nint device,
        in SdlGpuGraphicsPipelineCreateInfo createInfo);

    void ReleaseGpuGraphicsPipeline(nint device, nint pipeline);

    nint CreateGpuBuffer(nint device, in SdlGpuBufferCreateInfo createInfo);

    void ReleaseGpuBuffer(nint device, nint buffer);

    nint CreateGpuSampler(nint device, in SdlGpuSamplerCreateInfo createInfo);

    void ReleaseGpuSampler(nint device, nint sampler);

    nint AcquireGpuCommandBuffer(nint device);

    bool WaitAndAcquireGpuSwapchainTexture(
        nint commandBuffer,
        nint window,
        out nint texture,
        out uint width,
        out uint height);

    nint BeginGpuRenderPass(nint commandBuffer, in SdlGpuColorTargetInfo target);

    nint BeginGpuRenderPass(
        nint commandBuffer,
        in SdlGpuColorTargetInfo target,
        in SdlGpuDepthStencilTargetInfo depthStencilTarget);

    void EndGpuRenderPass(nint renderPass);

    void GenerateGpuMipmaps(nint commandBuffer, nint texture);

    void BlitGpuTexture(nint commandBuffer, in SdlGpuBlitInfo blitInfo);

    bool SubmitGpuCommandBuffer(nint commandBuffer);

    nint SubmitGpuCommandBufferAndAcquireFence(nint commandBuffer);

    bool CancelGpuCommandBuffer(nint commandBuffer);

    nint CreateGpuTransferBuffer(nint device, in SdlGpuTransferBufferCreateInfo createInfo);

    void ReleaseGpuTransferBuffer(nint device, nint transferBuffer);

    nint MapGpuTransferBuffer(nint device, nint transferBuffer, bool cycle);

    void UnmapGpuTransferBuffer(nint device, nint transferBuffer);

    nint BeginGpuCopyPass(nint commandBuffer);

    void UploadToGpuBuffer(
        nint copyPass,
        nint transferBuffer,
        uint transferOffset,
        nint buffer,
        uint bufferOffset,
        uint size,
        bool cycle);

    void UploadToGpuTexture(
        nint copyPass,
        in SdlGpuTextureTransferInfo source,
        in SdlGpuTextureRegion destination,
        bool cycle);

    void DownloadFromGpuTexture(
        nint copyPass,
        in SdlGpuTextureRegion source,
        in SdlGpuTextureTransferInfo destination);

    void EndGpuCopyPass(nint copyPass);

    void BindGpuGraphicsPipeline(nint renderPass, nint pipeline);

    void BindGpuVertexBuffer(
        nint renderPass,
        uint slot,
        in SdlGpuBufferBinding binding);

    void BindGpuIndexBuffer(
        nint renderPass,
        in SdlGpuBufferBinding binding);

    void BindGpuFragmentSampler(
        nint renderPass,
        uint slot,
        in SdlGpuTextureSamplerBinding binding);

    void PushGpuVertexUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data);

    void PushGpuFragmentUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data);

    void SetGpuViewport(nint renderPass, in SdlGpuViewport viewport);

    void SetGpuScissor(nint renderPass, in SdlRect scissor);

    void SetGpuStencilReference(nint renderPass, byte reference);

    void DrawGpuIndexedPrimitives(
        nint renderPass,
        uint indexCount,
        uint firstIndex,
        int vertexOffset);

    void DrawGpuPrimitives(
        nint renderPass,
        uint vertexCount,
        uint firstVertex);

    bool WaitForGpuFence(nint device, nint fence);

    void ReleaseGpuFence(nint device, nint fence);

    nint CreateWindow(string title, int width, int height, SdlWindowOptions options);

    void DestroyWindow(nint window);

    uint GetWindowId(nint window);

    bool SetWindowTitle(nint window, string title);

    bool SetWindowSize(nint window, int width, int height);

    bool SetWindowMinimumSize(nint window, int width, int height);

    bool SetWindowMaximumSize(nint window, int width, int height);

    bool SetWindowPosition(nint window, int x, int y);

    bool SetWindowAlwaysOnTop(nint window, bool alwaysOnTop);

    bool SetWindowBordered(nint window, bool bordered);

    bool SetWindowResizable(nint window, bool resizable);

    bool ShowWindow(nint window);

    bool HideWindow(nint window);

    bool RaiseWindow(nint window);

    bool MinimizeWindow(nint window);

    bool MaximizeWindow(nint window);

    bool RestoreWindow(nint window);

    bool SetWindowParent(nint window, nint parent);

    float GetWindowPixelDensity(nint window);

    bool GetWindowSizeInPixels(nint window, out int width, out int height);

    bool GetWindowPosition(nint window, out int x, out int y);

    bool GetWindowSize(nint window, out int width, out int height);

    uint GetPrimaryDisplay();

    bool GetDisplayUsableBounds(uint displayId, out SdlRect bounds);

    bool PollEvent(out SdlEvent @event);

    bool AddEventWatch(SdlEventWatch watch);

    void RemoveEventWatch(SdlEventWatch watch);

    nint CreateSystemCursor(SdlSystemCursor cursor);

    bool SetCursor(nint cursor);

    void DestroyCursor(nint cursor);

    void PushGpuDebugGroup(nint commandBuffer, string label);

    void PopGpuDebugGroup(nint commandBuffer);

    void InsertGpuDebugLabel(nint commandBuffer, string label);
}

internal static class SdlApiError
{
    public static InvalidOperationException Create(ISdlApi api, string operation)
    {
        string error = api.GetError();
        return new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? $"{operation} failed without an SDL error message."
                : $"{operation} failed: {error}");
    }
}
