using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using Cerneala.SceneVillage;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using Xunit.Abstractions;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SceneVillage;

// Hardware-dependent post-submit callback cadence diagnostic for the real
// SDL_GPU Village window. FrameRendered is not physical display presentation.
// Opt-in because this reports the machine it runs on.
internal sealed class VillagePerformanceGateFactAttribute : FactAttribute
{
    public VillagePerformanceGateFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CERNEALA_SDL_NATIVE_TESTS") != "1" ||
            Environment.GetEnvironmentVariable("CERNEALA_VILLAGE_PERF_GATE") != "1")
        {
            Skip = "Set CERNEALA_SDL_NATIVE_TESTS=1 and CERNEALA_VILLAGE_PERF_GATE=1 on the reference Windows SDL_GPU machine.";
        }
    }
}

[Collection(VillageNativeTestCollection.Name)]
public sealed class NativeVillageFrameRateGateTests
{
    private const double P95BudgetMilliseconds = 8.33;
    private const int SampleFrames = 1500;
    private const float MinimumMovement = 50f;
    private static readonly InputKey[] WalkCycle = [InputKey.D, InputKey.S, InputKey.A, InputKey.W];
    private static readonly TimeSpan HoldSegment = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan InputTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SetupLimit = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Warmup = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan WarmupLimit = Warmup + InputTimeout;
    // A scenario that cannot produce SampleFrames callbacks within this window
    // fails regardless; the cap keeps a regressed run bounded.
    private static readonly TimeSpan SampleWallLimit = TimeSpan.FromSeconds(90);

    private static readonly GateScenario[] Scenarios =
    [
        new("village-walk-0", "mode-static", 0, "village"),
        new("field-walk-static-1k", "mode-static", 1000, "stress-field"),
        new("field-walk-animated-1k", "mode-animated", 1000, "stress-field"),
        new("field-walk-collision-1k", "mode-collision", 1000, "stress-field"),
        new("field-walk-static-10k", "mode-static", 10000, "stress-field"),
        new("field-walk-animated-10k", "mode-animated", 10000, "stress-field"),
        new("field-walk-collision-10k", "mode-collision", 10000, "stress-field"),
    ];

    private readonly ITestOutputHelper output;

    public NativeVillageFrameRateGateTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [VillagePerformanceGateFact]
    [Trait("Category", "NativePerformance")]
    public void WalkingAndStressPresetsMeetPostSubmitCallbackCadenceGate()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        Exception? failure = null;
        bool started = false;
        var results = new List<GateResult>();
        FrameSampler? sampler = null;

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    var window = new MainWindow();
                    UIElement[] tree = DescendantsAndSelf(window).ToArray();
                    VillageGameSurface surface = Assert.Single(tree.OfType<VillageGameSurface>());
                    ServoApi.SetId(surface, "village-surface");
                    SetButtonId(tree, "0", "count-0");
                    SetButtonId(tree, "1k", "count-1000");
                    SetButtonId(tree, "10k", "count-10000");
                    SetButtonId(tree, "Static", "mode-static");
                    SetButtonId(tree, "Animated", "mode-animated");
                    SetButtonId(tree, "Collision", "mode-collision");
                    SetButtonId(tree, "Stress field", "stress-field");
                    SetButtonId(tree, "Village", "village");
                    sampler = new FrameSampler(window);
                    window.ContentRendered += async (_, _) =>
                    {
                        if (started)
                        {
                            return;
                        }

                        started = true;
                        try
                        {
                            await ExerciseAsync(window, surface, sampler, results);
                        }
                        catch (Exception ex)
                        {
                            failure = ex;
                        }
                        finally
                        {
                            window.Close();
                        }
                    };
                    return window;
                },
                startupWindowTypeName: typeof(MainWindow).FullName),
            []);

        var report = new StringBuilder();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"metric=post-submit FrameRendered callback cadence; physical display interval=unverified; backend=SDL_GPU configured; declared logical window=1240x800; physical swapchain/display mode=unverified; processId={Environment.ProcessId}; qpcFrequency={Stopwatch.Frequency}; thresholdP95={P95BudgetMilliseconds:F2}ms"));
        foreach (GateResult result in results)
        {
            report.AppendLine(result.ToString());
        }

        output.WriteLine(report.ToString());
        string? reportPath = Environment.GetEnvironmentVariable("CERNEALA_VILLAGE_PERF_REPORT");
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            File.AppendAllText(reportPath, report.ToString());
        }

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.True(started, "The real SDL window never rendered its content.");
        Assert.Equal(0, exitCode);
        Assert.Equal(Scenarios.Length, results.Count);
        GateResult[] failed = results.Where(result => !result.Passed).ToArray();
        Assert.True(failed.Length == 0, "Post-submit callback cadence diagnostic failed:" + Environment.NewLine + string.Join(Environment.NewLine, failed.Select(result => result.ToString())));
    }

    private static async Task ExerciseAsync(
        MainWindow window,
        VillageGameSurface surface,
        FrameSampler sampler,
        List<GateResult> results)
    {
        var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = InputTimeout });
        Sprite2D player = Assert.Single(surface.Scene!.Children.OfType<Sprite2D>()
            .Where(sprite => sprite.Collider?.IsSimulated == true));
        foreach (GateScenario scenario in SelectedScenarios())
        {
            using var setupCancellation = new CancellationTokenSource(SetupLimit);
            await servo.ClickAsync(ServoTarget.ById(scenario.Mode), cancellationToken: setupCancellation.Token);
            await servo.ClickAsync(ServoTarget.ById(scenario.CountButtonId), cancellationToken: setupCancellation.Token);
            await servo.ClickAsync(ServoTarget.ById(scenario.Location), cancellationToken: setupCancellation.Token);
            // Players look at and point into the game while walking.
            await servo.HoverAsync(ServoTarget.ById("village-surface"), cancellationToken: setupCancellation.Token);
            await WaitForRealizedAsync(surface, scenario.ExpectedCount, setupCancellation.Token);
            bool setupTimedOut = setupCancellation.IsCancellationRequested;
            int segment = 0;
            using var warmupCancellation = new CancellationTokenSource(WarmupLimit);
            long warmupStartQpc = Stopwatch.GetTimestamp();
            var warmup = Stopwatch.StartNew();
            while (warmup.Elapsed < Warmup)
            {
                await servo.HoldKeyAsync(WalkCycle[segment++ % WalkCycle.Length], HoldSegment,
                    cancellationToken: warmupCancellation.Token);
            }
            long warmupEndQpc = Stopwatch.GetTimestamp();
            int requested = surface.RequestedStressCount;
            int realized = surface.RealizedStressCount;

            sampler.Start(surface, player, SampleWallLimit);
            using var sampleCancellation = new CancellationTokenSource();
            sampleCancellation.CancelAfter(SampleWallLimit);
            var sampling = Stopwatch.StartNew();
            bool sampleCapHit = false;
            FrameSample sample;
            try
            {
                while (sampler.Count < SampleFrames && sampling.Elapsed < SampleWallLimit &&
                       !sampleCancellation.IsCancellationRequested)
                {
                    try
                    {
                        await servo.HoldKeyAsync(WalkCycle[segment++ % WalkCycle.Length], HoldSegment,
                            cancellationToken: sampleCancellation.Token);
                    }
                    catch (OperationCanceledException) when (sampleCancellation.IsCancellationRequested)
                    {
                        sampleCapHit = sampler.Count < SampleFrames;
                        break;
                    }
                }
            }
            finally
            {
                sample = sampler.Stop();
            }

            sampleCapHit |= sample.Intervals.Length < SampleFrames &&
                (sample.TimeLimitReached || sampleCancellation.IsCancellationRequested ||
                 sampling.Elapsed >= SampleWallLimit);
            results.Add(GateResult.Create(scenario, requested, realized, surface.RealizedStressCount,
                sample, setupTimedOut, sampleCapHit, warmupStartQpc, warmupEndQpc));
        }
    }

    private static async Task WaitForRealizedAsync(VillageGameSurface surface, int count, CancellationToken cancellationToken)
    {
        while (surface.RealizedStressCount != count && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(20, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    // A diagnostic subset may be selected; the gate itself requires all scenarios.
    private static IEnumerable<GateScenario> SelectedScenarios()
    {
        string? selected = Environment.GetEnvironmentVariable("CERNEALA_VILLAGE_PERF_SCENARIOS");
        return string.IsNullOrWhiteSpace(selected)
            ? Scenarios
            : Scenarios.Where(scenario => selected.Split(',').Contains(scenario.Name));
    }

    private static void SetButtonId(IEnumerable<UIElement> tree, string content, string id)
    {
        Button button = Assert.Single(tree.OfType<Button>().Where(item => Equals(item.Content, content)));
        ServoApi.SetId(button, id);
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current))
            {
                yield break;
            }

            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private sealed record GateScenario(string Name, string Mode, int ExpectedCount, string Location)
    {
        public string CountButtonId => "count-" + ExpectedCount.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record FrameSample(
        double[] Intervals,
        int[] HeldFrames,
        int[] MaxHeldRun,
        int WalkFrames,
        float Movement,
        int MovedFrames,
        long StartQpc,
        long StopQpc,
        long DeadlineQpc,
        long TargetReachedQpc,
        bool TimeLimitReached);

    private sealed record GateResult(
        GateScenario Scenario,
        int Requested,
        int Realized,
        int RealizedEnd,
        FrameSample Sample,
        bool SetupTimedOut,
        bool TimeLimitReached,
        long WarmupStartQpc,
        long WarmupEndQpc,
        double P50,
        double P95,
        double P99,
        double Max,
        int Over8_33)
    {
        public bool Passed =>
            Requested == Scenario.ExpectedCount && Realized == Scenario.ExpectedCount &&
            RealizedEnd == Scenario.ExpectedCount &&
            Sample.Intervals.Length == SampleFrames && !SetupTimedOut && !TimeLimitReached &&
            P95 <= P95BudgetMilliseconds &&
            Sample.HeldFrames.Length == WalkCycle.Length &&
            Sample.MaxHeldRun.Length == WalkCycle.Length &&
            Sample.MaxHeldRun.All(run => run >= 2) &&
            Sample.WalkFrames >= 2 && Sample.MovedFrames >= 1 &&
            Sample.Movement >= MinimumMovement;

        public static GateResult Create(
            GateScenario scenario,
            int requested,
            int realized,
            int realizedEnd,
            FrameSample sample,
            bool setupTimedOut,
            bool timeLimitReached,
            long warmupStartQpc,
            long warmupEndQpc)
        {
            double[] sorted = sample.Intervals.Order().ToArray();
            return new GateResult(
                scenario, requested, realized, realizedEnd, sample, setupTimedOut,
                timeLimitReached, warmupStartQpc, warmupEndQpc,
                Percentile(sorted, 0.50), Percentile(sorted, 0.95),
                Percentile(sorted, 0.99), sorted.Length == 0 ? double.NaN : sorted[^1],
                sorted.Count(value => value > P95BudgetMilliseconds));
        }

        private static double Percentile(double[] sorted, double percentile) =>
            sorted.Length == 0 ? double.NaN : sorted[(int)Math.Ceiling(percentile * sorted.Length) - 1];

        private string FailureReasons()
        {
            var failures = new List<string>();
            if (Requested != Scenario.ExpectedCount || Realized != Scenario.ExpectedCount ||
                RealizedEnd != Scenario.ExpectedCount) { failures.Add("actor-count"); }
            if (SetupTimedOut) { failures.Add("setup-budget"); }
            if (Sample.Intervals.Length != SampleFrames) { failures.Add("sample-count"); }
            if (TimeLimitReached) { failures.Add("time-budget"); }
            if (P95 > P95BudgetMilliseconds || double.IsNaN(P95)) { failures.Add("p95-budget"); }
            if (Sample.HeldFrames.Length != WalkCycle.Length ||
                Sample.MaxHeldRun.Length != WalkCycle.Length ||
                Sample.MaxHeldRun.Any(run => run < 2)) { failures.Add("held-input"); }
            if (Sample.WalkFrames < 2) { failures.Add("walk-state"); }
            if (Sample.MovedFrames < 1 || Sample.Movement < MinimumMovement) { failures.Add("movement"); }
            return failures.Count == 0 ? "none" : string.Join(',', failures);
        }

        public override string ToString()
        {
            double settlePastDeadlineMilliseconds = Sample.StopQpc > Sample.DeadlineQpc
                ? Stopwatch.GetElapsedTime(Sample.DeadlineQpc, Sample.StopQpc).TotalMilliseconds
                : 0;
            return string.Create(CultureInfo.InvariantCulture,
                $"{(Passed ? "PASS" : "FAIL")} {Scenario.Name}: reason={FailureReasons()} expected={Scenario.ExpectedCount} requested={Requested} realizedStart={Realized} realizedEnd={RealizedEnd} intervals={Sample.Intervals.Length} p50={P50:F3}ms p95={P95:F3}ms p99={P99:F3}ms max={Max:F3}ms over8.33={Over8_33} heldFrames(D,S,A,W)={string.Join(',', Sample.HeldFrames)} maxHeldRun(D,S,A,W)={string.Join(',', Sample.MaxHeldRun)} walkFrames={Sample.WalkFrames} movement={Sample.Movement:F1} movedFrames={Sample.MovedFrames} setupCap={SetupTimedOut} sampleCap={TimeLimitReached} settlePastDeadline={settlePastDeadlineMilliseconds:F1}ms warmupQpc={WarmupStartQpc}..{WarmupEndQpc} sampleQpc={Sample.StartQpc}..{Sample.StopQpc} targetReachedQpc={Sample.TargetReachedQpc} sampleDeadlineQpc={Sample.DeadlineQpc}");
        }
    }

    [Fact]
    public void CadenceGateUsesLiteralEightPointThreeThreeMillisecondBudget()
    {
        double[] intervals = Enumerable.Repeat(8.331, SampleFrames).ToArray();

        GateResult result = GateResult.Create(Scenarios[0], 0, 0, 0,
            ValidSample(intervals), false, false, 0, 1);

        Assert.False(result.Passed);
    }

    [Fact]
    public void CadenceGateRejectsWrongRealizedActorCount()
    {
        double[] intervals = Enumerable.Repeat(8.0, SampleFrames).ToArray();

        GateResult result = GateResult.Create(Scenarios[1], 1000, 0, 0,
            ValidSample(intervals), false, false, 0, 1);

        Assert.False(result.Passed);
        Assert.False(GateResult.Create(Scenarios[1], 1000, 1000, 0,
            ValidSample(intervals), false, false, 0, 1).Passed);
    }

    [Fact]
    public void CadenceGateRejectsStationaryIdleOrNoncontinuousInput()
    {
        FrameSample sample = ValidSample(Enumerable.Repeat(8.0, SampleFrames).ToArray());

        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0,
            sample with { Movement = 0, MovedFrames = 0 }, false, false, 0, 1).Passed);
        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0, sample with { WalkFrames = 0 },
            false, false, 0, 1).Passed);
        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0, sample with { MaxHeldRun = [1, 2, 2, 2] },
            false, false, 0, 1).Passed);
    }

    [Fact]
    public void CadenceGateRejectsTimeCapAndIncompleteSample()
    {
        FrameSample sample = ValidSample(Enumerable.Repeat(8.0, SampleFrames).ToArray());

        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0, sample, false, true, 0, 1).Passed);
        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0, sample, true, false, 0, 1).Passed);
        Assert.False(GateResult.Create(Scenarios[0], 0, 0, 0,
            sample with { Intervals = sample.Intervals[..^1] }, false, false, 0, 1).Passed);
    }

    [Fact]
    public void CadenceGateDoesNotTreatPostSampleSettlementAsMissedSampleDeadline()
    {
        FrameSample sample = ValidSample(Enumerable.Repeat(8.0, SampleFrames).ToArray()) with
        {
            TargetReachedQpc = 80 * Stopwatch.Frequency,
            DeadlineQpc = 90 * Stopwatch.Frequency,
            StopQpc = 95 * Stopwatch.Frequency
        };

        Assert.True(GateResult.Create(Scenarios[0], 0, 0, 0, sample,
            false, false, 0, 1).Passed);
    }

    [Fact]
    public void CadencePercentilesUseNearestRankAndLiteralOverBudgetCount()
    {
        FrameSample sample = ValidSample([1.0, 2.0, 3.0, 4.0, 8.331]);

        GateResult result = GateResult.Create(Scenarios[0], 0, 0, 0, sample,
            false, false, 0, 1);

        Assert.Equal(3.0, result.P50);
        Assert.Equal(8.331, result.P95);
        Assert.Equal(8.331, result.P99);
        Assert.Equal(8.331, result.Max);
        Assert.Equal(1, result.Over8_33);
    }

    private static FrameSample ValidSample(double[] intervals) =>
        new(intervals, [2, 2, 2, 2], [2, 2, 2, 2], 2, MinimumMovement, 1,
            1, 2, long.MaxValue, 2, false);

    // Records the wall-clock interval between post-submit FrameRendered callbacks.
    private sealed class FrameSampler
    {
        private readonly List<double> intervals = new(SampleFrames);
        private readonly Window window;
        private readonly int[] heldFrames = new int[WalkCycle.Length];
        private readonly int[] heldRun = new int[WalkCycle.Length];
        private readonly int[] maxHeldRun = new int[WalkCycle.Length];
        private VillageGameSurface? surface;
        private Sprite2D? player;
        private Vector2 previousPosition;
        private float movement;
        private int movedFrames;
        private int walkFrames;
        private long previous;
        private long startQpc;
        private long deadlineQpc;
        private long targetReachedQpc;
        private bool timeLimitReached;
        private bool active;

        public FrameSampler(Window window)
        {
            this.window = window;
            window.FrameRendered += OnFrameRendered;
        }

        public int Count => intervals.Count;

        public void Start(VillageGameSurface surface, Sprite2D player, TimeSpan limit)
        {
            this.surface = surface;
            this.player = player;
            intervals.Clear();
            Array.Clear(heldFrames);
            Array.Clear(heldRun);
            Array.Clear(maxHeldRun);
            previousPosition = surface.PlayerPosition;
            movement = 0;
            movedFrames = 0;
            walkFrames = 0;
            previous = 0;
            targetReachedQpc = 0;
            timeLimitReached = false;
            startQpc = Stopwatch.GetTimestamp();
            deadlineQpc = startQpc + (long)Math.Ceiling(limit.TotalSeconds * Stopwatch.Frequency);
            active = true;
        }

        public FrameSample Stop()
        {
            active = false;
            return new FrameSample(intervals.ToArray(), heldFrames.ToArray(), maxHeldRun.ToArray(),
                walkFrames, movement, movedFrames, startQpc, Stopwatch.GetTimestamp(),
                deadlineQpc, targetReachedQpc, timeLimitReached);
        }

        private void OnFrameRendered(object? sender, EventArgs args)
        {
            if (!active)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            if (intervals.Count >= SampleFrames)
            {
                return;
            }
            if (now > deadlineQpc)
            {
                timeLimitReached = true;
                return;
            }

            if (previous != 0 && intervals.Count < SampleFrames)
            {
                intervals.Add(Stopwatch.GetElapsedTime(previous, now).TotalMilliseconds);
                if (intervals.Count == SampleFrames)
                {
                    targetReachedQpc = now;
                }
            }

            previous = now;
            if (window.LastFrame is not { } frame)
            {
                return;
            }

            for (int index = 0; index < WalkCycle.Length; index++)
            {
                if (frame.Input.Keyboard.IsDown(WalkCycle[index]))
                {
                    heldFrames[index]++;
                    heldRun[index]++;
                    maxHeldRun[index] = Math.Max(maxHeldRun[index], heldRun[index]);
                }
                else
                {
                    heldRun[index] = 0;
                }
            }

            if (player?.AnimationState?.StartsWith("Walk", StringComparison.Ordinal) == true)
            {
                walkFrames++;
            }

            if (surface is not null)
            {
                Vector2 position = surface.PlayerPosition;
                float displacement = Vector2.Distance(previousPosition, position);
                movement += displacement;
                if (displacement > 0)
                {
                    movedFrames++;
                }
                previousPosition = position;
            }
        }
    }
}
