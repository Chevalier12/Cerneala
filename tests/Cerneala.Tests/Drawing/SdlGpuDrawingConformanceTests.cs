using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.Playground;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.Tests.UI.Hosting;
using SkiaSharp;
using Xunit.Abstractions;

namespace Cerneala.Tests.Drawing;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class SdlGpuDrawingConformanceTests : IDisposable
{
    private const double MaximumMeanAbsoluteError = 1.0;
    private const int MaximumPercentile99 = 10;
    private const int MaximumAbsoluteDelta = 49;
    private readonly ITestOutputHelper output;

    public SdlGpuDrawingConformanceTests(ITestOutputHelper output)
    {
        this.output = output;
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [SdlDrawingNativeFact]
    [Trait("Category", "Native")]
    public void DrawingApiShowcaseMatchesWindowsDxPixelThresholds()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string directory = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-sdlgpu-drawing-conformance-{Guid.NewGuid():N}");
        string windowsPath = Path.Combine(directory, "windowsdx.png");
        string sdlPath = Path.Combine(directory, "sdlgpu.png");

        try
        {
            Directory.CreateDirectory(directory);
            CaptureWindowsDx(windowsPath);
            CaptureSdlGpu(sdlPath);

            using SKBitmap windows = SKBitmap.Decode(windowsPath);
            using SKBitmap sdl = SKBitmap.Decode(sdlPath);
            PixelDiffResult result = Compare(windows, sdl);
            WriteDiffArtifacts(directory, windows, sdl, result);
            output.WriteLine($"Canonical WindowsDX/SDL_GPU RGBA diff: {result}");

            Assert.True(
                result.MeanAbsoluteError <= MaximumMeanAbsoluteError &&
                result.Percentile99 <= MaximumPercentile99 &&
                result.MaximumAbsoluteDelta <= MaximumAbsoluteDelta,
                $"SDL_GPU drawing diff exceeded the canonical RGBA thresholds. {result}; artifacts: {directory}");
        }
        catch
        {
            PreserveFailureArtifacts(directory);
            throw;
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static void CaptureWindowsDx(string path)
    {
        using DesignPreviewSession session = DesignPreviewSession.Create(
            new Application(),
            static () => new DrawingApiShowcase(),
            width: 500,
            height: 464,
            renderScale: 1);
        session.Pump(TimeSpan.FromMilliseconds(16));
        session.Pump(TimeSpan.FromMilliseconds(16));
        session.SaveScreenshot(path);
    }

    private static void CaptureSdlGpu(string path)
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        Window window = new()
        {
            Title = "SDL_GPU drawing conformance",
            Width = 500,
            Height = 464,
            Content = new DrawingApiShowcase()
        };

        try
        {
            runtime.Show(window, modal: false);
            window.SaveScreenshot(path);
        }
        finally
        {
            runtime.Close(window, force: true);
        }
    }

    private static PixelDiffResult Compare(SKBitmap expected, SKBitmap actual)
    {
        Assert.Equal((expected.Width, expected.Height), (actual.Width, actual.Height));

        int[] deltas = new int[checked(expected.Width * expected.Height * 4)];
        long total = 0;
        int maximum = 0;
        int cursor = 0;
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                SKColor left = expected.GetPixel(x, y);
                SKColor right = actual.GetPixel(x, y);
                Add(Math.Abs(left.Red - right.Red));
                Add(Math.Abs(left.Green - right.Green));
                Add(Math.Abs(left.Blue - right.Blue));
                Add(Math.Abs(left.Alpha - right.Alpha));
            }
        }

        Array.Sort(deltas);
        int percentileIndex = (int)Math.Ceiling(deltas.Length * 0.99) - 1;
        return new PixelDiffResult(
            (double)total / deltas.Length,
            deltas[Math.Clamp(percentileIndex, 0, deltas.Length - 1)],
            maximum);

        void Add(int delta)
        {
            deltas[cursor++] = delta;
            total += delta;
            maximum = Math.Max(maximum, delta);
        }
    }

    private static void PreserveFailureArtifacts(string sourceDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        string artifactRoot = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            Path.GetFileName(sourceDirectory));
        Directory.CreateDirectory(artifactRoot);
        foreach (string source in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(source, Path.Combine(artifactRoot, Path.GetFileName(source)), overwrite: true);
        }
    }

    private static void WriteDiffArtifacts(
        string directory,
        SKBitmap expected,
        SKBitmap actual,
        PixelDiffResult result)
    {
        File.WriteAllText(
            Path.Combine(directory, "pixel-diff.txt"),
            $"{result}{Environment.NewLine}" +
            $"Thresholds: MAE<={MaximumMeanAbsoluteError:F1}, " +
            $"P99<={MaximumPercentile99}, max<={MaximumAbsoluteDelta}{Environment.NewLine}");

        using SKBitmap heatmap = new(
            expected.Width,
            expected.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Opaque);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                SKColor left = expected.GetPixel(x, y);
                SKColor right = actual.GetPixel(x, y);
                byte delta = (byte)Math.Max(
                    Math.Abs(left.Red - right.Red),
                    Math.Max(
                        Math.Abs(left.Green - right.Green),
                        Math.Max(
                            Math.Abs(left.Blue - right.Blue),
                            Math.Abs(left.Alpha - right.Alpha))));
                heatmap.SetPixel(x, y, new SKColor(delta, 0, 0, 255));
            }
        }

        using SKImage image = SKImage.FromBitmap(heatmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream output = File.Create(Path.Combine(directory, "heatmap.png"));
        data.SaveTo(output);
    }

    private readonly record struct PixelDiffResult(
        double MeanAbsoluteError,
        int Percentile99,
        int MaximumAbsoluteDelta)
    {
        public override string ToString() =>
            $"MAE={MeanAbsoluteError:F4}, P99={Percentile99}, max={MaximumAbsoluteDelta}";
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class SdlDrawingNativeFactAttribute : FactAttribute
    {
        public SdlDrawingNativeFactAttribute()
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("CERNEALA_SDL_NATIVE_TESTS"),
                    "1",
                    StringComparison.Ordinal))
            {
                Skip = "Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured native Windows runner.";
            }
        }
    }
}
