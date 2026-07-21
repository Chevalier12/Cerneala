using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

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
        "SOLAR MOTION",
        "FRAME PIPELINE",
        "DIAGNOSTICS"
    ];

    private int currentChapter;
    private bool contentReady;
    private ToggleButton[] tourNavigation = [];

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
        PrepareDeterministicPrismCaptureState(ref initialChapter);
        ShowChapter(initialChapter);
        _ = CaptureIfRequestedAsync();
        _ = RunAutomationIfRequestedAsync();
    }

    private void InitializeTourNavigation()
    {
        tourNavigation =
        [
            NavWelcome,
            NavRetained,
            NavMarkup,
            NavAspect,
            NavMotion,
            NavSolarSystem,
            NavPipeline,
            NavDiagnostics
        ];
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (LastFrame is null || currentChapter != 7)
        {
            return;
        }

        PageDiagnostics.UpdateDiagnostics(LastFrame);
    }

    private void OnWelcome(UiElementId sender, RoutedEventArgs args) => ShowChapter(0);
    private void OnRetained(UiElementId sender, RoutedEventArgs args) => ShowChapter(1);
    private void OnMarkup(UiElementId sender, RoutedEventArgs args) => ShowChapter(2);
    private void OnAspect(UiElementId sender, RoutedEventArgs args) => ShowChapter(3);
    private void OnMotion(UiElementId sender, RoutedEventArgs args) => ShowChapter(4);
    private void OnSolarSystem(UiElementId sender, RoutedEventArgs args) => ShowChapter(5);
    private void OnPipeline(UiElementId sender, RoutedEventArgs args) => ShowChapter(6);
    private void OnDiagnostics(UiElementId sender, RoutedEventArgs args) => ShowChapter(7);

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
        UIElement[] pages =
        [
            PageWelcome,
            PageRetained,
            PageMarkup,
            PageAspect,
            PageMotion,
            PageSolarSystem,
            PagePipeline,
            PageDiagnostics
        ];
        int nextChapter = Math.Clamp(index, 0, pages.Length - 1);
        currentChapter = nextChapter;
        for (int i = 0; i < pages.Length; i++)
        {
            bool selected = i == currentChapter;
            pages[i].Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            tourNavigation[i].IsChecked = selected;
        }

        HeaderChapterText.Text = ChapterNames[currentChapter];
        ChapterCounter.Text = $"CHAPTER {currentChapter + 1:00} / {ChapterNames.Length:00}";
        PreviousButton.IsEnabled = currentChapter > 0;
        NextButton.Content = currentChapter == ChapterNames.Length - 1 ? "RESTART TOUR  ->" : "NEXT  ->";

    }
    private async Task CaptureIfRequestedAsync()
    {
        string? path = Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TOUR_CAPTURE");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        bool prismCapture = IsPrismCaptureRequested();
        string fullPath = Path.GetFullPath(path);
        string errorPath = fullPath + ".error.txt";
        File.Delete(errorPath);
        try
        {
            if (prismCapture)
            {
                await PreparePrismCaptureAsync();
            }
            else
            {
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
                    await WaitForFrameIdleAsync(TimeSpan.FromSeconds(5));
                    await Task.Delay(100);
                }
            }

            PrismOperationalDiagnostics? beforeCapture = prismCapture
                ? CapturePrismDiagnosticsSnapshot()
                : null;
            await CaptureScreenshotFrameAsync(fullPath);
            PrismOperationalDiagnostics? afterCapture = prismCapture
                ? CapturePrismDiagnosticsSnapshot()
                : null;

            if (prismCapture)
            {
                await WritePrismCaptureReportAsync(fullPath, beforeCapture, afterCapture);
            }
            else
            {
                await File.WriteAllLinesAsync(Path.ChangeExtension(fullPath, ".metrics.txt"),
                [
                    $"Chapter={currentChapter + 1}",
                    $"RootCommands={Root?.RetainedRenderCache.RootCommands.Count ?? 0}",
                    $"RenderCacheVersion={Root?.RetainedRenderCache.Version ?? 0}"
                ]);
            }
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
        }
        finally
        {
            if (prismCapture)
            {
                Close();
            }
        }
    }

    private async Task CaptureScreenshotFrameAsync(string fullPath)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
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
        Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }
}
