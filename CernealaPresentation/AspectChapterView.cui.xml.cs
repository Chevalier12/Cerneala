using System.Globalization;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Text;
using ColumnDefinition = Cerneala.UI.Layout.Panels.ColumnDefinition;
using Grid = Cerneala.UI.Layout.Panels.Grid;
using GridLength = Cerneala.UI.Layout.Panels.GridLength;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Presentation;

internal enum AspectStudioElementKind
{
    Border,
    TextBlock,
    Button
}

public partial class AspectChapterView : UserControl
{
    private static readonly SolidColorBrush PanelBrush = new(new Color(20, 24, 30));
    private static readonly SolidColorBrush SelectedBrush = new(new Color(20, 55, 61));
    private static readonly SolidColorBrush LineBrush = new(new Color(52, 60, 70));
    private static readonly SolidColorBrush PaperBrush = new(new Color(237, 239, 243));
    private static readonly SolidColorBrush MutedBrush = new(new Color(150, 160, 171));
    private static readonly SolidColorBrush CyanBrush = new(new Color(77, 240, 255));
    private static readonly SolidColorBrush PinkBrush = new(new Color(255, 62, 165));
    private static readonly SolidColorBrush LimeBrush = new(new Color(198, 255, 61));
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
        ReleaseDynamicControls();
    }

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
            BuildTargets();
        }

        editorBuilt = true;
        SelectTarget(selectedKind);
    }

    private void BuildTargets()
    {
        Border border = new()
        {
            Child = new TextBlock
            {
                Text = "BORDER",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        AddTarget(
            AspectStudioElementKind.Border,
            "BORDER",
            border,
            (Control.BackgroundProperty, new SolidColorBrush(new Color(27, 34, 42))),
            (Control.BorderBrushProperty, CyanBrush),
            (Control.BorderThicknessProperty, new Thickness(2)),
            (Control.PaddingProperty, new Thickness(24)),
            (Control.FontFamilyProperty, "Cascadia Mono SemiBold"),
            (Control.FontSizeProperty, 12f),
            (Control.ForegroundProperty, PaperBrush),
            (UIElement.WidthProperty, 260f),
            (UIElement.HeightProperty, 180f),
            (UIElement.HorizontalAlignmentProperty, HorizontalAlignment.Center),
            (UIElement.VerticalAlignmentProperty, VerticalAlignment.Center));

        TextBlock textBlock = new();
        AddTarget(
            AspectStudioElementKind.TextBlock,
            "TEXTBLOCK",
            textBlock,
            (TextBlock.TextProperty, "Cerneala, live."),
            (Control.FontFamilyProperty, "Bahnschrift SemiBold"),
            (Control.FontSizeProperty, 30f),
            (Control.ForegroundProperty, new SolidColorBrush(new Color(237, 239, 243))),
            (TextBlock.TextWrappingProperty, TextWrapping.Wrap),
            (UIElement.WidthProperty, 320f),
            (UIElement.HorizontalAlignmentProperty, HorizontalAlignment.Center),
            (UIElement.VerticalAlignmentProperty, VerticalAlignment.Center));

        Button button = new();
        button.ClearValue(UIElement.FocusableProperty);
        button.ClearValue(UIElement.IsTabStopProperty);
        button.ClearValue(UIElement.CursorProperty);
        AddTarget(
            AspectStudioElementKind.Button,
            "BUTTON",
            button,
            ((UiProperty)ContentControl.ContentProperty, (object?)"APLICA ASPECT"),
            (Control.BackgroundProperty, LimeBrush),
            (Control.ForegroundProperty, new SolidColorBrush(new Color(10, 11, 14))),
            (Control.BorderBrushProperty, LimeBrush),
            (Control.BorderThicknessProperty, new Thickness(1)),
            (Control.PaddingProperty, new Thickness(22, 13, 22, 13)),
            (Control.FontFamilyProperty, "Cascadia Mono SemiBold"),
            (Control.FontSizeProperty, 12f),
            (UIElement.HorizontalAlignmentProperty, HorizontalAlignment.Center),
            (UIElement.VerticalAlignmentProperty, VerticalAlignment.Center),
            (UIElement.FocusableProperty, true),
            (UIElement.IsTabStopProperty, true),
            (UIElement.CursorProperty, Cerneala.UI.Input.Cursor.Hand));
    }

    private void AddTarget(
        AspectStudioElementKind kind,
        string name,
        UIElement element,
        params (UiProperty Property, object? Value)[] values)
    {
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

        PreviewStage.Child = null;
        Clear(PropertyHost.Panel);
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
        PreviewStage.Child = target.Element;
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

        Clear(PropertyHost.Panel);
        AspectStudioTarget target = SelectedTarget;
        IReadOnlyList<UiProperty> allProperties = GetInspectableProperties(target.Element.GetType());
        string filter = PropertySearch.Text.Trim();
        UiProperty[] visibleProperties = allProperties
            .Where(property => filter.Length == 0 ||
                property.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                property.OwnerType.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Type? previousOwner = null;
        foreach (UiProperty property in visibleProperties)
        {
            if (property.OwnerType != previousOwner)
            {
                Add(PropertyHost.Panel, Label(property.OwnerType.Name, OwnerBrush(property.OwnerType)));
                previousOwner = property.OwnerType;
            }

            Add(PropertyHost.Panel, CreatePropertyRow(target, property));
        }

        if (visibleProperties.Length == 0)
        {
            Add(PropertyHost.Panel, Label("NO MATCHES", MutedBrush));
        }

        PropertyCountText.Text = filter.Length == 0
            ? $"{allProperties.Count:00} EDITABLE"
            : $"{visibleProperties.Length:00} / {allProperties.Count:00}";
        UpdateStatus(StatusMessage.Text);
    }

    private AspectStudioPropertyRow CreatePropertyRow(AspectStudioTarget target, UiProperty property)
    {
        object? current = target.GetValue(property);
        UIElement editor = CreateEditor(target, property, current);
        Grid row = new();
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(126)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        TextBlock name = new()
        {
            Text = property.Name,
            FontFamily = "Cascadia Mono",
            FontSize = 8,
            Foreground = target.Modified.Contains(property) ? LimeBrush : MutedBrush,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(editor, 1);
        Add(row, name);
        Add(row, editor);
        return new AspectStudioPropertyRow(property, editor)
        {
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 7, 0, 7),
            Child = row
        };
    }

    private UIElement CreateEditor(AspectStudioTarget target, UiProperty property, object? current)
    {
        Type valueType = property.ValueType;
        if (valueType == typeof(bool))
        {
            CheckBox checkBox = new()
            {
                IsChecked = current is true,
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            checkBox.Checked += (_, _) => CommitProperty(target, property, true);
            checkBox.Unchecked += (_, _) => CommitProperty(target, property, false);
            return checkBox;
        }

        if (valueType.IsEnum)
        {
            Array values = Enum.GetValues(valueType);
            ComboBox comboBox = CreateComboBox(values, current);
            comboBox.SelectionChanged += (_, _) =>
            {
                if (comboBox.SelectedItem is object value)
                {
                    CommitProperty(target, property, value);
                }
            };
            return comboBox;
        }

        if (Nullable.GetUnderlyingType(valueType) == typeof(Cursor))
        {
            string[] values = ["DEFAULT", "ARROW", "HAND", "IBEAM", "CROSSHAIR"];
            string selected = current is Cursor cursor ? cursor.Name.ToUpperInvariant() : "DEFAULT";
            ComboBox comboBox = CreateComboBox(values, selected);
            comboBox.SelectionChanged += (_, _) =>
            {
                object? value = comboBox.SelectedItem is string name && name != "DEFAULT"
                    ? new Cursor(name.Equals("IBEAM", StringComparison.Ordinal) ? "IBeam" : ToTitleCase(name))
                    : null;
                CommitProperty(target, property, value);
            };
            return comboBox;
        }

        if (typeof(Brush).IsAssignableFrom(valueType))
        {
            return CreateBrushEditor(target, property, current as Brush);
        }

        TextBox input = CreateInput(FormatValue(property, current));
        input.TextChanged += (_, _) => CommitTextInput(target, property, input);
        return input;
    }

    private UIElement CreateBrushEditor(AspectStudioTarget target, UiProperty property, Brush? current)
    {
        Grid editor = new();
        editor.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(26)));
        editor.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));
        Border swatch = new()
        {
            Width = 20,
            Height = 20,
            Background = current,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox input = CreateInput(FormatBrush(current));
        input.Margin = Thickness.Zero;
        input.TextChanged += (_, _) =>
        {
            if (TryParseValue(property, input.Text, out object? value, out string error))
            {
                input.BorderBrush = LineBrush;
                swatch.Background = value as Brush;
                CommitProperty(target, property, value);
            }
            else
            {
                input.BorderBrush = PinkBrush;
                UpdateStatus(error);
            }
        };
        Grid.SetColumn(input, 1);
        Add(editor, swatch);
        Add(editor, input);
        return editor;
    }

    private void CommitTextInput(AspectStudioTarget target, UiProperty property, TextBox input)
    {
        if (TryParseValue(property, input.Text, out object? value, out string error))
        {
            input.BorderBrush = LineBrush;
            CommitProperty(target, property, value);
        }
        else
        {
            input.BorderBrush = PinkBrush;
            UpdateStatus(error);
        }
    }

    private void CommitProperty(AspectStudioTarget target, UiProperty property, object? value)
    {
        target.Values[property] = value;
        target.Modified.Add(property);
        ApplyTargetAspect(target);
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

    private static string FormatBrush(Brush? brush) => brush switch
    {
        null => "none",
        SolidColorBrush solid => $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}",
        _ => brush.GetType().Name
    };

    private ComboBox CreateComboBox(System.Collections.IEnumerable values, object? current)
    {
        ComboBox comboBox = new()
        {
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            Foreground = PaperBrush,
            FontFamily = "Cascadia Mono SemiBold",
            FontSize = 8,
            Padding = new Thickness(7, 5, 7, 5),
            MaxDropDownHeight = 180
        };
        comboBox.ApplyTemplate();
        Overlay overlay = (Overlay)comboBox.ComponentTemplateInstance!.Parts["PART_DropDownOverlay"];
        Border dropDownBorder = (Border)overlay.Content!;
        ScrollViewer dropDownScrollViewer = (ScrollViewer)dropDownBorder.Child!;
        dropDownScrollViewer.ComponentTemplate = PropertyHost.ComponentTemplate;
        ToggleButton toggle = (ToggleButton)comboBox.ComponentTemplateInstance.Parts["PART_DropDownToggle"];
        toggle.Background = PanelBrush;
        toggle.BorderBrush = LineBrush;
        toggle.BorderThickness = new Thickness(1);
        toggle.Foreground = PaperBrush;
        toggle.FontFamily = "Cascadia Mono SemiBold";
        toggle.Padding = new Thickness(6, 4, 6, 4);
        if (toggle.Content is Cerneala.UI.Controls.Shapes.Shape glyph)
        {
            glyph.Fill = PaperBrush;
        }

        object?[] items = values.Cast<object?>().ToArray();
        comboBox.SetItems(items);
        comboBox.SelectedIndex = Array.FindIndex(items, value => Equals(value, current));
        return comboBox;
    }

    private static TextBlock Label(string text, Brush brush) => new()
    {
        Text = text.ToUpperInvariant(),
        FontFamily = "Cascadia Mono SemiBold",
        FontSize = 8,
        Foreground = brush,
        Margin = new Thickness(0, 8, 0, 4)
    };

    private static TextBox CreateInput(string text) => new()
    {
        Text = text,
        Background = PanelBrush,
        BorderBrush = LineBrush,
        BorderThickness = new Thickness(1),
        Foreground = PaperBrush,
        CaretBrush = CyanBrush,
        FontFamily = "Cascadia Mono",
        FontSize = 8,
        Padding = new Thickness(6, 5, 6, 5)
    };

    private static Brush OwnerBrush(Type ownerType) => ownerType == typeof(TextBlock) || ownerType == typeof(ContentControl)
        ? PinkBrush
        : ownerType == typeof(Control) ? LimeBrush : CyanBrush;

    private static string ToTitleCase(string value) => value.Length == 0
        ? value
        : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private static void Add(StackPanel parent, UIElement child)
    {
        parent.LogicalChildren.Add(child);
        parent.VisualChildren.Add(child);
    }

    private static void Add(Grid parent, UIElement child)
    {
        parent.LogicalChildren.Add(child);
        parent.VisualChildren.Add(child);
    }

    private static void Clear(StackPanel parent)
    {
        while (parent.VisualChildren.Count > 0)
        {
            parent.VisualChildren.Remove(parent.VisualChildren[0]);
        }

        while (parent.LogicalChildren.Count > 0)
        {
            parent.LogicalChildren.Remove(parent.LogicalChildren[0]);
        }
    }
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

internal sealed class AspectStudioPropertyRow : Border
{
    public AspectStudioPropertyRow(UiProperty property, UIElement editor)
    {
        Property = property;
        Editor = editor;
    }

    public UiProperty Property { get; }

    public UIElement Editor { get; }
}

internal sealed class AspectStudioScrollHost : ScrollViewer
{
    public AspectStudioScrollHost()
    {
        Panel = new StackPanel { Margin = new Thickness(9) };
        Content = Panel;
    }

    public StackPanel Panel { get; }
}
