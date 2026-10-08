using Cerneala.Timbre;
using Cerneala.UI.Elements;
using Cerneala.UI.Timbre;

namespace Cerneala.UI.Motion;

public static class MotionExtensions
{
    public static MotionElementFacade Motion(this UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new MotionElementFacade(element);
    }

    // Audio Motion of one captured playback, sampled by the UIRoot that owns
    // its element scope. Declared here so it always outranks the generic
    // object facade below for a TimbrePlayback receiver.
    public static TimbreMotionFacade Motion(this TimbrePlayback playback)
    {
        ArgumentNullException.ThrowIfNull(playback);
        return new TimbreMotionFacade(playback, root: null);
    }

    // Audio Motion sampled by an explicit root, for playbacks of scopes that
    // no element owns (Application.Timbre or a standalone runtime).
    public static TimbreMotionFacade Motion(this TimbrePlayback playback, UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(playback);
        ArgumentNullException.ThrowIfNull(root);
        return new TimbreMotionFacade(playback, root);
    }

    public static MotionElementFacade Motion<TElement>(this TElement element)
        where TElement : UIElement
    {
        ArgumentNullException.ThrowIfNull(element);
        return new MotionElementFacade(element);
    }

    public static ObjectMotionFacade<TTarget> Motion<TTarget>(
        this TTarget target,
        params object[] _)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);
        return new ObjectMotionFacade<TTarget>(
            ObjectMotionRuntime.Current,
            target);
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
