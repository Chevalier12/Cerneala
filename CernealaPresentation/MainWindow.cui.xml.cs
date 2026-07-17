using Cerneala.UI.Controls;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Presentation;

public partial class MainWindow : Window
{
    private bool sequenceStarted;
    private bool isContinuing;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;
        string? motionLabCapture = Environment.GetEnvironmentVariable("CERNEALA_MOTION_LAB_CAPTURE");
        if (!string.IsNullOrWhiteSpace(motionLabCapture))
        {
            MotionLabWindow lab = new();
            lab.Closed += (_, _) => Close();
            lab.Show();
            Hide();
            return;
        }

        _ = RunLoadingAutomationAsync();
    }

    private async Task RunLoadingAutomationAsync()
    {
        await WaitForContinueButtonAsync();
        await CaptureIfRequestedAsync("CERNEALA_PRESENTATION_LOADING_CAPTURE");
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT")) ||
            string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTO_CONTINUE"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            new ButtonAutomationPeer(ContinueButton).Invoke();
        }
    }

    private Task WaitForContinueButtonAsync()
    {
        if (ContinueButton.IsEnabled)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<UiPropertyChangedEventArgs>? handler = null;
        handler = (_, _) =>
        {
            if (!ContinueButton.IsEnabled)
            {
                return;
            }

            ContinueButton.IsEnabledChanged -= handler;
            completion.TrySetResult();
        };

        ContinueButton.IsEnabledChanged += handler;
        return completion.Task;
    }

    private void OnContinue(UiElementId sender, RoutedEventArgs args)
    {
        if (isContinuing)
        {
            return;
        }

        isContinuing = true;
        ContinueButton.IsEnabled = false;
        PresentationWindow presentation = new();
        presentation.Closed += (_, _) => Close();
        presentation.Show();
        Hide();
    }

    private async Task CaptureIfRequestedAsync(string environmentVariable)
    {
        string? path = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Task.Delay(150);
        await CaptureNextFrameAsync(Path.GetFullPath(path));
    }

    private async Task CaptureNextFrameAsync(string path)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            FrameRendered -= handler;
            SaveScreenshot(path);
            completion.TrySetResult();
        };

        FrameRendered += handler;
        Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
        await completion.Task;
    }
}
