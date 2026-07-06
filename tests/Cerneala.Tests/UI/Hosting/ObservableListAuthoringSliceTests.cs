using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Hosting;

public sealed class ObservableListAuthoringSliceTests
{
    [Fact]
    public void ObservableListMutationUpdatesRetainedListWithoutSecondFrameWork()
    {
        UIRoot root = new();
        ObservableList<string> items = new(["one"]);
        ItemsControl control = new() { ItemsSource = items };
        root.VisualChildren.Add(control);
        UiHost host = new(new UiHostOptions { Root = root });
        UiViewport viewport = new(200, 100);
        InputFrame empty = new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
        host.Update(empty, viewport, TimeSpan.Zero);

        items.Add("two");
        UiFrame changed = host.Update(empty, viewport, TimeSpan.Zero);
        UiFrame unchanged = host.Update(empty, viewport, TimeSpan.Zero);

        Assert.True(changed.Stats.HasWork);
        Assert.Equal(2, control.ItemCount);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);
    }
}
