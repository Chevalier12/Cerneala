using Cerneala.UI.Elements;

namespace Cerneala.UI.Motion;

public static class MotionExtensions
{
    public static MotionElementFacade Motion(this UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new MotionElementFacade(element);
    }
}
