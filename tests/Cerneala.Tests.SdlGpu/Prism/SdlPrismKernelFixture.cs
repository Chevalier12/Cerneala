using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.Drawing.SdlGpu;

/// <summary>Exercises the shipped SDL Prism shader on floating-point GPU targets and reads back RGBA16F values.</summary>
internal sealed class SdlPrismKernelFixture : IDisposable
{
    private readonly SdlDrawingFixture fixture = new();
    private readonly object dissolveThresholdKey = new();
    private static readonly byte[] DissolveThresholdPixels = CreateDissolveThresholdPixels();
    internal SdlGpuWindowGraphicsSession Session => fixture.Session;

    public nint FloatPipeline => Session.DrawingResources.PrismResources.GetPipeline(
        SdlGpuTextureFormat.R16G16B16A16Float);

    public Vector4[] RunCatalog(
        PrismCatalogFilterPlan plan,
        Vector4[] pixels,
        int width,
        int height,
        PrismColorProfile profile = PrismColorProfile.LinearSrgb,
        Action<SdlGpuPrismUniforms>? configure = null,
        SdlPrismTextureData? secondary = null,
        int? passCount = null,
        DrawSamplingMode sampling = DrawSamplingMode.Linear)
    {
        int count = passCount ?? plan.Passes.Length;
        Assert.InRange(count, 1, plan.Passes.Length);
        return RunPasses(pixels, width, height, count, index =>
        {
            PrismCatalogFilterPass pass = plan.Passes[index];
            SdlGpuPrismUniforms uniforms = CreateUniforms(
                SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(plan.Filter, pass.Iteration), width, height);
            uniforms[23] = new Vector4((int)plan.Filter, (int)profile, (int)plan.Primitive, 0);
            uniforms[24] = plan.Options0;
            uniforms[25] = plan.Options1;
            uniforms[26] = plan.Options2;
            uniforms[27] = plan.Options3;
            uniforms[28] = plan.Options4;
            uniforms[29] = plan.Options5;
            uniforms[30] = plan.Options6;
            uniforms[31] = plan.Options7;
            uniforms[32] = plan.Options8;
            uniforms[33] = new Vector4(pass.RadiusX, pass.RadiusY,
                (int)pass.Kind + (pass.Iteration * 4),
                SdlGpuPrismKernelSelector.ResolveBlendMode(plan.BlendMode));
            configure?.Invoke(uniforms);
            return uniforms;
        }, secondary, sampling, SdlGpuTextureFormat.R16G16B16A16Float);
    }

    public Vector4[] RunKernel(
        int kernel,
        Vector4[] pixels,
        int width,
        int height,
        Action<SdlGpuPrismUniforms>? configure = null,
        SdlPrismTextureData? secondary = null,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        SdlGpuTextureFormat outputFormat = SdlGpuTextureFormat.R16G16B16A16Float,
        IReadOnlyDictionary<uint, SdlPrismTextureData>? inputs = null) =>
        RunPasses(pixels, width, height, 1, _ =>
        {
            SdlGpuPrismUniforms uniforms = CreateUniforms(kernel, width, height);
            configure?.Invoke(uniforms);
            return uniforms;
        }, secondary, sampling, outputFormat, inputs);

    internal static SdlGpuPrismUniforms CreateUniforms(int kernel, int width, int height)
    {
        SdlGpuPrismUniforms uniforms = new();
        uniforms.Reset();
        uniforms[0] = new Vector4(1, 1f / width, 1f / height, 0);
        uniforms[1] = new Vector4(1, 1, 0, 0);
        uniforms[34] = new Vector4(width, height, kernel, 0);
        return uniforms;
    }

