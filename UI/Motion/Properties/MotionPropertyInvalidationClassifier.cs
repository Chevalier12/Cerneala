using Cerneala.UI.Core;

namespace Cerneala.UI.Motion.Properties;

public static class MotionPropertyInvalidationClassifier
{
    public static MotionPropertyInvalidationCategory Classify(UiProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return Classify(property.Options);
    }

    public static MotionPropertyInvalidationCategory Classify(UiPropertyOptions options)
    {
        MotionPropertyInvalidationCategory category = MotionPropertyInvalidationCategory.None;
        if ((options & (UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange)) != UiPropertyOptions.None)
        {
            category |= MotionPropertyInvalidationCategory.Layout;
        }

        if ((options & (UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual)) != UiPropertyOptions.None)
        {
            category |= MotionPropertyInvalidationCategory.Render;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsHitTest))
        {
            category |= MotionPropertyInvalidationCategory.HitTest;
        }

        if (options.HasFlag(UiPropertyOptions.AffectsSemantics))
        {
            category |= MotionPropertyInvalidationCategory.Semantics;
        }

        return category;
    }
}
