using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class Control : UIElement
{
    public static readonly UiProperty<ControlTemplate?> TemplateProperty = UiProperty<ControlTemplate?>.Register(
        nameof(Template),
        typeof(Control),
        new UiPropertyMetadata<ControlTemplate?>(
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

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

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

    public float FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public ControlTemplate? Template
    {
        get => GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public TemplateInstance? TemplateInstance { get; private set; }

    protected UIElement? TemplateChild => TemplateInstance?.Root;

    protected Thickness Insets => new(
        Padding.Left + BorderThickness.Left,
        Padding.Top + BorderThickness.Top,
        Padding.Right + BorderThickness.Right,
        Padding.Bottom + BorderThickness.Bottom);

    public void ApplyTemplate()
    {
        ControlTemplate? template = Template;
        if (TemplateInstance is not null && ReferenceEquals(TemplateInstanceTemplate, template))
        {
            return;
        }

        TemplateInstance?.Detach();
        TemplateInstance = null;
        TemplateInstanceTemplate = null;

        if (template is null)
        {
            return;
        }

        TemplateInstance instance = template.CreateInstance(this);
        instance.Attach(this);
        TemplateInstance = instance;
        TemplateInstanceTemplate = template;
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
        if (ReferenceEquals(args.Property, TemplateProperty))
        {
            ApplyTemplate();
        }
    }

    private ControlTemplate? TemplateInstanceTemplate { get; set; }

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
