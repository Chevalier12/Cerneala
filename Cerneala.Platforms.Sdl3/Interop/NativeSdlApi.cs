using SDL3;
using System.Runtime.InteropServices;

namespace Cerneala.Platforms.Sdl3;

internal sealed class NativeSdlApi : ISdlApi
{
    private readonly Dictionary<SdlEventWatch, SDL.EventFilter> eventWatches = [];

    public bool InitializeVideo() =>
        SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events);

    public void Quit() => SDL.Quit();

    public string GetError() => SDL.GetError();

    public nint CreateGpuDevice(
        SdlGpuShaderFormats shaderFormats,
        bool debugMode,
        string? preferredDriver) =>
        SDL.CreateGPUDevice(
            (SDL.GPUShaderFormat)(uint)shaderFormats,
            debugMode,
            preferredDriver!);

    public void DestroyGpuDevice(nint device) => SDL.DestroyGPUDevice(device);

    public SdlGpuShaderFormats GetGpuShaderFormats(nint device) =>
        (SdlGpuShaderFormats)(uint)SDL.GetGPUShaderFormats(device);

    public bool ClaimWindowForGpuDevice(nint device, nint window) =>
        SDL.ClaimWindowForGPUDevice(device, window);

    public void ReleaseWindowFromGpuDevice(nint device, nint window) =>
        SDL.ReleaseWindowFromGPUDevice(device, window);

    public bool WindowSupportsGpuSwapchainComposition(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition) =>
        SDL.WindowSupportsGPUSwapchainComposition(
            device,
            window,
            (SDL.GPUSwapchainComposition)composition);

    public bool WindowSupportsGpuPresentMode(
        nint device,
        nint window,
        SdlGpuPresentMode presentMode) =>
        SDL.WindowSupportsGPUPresentMode(
            device,
            window,
            (SDL.GPUPresentMode)presentMode);

    public bool SetGpuSwapchainParameters(
        nint device,
        nint window,
        SdlGpuSwapchainComposition composition,
        SdlGpuPresentMode presentMode) =>
        SDL.SetGPUSwapchainParameters(
            device,
            window,
            (SDL.GPUSwapchainComposition)composition,
            (SDL.GPUPresentMode)presentMode);

    public SdlGpuTextureFormat GetGpuSwapchainTextureFormat(nint device, nint window) =>
        (SdlGpuTextureFormat)SDL.GetGPUSwapchainTextureFormat(device, window);

    public bool GpuTextureSupportsFormat(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuTextureUsage usage) =>
        SDL.GPUTextureSupportsFormat(
            device,
            (SDL.GPUTextureFormat)format,
            SDL.GPUTextureType.TextureType2D,
            (SDL.GPUTextureUsageFlags)usage);

    public bool GpuTextureSupportsSampleCount(
        nint device,
        SdlGpuTextureFormat format,
        SdlGpuSampleCount sampleCount) =>
        SDL.GPUTextureSupportsSampleCount(
            device,
            (SDL.GPUTextureFormat)format,
            (SDL.GPUSampleCount)sampleCount);

    public nint CreateGpuTexture(nint device, in SdlGpuTextureCreateInfo createInfo)
    {
        SDL.GPUTextureCreateInfo native = new()
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = (SDL.GPUTextureFormat)createInfo.Format,
            Usage = (SDL.GPUTextureUsageFlags)createInfo.Usage,
            Width = createInfo.Width,
            Height = createInfo.Height,
            LayerCountOrDepth = 1,
            NumLevels = createInfo.MipLevelCount,
            SampleCount = (SDL.GPUSampleCount)createInfo.SampleCount
        };
        return SDL.CreateGPUTexture(device, in native);
    }

    public void ReleaseGpuTexture(nint device, nint texture) =>
        SDL.ReleaseGPUTexture(device, texture);

    public nint CreateGpuShader(nint device, in SdlGpuShaderCreateInfo createInfo)
    {
        SDL.GPUShaderCreateInfo native = new()
        {
            Format = (SDL.GPUShaderFormat)(uint)createInfo.Format,
            Stage = (SDL.GPUShaderStage)createInfo.Stage,
            NumSamplers = createInfo.SamplerCount,
            NumStorageTextures = createInfo.StorageTextureCount,
            NumStorageBuffers = createInfo.StorageBufferCount,
            NumUniformBuffers = createInfo.UniformBufferCount
        };
        return SDL.CreateGPUShader(
            device,
            in native,
            createInfo.Code.Span,
            createInfo.EntryPoint);
    }

    public void ReleaseGpuShader(nint device, nint shader) =>
        SDL.ReleaseGPUShader(device, shader);

    public nint CreateGpuGraphicsPipeline(
        nint device,
        in SdlGpuGraphicsPipelineCreateInfo createInfo)
    {
        SDL.GPUColorTargetBlendState blendState = new()
        {
            SrcColorBlendFactor = (SDL.GPUBlendFactor)createInfo.BlendState.SourceColor,
            DstColorBlendFactor = (SDL.GPUBlendFactor)createInfo.BlendState.DestinationColor,
            ColorBlendOp = (SDL.GPUBlendOp)createInfo.BlendState.ColorOperation,
            SrcAlphaBlendFactor = (SDL.GPUBlendFactor)createInfo.BlendState.SourceAlpha,
            DstAlphaBlendFactor = (SDL.GPUBlendFactor)createInfo.BlendState.DestinationAlpha,
            AlphaBlendOp = (SDL.GPUBlendOp)createInfo.BlendState.AlphaOperation,
            ColorWriteMask = createInfo.StencilMode is SdlGpuStencilMode.Increment or SdlGpuStencilMode.Decrement
                ? 0
                : ToNativeColorWriteMask(createInfo.ColorWriteMask),
            EnableBlend = createInfo.BlendState.Enabled,
            EnableColorWriteMask = true
        };
        SDL.GPUVertexBufferDescription[] vertexBuffers = createInfo.UsesVertexInput
            ?
            [
                new SDL.GPUVertexBufferDescription
                {
                    Slot = 0,
                    Pitch = 32,
                    InputRate = SDL.GPUVertexInputRate.Vertex
                }
            ]
            : [];
        SDL.GPUVertexAttribute[] attributes = createInfo.UsesVertexInput
            ?
            [
                new SDL.GPUVertexAttribute
                {
                    Location = 0,
                    BufferSlot = 0,
                    Format = SDL.GPUVertexElementFormat.Float2,
                    Offset = 0
                },
                new SDL.GPUVertexAttribute
                {
                    Location = 1,
                    BufferSlot = 0,
                    Format = SDL.GPUVertexElementFormat.Float2,
                    Offset = 8
                },
                new SDL.GPUVertexAttribute
                {
                    Location = 2,
                    BufferSlot = 0,
                    Format = SDL.GPUVertexElementFormat.Float4,
                    Offset = 16
                }
            ]
            : [];
        SDL.GPUColorTargetDescription[] colorTargets =
        [
            new SDL.GPUColorTargetDescription
            {
                Format = (SDL.GPUTextureFormat)createInfo.ColorFormat,
                BlendState = blendState
            }
        ];
        SDL.GPUDepthStencilState depthStencilState = CreateDepthStencilState(
            createInfo.StencilMode);
        SDL.GPUGraphicsPipelineCreateInfo native = new()
        {
            VertexShader = createInfo.VertexShader,
            FragmentShader = createInfo.FragmentShader,
            PrimitiveType = (SDL.GPUPrimitiveType)createInfo.PrimitiveType,
            RasterizerState = new SDL.GPURasterizerState
            {
                FillMode = SDL.GPUFillMode.Fill,
                CullMode = SDL.GPUCullMode.None,
                FrontFace = SDL.GPUFrontFace.CounterClockwise
            },
            MultisampleState = new SDL.GPUMultisampleState
            {
                SampleCount = (SDL.GPUSampleCount)createInfo.SampleCount
            },
            DepthStencilState = depthStencilState,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo
            {
                DepthStencilFormat = (SDL.GPUTextureFormat)createInfo.DepthStencilFormat
            }
        };
        return SDL.CreateGPUGraphicsPipeline(
            device,
            in native,
            vertexBuffers,
            attributes,
            colorTargets);
    }

    public void ReleaseGpuGraphicsPipeline(nint device, nint pipeline) =>
        SDL.ReleaseGPUGraphicsPipeline(device, pipeline);

    public nint CreateGpuBuffer(nint device, in SdlGpuBufferCreateInfo createInfo)
    {
        SDL.GPUBufferCreateInfo native = new()
        {
            Usage = (SDL.GPUBufferUsageFlags)createInfo.Usage,
            Size = createInfo.Size
        };
        return SDL.CreateGPUBuffer(device, in native);
    }

    public void ReleaseGpuBuffer(nint device, nint buffer) =>
        SDL.ReleaseGPUBuffer(device, buffer);

    public nint CreateGpuSampler(nint device, in SdlGpuSamplerCreateInfo createInfo)
    {
        SDL.GPUSamplerCreateInfo native = new()
        {
            MinFilter = (SDL.GPUFilter)createInfo.Filter,
            MagFilter = (SDL.GPUFilter)createInfo.Filter,
            MipmapMode = (SDL.GPUSamplerMipmapMode)createInfo.MipmapMode,
            AddressModeU = (SDL.GPUSamplerAddressMode)createInfo.AddressMode,
            AddressModeV = (SDL.GPUSamplerAddressMode)createInfo.AddressMode,
            AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,
            MinLod = createInfo.MinLod,
            MaxLod = createInfo.MaxLod
        };
        return SDL.CreateGPUSampler(device, in native);
    }

    public void ReleaseGpuSampler(nint device, nint sampler) =>
        SDL.ReleaseGPUSampler(device, sampler);

    public nint AcquireGpuCommandBuffer(nint device) =>
        SDL.AcquireGPUCommandBuffer(device);

    public bool WaitAndAcquireGpuSwapchainTexture(
        nint commandBuffer,
        nint window,
        out nint texture,
        out uint width,
        out uint height) =>
        SDL.WaitAndAcquireGPUSwapchainTexture(
            commandBuffer,
            window,
            out texture,
            out width,
            out height);

    public nint BeginGpuRenderPass(nint commandBuffer, in SdlGpuColorTargetInfo target)
    {
        SDL.GPUColorTargetInfo native = new()
        {
            Texture = target.Texture,
            ClearColor = new SDL.FColor(
                target.ClearColor.R,
                target.ClearColor.G,
                target.ClearColor.B,
                target.ClearColor.A),
            LoadOp = (SDL.GPULoadOp)target.LoadOp,
            StoreOp = (SDL.GPUStoreOp)target.StoreOp,
            ResolveTexture = target.ResolveTexture,
            Cycle = target.Cycle,
            CycleResolveTexture = target.CycleResolveTexture
        };
        ReadOnlySpan<SDL.GPUColorTargetInfo> targets = [native];
        return SDL.BeginGPURenderPass(commandBuffer, targets, 1, 0);
    }

    public nint BeginGpuRenderPass(
        nint commandBuffer,
        in SdlGpuColorTargetInfo target,
        in SdlGpuDepthStencilTargetInfo depthStencilTarget)
    {
        SDL.GPUColorTargetInfo nativeColor = CreateColorTarget(target);
        SDL.GPUDepthStencilTargetInfo nativeDepthStencil = new()
        {
            Texture = depthStencilTarget.Texture,
            ClearDepth = 1,
            LoadOp = (SDL.GPULoadOp)depthStencilTarget.DepthLoadOp,
            StoreOp = (SDL.GPUStoreOp)depthStencilTarget.DepthStoreOp,
            StencilLoadOp = (SDL.GPULoadOp)depthStencilTarget.StencilLoadOp,
            StencilStoreOp = (SDL.GPUStoreOp)depthStencilTarget.StencilStoreOp,
            ClearStencil = depthStencilTarget.ClearStencil,
            Cycle = depthStencilTarget.Cycle ? (byte)1 : (byte)0
        };
        ReadOnlySpan<SDL.GPUColorTargetInfo> targets = [nativeColor];
        return SDL.BeginGPURenderPass(
            commandBuffer,
            targets,
            1,
            in nativeDepthStencil);
    }

    public void EndGpuRenderPass(nint renderPass) =>
        SDL.EndGPURenderPass(renderPass);

    public void GenerateGpuMipmaps(nint commandBuffer, nint texture) =>
        SDL.GenerateMipmapsForGPUTexture(commandBuffer, texture);

    public void BlitGpuTexture(nint commandBuffer, in SdlGpuBlitInfo blitInfo)
    {
        SDL.GPUBlitInfo native = new()
        {
            Source = new SDL.GPUBlitRegion
            {
                Texture = blitInfo.SourceTexture,
                W = blitInfo.SourceWidth,
                H = blitInfo.SourceHeight
            },
            Destination = new SDL.GPUBlitRegion
            {
                Texture = blitInfo.DestinationTexture,
                W = blitInfo.DestinationWidth,
                H = blitInfo.DestinationHeight
            },
            LoadOp = SDL.GPULoadOp.DontCare,
            Filter = blitInfo.LinearFilter ? SDL.GPUFilter.Linear : SDL.GPUFilter.Nearest
        };
        SDL.BlitGPUTexture(commandBuffer, in native);
    }

    public bool SubmitGpuCommandBuffer(nint commandBuffer) =>
        SDL.SubmitGPUCommandBuffer(commandBuffer);

    public nint SubmitGpuCommandBufferAndAcquireFence(nint commandBuffer) =>
        SDL.SubmitGPUCommandBufferAndAcquireFence(commandBuffer);

    public bool CancelGpuCommandBuffer(nint commandBuffer) =>
        SDL.CancelGPUCommandBuffer(commandBuffer);

    public nint CreateGpuTransferBuffer(
        nint device,
        in SdlGpuTransferBufferCreateInfo createInfo)
    {
        SDL.GPUTransferBufferCreateInfo native = new()
        {
            Usage = (SDL.GPUTransferBufferUsage)createInfo.Usage,
            Size = createInfo.Size
        };
        return SDL.CreateGPUTransferBuffer(device, in native);
    }

    public void ReleaseGpuTransferBuffer(nint device, nint transferBuffer) =>
        SDL.ReleaseGPUTransferBuffer(device, transferBuffer);

    public nint MapGpuTransferBuffer(nint device, nint transferBuffer, bool cycle) =>
        SDL.MapGPUTransferBuffer(device, transferBuffer, cycle);

    public void UnmapGpuTransferBuffer(nint device, nint transferBuffer) =>
        SDL.UnmapGPUTransferBuffer(device, transferBuffer);

    public nint BeginGpuCopyPass(nint commandBuffer) =>
        SDL.BeginGPUCopyPass(commandBuffer);

    public void UploadToGpuBuffer(
        nint copyPass,
        nint transferBuffer,
        uint transferOffset,
        nint buffer,
        uint bufferOffset,
        uint size,
        bool cycle)
    {
        SDL.GPUTransferBufferLocation source = new()
        {
            TransferBuffer = transferBuffer,
            Offset = transferOffset
        };
        SDL.GPUBufferRegion destination = new()
        {
            Buffer = buffer,
            Offset = bufferOffset,
            Size = size
        };
        SDL.UploadToGPUBuffer(copyPass, in source, in destination, cycle);
    }

    public void UploadToGpuTexture(
        nint copyPass,
        in SdlGpuTextureTransferInfo source,
        in SdlGpuTextureRegion destination,
        bool cycle)
    {
        SDL.GPUTextureTransferInfo nativeSource = new()
        {
            TransferBuffer = source.TransferBuffer,
            Offset = source.Offset,
            PixelsPerRow = source.PixelsPerRow,
            RowsPerLayer = source.RowsPerLayer
        };
        SDL.GPUTextureRegion nativeDestination = new()
        {
            Texture = destination.Texture,
            X = destination.X,
            Y = destination.Y,
            W = destination.Width,
            H = destination.Height,
            D = 1
        };
        SDL.UploadToGPUTexture(
            copyPass,
            in nativeSource,
            in nativeDestination,
            cycle);
    }

    public void DownloadFromGpuTexture(
        nint copyPass,
        in SdlGpuTextureRegion source,
        in SdlGpuTextureTransferInfo destination)
    {
        SDL.GPUTextureRegion nativeSource = new()
        {
            Texture = source.Texture,
            X = source.X,
            Y = source.Y,
            W = source.Width,
            H = source.Height,
            D = 1
        };
        SDL.GPUTextureTransferInfo nativeDestination = new()
        {
            TransferBuffer = destination.TransferBuffer,
            Offset = destination.Offset,
            PixelsPerRow = destination.PixelsPerRow,
            RowsPerLayer = destination.RowsPerLayer
        };
        SDL.DownloadFromGPUTexture(copyPass, in nativeSource, in nativeDestination);
    }

    public void EndGpuCopyPass(nint copyPass) =>
        SDL.EndGPUCopyPass(copyPass);

    public void BindGpuGraphicsPipeline(nint renderPass, nint pipeline) =>
        SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

    public void BindGpuVertexBuffer(
        nint renderPass,
        uint slot,
        in SdlGpuBufferBinding binding)
    {
        SDL.GPUBufferBinding native = new()
        {
            Buffer = binding.Buffer,
            Offset = binding.Offset
        };
        ReadOnlySpan<SDL.GPUBufferBinding> bindings = [native];
        SDL.BindGPUVertexBuffers(renderPass, slot, bindings, 1);
    }

    public void BindGpuIndexBuffer(
        nint renderPass,
        in SdlGpuBufferBinding binding)
    {
        SDL.GPUBufferBinding native = new()
        {
            Buffer = binding.Buffer,
            Offset = binding.Offset
        };
        SDL.BindGPUIndexBuffer(
            renderPass,
            in native,
            SDL.GPUIndexElementSize.IndexElementSize32Bit);
    }

    public void BindGpuFragmentSampler(
        nint renderPass,
        uint slot,
        in SdlGpuTextureSamplerBinding binding)
    {
        SDL.GPUTextureSamplerBinding native = new()
        {
            Texture = binding.Texture,
            Sampler = binding.Sampler
        };
        ReadOnlySpan<SDL.GPUTextureSamplerBinding> bindings = [native];
        SDL.BindGPUFragmentSamplers(renderPass, slot, bindings, 1);
    }

    public void PushGpuVertexUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data) =>
        SDL.PushGPUVertexUniformData(
            commandBuffer,
            slot,
            data,
            checked((uint)data.Length));

    public void PushGpuFragmentUniformData(
        nint commandBuffer,
        uint slot,
        ReadOnlySpan<byte> data) =>
        SDL.PushGPUFragmentUniformData(
            commandBuffer,
            slot,
            data,
            checked((uint)data.Length));

    public void SetGpuViewport(nint renderPass, in SdlGpuViewport viewport)
    {
        SDL.GPUViewport native = new()
        {
            X = viewport.X,
            Y = viewport.Y,
            W = viewport.Width,
            H = viewport.Height,
            MinDepth = viewport.MinDepth,
            MaxDepth = viewport.MaxDepth
        };
        SDL.SetGPUViewport(renderPass, in native);
    }

    public void SetGpuScissor(nint renderPass, in SdlRect scissor)
    {
        SDL.Rect native = new()
        {
            X = scissor.X,
            Y = scissor.Y,
            W = scissor.Width,
            H = scissor.Height
        };
        SDL.SetGPUScissor(renderPass, in native);
    }

    public void SetGpuStencilReference(nint renderPass, byte reference) =>
        SDL.SetGPUStencilReference(renderPass, reference);

    public void DrawGpuIndexedPrimitives(
        nint renderPass,
        uint indexCount,
        uint firstIndex,
        int vertexOffset) =>
        SDL.DrawGPUIndexedPrimitives(
            renderPass,
            indexCount,
            1,
            firstIndex,
            vertexOffset,
            0);

    public void DrawGpuPrimitives(
        nint renderPass,
        uint vertexCount,
        uint firstVertex) =>
        SDL.DrawGPUPrimitives(
            renderPass,
            vertexCount,
            1,
            firstVertex,
            0);

    public bool WaitForGpuFence(nint device, nint fence) =>
        SDL.WaitForGPUFences(device, waitAll: true, [fence], 1);

    public void ReleaseGpuFence(nint device, nint fence) =>
        SDL.ReleaseGPUFence(device, fence);

    public nint CreateWindow(string title, int width, int height, SdlWindowOptions options) =>
        SDL.CreateWindow(title, width, height, (SDL.WindowFlags)(ulong)options);

    public void DestroyWindow(nint window) => SDL.DestroyWindow(window);

    public uint GetWindowId(nint window) => SDL.GetWindowID(window);

    public bool SetWindowTitle(nint window, string title) => SDL.SetWindowTitle(window, title);

    public bool SetWindowSize(nint window, int width, int height) => SDL.SetWindowSize(window, width, height);

    public bool SetWindowMinimumSize(nint window, int width, int height) => SDL.SetWindowMinimumSize(window, width, height);

    public bool SetWindowMaximumSize(nint window, int width, int height) => SDL.SetWindowMaximumSize(window, width, height);

    public bool SetWindowPosition(nint window, int x, int y) => SDL.SetWindowPosition(window, x, y);

    public bool SetWindowAlwaysOnTop(nint window, bool alwaysOnTop) => SDL.SetWindowAlwaysOnTop(window, alwaysOnTop);

    public bool SetWindowBordered(nint window, bool bordered) => SDL.SetWindowBordered(window, bordered);

    public bool SetWindowResizable(nint window, bool resizable) => SDL.SetWindowResizable(window, resizable);

    public bool ShowWindow(nint window) => SDL.ShowWindow(window);

    public bool HideWindow(nint window) => SDL.HideWindow(window);

    public bool RaiseWindow(nint window) => SDL.RaiseWindow(window);

    public bool MinimizeWindow(nint window) => SDL.MinimizeWindow(window);

    public bool MaximizeWindow(nint window) => SDL.MaximizeWindow(window);

    public bool RestoreWindow(nint window) => SDL.RestoreWindow(window);

    public bool SetWindowParent(nint window, nint parent) => SDL.SetWindowParent(window, parent);

    public float GetWindowPixelDensity(nint window) => SDL.GetWindowPixelDensity(window);

    public bool GetWindowSizeInPixels(nint window, out int width, out int height) =>
        SDL.GetWindowSizeInPixels(window, out width, out height);

    public bool GetWindowPosition(nint window, out int x, out int y) =>
        SDL.GetWindowPosition(window, out x, out y);

    public bool GetWindowSize(nint window, out int width, out int height) =>
        SDL.GetWindowSize(window, out width, out height);

    public uint GetPrimaryDisplay() => SDL.GetPrimaryDisplay();

    public bool GetDisplayUsableBounds(uint displayId, out SdlRect bounds)
    {
        bool result = SDL.GetDisplayUsableBounds(displayId, out SDL.Rect native);
        bounds = new SdlRect(native.X, native.Y, native.W, native.H);
        return result;
    }

    public bool PollEvent(out SdlEvent @event)
    {
        if (!SDL.PollEvent(out SDL.Event native))
        {
            @event = default;
            return false;
        }

        @event = ConvertEvent(native);
        return true;
    }

    public bool AddEventWatch(SdlEventWatch watch)
    {
        ArgumentNullException.ThrowIfNull(watch);
        if (eventWatches.ContainsKey(watch))
        {
            throw new InvalidOperationException("The SDL event watch is already registered.");
        }

        SDL.EventFilter nativeWatch = (nint _, ref SDL.Event native) =>
        {
            watch(ConvertEvent(native));
            return true;
        };
        if (!SDL.AddEventWatch(nativeWatch, 0))
        {
            return false;
        }

        eventWatches.Add(watch, nativeWatch);
        return true;
    }

    public void RemoveEventWatch(SdlEventWatch watch)
    {
        ArgumentNullException.ThrowIfNull(watch);
        if (eventWatches.Remove(watch, out SDL.EventFilter? nativeWatch))
        {
            SDL.RemoveEventWatch(nativeWatch, 0);
        }
    }

    private static SdlEvent ConvertEvent(in SDL.Event native)
    {
        SDL.EventType type = (SDL.EventType)native.Type;
        return type switch
        {
            SDL.EventType.Quit => new SdlEvent(SdlEventKind.Quit),
            SDL.EventType.WindowShown => Window(SdlEventKind.WindowShown, native.Window),
            SDL.EventType.WindowHidden => Window(SdlEventKind.WindowHidden, native.Window),
            SDL.EventType.WindowExposed => Window(SdlEventKind.WindowExposed, native.Window),
            SDL.EventType.WindowMoved => Window(SdlEventKind.WindowMoved, native.Window),
            SDL.EventType.WindowResized => Window(SdlEventKind.WindowResized, native.Window),
            SDL.EventType.WindowPixelSizeChanged => Window(SdlEventKind.WindowPixelSizeChanged, native.Window),
            SDL.EventType.WindowMinimized => Window(SdlEventKind.WindowMinimized, native.Window),
            SDL.EventType.WindowMaximized => Window(SdlEventKind.WindowMaximized, native.Window),
            SDL.EventType.WindowRestored => Window(SdlEventKind.WindowRestored, native.Window),
            SDL.EventType.WindowMouseEnter => Window(SdlEventKind.WindowMouseEnter, native.Window),
            SDL.EventType.WindowMouseLeave => Window(SdlEventKind.WindowMouseLeave, native.Window),
            SDL.EventType.WindowFocusGained => Window(SdlEventKind.WindowFocusGained, native.Window),
            SDL.EventType.WindowFocusLost => Window(SdlEventKind.WindowFocusLost, native.Window),
            SDL.EventType.WindowCloseRequested => Window(SdlEventKind.WindowCloseRequested, native.Window),
            SDL.EventType.WindowDisplayChanged => Window(SdlEventKind.WindowDisplayChanged, native.Window),
            SDL.EventType.WindowDisplayScaleChanged => Window(SdlEventKind.WindowDisplayScaleChanged, native.Window),
            SDL.EventType.WindowDestroyed => Window(SdlEventKind.WindowDestroyed, native.Window),
            SDL.EventType.KeyDown => Key(SdlEventKind.KeyDown, native.Key),
            SDL.EventType.KeyUp => Key(SdlEventKind.KeyUp, native.Key),
            SDL.EventType.TextEditing => Text(SdlEventKind.TextEditing, native.Edit.WindowID, native.Edit.Text),
            SDL.EventType.TextInput => Text(SdlEventKind.TextInput, native.Text.WindowID, native.Text.Text),
            SDL.EventType.MouseMotion => new SdlEvent(SdlEventKind.MouseMotion, native.Motion.WindowID, X: native.Motion.X, Y: native.Motion.Y),
            SDL.EventType.MouseButtonDown => Button(SdlEventKind.MouseButtonDown, native.Button),
            SDL.EventType.MouseButtonUp => Button(SdlEventKind.MouseButtonUp, native.Button),
            SDL.EventType.MouseWheel => new SdlEvent(
                SdlEventKind.MouseWheel,
                native.Wheel.WindowID,
                Data1: native.Wheel.IntegerX,
                Data2: native.Wheel.IntegerY,
                X: native.Wheel.MouseX,
                Y: native.Wheel.MouseY,
                WheelFlipped: native.Wheel.Direction == SDL.MouseWheelDirection.Flipped),
            _ => default
        };
    }

    public nint CreateSystemCursor(SdlSystemCursor cursor) =>
        SDL.CreateSystemCursor(cursor switch
        {
            SdlSystemCursor.Text => SDL.SystemCursor.Text,
            SdlSystemCursor.Crosshair => SDL.SystemCursor.Crosshair,
            SdlSystemCursor.ResizeHorizontal => SDL.SystemCursor.EWResize,
            SdlSystemCursor.ResizeVertical => SDL.SystemCursor.NSResize,
            SdlSystemCursor.Pointer => SDL.SystemCursor.Pointer,
            _ => SDL.SystemCursor.Default
        });

    public bool SetCursor(nint cursor) => SDL.SetCursor(cursor);

    public void DestroyCursor(nint cursor) => SDL.DestroyCursor(cursor);

    public void PushGpuDebugGroup(nint commandBuffer, string label) =>
        SDL.PushGPUDebugGroup(commandBuffer, label);

    public void PopGpuDebugGroup(nint commandBuffer) =>
        SDL.PopGPUDebugGroup(commandBuffer);

    public void InsertGpuDebugLabel(nint commandBuffer, string label) =>
        SDL.InsertGPUDebugLabel(commandBuffer, label);

    private static SdlEvent Window(SdlEventKind kind, SDL.WindowEvent @event) =>
        new(kind, @event.WindowID, @event.Data1, @event.Data2);

    private static SdlEvent Key(SdlEventKind kind, SDL.KeyboardEvent @event) =>
        new(kind, @event.WindowID, Scancode: (int)@event.Scancode, Repeat: @event.Repeat);

    private static SdlEvent Button(SdlEventKind kind, SDL.MouseButtonEvent @event) =>
        new(kind, @event.WindowID, X: @event.X, Y: @event.Y, MouseButton: @event.Button);

    private static SdlEvent Text(SdlEventKind kind, uint windowId, nint text) =>
        new(kind, windowId, Text: Marshal.PtrToStringUTF8(text));

    private static SDL.GPUColorTargetInfo CreateColorTarget(
        in SdlGpuColorTargetInfo target) =>
        new()
        {
            Texture = target.Texture,
            ClearColor = new SDL.FColor(
                target.ClearColor.R,
                target.ClearColor.G,
                target.ClearColor.B,
                target.ClearColor.A),
            LoadOp = (SDL.GPULoadOp)target.LoadOp,
            StoreOp = (SDL.GPUStoreOp)target.StoreOp,
            ResolveTexture = target.ResolveTexture,
            Cycle = target.Cycle,
            CycleResolveTexture = target.CycleResolveTexture
        };

    private static SDL.GPUDepthStencilState CreateDepthStencilState(
        SdlGpuStencilMode mode)
    {
        SDL.GPUStencilOpState stencil = new()
        {
            FailOp = SDL.GPUStencilOp.Keep,
            PassOp = mode switch
            {
                SdlGpuStencilMode.Increment => SDL.GPUStencilOp.IncrementAndClamp,
                SdlGpuStencilMode.Decrement => SDL.GPUStencilOp.DecrementAndClamp,
                _ => SDL.GPUStencilOp.Keep
            },
            DepthFailOp = SDL.GPUStencilOp.Keep,
            CompareOp = mode == SdlGpuStencilMode.Disabled
                ? SDL.GPUCompareOp.Always
                : SDL.GPUCompareOp.Equal
        };
        return new SDL.GPUDepthStencilState
        {
            BackStencilState = stencil,
            FrontStencilState = stencil,
            CompareMask = byte.MaxValue,
            WriteMask = byte.MaxValue,
            EnableStencilTest = mode != SdlGpuStencilMode.Disabled
        };
    }

    private static SDL.GPUColorComponentFlags ToNativeColorWriteMask(
        SdlGpuColorWriteMask mask)
    {
        SDL.GPUColorComponentFlags result = 0;
        if ((mask & SdlGpuColorWriteMask.Red) != 0)
        {
            result |= SDL.GPUColorComponentFlags.R;
        }
        if ((mask & SdlGpuColorWriteMask.Green) != 0)
        {
            result |= SDL.GPUColorComponentFlags.G;
        }
        if ((mask & SdlGpuColorWriteMask.Blue) != 0)
        {
            result |= SDL.GPUColorComponentFlags.B;
        }
        if ((mask & SdlGpuColorWriteMask.Alpha) != 0)
        {
            result |= SDL.GPUColorComponentFlags.A;
        }
        return result;
    }
}
