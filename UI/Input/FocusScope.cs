using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class FocusScope
{
    public FocusScope(UIElement owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public UIElement Owner { get; }
}
