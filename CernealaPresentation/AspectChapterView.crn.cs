using System.Collections;
using System.Globalization;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using ServoApi = Cerneala.UI.Servo.Servo;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

namespace Cerneala.Presentation;

internal enum AspectStudioElementKind
{
    Border,
    TextBlock,
    Button
}

public partial class AspectChapterView : UserControl
{
    internal static readonly UiProperty<IEnumerable?> PropertyRowsProperty = UiProperty<IEnumerable?>.Register(
        nameof(PropertyRows),
        typeof(AspectChapterView),
        new UiPropertyMetadata<IEnumerable?>(null));

    private static readonly HashSet<UiProperty> HiddenProperties =
    [
        UIElement.DataContextProperty,
        UIElement.AspectProperty,
        UIElement.LayoutMotionIdProperty,
        UIElement.LayoutMotionOptionsProperty,
        UIElement.PresenceProperty,
        UIElement.IsPointerOverProperty,
        UIElement.IsKeyboardFocusedProperty,
        UIElement.IsKeyboardFocusWithinProperty,
        Control.ComponentTemplateProperty,
        Control.ComponentTemplateKeyProperty,
        ButtonBase.IsPressedProperty,
        ButtonBase.CommandProperty,
        ButtonBase.CommandParameterProperty
    ];

    private readonly Dictionary<AspectStudioElementKind, AspectStudioTarget> targets = [];
    private AspectStudioElementKind selectedKind = AspectStudioElementKind.Border;
    private bool editorBuilt;
    private bool active;

    internal UIElement SelectedPreview => SelectedTarget.Element;

    internal IReadOnlyList<UiProperty> SelectedProperties => GetInspectableProperties(SelectedPreview.GetType());

    internal IEnumerable? PropertyRows
    {
        get => GetValue(PropertyRowsProperty);
        set => SetValue(PropertyRowsProperty, value);
    }

    private Brush PanelBrush => FindResource<Brush>("PanelBrush");

    private Brush SelectedBrush => FindResource<Brush>("CyanWashBrush");

    private Brush LineBrush => FindResource<Brush>("LineStrongBrush");

    private Brush MutedBrush => FindResource<Brush>("SlateBrush");

    private Brush CyanBrush => FindResource<Brush>("CyanBrush");

    private Brush PinkBrush => FindResource<Brush>("PinkBrush");

    private Brush LimeBrush => FindResource<Brush>("LimeBrush");

    protected override void OnAttached()
    {
        base.OnAttached();
        if (Visibility == Visibility.Visible)
        {
            Activate();
        }
    }

    protected override void OnDetached()
    {
        Deactivate();
        ReleaseDynamicControls();
        base.OnDetached();
    }

    internal void Activate()
    {
        active = true;
        EnsureEditorBuilt();
    }

    internal void Deactivate()
    {
        active = false;
    }

    internal void PrepareEditor() => EnsureEditorBuilt();

    internal void PrepareEditorForTests() => EnsureEditorBuilt();

    internal void UpdateDiagnostics()
    {
        if (active && editorBuilt)
        {
            UpdateStatus("LIVE");
        }
    }

    internal void SelectForTests(AspectStudioElementKind kind)
    {
        EnsureEditorBuilt();
        SelectTarget(kind);
    }

    internal void FilterPropertyForTests(string propertyName)
    {
        PropertySearch.Text = propertyName ?? string.Empty;
    }

    internal bool TrySetPropertyForTests(UiProperty property, string text)
    {
        if (!TryParseValue(property, text, out object? value, out _))
        {
            return false;
        }

        CommitProperty(SelectedTarget, property, value);
        return true;
    }

    private AspectStudioTarget SelectedTarget => targets[selectedKind];

    private void EnsureEditorBuilt()
    {
        if (editorBuilt)
        {
            return;
        }

        if (targets.Count == 0)
        {
            ServoApi.SetId(BorderElementButton, "aspect-element-border");
            ServoApi.SetId(TextBlockElementButton, "aspect-element-textblock");
            ServoApi.SetId(ButtonElementButton, "aspect-element-button");
            BuildTargets();
        }

        editorBuilt = true;
        SelectTarget(selectedKind);
    }

