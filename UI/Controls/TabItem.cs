using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class TabItem : ContentControl, ISelectableItemContainer
{
    public static readonly UiProperty<bool> IsSelectedProperty = UiProperty<bool>.Register(
        nameof(IsSelected),
        typeof(TabItem),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<object?> HeaderProperty = UiProperty<object?>.Register(
        nameof(Header),
        typeof(TabItem),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set
        {
            object? oldHeader = Header;
            if (ContentEqualityComparer.Equals(oldHeader, value))
            {
                SetValue(HeaderProperty, value);
                return;
            }

            ValidateCanOwnChild(this, value as UIElement);
            if (HostsHeaderDirectly)
            {
                RemoveHeaderElement(oldHeader);
                try
                {
                    AddHeaderElement(value);
                    SetValue(HeaderProperty, value);
                }
                catch
                {
                    RemoveHeaderElement(value);
                    AddHeaderElement(oldHeader);
                    SetValue(HeaderProperty, oldHeader);
                    throw;
                }

                return;
            }

            SetValue(HeaderProperty, value);
        }
    }

    private UIElement? HeaderElement => Header as UIElement;

    private bool HostsHeaderDirectly => Template is null;

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null || HeaderElement is null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = Deflate(context.AvailableSize, insets);
        LayoutSize headerSize = HeaderElement.Measure(new MeasureContext(available, context.Rounding));
        ContentElement?.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
        return Inflate(headerSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null || HeaderElement is null)
        {
            return base.ArrangeCore(context);
        }

        LayoutRect inner = Deflate(context.FinalRect, Insets);
        HeaderElement.Arrange(new ArrangeContext(inner, context.Rounding));
        ContentElement?.Arrange(new ArrangeContext(new LayoutRect(inner.X, inner.Y, 0, 0), context.Rounding));
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (!ReferenceEquals(args.Property, TemplateProperty))
        {
            base.OnPropertyChanged(args);
            return;
        }

        ReleaseHeaderElementFromOwnedSubtree();
        base.OnPropertyChanged(args);
        if (HostsHeaderDirectly)
        {
            AddHeaderElement(Header);
        }
    }

    private void AddHeaderElement(object? header)
    {
        if (header is not UIElement element)
        {
            return;
        }

        LogicalChildren.Add(element);
        VisualChildren.Add(element);
    }

    private void RemoveHeaderElement(object? header)
    {
        if (header is not UIElement element)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
    }

    private void ReleaseHeaderElementFromOwnedSubtree()
    {
        if (Header is UIElement element)
        {
            DetachChildFromOwnedSubtree(this, element);
        }
    }
}
