using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.UI.Aspect;

public sealed class AspectSlotTests
{
    [Fact]
    public void SlotKeyIsTypedByOwnerAndPart()
    {
        AspectSlot<Button, ContentPresenter> slot = AspectSlot.For<Button, ContentPresenter>("Content");

        Assert.Equal("Content", slot.Name);
        Assert.Equal(typeof(Button), slot.OwnerType);
        Assert.Equal(typeof(ContentPresenter), slot.TargetType);
    }

    [Fact]
    public void RootSlotIsAlwaysAvailable()
    {
        AspectSlot<Button, Button> slot = AspectSlot.Root<Button>();

        Assert.Equal("Root", slot.Name);
        Assert.Equal(typeof(Button), slot.OwnerType);
        Assert.Equal(typeof(Button), slot.TargetType);
    }

    [Fact]
    public void SlotPathFormatsForDiagnostics()
    {
        AspectSlot<Button, ContentPresenter> slot = AspectSlot.For<Button, ContentPresenter>("Content");
        AspectSlotPath path = new(slot, "Root/Content");

        Assert.Equal("Button.Content (Root/Content)", path.ToString());
    }
}
