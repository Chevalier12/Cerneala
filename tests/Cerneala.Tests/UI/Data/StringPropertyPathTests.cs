using Cerneala.UI.Data;

namespace Cerneala.Tests.UI.Data;

public sealed class StringPropertyPathTests
{
    [Fact]
    public void StringPropertyPathReportsUnsupportedCreation()
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => StringPropertyPath.Parse("User.Name"));

        Assert.Contains("deferred", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(StringPropertyPath.IsSupported);
    }

    [Fact]
    public void BindingDoesNotUseStringPropertyPaths()
    {
        ObservableValue<int> source = new(1);
        int target = 0;
        using Binding<int> binding = Binding.OneWay(source, value => target = value);

        source.Value = 5;

        Assert.Equal(5, target);
    }
}
