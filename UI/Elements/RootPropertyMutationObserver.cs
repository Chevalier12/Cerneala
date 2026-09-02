using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

internal sealed class RootPropertyMutationObserver(UIRoot root) : UiPropertyMutationObserver
{
    internal override void OnPropertyMutated(UiPropertyMutation mutation)
    {
        root.Motion.Transactions.OnPropertyMutated(mutation);
        root.AspectProcessor.OnPropertyMutated(mutation);
    }
}
