using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class RangeBaseTests
{
    [Fact]
    public void ValueIsClampedToRange()
    {
        RangeBase range = new()
        {
            Minimum = 10,
            Maximum = 20
        };

        range.Value = 25;

        Assert.Equal(20, range.Value);
        range.Value = 5;
        Assert.Equal(10, range.Value);
    }

    [Fact]
    public void EndpointChangesCoerceValue()
    {
        RangeBase range = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 80
        };

        range.Maximum = 40;

        Assert.Equal(40, range.Value);
    }

    [Fact]
    public void ClearingValueKeepsEffectiveValueWithinRange()
    {
        RangeBase range = new()
        {
            Minimum = 10,
            Maximum = 20,
            Value = 15
        };

        range.ClearValue(RangeBase.ValueProperty);

        Assert.Equal(10, range.Value);
    }

    [Fact]
    public void RangePropertyChangesInvalidateRetainedOutput()
    {
        UIRoot root = new(100, 100);
        RangeBase range = new();
        root.VisualChildren.Add(range);

        range.Value = 0.5f;

        Assert.Contains(range, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(range, root.RenderQueue.Snapshot());
    }
}
