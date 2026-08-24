using Cerneala.UI.Elements;

namespace Cerneala.UI.Motion;

public static class MotionExtensions
{
    public static MotionElementFacade Motion(this UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new MotionElementFacade(element);
    }

    public static ObjectMotionFacade Motion(this object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.GetType().IsValueType)
        {
            throw new InvalidOperationException(
                "Object Motion requires a reference-type receiver so property writes affect the original object.");
        }

        return new ObjectMotionFacade(
            ObjectMotionRuntime.Current,
            target);
    }
}
