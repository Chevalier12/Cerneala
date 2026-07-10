using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;

namespace Cerneala.Tests.Controls;

public sealed class ElementAspectTests
{
    [Fact]
    public void AspectAppliesReplacesAndClearsLocalAspectValues()
    {
        Button button = new();
        ElementAspect first = new(
            [new ElementAspectValue(Control.BackgroundProperty, DrawColor.White)]);
        ElementAspect second = new(
            [new ElementAspectValue(Control.ForegroundProperty, DrawColor.Transparent)]);

        button.Aspect = first;

        Assert.Same(first, button.Aspect);
        Assert.Equal(DrawColor.White, button.Background);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = second;

        Assert.Equal(DrawColor.Transparent, button.Background);
        Assert.Equal(DrawColor.Transparent, button.Foreground);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.ForegroundProperty));

        button.Aspect = null;

        Assert.Null(button.Aspect);
        Assert.Equal(DrawColor.Black, button.Foreground);
    }

    [Fact]
    public void AspectRejectsDuplicateDefaultProperties()
    {
        Assert.Throws<ArgumentException>(() => new ElementAspect(
        [
            new ElementAspectValue(Control.BackgroundProperty, DrawColor.Black),
            new ElementAspectValue(Control.BackgroundProperty, DrawColor.White)
        ]));
    }
}
