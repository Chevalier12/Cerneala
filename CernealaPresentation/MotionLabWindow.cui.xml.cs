using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using MotionSpec = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Presentation;

public partial class MotionLabWindow : Window
{
    private bool initialized;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        RunSpec();
    }

    private void OnRun(UiElementId sender, RoutedEventArgs args) => RunSpec();

    private void RunSpec()
    {
        LabStatus.Text = "SAMPLING";

        MotionSpec<float> movement;
        if (TweenModeCheck.IsChecked == true)
        {
            movement = MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(820), Easings.Emphasized);
            LabReadout.Text = "tween(duration: 820ms, easing: emphasized)";
        }
        else
        {
            float stiffness = StiffnessSlider.Value;
            float damping = DampingSlider.Value;
            movement = MotionSpec.Spring<float>(stiffness, damping);
            LabReadout.Text = $"spring(stiffness: {stiffness:0}, damping: {damping:0})";
        }

        MotionGroupHandle group = MotionGroup.Parallel(
            LabTarget.Motion().Animate(UIElement.TranslateXProperty).From(0f).To(430f).With(movement),
            LabTarget.Motion().Animate(UIElement.ScaleProperty).From(0.72f).To(1f).With(MotionSpec.Spring<float>(560, 38)),
            LabTarget.Motion().Animate(UIElement.OpacityProperty).From(0.35f).To(1f).With(MotionSpec.Tween<float>(TimeSpan.FromMilliseconds(260), Easings.EaseOut)));
        _ = MarkSettledAsync(group);
    }

    private async Task MarkSettledAsync(MotionGroupHandle group)
    {
        try
        {
            await group.Completion;
            LabStatus.Text = "SETTLED";
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnClose(UiElementId sender, RoutedEventArgs args) => Close();
}
