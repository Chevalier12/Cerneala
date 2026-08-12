using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class UserControlTests
{
    [Fact]
    public void UserControlRendersItsBackgroundAcrossItsArrangedBounds()
    {
        SolidColorBrush background = new(Color.Black);
        UserControl control = new() { Background = background };
        UIRoot root = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        control.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 30)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root));

        Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
        Assert.Same(background, command.Brush);
        Assert.Equal(new DrawRect(3, 4, 40, 30), command.Rect);
    }

    [Fact]
    public void TypedViewModelReturnsExistingDataContextWithoutCreatingOne()
    {
        TestViewModel viewModel = new();
        TestUserControl control = new() { DataContext = viewModel };

        Assert.Same(viewModel, control.CurrentViewModel);
    }

    [Fact]
    public void TypedViewModelReportsMissingAndIncompatibleDataContextClearly()
    {
        TestUserControl control = new();

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => control.CurrentViewModel);
        Assert.Contains(typeof(TestViewModel).FullName!, missing.Message, StringComparison.Ordinal);
        Assert.Contains("null", missing.Message, StringComparison.Ordinal);

        control.DataContext = new object();
        InvalidOperationException incompatible = Assert.Throws<InvalidOperationException>(() => control.CurrentViewModel);
        Assert.Contains(typeof(object).FullName!, incompatible.Message, StringComparison.Ordinal);
    }

    private sealed class TestUserControl : UserControl<TestViewModel>
    {
        public TestViewModel CurrentViewModel => ViewModel;
    }

    private sealed class TestViewModel;
}
