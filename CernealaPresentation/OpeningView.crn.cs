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
        _ = RunLoadingServoAsync();
    }

    private async Task RunLoadingServoAsync()
    {
        ServoApi servo = new(FindHostWindow());
        ServoTarget continueButton = ServoTarget.ById("presentation-continue");
        if (IsPresentationServoRequested())
        {
            ContinueButton.IsEnabled = true;
            await servo.ClickAsync(continueButton);
            return;
        }

        await servo.WaitForAsync(continueButton, ServoCondition.Enabled);
        await CaptureIfRequestedAsync(servo, "CERNEALA_PRESENTATION_LOADING_CAPTURE");
    }

    private static bool IsPresentationServoRequested()
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

    private async Task CaptureIfRequestedAsync(ServoApi servo, string environmentVariable)
    {
        string? path = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Task.Delay(150);
        await servo.SaveScreenshotAsync(Path.GetFullPath(path));
        if (string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_CLOSE_AFTER_CAPTURE"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            FindHostWindow().Close();
        }
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
