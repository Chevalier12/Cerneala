using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using SkiaSharp;
using Xunit.Abstractions;

namespace Cerneala.Tests.Drawing.Prism;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PrismSdlGpuPixelConformanceTests : IDisposable
{
    private const double MaximumMeanAbsoluteError = 1.0;
    private const int MaximumPercentile99 = 10;
    private const int MaximumAbsoluteDelta = 49;
    private const int CellWidth = 96;
    private const int CellHeight = 72;
    private const int TargetWidth = 64;
    private const int TargetHeight = 40;
    private readonly ITestOutputHelper output;

    public PrismSdlGpuPixelConformanceTests(ITestOutputHelper output)
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

    public static IEnumerable<object[]> ResourceFreeCatalogEntries() =>
        PrismCatalog.Filters
            .Concat(PrismCatalog.Styles)
            .Where(static operation => !operation.RequiresResource)
            .Select(static operation => new object[] { operation.Symbol });

    [SdlPrismNativeTheory]
    [MemberData(nameof(ResourceFreeCatalogEntries))]
    [Trait("Category", "Native")]
    public void EveryResourceFreeCatalogEntryMatchesWindowsDxPixelThresholds(
        string symbol)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismCatalogOperationInfo operation = PrismCatalog.Filters
            .Concat(PrismCatalog.Styles)
            .Single(candidate => candidate.Symbol == symbol);
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-sdlgpu-prism-conformance-{symbol}-{Guid.NewGuid():N}");
        string windowsPath = Path.Combine(directory, "windowsdx.png");
        string sdlPath = Path.Combine(directory, "sdlgpu.png");

        try
        {
            Directory.CreateDirectory(directory);
            CaptureWindowsDx(
                windowsPath,
                [operation],
                CellWidth,
                CellHeight);
            CaptureSdlGpu(
                sdlPath,
                [operation],
                CellWidth,
                CellHeight);

            using SKBitmap windows = SKBitmap.Decode(windowsPath);
            using SKBitmap sdl = SKBitmap.Decode(sdlPath);
            Assert.Equal((windows.Width, windows.Height), (sdl.Width, sdl.Height));

            PixelDiffResult diff = Compare(
                windows,
                sdl,
                0,
                0,
                CellWidth,
                CellHeight);
            CatalogDiffResult result = new(symbol, diff);
            output.WriteLine($"{symbol}: {diff}");
            WriteDiffArtifacts(directory, windows, sdl, [result]);
            Assert.True(
                diff.Passes,
                $"SDL_GPU Prism pixel diff exceeded the canonical RGBA thresholds for " +
                $"{symbol}: {diff}" +
                $"; artifacts: {directory}");
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

    private static void CaptureWindowsDx(
        string path,
        IReadOnlyList<PrismCatalogOperationInfo> operations,
        int width,
        int height)
    {
        using CatalogScene scene = new(operations, width, height);
        using DesignPreviewSession session = DesignPreviewSession.Create(
            new Application(),
            () => scene,
            width,
            height,
            renderScale: 1);
        session.Pump(TimeSpan.FromMilliseconds(16));
        session.Pump(TimeSpan.FromMilliseconds(16));
        session.SaveScreenshot(path);
    }

    private static void CaptureSdlGpu(
        string path,
        IReadOnlyList<PrismCatalogOperationInfo> operations,
        int width,
        int height)
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        using CatalogScene scene = new(operations, width, height);
        Window window = CreateWindow("SDL_GPU Prism conformance", scene, width, height);
        try
        {
            runtime.Show(window, modal: false);
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
            window.SaveScreenshot(path);
        }
        finally
        {
            runtime.Close(window, force: true);
        }
    }

    private static Window CreateWindow(
        string title,
        UIElement content,
        int width,
        int height) =>
        new()
        {
            Title = title,
            Width = width,
            Height = height,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32_000,
            Top = -32_000,
            Content = content
        };

