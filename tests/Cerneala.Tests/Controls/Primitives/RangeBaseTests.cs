using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
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

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteValueIsRejectedAndKeepsPreviousValue(float nonFinite)
    {
        RangeBase range = new() { Minimum = 10, Maximum = 20, Value = 15 };

        Assert.Throws<ArgumentException>(() => range.Value = nonFinite);

        Assert.Equal(15, range.Value);
    }

    [Theory]
    [InlineData(nameof(Slider), float.PositiveInfinity)]
    [InlineData(nameof(Slider), float.NegativeInfinity)]
    [InlineData(nameof(ScrollBar), float.PositiveInfinity)]
    [InlineData(nameof(ScrollBar), float.NegativeInfinity)]
    [InlineData(nameof(ProgressBar), float.PositiveInfinity)]
    [InlineData(nameof(ProgressBar), float.NegativeInfinity)]
    public void InfiniteValueIsRejectedByEveryRangeControl(string control, float infinite)
    {
        RangeBase range = control switch
        {
            nameof(Slider) => new Slider(),
            nameof(ScrollBar) => new ScrollBar(),
            _ => new ProgressBar()
        };
        range.Minimum = 10;
        range.Maximum = 20;
        range.Value = 15;

        Assert.Throws<ArgumentException>(() => range.Value = infinite);

        Assert.Equal(15, range.Value);
        range.Value = 25;
        Assert.Equal(20, range.Value);
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

    // #733: generated markup writes Minimum, Maximum and Value in attribute order with MarkupBase. A coercion on the
    // way must not add a Local value that hides the later markup values.
    [Theory]
    [InlineData(1f, 10f, 4f)]
    [InlineData(5f, 10f, 7f)]
    public void MarkupBaseRangeWritesInAttributeOrderKeepEveryMarkupValue(float minimum, float maximum, float value)
    {
        RangeBase range = new();

        range.SetValue(RangeBase.MinimumProperty, minimum, UiPropertyValueSource.MarkupBase);
        range.SetValue(RangeBase.MaximumProperty, maximum, UiPropertyValueSource.MarkupBase);
        range.SetValue(RangeBase.ValueProperty, value, UiPropertyValueSource.MarkupBase);

        Assert.Equal(minimum, range.Minimum);
        Assert.Equal(maximum, range.Maximum);
        Assert.Equal(value, range.Value);
        Assert.Equal(UiPropertyValueSource.MarkupBase, range.GetValueSource(RangeBase.MinimumProperty));
        Assert.Equal(UiPropertyValueSource.MarkupBase, range.GetValueSource(RangeBase.MaximumProperty));
        Assert.Equal(UiPropertyValueSource.MarkupBase, range.GetValueSource(RangeBase.ValueProperty));
    }

    // #733: an endpoint change coerces Value at the source that set it, never as a new Local value.
    [Fact]
    public void EndpointChangeCoercesValueAtItsOwnSource()
    {
        RangeBase range = new();
        range.SetValue(RangeBase.MaximumProperty, 100f, UiPropertyValueSource.MarkupBase);
        range.SetValue(RangeBase.ValueProperty, 80f, UiPropertyValueSource.MarkupBase);
        int changes = 0;
        range.ValueChanged += (_, _) => changes++;

        range.SetValue(RangeBase.MaximumProperty, 40f, UiPropertyValueSource.MarkupBase);

        Assert.Equal(40, range.Value);
        Assert.Equal(UiPropertyValueSource.MarkupBase, range.GetValueSource(RangeBase.ValueProperty));
        Assert.Equal(1, changes);

        range.Value = 30;

        Assert.Equal(30, range.Value);
        Assert.Equal(UiPropertyValueSource.Local, range.GetValueSource(RangeBase.ValueProperty));
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
