using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;

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

    public static readonly UiProperty<Brush?> BackgroundProperty = UiProperty<Brush?>.Register(
        nameof(Background),
        typeof(Control),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public static readonly UiProperty<Brush?> ForegroundProperty = UiProperty<Brush?>.Register(
        nameof(Foreground),
        typeof(Control),
        new UiPropertyMetadata<Brush?>(new SolidColorBrush(Color.Black), UiPropertyOptions.Inherits | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Brush?> BorderBrushProperty = UiProperty<Brush?>.Register(
        nameof(BorderBrush),
        typeof(Control),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

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

    public Brush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Brush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public Brush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
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
        if (ReferenceEquals(ComponentTemplateInstanceTemplate, template) &&
            (template is null || ComponentTemplateInstance is not null))
        {
            return;
        }

        ComponentTemplateInstance? previousInstance = ComponentTemplateInstance;
        ComponentTemplateInstance = null;
        ComponentTemplateInstanceTemplate = null;
        if (previousInstance is not null)
        {
            try
            {
                OnTemplateApplied(null);
            }
            finally
            {
                previousInstance.Dispose();
            }
        }

        if (template is null)
        {
            return;
        }

        AspectEnvironment environment = Root?.AspectProcessor.Environment ?? new AspectEnvironment("template");
        ComponentTemplateContext context = new(this, environment, AspectStateSet.FromElement(this), AspectVariants);
        ComponentTemplateInstance instance = template.CreateInstance(this, context);
        try
        {
            instance.Attach(this);
            ResolvingTemplateInstance = instance;
            OnTemplateApplied(instance);
            ComponentTemplateInstance = instance;
            ComponentTemplateInstanceTemplate = template;
        }
        catch
        {
            try
            {
                OnTemplateApplied(null);
            }
            finally
            {
                ResolvingTemplateInstance = null;
                instance.Dispose();
            }

            throw;
        }
        finally
        {
            ResolvingTemplateInstance = null;
        }
    }

    protected virtual void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
    }

    protected TElement GetRequiredTemplatePart<TElement>(string name)
        where TElement : UIElement
    {
        UIElement? element = GetTemplatePart(name);
        if (element is null)
        {
            throw new InvalidOperationException(
                $"Required template part '{name}' of type '{typeof(TElement).FullName}' was not provided for '{GetType().FullName}'.");
        }

        return element as TElement ?? throw new InvalidOperationException(
            $"Template part '{name}' for '{GetType().FullName}' must be of type '{typeof(TElement).FullName}', but was '{element.GetType().FullName}'.");
    }

    protected TElement? GetOptionalTemplatePart<TElement>(string name)
        where TElement : UIElement
    {
        UIElement? element = GetTemplatePart(name);
        if (element is null)
        {
            return null;
        }

        return element as TElement ?? throw new InvalidOperationException(
            $"Template part '{name}' for '{GetType().FullName}' must be of type '{typeof(TElement).FullName}', but was '{element.GetType().FullName}'.");
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

    private ComponentTemplateInstance? ResolvingTemplateInstance { get; set; }

    private UIElement? GetTemplatePart(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template part name cannot be empty.", nameof(name));
        }

        ComponentTemplateInstance? instance = ResolvingTemplateInstance ?? ComponentTemplateInstance;
        return instance?.Parts.TryGetValue(name, out UIElement? element) == true ? element : null;
    }

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
