using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyOptions
{
    public MotionPropertyOptions(
        Type mixerType,
        MotionSpec defaultSpec,
        MotionPropertyInvalidationCategory invalidationCategory,
        bool isSafeForImplicitAnimation)
    {
        MixerType = mixerType ?? throw new ArgumentNullException(nameof(mixerType));
        DefaultSpec = defaultSpec ?? throw new ArgumentNullException(nameof(defaultSpec));
        InvalidationCategory = invalidationCategory;
        IsSafeForImplicitAnimation = isSafeForImplicitAnimation;
    }

    public Type MixerType { get; }

    public MotionSpec DefaultSpec { get; }

    public MotionPropertyInvalidationCategory InvalidationCategory { get; }

    public bool IsSafeForImplicitAnimation { get; }
}
