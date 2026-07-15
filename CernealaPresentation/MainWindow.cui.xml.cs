using Cerneala.UI.Controls;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionSpec = Cerneala.UI.Motion.Specs.Motion;

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
        _ = StartLoadingSequenceAsync();
    }

    private async Task StartLoadingSequenceAsync()
    {
        await MotionGroup.Parallel(
            VisualStage.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(680), Easings.EaseOut)),
            VisualStage.Motion().TranslateX.To(0f, MotionSpec.Spring<float>(460, 38)),
            HeroKicker.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(420), Easings.EaseOut)),
            HeroKicker.Motion().TranslateX.To(0f, MotionSpec.Spring<float>(500, 40)),
            HeroTitle.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(720), Easings.EaseOut)),
            HeroTitle.Motion().TranslateY.To(0f, MotionSpec.Spring<float>(430, 34)),
            HeroDescription.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(780), Easings.EaseOut)),
            HeroDescription.Motion().TranslateY.To(0f, MotionSpec.Spring<float>(470, 38))).Completion;

        await MotionGroup.Parallel(
            MascotImage.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(620), Easings.EaseOut)),
            MascotImage.Motion().Scale.To(1f, MotionSpec.Spring<float>(390, 30)),
            MascotImage.Motion().TranslateY.To(0f, MotionSpec.Spring<float>(420, 32)),
            StageIndex.Motion().Opacity.To(0.42f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(760), Easings.EaseOut)),
            ScanLine.Motion().Opacity.To(0.7f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(240), Easings.EaseOut)),
            ScanLine.Motion().TranslateY.To(126f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(1_100), Easings.EaseInOut)),
            SequenceLabel.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(520), Easings.EaseOut)),
            MessageLine.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(560), Easings.EaseOut)),
            MessageLine.Motion().TranslateY.To(0f, MotionSpec.Spring<float>(430, 34))).Completion;
        await ScanLine.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(220), Easings.EaseIn)).Completion;
        await MessageLine.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(780))).Completion;
        await MessageLine.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseIn)).Completion;
        await RevealMessage("The tree persists. Only dirty work moves.").Completion;
        await MessageLine.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(880))).Completion;
        await MessageLine.Motion().Opacity.To(0f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseIn)).Completion;
        await RevealMessage("Ready to step inside the frame?").Completion;
        await HeroFacts.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(460), Easings.EaseOut)).Completion;
        await RevealContinueButton().Completion;
        await CaptureIfRequestedAsync("CERNEALA_PRESENTATION_LOADING_CAPTURE");
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTOMATION_REPORT")) ||
            string.Equals(
                Environment.GetEnvironmentVariable("CERNEALA_PRESENTATION_AUTO_CONTINUE"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            new ButtonAutomationPeer(ContinueButton).Invoke();
        }
    }

    private MotionGroupHandle RevealMessage(string text)
    {
        MessageLine.Text = text;
        return MotionGroup.Parallel(
            MessageLine.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(520), Easings.EaseOut)),
            MessageLine.Motion().Animate(UIElement.TranslateYProperty)
                .From(12f)
                .To(0f)
                .With(MotionSpec.Spring<float>(460, 36)));
    }

    private MotionGroupHandle RevealContinueButton()
    {
        ContinueButton.IsEnabled = true;
        return MotionGroup.Parallel(
            ContinueButton.Motion().Opacity.To(1f, MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(420), Easings.EaseOut)),
            ContinueButton.Motion().TranslateY.To(0f, MotionSpec.Spring<float>(520, 38)));
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
