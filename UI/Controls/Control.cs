using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

public class Control : UIElement
{
    public static readonly RoutedEvent PreviewMouseDoubleClickEvent = InputEvents.PreviewMouseDoubleClickEvent.AddOwner(typeof(Control));
    public static readonly RoutedEvent MouseDoubleClickEvent = InputEvents.MouseDoubleClickEvent.AddOwner(typeof(Control));

    public event RoutedEventHandler PreviewMouseDoubleClick { add => AddHandler(PreviewMouseDoubleClickEvent, value); remove => RemoveHandler(PreviewMouseDoubleClickEvent, value); }
    public event RoutedEventHandler MouseDoubleClick { add => AddHandler(MouseDoubleClickEvent, value); remove => RemoveHandler(MouseDoubleClickEvent, value); }
    public static readonly UiProperty<ComponentTemplate?> ComponentTemplateProperty = UiProperty<ComponentTemplate?>.Register(
        nameof(ComponentTemplate),
        typeof(Control),
        new UiPropertyMetadata<ComponentTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure |
            UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender |
            UiPropertyOptions.AffectsHitTest |
            UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<DrawColor> BackgroundProperty = UiProperty<DrawColor>.Register(
        nameof(Background),
        typeof(Control),
        new UiPropertyMetadata<DrawColor>(DrawColor.Transparent, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<DrawColor> ForegroundProperty = UiProperty<DrawColor>.Register(
        nameof(Foreground),
        typeof(Control),
        new UiPropertyMetadata<DrawColor>(DrawColor.Black, UiPropertyOptions.Inherits | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawColor> BorderColorProperty = UiProperty<DrawColor>.Register(
        nameof(BorderColor),
        typeof(Control),
        new UiPropertyMetadata<DrawColor>(DrawColor.Transparent, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<Thickness> BorderThicknessProperty = UiProperty<Thickness>.Register(
        nameof(BorderThickness),
        typeof(Control),
        new UiPropertyMetadata<Thickness>(Thickness.Zero, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender, validateValue: IsValidThickness));

    public static readonly UiProperty<Thickness> PaddingProperty = UiProperty<Thickness>.Register(
        nameof(Padding),
        typeof(Control),
        new UiPropertyMetadata<Thickness>(Thickness.Zero, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender, validateValue: IsValidThickness));

    public static readonly UiProperty<string> FontFamilyProperty = UiProperty<string>.Register(
        nameof(FontFamily),
        typeof(Control),
        new UiPropertyMetadata<string>(
            "Default",
            UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => !string.IsNullOrWhiteSpace(value)));

    public static readonly UiProperty<float> FontSizeProperty = UiProperty<float>.Register(
        nameof(FontSize),
        typeof(Control),
        new UiPropertyMetadata<float>(
            16,
            UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => value > 0 && float.IsFinite(value)));

    public DrawColor Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public DrawColor Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public DrawColor BorderColor
    {
        get => GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public string FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public float FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public ComponentTemplate? ComponentTemplate
    {
        get => GetValue(ComponentTemplateProperty);
        set => SetValue(ComponentTemplateProperty, value);
    }

    public AspectVariantSet AspectVariants { get; private set; } = AspectVariantSet.Empty;

    public void SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)
    {
        AspectVariantSet next = AspectVariants.Set(key, value);
        if (AspectVariants.Equals(next))
        {
            return;
        }

        AspectVariants = next;
        Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Render, "Aspect variant changed");
    }

    public ComponentTemplateInstance? ComponentTemplateInstance { get; private set; }

    protected UIElement? TemplateChild => ComponentTemplateInstance?.Root;

    protected Thickness Insets => new(
        Padding.Left + BorderThickness.Left,
        Padding.Top + BorderThickness.Top,
        Padding.Right + BorderThickness.Right,
        Padding.Bottom + BorderThickness.Bottom);

    public void ApplyTemplate()
    {
        ComponentTemplate? template = ComponentTemplate;
        if (ComponentTemplateInstance is not null && ReferenceEquals(ComponentTemplateInstanceTemplate, template))
        {
            return;
        }

        ComponentTemplateInstance?.Detach();
        ComponentTemplateInstance = null;
        ComponentTemplateInstanceTemplate = null;

        if (template is null)
        {
            return;
        }

        AspectEnvironment environment = Root?.ThemeProvider is null
            ? new AspectEnvironment("template")
            : ThemeTokenBridge.CreateEnvironment(Root.ThemeProvider.Theme);
        ComponentTemplateContext context = new(this, environment, AspectStateSet.FromElement(this), AspectVariants);
        ComponentTemplateInstance instance = template.CreateInstance(this, context);
        instance.Attach(this);
        ComponentTemplateInstance = instance;
        ComponentTemplateInstanceTemplate = template;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        return TemplateChild?.Measure(context) ?? LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ApplyTemplate();
        TemplateChild?.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ComponentTemplateProperty))
        {
            ApplyTemplate();
        }
    }

    private ComponentTemplate? ComponentTemplateInstanceTemplate { get; set; }

    private static bool IsValidThickness(Thickness value)
    {
        return IsValidThicknessSide(value.Left) &&
            IsValidThicknessSide(value.Top) &&
            IsValidThicknessSide(value.Right) &&
            IsValidThicknessSide(value.Bottom);
    }

    private static bool IsValidThicknessSide(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }
}
