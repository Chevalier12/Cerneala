using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Text;
using Grid = Cerneala.UI.Layout.Panels.Grid;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Presentation;

public partial class PrismChapterView : UserControl
{
    private static readonly SolidColorBrush PanelBrush = new(new Color(20, 24, 30));
    private static readonly SolidColorBrush SelectedBrush = new(new Color(20, 55, 61));
    private static readonly SolidColorBrush LineBrush = new(new Color(52, 60, 70));
    private static readonly SolidColorBrush PaperBrush = new(new Color(232, 235, 232));
    private static readonly SolidColorBrush MutedBrush = new(new Color(150, 160, 171));
    private static readonly SolidColorBrush CyanBrush = new(new Color(77, 240, 255));
    private static readonly SolidColorBrush PinkBrush = new(new Color(255, 62, 165));
    private static readonly SolidColorBrush LimeBrush = new(new Color(198, 255, 61));
    private static readonly Color[] Palette =
    [
        new Color(77, 240, 255),
        new Color(255, 62, 165),
        new Color(198, 255, 61),
        new Color(255, 138, 61),
        Color.White,
        Color.Black
    ];

    private readonly PrismStudioModel model = new();
    private PrismInstance? prismInstance;
    private IDisposable? prismLifetime;
    private PrismCatalogOperationKind catalogKind = PrismCatalogOperationKind.Filter;
    private string? catalogCategory;
    private bool editorBuilt;
    private bool active;

    internal PrismStudioModel Model => model;

    internal int VisibleCatalogCount { get; private set; }

    internal bool HasPrismAttachment => prismLifetime is not null;

