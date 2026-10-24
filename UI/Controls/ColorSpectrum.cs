using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class ColorSpectrum : Control, IPointerDragSource
{
    public static readonly RoutedEvent ValueChangedEvent = RoutedEventRegistry.Register(
        nameof(ValueChanged),
        typeof(ColorSpectrum),
        RoutingStrategy.Bubble,
        typeof(RoutedEventArgs));

    public static readonly UiProperty<float> HueProperty = UiProperty<float>.Register(
        nameof(Hue),
        typeof(ColorSpectrum),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsRender,
            validateValue: float.IsFinite,
            coerceValue: CoerceHue));

    public static readonly UiProperty<float> SaturationProperty = UiProperty<float>.Register(
        nameof(Saturation),
        typeof(ColorSpectrum),
        new UiPropertyMetadata<float>(
            1,
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            validateValue: float.IsFinite,
            coerceValue: CoerceUnit));

    public static readonly UiProperty<float> ValueProperty = UiProperty<float>.Register(
        nameof(Value),
        typeof(ColorSpectrum),
        new UiPropertyMetadata<float>(
            1,
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            validateValue: float.IsFinite,
            coerceValue: CoerceUnit));

    private bool isDragging;
    private bool suppressValueChanged;

    public ColorSpectrum()
    {
        SetFrameworkDefault(FocusableProperty, true);
        SetFrameworkDefault(IsTabStopProperty, true);
        SetFrameworkDefault(CursorProperty, Cerneala.UI.Input.Cursor.Crosshair);
        SetFrameworkDefault(BorderBrushProperty, new SolidColorBrush(new Color(70, 76, 86)));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
        Handlers.AddHandler(InputEvents.MouseMoveEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseWheelEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseLeftButtonDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseLeftButtonUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseRightButtonDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseRightButtonUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => isDragging = false);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public float Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public float Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public float Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // A left press on an enabled spectrum starts its drag, as a press on a Thumb does (#619), so an enclosing button
    // is not pressed, does not repeat, and does not click for it.
    internal override bool OwnsPointerClick => IsEnabled;

    public void SetSelection(float saturation, float value)
    {
        float nextSaturation = Math.Clamp(saturation, 0, 1);
        float nextValue = Math.Clamp(value, 0, 1);
        if (Saturation == nextSaturation && Value == nextValue)
        {
            return;
        }

        suppressValueChanged = true;
        try
        {
            Saturation = nextSaturation;
            Value = nextValue;
        }
        finally
        {
            suppressValueChanged = false;
        }

        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(200, 140);
    }

    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        Color hueColor = ColorPickerColorMath.FromHsv(Hue, 1, 1);
        context.DrawingContext.FillRectangle(
            rect,
            new LinearGradientBrush(
                new DrawPoint(0, 0),
                new DrawPoint(rect.Width, 0),
                [new GradientStop(0, Color.White), new GradientStop(1, hueColor)]));
        context.DrawingContext.FillRectangle(
            rect,
            new LinearGradientBrush(
                new DrawPoint(0, 0),
                new DrawPoint(0, rect.Height),
                [new GradientStop(0, new Color(0, 0, 0, 0)), new GradientStop(1, Color.Black)]));

        Border.RenderBorder(this, context);

        float markerX = rect.X + (Saturation * rect.Width);
        float markerY = rect.Y + ((1 - Value) * rect.Height);
        DrawRect outer = new(markerX - 6, markerY - 6, 12, 12);
        DrawRect inner = new(markerX - 4, markerY - 4, 8, 8);
        context.DrawingContext.DrawEllipse(outer, new SolidColorBrush(Color.Black), 3);
        context.DrawingContext.DrawEllipse(inner, new SolidColorBrush(Color.White), 2);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (!suppressValueChanged &&
            (ReferenceEquals(args.Property, HueProperty) ||
             ReferenceEquals(args.Property, SaturationProperty) ||
             ReferenceEquals(args.Property, ValueProperty)))
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
        }
    }

    bool IPointerDragSource.BeginPointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args)
    {
        if (!IsEnabled || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        isDragging = true;
        UpdateSelectionFromPointer(args);
        captureManager.Capture(this, routeMap);
        args.Handled = true;
        return true;
    }

    bool IPointerDragSource.UpdatePointerDrag(MouseEventArgs args)
    {
        if (!isDragging)
        {
            return false;
        }

        UpdateSelectionFromPointer(args);
        args.Handled = true;
        return true;
    }

    bool IPointerDragSource.CompletePointerDrag(
        PointerCaptureManager captureManager,
        ElementInputRouteMap routeMap,
        MouseButtonEventArgs args)
    {
        if (!isDragging || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        UpdateSelectionFromPointer(args);
        isDragging = false;
        captureManager.Release(routeMap);
        args.Handled = true;
        return true;
    }

    // The root point goes back through the render transforms of the spectrum and every ancestor, so the
    // selection is the point drawn under the pointer. A point that cannot be converted keeps the selection.
    private void UpdateSelectionFromPointer(MouseEventArgs args)
    {
        if (!InputCoordinateConverter.TryRootToElement(this, new Vector2(args.RootX, args.RootY), out Vector2 point))
        {
            return;
        }

        LayoutRect bounds = ArrangedBounds;
        float saturation = bounds.Width <= 0 ? 0 : point.X / bounds.Width;
        float value = bounds.Height <= 0 ? 0 : 1 - (point.Y / bounds.Height);
        SetSelection(saturation, value);
    }

    // The pointer input of the spectrum's own drag is handled, as a Thumb handles it, so the release that ends the drag
    // does not click an enclosing button.
    private void OnCapturedPointerInput(object _, RoutedEventArgs args)
    {
        if (isDragging)
        {
            args.Handled = true;
        }
    }

    private void OnKeyDown(object _, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs || !IsEnabled)
        {
            return;
        }

        const float Step = 0.01f;
        switch (keyArgs.Key)
        {
            case InputKey.Left:
                SetSelection(Saturation - Step, Value);
                break;
            case InputKey.Right:
                SetSelection(Saturation + Step, Value);
                break;
            case InputKey.Up:
                SetSelection(Saturation, Value + Step);
                break;
            case InputKey.Down:
                SetSelection(Saturation, Value - Step);
                break;
            default:
                return;
        }

        args.Handled = true;
    }

    private static float CoerceHue(UiObject _, float value)
    {
        return Math.Clamp(value, 0, 360);
    }

    private static float CoerceUnit(UiObject _, float value)
    {
        return Math.Clamp(value, 0, 1);
    }
}