    private void BuildTargets()
    {
        ButtonPreview.Cursor = Cerneala.UI.Input.Cursor.Hand;
        AddTarget(
            AspectStudioElementKind.Border,
            "BORDER",
            BorderPreview,
            Control.BackgroundProperty,
            Control.BorderBrushProperty,
            Control.BorderThicknessProperty,
            Control.PaddingProperty,
            Control.FontFamilyProperty,
            Control.FontSizeProperty,
            Control.ForegroundProperty,
            UIElement.WidthProperty,
            UIElement.HeightProperty,
            UIElement.HorizontalAlignmentProperty,
            UIElement.VerticalAlignmentProperty);

        AddTarget(
            AspectStudioElementKind.TextBlock,
            "TEXTBLOCK",
            TextBlockPreview,
            TextBlock.TextProperty,
            Control.FontFamilyProperty,
            Control.FontSizeProperty,
            Control.ForegroundProperty,
            TextBlock.TextWrappingProperty,
            UIElement.WidthProperty,
            UIElement.HorizontalAlignmentProperty,
            UIElement.VerticalAlignmentProperty);

        AddTarget(
            AspectStudioElementKind.Button,
            "BUTTON",
            ButtonPreview,
            ContentControl.ContentProperty,
            Control.BackgroundProperty,
            Control.ForegroundProperty,
            Control.BorderBrushProperty,
            Control.BorderThicknessProperty,
            Control.PaddingProperty,
            Control.FontFamilyProperty,
            Control.FontSizeProperty,
            UIElement.HorizontalAlignmentProperty,
            UIElement.VerticalAlignmentProperty,
            UIElement.FocusableProperty,
            UIElement.IsTabStopProperty,
            UIElement.CursorProperty);
    }

    private void AddTarget(
        AspectStudioElementKind kind,
        string name,
        UIElement element,
        params UiProperty[] properties)
    {
        (UiProperty Property, object? Value)[] values = properties
            .Select(property => (property, element.GetValue(property)))
            .ToArray();
        foreach (UiProperty property in properties)
        {
            UiPropertyValueSource source = element.GetValueSource(property);
            if (source is UiPropertyValueSource.MarkupBase or UiPropertyValueSource.Local)
            {
                element.ClearValueUntyped(property, source);
            }
        }

        AspectStudioTarget target = new(kind, name, element, values);
        targets.Add(kind, target);
        ApplyTargetAspect(target);
    }

    private void ReleaseDynamicControls()
    {
        if (!editorBuilt)
        {
            return;
        }

        PropertyRows = null;
        editorBuilt = false;
    }

    private void OnReset(UiElementId sender, RoutedEventArgs args)
    {
        AspectStudioTarget target = SelectedTarget;
        target.Reset();
        ApplyTargetAspect(target);
        RebuildPropertyList();
        UpdateStatus("RESET");
    }

    private void OnSelectBorder(UiElementId sender, RoutedEventArgs args) => SelectTarget(AspectStudioElementKind.Border);

    private void OnSelectTextBlock(UiElementId sender, RoutedEventArgs args) => SelectTarget(AspectStudioElementKind.TextBlock);

    private void OnSelectButton(UiElementId sender, RoutedEventArgs args) => SelectTarget(AspectStudioElementKind.Button);

    private void OnPropertySearchChanged(object? sender, TextChangedEventArgs args)
    {
        if (editorBuilt)
        {
            RebuildPropertyList();
        }
    }

    private void SelectTarget(AspectStudioElementKind kind)
    {
        selectedKind = kind;
        AspectStudioTarget target = SelectedTarget;
        BorderPreviewHost.Visibility = kind == AspectStudioElementKind.Border ? Visibility.Visible : Visibility.Collapsed;
        TextBlockPreviewHost.Visibility = kind == AspectStudioElementKind.TextBlock ? Visibility.Visible : Visibility.Collapsed;
        ButtonPreviewHost.Visibility = kind == AspectStudioElementKind.Button ? Visibility.Visible : Visibility.Collapsed;
        PreviewTypeText.Text = $"{target.Name} / LOCAL ASPECT";
        BorderElementButton.Background = kind == AspectStudioElementKind.Border ? SelectedBrush : PanelBrush;
        TextBlockElementButton.Background = kind == AspectStudioElementKind.TextBlock ? SelectedBrush : PanelBrush;
        ButtonElementButton.Background = kind == AspectStudioElementKind.Button ? SelectedBrush : PanelBrush;
        RebuildPropertyList();
        UpdateStatus("READY");
    }

