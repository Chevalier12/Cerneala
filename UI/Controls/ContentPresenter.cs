using System.Runtime.CompilerServices;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class ContentPresenter : Control
{
    private static readonly IEqualityComparer<object?> ContentReferenceEqualityComparer = new ReferenceContentEqualityComparer();
    private UIElement? presentedChild;
    private bool presentationDirty = true;

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(ContentPresenter),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ContentReferenceEqualityComparer));

    public static readonly UiProperty<DataTemplate?> ContentTemplateProperty = UiProperty<DataTemplate?>.Register(
        nameof(ContentTemplate),
        typeof(ContentPresenter),
        new UiPropertyMetadata<DataTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public DataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public UIElement? PresentedChild => presentedChild;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RefreshPresentedChild();
        return presentedChild?.Measure(context) ?? LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RefreshPresentedChild();
        presentedChild?.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ContentProperty) ||
            ReferenceEquals(args.Property, ContentTemplateProperty))
        {
            presentationDirty = true;
            RefreshPresentedChild();
        }
    }

    private void RefreshPresentedChild()
    {
        if (!presentationDirty)
        {
            return;
        }

        presentationDirty = false;
        UIElement? next = CreatePresentedChild();
        if (ReferenceEquals(presentedChild, next))
        {
            return;
        }

        RemovePresentedChild(presentedChild);
        try
        {
            AddPresentedChild(next);
            presentedChild = next;
        }
        catch
        {
            RemovePresentedChild(next);
            AddPresentedChild(presentedChild);
            throw;
        }
    }

    private UIElement? CreatePresentedChild()
    {
        object? content = Content;
        if (content is UIElement element)
        {
            return element;
        }

        DataTemplate? template = ContentTemplate;
        return template is null ? null : template.CreateElement(content);
    }

    private void AddPresentedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        ContentControl.ValidateCanOwnChild(this, child);
        LogicalChildren.Add(child);
        try
        {
            VisualChildren.Add(child);
        }
        catch
        {
            LogicalChildren.Remove(child);
            throw;
        }
    }

    private void RemovePresentedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        VisualChildren.Remove(child);
        LogicalChildren.Remove(child);
    }

    private sealed class ReferenceContentEqualityComparer : IEqualityComparer<object?>
    {
        public new bool Equals(object? left, object? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(object? value)
        {
            return value is null ? 0 : RuntimeHelpers.GetHashCode(value);
        }
    }
}
