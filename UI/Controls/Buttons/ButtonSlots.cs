using Cerneala.UI.Aspect;

namespace Cerneala.UI.Controls.Buttons;

public static class ButtonSlots
{
    public static readonly AspectSlot<Button, Border> Root = AspectSlot.For<Button, Border>("Root");
    public static readonly AspectSlot<Button, ContentPresenter> Content = AspectSlot.For<Button, ContentPresenter>("Content");
}