    private void RebuildPropertyList()
    {
        if (!editorBuilt)
        {
            return;
        }

        AspectStudioTarget target = SelectedTarget;
        IReadOnlyList<UiProperty> allProperties = GetInspectableProperties(target.Element.GetType());
        string filter = PropertySearch.Text.Trim();
        UiProperty[] visibleProperties = allProperties
            .Where(property => filter.Length == 0 ||
                property.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                property.OwnerType.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        List<AspectStudioPropertyRowModel> rows = [];
        Type? previousOwner = null;
        foreach (UiProperty property in visibleProperties)
        {
            if (property.OwnerType != previousOwner)
            {
                rows.Add(new AspectStudioHeaderRow(
                    property.OwnerType.Name.ToUpperInvariant(),
                    OwnerBrush(property.OwnerType)));
                previousOwner = property.OwnerType;
            }

            rows.Add(CreatePropertyRow(target, property));
        }

        if (visibleProperties.Length == 0)
        {
            rows.Add(new AspectStudioHeaderRow("NO MATCHES", MutedBrush));
        }

        PropertyRows = rows;

        PropertyCountText.Text = filter.Length == 0
            ? $"{allProperties.Count:00} EDITABLE"
            : $"{visibleProperties.Length:00} / {allProperties.Count:00}";
        UpdateStatus(StatusMessage.Text);
    }

    private AspectStudioPropertyRowModel CreatePropertyRow(AspectStudioTarget target, UiProperty property)
    {
        object? current = target.GetValue(property);
        Brush labelBrush = target.Modified.Contains(property) ? LimeBrush : MutedBrush;
        Action<object?> commit = value => CommitProperty(target, property, value);
        Type valueType = property.ValueType;
        if (valueType == typeof(bool))
        {
            return new AspectStudioBooleanRow(property.Name, labelBrush, current is true, commit);
        }

        if (valueType.IsEnum)
        {
            object?[] values = Enum.GetValues(valueType).Cast<object?>().ToArray();
            int selectedIndex = Array.FindIndex(values, value => Equals(value, current));
            return new AspectStudioChoiceRow(
                property.Name,
                labelBrush,
                values,
                selectedIndex,
                value => value,
                commit);
        }

        if (Nullable.GetUnderlyingType(valueType) == typeof(Cursor))
        {
            object?[] values = ["DEFAULT", "ARROW", "HAND", "IBEAM", "CROSSHAIR"];
            string selected = current is Cursor cursor ? cursor.Name.ToUpperInvariant() : "DEFAULT";
            return new AspectStudioChoiceRow(
                property.Name,
                labelBrush,
                values,
                Array.FindIndex(values, value => Equals(value, selected)),
                value => value is string name && name != "DEFAULT"
                    ? new Cursor(name.Equals("IBEAM", StringComparison.Ordinal) ? "IBeam" : ToTitleCase(name))
                    : null,
                commit);
        }

        Func<string, (bool Success, object? Value, string Error)> parse = text =>
        {
            bool success = TryParseValue(property, text, out object? value, out string error);
            return (success, value, error);
        };
        if (typeof(Brush).IsAssignableFrom(valueType))
        {
            Brush? brush = current as Brush;
            return new AspectStudioColorRow(
                property.Name,
                labelBrush,
                FormatBrush(brush),
                brush is SolidColorBrush solid ? solid.Color : Color.Transparent,
                LineBrush,
                LineBrush,
                PinkBrush,
                parse,
                commit,
                UpdateStatus);
        }

        return new AspectStudioTextRow(
            property.Name,
            labelBrush,
            FormatValue(property, current),
            LineBrush,
            LineBrush,
            PinkBrush,
            parse,
            commit,
            UpdateStatus);
    }

    private void CommitProperty(AspectStudioTarget target, UiProperty property, object? value)
    {
        target.Values[property] = value;
        target.Modified.Add(property);
        (target.Element.Aspect ?? throw new InvalidOperationException("The Aspect Studio target has no local aspect."))
            .SetValue(property, value);
        UpdateStatus($"{property.Name.ToUpperInvariant()} UPDATED");
    }

    private static void ApplyTargetAspect(AspectStudioTarget target)
    {
        target.Element.Aspect = new ElementAspect(target.Values
            .Select(pair => new ElementAspectValue(pair.Key, pair.Value))
            .ToArray());
    }

    private void UpdateStatus(string message)
    {
        AspectStudioTarget target = SelectedTarget;
        StatusElement.Text = target.Name;
        StatusModified.Text = $"MODIFIED {target.Modified.Count:00}";
        StatusMessage.Text = message.ToUpperInvariant();
        StatusMessage.Foreground = message.Contains("INVALID", StringComparison.OrdinalIgnoreCase) ? PinkBrush : MutedBrush;
    }

    private static IReadOnlyList<UiProperty> GetInspectableProperties(Type elementType)
    {
        return UiPropertyRegistry.GetRegisteredProperties()
            .Where(property => property.OwnerType.IsAssignableFrom(elementType))
            .Where(property => !property.IsReadOnly && !HiddenProperties.Contains(property))
            .Where(IsSupportedProperty)
            .OrderBy(property => InheritanceDistance(elementType, property.OwnerType))
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsSupportedProperty(UiProperty property)
    {
        Type valueType = property.ValueType;
        return valueType == typeof(string) ||
            valueType == typeof(bool) ||
            valueType == typeof(int) ||
            valueType == typeof(float) ||
            valueType.IsEnum ||
            valueType == typeof(Thickness) ||
            valueType == typeof(LayoutPoint) ||
            valueType == typeof(Transform) ||
            typeof(Brush).IsAssignableFrom(valueType) ||
            Nullable.GetUnderlyingType(valueType) == typeof(Cursor) ||
            ReferenceEquals(property, ContentControl.ContentProperty);
    }

    private static int InheritanceDistance(Type elementType, Type ownerType)
    {
        int distance = 0;
        for (Type? current = elementType; current is not null; current = current.BaseType)
        {
            if (current == ownerType)
            {
                return distance;
            }

            distance++;
        }

        return int.MaxValue;
    }

    private static bool TryParseValue(UiProperty property, string text, out object? value, out string error)
    {
        string trimmed = text.Trim();
        Type valueType = property.ValueType;
        value = null;
        error = $"INVALID {property.Name.ToUpperInvariant()}";

        if (valueType == typeof(string) || ReferenceEquals(property, ContentControl.ContentProperty))
        {
            value = text;
            return true;
        }

        if (valueType == typeof(bool) && bool.TryParse(trimmed, out bool boolean))
        {
            value = boolean;
            return true;
        }

        if (valueType == typeof(int) && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
        {
            if (ReferenceEquals(property, UIElement.TabIndexProperty) && integer < 0)
            {
                return false;
            }

            value = integer;
            return true;
        }

        if (valueType == typeof(float))
        {
            bool isAutoDimension =
                (ReferenceEquals(property, UIElement.WidthProperty) || ReferenceEquals(property, UIElement.HeightProperty)) &&
                trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase);
            float parsedFloat = float.NaN;
            if (!isAutoDimension &&
                !float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedFloat))
            {
                return false;
            }

            float number = isAutoDimension ? float.NaN : parsedFloat;
            if (!ValidateFloat(property, number))
            {
                return false;
            }

            value = number;
            return true;
        }

        if (valueType.IsEnum && Enum.TryParse(valueType, trimmed, ignoreCase: true, out object? enumValue))
        {
            value = enumValue;
            return true;
        }

        if (valueType == typeof(Thickness) && TryParseFloats(trimmed, [1, 2, 4], out float[] thickness))
        {
            Thickness parsed = thickness.Length switch
            {
                1 => new Thickness(thickness[0]),
                2 => new Thickness(thickness[0], thickness[1], thickness[0], thickness[1]),
                _ => new Thickness(thickness[0], thickness[1], thickness[2], thickness[3])
            };
            if ((ReferenceEquals(property, Control.BorderThicknessProperty) || ReferenceEquals(property, Control.PaddingProperty)) &&
                (parsed.Left < 0 || parsed.Top < 0 || parsed.Right < 0 || parsed.Bottom < 0))
            {
                return false;
            }

            value = parsed;
            return true;
        }

        if (valueType == typeof(LayoutPoint) && TryParseFloats(trimmed, [2], out float[] point))
        {
            LayoutPoint parsed = new(point[0], point[1]);
            if (ReferenceEquals(property, UIElement.RenderTransformOriginProperty) &&
                (parsed.X < 0 || parsed.X > 1 || parsed.Y < 0 || parsed.Y > 1))
            {
                return false;
            }

            value = parsed;
            return true;
        }

        if (valueType == typeof(Transform))
        {
            if (trimmed.Equals("identity", StringComparison.OrdinalIgnoreCase))
            {
                value = Transform.Identity;
                return true;
            }

            if (TryParseFloats(trimmed, [6], out float[] matrix))
            {
                value = new Transform(new Matrix3x2(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5]));
                return true;
            }
        }

        if (typeof(Brush).IsAssignableFrom(valueType))
        {
            if (trimmed.Length == 0 || trimmed.Equals("none", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                value = null;
                return true;
            }

            if (Color.TryParse(trimmed, out Color color))
            {
                value = new SolidColorBrush(color);
                return true;
            }

            if (trimmed.StartsWith('<'))
            {
                MarkupResult<Brush> result = new BrushMarkupReader().Read(trimmed);
                if (!result.HasErrors && result.Value is not null)
                {
                    value = result.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ValidateFloat(UiProperty property, float value)
    {
        if (ReferenceEquals(property, UIElement.WidthProperty) || ReferenceEquals(property, UIElement.HeightProperty))
        {
            return float.IsNaN(value) || (float.IsFinite(value) && value >= 0);
        }

        if (!float.IsFinite(value))
        {
            return false;
        }

        if (ReferenceEquals(property, UIElement.OpacityProperty))
        {
            return value is >= 0 and <= 1;
        }

        if (ReferenceEquals(property, Control.FontSizeProperty))
        {
            return value > 0;
        }

        return true;
    }

    private static bool TryParseFloats(string text, int[] acceptedCounts, out float[] values)
    {
        string[] parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!acceptedCounts.Contains(parts.Length))
        {
            values = [];
            return false;
        }

        values = new float[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            if (!float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index]) ||
                !float.IsFinite(values[index]))
            {
                values = [];
                return false;
            }
        }

        return true;
    }

    private static string FormatValue(UiProperty property, object? value)
    {
        return value switch
        {
            null => string.Empty,
            float number when float.IsNaN(number) &&
                (ReferenceEquals(property, UIElement.WidthProperty) || ReferenceEquals(property, UIElement.HeightProperty)) => "Auto",
            float number => number.ToString("0.###", CultureInfo.InvariantCulture),
            Thickness thickness when thickness.Left == thickness.Top && thickness.Left == thickness.Right && thickness.Left == thickness.Bottom =>
                thickness.Left.ToString("0.###", CultureInfo.InvariantCulture),
            Thickness thickness => string.Join(',',
                thickness.Left.ToString("0.###", CultureInfo.InvariantCulture),
                thickness.Top.ToString("0.###", CultureInfo.InvariantCulture),
                thickness.Right.ToString("0.###", CultureInfo.InvariantCulture),
                thickness.Bottom.ToString("0.###", CultureInfo.InvariantCulture)),
            LayoutPoint point => $"{point.X.ToString("0.###", CultureInfo.InvariantCulture)},{point.Y.ToString("0.###", CultureInfo.InvariantCulture)}",
            Transform transform when transform == Transform.Identity => "Identity",
            Transform transform => FormatMatrix(transform.Matrix),
            Brush brush => FormatBrush(brush),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string FormatMatrix(Matrix3x2 matrix) => string.Join(',',
        matrix.M11.ToString("0.###", CultureInfo.InvariantCulture),
        matrix.M12.ToString("0.###", CultureInfo.InvariantCulture),
        matrix.M21.ToString("0.###", CultureInfo.InvariantCulture),
        matrix.M22.ToString("0.###", CultureInfo.InvariantCulture),
        matrix.M31.ToString("0.###", CultureInfo.InvariantCulture),
        matrix.M32.ToString("0.###", CultureInfo.InvariantCulture));

    internal static string FormatBrush(Brush? brush) => brush switch
    {
        null => "none",
        SolidColorBrush solid => $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}",
        _ => brush.GetType().Name
    };

    private Brush OwnerBrush(Type ownerType) => ownerType == typeof(TextBlock) || ownerType == typeof(ContentControl)
        ? PinkBrush
        : ownerType == typeof(Control) ? LimeBrush : CyanBrush;

    private static string ToTitleCase(string value) => value.Length == 0
        ? value
        : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

}

internal sealed class AspectStudioTarget
{
    public AspectStudioTarget(
        AspectStudioElementKind kind,
        string name,
        UIElement element,
        IEnumerable<(UiProperty Property, object? Value)> initialValues)
    {
        Kind = kind;
        Name = name;
        Element = element;
        InitialValues = initialValues.ToDictionary(pair => pair.Property, pair => pair.Value);
        Values = new Dictionary<UiProperty, object?>(InitialValues);
    }

    public AspectStudioElementKind Kind { get; }

    public string Name { get; }

    public UIElement Element { get; }

    public Dictionary<UiProperty, object?> InitialValues { get; }

    public Dictionary<UiProperty, object?> Values { get; }

    public HashSet<UiProperty> Modified { get; } = [];

    public object? GetValue(UiProperty property) => Values.TryGetValue(property, out object? value)
        ? value
        : Element.GetValue(property);

    public void Reset()
    {
        Values.Clear();
        foreach ((UiProperty property, object? value) in InitialValues)
        {
            Values.Add(property, value);
        }

        Modified.Clear();
    }
}
