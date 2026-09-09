using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Text;
using SkiaSharp;

namespace Cerneala.Benchmarks;

// Characterization, not an acceptance gate. Run each case in a fresh process.
internal static class TextCharacterizationRunner
{
    private const int Repetitions = 256;
    private const float FontSize = 16;
    private const string FontFamily = "Arial";

    internal static void Run(string scenario, string reportPath)
    {
        object result = scenario switch
        {
            "pipeline-32" => Pipeline(32),
            "pipeline-512" => Pipeline(512),
            "cache-shape-32" => Cache("shape", 32),
            "cache-shape-512" => Cache("shape", 512),
            "cache-blob-32" => Cache("blob", 32),
            "cache-blob-512" => Cache("blob", 512),
            "cache-layout-32" => Cache("layout", 32),
            "cache-layout-512" => Cache("layout", 512),
            "native-static" or "native-unique" or "native-cycle" or
                "native-phases" or "native-first-view" => Native(scenario),
            _ => throw new ArgumentException("Unknown text characterization case.", nameof(scenario))
        };
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var report = new
        {
            Schema = "cerneala-text-characterization-v1",
            Scenario = scenario,
            TimestampUtc = DateTimeOffset.UtcNow,
            ProcessId = Environment.ProcessId,
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            LogicalProcessors = Environment.ProcessorCount,
            Stopwatch.Frequency,
            RequestedFont = FontFamily,
            FontSize,
            ActualFont = ((SkiaFont)new SystemFontSource().LoadFont(FontFamily, FontSize)).Typeface.FamilyName,
            Binaries = new[] { typeof(TextCharacterizationRunner).Assembly, typeof(SkiaTextShaper).Assembly,
                typeof(SdlGpuDrawingBackend).Assembly, typeof(SKTypeface).Assembly }
                .Select(assembly => new
                {
                    Path = assembly.Location,
                    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
                }).ToArray(),
            Result = result
        };
        File.WriteAllText(fullPath, JsonSerializer.Serialize(report,
            new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Console.WriteLine($"{scenario}: characterization written to {fullPath}");
    }

    private static object Pipeline(int length)
    {
        List<object> rows = [];
        rows.Add(Cpu("observer-control", Repetitions, static _ => { }));
        SystemFontSource? source = null;
        SkiaFont font = null!;
        rows.Add(Cpu("font-source-and-first-resolve", 1, _ =>
        {
            source = new SystemFontSource();
            font = (SkiaFont)source.LoadFont(FontFamily, FontSize);
        }));
        rows.Add(Cpu("font-resolve-repeat", Repetitions,
            _ => font = (SkiaFont)source!.LoadFont(FontFamily, FontSize)));
        DrawTextRun run = new(font, Input(0, length), FontSize);
        SkiaTextShaper shaper = new();
        TextShapeResult shape = default;
        rows.Add(Cpu("shape-first", 1, _ => shape = shaper.Shape(run)));
        rows.Add(Cpu("shape-repeat", Repetitions, _ => shape = shaper.Shape(run)));
        rows.Add(Cpu("blob-first-shape-already-warm", 1, _ => RentBlob(font, shape)));
        rows.Add(Cpu("blob-repeat", Repetitions, _ => RentBlob(font, shape)));
        SkiaTextRasterizer rasterizer = new(shaper);
        long pixels = 0;
        void Rasterize(int _)
        {
            RasterizedText[] layers = rasterizer.RasterizeSubpixelAtPhase(
                run, Color.White, 1, new DrawPoint(0.125f, 0.375f));
            try { foreach (RasterizedText layer in layers) { pixels += (long)layer.Width * layer.Height; } }
            finally { foreach (RasterizedText layer in layers) { layer.ReturnPixelBuffer(); } }
        }
        rows.Add(Cpu("raster-first-shape-and-blob-already-warm", 1, Rasterize));
        rows.Add(Cpu("raster-repeat-no-atlas", Repetitions, Rasterize));
        TextMeasurer measurer = new();
        TextAspect noWrap = new(FontFamily, FontSize);
        TextAspect wrap = new(FontFamily, FontSize, wrapping: TextWrapping.Wrap);
        TextMeasureResult? measurement = null;
        rows.Add(Cpu("layout-no-wrap-first-font-and-shape-warm", 1,
            _ => measurement = measurer.Measure(run.Text, noWrap, 240)));
        rows.Add(Cpu("layout-no-wrap-repeat", Repetitions,
            _ => measurement = measurer.Measure(run.Text, noWrap, 240)));
        rows.Add(Cpu("layout-wrap-first", 1,
            _ => measurement = measurer.Measure(run.Text, wrap, 240)));
        rows.Add(Cpu("layout-wrap-repeat", Repetitions,
            _ => measurement = measurer.Measure(run.Text, wrap, 240)));
        GC.KeepAlive(measurement);
        return new
        {
            TextLength = length, Rows = rows, RasterizedLayerPixels = pixels,
            measurer.LayoutCache.Count, measurer.LayoutCache.Hits, measurer.LayoutCache.Misses,
            Notes = "First means first measured call at that stage, not independent process-cold stages. " +
                "Instances/inputs are prepared outside samples except font-source-and-first-resolve. " +
                "Rows are not an additive startup breakdown. Raster repeats intentionally bypass the atlas. " +
                "No forced collection or custom JIT/tiering settings."
        };
    }

    private static object Cache(string kind, int length)
    {
        int capacity = kind == "layout" ? TextLayoutCache.DefaultCapacity : 4096;
        int inputCount = capacity * 2;
        SkiaFont font = (SkiaFont)new SystemFontSource().LoadFont(FontFamily, FontSize);
        SkiaTextShaper shaper = new();
        TextMeasurer measurer = new();
        TextAspect aspect = new(FontFamily, FontSize);
        DrawTextRun[] inputs = Enumerable.Range(0, inputCount)
            .Select(i => new DrawTextRun(font, Input(i, length), FontSize)).ToArray();
        // Blob-only measurement retains precomputed shapes in its baseline, so
        // shape creation and its arrays are not charged to the blob operation.
        TextShapeResult[] shapes = kind == "blob"
            ? inputs.Select(shaper.Shape).ToArray() : [];
        CpuSample[] fill = new CpuSample[capacity];
        CpuSample[] overflow = new CpuSample[capacity];
        CpuSample[] early = new CpuSample[Repetitions];
        CpuSample[] late = new CpuSample[Repetitions];
        void Access(int index)
        {
            if (kind == "shape") { _ = shaper.Shape(inputs[index]); }
            else if (kind == "blob") { RentBlob(font, shapes[index]); }
            else { _ = measurer.Measure(inputs[index].Text, aspect, float.PositiveInfinity); }
        }
        _ = Cpu("observer-control", 32, static _ => { });
        MemorySnapshot baseline = Memory();
        Measure(fill, Access);
        MemorySnapshot atCapacity = Memory();
        object firstInventory = Inventory();
        Measure(overflow, i => Access(capacity + i));
        MemorySnapshot afterOverflow = Memory();
        object secondInventory = Inventory();
        int layoutHitsBefore = measurer.LayoutCache.Hits;
        int layoutMissesBefore = measurer.LayoutCache.Misses;
        Measure(early, _ => Access(0));
        Measure(late, _ => Access(inputCount - 1));
        bool? earlyBlobReused = kind == "blob" ? BlobReused(font, shapes[0]) : null;
        bool? lateBlobReused = kind == "blob" ? BlobReused(font, shapes[^1]) : null;
        GC.KeepAlive(inputs);
        GC.KeepAlive(shapes);
        return new
        {
            Kind = kind, TextLength = length, InputCount = inputCount, ConfiguredEntryCapacity = capacity,
            Baseline = baseline, AtCapacity = atCapacity, AfterOverflow = afterOverflow,
            InventoryAtCapacity = firstInventory, InventoryAfterOverflow = secondInventory,
            Rows = new[]
            {
                CpuResult("fill-to-capacity", fill),
                CpuResult("add-another-capacity-of-unique-keys", overflow),
                CpuResult("replay-earliest-key", early),
                CpuResult("replay-latest-key", late)
            },
            EarlyBlobReused = earlyBlobReused, LateBlobReused = lateBlobReused,
            ReplayLayoutHits = measurer.LayoutCache.Hits - layoutHitsBefore,
            ReplayLayoutMisses = measurer.LayoutCache.Misses - layoutMissesBefore,
            Notes = "Inputs and sample arrays exist before the memory baseline. Memory snapshots force full GC " +
                "between, never within, timed blocks. Managed live/process-private deltas are process observations, " +
                "not exact ownership accounting; native allocators/JIT can retain memory. Shape glyph-array payload " +
                "is counted separately, excluding object headers, dictionaries and shared input strings. " +
                "Layout also exercises the shared shaping/font caches. Blob inputs include precomputed shapes. " +
                "The first replay of an evicted key can miss even when subsequent replays hit."
        };

        object Inventory() => new
        {
            Shape = ShapeInventory(font),
            BlobEntries = SkiaTextBlobCache.GetCachedEntryCount(font),
            LayoutEntries = measurer.LayoutCache.Count
        };
    }

    private static void RentBlob(SkiaFont font, TextShapeResult shape)
    {
        using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(font, FontSize, shape);
        GC.KeepAlive(lease.Value);
    }

    private static bool BlobReused(SkiaFont font, TextShapeResult shape)
    {
        using SkiaTextBlobCache.Lease first = SkiaTextBlobCache.Rent(font, FontSize, shape);
        using SkiaTextBlobCache.Lease second = SkiaTextBlobCache.Rent(font, FontSize, shape);
        return ReferenceEquals(first.Value, second.Value);
    }

    private static object ShapeInventory(SkiaFont font)
    {
        // Read-only, source-coupled inspection outside measured operations avoids
        // adding production diagnostics just for this benchmark.
        object table = typeof(SkiaTextShaper)
            .GetField("ShapesByTypeface", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        object?[] arguments = [font.Typeface, null];
        bool found = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, arguments)!;
        int count = 0;
        long glyphPayload = 0;
        if (found)
        {
            object cache = arguments[1]!;
            IEnumerable entries = (IEnumerable)cache.GetType()
                .GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cache)!;
            foreach (object entry in entries)
            {
                Lazy<TextShapeResult> value = (Lazy<TextShapeResult>)entry.GetType()
                    .GetProperty("Value")!.GetValue(entry)!;
                if (!value.IsValueCreated) { throw new InvalidOperationException("An unshaped cache entry was observed."); }
                TextShapeResult shape = value.Value;
                count++;
                glyphPayload += shape.GlyphIdBuffer.LongLength * sizeof(ushort) +
                    shape.GlyphPositionBuffer.LongLength * Unsafe.SizeOf<DrawPoint>();
            }
        }
        return new { Entries = count, GlyphArrayPayloadBytes = glyphPayload };
    }

    private static object Native(string scenario)
    {
        using NativeFixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Session.DrawingResources;
        SkiaFont font = (SkiaFont)new SystemFontSource().LoadFont(FontFamily, FontSize);
        int variantCount = scenario == "native-unique" ? 64 + Repetitions :
            scenario is "native-cycle" or "native-phases" ? 64 : 1;
        int labels = scenario == "native-first-view" ? 64 : 1;
        DrawCommandList[] commands = new DrawCommandList[variantCount];
        PrismFrameAnalysis[] analyses = new PrismFrameAnalysis[variantCount];
        for (int variant = 0; variant < variantCount; variant++)
        {
            commands[variant] = new DrawCommandList();
            for (int label = 0; label < labels; label++)
            {
                string text = scenario is "native-unique" or "native-cycle"
                    ? variant.ToString("D10", CultureInfo.InvariantCulture)
                    : labels == 1 ? "0123456789" : $"Label {label:D2}: 0123456789";
                float phaseX = scenario == "native-phases" ? (variant % 8) / 8f : 0.125f;
                float phaseY = scenario == "native-phases" ? (variant / 8) / 8f : 0.375f;
                DrawPoint position = new(8 + (label % 4) * 230 + phaseX, 28 + (label / 4) * 36 + phaseY);
                commands[variant].Add(DrawCommand.DrawText(new DrawTextRun(font, text, FontSize), position, Color.White));
            }
            analyses[variant] = new PrismFrameAnalyzer().Analyze(commands[variant]);
        }
        NativeSample[] first = new NativeSample[1];
        NativeSample[] population = new NativeSample[variantCount == 1 ? 16 : 63];
        NativeSample[] steady = new NativeSample[Repetitions];
        MemorySnapshot baseline = Memory();
        Sample(first, static _ => 0);
        Sample(population, i => variantCount == 1 ? 0 : i + 1);
        Sample(steady, i => scenario == "native-unique" ? 64 + i : i % variantCount);
        int pages = resources.TextAtlasPageCount;
        int entries = resources.TextAtlasEntryCount;
        int textures = resources.CachedTextureCount;
        MemorySnapshot retained = Memory();
        fixture.Dispose();
        int disposedEntries = resources.TextAtlasEntryCount;
        int disposedTextures = resources.CachedTextureCount;
        if (disposedEntries != 0 || disposedTextures != 0)
        {
            throw new InvalidOperationException("The benchmark left device-owned cached text/textures alive.");
        }
        MemorySnapshot disposed = Memory();
        return new
        {
            LabelsPerFrame = labels, DistinctPreparedVariants = variantCount,
            CoordinateScale = 1, Width = 960, Height = 640, Multisampling = false, Present = false,
            Rows = new[]
            {
                NativeResult("first-text-frame", first),
                NativeResult(variantCount == 1 ? "settling-replay" : "populate-remaining-63-variants", population),
                NativeResult(scenario == "native-unique" ? "steady-unique-text" : "steady-replay", steady)
            },
            AtlasPages = pages, AtlasChannelEntries = entries, CachedTextures = textures,
            AtlasCpuPixelPayloadBytes = (long)pages * 1024 * 1024 * 4,
            AtlasGpuPixelPayloadBytes = (long)pages * 1024 * 1024 * 4,
            Baseline = baseline, Retained = retained, Disposed = disposed,
            DisposedEntries = disposedEntries, DisposedTextures = disposedTextures,
            Notes = "Real native SDL_GPU draw/submit path, no simulated GPU. Font resolution, command creation, " +
                "Prism analysis, event pumping and JSON output are outside frame samples. No UI layout/input is measured. " +
                "First text frame includes first-use backend/JIT costs, not only text. No forced GC between frame blocks. " +
                "Present=false: no pacing/VSync; CPU wall time includes native submission/fence waits, not isolated GPU time. " +
                "Atlas byte counts are pixel payload only, not driver/metadata overhead. Phase case visits the full 8x8 grid " +
                "at a changing baseline, not a post-raster Motion transform. CPU timing components are not guaranteed disjoint."
        };

        void Sample(NativeSample[] samples, Func<int, int> select)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                int index = select(i);
                fixture.PumpEvents();
                GcCounts gcBefore = GcCounts.Read();
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                long started = Stopwatch.GetTimestamp();
                fixture.Session.BeginFrame(Color.Black);
                try
                {
                    DrawingFrameContext frame = new(analyses[index]);
                    fixture.Session.DrawingBackend.Render(commands[index], in frame);
                }
                finally { fixture.Session.CompleteFrame(present: false); }
                double wall = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
                long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                DrawingBackendFrameTiming timing = fixture.Backend.LastFrameTiming;
                SdlGpuDrawingFrameCounters counters = fixture.Backend.LastFrameCounters;
                if (counters.DrawCallCount <= 0) { throw new InvalidOperationException("No native text draw was submitted."); }
                samples[i] = new(wall, bytes, timing.TextRasterization.TotalMicroseconds,
                    timing.TextAtlasUpload.TotalMicroseconds, timing.CommandRendering.TotalMicroseconds,
                    timing.TextRequestCount, timing.RasterizedPixelCount, counters.DrawCallCount,
                    counters.VertexBytes + counters.IndexBytes, resources.TextAtlasPageCount,
                    resources.TextAtlasEntryCount, GcCounts.Read().Subtract(gcBefore));
            }
        }
    }

    private static object Cpu(string name, int count, Action<int> operation)
    {
        CpuSample[] samples = new CpuSample[count];
        Measure(samples, operation);
        return CpuResult(name, samples);
    }

    private static void Measure(CpuSample[] samples, Action<int> operation)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            GcCounts gcBefore = GcCounts.Read();
            long before = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            operation(i);
            double elapsed = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
            samples[i] = new(elapsed, GC.GetAllocatedBytesForCurrentThread() - before,
                GcCounts.Read().Subtract(gcBefore));
        }
    }

    private static object CpuResult(string name, CpuSample[] samples) => new
    {
        Name = name, Operations = samples.Length,
        TimeMicroseconds = Distribution(samples.Select(sample => sample.Microseconds)),
        MeanManagedBytes = samples.Average(sample => sample.ManagedBytes),
        TotalManagedBytes = samples.Sum(sample => sample.ManagedBytes),
        Collections = GcCounts.Total(samples.Select(sample => sample.Collections)),
        Samples = samples
    };

    private static object NativeResult(string name, NativeSample[] samples) => new
    {
        Name = name, Frames = samples.Length,
        WallMicroseconds = Distribution(samples.Select(sample => sample.WallMicroseconds)),
        RasterMicroseconds = Distribution(samples.Select(sample => sample.RasterMicroseconds)),
        UploadMicroseconds = Distribution(samples.Select(sample => sample.UploadMicroseconds)),
        CommandMicroseconds = Distribution(samples.Select(sample => sample.CommandMicroseconds)),
        MeanManagedBytes = samples.Average(sample => sample.ManagedBytes),
        RasterRequests = samples.Sum(sample => sample.RasterRequests),
        RasterizedLayerPixels = samples.Sum(sample => sample.RasterizedLayerPixels),
        MeanDrawCalls = samples.Average(sample => sample.DrawCalls),
        MeanGeometryUploadBytes = samples.Average(sample => sample.GeometryUploadBytes),
        Collections = GcCounts.Total(samples.Select(sample => sample.Collections)),
        Samples = samples
    };

    private static object Distribution(IEnumerable<double> values)
    {
        double[] ordered = values.Order().ToArray();
        return new
        {
            P50 = At(0.50), P95 = At(0.95), P99 = At(0.99), Maximum = ordered[^1], Mean = ordered.Average()
        };
        double At(double percentile) => ordered[Math.Clamp((int)Math.Ceiling(ordered.Length * percentile) - 1, 0, ordered.Length - 1)];
    }

    private static MemorySnapshot Memory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long managed = GC.GetTotalMemory(forceFullCollection: false);
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        return new(managed, process.PrivateMemorySize64, process.WorkingSet64);
    }

    private static string Input(int index, int length)
    {
        string prefix = index.ToString("D8", CultureInfo.InvariantCulture) + " ";
        return prefix + string.Concat(Enumerable.Repeat("text ", (length / 5) + 1))[..(length - prefix.Length)];
    }

    private readonly record struct CpuSample(double Microseconds, long ManagedBytes, GcCounts Collections);
    private readonly record struct MemorySnapshot(long ManagedLiveBytes, long ProcessPrivateBytes, long WorkingSetBytes);
    private readonly record struct NativeSample(double WallMicroseconds, long ManagedBytes,
        double RasterMicroseconds, double UploadMicroseconds, double CommandMicroseconds,
        int RasterRequests, long RasterizedLayerPixels, int DrawCalls, long GeometryUploadBytes,
        int AtlasPages, int AtlasChannelEntries, GcCounts Collections);

    private readonly record struct GcCounts(int Gen0, int Gen1, int Gen2)
    {
        internal static GcCounts Read() => new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
        internal GcCounts Subtract(GcCounts previous) =>
            new(Gen0 - previous.Gen0, Gen1 - previous.Gen1, Gen2 - previous.Gen2);
        internal static GcCounts Total(IEnumerable<GcCounts> counts) => new(
            counts.Sum(value => value.Gen0), counts.Sum(value => value.Gen1), counts.Sum(value => value.Gen2));
    }

    private sealed class NativeFixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory graphics;
        private readonly SdlWindowPlatform platform;
        private readonly IPlatformWindow window;
        private bool disposed;

        internal NativeFixture()
        {
            NativeSdlApi api = new();
            graphics = new(api, useMultisampling: false);
            platform = new(api, graphics, coordinateScaleOverride: 1);
            try
            {
                window = platform.CreateWindow(new Window
                {
                    Title = "Cerneala text characterization", Width = 960, Height = 640
                }, new Callbacks());
                window.Show();
                platform.PumpEvents();
                Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ??
                    throw new InvalidOperationException("No SDL_GPU graphics session.");
            }
            catch
            {
                platform.Dispose();
                graphics.Dispose();
                throw;
            }
        }

        internal SdlGpuWindowGraphicsSession Session { get; }
        internal SdlGpuDrawingBackend Backend => (SdlGpuDrawingBackend)Session.DrawingBackend;
        internal void PumpEvents() => platform.PumpEvents();

        public void Dispose()
        {
            if (disposed) { return; }
            disposed = true;
            window.Dispose();
            platform.Dispose();
            graphics.Dispose();
        }
    }

    private sealed class Callbacks : IWindowPlatformCallbacks
    {
        public void RequestClose() => throw new OperationCanceledException("Characterization window was closed.");
        public void ActivationChanged(bool active) { }
        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }
        public void RenderRequested() { }
    }
}
