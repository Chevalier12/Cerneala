using System.Collections;
using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Presentation;

public partial class PrismChapterView : UserControl
{
    internal static readonly UiProperty<IEnumerable?> LayerRowsProperty = UiProperty<IEnumerable?>.Register(
        nameof(LayerRows),
        typeof(PrismChapterView),
        new UiPropertyMetadata<IEnumerable?>(null));

    internal static readonly UiProperty<IEnumerable?> CatalogRowsProperty = UiProperty<IEnumerable?>.Register(
        nameof(CatalogRows),
        typeof(PrismChapterView),
        new UiPropertyMetadata<IEnumerable?>(null));

    internal static readonly UiProperty<IEnumerable?> InspectorRowsProperty = UiProperty<IEnumerable?>.Register(
        nameof(InspectorRows),
        typeof(PrismChapterView),
        new UiPropertyMetadata<IEnumerable?>(null));

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

    internal IEnumerable? LayerRows
    {
        get => GetValue(LayerRowsProperty);
        set => SetValue(LayerRowsProperty, value);
    }

    internal IEnumerable? CatalogRows
    {
        get => GetValue(CatalogRowsProperty);
        set => SetValue(CatalogRowsProperty, value);
    }

    internal IEnumerable? InspectorRows
    {
        get => GetValue(InspectorRowsProperty);
        set => SetValue(InspectorRowsProperty, value);
    }

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

        LayerRows = null;
        CatalogRows = null;
        InspectorRows = null;
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

        string search = SearchBox.Text.Trim();
        PrismCatalogOperationInfo[] visible = CurrentCatalog()
            .Where(operation => catalogCategory is null || operation.Category == catalogCategory)
            .Where(operation => search.Length == 0 ||
                operation.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                operation.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(operation => operation.Category, StringComparer.Ordinal)
            .ThenBy(operation => operation.Symbol, StringComparer.Ordinal)
            .ToArray();

        CatalogRows = visible.Select(operation => new PrismStudioCatalogRow(
            operation.RequiresResource
                ? $"{operation.Symbol}  / RESOURCE REQUIRED"
                : $"+ {operation.Symbol}",
            model.Layers.Count > 0 && !operation.RequiresResource,
            () => AddCatalogOperation(operation)))
            .ToArray();

        VisibleCatalogCount = visible.Length;
        int catalogCount = PrismCatalog.Filters.Length + PrismCatalog.Styles.Length;
        CatalogCountText.Text = $"{visible.Length:000} / {catalogCount:000}";
        CategoryButton.Content = catalogCategory?.ToUpperInvariant() ?? "ALL CATEGORIES";
        FilterTab.IsChecked = catalogKind == PrismCatalogOperationKind.Filter;
        StyleTab.IsChecked = catalogKind == PrismCatalogOperationKind.Style;
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

        LayerRows = model.Layers.Select(CreateLayerRow).ToArray();

        LayerCountText.Text = model.Layers.Count.ToString("00", CultureInfo.InvariantCulture);
    }

    private PrismStudioLayerRow CreateLayerRow(PrismStudioLayer layer)
    {
        return new PrismStudioLayerRow(
            layer.Name,
            layer.Id == model.SelectedLayerId && model.SelectedOperationId is null,
            layer.IsVisible,
            layer.Filters.Select(CreateOperationRow).ToArray(),
            layer.Styles.Select(CreateOperationRow).ToArray(),
            () =>
            {
                model.SelectLayer(layer.Id);
                RebuildLayers();
                RebuildInspector();
            },
            () => MoveLayer(layer.Id, -1),
            () => MoveLayer(layer.Id, 1),
            value => CommitLayerValue(instance => model.SetLayerVisibility(instance, layer.Id, value)));
    }