    internal Vector4[] RunPasses(
        Vector4[] pixels,
        int width,
        int height,
        int passCount,
        Func<int, SdlGpuPrismUniforms> configure,
        SdlPrismTextureData? secondary,
        DrawSamplingMode sampling,
        SdlGpuTextureFormat outputFormat,
        IReadOnlyDictionary<uint, SdlPrismTextureData>? inputs = null)
    {
        SdlGpuDrawingResources resources = Session.DrawingResources;
        SdlGpuPrismDeviceResources prism = resources.PrismResources;
        object sourceKey = new();
        object secondaryKey = new();
        Dictionary<uint, nint> textures = [];
        List<object> inputKeys = [];
        using SdlGpuPrismSurfaceLease first = prism.RentSurface(
            Session.WindowIdentity, width, height, outputFormat, false);
        using SdlGpuPrismSurfaceLease second = prism.RentSurface(
            Session.WindowIdentity, width, height, outputFormat, false);
        SdlGpuRenderTarget target = first.Target;
        SdlGpuRenderTarget output = target;
        Session.BeginFrame(Color.Transparent);
        try
        {
            nint original = resources.GetOrCreateHalfVector4Texture(
                Session, sourceKey, width, height, pixels).Handle;
            nint secondInput = secondary is null ? original : resources.GetOrCreateHalfVector4Texture(
                Session, secondaryKey, secondary.Width, secondary.Height, secondary.Pixels).Handle;
            if (inputs is not null)
            {
                foreach ((uint slot, SdlPrismTextureData data) in inputs)
                {
                    Assert.InRange(slot, 0u, 14u);
                    object key = new();
                    inputKeys.Add(key);
                    textures.Add(slot, resources.GetOrCreateHalfVector4Texture(
                        Session, key, data.Width, data.Height, data.Pixels).Handle);
                }
            }
            nint current = original;
            for (int passIndex = 0; passIndex < passCount; passIndex++)
            {
                Draw(target, current, secondInput, original, configure(passIndex), sampling, textures);
                output = target;
                current = target.SampleTexture;
                target = ReferenceEquals(target, first.Target) ? second.Target : first.Target;
            }
        }
        finally
        {
            Session.CompleteFrame(present: false);
            resources.InvalidateTexture(sourceKey);
            resources.InvalidateTexture(secondaryKey);
            foreach (object key in inputKeys) { resources.InvalidateTexture(key); }
        }
        return ReadPixels(output);
    }

