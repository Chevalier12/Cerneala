using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionSpec = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Presentation;

public partial class PresentationWindow : Window
{
    private static readonly string[] ChapterNames =
    [
        "WELCOME",
        "RETAINED MODEL",
        "BUILD-TIME MARKUP",
        "ASPECT DESIGN SYSTEM",
        "MOTION",
        "FRAME PIPELINE",
        "DIAGNOSTICS"
    ];

    private int currentChapter;
    private bool contentReady;
    private bool skipNextDiagnosticsRefresh;
    private int motionReplayVersion;
    private MotionGroupHandle? activeMotionGroup;
    private ToggleButton[] tourNavigation = [];
    private readonly Dictionary<ToggleButton, MotionGroupHandle> navHoverMotions = [];

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (contentReady)
        {
            return;
        }

        contentReady = true;
        InitializeTourNavigation();
        int initialChapter = int.TryParse(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_START_CHAPTER"),
            out int requestedChapter)
            ? Math.Clamp(requestedChapter - 1, 0, ChapterNames.Length - 1)
            : 0;
        ShowChapter(initialChapter);
        _ = CaptureIfRequestedAsync();
        _ = RunAutomationIfRequestedAsync();
    }

    private void InitializeTourNavigation()
    {
        tourNavigation = [NavWelcome, NavRetained, NavMarkup, NavAspect, NavMotion, NavPipeline, NavDiagnostics];
        foreach (ToggleButton button in tourNavigation)
        {
            button.PropertyChanged += OnTourNavigationPropertyChanged;
        }
    }

    private void OnTourNavigationPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (sender is not ToggleButton button ||
            !ReferenceEquals(args.Property, UIElement.IsMouseOverProperty) ||
            button.ComponentTemplateInstance?.Parts.TryGetValue("PART_HoverLine", out UIElement? hoverLine) != true ||
            hoverLine is null ||
            button.ComponentTemplateInstance.Parts.TryGetValue("PART_HoverText", out UIElement? hoverText) != true ||
            hoverText is null)
        {
            return;
        }

        if (navHoverMotions.Remove(button, out MotionGroupHandle? previous))
        {
            previous.Cancel();
        }

        bool isHovered = button.GetValue(UIElement.IsMouseOverProperty);
        TimeSpan lineDuration = TimeSpan.FromMilliseconds(isHovered ? 180 : 150);
        TimeSpan textDuration = TimeSpan.FromMilliseconds(150);
        navHoverMotions[button] = MotionGroup.Parallel(
            hoverLine.Motion()
                .Animate(UIElement.ScaleXProperty)
                .To(isHovered ? 1f : 0f)
                .With(MotionSpec.Tween<float>(lineDuration, isHovered ? Easings.EaseOut : Easings.EaseIn)),
            hoverText.Motion()
                .Animate(UIElement.OpacityProperty)
                .To(isHovered ? 1f : 0f)
                .With(MotionSpec.Tween<float>(textDuration, Easings.EaseOut)));
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null || currentChapter != 6)
        {
            return;
        }

        if (skipNextDiagnosticsRefresh)
        {
            skipNextDiagnosticsRefresh = false;
            return;
        }

        DiagFrameTime.Text = $"{LastFrame.ElapsedTime.TotalMilliseconds:0.00} ms";
        DiagLayout.Text = $"{LastFrame.Stats.MeasuredElements} / {LastFrame.Stats.ArrangedElements}";
        DiagRender.Text = $"{LastFrame.Stats.RenderedElements} / {LastFrame.Stats.HitTestElements}";
        DiagSummary.Text = LastFrame.Stats.HasWork ? "dirty work committed" : "idle fast path";
        skipNextDiagnosticsRefresh = true;
    }

    private void OnWelcome(UiElementId sender, RoutedEventArgs args) => ShowChapter(0);
    private void OnRetained(UiElementId sender, RoutedEventArgs args) => ShowChapter(1);
    private void OnMarkup(UiElementId sender, RoutedEventArgs args) => ShowChapter(2);
    private void OnAspect(UiElementId sender, RoutedEventArgs args) => ShowChapter(3);
    private void OnMotion(UiElementId sender, RoutedEventArgs args) => ShowChapter(4);
    private void OnPipeline(UiElementId sender, RoutedEventArgs args) => ShowChapter(5);
    private void OnDiagnostics(UiElementId sender, RoutedEventArgs args) => ShowChapter(6);

    private void OnPrevious(UiElementId sender, RoutedEventArgs args)
    {
        ShowChapter(Math.Max(0, currentChapter - 1));
    }

    private void OnNext(UiElementId sender, RoutedEventArgs args)
    {
        ShowChapter(currentChapter == ChapterNames.Length - 1 ? 0 : currentChapter + 1);
    }

    private void ShowChapter(int index)
    {
        UIElement[] pages = [PageWelcome, PageRetained, PageMarkup, PageAspect, PageMotion, PagePipeline, PageDiagnostics];
        int nextChapter = Math.Clamp(index, 0, pages.Length - 1);
        if (currentChapter == 4 && nextChapter != 4)
        {
            motionReplayVersion++;
            activeMotionGroup?.Cancel();
            activeMotionGroup = null;
        }

        currentChapter = nextChapter;
        for (int i = 0; i < pages.Length; i++)
        {
            bool selected = i == currentChapter;
            pages[i].Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            tourNavigation[i].IsChecked = selected;
        }

        UIElement page = pages[currentChapter];
        MotionGroup.Parallel(
            page.Motion().Animate(UIElement.OpacityProperty)
                .From(0.25f)
                .To(1f)
                .With(MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(360), Easings.EaseOut)),
            page.Motion().Animate(UIElement.TranslateYProperty)
                .From(18f)
                .To(0f)
                .With(MotionSpec.Spring<float>(460, 38)));

        HeaderChapterText.Text = ChapterNames[currentChapter];
        ChapterCounter.Text = $"CHAPTER {currentChapter + 1:00} / {ChapterNames.Length:00}";
        PreviousButton.IsEnabled = currentChapter > 0;
        NextButton.Content = currentChapter == ChapterNames.Length - 1 ? "RESTART TOUR  ->" : "NEXT  ->";

        if (currentChapter == 4)
        {
            ReplayMotion();
        }
        else if (currentChapter == 5)
        {
            RunPipeline();
        }
    }

    private void OnReplayMotion(UiElementId sender, RoutedEventArgs args) => ReplayMotion();

    private void ReplayMotion()
    {
        activeMotionGroup?.Cancel();
        int version = ++motionReplayVersion;
        _ = ReplayMotionAsync(version);
    }

    private async Task ReplayMotionAsync(int version)
    {
        MotionStatusText.Text = "ARMING / 00 NODES";
        MotionClockText.Text = "T + 00:00.000";
        MotionTransactionText.Text = "OPEN";
        MotionPhaseIndex.Text = "00";
        MotionFieldReadout.Text = "FIELD LOCKED";
        MotionReactorState.Text = "STANDBY";

        if (!await RunMotionStageAsync(
            version,
            MotionOrbTween.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionOrbSpring.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionOrbPresence.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionStarA.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionStarB.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionStarC.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionStarD.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionActIgnition.Motion().Opacity.To(0.38f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionActOrbit.Motion().Opacity.To(0.38f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionActCommit.Motion().Opacity.To(0.38f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionCore.Motion().Scale.To(0.72f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionCoreHalo.Motion().Scale.To(0.68f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90))),
            MotionCoreRing.Motion().Scale.To(0.74f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(90)))))
        {
            return;
        }

        MotionPhaseIndex.Text = "01";
        MotionStatusText.Text = "IGNITION / 04 NODES";
        MotionReactorState.Text = "CLOCK PRIMED";
        if (!await RunMotionStageAsync(
            version,
            AnimateFrom(MotionCore, UIElement.ScaleProperty, 0.72f, 1.08f, MotionSpec.Spring<float>(360, 25)),
            AnimateFrom(MotionCoreHalo, UIElement.ScaleProperty, 0.68f, 1f, MotionSpec.Spring<float>(300, 23)),
            AnimateFrom(MotionCoreHalo, UIElement.OpacityProperty, 0.08f, 0.42f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(420), Easings.EaseOut)),
            AnimateFrom(MotionCoreRing, UIElement.ScaleProperty, 0.74f, 1f, MotionSpec.Spring<float>(480, 31)),
            MotionActIgnition.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseOut))))
        {
            return;
        }

        MotionPhaseIndex.Text = "02";
        MotionClockText.Text = "T + 00:00.620";
        MotionStatusText.Text = "TRAJECTORIES / 15 NODES";
        MotionFieldReadout.Text = "FIELD IN MOTION";
        MotionReactorState.Text = "COMPOSING";
        MotionActOrbit.Opacity = 1f;

        KeyframesSpec<float> orbitX = MotionSpec.Keyframes(
            new MotionKeyframe<float>(0f, -220f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.24f, 0f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.5f, 220f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.76f, 0f, Easings.EaseInOut),
            new MotionKeyframe<float>(1f, -180f, Easings.EaseInOut)).WithDuration(TimeSpan.FromMilliseconds(1_780));
        KeyframesSpec<float> orbitY = MotionSpec.Keyframes(
            new MotionKeyframe<float>(0f, 0f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.24f, -118f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.5f, 0f, Easings.EaseInOut),
            new MotionKeyframe<float>(0.76f, 118f, Easings.EaseInOut),
            new MotionKeyframe<float>(1f, 30f, Easings.EaseInOut)).WithDuration(TimeSpan.FromMilliseconds(1_780));

        if (!await RunMotionStageAsync(
            version,
            AnimateFrom(MotionOrbTween, UIElement.TranslateXProperty, -220f, -180f, orbitX),
            AnimateFrom(MotionOrbTween, UIElement.TranslateYProperty, 0f, 30f, orbitY),
            AnimateFrom(MotionOrbTween, UIElement.OpacityProperty, 0f, 1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(180), Easings.EaseOut)),
            AnimateFrom(MotionOrbSpring, UIElement.TranslateXProperty, -42f, 166f, MotionSpec.Spring<float>(310, 19)),
            AnimateFrom(MotionOrbSpring, UIElement.TranslateYProperty, 12f, 76f, MotionSpec.Spring<float>(420, 24)),
            AnimateFrom(MotionOrbSpring, UIElement.ScaleProperty, 0.35f, 1f, MotionSpec.Spring<float>(520, 30)),
            AnimateFrom(MotionOrbSpring, UIElement.OpacityProperty, 0f, 1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(280), Easings.EaseOut)),
            AnimateFrom(MotionOrbPresence, UIElement.TranslateXProperty, 0f, -108f, MotionSpec.Spring<float>(560, 36)),
            AnimateFrom(MotionOrbPresence, UIElement.TranslateYProperty, 0f, -82f, MotionSpec.Spring<float>(600, 38)),
            AnimateFrom(MotionOrbPresence, UIElement.ScaleProperty, 0.05f, 1f, MotionSpec.Spring<float>(440, 27)),
            AnimateFrom(MotionOrbPresence, UIElement.OpacityProperty, 0f, 1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(520), Easings.EaseOut)),
            AnimateFrom(MotionStarA, UIElement.TranslateXProperty, -80f, 720f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(920), Easings.EaseIn)),
            AnimateFrom(MotionStarA, UIElement.TranslateYProperty, -90f, 250f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(920), Easings.EaseIn)),
            AnimateFrom(MotionStarA, UIElement.OpacityProperty, 0f, 0.72f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(180), Easings.EaseOut)),
            AnimateFrom(MotionStarB, UIElement.TranslateXProperty, -180f, 640f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_180), Easings.EaseIn)),
            AnimateFrom(MotionStarB, UIElement.TranslateYProperty, -120f, 280f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_180), Easings.EaseIn)),
            AnimateFrom(MotionStarB, UIElement.OpacityProperty, 0f, 0.82f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(240), Easings.EaseOut)),
            AnimateFrom(MotionStarC, UIElement.TranslateXProperty, -280f, 520f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_460), Easings.EaseIn)),
            AnimateFrom(MotionStarC, UIElement.TranslateYProperty, -100f, 260f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_460), Easings.EaseIn)),
            AnimateFrom(MotionStarC, UIElement.OpacityProperty, 0f, 0.68f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(320), Easings.EaseOut)),
            AnimateFrom(MotionStarD, UIElement.TranslateXProperty, -420f, 380f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_020), Easings.EaseIn)),
            AnimateFrom(MotionStarD, UIElement.TranslateYProperty, -150f, 220f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_020), Easings.EaseIn)),
            AnimateFrom(MotionStarD, UIElement.OpacityProperty, 0f, 0.74f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(220), Easings.EaseOut))))
        {
            return;
        }

        MotionPhaseIndex.Text = "03";
        MotionClockText.Text = "T + 00:02.400";
        MotionStatusText.Text = "COMMITTING / 06 NODES";
        MotionTransactionText.Text = "COMMITTING";
        MotionReactorState.Text = "SETTLING";
        if (!await RunMotionStageAsync(
            version,
            AnimateFrom(MotionCore, UIElement.ScaleProperty, 1.08f, 1f, MotionSpec.Spring<float>(620, 42)),
            AnimateFrom(MotionCoreHalo, UIElement.ScaleProperty, 1f, 1.28f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(520), Easings.EaseOut)),
            AnimateFrom(MotionCoreHalo, UIElement.OpacityProperty, 0.42f, 0.14f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(520), Easings.EaseOut)),
            MotionActCommit.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseOut)),
            MotionStarA.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(180), Easings.EaseIn)),
            MotionStarB.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(220), Easings.EaseIn)),
            MotionStarC.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseIn)),
            MotionStarD.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(200), Easings.EaseIn))))
        {
            return;
        }

        MotionClockText.Text = "T + 00:02.920";
        MotionStatusText.Text = "SETTLED / GRAPH IDLE";
        MotionTransactionText.Text = "COMMITTED";
        MotionFieldReadout.Text = "FIELD STABLE";
        MotionReactorState.Text = "AT REST";
    }

    private async Task<bool> RunMotionStageAsync(int version, params MotionHandle[] handles)
    {
        MotionGroupHandle group = MotionGroup.Parallel(handles);
        activeMotionGroup = group;
        try
        {
            await group.Completion;
            return version == motionReplayVersion && currentChapter == 4;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (ReferenceEquals(activeMotionGroup, group))
            {
                activeMotionGroup = null;
            }
        }
    }

    private static MotionHandle AnimateFrom(
        UIElement element,
        Cerneala.UI.Core.UiProperty<float> property,
        float from,
        float to,
        MotionSpec<float> spec)
    {
        return element.Motion().Animate(property).From(from).To(to).With(spec);
    }

    private void OnRunPipeline(UiElementId sender, RoutedEventArgs args) => RunPipeline();

    private void RunPipeline() => _ = RunPipelineAsync();

    private async Task RunPipelineAsync()
    {
        Border[] stages = [PipelineRelay, PipelineInput, PipelineState, PipelineMeasure, PipelineArrange, PipelineCache, PipelineHit, PipelineBackend];
        List<MotionHandle> resetHandles = [];
        foreach (Border stage in stages)
        {
            resetHandles.Add(stage.Motion().Opacity.To(0.24f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(80))));
            resetHandles.Add(stage.Motion().Scale.To(0.94f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(80))));
        }

        try
        {
            await MotionGroup.Parallel(resetHandles.ToArray()).Completion;
            foreach (Border stage in stages)
            {
                await AnimateStage(stage).Completion;
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static MotionGroupHandle AnimateStage(Border stage)
    {
        return MotionGroup.Parallel(
            stage.Motion().Animate(UIElement.OpacityProperty)
                .From(0.24f)
                .To(1f)
                .With(MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(150), Easings.EaseOut)),
            stage.Motion().Animate(UIElement.ScaleProperty)
                .From(0.94f)
                .To(1f)
                .With(MotionSpec.Spring<float>(700, 45)));
    }

    private void OnOpenMotionLab(UiElementId sender, RoutedEventArgs args)
    {
        MotionLabWindow lab = new() { Owner = this };
        lab.Show();
    }

    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TOUR_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (int.TryParse(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_HOVER_CHAPTER"),
                out int hoverChapter) &&
            hoverChapter >= 1 &&
            hoverChapter <= tourNavigation.Length)
        {
            tourNavigation[hoverChapter - 1].IsPointerOver = true;
        }

        bool captureDuringMotion = string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        if (captureDuringMotion)
        {
            await Task.Delay(1_350);
        }
        else
        {
            await WaitForMotionIdleAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100);
        }
        string fullPath = Path.GetFullPath(path);
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            SaveScreenshot(fullPath);
            File.WriteAllLines(Path.ChangeExtension(fullPath, ".metrics.txt"),
            [
                $"Chapter={currentChapter + 1}",
                $"RootCommands={Root?.RetainedRenderCache.RootCommands.Count ?? 0}",
                $"ActiveMotionNodes={Root?.Motion.Graph.ActiveNodeCount ?? 0}",
                $"RenderCacheVersion={Root?.RetainedRenderCache.Version ?? 0}"
            ]);
            completion.TrySetResult();
        };

        FrameRendered += handler;
        Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
        await completion.Task;
    }
}
