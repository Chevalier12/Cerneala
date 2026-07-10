using Cerneala.Playground;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Cerneala.Tests.Playground;

public sealed class MainWindowContractTests
{
    [Fact]
    public void GeneratedWindow_ComposesMarkupAndUsesTypedDataContext()
    {
        MainWindowViewModel viewModel = CreateViewModel();

        MainWindow window = new(viewModel);

        Assert.Same(viewModel, window.DataContext);
        Assert.Equal("Cerneala generator playground", window.Title);
        Assert.Equal(1180, window.Width);
        Assert.Equal(900, window.Height);
        Assert.IsType<StackPanel>(window.Content);
    }

    [Fact]
    public void DataContextChanges_ReevaluateTypedNestedAndEnumConditions()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MainWindow window = new(viewModel);
        UIRoot root = new();
        root.VisualChildren.Add(window);
        (TextBlock status, TextBlock score, TextBlock details, _) = FindReactiveElements(window);

        Assert.Equal("Mode: Idle", status.Text);
        Assert.Equal("Score: low", score.Text);
        Assert.Equal("Details: healthy", details.Text);

        viewModel.Mode = ShowcaseMode.Running;
        viewModel.Score = 10;
        viewModel.Details!.IsHealthy = false;

        Assert.Equal("Mode: Running", status.Text);
        Assert.Equal("Score reached target", score.Text);
        Assert.Equal("Details: unhealthy", details.Text);

        viewModel.Details = null;

        Assert.Equal("Details: null", details.Text);
    }

    [Fact]
    public void ConditionalChild_IsCreatedLazilyAndReusedAfterReactivation()
    {
        MainWindowViewModel viewModel = CreateViewModel();
        MainWindow window = new(viewModel);
        UIRoot root = new();
        root.VisualChildren.Add(window);
        (_, _, _, StackPanel actions) = FindReactiveElements(window);

        Assert.Equal(5, actions.VisualChildren.Count);

        viewModel.ShowAdvanced = true;
        Button first = Assert.IsType<Button>(actions.VisualChildren[5]);

        viewModel.ShowAdvanced = false;
        Assert.Equal(5, actions.VisualChildren.Count);

        viewModel.ShowAdvanced = true;
        Assert.Same(first, actions.VisualChildren[5]);
    }

    [Fact]
    public void ComparisonRows_AreArrangedWithoutOverlapping()
    {
        MainWindow window = new(CreateViewModel());
        UIRoot root = new();
        root.VisualChildren.Add(window);
        window.Measure(new MeasureContext(new LayoutSize(900, 700)));
        window.Arrange(new ArrangeContext(new LayoutRect(0, 0, 900, 700)));

        StackPanel rootPanel = Assert.IsType<StackPanel>(window.Content);
        Border card = Assert.IsType<Border>(rootPanel.VisualChildren[4]);
        StackPanel cardContent = Assert.IsType<StackPanel>(card.Child);
        UIElement comparisons = cardContent.VisualChildren[1];
        UIElement[] rows = comparisons.VisualChildren.ToArray();

        Assert.Collection(
            rows.Zip(rows.Skip(1)),
            pair => Assert.True(
                pair.First.ArrangedBounds.Y + pair.First.ArrangedBounds.Height <= pair.Second.ArrangedBounds.Y,
                $"'{((TextBlock)pair.First).Text}' overlaps '{((TextBlock)pair.Second).Text}'."),
            pair => Assert.True(
                pair.First.ArrangedBounds.Y + pair.First.ArrangedBounds.Height <= pair.Second.ArrangedBounds.Y,
                $"'{((TextBlock)pair.First).Text}' overlaps '{((TextBlock)pair.Second).Text}'."));
    }

    [Fact]
    public void AppHook_RegistersTheSeedUsedByDependencyInjection()
    {
        ServiceCollection services = new();
        App.ConfigureServices(services);

        using ServiceProvider provider = services.BuildServiceProvider();
        ShowcaseSeed seed = provider.GetRequiredService<ShowcaseSeed>();

        Assert.Equal("Ada", seed.UserName);
        Assert.Equal(2, seed.Score);
        Assert.Equal(10, seed.TargetScore);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        return new MainWindowViewModel(new ShowcaseSeed(
            "Ada",
            score: 2,
            targetScore: 10,
            new ShowcaseDetails(true)));
    }

    private static (TextBlock Status, TextBlock Score, TextBlock Details, StackPanel Actions) FindReactiveElements(MainWindow window)
    {
        StackPanel root = Assert.IsType<StackPanel>(window.Content);
        Border card = Assert.IsType<Border>(root.VisualChildren[4]);
        StackPanel cardContent = Assert.IsType<StackPanel>(card.Child);
        TextBlock status = Assert.IsType<TextBlock>(cardContent.VisualChildren[0]);
        StackPanel comparisons = Assert.IsType<StackPanel>(cardContent.VisualChildren[1]);
        TextBlock score = Assert.IsType<TextBlock>(comparisons.VisualChildren[0]);
        TextBlock details = Assert.IsType<TextBlock>(comparisons.VisualChildren[2]);
        StackPanel actions = Assert.IsType<StackPanel>(cardContent.VisualChildren[2]);
        return (status, score, details, actions);
    }
}