    private PrismStudioOperationRow CreateOperationRow(PrismStudioOperation operation)
    {
        return new PrismStudioOperationRow(
            operation.Catalog.Symbol,
            operation.Id == model.SelectedOperationId,
            operation.IsVisible,
            () =>
            {
                model.SelectOperation(operation.Id);
                RebuildLayers();
                RebuildInspector();
            },
            () => MoveOperation(operation.Id, -1),
            () => MoveOperation(operation.Id, 1),
            value => CommitValue(instance => model.SetOperationVisibility(instance, operation.Id, value)));
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

        if (model.Layers.Count == 0)
        {
            InspectorSelectionText.Text = "NO LAYER";
            InspectorRows = null;
            return;
        }

        PrismStudioLayer layer = model.Layer(model.SelectedLayerId);
        List<object> rows =
        [
            new PrismStudioLayerTitleRow(layer.Name),
            new PrismStudioUnitSliderRow("OPACITY", layer.Opacity, value =>
                CommitLayerValue(instance => model.SetLayerOpacity(instance, layer.Id, value))),
            new PrismStudioUnitSliderRow("FILL", layer.Fill, value =>
                CommitLayerValue(instance => model.SetLayerFill(instance, layer.Id, value))),
            CreateChoiceRow("BLEND", BlendModes(), layer.BlendMode, value =>
                CommitLayerValue(instance => model.SetLayerBlendMode(instance, layer.Id, value)))
        ];
        InspectorSelectionText.Text = model.SelectedOperationId is int operationId
            ? model.Operation(operationId).Catalog.Symbol.ToUpperInvariant()
            : layer.Name;

        if (model.SelectedOperationId is not int selectedOperationId)
        {
            InspectorRows = rows;
            return;
        }

        PrismStudioOperation operation = model.Operation(selectedOperationId);
        rows.Add(new PrismStudioOperationTitleRow(operation.Catalog.Symbol));
        if (operation.Catalog.Kind == PrismCatalogOperationKind.Filter)
        {
            rows.Add(new PrismStudioUnitSliderRow("FILTER OPACITY", operation.Opacity, value =>
                CommitValue(instance => model.SetFilterOpacity(instance, operation.Id, value))));
            rows.Add(CreateChoiceRow("FILTER BLEND", BlendModes(), operation.BlendMode, value =>
                CommitValue(instance => model.SetFilterBlendMode(instance, operation.Id, value))));
        }

        rows.Add(new PrismStudioOperationActionsRow(
            () => MoveOperation(operation.Id, -1),
            () => MoveOperation(operation.Id, 1),
            () =>
            {
                if (model.RemoveOperation(operation.Id))
                {
                    CommitStructure();
                }
            }));

        foreach (PrismCatalogParameterInfo parameter in operation.Catalog.Parameters)
        {
            rows.Add(CreateParameterRow(operation, parameter));
        }

        InspectorRows = rows;
    }

    private object CreateParameterRow(
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter)
    {
        string label = parameter.Unit.Length == 0
            ? parameter.Name
            : $"{parameter.Name} / {parameter.Unit}";
        return parameter.ValueKind switch
        {
            PrismCatalogValueKind.Boolean => new PrismStudioBooleanRow(
                label,
                (bool)operation.GetValue(parameter),
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Number when parameter.Minimum is double minimum && parameter.Maximum is double maximum =>
                new PrismStudioFiniteNumberRow(
                    label,
                    (float)operation.GetValue(parameter),
                    (float)minimum,
                    (float)maximum,
                    value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Number => new PrismStudioStepperRow(
                label,
                Convert.ToDouble(operation.GetValue(parameter), CultureInfo.InvariantCulture),
                integer: false,
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Integer => new PrismStudioStepperRow(
                label,
                (int)operation.GetValue(parameter),
                integer: true,
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Color => new PrismStudioColorRow(
                label,
                (Color)operation.GetValue(parameter),
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Vector => new PrismStudioVectorRow(
                label,
                (Vector4)operation.GetValue(parameter),
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Symbol => CreateChoiceRow(
                label,
                parameter.SymbolOptions,
                (string)operation.GetValue(parameter),
                value => SetParameter(operation, parameter, value)),
            PrismCatalogValueKind.Resource => new PrismStudioResourceRow(label),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter.ValueKind, "Unknown Prism parameter kind.")
        };
    }

    private void SetParameter(
        PrismStudioOperation operation,
        PrismCatalogParameterInfo parameter,
        object value) =>
        CommitValue(instance => model.SetOperationValue(instance, operation.Id, parameter, value));

    private static PrismStudioChoiceRow CreateChoiceRow<T>(
        string label,
        IReadOnlyList<T> values,
        T current,
        Action<T> changed)
    {
        object?[] items = values.Cast<object?>().ToArray();
        int selectedIndex = Array.FindIndex(items, value => EqualityComparer<T>.Default.Equals((T)value!, current));
        return new PrismStudioChoiceRow(label, items, selectedIndex, value => changed((T)value!));
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

    private static PrismBlendMode[] BlendModes() =>
        Enum.GetValues<PrismBlendMode>()
            .Where(value => value != PrismBlendMode.PassThrough)
            .ToArray();

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024d * 1024d):0.0} MB",
        >= 1024 => $"{bytes / 1024d:0.0} KB",
        _ => $"{bytes} B"
    };
}
