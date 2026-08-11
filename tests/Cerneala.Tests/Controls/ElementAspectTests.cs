using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ElementAspectTests
{
    [Fact]
    public void AspectAppliesReplacesAndClearsLocalAspectValues()
    {
        Button button = new();
        ElementAspect first = new(
            [new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.White))]);
        ElementAspect second = new(
            [new ElementAspectValue(Control.ForegroundProperty, new SolidColorBrush(Color.Transparent))]);

        button.Aspect = first;

        Assert.Same(first, button.Aspect);
        Assert.Equal(new Cerneala.UI.Media.SolidColorBrush(Color.White), button.Background);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = second;

        Assert.Null(button.Background);
        Assert.Equal(new SolidColorBrush(Color.Transparent), button.Foreground);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.ForegroundProperty));

        button.Aspect = null;

        Assert.Null(button.Aspect);
        Assert.Equal(new SolidColorBrush(Color.Black), button.Foreground);
    }

    [Fact]
    public void AspectRejectsDuplicateDefaultProperties()
    {
        Assert.Throws<ArgumentException>(() => new ElementAspect(
        [
            new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.Black)),
            new ElementAspectValue(Control.BackgroundProperty, new Cerneala.UI.Media.SolidColorBrush(Color.White))
        ]));
    }

    [Fact]
    public void ReplacingAspectOnlyInvalidatesPropertiesWhoseValuesChanged()
    {
        Button button = new();
        ElementAspect first = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f),
            new ElementAspectValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White))
        ]);
        ElementAspect second = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f),
            new ElementAspectValue(Control.BorderBrushProperty, new SolidColorBrush(Color.Black))
        ]);

        button.Aspect = first;
        button.DirtyState.ClearAll();

        button.Aspect = second;

        Assert.False(button.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(button.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.True(button.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(120f, button.Width);
        Assert.Equal(new SolidColorBrush(Color.Black), button.BorderBrush);
    }

    [Fact]
    public void UpdatingAnAspectValuePreservesTheAspectAndInvalidatesOnlyThatProperty()
    {
        Button button = new();
        ElementAspect aspect = new(
        [
            new ElementAspectValue(UIElement.WidthProperty, 120f)
        ]);
        button.Aspect = aspect;
        button.DirtyState.ClearAll();

        Assert.True(aspect.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White)));

        Assert.Same(aspect, button.Aspect);
        Assert.Equal(new SolidColorBrush(Color.White), button.BorderBrush);
        Assert.Equal(new SolidColorBrush(Color.White), aspect.DefaultValues[1].Value);
        Assert.False(button.DirtyState.Has(InvalidationFlags.Aspect));
        Assert.False(button.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(button.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.True(button.DirtyState.Has(InvalidationFlags.Render));

        button.DirtyState.ClearAll();
        Assert.False(aspect.SetValue(Control.BorderBrushProperty, new SolidColorBrush(Color.White)));
        Assert.Equal(InvalidationFlags.None, button.DirtyState.Flags);
    }
}
