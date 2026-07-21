using System.Diagnostics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Text;

namespace Cerneala.Presentation;

public partial class PresentationWindow
{
    private async Task RunAutomationIfRequestedAsync()
    {
        string? prismLifecycleReportPath =
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_PRISM_LIFECYCLE_REPORT");
        if (!string.IsNullOrWhiteSpace(prismLifecycleReportPath))
        {
            await RunAutomationWithErrorReportAsync(
                prismLifecycleReportPath,
                () => ExecutePrismLifecycleAutomationAsync(prismLifecycleReportPath));
            return;
        }

        string? frameBudgetReportPath =
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT");
        if (!string.IsNullOrWhiteSpace(frameBudgetReportPath))
        {
            await RunAutomationWithErrorReportAsync(
                frameBudgetReportPath,
                () => ExecuteFrameBudgetAutomationAsync(frameBudgetReportPath));
            return;
        }

        string? reportPath = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return;
        }

        await RunAutomationWithErrorReportAsync(reportPath, () => ExecuteAutomationAsync(reportPath));
    }

    private async Task ExecutePrismLifecycleAutomationAsync(string reportPath)
    {
        int cycles = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_PRISM_LIFECYCLE_CYCLES",
            defaultValue: 32,
            minimum: 1,
            maximum: 1_000);
        List<PrismLifecycleSample> samples = new(cycles * 2 + 1);
        List<string> failures = [];
        Stopwatch runTime = Stopwatch.StartNew();

        await ShowChapterAndWaitForFrameAsync(5);
        await WaitForPrismLifecycleStateAsync(expectActive: true, TimeSpan.FromSeconds(5));
        await ShowChapterAndWaitForFrameAsync(7);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
        await WaitForPrismLifecycleStateAsync(expectActive: false, TimeSpan.FromSeconds(5));
        PrismLifecycleSample baseline = CapturePrismLifecycleSample(0, "baseline-detached");
        samples.Add(baseline);
        ValidatePrismLifecycleSample(
            baseline,
            expectActive: false,
            baseline.MaterializedPrismInstanceCount,
            failures);

        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            await ShowChapterAndWaitForFrameAsync(5);
            await WaitForPrismLifecycleStateAsync(expectActive: true, TimeSpan.FromSeconds(5));
            PrismLifecycleSample visible = CapturePrismLifecycleSample(cycle, "solar-visible");
            samples.Add(visible);
            ValidatePrismLifecycleSample(
                visible,
                expectActive: true,
                baseline.MaterializedPrismInstanceCount,
                failures);

            await ShowChapterAndWaitForFrameAsync(7);
            await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
            await WaitForPrismLifecycleStateAsync(expectActive: false, TimeSpan.FromSeconds(5));
            PrismLifecycleSample detached = CapturePrismLifecycleSample(cycle, "diagnostics-detached");
            samples.Add(detached);
            ValidatePrismLifecycleSample(
                detached,
                expectActive: false,
                baseline.MaterializedPrismInstanceCount,
                failures);
        }

        AutomationSample first = baseline.Runtime;
        PrismLifecycleSample final = samples[^1];
        AutomationSample last = final.Runtime;
        PrismLifecycleReport report = new(
            SchemaVersion: 1,
            StartedUtc: DateTimeOffset.UtcNow - runTime.Elapsed,
            Cycles: cycles,
            MaterializedPrismInstanceDelta:
                final.MaterializedPrismInstanceCount - baseline.MaterializedPrismInstanceCount,
            ManagedByteDelta: last.ManagedBytes - first.ManagedBytes,
            PrivateByteDelta: last.PrivateBytes - first.PrivateBytes,
            WorkingSetByteDelta: last.WorkingSetBytes - first.WorkingSetBytes,
            Samples: samples,
            Validation: new PrismLifecycleValidation(failures.Count == 0, failures));
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Prism lifecycle validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures));
        }

        Close();
    }

    private async Task ShowChapterAndWaitForFrameAsync(int chapterIndex)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            completion.TrySetResult();
        };

        FrameRendered += handler;
        ShowChapter(chapterIndex);
        Invalidate(InvalidationFlags.Render, "Prism lifecycle chapter navigation");
        try
        {
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            FrameRendered -= handler;
            throw;
        }
    }

    private async Task WaitForPrismLifecycleStateAsync(bool expectActive, TimeSpan maximumWait)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        PrismOperationalDiagnostics? last = null;
        while (timeout.Elapsed <= maximumWait)
        {
            last = CapturePrismDiagnosticsSnapshot();
            if (MatchesPrismLifecycleState(last, expectActive))
            {
                return;
            }

            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                FrameRendered -= handler;
                completion.TrySetResult();
            };
            FrameRendered += handler;
            Invalidate(InvalidationFlags.Render, "wait for Prism lifecycle state");
            try
            {
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
                FrameRendered -= handler;
                throw;
            }
        }

        throw new TimeoutException(
            $"Prism lifecycle state did not become {(expectActive ? "active" : "detached")}; " +
            $"last diagnostics: {last?.ToString() ?? "unavailable"}.");
    }

    private static bool MatchesPrismLifecycleState(
        PrismOperationalDiagnostics? diagnostics,
        bool expectActive)
    {
        if (diagnostics is not PrismOperationalDiagnostics current ||
            current.ActiveBackdropLeaseCount != 0 ||
            current.ActiveSurfaceCount != 0)
        {
            return false;
        }

        return expectActive
            ? current.ActiveCompositionCount >= 1 &&
                current.PlannedPassCount >= 1 &&
                current.ExecutedPassCount >= 1 &&
                current.Backdrop.AcquiredFrames >= 1 &&
                current.FallbackCount == 0 &&
                current.MotionActive
            : current.ActiveCompositionCount == 0 &&
                current.PlannedPassCount == 0 &&
                current.ExecutedPassCount == 0 &&
                current.CaptureCount == 0 &&
                current.SurfaceAllocationCount == 0 &&
                current.SurfaceReuseCount == 0 &&
                current.SurfaceByteCount == 0 &&
                current.PeakSurfaceByteCount == 0 &&
                current.FallbackCount == 0 &&
                !current.MotionActive;
    }

    private PrismLifecycleSample CapturePrismLifecycleSample(int cycle, string state)
    {
        AutomationSample runtime = CaptureAutomationSample(cycle, state);
        int materializedPrismInstanceCount = EnumerateVisualTree(PageSolarSystem)
            .Count(element => PrismAttachment.TryGetInstance(element, out _));
        int activePrismRenderStateCount = EnumerateVisualTree(PageSolarSystem)
            .Count(element => PrismAttachment.TryGetRenderState(element, out _, out _));
        return new PrismLifecycleSample(
            cycle,
            state,
            materializedPrismInstanceCount,
            activePrismRenderStateCount,
            runtime);
    }

    private static void ValidatePrismLifecycleSample(
        PrismLifecycleSample sample,
        bool expectActive,
        int baselineMaterializedPrismInstanceCount,
        List<string> failures)
    {
        bool countersMatch = MatchesPrismLifecycleState(sample.Runtime.Prism, expectActive);
        AddFailureIfFalse(
            countersMatch,
            $"Cycle {sample.Cycle} ({sample.State}) has unexpected Prism counters: " +
            $"{sample.Runtime.Prism?.ToString() ?? "unavailable"}.",
            failures);
        AddFailureIfFalse(
            expectActive
                ? sample.ActivePrismRenderStateCount >= 1
                : sample.ActivePrismRenderStateCount == 0,
            $"Cycle {sample.Cycle} ({sample.State}) has " +
            $"{sample.ActivePrismRenderStateCount} active Prism render state(s).",
            failures);
        AddFailureIfFalse(
            sample.MaterializedPrismInstanceCount == baselineMaterializedPrismInstanceCount,
            $"Cycle {sample.Cycle} ({sample.State}) changed the materialized Prism instance baseline " +
            $"from {baselineMaterializedPrismInstanceCount} to {sample.MaterializedPrismInstanceCount}.",
            failures);
    }

    private async Task RunAutomationWithErrorReportAsync(string reportPath, Func<Task> automation)
    {
        try
        {
            await automation();
        }
        catch (Exception exception)
        {
            string errorPath = Path.GetFullPath(reportPath) + ".error.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Close();
        }
    }

    private async Task ExecuteFrameBudgetAutomationAsync(string reportPath)
    {
        int cycles = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_FRAME_BUDGET_CYCLES",
            defaultValue: 8,
            minimum: 1,
            maximum: 100);
        int framesPerLoad = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_FRAME_BUDGET_FRAMES_PER_LOAD",
            defaultValue: 45,
            minimum: 1,
            maximum: 1_000);
        List<FrameBudgetSample> samples = new(cycles * framesPerLoad * (ChapterNames.Length - 1));
        ButtonAutomationPeer next = new(NextButton);
        Stopwatch runTime = Stopwatch.StartNew();

        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            for (int chapterIndex = 1; chapterIndex < ChapterNames.Length; chapterIndex++)
            {
                while (currentChapter != chapterIndex - 1)
                {
                    await InvokeNextAndWaitForFrameAsync(next);
                }

                await CaptureFrameBudgetLoadAsync(
                    next,
                    cycle,
                    chapterIndex,
                    framesPerLoad,
                    runTime,
                    samples);
            }
        }

        FrameBudgetReport report = new(
            SchemaVersion: 1,
            StartedUtc: DateTimeOffset.UtcNow - runTime.Elapsed,
            Cycles: cycles,
            FramesPerLoad: framesPerLoad,
            Samples: samples);
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }

    private async Task CaptureFrameBudgetLoadAsync(
        ButtonAutomationPeer next,
        int cycle,
        int chapterIndex,
        int framesPerLoad,
        Stopwatch runTime,
        List<FrameBudgetSample> samples)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int frameIndex = 0;
        int previousGen0Collections = GC.CollectionCount(0);
        int previousGen1Collections = GC.CollectionCount(1);
        int previousGen2Collections = GC.CollectionCount(2);
        long previousAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            UiFrame frame = LastFrame ??
                throw new InvalidOperationException("FrameRendered was raised without a frame.");
            int gen0Collections = GC.CollectionCount(0);
            int gen1Collections = GC.CollectionCount(1);
            int gen2Collections = GC.CollectionCount(2);
            long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
            samples.Add(new FrameBudgetSample(
                cycle,
                ChapterNames[chapterIndex],
                chapterIndex,
                frameIndex,
                frame.ProcessingTime.TotalMilliseconds,
                frame.ElapsedTime.TotalMilliseconds,
                frame.Stats,
                FrameBudgetTimingSample.From(frame.DiagnosticsTiming),
                gen0Collections - previousGen0Collections,
                gen1Collections - previousGen1Collections,
                gen2Collections - previousGen2Collections,
                allocatedBytes - previousAllocatedBytes,
                cycle == 1,
                runTime.Elapsed.TotalMilliseconds));
            previousGen0Collections = gen0Collections;
            previousGen1Collections = gen1Collections;
            previousGen2Collections = gen2Collections;
            previousAllocatedBytes = allocatedBytes;
            frameIndex++;

            if (frameIndex >= framesPerLoad)
            {
                FrameRendered -= handler;
                completion.TrySetResult();
                return;
            }

            Invalidate(InvalidationFlags.Render, "frame budget automation sample");
        };

        FrameRendered += handler;
        if (!next.Invoke())
        {
            FrameRendered -= handler;
            throw new InvalidOperationException("Frame-budget automation could not invoke the Next button.");
        }

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private async Task InvokeNextAndWaitForFrameAsync(ButtonAutomationPeer next)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            completion.TrySetResult();
        };

        FrameRendered += handler;
        if (!next.Invoke())
        {
            FrameRendered -= handler;
            throw new InvalidOperationException("Frame-budget automation could not navigate to the next chapter.");
        }

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static int ReadBoundedEnvironmentInteger(
        string variableName,
        int defaultValue,
        int minimum,
        int maximum)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(variableName), out int requestedValue)
            ? Math.Clamp(requestedValue, minimum, maximum)
            : defaultValue;
    }

    private static bool IsPrismCaptureRequested()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_PRISM_CAPTURE"),
            "1",
            StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadPrismCaptureWidth()
    {
        return ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_CAPTURE_WIDTH",
            defaultValue: 1320,
            minimum: 1080,
            maximum: 2560);
    }

    private static int ReadPrismCaptureHeight()
    {
        return ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_CAPTURE_HEIGHT",
            defaultValue: 860,
            minimum: 720,
            maximum: 1600);
    }

    private void PrepareDeterministicPrismCaptureState(ref int initialChapter)
    {
        if (!IsPrismCaptureRequested())
        {
            return;
        }

        initialChapter = 5;
        Width = ReadPrismCaptureWidth();
        Height = ReadPrismCaptureHeight();
        (Root ?? throw new InvalidOperationException("Prism capture requires an attached UI root."))
            .Motion.ReducedMotion.SetMode(ReducedMotionMode.Reduce);
        PageSolarSystem.Visibility = Visibility.Collapsed;
    }

    private async Task PreparePrismCaptureAsync()
    {
        SelectEarthForPrismCapture();
        foreach (ToggleButton navigation in tourNavigation)
        {
            navigation.IsPointerOver = false;
        }
        PreviousButton.IsPointerOver = false;
        NextButton.IsPointerOver = false;
        Invalidate(InvalidationFlags.Render, "deterministic Prism capture state");
        await WaitForPrismCaptureViewportAsync(
            ReadPrismCaptureWidth(),
            ReadPrismCaptureHeight(),
            TimeSpan.FromSeconds(10));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        await WaitForPrismMotionIdleAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(100);
    }

    private async Task WaitForPrismMotionIdleAsync(TimeSpan maximumWait)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (Root?.Motion.HasActiveMotion == true &&
            timeout.Elapsed <= maximumWait)
        {
            await Task.Delay(25);
        }

        if (Root?.Motion.HasActiveMotion == true)
        {
            throw new TimeoutException(
                "Prism capture Motion did not reach its deterministic terminal state.");
        }
    }

    private void SelectEarthForPrismCapture()
    {
        TextBlock earthLabel = EnumerateVisualTree(PageSolarSystem)
            .OfType<TextBlock>()
            .Single(text =>
                string.Equals(text.Text, "Pamant", StringComparison.Ordinal) &&
                text.FontSize <= 10.5f);
        UIElement earthBody = earthLabel.VisualParent ??
            throw new InvalidOperationException("The Earth label is detached from its clickable planet body.");
        earthBody.RaiseEvent(new MouseButtonEventArgs(
            UIElement.MouseUpEvent,
            earthBody,
            InputMouseButton.Left,
            x: 0,
            y: 0,
            clickCount: 1));
    }

    private async Task WaitForPrismCaptureViewportAsync(
        int requestedWidth,
        int requestedHeight,
        TimeSpan maximumWait)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (timeout.Elapsed <= maximumWait)
        {
            UiViewport? viewport = LastFrame?.Viewport;
            if (viewport is UiViewport current &&
                Math.Abs(current.Width - requestedWidth) <= 1 &&
                Math.Abs(current.Height - requestedHeight) <= 1)
            {
                return;
            }

            Invalidate(InvalidationFlags.Render, "wait for Prism capture viewport");
            await Task.Delay(25);
        }

        UiViewport? actual = LastFrame?.Viewport;
        throw new TimeoutException(
            $"Prism capture viewport did not reach {requestedWidth}x{requestedHeight}; " +
            $"last frame was {actual?.Width ?? 0}x{actual?.Height ?? 0}.");
    }

    private PrismOperationalDiagnostics? CapturePrismDiagnosticsSnapshot()
    {
        return WindowApplicationRuntime.Current?.CapturePrismDiagnostics(this);
    }

    private async Task WritePrismCaptureReportAsync(
        string screenshotPath,
        PrismOperationalDiagnostics? beforeCapture,
        PrismOperationalDiagnostics? afterCapture)
    {
        PrismCaptureValidation validation = ValidatePrismCapture(beforeCapture, afterCapture);
        UiViewport viewport = LastFrame?.Viewport ?? default;
        PrismCaptureReport report = new(
            SchemaVersion: 1,
            CaptureApi: "Window.SaveScreenshot -> IWindowScreenshotSource.RenderPng",
            Chapter: ChapterNames[currentChapter],
            Planet: "Pamant",
            RequestedViewport: new CaptureViewport(
                ReadPrismCaptureWidth(),
                ReadPrismCaptureHeight(),
                viewport.Scale),
            ActualViewport: new CaptureViewport(viewport.Width, viewport.Height, viewport.Scale),
            MotionMode: Root?.Motion.ReducedMotion.Mode.ToString() ?? "unavailable",
            AnimationTime: "terminal values (reduced motion)",
            RootCommandCount: Root?.RetainedRenderCache.RootCommands.Count ?? 0,
            RenderCacheVersion: Root?.RetainedRenderCache.Version ?? 0,
            BeforeCapture: beforeCapture,
            AfterCapture: afterCapture,
            Validation: validation);

        string reportPath = Path.ChangeExtension(screenshotPath, ".metrics.json");
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        if (!validation.Passed)
        {
            throw new InvalidOperationException(
                "Prism capture validation failed:" + Environment.NewLine +
                string.Join(Environment.NewLine, validation.Failures));
        }
    }

    private PrismCaptureValidation ValidatePrismCapture(
        PrismOperationalDiagnostics? beforeCapture,
        PrismOperationalDiagnostics? afterCapture)
    {
        List<string> failures = [];
        UiViewport viewport = LastFrame?.Viewport ?? default;
        bool viewportMatches =
            Math.Abs(viewport.Width - ReadPrismCaptureWidth()) <= 1 &&
            Math.Abs(viewport.Height - ReadPrismCaptureHeight()) <= 1;
        AddFailureIfFalse(
            viewportMatches,
            $"Viewport is {viewport.Width}x{viewport.Height}, not the requested " +
            $"{ReadPrismCaptureWidth()}x{ReadPrismCaptureHeight()}.",
            failures);

        HashSet<string> visiblePlanetValues = EnumerateVisualTree(PageSolarSystem)
            .OfType<TextBlock>()
            .Select(text => text.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToHashSet(StringComparer.Ordinal);
        bool planetIsEarth = new[] { "Pamant", "Planeta terestra", "149,6 mil. km", "12.742 km", "365 zile" }
            .All(visiblePlanetValues.Contains);
        AddFailureIfFalse(planetIsEarth, "Planet card is not fixed to the Earth data set.", failures);

        UIElement? card = EnumerateVisualTree(PageSolarSystem)
            .SingleOrDefault(element => PrismAttachment.TryGetInstance(element, out _));
        TextBlock? hint = EnumerateVisualTree(PageSolarSystem)
            .OfType<TextBlock>()
            .SingleOrDefault(text => text.Text.StartsWith("Trage pentru", StringComparison.Ordinal));

        CaptureRect? cardBounds = card is null
            ? null
            : CaptureRect.From(GetBoundsRelativeTo(card, PageSolarSystem));
        CaptureRect? hintBounds = hint is null
            ? null
            : CaptureRect.From(GetBoundsRelativeTo(hint, PageSolarSystem));
        bool cardInsideViewport = cardBounds is CaptureRect cardRect &&
            Contains(
                new CaptureRect(0, 0, PageSolarSystem.ArrangedBounds.Width, PageSolarSystem.ArrangedBounds.Height),
                cardRect,
                tolerance: 1);
        AddFailureIfFalse(cardInsideViewport, "The Prism planet card is clipped by the Solar System viewport.", failures);

        bool hintDoesNotOverlapCard = cardBounds is CaptureRect cardArea &&
            hintBounds is CaptureRect hintArea &&
            !Intersects(cardArea, hintArea, tolerance: 1);
        AddFailureIfFalse(hintDoesNotOverlapCard, "The interaction hint overlaps the Prism planet card.", failures);

        TextBlock[] cardText = card is null
            ? []
            : EnumerateVisualTree(card)
                .OfType<TextBlock>()
                .Where(text => !string.IsNullOrWhiteSpace(text.Text))
                .ToArray();
        bool textNotClipped = card is not null &&
            cardBounds is CaptureRect textContainer &&
            cardText.Length > 0 &&
            cardText.All(text =>
            {
                CaptureRect bounds = CaptureRect.From(GetBoundsRelativeTo(text, PageSolarSystem));
                return Contains(textContainer, bounds, tolerance: 1) &&
                    text.DesiredSize.Width - text.Margin.Horizontal <= text.ArrangedBounds.Width + 1 &&
                    text.DesiredSize.Height - text.Margin.Vertical <= text.ArrangedBounds.Height + 1;
            });
        AddFailureIfFalse(textNotClipped, "At least one planet-card text run is clipped.", failures);

        double[] contrastRatios = card is null
            ? []
            : cardText.Select(text => CalculateTextContrast(text, card)).ToArray();
        double minimumTextContrast = contrastRatios.Length == 0 ? 0 : contrastRatios.Min();
        bool textReadable = cardText.Length > 0 &&
            cardText.All(text => text.FontSize >= 10 && text.Opacity >= 0.85f) &&
            minimumTextContrast >= 4.5;
        AddFailureIfFalse(
            textReadable,
            $"Planet-card text readability failed; minimum contrast was {minimumTextContrast:F2}:1.",
            failures);

        bool motionIsDeterministic = Root?.Motion.ReducedMotion.Mode == ReducedMotionMode.Reduce &&
            Root.Motion.HasActiveMotion == false;
        AddFailureIfFalse(motionIsDeterministic, "Prism capture still has active or nondeterministic Motion.", failures);

        PrismOperationalDiagnostics? diagnostics = afterCapture ?? beforeCapture;
        bool prismCountersReported = diagnostics is PrismOperationalDiagnostics current &&
            current.ActiveCompositionCount >= 1 &&
            current.PlannedPassCount >= 1 &&
            current.Backdrop.RequestedFrames >= 1 &&
            current.Backdrop.AcquiredFrames >= 1 &&
            current.FallbackCount == 0;
        AddFailureIfFalse(prismCountersReported, "Prism counters are missing, inactive, or report a fallback.", failures);

        return new PrismCaptureValidation(
            Passed: failures.Count == 0,
            ViewportMatches: viewportMatches,
            PlanetIsEarth: planetIsEarth,
            MotionIsDeterministic: motionIsDeterministic,
            CardInsideViewport: cardInsideViewport,
            HintDoesNotOverlapCard: hintDoesNotOverlapCard,
            TextNotClipped: textNotClipped,
            TextReadable: textReadable,
            PrismCountersReported: prismCountersReported,
            MinimumTextContrast: minimumTextContrast,
            CardBounds: cardBounds,
            HintBounds: hintBounds,
            Failures: failures);
    }

    private static void AddFailureIfFalse(bool condition, string failure, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(failure);
        }
    }

    private static IEnumerable<UIElement> EnumerateVisualTree(UIElement root)
    {
        yield return root;
        foreach (UIElement child in root.VisualChildren)
        {
            foreach (UIElement descendant in EnumerateVisualTree(child))
            {
                yield return descendant;
            }
        }
    }

    private static LayoutRect GetBoundsRelativeTo(UIElement element, UIElement ancestor)
    {
        UIElement? current = element;
        while (!ReferenceEquals(current, ancestor))
        {
            current = current?.VisualParent ??
                throw new InvalidOperationException(
                    "The validated element is outside the expected visual subtree.");
        }

        return new LayoutRect(
            element.ArrangedBounds.X - ancestor.ArrangedBounds.X,
            element.ArrangedBounds.Y - ancestor.ArrangedBounds.Y,
            element.ArrangedBounds.Width,
            element.ArrangedBounds.Height);
    }

    private static bool Contains(CaptureRect outer, CaptureRect inner, float tolerance)
    {
        return inner.X >= outer.X - tolerance &&
            inner.Y >= outer.Y - tolerance &&
            inner.X + inner.Width <= outer.X + outer.Width + tolerance &&
            inner.Y + inner.Height <= outer.Y + outer.Height + tolerance;
    }

    private static bool Intersects(CaptureRect left, CaptureRect right, float tolerance)
    {
        return left.X + left.Width > right.X + tolerance &&
            right.X + right.Width > left.X + tolerance &&
            left.Y + left.Height > right.Y + tolerance &&
            right.Y + right.Height > left.Y + tolerance;
    }

    private static double CalculateTextContrast(TextBlock text, UIElement card)
    {
        if (text.Foreground is not { SolidColor: Color foregroundColor } foreground)
        {
            return 0;
        }

        Color backgroundColor = Color.Black;
        for (UIElement? current = text.VisualParent; current is not null; current = current.VisualParent)
        {
            if (current is Border { Background: { SolidColor: Color color } background })
            {
                backgroundColor = CompositeOverBlack(color, background.Opacity * current.Opacity);
                break;
            }

            if (ReferenceEquals(current, card))
            {
                break;
            }
        }

        double textAlpha = foreground.Opacity * text.Opacity * foregroundColor.A / 255d;
        Color displayedText = Composite(foregroundColor, backgroundColor, textAlpha);
        double textLuminance = RelativeLuminance(displayedText);
        double backgroundLuminance = RelativeLuminance(backgroundColor);
        return (Math.Max(textLuminance, backgroundLuminance) + 0.05) /
            (Math.Min(textLuminance, backgroundLuminance) + 0.05);
    }

    private static Color CompositeOverBlack(Color color, double opacity)
    {
        return Composite(color, Color.Black, opacity * color.A / 255d);
    }

    private static Color Composite(Color foreground, Color background, double alpha)
    {
        alpha = Math.Clamp(alpha, 0, 1);
        return new Color(
            (byte)Math.Round(foreground.R * alpha + background.R * (1 - alpha)),
            (byte)Math.Round(foreground.G * alpha + background.G * (1 - alpha)),
            (byte)Math.Round(foreground.B * alpha + background.B * (1 - alpha)));
    }

    private static double RelativeLuminance(Color color)
    {
        static double Linearize(byte channel)
        {
            double value = channel / 255d;
            return value <= 0.04045
                ? value / 12.92
                : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R) +
            0.7152 * Linearize(color.G) +
            0.0722 * Linearize(color.B);
    }

    private async Task ExecuteAutomationAsync(string reportPath)
    {
        int cycles = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_CYCLES"),
            out int requestedCycles)
            ? Math.Clamp(requestedCycles, 1, 100)
            : 10;
        List<AutomationSample> samples = [];
        ButtonAutomationPeer next = new(NextButton);

        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(2));
        samples.Add(CaptureAutomationSample(0, "baseline"));
        for (int cycle = 1; cycle <= cycles; cycle++)
        {
            for (int click = 0; click < ChapterNames.Length; click++)
            {
                if (!next.Invoke())
                {
                    throw new InvalidOperationException("Presentation automation could not invoke the Next button.");
                }

                if (currentChapter == 5)
                {
                    await Task.Delay(250);
                }
                else
                {
                    TimeSpan maximumWait = currentChapter == 6
                        ? TimeSpan.FromSeconds(5)
                        : TimeSpan.FromSeconds(2);
                    await WaitForFrameIdleAsync(maximumWait);
                }
                samples.Add(CaptureAutomationSample(cycle, ChapterNames[currentChapter]));
            }
        }

        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
        Close();
    }

    private async Task WaitForFrameIdleAsync(TimeSpan maximumWait)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        int stableChecks = 0;
        while (stableChecks < 4)
        {
            await Task.Delay(25);
            bool hasWork = LastFrame?.Stats.HasWork != false;
            stableChecks = hasWork ? 0 : stableChecks + 1;
            if (timeout.Elapsed > maximumWait)
            {
                return;
            }
        }
    }

    private AutomationSample CaptureAutomationSample(int cycle, string chapter)
    {
        CollectAutomationGarbage();

        using Process process = Process.GetCurrentProcess();
        PrismOperationalDiagnostics? prism = CapturePrismDiagnosticsSnapshot();
        return new AutomationSample(
            cycle,
            chapter,
            GC.GetTotalMemory(forceFullCollection: false),
            process.PrivateMemorySize64,
            process.WorkingSet64,
            TextMeasurer.Default.LayoutCache.Count,
            Root?.Trace.Entries.Count ?? 0,
            Root?.RetainedRenderCache.RootCommands.Count ?? 0,
            prism);
    }

    private static void CollectAutomationGarbage()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private sealed record AutomationSample(
        int Cycle,
        string Chapter,
        long ManagedBytes,
        long PrivateBytes,
        long WorkingSetBytes,
        int TextLayoutCacheCount,
        int InvalidationTraceCount,
        int RootCommandCount,
        PrismOperationalDiagnostics? Prism);

    private sealed record PrismLifecycleReport(
        int SchemaVersion,
        DateTimeOffset StartedUtc,
        int Cycles,
        int MaterializedPrismInstanceDelta,
        long ManagedByteDelta,
        long PrivateByteDelta,
        long WorkingSetByteDelta,
        IReadOnlyList<PrismLifecycleSample> Samples,
        PrismLifecycleValidation Validation);

    private sealed record PrismLifecycleSample(
        int Cycle,
        string State,
        int MaterializedPrismInstanceCount,
        int ActivePrismRenderStateCount,
        AutomationSample Runtime);

    private sealed record PrismLifecycleValidation(
        bool Passed,
        IReadOnlyList<string> Failures);

    private sealed record PrismCaptureReport(
        int SchemaVersion,
        string CaptureApi,
        string Chapter,
        string Planet,
        CaptureViewport RequestedViewport,
        CaptureViewport ActualViewport,
        string MotionMode,
        string AnimationTime,
        int RootCommandCount,
        long RenderCacheVersion,
        PrismOperationalDiagnostics? BeforeCapture,
        PrismOperationalDiagnostics? AfterCapture,
        PrismCaptureValidation Validation);

    private sealed record PrismCaptureValidation(
        bool Passed,
        bool ViewportMatches,
        bool PlanetIsEarth,
        bool MotionIsDeterministic,
        bool CardInsideViewport,
        bool HintDoesNotOverlapCard,
        bool TextNotClipped,
        bool TextReadable,
        bool PrismCountersReported,
        double MinimumTextContrast,
        CaptureRect? CardBounds,
        CaptureRect? HintBounds,
        IReadOnlyList<string> Failures);

    private readonly record struct CaptureViewport(float Width, float Height, float Scale);

    private readonly record struct CaptureRect(float X, float Y, float Width, float Height)
    {
        public static CaptureRect From(LayoutRect bounds)
        {
            return new CaptureRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
    }

    private sealed record FrameBudgetReport(
        int SchemaVersion,
        DateTimeOffset StartedUtc,
        int Cycles,
        int FramesPerLoad,
        IReadOnlyList<FrameBudgetSample> Samples);

    private readonly record struct FrameBudgetSample(
        int Cycle,
        string Chapter,
        int ChapterIndex,
        int FrameIndex,
        double ProcessingTimeMs,
        double ElapsedTimeMs,
        FrameStats FrameStats,
        FrameBudgetTimingSample Timing,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        long AllocatedBytes,
        bool IsCold,
        double TimestampMs);

    private readonly record struct FrameBudgetTimingSample(
        double InputCollectionMs,
        double RetainedUpdateMs,
        double UpdatePreparationMs,
        double ScheduledProcessingMs,
        double InputDispatchMs,
        double InputProcessingMs,
        double RetainedCommitMs,
        double CursorPublicationMs,
        double ScheduledInheritedMs,
        double ScheduledCommandStateMs,
        double ScheduledAspectMs,
        double ScheduledMeasureMs,
        double ScheduledArrangeMs,
        double ScheduledRenderMs,
        double ScheduledHitTestMs,
        double ScheduledMotionMs,
        double BeginFrameMs,
        double DrawingMs,
        double DrawingPreparationMs,
        double TextRequestCollectionMs,
        double TextRasterizationMs,
        double TextAtlasUploadMs,
        double CommandRenderingMs,
        double DrawingCleanupMs,
        int TextRequestCount,
        long RasterizedPixelCount)
    {
        public static FrameBudgetTimingSample From(UiFrameTiming timing)
        {
            DrawingBackendFrameTiming drawing = timing.DrawingBackend;
            FramePhaseTiming scheduled = timing.ScheduledPhases;
            return new FrameBudgetTimingSample(
                timing.InputCollection.TotalMilliseconds,
                timing.RetainedUpdate.TotalMilliseconds,
                timing.UpdatePreparation.TotalMilliseconds,
                timing.ScheduledProcessing.TotalMilliseconds,
                timing.InputDispatch.TotalMilliseconds,
                timing.InputProcessing.TotalMilliseconds,
                timing.RetainedCommit.TotalMilliseconds,
                timing.CursorPublication.TotalMilliseconds,
                scheduled.InheritedProperties.TotalMilliseconds,
                scheduled.CommandState.TotalMilliseconds,
                scheduled.Aspect.TotalMilliseconds,
                scheduled.Measure.TotalMilliseconds,
                scheduled.Arrange.TotalMilliseconds,
                scheduled.Render.TotalMilliseconds,
                scheduled.HitTest.TotalMilliseconds,
                scheduled.Motion.TotalMilliseconds,
                timing.BeginFrame.TotalMilliseconds,
                timing.Drawing.TotalMilliseconds,
                drawing.Preparation.TotalMilliseconds,
                drawing.TextRequestCollection.TotalMilliseconds,
                drawing.TextRasterization.TotalMilliseconds,
                drawing.TextAtlasUpload.TotalMilliseconds,
                drawing.CommandRendering.TotalMilliseconds,
                drawing.Cleanup.TotalMilliseconds,
                drawing.TextRequestCount,
                drawing.RasterizedPixelCount);
        }
    }
}
