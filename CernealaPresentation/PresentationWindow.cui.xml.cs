using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Presentation;

internal enum PresentationChapter
{
    Welcome,
    RetainedModel,
    Markup,
    Aspect,
    Motion,
    Prism,
    FramePipeline,
    Diagnostics
}

public partial class PresentationWindow : Window
{
    private static readonly PresentationChapter[] ChapterOrder =
    [
        PresentationChapter.Welcome,
        PresentationChapter.RetainedModel,
        PresentationChapter.Markup,
        PresentationChapter.Aspect,
        PresentationChapter.Motion,
        PresentationChapter.Prism,
        PresentationChapter.FramePipeline,
        PresentationChapter.Diagnostics
    ];

    private PresentationChapter currentChapter = PresentationChapter.Welcome;
    private bool contentReady;
    private bool suppressLiveDiagnostics;
    private IReadOnlyDictionary<PresentationChapter, ToggleButton> tourNavigation =
        new Dictionary<PresentationChapter, ToggleButton>();
    private IReadOnlyDictionary<PresentationChapter, UIElement> tourPages =
        new Dictionary<PresentationChapter, UIElement>();

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
            ? Math.Clamp(requestedChapter - 1, 0, ChapterOrder.Length - 1)
            : 0;
        ShowChapter(ChapterOrder[initialChapter]);
        _ = RunRequestedWorkAsync();
    }

    internal void ApplyRequestedWindowSize()
    {
        Width = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_WIDTH",
            (int)Width,
            (int)MinWidth,
            7680);
        Height = ReadBoundedEnvironmentInteger(
            "CERNEALA_PRESENTATION_HEIGHT",
            (int)Height,
            (int)MinHeight,
            4320);
    }

    private async Task RunRequestedWorkAsync()
    {
        await CaptureIfRequestedAsync();
        await RunAutomationIfRequestedAsync();
    }

    private void InitializeTourNavigation()
    {
        tourNavigation = new Dictionary<PresentationChapter, ToggleButton>
        {
            [PresentationChapter.Welcome] = NavWelcome,
            [PresentationChapter.RetainedModel] = NavRetained,
            [PresentationChapter.Markup] = NavMarkup,
            [PresentationChapter.Aspect] = NavAspect,
            [PresentationChapter.Motion] = NavMotion,
            [PresentationChapter.Prism] = NavPrism,
            [PresentationChapter.FramePipeline] = NavPipeline,
            [PresentationChapter.Diagnostics] = NavDiagnostics
        };
        tourPages = new Dictionary<PresentationChapter, UIElement>
        {
            [PresentationChapter.Welcome] = PageWelcome,
            [PresentationChapter.RetainedModel] = PageRetained,
            [PresentationChapter.Markup] = PageMarkup,
            [PresentationChapter.Aspect] = PageAspect,
            [PresentationChapter.Motion] = PageMotion,
            [PresentationChapter.Prism] = PagePrism,
            [PresentationChapter.FramePipeline] = PagePipeline,
            [PresentationChapter.Diagnostics] = PageDiagnostics
        };
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null)
        {
            return;
        }

        if (currentChapter == PresentationChapter.Prism && !suppressLiveDiagnostics)
        {
            PagePrism.UpdateDiagnostics(CapturePrismDiagnosticsSnapshot());
        }
        else if (currentChapter == PresentationChapter.Diagnostics)
        {
            PageDiagnostics.UpdateDiagnostics(LastFrame);
        }
    }

    private void OnWelcome(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Welcome);
    private void OnRetained(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.RetainedModel);
    private void OnMarkup(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Markup);
    private void OnAspect(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Aspect);
    private void OnMotion(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Motion);
    private void OnPrism(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Prism);
    private void OnPipeline(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.FramePipeline);
    private void OnDiagnostics(UiElementId sender, RoutedEventArgs args) => ShowChapter(PresentationChapter.Diagnostics);

    private void OnPrevious(UiElementId sender, RoutedEventArgs args)
    {
        int index = ChapterIndex(currentChapter);
        ShowChapter(ChapterOrder[Math.Max(0, index - 1)]);
    }

    private void OnNext(UiElementId sender, RoutedEventArgs args)
    {
        int index = ChapterIndex(currentChapter);
        ShowChapter(ChapterOrder[index == ChapterOrder.Length - 1 ? 0 : index + 1]);
    }

    private void ShowChapter(PresentationChapter chapter)
    {
        if (currentChapter == PresentationChapter.Prism && chapter != PresentationChapter.Prism)
        {
            PagePrism.Deactivate();
        }

        currentChapter = chapter;
        foreach (PresentationChapter candidate in ChapterOrder)
        {
            bool selected = candidate == currentChapter;
            tourPages[candidate].Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            tourNavigation[candidate].IsChecked = selected;
        }

        if (currentChapter == PresentationChapter.Prism)
        {
            PagePrism.Activate();
        }

        int index = ChapterIndex(currentChapter);
        HeaderChapterText.Text = ChapterName(currentChapter);
        ChapterCounter.Text = $"CHAPTER {index + 1:00} / {ChapterOrder.Length:00}";
        PreviousButton.IsEnabled = index > 0;
        NextButton.Content = index == ChapterOrder.Length - 1 ? "RESTART TOUR  ->" : "NEXT  ->";

    }

    private static int ChapterIndex(PresentationChapter chapter) => Array.IndexOf(ChapterOrder, chapter);

    private static string ChapterName(PresentationChapter chapter) => chapter switch
    {
        PresentationChapter.Welcome => "WELCOME",
        PresentationChapter.RetainedModel => "RETAINED MODEL",
        PresentationChapter.Markup => "BUILD-TIME MARKUP",
        PresentationChapter.Aspect => "ASPECT DESIGN SYSTEM",
        PresentationChapter.Motion => "MOTION",
        PresentationChapter.Prism => "PRISM",
        PresentationChapter.FramePipeline => "FRAME PIPELINE",
        PresentationChapter.Diagnostics => "DIAGNOSTICS",
        _ => throw new ArgumentOutOfRangeException(nameof(chapter), chapter, "Unknown presentation chapter.")
    };
    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TOUR_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath = Path.GetFullPath(path);
        string errorPath = fullPath + ".error.txt";
        bool closeAfterCapture = string.Equals(
            Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CLOSE_AFTER_CAPTURE"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        File.Delete(errorPath);
        try
        {
            PresentationChapter captureChapter = currentChapter;
            if (int.TryParse(
                    Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_HOVER_CHAPTER"),
                    out int hoverChapter) &&
                hoverChapter >= 1 &&
                hoverChapter <= ChapterOrder.Length)
            {
                tourNavigation[ChapterOrder[hoverChapter - 1]].IsPointerOver = true;
            }

            bool captureDuringMotion = string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CAPTURE_DURING_MOTION"),
                "1",
                StringComparison.OrdinalIgnoreCase);
            if (captureDuringMotion && !closeAfterCapture)
            {
                await Task.Delay(1_350);
            }
            else if (!closeAfterCapture)
            {
                await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(100);
            }

            int captureIndex = ChapterIndex(captureChapter);
            PresentationChapter previousChapter = ChapterOrder[
                (captureIndex - 1 + ChapterOrder.Length) % ChapterOrder.Length];
            ShowChapter(previousChapter);
            ButtonAutomationPeer next = new(NextButton);
            suppressLiveDiagnostics = true;
            try
            {
                await CaptureScreenshotFrameAsync(fullPath, () =>
                {
                    if (!next.Invoke())
                    {
                        throw new InvalidOperationException("Presentation capture could not navigate to its target chapter.");
                    }
                });
            }
            finally
            {
                suppressLiveDiagnostics = false;
            }
            await File.WriteAllLinesAsync(Path.ChangeExtension(fullPath, ".metrics.txt"),
            [
                $"Chapter={ChapterIndex(currentChapter) + 1}",
                $"RootCommands={Root?.RetainedRenderCache.RootCommands.Count ?? 0}",
                $"RenderCacheVersion={Root?.RetainedRenderCache.Version ?? 0}"
            ]);
            if (closeAfterCapture)
            {
                Close();
            }
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
        }
    }

    private async Task CaptureScreenshotFrameAsync(string fullPath, Action frameTrigger)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int renderedFrames = 0;
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            renderedFrames++;
            if (renderedFrames < 4)
            {
                Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot settle");
                return;
            }

            FrameRendered -= handler;
            try
            {
                SaveScreenshot(fullPath);
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        };

        FrameRendered += handler;
        frameTrigger();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        while (!completion.Task.IsCompleted)
        {
            timeout.Token.ThrowIfCancellationRequested();
            Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
            await Task.WhenAny(completion.Task, Task.Delay(16, timeout.Token));
        }
        await completion.Task;
    }
}
