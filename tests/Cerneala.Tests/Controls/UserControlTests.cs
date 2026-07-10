using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class UserControlTests
{
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
