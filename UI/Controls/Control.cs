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

    public static readonly UiProperty<string?> ComponentTemplateKeyProperty = UiProperty<string?>.Register(
        nameof(ComponentTemplateKey),
        typeof(Control),
        new UiPropertyMetadata<string?>(
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

    public static readonly UiProperty<FontWeight> FontWeightProperty = UiProperty<FontWeight>.Register(
        nameof(FontWeight),
        typeof(Control),
        new UiPropertyMetadata<FontWeight>(
            FontWeight.Normal,
            UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<FontStyle> FontStyleProperty = UiProperty<FontStyle>.Register(
        nameof(FontStyle),
        typeof(Control),
        new UiPropertyMetadata<FontStyle>(
            FontStyle.Normal,
            UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<FontStretch> FontStretchProperty = UiProperty<FontStretch>.Register(
        nameof(FontStretch),
        typeof(Control),
        new UiPropertyMetadata<FontStretch>(
            FontStretch.Normal,
            UiPropertyOptions.Inherits | UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    // As in WPF, the defaults are Left and Top; a control that places its content reads them, for example Button,
    // whose framework defaults are Center, and a template that binds them to its ContentPresenter's alignment.
    public static readonly UiProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = UiProperty<HorizontalAlignment>.Register(
        nameof(HorizontalContentAlignment),
        typeof(Control),
        new UiPropertyMetadata<HorizontalAlignment>(HorizontalAlignment.Left, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<VerticalAlignment> VerticalContentAlignmentProperty = UiProperty<VerticalAlignment>.Register(
        nameof(VerticalContentAlignment),
        typeof(Control),
        new UiPropertyMetadata<VerticalAlignment>(VerticalAlignment.Top, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

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

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public ComponentTemplate? ComponentTemplate
    {
        get => GetValue(ComponentTemplateProperty);
        set => SetValue(ComponentTemplateProperty, value);
    }

    public string? ComponentTemplateKey
    {
        get => GetValue(ComponentTemplateKeyProperty);
        set => SetValue(ComponentTemplateKeyProperty, value);
    }

    public AspectVariantSet AspectVariants { get; private set; } = AspectVariantSet.Empty;

    public void SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!key.OwnerType.IsInstanceOfType(this))
        {
            throw new ArgumentException(
                $"Aspect variant '{key}' cannot be set on control '{GetType().FullName}'.",
                nameof(key));
        }

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
        ComponentTemplate? template = ComponentTemplate ??
            Root?.AspectProcessor.ResolveComponentTemplate(this, ComponentTemplateKey);
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

        AspectEnvironment environment = Root?.AspectProcessor.GetEnvironment(this) ?? new AspectEnvironment("template");
        ComponentTemplateContext context = new(this, environment, AspectStateSet.FromElement(this), AspectVariants);
        OnTemplateInstanceCreating();
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

    // True for a control that builds its first template instance only in its own measure, as every WPF control does
    // (FrameworkElement.ApplyTemplate runs in MeasureCore). A Collapsed control is never measured, so it never builds
    // it: a ScrollViewer's hidden ScrollBar has no Track, Thumb or RepeatButton until it is shown (#714). A control
    // that already has an instance still replaces it at once when its template changes.
    internal virtual bool BuildsFirstTemplateInMeasure => false;

    // Applies the template outside layout: when ComponentTemplate or ComponentTemplateKey changes, and in the Aspect
    // pass, where a ComponentTemplateKey resolves through the aspect catalog.
    internal void ApplyTemplateOutsideLayout()
    {
        if (BuildsFirstTemplateInMeasure && ComponentTemplateInstance is null)
        {
            return;
        }

        ApplyTemplate();
    }

    // Applies the template in this control's measure or arrange. A template built here, such as the first one of a
    // control that builds it in measure, gets the frame's Inherited and Aspect work before it is measured, because
    // the frame ran those phases before the layout; so it is laid out and drawn in this frame as if it had been built
    // before the layout, and leaves no work for the next frame.
    internal void ApplyTemplateInLayout()
    {
        ComponentTemplateInstance? previous = ComponentTemplateInstance;
        ApplyTemplate();
        if (ComponentTemplateInstance is { Root: { } templateRoot } instance &&
            !ReferenceEquals(instance, previous))
        {
            Root?.ProcessInheritedAndAspectForSubtree(this, templateRoot);
        }
    }

    // Runs right before a template builds a new instance, however the template is applied: when ComponentTemplate or
    // ComponentTemplateKey changes, in measure or arrange, or when a ComponentTemplateKey starts to resolve through
    // the aspect catalog.
    internal virtual void OnTemplateInstanceCreating()
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
        ApplyTemplateInLayout();
        return TemplateChild?.Measure(context) ?? LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        ApplyTemplateInLayout();
        TemplateChild?.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ComponentTemplateProperty) ||
            ReferenceEquals(args.Property, ComponentTemplateKeyProperty))
        {
            ApplyTemplateOutsideLayout();
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