    internal IReadOnlySet<PrismCatalogValueKind> SupportedEditorKinds { get; } =
        Enum.GetValues<PrismCatalogValueKind>().ToHashSet();

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
        AttachSelectedTarget();
    }

    internal void Deactivate()
    {
        active = false;
        DetachPrism();
        prismInstance = null;
        ReleaseDynamicControls();
    }

    internal void PrepareEditorForTests() => EnsureEditorBuilt();

    internal void SelectTargetForTests(PrismStudioTarget target) => SelectTarget(target);

    internal void AddLayerForTests()
    {
        model.AddLayer();
        CommitStructure();
    }

    internal void UpdateDiagnostics(PrismOperationalDiagnostics? diagnostics)
    {
        UpdateModelStatus();
        if (diagnostics is not PrismOperationalDiagnostics value)
        {
            StatusPasses.Text = "PASSES 00";
            StatusSurfaces.Text = "SURFACES 00 / 0 B";
            StatusFallback.Text = "FALLBACK NONE";
            return;
        }

        StatusPasses.Text = $"PASSES {value.ExecutedPassCount:00}";
        StatusSurfaces.Text = $"SURFACES {value.ActiveSurfaceCount:00} / {FormatBytes(value.SurfaceByteCount)}";
        StatusFallback.Text = value.LastFallback is null
            ? $"FALLBACK {value.FallbackCount:00} / NONE"
            : $"FALLBACK {value.FallbackCount:00} / {value.LastFallback.Value.Reason}";
    }

    private void EnsureEditorBuilt()
    {
        if (editorBuilt)
        {
            return;
        }

        editorBuilt = true;
        UpdateTargetVisibility();
        RebuildLayers();
        RebuildCatalog();
        RebuildInspector();
        UpdateModelStatus();
    }

    private void ReleaseDynamicControls()
    {
        if (!editorBuilt)
        {
            return;
        }

        Clear(LayersHost.Panel);
        Clear(CatalogHost.Panel);
        Clear(InspectorHost.Panel);
        editorBuilt = false;
        VisibleCatalogCount = 0;
    }

    private void AttachSelectedTarget()
    {
        if (!active || prismLifetime is not null || model.OperationCount == 0)
        {
            return;
        }

        prismInstance ??= new PrismInstance(model.BuildDefinition());
        model.ApplyTo(prismInstance);
        UIElement target = SelectedTargetElement();
        prismLifetime = GeneratedMarkup.AttachPrism(target, () => prismInstance);
        target.Invalidate(InvalidationFlags.Render, "Prism Studio target attached");
    }

    private void DetachPrism()
    {
        prismLifetime?.Dispose();
        prismLifetime = null;
    }

    private void CommitStructure()
    {
        if (model.OperationCount == 0)
        {
            DetachPrism();
            prismInstance = null;
        }
        else
        {
            prismInstance ??= new PrismInstance(model.BuildDefinition());
            model.ApplyTo(prismInstance);
            AttachSelectedTarget();
        }

        SelectedTargetElement().Invalidate(InvalidationFlags.Render, "Prism Studio structure changed");
        RebuildLayers();
        RebuildCatalog();
        RebuildInspector();
        UpdateModelStatus();
    }

    private void CommitValue(Action<PrismInstance> update)
    {
        prismInstance ??= new PrismInstance(model.BuildDefinition());
        update(prismInstance);
        SelectedTargetElement().Invalidate(InvalidationFlags.Render, "Prism Studio value changed");
        UpdateModelStatus();
    }

    private void CommitLayerValue(Action<PrismInstance?> update)
    {
        if (model.OperationCount > 0)
        {
            prismInstance ??= new PrismInstance(model.BuildDefinition());
        }

        update(prismInstance);
        SelectedTargetElement().Invalidate(InvalidationFlags.Render, "Prism Studio layer value changed");
        UpdateModelStatus();
    }

    private void OnReset(UiElementId sender, RoutedEventArgs args)
    {
        model.Reset();
        DetachPrism();
        prismInstance = null;
        UpdateTargetVisibility();
        CommitStructure();
    }

    private void OnAddLayer(UiElementId sender, RoutedEventArgs args)
    {
        model.AddLayer();
        CommitStructure();
    }

    private void OnDeleteLayer(UiElementId sender, RoutedEventArgs args)
    {
        if (model.RemoveLayer(model.SelectedLayerId))
        {
            CommitStructure();
        }
    }

    private void OnTargetMascot(UiElementId sender, RoutedEventArgs args) => SelectTarget(PrismStudioTarget.Mascot);

    private void OnTargetTypography(UiElementId sender, RoutedEventArgs args) => SelectTarget(PrismStudioTarget.Typography);

    private void OnTargetBadge(UiElementId sender, RoutedEventArgs args) => SelectTarget(PrismStudioTarget.Badge);

    private void OnTargetCard(UiElementId sender, RoutedEventArgs args) => SelectTarget(PrismStudioTarget.Card);

    private void SelectTarget(PrismStudioTarget target)
    {
        if (model.Target == target)
        {
            return;
        }

        DetachPrism();
        model.SelectTarget(target);
        UpdateTargetVisibility();
        AttachSelectedTarget();
    }

    private void OnFiltersTab(UiElementId sender, RoutedEventArgs args)
    {
        catalogKind = PrismCatalogOperationKind.Filter;
        catalogCategory = null;
        RebuildCatalog();
    }

    private void OnStylesTab(UiElementId sender, RoutedEventArgs args)
    {
        catalogKind = PrismCatalogOperationKind.Style;
        catalogCategory = null;
        RebuildCatalog();
    }

    private void OnCategory(UiElementId sender, RoutedEventArgs args)
    {
        string[] categories = CurrentCatalog()
            .Select(operation => operation.Category)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        int current = catalogCategory is null ? -1 : Array.IndexOf(categories, catalogCategory);
        catalogCategory = current + 1 < categories.Length ? categories[current + 1] : null;
        RebuildCatalog();
    }

    private void OnCatalogSearchChanged(object? sender, TextChangedEventArgs args) => RebuildCatalog();

    private void RebuildCatalog()
    {
        if (!editorBuilt)
        {
            return;
        }

        Clear(CatalogHost.Panel);
        string search = SearchBox.Text.Trim();
        PrismCatalogOperationInfo[] visible = CurrentCatalog()
            .Where(operation => catalogCategory is null || operation.Category == catalogCategory)
            .Where(operation => search.Length == 0 ||
                operation.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                operation.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(operation => operation.Category, StringComparer.Ordinal)
            .ThenBy(operation => operation.Symbol, StringComparer.Ordinal)
            .ToArray();

        foreach (PrismCatalogOperationInfo operation in visible)
        {
            Button button = CreateButton(
                operation.RequiresResource
                    ? $"{operation.Symbol}  / RESOURCE REQUIRED"
                    : $"+ {operation.Symbol}",
                () => AddCatalogOperation(operation));
            button.IsEnabled = model.Layers.Count > 0 && !operation.RequiresResource;
            button.Margin = new Thickness(0, 0, 0, 4);
            button.Foreground = operation.RequiresResource ? MutedBrush : PaperBrush;
            Add(CatalogHost.Panel, button);
        }

        VisibleCatalogCount = visible.Length;
        CatalogCountText.Text = $"{visible.Length:000} / 144";
        CategoryButton.Content = catalogCategory?.ToUpperInvariant() ?? "ALL CATEGORIES";
        FilterTab.Background = catalogKind == PrismCatalogOperationKind.Filter ? SelectedBrush : PanelBrush;
        StyleTab.Background = catalogKind == PrismCatalogOperationKind.Style ? SelectedBrush : PanelBrush;
    }

    private void AddCatalogOperation(PrismCatalogOperationInfo operation)
    {
        if (model.Layers.Count == 0)
        {
            return;
        }

        if (model.AddOperation(model.SelectedLayerId, operation))
        {
            CommitStructure();
        }
    }

    private IEnumerable<PrismCatalogOperationInfo> CurrentCatalog() =>
        catalogKind == PrismCatalogOperationKind.Filter ? PrismCatalog.Filters : PrismCatalog.Styles;

    private void RebuildLayers()
    {
        if (!editorBuilt)
        {
            return;
        }

        Clear(LayersHost.Panel);
        foreach (PrismStudioLayer layer in model.Layers)
        {
            StackPanel layerBody = new();
            Border layerBlock = new()
            {
                Background = layer.Id == model.SelectedLayerId ? SelectedBrush : PanelBrush,
                Margin = new Thickness(0, 0, 0, 7),
                Padding = new Thickness(7),
                Child = layerBody
            };
            StackPanel heading = Horizontal();
            CheckBox visibility = CreateCheckBox(layer.IsVisible, value =>
                CommitLayerValue(instance => model.SetLayerVisibility(instance, layer.Id, value)));
            Button select = CreateButton(layer.Name, () =>
            {
                model.SelectLayer(layer.Id);
                RebuildLayers();
                RebuildInspector();
            });
            select.Width = 100;
            select.FontSize = 8;
            Button up = CreateButton("^", () => MoveLayer(layer.Id, -1), compact: true);
            Button down = CreateButton("v", () => MoveLayer(layer.Id, 1), compact: true);
            up.Padding = new Thickness(4);
            down.Padding = new Thickness(4);
            Add(heading, visibility);
            Add(heading, select);
            Add(heading, up);
            Add(heading, down);
            Add(layerBody, heading);
            AddOperationSection(layerBody, layer, PrismCatalogOperationKind.Filter);
            AddOperationSection(layerBody, layer, PrismCatalogOperationKind.Style);
            Add(LayersHost.Panel, layerBlock);
        }

        LayerCountText.Text = model.Layers.Count.ToString("00", CultureInfo.InvariantCulture);
    }

    private void AddOperationSection(
        StackPanel parent,
        PrismStudioLayer layer,
        PrismCatalogOperationKind kind)
    {
        IReadOnlyList<PrismStudioOperation> operations = kind == PrismCatalogOperationKind.Filter
            ? layer.Filters
            : layer.Styles;
        Add(parent, Label(kind == PrismCatalogOperationKind.Filter ? "FILTERS" : "STYLES", MutedBrush));
        foreach (PrismStudioOperation operation in operations)
        {
            StackPanel row = Horizontal();
            CheckBox visibility = CreateCheckBox(operation.IsVisible, value =>
                CommitValue(instance => model.SetOperationVisibility(instance, operation.Id, value)));
            Button select = CreateButton(operation.Catalog.Symbol, () =>
            {
                model.SelectOperation(operation.Id);
                RebuildLayers();
                RebuildInspector();
            });
            select.Width = 100;
            select.FontSize = 8;
            Button up = CreateButton("^", () => MoveOperation(operation.Id, -1), compact: true);
            Button down = CreateButton("v", () => MoveOperation(operation.Id, 1), compact: true);
            up.Padding = new Thickness(4);
            down.Padding = new Thickness(4);
            Add(row, visibility);
            Add(row, select);
            Add(row, up);
            Add(row, down);
            Add(parent, row);
        }
    }

    private void MoveLayer(int layerId, int offset)
    {
        if (model.MoveLayer(layerId, offset))
        {
            CommitStructure();
        }
    }

    private void MoveOperation(int operationId, int offset)
    {
        if (model.MoveOperation(operationId, offset))
        {
            CommitStructure();
        }
    }

    private void RebuildInspector()
    {
        if (!editorBuilt)
        {
            return;
        }

        Clear(InspectorHost.Panel);
        if (model.Layers.Count == 0)
        {
            InspectorSelectionText.Text = "NO LAYER";
            return;
        }

        PrismStudioLayer layer = model.Layer(model.SelectedLayerId);
        InspectorSelectionText.Text = model.SelectedOperationId is int operationId
            ? model.Operation(operationId).Catalog.Symbol.ToUpperInvariant()
            : layer.Name;
        Add(InspectorHost.Panel, Label(layer.Name, LimeBrush));
        AddUnitSlider("OPACITY", layer.Opacity, value =>
            CommitLayerValue(instance => model.SetLayerOpacity(instance, layer.Id, value)));
        AddUnitSlider("FILL", layer.Fill, value =>
            CommitLayerValue(instance => model.SetLayerFill(instance, layer.Id, value)));
        Add(InspectorHost.Panel, CreateCycleButton(
            $"BLEND / {layer.BlendMode}",
            BlendModes(),
            layer.BlendMode,
            value => CommitLayerValue(instance => model.SetLayerBlendMode(instance, layer.Id, value))));

        if (model.SelectedOperationId is not int selectedOperationId)
        {
            return;
        }

        PrismStudioOperation operation = model.Operation(selectedOperationId);
        Add(InspectorHost.Panel, Label(operation.Catalog.Symbol, PinkBrush));
        if (operation.Catalog.Kind == PrismCatalogOperationKind.Filter)
        {
            AddUnitSlider("FILTER OPACITY", operation.Opacity, value =>
                CommitValue(instance => model.SetFilterOpacity(instance, operation.Id, value)));
            Add(InspectorHost.Panel, CreateCycleButton(
                $"FILTER BLEND / {operation.BlendMode}",
                BlendModes(),
                operation.BlendMode,
                value => CommitValue(instance => model.SetFilterBlendMode(instance, operation.Id, value))));
        }

        StackPanel actions = Horizontal();
        Add(actions, CreateButton("^", () => MoveOperation(operation.Id, -1), compact: true));
        Add(actions, CreateButton("v", () => MoveOperation(operation.Id, 1), compact: true));
        Add(actions, CreateButton("DELETE OP", () =>
        {
            if (model.RemoveOperation(operation.Id))
            {
                CommitStructure();
            }
        }));
        Add(InspectorHost.Panel, actions);

        foreach (PrismCatalogParameterInfo parameter in operation.Catalog.Parameters)
        {
            Add(InspectorHost.Panel, CreateParameterEditor(operation, parameter));
        }
    }

    private UIElement CreateParameterEditor(
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter)
    {
        StackPanel editor = new()
        {
            Margin = new Thickness(0, 8, 0, 0)
        };
        Add(editor, Label(
            parameter.Unit.Length == 0 ? parameter.Name : $"{parameter.Name} / {parameter.Unit}",
            MutedBrush));
        switch (parameter.ValueKind)
        {
            case PrismCatalogValueKind.Boolean:
                Add(editor, CreateCheckBox((bool)operation.GetValue(parameter), value =>
                    SetParameter(operation, parameter, value)));
                break;
            case PrismCatalogValueKind.Number when parameter.Minimum is double minimum && parameter.Maximum is double maximum:
                AddFiniteNumberEditor(editor, operation, parameter, (float)minimum, (float)maximum);
                break;
            case PrismCatalogValueKind.Number:
                AddStepper(editor, operation, parameter, Convert.ToDouble(operation.GetValue(parameter), CultureInfo.InvariantCulture), false);
                break;
            case PrismCatalogValueKind.Integer:
                AddStepper(editor, operation, parameter, (int)operation.GetValue(parameter), true);
                break;
            case PrismCatalogValueKind.Color:
                AddColorEditor(editor, operation, parameter, (Color)operation.GetValue(parameter));
                break;
            case PrismCatalogValueKind.Vector:
                AddVectorEditor(editor, operation, parameter, (Vector4)operation.GetValue(parameter));
                break;
            case PrismCatalogValueKind.Symbol:
                string symbol = (string)operation.GetValue(parameter);
                Add(editor, CreateCycleButton(symbol, parameter.SymbolOptions, symbol,
                    value => SetParameter(operation, parameter, value)));
                break;
            case PrismCatalogValueKind.Resource:
                Add(editor, new TextBlock
                {
                    Text = "RESOURCE REQUIRED / IMPORT DISABLED",
                    FontFamily = "Cascadia Mono",
                    FontSize = 9,
                    Foreground = PinkBrush,
                    Margin = new Thickness(0, 5, 0, 0)
                });
                break;
        }
        return editor;
    }

    private void AddFiniteNumberEditor(
        StackPanel editor,
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        float minimum,
        float maximum)
    {
        float current = (float)operation.GetValue(parameter);
        PrismStudioSlider sliderView = new();
        Slider slider = sliderView.ValueControl;
        slider.Minimum = minimum;
        slider.Maximum = maximum;
        slider.Value = current;
        TextBox input = CreateInput(current.ToString("0.###", CultureInfo.InvariantCulture));
        slider.ValueChanged += (_, args) =>
        {
            input.Text = args.NewValue.ToString("0.###", CultureInfo.InvariantCulture);
            SetParameter(operation, parameter, args.NewValue);
        };
        input.TextChanged += (_, _) =>
        {
            if (float.TryParse(input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
                value >= minimum && value <= maximum)
            {
                slider.Value = value;
                SetParameter(operation, parameter, value);
            }
        };
        Add(editor, sliderView);
        Add(editor, input);
    }

    private void AddStepper(
        StackPanel editor,
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        double current,
        bool integer)
    {
        StackPanel row = Horizontal();
        TextBox input = CreateInput(current.ToString("0.###", CultureInfo.InvariantCulture));
        Action<double> commit = value =>
        {
            object typed = integer ? checked((int)Math.Round(value)) : (float)value;
            try
            {
                SetParameter(operation, parameter, typed);
                input.Text = Convert.ToString(typed, CultureInfo.InvariantCulture) ?? "0";
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        };
        Add(row, CreateButton("-", () => commit(ReadNumber(input.Text, current) - 1), compact: true));
        input.Width = 98;
        input.TextChanged += (_, _) =>
        {
            if (double.TryParse(input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                try
                {
                    SetParameter(operation, parameter, integer ? checked((int)Math.Round(value)) : (float)value);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        };
        Add(row, input);
        Add(row, CreateButton("+", () => commit(ReadNumber(input.Text, current) + 1), compact: true));
        Add(editor, row);
    }

    private void AddColorEditor(
        StackPanel editor,
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        Color current)
    {
        StackPanel row = Horizontal();
        Border swatch = new()
        {
            Width = 24,
            Height = 24,
            Background = new SolidColorBrush(current),
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 5, 0)
        };
        TextBox input = CreateInput(FormatColor(current));
        input.Width = 112;
        input.TextChanged += (_, _) =>
        {
            if (Color.TryParse(input.Text, out Color value))
            {
                swatch.Background = new SolidColorBrush(value);
                SetParameter(operation, parameter, value);
            }
        };
        Add(row, swatch);
        Add(row, input);
        Add(editor, row);
        StackPanel palette = Horizontal();
        foreach (Color color in Palette)
        {
            Button chip = CreateButton(" ", () =>
            {
                input.Text = FormatColor(color);
                swatch.Background = new SolidColorBrush(color);
                SetParameter(operation, parameter, color);
            }, compact: true);
            chip.Background = new SolidColorBrush(color);
            Add(palette, chip);
        }
        Add(editor, palette);
    }

    private void AddVectorEditor(
        StackPanel editor,
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        Vector4 current)
    {
        StackPanel row = Horizontal();
        float[] values = [current.X, current.Y, current.Z, current.W];
        TextBox[] inputs = new TextBox[4];
        for (int index = 0; index < inputs.Length; index++)
        {
            int component = index;
            TextBox input = CreateInput(values[index].ToString("0.###", CultureInfo.InvariantCulture));
            input.Width = 54;
            input.Margin = new Thickness(index == 0 ? 0 : 3, 0, 0, 0);
            input.TextChanged += (_, _) =>
            {
                if (!float.TryParse(input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    return;
                }
                values[component] = value;
                SetParameter(operation, parameter, new Vector4(values[0], values[1], values[2], values[3]));
            };
            inputs[index] = input;
            Add(row, input);
        }
        Add(editor, row);
    }

    private void SetParameter(
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        object value) =>
        CommitValue(instance => model.SetOperationValue(instance, operation.Id, parameter, value));

    private void AddUnitSlider(string label, float current, Action<float> changed)
    {
        Add(InspectorHost.Panel, Label(label, MutedBrush));
        PrismStudioSlider sliderView = new()
        {
            Margin = new Thickness(0, 3, 0, 7)
        };
        Slider slider = sliderView.ValueControl;
        slider.Minimum = 0;
        slider.Maximum = 1;
        slider.Value = current;
        slider.ValueChanged += (_, args) => changed(args.NewValue);
        Add(InspectorHost.Panel, sliderView);
    }

    private void UpdateTargetVisibility()
    {
        PreviewMascot.Visibility = model.Target == PrismStudioTarget.Mascot ? Visibility.Visible : Visibility.Collapsed;
        PreviewTypography.Visibility = model.Target == PrismStudioTarget.Typography ? Visibility.Visible : Visibility.Collapsed;
        PreviewBadge.Visibility = model.Target == PrismStudioTarget.Badge ? Visibility.Visible : Visibility.Collapsed;
        PreviewCard.Visibility = model.Target == PrismStudioTarget.Card ? Visibility.Visible : Visibility.Collapsed;
    }

    private UIElement SelectedTargetElement() => model.Target switch
    {
        PrismStudioTarget.Mascot => PreviewMascotImage,
        PrismStudioTarget.Typography => PreviewTypography,
        PrismStudioTarget.Badge => PreviewBadge,
        PrismStudioTarget.Card => PreviewCard,
        _ => throw new InvalidOperationException("Unknown Prism Studio target.")
    };

    private void UpdateModelStatus()
    {
        StatusLayers.Text = $"LAYERS {model.Layers.Count:00}";
        StatusOperations.Text = $"OPS {model.OperationCount:00}";
    }

    private static TextBlock Label(string text, Brush brush) => new()
    {
        Text = text.ToUpperInvariant(),
        FontFamily = "Cascadia Mono SemiBold",
        FontSize = 8,
        Foreground = brush,
        Margin = new Thickness(0, 6, 0, 4),
        TextWrapping = TextWrapping.Wrap
    };

    private static Button CreateButton(string text, Action clicked, bool compact = false)
    {
        Button button = new()
        {
            Content = text,
            Background = PanelBrush,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(1),
            Foreground = PaperBrush,
            FontFamily = "Cascadia Mono SemiBold",
            FontSize = compact ? 8 : 9,
            Padding = compact ? new Thickness(6, 4, 6, 4) : new Thickness(7, 6, 7, 6),
            Margin = new Thickness(0, 0, 3, 3)
        };
        button.Click += (_, _) => clicked();
        return button;
    }

    private static CheckBox CreateCheckBox(bool value, Action<bool> changed)
    {
        CheckBox checkBox = new()
        {
            IsChecked = value,
            Width = 22,
            Height = 22,
            Margin = new Thickness(0, 0, 4, 3)
        };
        checkBox.Checked += (_, _) => changed(true);
        checkBox.Unchecked += (_, _) => changed(false);
        return checkBox;
    }

    private static TextBox CreateInput(string text) => new()
    {
        Text = text,
        Background = PanelBrush,
        BorderBrush = LineBrush,
        BorderThickness = new Thickness(1),
        Foreground = PaperBrush,
        FontFamily = "Cascadia Mono",
        FontSize = 9,
        Padding = new Thickness(6, 5, 6, 5),
        Margin = new Thickness(0, 3, 0, 3)
    };

    private static Button CreateCycleButton<T>(
        string text,
        IReadOnlyList<T> values,
        T current,
        Action<T> changed)
    {
        int index = Enumerable.Range(0, values.Count)
            .FirstOrDefault(candidate => EqualityComparer<T>.Default.Equals(values[candidate], current));
        Button? button = null;
        button = CreateButton(text, () =>
        {
            index = (index + 1) % values.Count;
            T value = values[index];
            button!.Content = value?.ToString() ?? string.Empty;
            changed(value);
        });
        return button;
    }

    private static PrismBlendMode[] BlendModes() =>
        Enum.GetValues<PrismBlendMode>()
            .Where(value => value != PrismBlendMode.PassThrough)
            .ToArray();

    private static StackPanel Horizontal() => new()
    {
        Orientation = Orientation.Horizontal
    };

    private static void Add(StackPanel parent, UIElement child)
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

    private static string FormatColor(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024d * 1024d):0.0} MB",
        >= 1024 => $"{bytes / 1024d:0.0} KB",
        _ => $"{bytes} B"
    };

    private static double ReadNumber(string text, double fallback) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : fallback;
}

internal sealed class PrismStudioScrollHost : ScrollViewer
{
    public PrismStudioScrollHost()
    {
        Panel = new StackPanel
        {
            Margin = new Thickness(8)
        };
        Content = Panel;
    }

    public StackPanel Panel { get; }
}
