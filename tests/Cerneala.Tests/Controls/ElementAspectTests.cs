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
            [new ElementAspectValue(Control.BackgroundProperty, Color.White)]);
        ElementAspect second = new(
            [new ElementAspectValue(Control.ForegroundProperty, Color.Transparent)]);

        button.Aspect = first;

        Assert.Same(first, button.Aspect);
        Assert.Equal(Color.White, button.Background);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.BackgroundProperty));

        button.Aspect = second;

        Assert.Equal(Color.Transparent, button.Background);
        Assert.Equal(Color.Transparent, button.Foreground);
        Assert.Equal(UiPropertyValueSource.LocalAspectBase, button.GetValueSource(Control.ForegroundProperty));

        button.Aspect = null;

        Assert.Null(button.Aspect);
        Assert.Equal(Color.Black, button.Foreground);
    }

    [Fact]
    public void AspectRejectsDuplicateDefaultProperties()
    {
        Assert.Throws<ArgumentException>(() => new ElementAspect(
        [
            new ElementAspectValue(Control.BackgroundProperty, Color.Black),
            new ElementAspectValue(Control.BackgroundProperty, Color.White)
        ]));
    }
}
