using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ItemsControlRecyclingStabilityTests
{
    [Fact]
    public void ObservableReplaceReusesCompatibleRealizedContainerAtIndex()
    {
        ObservableList<string> items = new(["one"]);
        ItemsControl control = new() { ItemsSource = items };
        control.Measure(new MeasureContext(new LayoutSize(200, 100)));
        UIElement container = control.ItemContainerGenerator.RealizedContainers[0];

        items[0] = "two";
        control.Measure(new MeasureContext(new LayoutSize(200, 100)));

        Assert.Same(container, control.ItemContainerGenerator.RealizedContainers[0]);
        Assert.Equal("two", ItemContainerGenerator.GetItem(container));
    }
}
