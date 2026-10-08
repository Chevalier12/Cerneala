using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.Benchmarks;

// Timbre Motion cost (plan Motion, stage 3): the frozen core fixture (32
// looping voices, 4 streaming, LowPass + Delay, 480-frame blocks, 1000 warmup
// and 10000 measured mixer blocks on the paced device) with every voice
// animating Volume and Cutoff on the owning UIRoot. A 60 Hz UI loop runs
// UiHost.Update for the whole measurement; each frame samples all 64
// animated slots and publishes them. TIMBRE_MOTION_HIDDEN=1 collapses the
// owning control. Reported per process: mixer block cost/allocations/
// underruns, UI frame cost/allocations after 100 warmup frames, publications
// and layout/render work.
internal static class TimbreMotionBenchmarkRunner
{
    private const int WarmupBlocks = 1000;
    private const int MeasuredBlocks = 10000;
    private const int UiWarmupFrames = 100;
    private const int PreloadedVoices = 28;
    private const int StreamingVoices = 4;
    private const double BlockMilliseconds = 10.0;
    private static readonly TimeSpan FramePeriod = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    private static readonly TimeSpan AnimationLength = TimeSpan.FromSeconds(600);

    public static void Run(string reportPath)
    {
        bool hidden = Environment.GetEnvironmentVariable("TIMBRE_MOTION_HIDDEN") == "1";
        // Attribution controls: "none" keeps the voices and the UI loop without
        // any Motion; "visual" replaces the audio animations with one visual one.
        string control = Environment.GetEnvironmentVariable("TIMBRE_MOTION_CONTROL") ?? "audio";
        TimbreCoreBenchmarkRunner.PacedDevice device = new();
        TimbreCoreBenchmarkRunner.BlockRecorder recorder = new(WarmupBlocks, MeasuredBlocks);
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = device });
        runtime.BlockObserver = recorder;

        UIRoot root = new();
        root.SetTimbreRuntime(runtime);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(400, 300), TimbreRuntime = runtime });
        Button owner = new() { Content = "voices", Width = 120, Height = 40 };
        root.VisualChildren.Add(owner);
        Update(host);
        if (hidden)
        {
            owner.Visibility = Visibility.Collapsed;
            Update(host);
        }

        List<(TimbreSound Clip, TimbreParameter<float> Cutoff)> clips = [];
        for (int index = 0; index < 4; index++)
        {
            int seed = index;
            clips.Add(Clip(() => new TimbreCoreBenchmarkRunner.NoiseReader(2 * 48000, seed), $"preloaded-{index}", TimbreLoading.Preload));
        }

        (TimbreSound streaming, TimbreParameter<float> streamingCutoff) = Clip(() => new TimbreCoreBenchmarkRunner.NoiseReader(60 * 48000, seed: 99), "streaming", TimbreLoading.Streaming);
        foreach ((TimbreSound clip, _) in clips)
        {
            runtime.PrepareAsync(clip).GetAwaiter().GetResult();
        }

        TweenSpec<float> spec = new(AnimationLength, Easings.Linear);
        List<TimbrePlayback> voices = [];
        for (int index = 0; index < PreloadedVoices + StreamingVoices; index++)
        {
            (TimbreSound clip, TimbreParameter<float> cutoff) = index < PreloadedVoices ? clips[index % clips.Count] : (streaming, streamingCutoff);
            TimbrePlayback voice = owner.Timbre.Play(clip, start => start.Volume = 0.3f);
            if (control == "audio")
            {
                voice.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.5f).With(spec);
                voice.Motion().Animate(cutoff).To(3000f).With(spec);
            }
            voices.Add(voice);
        }

        if (control == "visual")
        {
            Border marker = new() { Width = 10, Height = 10 };
            root.VisualChildren.Add(marker);
            marker.Motion().Opacity.To(0.2f, spec);
        }

        device.Start();
        List<double> frameMilliseconds = [];
        long uiAllocated = 0;
        int allocatingFrames = 0;
        long measures = 0;
        long arranges = 0;
        long rendered = 0;
        long renderInvalidations = 0;
        long layoutInvalidations = 0;
        long valuesChanged = 0;
        long publicationsAtWarmup = 0;
        TimbreRuntimeDiagnostics? atMixerWarmup = null;
        long deviceUnderrunAtWarmup = 0;
        int frame = 0;
        Stopwatch cadence = Stopwatch.StartNew();
        while (!recorder.Completed.IsCompleted)
        {
            if (atMixerWarmup is null && recorder.WarmedUp.IsCompleted)
            {
                atMixerWarmup = runtime.GetDiagnostics();
                deviceUnderrunAtWarmup = device.UnderrunFrames;
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            FrameStats stats = Update(host);
            long elapsed = Stopwatch.GetTimestamp() - started;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            frame++;
            if (frame == UiWarmupFrames)
            {
                publicationsAtWarmup = runtime.GetDiagnostics().AnimatedPublications;
            }
            else if (frame > UiWarmupFrames)
            {
                frameMilliseconds.Add(elapsed * 1000.0 / Stopwatch.Frequency);
                uiAllocated += allocated;
                allocatingFrames += allocated != 0 ? 1 : 0;
                measures += stats.MeasureCalls;
                arranges += stats.ArrangeCalls;
                rendered += stats.RenderedElements;
                renderInvalidations += stats.MotionRenderInvalidations;
                layoutInvalidations += stats.MotionLayoutInvalidations;
                valuesChanged += stats.MotionValuesChanged;
            }

            TimeSpan next = FramePeriod * frame;
            TimeSpan wait = next - cadence.Elapsed;
            if (wait > TimeSpan.Zero)
            {
                Thread.Sleep(wait);
            }
        }

        TimbreRuntimeDiagnostics atEnd = runtime.GetDiagnostics();
        int activeAnimations = voices.Count(voice => voice.State == TimbrePlaybackState.Playing);
        foreach (TimbrePlayback voice in voices)
        {
            voice.Cancel();
        }

        device.Stop();
        double[] mix = recorder.MeasuredTicks.Select(ticks => ticks * 1000.0 / Stopwatch.Frequency).ToArray();
        Array.Sort(mix);
        double[] ui = frameMilliseconds.ToArray();
        Array.Sort(ui);
        Report report = new(
            Environment.GetEnvironmentVariable("TIMBRE_BENCH_PROCESS") ?? "0",
            (hidden ? "hidden-control" : "visible-root") + (control == "audio" ? string.Empty : "/control-" + control),
            DateTimeOffset.Now.ToString("O"),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            TimbreCoreBenchmarkRunner.ProcessorName(),
            Environment.ProcessorCount,
            PreloadedVoices + StreamingVoices,
            activeAnimations,
            mix.Length,
            TimbreCoreBenchmarkRunner.Percentile(mix, 0.50),
            TimbreCoreBenchmarkRunner.Percentile(mix, 0.95),
            TimbreCoreBenchmarkRunner.Percentile(mix, 0.99),
            mix[^1],
            recorder.MeasuredAllocatedBytes.Sum(),
            device.MaxQueuedFrames,
            atEnd.UnderrunFrames - (atMixerWarmup?.UnderrunFrames ?? 0),
            device.UnderrunFrames - deviceUnderrunAtWarmup,
            ui.Length,
            TimbreCoreBenchmarkRunner.Percentile(ui, 0.50),
            TimbreCoreBenchmarkRunner.Percentile(ui, 0.95),
            TimbreCoreBenchmarkRunner.Percentile(ui, 0.99),
            ui[^1],
            uiAllocated,
            allocatingFrames,
            atEnd.AnimatedPublications - publicationsAtWarmup,
            valuesChanged,
            atEnd.MotionSamplesRejected,
            measures,
            arranges,
            rendered,
            renderInvalidations,
            layoutInvalidations,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            new Thresholds(BlockMilliseconds * 0.25, 0, 1920, 0));
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, json);
        Console.WriteLine(json);
    }

    private static (TimbreSound Clip, TimbreParameter<float> Cutoff) Clip(Func<TimbreReader> open, string name, TimbreLoading loading)
    {
        TimbreParameter<float> cutoff = new("Cutoff", 2000f);
        TimbreSound clip = new(
            TimbreSource.FromReader(open, name),
            volume: 0.5f,
            loop: true,
            loading: loading,
            parameters: [cutoff],
            modifiers: [new LowPass(cutoff), new Delay(time: 0.25f, feedback: 0.4f, mix: 0.3f)]);
        return (clip, cutoff);
    }

    private static FrameStats Update(UiHost host) =>
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            host.Viewport,
            FramePeriod).Stats;

    private sealed record Thresholds(
        double MaxP99MixMilliseconds,
        long MaxSteadyStateAllocatedBytes,
        int MaxQueuedFrames,
        long MaxUnderrunFrames);

    private sealed record Report(
        string Process,
        string Scenario,
        string Timestamp,
        string Runtime,
        string OperatingSystem,
        string Architecture,
        string Processor,
        int LogicalProcessors,
        int Voices,
        int VoicesPlayingAtEnd,
        int MeasuredBlocks,
        double MixP50Milliseconds,
        double MixP95Milliseconds,
        double MixP99Milliseconds,
        double MixMaxMilliseconds,
        long MixMeasuredAllocatedBytes,
        int MaxQueuedFrames,
        long UnderrunFramesMeasured,
        long DeviceUnderrunFramesMeasured,
        int MeasuredUiFrames,
        double UiFrameP50Milliseconds,
        double UiFrameP95Milliseconds,
        double UiFrameP99Milliseconds,
        double UiFrameMaxMilliseconds,
        long UiMeasuredAllocatedBytes,
        int UiAllocatingFrames,
        long AnimatedPublicationsMeasured,
        long MotionValuesChangedMeasured,
        long MotionSamplesRejected,
        long MeasureCalls,
        long ArrangeCalls,
        long RenderedElements,
        long MotionRenderInvalidations,
        long MotionLayoutInvalidations,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        Thresholds Gate);
}
