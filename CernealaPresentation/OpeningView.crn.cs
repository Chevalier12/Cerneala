using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Presentation;

internal partial class OpeningView : UserControl
{
    private bool sequenceStarted;
    private bool isContinuing;

    internal event EventHandler? ContinueRequested;
    internal event EventHandler? StartRequested;

    internal void Start()
    {
        if (sequenceStarted)
        {
            return;
        }

        sequenceStarted = true;
        ServoApi.SetId(ContinueButton, "presentation-continue");
        StartRequested?.Invoke(this, EventArgs.Empty);
        _ = RunLoadingAutomationAsync();
    }

    private async Task RunLoadingAutomationAsync()
    {
        if (IsPresentationAutomationRequested())
        {
            ContinueButton.IsEnabled = true;
            await new ServoApi(FindHostWindow())
                .ClickAsync(ServoTarget.ById("presentation-continue"));
            return;
        }

        await WaitForContinueButtonAsync();
        await CaptureIfRequestedAsync("CERNEALA_PRESENTATION_LOADING_CAPTURE");
    }

    private static bool IsPresentationAutomationRequested()
    {
        return
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_FRAME_BUDGET_REPORT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRISM_OUTER_GLOW_LAB_REPORT")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_TRANSITION_CAPTURE")) ||
            string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTO_CONTINUE"),
                "1",
                StringComparison.OrdinalIgnoreCase);
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
        ContinueRequested?.Invoke(this, EventArgs.Empty);
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
        if (string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CLOSE_AFTER_CAPTURE"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            FindHostWindow().Close();
        }
    }

    private async Task CaptureNextFrameAsync(string path)
    {
        Window host = FindHostWindow();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            host.FrameRendered -= handler;
            host.SaveScreenshot(path);
            completion.TrySetResult();
        };

        host.FrameRendered += handler;
        host.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "presentation screenshot");
        await completion.Task;
    }

    private Window FindHostWindow()
    {
        for (UIElement? current = this;
             current is not null;
             current = current.LogicalParent ?? current.VisualParent)
        {
            if (current is Window window)
            {
                return window;
            }
        }

        throw new InvalidOperationException("The opening view must be attached to a Window before capturing a frame.");
    }
}