    private static PixelDiffResult Compare(
        SKBitmap expected,
        SKBitmap actual,
        int left,
        int top,
        int width,
        int height)
    {
        int[] deltas = new int[checked(width * height * 4)];
        long total = 0;
        int maximum = 0;
        int cursor = 0;
        for (int y = top; y < top + height; y++)
        {
            for (int x = left; x < left + width; x++)
            {
                SKColor expectedPixel = expected.GetPixel(x, y);
                SKColor actualPixel = actual.GetPixel(x, y);
                Add(Math.Abs(expectedPixel.Red - actualPixel.Red));
                Add(Math.Abs(expectedPixel.Green - actualPixel.Green));
                Add(Math.Abs(expectedPixel.Blue - actualPixel.Blue));
                Add(Math.Abs(expectedPixel.Alpha - actualPixel.Alpha));
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
        IReadOnlyList<CatalogDiffResult> results)
    {
        File.WriteAllLines(
            Path.Combine(directory, "pixel-diff.txt"),
            new[]
            {
                $"Thresholds: MAE<={MaximumMeanAbsoluteError:F1}, P99<={MaximumPercentile99}, max<={MaximumAbsoluteDelta}"
            }.Concat(results.Select(static result => $"{result.Symbol}: {result.Diff}")));

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
        using FileStream stream = File.Create(Path.Combine(directory, "heatmap.png"));
        data.SaveTo(stream);
    }

    private sealed class CatalogScene : Cerneala.UI.Layout.Panels.Canvas, IDisposable
    {
        private readonly List<IDisposable> attachments = [];

        public CatalogScene(
            IReadOnlyList<PrismCatalogOperationInfo> operations,
            int width,
            int height)
        {
            Width = width;
            Height = height;
            for (int index = 0; index < operations.Count; index++)
            {
                PrismCatalogOperationInfo operation = operations[index];
                float cellX = 0;
                float cellY = 0;

                PatternElement backdrop = new(isBackdrop: true)
                {
                    Width = CellWidth,
                    Height = CellHeight
                };
                SetLeft(backdrop, cellX);
                SetTop(backdrop, cellY);
                VisualChildren.Add(backdrop);

                PatternElement target = new(isBackdrop: false)
                {
                    Width = TargetWidth,
                    Height = TargetHeight
                };
                SetLeft(target, cellX + ((CellWidth - TargetWidth) / 2f));
                SetTop(target, cellY + ((CellHeight - TargetHeight) / 2f));
                VisualChildren.Add(target);
                attachments.Add(GeneratedMarkup.AttachPrism(
                    target,
                    () => CreateInstance(operation)));
            }
        }

        public void Dispose()
        {
            for (int index = attachments.Count - 1; index >= 0; index--)
            {
                attachments[index].Dispose();
            }
        }

        private static PrismInstance CreateInstance(PrismCatalogOperationInfo operation)
        {
            PrismLayerDefinition layer = operation.Kind == PrismCatalogOperationKind.Filter
                ? new PrismLayerDefinition(
                    new PrismNodeId(1),
                    operation.Symbol,
                    filters: [new PrismFilterDefinition((PrismFilterId)operation.StableId)])
                : new PrismLayerDefinition(
                    new PrismNodeId(1),
                    operation.Symbol,
                    styles: [new PrismStyleDefinition((PrismStyleId)operation.StableId)]);
            return new PrismInstance(new PrismCompositionDefinition(operation.Symbol, [layer]));
        }
    }

    private sealed class PatternElement(bool isBackdrop) : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            DrawRect bounds = new(
                context.Bounds.X,
                context.Bounds.Y,
                context.Bounds.Width,
                context.Bounds.Height);
            if (isBackdrop)
            {
                context.DrawingContext.FillRectangle(bounds, new Color(21, 29, 48, 255));
                context.DrawingContext.FillRectangle(
                    new DrawRect(bounds.X, bounds.Y, bounds.Width / 2, bounds.Height),
                    new Color(37, 71, 112, 255));
                context.DrawingContext.FillRectangle(
                    new DrawRect(bounds.X, bounds.Y + (bounds.Height / 2), bounds.Width, bounds.Height / 2),
                    new Color(68, 35, 82, 255));
                return;
            }

            context.DrawingContext.FillRoundedRectangle(
                bounds,
                new DrawCornerRadius(8),
                new Color(238, 116, 64, 255));
            context.DrawingContext.FillEllipse(
                new DrawRect(
                    bounds.X + 7,
                    bounds.Y + 6,
                    bounds.Width * 0.46f,
                    bounds.Height * 0.64f),
                new Color(53, 193, 184, 255));
            context.DrawingContext.FillRectangle(
                new DrawRect(
                    bounds.X + (bounds.Width * 0.52f),
                    bounds.Y + 9,
                    bounds.Width * 0.34f,
                    bounds.Height * 0.52f),
                new Color(246, 218, 90, 255));
            context.DrawingContext.DrawLine(
                new DrawPoint(bounds.X + 5, bounds.Bottom - 6),
                new DrawPoint(bounds.Right - 5, bounds.Y + 5),
                new Color(247, 247, 255, 255),
                2);
        }
    }

    private readonly record struct CatalogDiffResult(string Symbol, PixelDiffResult Diff);

    private readonly record struct PixelDiffResult(
        double MeanAbsoluteError,
        int Percentile99,
        int MaximumAbsoluteDelta)
    {
        public bool Passes =>
            MeanAbsoluteError <= MaximumMeanAbsoluteError &&
            Percentile99 <= MaximumPercentile99 &&
            MaximumAbsoluteDelta <= PrismSdlGpuPixelConformanceTests.MaximumAbsoluteDelta;

        public override string ToString() =>
            $"MAE={MeanAbsoluteError:F4}, P99={Percentile99}, max={MaximumAbsoluteDelta}";
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class SdlPrismNativeTheoryAttribute : TheoryAttribute
    {
        public SdlPrismNativeTheoryAttribute()
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
