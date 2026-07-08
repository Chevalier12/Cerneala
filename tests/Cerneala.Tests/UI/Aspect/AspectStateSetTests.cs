using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectStateSetTests
{
    [Fact]
    public void StateSetTracksHoverPressedFocusDisabledSelected()
    {
        StatefulSelectableButton button = new()
        {
            IsPointerOver = true,
            IsPressed = true,
            IsKeyboardFocused = true,
            IsEnabled = false,
            IsSelected = true
        };

        AspectStateSet states = AspectStateSet.FromElement(button);

        Assert.True(states.Contains(AspectState.Hover));
        Assert.True(states.Contains(AspectState.Pressed));
        Assert.True(states.Contains(AspectState.Focus));
        Assert.True(states.Contains(AspectState.Disabled));
        Assert.True(states.Contains(AspectState.Selected));
    }

    [Fact]
    public void StateSetCanContainCustomNamedState()
    {
        AspectState danger = AspectState.Create("danger");

        AspectStateSet states = AspectStateSet.Empty.Add(danger);

        Assert.True(states.Contains(danger));
    }

    [Fact]
    public void StateSetEqualityIsOrderIndependent()
    {
        AspectStateSet first = AspectStateSet.Empty.Add(AspectState.Hover).Add(AspectState.Pressed);
        AspectStateSet second = AspectStateSet.Empty.Add(AspectState.Pressed).Add(AspectState.Hover);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    private sealed class StatefulSelectableButton : Button, ISelectableItemContainer
    {
        public int ItemIndex { get; set; }

        public object? Item { get; set; }

        public bool IsSelected { get; set; }
    }
}
