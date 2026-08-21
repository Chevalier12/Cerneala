using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

internal sealed class OverlayDismissScope
{
    private readonly Action dismiss;
    private readonly Func<UIElement?, bool>? contains;
    private bool isDismissing;

    public OverlayDismissScope(Action dismiss, Func<UIElement?, bool>? contains = null)
    {
        this.dismiss = dismiss ?? throw new ArgumentNullException(nameof(dismiss));
        this.contains = contains;
    }

    public bool Contains(UIElement? candidate) => contains?.Invoke(candidate) == true;

    public void Dismiss()
    {
        if (isDismissing)
        {
            return;
        }

        try
        {
            isDismissing = true;
            dismiss();
        }
        finally
        {
            isDismissing = false;
        }
    }
}
