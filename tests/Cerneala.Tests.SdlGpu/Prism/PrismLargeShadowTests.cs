using System.Security.Cryptography;
using System.Diagnostics;
using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Xunit.Abstractions;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismLargeShadowTests(ITestOutputHelper output)
{
    [SdlNativeTheory]
    [InlineData(64, false)]
    [InlineData(200, false)]
    [InlineData(64, true)]
    [InlineData(200, true)]
    public void LargeShadowHasRequestedExtentAndContinuousFalloff(int radius, bool spread)
    {
        int side = radius * 4 + 128;
        int left = radius + 32;
        DrawRect shape = new(left, left, 128, 128);
        using SdlDrawingFixture fixture = new(side, side);
        Color[] pixels = fixture.Render(Shadow(shape, radius, spread), Color.Black);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        int centerY = left + 64;
        int near = Red(radius / 4);
        int middle = Red(radius / 2);
        int far = Red(radius * 3 / 4);
        output.WriteLine($"radius={radius} spread={spread}: near={near}, middle={middle}, far={far}, outside={Red(radius + 16)}");
        Assert.True(far > 0, "Requested shadow coverage beyond the 32-device-pixel kernel limit is missing.");
        Assert.Equal(0, Red(radius + 16));
        if (!spread)
        {
            Assert.True(near > middle && middle > far, "Gaussian coverage must fall continuously with distance.");
            Assert.InRange(Math.Abs(Red(radius / 2) - pixels[(left - radius / 2 - 1) * side + left + 64].R), 0, 3);
        }
        else
        {
            Assert.True(far >= 240, "A large spread must retain solid interior coverage.");
        }

        int Red(int distance) => pixels[centerY * side + left - distance - 1].R;
    }

    [SdlNativeTheory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(32)]
    public void SmallShadowPixelsRemainDeterministic(int radius)
    {
        using SdlDrawingFixture fixture = new(192, 192);
        DrawCommandList commands = Shadow(new(64, 64, 64, 64), radius, false);
        Color[] first = fixture.Render(commands, Color.Black);
        Color[] second = fixture.Render(commands, Color.Black);
        Assert.Equal(first, second);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        byte[] bytes = first.SelectMany(pixel => new[] { pixel.R, pixel.G, pixel.B, pixel.A }).ToArray();
        output.WriteLine($"radius={radius} SHA256={Convert.ToHexString(SHA256.HashData(bytes))}");
    }

    [SdlNativeFact]
    public void BoxReductionAndBilinearReconstructionPreserveTheUnpaddedDomain()
    {
        const int width = 9, height = 5;
        using SdlPrismKernelFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        SdlGpuDrawingResources resources = session.DrawingResources;
        foreach (int factor in new[] { 2, 7, 16 })
        {
            int reducedWidth = (width + factor - 1) / factor;
            int reducedHeight = (height + factor - 1) / factor;
            object key = new();
            Vector4[] pixels = Enumerable.Range(0, width * height)
                .Select(index => new Vector4((index % 5) / 4f)).ToArray();
            using SdlGpuPrismSurfaceLease horizontal = resources.PrismResources.RentSurface(
                session.WindowIdentity, reducedWidth, height, SdlGpuTextureFormat.R16G16B16A16Float, false);
            using SdlGpuPrismSurfaceLease reduced = resources.PrismResources.RentSurface(
                session.WindowIdentity, reducedWidth, reducedHeight, SdlGpuTextureFormat.R16G16B16A16Float, false);
            using SdlGpuPrismSurfaceLease reconstructed = resources.PrismResources.RentSurface(
                session.WindowIdentity, width, height, SdlGpuTextureFormat.R16G16B16A16Float, false);
            session.BeginFrame(Color.Transparent);
            try
            {
                nint source = resources.GetOrCreateHalfVector4Texture(session, key, width, height, pixels).Handle;
                SdlGpuPrismUniforms uniforms = SdlPrismKernelFixture.CreateUniforms(98, reducedWidth, height);
                uniforms[24] = new Vector4(width, height, factor, 1);
                fixture.Draw(horizontal.Target, source, source, source, uniforms);
                uniforms = SdlPrismKernelFixture.CreateUniforms(98, reducedWidth, reducedHeight);
                uniforms[24] = new Vector4(reducedWidth, height, factor, 0);
                source = horizontal.Target.SampleTexture;
                fixture.Draw(reduced.Target, source, source, source, uniforms);
                uniforms = SdlPrismKernelFixture.CreateUniforms(0, reducedWidth, reducedHeight);
                uniforms[1] = new Vector4(width / (float)(reducedWidth * factor),
                    height / (float)(reducedHeight * factor), 0, 0);
                source = reduced.Target.SampleTexture;
                fixture.Draw(reconstructed.Target, source, source, source, uniforms);
            }
            finally
            {
                session.CompleteFrame(present: false);
                resources.InvalidateTexture(key);
            }
            // Independent CPU box sums, including the zero-padded trailing cell.
            float[] expected = new float[reducedWidth * reducedHeight];
            for (int y = 0; y < reducedHeight; y++)
            for (int x = 0; x < reducedWidth; x++)
            {
                float sum = 0;
                for (int j = y * factor; j < Math.Min((y + 1) * factor, height); j++)
                for (int i = x * factor; i < Math.Min((x + 1) * factor, width); i++)
                    sum += pixels[j * width + i].W;
                expected[y * reducedWidth + x] = sum / (factor * factor);
            }
            Vector4[] actualReduced = fixture.ReadPixels(reduced.Target);
            for (int index = 0; index < expected.Length; index++)
                Assert.InRange(Math.Abs(actualReduced[index].W - expected[index]), 0, 0.001f);
            Vector4[] actual = fixture.ReadPixels(reconstructed.Target);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float sx = Math.Clamp((x + 0.5f) / factor - 0.5f, 0, reducedWidth - 1);
                float sy = Math.Clamp((y + 0.5f) / factor - 0.5f, 0, reducedHeight - 1);
                int x0 = (int)sx, y0 = (int)sy;
                int x1 = Math.Min(x0 + 1, reducedWidth - 1), y1 = Math.Min(y0 + 1, reducedHeight - 1);
                float top = float.Lerp(expected[y0 * reducedWidth + x0], expected[y0 * reducedWidth + x1], sx - x0);
                float bottom = float.Lerp(expected[y1 * reducedWidth + x0], expected[y1 * reducedWidth + x1], sx - x0);
                Assert.InRange(Math.Abs(actual[y * width + x].W - float.Lerp(top, bottom, sy - y0)), 0, 0.002f);
            }
        }
    }

    [SdlNativeFact]
    public void WarmedRadiusSweepReportsUncachedSubmissionAndCompletionCost()
    {
        const int frames = 16, warmup = 8;
        using SdlDrawingFixture fixture = new(1024, 1024);
        using SdlGpuPrismExecutor executor = new(fixture.Session, fixture.Backend);
        SdlGpuPrismDeviceResources resources = fixture.Session.DrawingResources.PrismResources;
        output.WriteLine($"SDL_GPU; OS={Environment.OSVersion}; cores={Environment.ProcessorCount}; target=1024x1024; " +
            $"warmup={warmup}; samples={frames}; retained outputs invalidated each frame; " +
            "GPU timestamp queries unavailable: completion includes CPU submission and synchronized readback, not isolated GPU time.");
        foreach (bool spread in new[] { false, true })
        foreach (int radius in new[] { 32, 64, 200 })
        {
            DrawCommandList commands = Shadow(new(448, 448, 128, 128), radius, spread);
            DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
            for (int frame = 0; frame < warmup; frame++) Render();
            long created = resources.CreatedSurfaceCount;
            double[] cpu = new double[frames], completion = new double[frames];
            long allocated = 0;
            for (int frame = 0; frame < frames; frame++)
            {
                resources.Invalidate(PrismCacheInvalidation.All);
                fixture.Session.BeginFrame(Color.Black);
                long started = Stopwatch.GetTimestamp();
                try
                {
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    executor.Execute(commands, in context);
                    allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                    cpu[frame] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
                }
                finally { fixture.Session.CompleteFrame(present: false); }
                _ = fixture.Session.CapturePresentedFrame(); // Synchronize all submitted GPU work.
                completion[frame] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
            }
            Assert.Equal(created, resources.CreatedSurfaceCount);
            Assert.Equal(0, executor.Diagnostics.Count);
            Assert.True(executor.Diagnostics.Counters.PassCount > 1); // Not a retained hit.
            output.WriteLine($"radius={radius} spread={spread} passes={executor.Diagnostics.Counters.PassCount} " +
                $"cpu-mean-us={cpu.Average():F3} cpu-p95-us={cpu.Order().ElementAt(15):F3} " +
                $"completion-upper-bound-mean-us={completion.Average():F3} completion-p95-us={completion.Order().ElementAt(15):F3} " +
                $"executor-managed-bytes/frame={allocated / frames} new-surfaces=0");

            void Render()
            {
                resources.Invalidate(PrismCacheInvalidation.All);
                fixture.Session.BeginFrame(Color.Black);
                try { executor.Execute(commands, in context); }
                finally { fixture.Session.CompleteFrame(present: false); }
                _ = fixture.Session.CapturePresentedFrame();
            }
        }
    }

    private static DrawCommandList Shadow(DrawRect shape, int radius, bool spread)
    {
        PrismLayerDefinition layer = new(new(1), "Large shadow", styles: [new(PrismStyleId.DropShadow)]);
        PrismDrawScope scope = PrismTestData.Scope(new("Large shadow", [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb), bounds: shape);
        var state = Assert.Single(scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry((int)PrismStyleId.DropShadow);
        Number("Size", spread ? 0 : (radius - 0.25f) / 1.5f);
        Number("Spread", spread ? radius : 0);
        Number("Distance", 0);
        Number("Opacity", 1);
        GeneratedMarkup.SetPrismStyleColor(state, entry.StableId,
            entry.Properties.Single(property => property.Name == "Color").TypeSlot, Color.White);
        GeneratedMarkup.SetPrismStyleInteger(state, entry.StableId,
            entry.Properties.Single(property => property.Name == "BlendMode").TypeSlot,
            PrismCatalogRuntime.ResolveSymbol("BlendMode", "Normal"));
        return PrismTestData.Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(shape, Color.White), DrawCommand.EndPrism());

        void Number(string name, float value) => GeneratedMarkup.SetPrismStyleNumber(state, entry.StableId,
            entry.Properties.Single(property => property.Name == name).TypeSlot, value);
    }
}
