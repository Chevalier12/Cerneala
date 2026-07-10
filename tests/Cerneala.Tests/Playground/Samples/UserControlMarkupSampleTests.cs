using Cerneala.Playground.Samples.UserControlShowcase;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Playground.Samples;

public sealed class UserControlMarkupSampleTests
{
    [Fact]
    public void GeneratedMainWindowBuildsWiresEventsAndReconcilesConditionalContent()
    {
        UserControlMarkupSample sample = new();
        MainWindow window = Assert.IsType<MainWindow>(sample.Build());
        MainWindowViewModel viewModel = Assert.IsType<MainWindowViewModel>(window.DataContext);
        StackPanel rootPanel = Assert.IsType<StackPanel>(window.ComponentTemplateInstance!.Root);
        TextBlock lifecycle = Assert.IsType<TextBlock>(rootPanel.VisualChildren[1]);
        Border featureCard = Assert.IsType<Border>(rootPanel.VisualChildren[3]);
        StackPanel cardContent = Assert.IsType<StackPanel>(featureCard.Child);
        StackPanel actions = Assert.IsType<StackPanel>(cardContent.VisualChildren[2]);
        Button primary = Assert.IsType<Button>(actions.VisualChildren[0]);

        Assert.Equal(3, actions.VisualChildren.Count);
        UIRoot uiRoot = new();
        uiRoot.VisualChildren.Add(window);
        Assert.Contains("Loaded", lifecycle.Text, StringComparison.Ordinal);

        primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, primary));

        Assert.Equal(3, viewModel.Score);
        Assert.True(viewModel.ShowAdvanced);
        Assert.Equal(4, actions.VisualChildren.Count);
        Assert.Equal("Complete workflow", Assert.IsType<Button>(actions.VisualChildren[3]).Content);
    }
}
