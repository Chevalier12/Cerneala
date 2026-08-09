using System.Diagnostics;
using System.Text.Json;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Presentation;

public partial class PresentationWindow
{
    private async Task ExecuteOuterGlowLabAsync(string reportPath)
    {
        const int forcedFrameCount = 60;
        TimeSpan animationDuration = TimeSpan.FromSeconds(4);
        DateTimeOffset startedUtc = DateTimeOffset.UtcNow;

        PrismOuterGlowLabView lab = new();
        outerGlowLabActive = true;
        Content = lab;
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        UIElement target = lab.Target;
        if (IsOuterGlowColdOnlyRequested())
        {
            if (IsOuterGlowPrewarmRequested())
            {
                lab.AttachOuterGlow();
                await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
                lab.ResetPrism();
                await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
            }

            PrismEffectAddSample coldAdd = await CapturePrismAddAsync(
                "cold-add",
                lab.AttachOuterGlow);
            PrismCatalogOperationInfo catalog = PrismCatalog.GetStyle(PrismStyleId.OuterGlow);
            await WriteOuterGlowReportAsync(
                reportPath,
                new PrismOuterGlowColdLabReport(
                    SchemaVersion: 1,
                    StartedUtc: startedUtc,
                    MachineName: Environment.MachineName,
                    Framework: System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    WindowWidth: ArrangedBounds.Width,
                    WindowHeight: ArrangedBounds.Height,
                    TargetWidth: target.ArrangedBounds.Width,
                    TargetHeight: target.ArrangedBounds.Height,
                    ColdAdd: coldAdd,
                    OuterGlowDefaults: CatalogDefaults(catalog)));
            Close();
            return;
        }

        PrismOuterGlowFramePhase baseline = await CaptureForcedFramePhaseAsync(
            "baseline-no-prism",
            target,
            forcedFrameCount);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));

        using IDisposable motionSession = GeneratedMarkup.AttachMotionSession(lab);
        PrismOuterGlowFramePhase uiOpacityMotion = await CaptureMotionPhaseAsync(
            "motion-ui-opacity-baseline",
            target,
            animationDuration,
            () => GeneratedMarkup.StartMotionProperty(
                motionSession,
                target,
                UIElement.OpacityProperty,
                hasFrom: true,
                from: 0.20f,
                toCurrent: false,
                to: 1.00f,
                spec: new TweenSpec<float>(animationDuration, Easings.Linear),
                new MotionPropertyStartOptions
                {
                    HoldOnComplete = true,
                    DebugName = "UI opacity performance lab baseline"
                }));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));

        PrismEffectAddSample coldFullAdd = await CapturePrismAddAsync(
            "cold-add",
            lab.AttachOuterGlow);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase coldStatic = await CaptureForcedFramePhaseAsync(
            "cold-static-outer-glow",
            target,
            forcedFrameCount);

        lab.ResetPrism();
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));

        PrismEffectAddSample warmAdd = await CapturePrismAddAsync(
            "warm-add",
            lab.AttachOuterGlow);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase warmStatic = await CaptureForcedFramePhaseAsync(
            "warm-static-outer-glow",
            target,
            forcedFrameCount);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));

        PrismOuterGlowFramePhase opacityMotion = await CaptureMotionPhaseAsync(
            "motion-opacity-0.20-to-1.00",
            target,
            animationDuration,
            () => StartOuterGlowMotion(
                motionSession,
                lab,
                target,
                "opacity",
                0.20f,
                1.00f,
                animationDuration,
                propertyId: 137_001));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase opacityMotionRepeat = await CaptureMotionPhaseAsync(
            "motion-opacity-1.00-to-0.20-repeat",
            target,
            animationDuration,
            () => StartOuterGlowMotion(
                motionSession,
                lab,
                target,
                "opacity",
                1.00f,
                0.20f,
                animationDuration,
                propertyId: 137_001));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase sizeMotion = await CaptureMotionPhaseAsync(
            "motion-size-5-to-40",
            target,
            animationDuration,
            () => StartOuterGlowMotion(
                motionSession,
                lab,
                target,
                "size",
                5f,
                40f,
                animationDuration,
                propertyId: 137_002));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase sizeMotionRepeat = await CaptureMotionPhaseAsync(
            "motion-size-40-to-5-repeat",
            target,
            animationDuration,
            () => StartOuterGlowMotion(
                motionSession,
                lab,
                target,
                "size",
                40f,
                5f,
                animationDuration,
                propertyId: 137_002));

        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        lab.ResetPrism();
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));

        PrismEffectAddSample motionBlurAdd = await CapturePrismAddAsync(
            "motion-blur-add",
            lab.AttachMotionBlur);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase motionBlurStatic = await CaptureForcedFramePhaseAsync(
            "motion-blur-static",
            target,
            forcedFrameCount);
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase motionBlurDistance = await CaptureMotionPhaseAsync(
            "motion-blur-distance-5-to-40",
            target,
            animationDuration,
            () => StartMotionBlurMotion(
                motionSession,
                lab,
                target,
                5f,
                40f,
                animationDuration,
                propertyId: 137_101));
        await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
        PrismOuterGlowFramePhase motionBlurDistanceRepeat = await CaptureMotionPhaseAsync(
            "motion-blur-distance-40-to-5-repeat",
            target,
            animationDuration,
            () => StartMotionBlurMotion(
                motionSession,
                lab,
                target,
                40f,
                5f,
                animationDuration,
                propertyId: 137_101));

        PrismCatalogOperationInfo operation = PrismCatalog.GetStyle(PrismStyleId.OuterGlow);
        PrismCatalogOperationInfo motionBlurOperation = PrismCatalog.GetFilter(PrismFilterId.MotionBlur);
        await WriteOuterGlowReportAsync(
            reportPath,
            new PrismOuterGlowLabReport(
                SchemaVersion: 2,
                StartedUtc: startedUtc,
                MachineName: Environment.MachineName,
                Framework: System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                WindowWidth: ArrangedBounds.Width,
                WindowHeight: ArrangedBounds.Height,
                TargetWidth: target.ArrangedBounds.Width,
                TargetHeight: target.ArrangedBounds.Height,
                OuterGlowDefaults: CatalogDefaults(operation),
                MotionBlurDefaults: CatalogDefaults(motionBlurOperation),
                Baseline: baseline,
                UiOpacityMotion: uiOpacityMotion,
                ColdAdd: coldFullAdd,
                ColdStatic: coldStatic,
                WarmAdd: warmAdd,
                WarmStatic: warmStatic,
                OpacityMotion: opacityMotion,
                OpacityMotionRepeat: opacityMotionRepeat,
                SizeMotion: sizeMotion,
                SizeMotionRepeat: sizeMotionRepeat,
                MotionBlurAdd: motionBlurAdd,
                MotionBlurStatic: motionBlurStatic,
                MotionBlurDistance: motionBlurDistance,
                MotionBlurDistanceRepeat: motionBlurDistanceRepeat));
        Close();
    }

    private async Task<PrismEffectAddSample> CapturePrismAddAsync(
        string name,
        Func<PrismInstance> attach)
    {
        PrismOperationalDiagnostics? beforeDiagnostics = CapturePrismDiagnosticsSnapshot();
        TaskCompletionSource<UiFrame> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            completion.TrySetResult(LastFrame ??
                throw new InvalidOperationException("OuterGlow add probe observed no rendered frame."));
        };

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
        FrameRendered += handler;
        Stopwatch wall = Stopwatch.StartNew();
        Stopwatch synchronous = Stopwatch.StartNew();
        attach();
        synchronous.Stop();
        UiFrame frame = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
        wall.Stop();

        return new PrismEffectAddSample(
            name,
            synchronous.Elapsed.TotalMilliseconds,
            wall.Elapsed.TotalMilliseconds,
            frame.ProcessingTime.TotalMilliseconds,
            frame.ElapsedTime.TotalMilliseconds,
            frame.Stats,
            FrameBudgetTimingSample.From(frame.DiagnosticsTiming),
            GC.CollectionCount(0) - gen0,
            GC.CollectionCount(1) - gen1,
            GC.CollectionCount(2) - gen2,
            GC.GetTotalAllocatedBytes(precise: false) - allocatedBytes,
            beforeDiagnostics,
            CapturePrismDiagnosticsSnapshot());
    }

    private async Task<PrismOuterGlowFramePhase> CaptureForcedFramePhaseAsync(
        string name,
        UIElement target,
        int frameCount)
    {
        return await CaptureFramePhaseAsync(
            name,
            target,
            requestedDuration: null,
            frameCount,
            start: null);
    }

    private async Task<PrismOuterGlowFramePhase> CaptureMotionPhaseAsync(
        string name,
        UIElement target,
        TimeSpan duration,
        Func<MotionHandle> start)
    {
        return await CaptureFramePhaseAsync(
            name,
            target,
            duration,
            requestedFrameCount: null,
            start);
    }

    private async Task<PrismOuterGlowFramePhase> CaptureFramePhaseAsync(
        string name,
        UIElement target,
        TimeSpan? requestedDuration,
        int? requestedFrameCount,
        Func<MotionHandle>? start)
    {
        int capacity = requestedFrameCount ?? 300;
        List<PrismOuterGlowFrameSample> samples = new(capacity);
        PrismOperationalDiagnostics? beforeDiagnostics = CapturePrismDiagnosticsSnapshot();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Stopwatch wall = Stopwatch.StartNew();
        MotionHandle? motion = null;
        int previousGen0 = GC.CollectionCount(0);
        int previousGen1 = GC.CollectionCount(1);
        int previousGen2 = GC.CollectionCount(2);
        long previousAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            UiFrame frame = LastFrame ??
                throw new InvalidOperationException("OuterGlow frame probe observed no rendered frame.");
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            long allocatedBytes = GC.GetTotalAllocatedBytes(precise: false);
            samples.Add(new PrismOuterGlowFrameSample(
                samples.Count,
                wall.Elapsed.TotalMilliseconds,
                frame.ProcessingTime.TotalMilliseconds,
                frame.ElapsedTime.TotalMilliseconds,
                frame.Stats,
                FrameBudgetTimingSample.From(frame.DiagnosticsTiming),
                gen0 - previousGen0,
                gen1 - previousGen1,
                gen2 - previousGen2,
                allocatedBytes - previousAllocatedBytes));
            previousGen0 = gen0;
            previousGen1 = gen1;
            previousGen2 = gen2;
            previousAllocatedBytes = allocatedBytes;

            bool completed = requestedFrameCount is int count
                ? samples.Count >= count
                : motion?.IsCompleted == true;
            if (completed)
            {
                FrameRendered -= handler;
                completion.TrySetResult();
                return;
            }

            if (samples.Count >= 1_000)
            {
                FrameRendered -= handler;
                completion.TrySetException(new InvalidOperationException(
                    $"Prism effect phase '{name}' exceeded 1,000 frames without completing."));
                return;
            }

            if (requestedFrameCount is not null)
            {
                target.Invalidate(InvalidationFlags.Render, $"Prism effect lab phase {name}");
            }
        };

        FrameRendered += handler;
        try
        {
            if (start is null)
            {
                target.Invalidate(InvalidationFlags.Render, $"Prism effect lab phase {name}");
            }
            else
            {
                motion = start();
            }

            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            FrameRendered -= handler;
            wall.Stop();
        }

        return new PrismOuterGlowFramePhase(
            name,
            requestedDuration?.TotalMilliseconds,
            requestedFrameCount,
            wall.Elapsed.TotalMilliseconds,
            samples,
            beforeDiagnostics,
            CapturePrismDiagnosticsSnapshot());
    }

    private MotionHandle StartOuterGlowMotion(
        IDisposable session,
        PrismOuterGlowLabView lab,
        UIElement target,
        string parameterId,
        float from,
        float to,
        TimeSpan duration,
        int propertyId)
    {
        PrismCatalogOperationInfo operation = PrismCatalog.GetStyle(PrismStyleId.OuterGlow);
        PrismCatalogParameterInfo parameter = operation.Parameters.Single(candidate =>
            string.Equals(candidate.Id, parameterId, StringComparison.Ordinal));
        PrismInstance instance = lab.Instance ??
            throw new InvalidOperationException("OuterGlow lab has no active Prism instance.");
        return GeneratedMarkup.StartPrismMotionProperty(
            session,
            target,
            propertyId,
            _ => instance.GetLayerState(PrismOuterGlowLabView.LayerId).Styles[0].GetValue<float>(parameter),
            (_, value) => instance.GetLayerState(PrismOuterGlowLabView.LayerId).Styles[0].SetValue(parameter, value),
            discrete: false,
            hasFrom: true,
            from,
            toCurrent: false,
            to,
            spec: new TweenSpec<float>(duration, Easings.Linear),
            new MotionPropertyStartOptions
            {
                HoldOnComplete = true,
                DebugName = $"OuterGlow.{parameterId} performance lab"
            });
    }

    private MotionHandle StartMotionBlurMotion(
        IDisposable session,
        PrismOuterGlowLabView lab,
        UIElement target,
        float from,
        float to,
        TimeSpan duration,
        int propertyId)
    {
        PrismCatalogOperationInfo operation = PrismCatalog.GetFilter(PrismFilterId.MotionBlur);
        PrismCatalogParameterInfo parameter = operation.Parameters.Single(candidate =>
            string.Equals(candidate.Id, "distance", StringComparison.Ordinal));
        PrismInstance instance = lab.Instance ??
            throw new InvalidOperationException("MotionBlur lab has no active Prism instance.");
        return GeneratedMarkup.StartPrismMotionProperty(
            session,
            target,
            propertyId,
            _ => instance.GetLayerState(PrismOuterGlowLabView.LayerId).Filters[0].GetValue<float>(parameter),
            (_, value) => instance.GetLayerState(PrismOuterGlowLabView.LayerId).Filters[0].SetValue(parameter, value),
            discrete: false,
            hasFrom: true,
            from,
            toCurrent: false,
            to,
            spec: new TweenSpec<float>(duration, Easings.Linear),
            new MotionPropertyStartOptions
            {
                HoldOnComplete = true,
                DebugName = "MotionBlur.distance performance lab"
            });
    }

    private static IReadOnlyDictionary<string, string?> CatalogDefaults(PrismCatalogOperationInfo catalog) =>
        catalog.Parameters.ToDictionary(
            parameter => parameter.Id,
            parameter => parameter.DefaultValue,
            StringComparer.Ordinal);

    private static bool IsOuterGlowColdOnlyRequested() =>
        string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRISM_OUTER_GLOW_LAB_COLD_ONLY"),
            "1",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsOuterGlowPrewarmRequested() =>
        string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRISM_OUTER_GLOW_LAB_PREWARM"),
            "1",
            StringComparison.OrdinalIgnoreCase);

    private static async Task WriteOuterGlowReportAsync<T>(string reportPath, T report)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record PrismOuterGlowColdLabReport(
        int SchemaVersion,
        DateTimeOffset StartedUtc,
        string MachineName,
        string Framework,
        float WindowWidth,
        float WindowHeight,
        float TargetWidth,
        float TargetHeight,
        PrismEffectAddSample ColdAdd,
        IReadOnlyDictionary<string, string?> OuterGlowDefaults);

    private sealed record PrismOuterGlowLabReport(
        int SchemaVersion,
        DateTimeOffset StartedUtc,
        string MachineName,
        string Framework,
        float WindowWidth,
        float WindowHeight,
        float TargetWidth,
        float TargetHeight,
        IReadOnlyDictionary<string, string?> OuterGlowDefaults,
        IReadOnlyDictionary<string, string?> MotionBlurDefaults,
        PrismOuterGlowFramePhase Baseline,
        PrismOuterGlowFramePhase UiOpacityMotion,
        PrismEffectAddSample ColdAdd,
        PrismOuterGlowFramePhase ColdStatic,
        PrismEffectAddSample WarmAdd,
        PrismOuterGlowFramePhase WarmStatic,
        PrismOuterGlowFramePhase OpacityMotion,
        PrismOuterGlowFramePhase OpacityMotionRepeat,
        PrismOuterGlowFramePhase SizeMotion,
        PrismOuterGlowFramePhase SizeMotionRepeat,
        PrismEffectAddSample MotionBlurAdd,
        PrismOuterGlowFramePhase MotionBlurStatic,
        PrismOuterGlowFramePhase MotionBlurDistance,
        PrismOuterGlowFramePhase MotionBlurDistanceRepeat);

    private sealed record PrismEffectAddSample(
        string Name,
        double HandlerMilliseconds,
        double WallToFrameMilliseconds,
        double FrameProcessingMilliseconds,
        double FrameElapsedMilliseconds,
        FrameStats FrameStats,
        FrameBudgetTimingSample Timing,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        long AllocatedBytes,
        PrismOperationalDiagnostics? BeforeDiagnostics,
        PrismOperationalDiagnostics? AfterDiagnostics);

    private sealed record PrismOuterGlowFramePhase(
        string Name,
        double? RequestedDurationMilliseconds,
        int? RequestedFrameCount,
        double WallDurationMilliseconds,
        IReadOnlyList<PrismOuterGlowFrameSample> Samples,
        PrismOperationalDiagnostics? BeforeDiagnostics,
        PrismOperationalDiagnostics? AfterDiagnostics);

    private readonly record struct PrismOuterGlowFrameSample(
        int FrameIndex,
        double WallTimestampMilliseconds,
        double ProcessingTimeMilliseconds,
        double ElapsedTimeMilliseconds,
        FrameStats FrameStats,
        FrameBudgetTimingSample Timing,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        long AllocatedBytes);
}