    internal void Draw(
        SdlGpuRenderTarget target,
        nint source,
        nint secondary,
        nint original,
        SdlGpuPrismUniforms uniforms,
        DrawSamplingMode sourceSampling = DrawSamplingMode.Linear,
        IReadOnlyDictionary<uint, nint>? inputs = null)
    {
        ISdlApi api = Session.Api;
        SdlGpuDrawingResources resources = Session.DrawingResources;
        nint white = resources.PrismResources.GetWhiteTexture(Session);
        nint dissolveThreshold = resources.GetOrCreateTexture(Session, dissolveThresholdKey,
            PrismDissolveBlend.ThresholdSize, PrismDissolveBlend.ThresholdSize,
            DissolveThresholdPixels).Handle;
        nint spatterPoints = (int)uniforms[34].Z is 35 or 36
            ? resources.PrismResources.GetSpatterPointTexture(Session) : white;
        Session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Clear);
        nint pass = Session.ActiveRenderPass;
        api.BindGpuGraphicsPipeline(pass, resources.PrismResources.GetPipeline(target.ColorFormat));
        Span<float> viewport = [target.PixelWidth, target.PixelHeight, 0, 0, 0, 0, target.PixelWidth, target.PixelHeight];
        api.PushGpuVertexUniformData(Session.ActiveCommandBuffer, 0, MemoryMarshal.AsBytes(viewport));
        api.PushGpuFragmentUniformData(Session.ActiveCommandBuffer, 0, uniforms.Pack());
        for (uint slot = 0; slot < 15; slot++)
        {
            nint texture = slot switch
            {
                0 => source,
                1 => secondary,
                6 or 12 => original,
                7 => dissolveThreshold,
                14 => spatterPoints,
                _ => white
            };
            if (inputs is not null && inputs.TryGetValue(slot, out nint supplied))
            {
                texture = supplied;
            }
            DrawSamplingMode sampling = slot is 7 or 9 or 10 or 11 or 13 or 14
                ? DrawSamplingMode.Point : slot == 0 ? sourceSampling : DrawSamplingMode.Linear;
            DrawAddressMode address = slot is 4 or 7 or 9 ? DrawAddressMode.Wrap : DrawAddressMode.Clamp;
            api.BindGpuFragmentSampler(pass, slot, new SdlGpuTextureSamplerBinding(
                texture, resources.GetSampler(sampling, address)));
        }
        api.SetGpuScissor(pass, new SdlRect(0, 0, target.PixelWidth, target.PixelHeight));
        api.SetGpuStencilReference(pass, 0);
        api.DrawGpuPrimitives(pass, 3, 0);
    }

    internal Vector4[] ReadPixels(SdlGpuRenderTarget target)
    {
        ISdlApi api = Session.Api;
        nint device = Session.Device;
        int bytesPerPixel = target.ColorFormat switch
        {
            SdlGpuTextureFormat.R16G16B16A16Float => 8,
            SdlGpuTextureFormat.R32G32B32A32Float => 16,
            SdlGpuTextureFormat.R8G8B8A8Unorm => 4,
            _ => throw new NotSupportedException($"Unsupported kernel test target: {target.ColorFormat}.")
        };
        int rowAlignment = 256 / bytesPerPixel;
        int pixelsPerRow = checked(((target.PixelWidth + rowAlignment - 1) / rowAlignment) * rowAlignment);
        int byteCount = checked(pixelsPerRow * target.PixelHeight * bytesPerPixel);
        nint transfer = 0, command = 0, copy = 0, fence = 0, mapped = 0;
        try
        {
            transfer = Require(api.CreateGpuTransferBuffer(device,
                new SdlGpuTransferBufferCreateInfo(SdlGpuTransferBufferUsage.Download, (uint)byteCount)));
            command = Require(api.AcquireGpuCommandBuffer(device));
            copy = Require(api.BeginGpuCopyPass(command));
            api.DownloadFromGpuTexture(copy,
                new SdlGpuTextureRegion(target.SampleTexture, (uint)target.PixelWidth, (uint)target.PixelHeight),
                new SdlGpuTextureTransferInfo(transfer, 0, (uint)pixelsPerRow, (uint)target.PixelHeight));
            api.EndGpuCopyPass(copy);
            copy = 0;
            fence = Require(api.SubmitGpuCommandBufferAndAcquireFence(command));
            command = 0;
            Assert.True(api.WaitForGpuFence(device, fence), api.GetError());
            mapped = Require(api.MapGpuTransferBuffer(device, transfer, cycle: false));
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(mapped, bytes, 0, byteCount);
            ReadOnlySpan<Half> halves = MemoryMarshal.Cast<byte, Half>(bytes);
            ReadOnlySpan<float> singles = MemoryMarshal.Cast<byte, float>(bytes);
            Vector4[] result = new Vector4[target.PixelWidth * target.PixelHeight];
            for (int y = 0; y < target.PixelHeight; y++)
            {
                for (int x = 0; x < target.PixelWidth; x++)
                {
                    int offset = ((y * pixelsPerRow) + x) * 4;
                    result[(y * target.PixelWidth) + x] = bytesPerPixel == 4
                        ? new Vector4(bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]) / 255f
                        : bytesPerPixel == 8
                        ? new Vector4((float)halves[offset], (float)halves[offset + 1],
                            (float)halves[offset + 2], (float)halves[offset + 3])
                        : new Vector4(singles[offset], singles[offset + 1], singles[offset + 2], singles[offset + 3]);
                }
            }
            return result;
        }
        finally
        {
            if (mapped != 0) { api.UnmapGpuTransferBuffer(device, transfer); }
            if (copy != 0) { api.EndGpuCopyPass(copy); }
            if (command != 0) { api.CancelGpuCommandBuffer(command); }
            if (fence != 0) { api.ReleaseGpuFence(device, fence); }
            if (transfer != 0) { api.ReleaseGpuTransferBuffer(device, transfer); }
        }
    }

    private nint Require(nint handle) => handle != 0 ? handle : throw SdlApiError.Create(Session.Api, "Kernel test GPU operation");

    private static byte[] CreateDissolveThresholdPixels()
    {
        ReadOnlySpan<byte> ranks = PrismDissolveBlend.Thresholds;
        byte[] pixels = new byte[ranks.Length * 4];
        for (int i = 0; i < ranks.Length; i++)
        {
            pixels[(i * 4) + 3] = ranks[i];
        }
        return pixels;
    }

    public static Vector4 QuantizeHalf(Vector4 value) => new(
        (float)(Half)value.X, (float)(Half)value.Y, (float)(Half)value.Z, (float)(Half)value.W);

    public void Dispose() => fixture.Dispose();
}

internal sealed record SdlPrismTextureData(int Width, int Height, Vector4[] Pixels);
