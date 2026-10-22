using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Elements;

internal sealed class RootPropertyMutationObserver(UIRoot root) : UiPropertyMutationObserver
{
    internal override void OnPropertyMutated(UiPropertyMutation mutation)
    {
        root.Motion.Layout.OnPropertyMutated(mutation);
        root.Motion.Transactions.OnPropertyMutated(mutation);
        root.AspectProcessor.OnPropertyMutated(mutation);
        QueueInheritedSourceChange(mutation);
    }

    // A write or a clear that changes an inherited property's effective value queues the inherited work through
    // OnPropertyInvalidated. One that keeps the value but changes its source still changes whether the element sets the
    // property (IsSet), and so what its visual children inherit: a local value equal to the metadata default starts the
    // inheritance, and clearing it stops it. A framework default equal to the value keeps the source at Default, but
    // starts the inheritance too when no other source has a value.
    private static void QueueInheritedSourceChange(UiPropertyMutation mutation)
    {
        if (mutation.Target is not UIElement element ||
            !mutation.Property.Options.HasFlag(UiPropertyOptions.Inherits) ||
            !mutation.Property.AreEqualUntyped(mutation.OldEffectiveValue, mutation.NewEffectiveValue))
        {
            return;
        }

        bool frameworkDefaultWrite = mutation.MutatingSource == UiPropertyValueSource.Default &&
            mutation.NewEffectiveSource == UiPropertyValueSource.Default;
        if (mutation.OldEffectiveSource == mutation.NewEffectiveSource && !frameworkDefaultWrite)
        {
            return;
        }

        element.Invalidate(new InvalidationRequest(element, InvalidationFlags.Inherited, "Inherited property source changed", mutation.Property));
    }
}
