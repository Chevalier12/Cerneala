using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.UI.Elements;

public class UIElement : UiObject
{
    public static readonly UiProperty<bool> IsEnabledProperty = UiProperty<bool>.Register(
        nameof(IsEnabled),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true));

    public static readonly UiProperty<bool> IsVisibleProperty = UiProperty<bool>.Register(
        nameof(IsVisible),
        typeof(UIElement),
        new UiPropertyMetadata<bool>(true));

    public UIElement()
    {
        LogicalChildren = new UIElementCollection(this, ElementChildRole.Logical);
        VisualChildren = new UIElementCollection(this, ElementChildRole.Visual);
    }

    public UIElement? LogicalParent { get; private set; }

    public UIElement? VisualParent { get; private set; }

    public UIElementCollection LogicalChildren { get; }

    public UIElementCollection VisualChildren { get; }

    public UIRoot? Root { get; private set; }

    public bool IsAttached => Root is not null;

    public UiElementId? ElementId { get; private set; }

    public ElementHandlerStore Handlers { get; } = new();

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    internal bool HasAttachedParent =>
        (LogicalParent?.Root is not null) || (VisualParent?.Root is not null);

    internal void SetLogicalParent(UIElement? parent)
    {
        LogicalParent = parent;
    }

    internal void SetVisualParent(UIElement? parent)
    {
        VisualParent = parent;
    }

    internal void AttachToRoot(UIRoot root, UiElementId id)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        ElementId = id;
        OnAttached();
    }

    internal void DetachFromRoot()
    {
        OnDetached();
        ElementId = null;
        Root = null;
    }

    protected virtual void OnAttached()
    {
    }

    protected virtual void OnDetached()
    {
    }
}
