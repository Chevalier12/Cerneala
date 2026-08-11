using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_SwatchButton", typeof(Button))]
[TemplatePart("PART_PickerOverlay", typeof(Overlay))]
[TemplatePart("PART_ColorPicker", typeof(ColorPicker))]
public class ColorSwatch : Control
{
    public static readonly RoutedEvent SelectedColorChangedEvent = RoutedEventRegistry.Register(
        nameof(SelectedColorChanged),
        typeof(ColorSwatch),
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventArgs<Color>));

    public static readonly UiProperty<Color> SelectedColorProperty = UiProperty<Color>.Register(
        nameof(SelectedColor),
        typeof(ColorSwatch),
        new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics));

    public static readonly UiProperty<bool> IsPickerOpenProperty = UiProperty<bool>.Register(
        nameof(IsPickerOpen),
        typeof(ColorSwatch),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics));

    private Button? swatchButton;
    private Overlay? pickerOverlay;
    private ColorPicker? picker;
    private bool synchronizing;

    public ColorSwatch()
    {
        Width = 20;
        Height = 20;
        SetValue(BorderBrushProperty, new SolidColorBrush(new Color(90, 98, 110)), UiPropertyValueSource.AspectBase);
        SetValue(BorderThicknessProperty, new Thickness(1), UiPropertyValueSource.AspectBase);
        SetValue(ComponentTemplateProperty, ColorSwatchTemplates.Default, UiPropertyValueSource.AspectBase);
    }

    public event EventHandler<RoutedPropertyChangedEventArgs<Color>> SelectedColorChanged
    {
        add => AddTypedHandler(SelectedColorChangedEvent, value);
        remove => RemoveTypedHandler(SelectedColorChangedEvent, value);
    }

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public bool IsPickerOpen
    {
        get => GetValue(IsPickerOpenProperty);
        set => SetValue(IsPickerOpenProperty, value);
    }

    public ColorPicker Picker
    {
        get
        {
            ApplyTemplate();
            return picker!;
        }
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        DetachTemplateParts();
        if (instance is null)
        {
            return;
        }

        swatchButton = GetRequiredTemplatePart<Button>("PART_SwatchButton");
        pickerOverlay = GetRequiredTemplatePart<Overlay>("PART_PickerOverlay");
        picker = GetRequiredTemplatePart<ColorPicker>("PART_ColorPicker");
        swatchButton.Click += OnSwatchClick;
        picker.SelectedColorChanged += OnPickerSelectedColorChanged;
        pickerOverlay.Opened += OnOverlayOpened;
        pickerOverlay.Closed += OnOverlayClosed;
        SynchronizeTemplateParts();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedColorProperty))
        {
            SynchronizeTemplateParts();
            RaiseEvent(new RoutedPropertyChangedEventArgs<Color>(
                SelectedColorChangedEvent,
                this,
                (Color)args.OldValue!,
                SelectedColor));
        }
        else if (ReferenceEquals(args.Property, IsPickerOpenProperty))
        {
            SynchronizeTemplateParts();
        }

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            GetSourceValue(ComponentTemplateProperty, UiPropertyValueSource.AspectBase) is ComponentTemplate)
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private void OnSwatchClick(UiElementId _, RoutedEventArgs args)
    {
        IsPickerOpen = true;
    }

    private void OnPickerSelectedColorChanged(object? sender, RoutedPropertyChangedEventArgs<Color> args)
    {
        if (!synchronizing)
        {
            SelectedColor = args.NewValue;
        }
    }

    private void OnOverlayOpened(UiElementId _, RoutedEventArgs args)
    {
        if (!IsPickerOpen)
        {
            IsPickerOpen = true;
        }
    }

    private void OnOverlayClosed(UiElementId _, RoutedEventArgs args)
    {
        if (IsPickerOpen)
        {
            IsPickerOpen = false;
        }
    }

    private void SynchronizeTemplateParts()
    {
        if (swatchButton is null || pickerOverlay is null || picker is null)
        {
            return;
        }

        synchronizing = true;
        try
        {
            swatchButton.Background = new SolidColorBrush(SelectedColor);
            picker.SelectedColor = SelectedColor;
            pickerOverlay.IsOpen = IsPickerOpen;
        }
        finally
        {
            synchronizing = false;
        }
    }

    private void DetachTemplateParts()
    {
        if (swatchButton is not null)
        {
            swatchButton.Click -= OnSwatchClick;
        }

        if (picker is not null)
        {
            picker.SelectedColorChanged -= OnPickerSelectedColorChanged;
        }

        if (pickerOverlay is not null)
        {
            pickerOverlay.Opened -= OnOverlayOpened;
            pickerOverlay.Closed -= OnOverlayClosed;
        }

        swatchButton = null;
        pickerOverlay = null;
        picker = null;
    }
}
