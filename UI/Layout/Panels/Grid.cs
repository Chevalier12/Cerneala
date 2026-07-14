using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Layout.Panels;

public class Grid : Panel
{
    private static readonly ConditionalWeakTable<UIElement, GridPlacement> placements = new();
    private float[] measuredColumnSizes = [0];
    private float[] measuredRowSizes = [0];

    public Grid()
    {
        ColumnDefinitions = new GridDefinitionCollection<ColumnDefinition>(
            this,
            static (definition, owner) => definition.Attach(owner),
            static (definition, owner) => definition.Detach(owner));
        RowDefinitions = new GridDefinitionCollection<RowDefinition>(
            this,
            static (definition, owner) => definition.Attach(owner),
            static (definition, owner) => definition.Detach(owner));
    }

    public GridDefinitionCollection<ColumnDefinition> ColumnDefinitions { get; }

    public GridDefinitionCollection<RowDefinition> RowDefinitions { get; }

    public static int GetColumn(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out GridPlacement? placement) ? placement.Column : 0;
    }

    public static void SetColumn(UIElement element, int column)
    {
        SetPlacement(element, placement => placement.Column = ValidateIndex(column, nameof(column)));
    }

    public static int GetRow(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out GridPlacement? placement) ? placement.Row : 0;
    }

    public static void SetRow(UIElement element, int row)
    {
        SetPlacement(element, placement => placement.Row = ValidateIndex(row, nameof(row)));
    }

    public static int GetColumnSpan(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out GridPlacement? placement) ? placement.ColumnSpan : 1;
    }

    public static void SetColumnSpan(UIElement element, int columnSpan)
    {
        SetPlacement(element, placement => placement.ColumnSpan = ValidateSpan(columnSpan, nameof(columnSpan)));
    }

    public static int GetRowSpan(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out GridPlacement? placement) ? placement.RowSpan : 1;
    }

    public static void SetRowSpan(UIElement element, int rowSpan)
    {
        SetPlacement(element, placement => placement.RowSpan = ValidateSpan(rowSpan, nameof(rowSpan)));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        GridLength[] columns = GetColumnLengths();
        GridLength[] rows = GetRowLengths();
        float[] columnSizes = ResolveFixedSizes(columns);
        float[] rowSizes = ResolveFixedSizes(rows);

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            child.Measure(new MeasureContext(context.AvailableSize, context.Rounding));
            GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
            if (placement.ColumnSpan == 1 &&
                (columns[placement.Column].IsAuto ||
                 (columns[placement.Column].IsStar && float.IsPositiveInfinity(context.AvailableSize.Width))))
            {
                columnSizes[placement.Column] = MathF.Max(columnSizes[placement.Column], child.DesiredSize.Width);
            }

            if (placement.RowSpan == 1 &&
                (rows[placement.Row].IsAuto ||
                 (rows[placement.Row].IsStar && float.IsPositiveInfinity(context.AvailableSize.Height))))
            {
                rowSizes[placement.Row] = MathF.Max(rowSizes[placement.Row], child.DesiredSize.Height);
            }
        }

        ResolveStarSizes(columns, columnSizes, context.AvailableSize.Width);
        ResolveStarSizes(rows, rowSizes, context.AvailableSize.Height);

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
            child.Measure(new MeasureContext(
                new LayoutSize(
                    Sum(columnSizes, placement.Column, placement.ColumnSpan),
                    Sum(rowSizes, placement.Row, placement.RowSpan)),
                context.Rounding));
        }

        measuredColumnSizes = columnSizes;
        measuredRowSizes = rowSizes;
        return new LayoutSize(columnSizes.Sum(), rowSizes.Sum());
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        GridLength[] columns = GetColumnLengths();
        GridLength[] rows = GetRowLengths();
        float[] columnSizes = ResolveArrangeSizes(columns, measuredColumnSizes, context.FinalRect.Width);
        float[] rowSizes = ResolveArrangeSizes(rows, measuredRowSizes, context.FinalRect.Height);

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
                continue;
            }

            GridPlacement placement = GetPlacement(child, columnSizes.Length, rowSizes.Length);
            child.Arrange(new ArrangeContext(
                new LayoutRect(
                    context.FinalRect.X + Sum(columnSizes, 0, placement.Column),
                    context.FinalRect.Y + Sum(rowSizes, 0, placement.Row),
                    Sum(columnSizes, placement.Column, placement.ColumnSpan),
                    Sum(rowSizes, placement.Row, placement.RowSpan)),
                context.Rounding));
        }

        return context.FinalRect;
    }

    private GridLength[] GetColumnLengths()
    {
        return ColumnDefinitions.Count == 0
            ? [GridLength.Star]
            : ColumnDefinitions.Select(definition => definition.Width).ToArray();
    }

    private GridLength[] GetRowLengths()
    {
        return RowDefinitions.Count == 0
            ? [GridLength.Star]
            : RowDefinitions.Select(definition => definition.Height).ToArray();
    }

    private static float[] ResolveFixedSizes(GridLength[] lengths)
    {
        float[] sizes = new float[lengths.Length];
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i].Validate();
            if (lengths[i].IsPixel)
            {
                sizes[i] = lengths[i].Value;
            }
        }

        return sizes;
    }

    private static float[] ResolveArrangeSizes(GridLength[] lengths, float[] measuredSizes, float finalSize)
    {
        float[] sizes = ResolveFixedSizes(lengths);
        for (int i = 0; i < sizes.Length; i++)
        {
            if (lengths[i].IsAuto && i < measuredSizes.Length)
            {
                sizes[i] = measuredSizes[i];
            }
        }

        ResolveStarSizes(lengths, sizes, finalSize);
        return sizes;
    }

    private static void ResolveStarSizes(GridLength[] lengths, float[] sizes, float availableSize)
    {
        if (float.IsPositiveInfinity(availableSize))
        {
            return;
        }

        float used = sizes.Sum();
        float remaining = MathF.Max(0, availableSize - used);
        float totalStar = lengths.Where(length => length.IsStar).Sum(length => MathF.Max(0, length.Value));
        if (totalStar <= 0)
        {
            return;
        }

        for (int i = 0; i < lengths.Length; i++)
        {
            if (lengths[i].IsStar)
            {
                sizes[i] = remaining * (MathF.Max(0, lengths[i].Value) / totalStar);
            }
        }
    }

    private static float Sum(float[] values, int start, int count)
    {
        float total = 0;
        int end = Math.Min(values.Length, start + count);
        for (int i = Math.Min(start, values.Length); i < end; i++)
        {
            total += values[i];
        }

        return total;
    }

    private static GridPlacement GetPlacement(UIElement element)
    {
        GridPlacement placement = placements.TryGetValue(element, out GridPlacement? existing)
            ? existing
            : new GridPlacement();
        placement.ClampToAtLeastOneCell();
        return placement;
    }

    private static GridPlacement GetPlacement(UIElement element, int columnCount, int rowCount)
    {
        GridPlacement placement = GetPlacement(element);
        placement.ClampToGrid(columnCount, rowCount);
        return placement;
    }

    internal void InvalidateDefinitions(string reason)
    {
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest,
            reason);
    }

    private static void SetPlacement(UIElement element, Action<GridPlacement> update)
    {
        ArgumentNullException.ThrowIfNull(element);
        GridPlacement placement = placements.GetOrCreateValue(element);
        int oldRow = placement.Row;
        int oldColumn = placement.Column;
        int oldRowSpan = placement.RowSpan;
        int oldColumnSpan = placement.ColumnSpan;

        update(placement);
        placement.ClampToAtLeastOneCell();

        if (placement.Row == oldRow &&
            placement.Column == oldColumn &&
            placement.RowSpan == oldRowSpan &&
            placement.ColumnSpan == oldColumnSpan)
        {
            return;
        }

        if (element.VisualParent is Grid grid)
        {
            grid.IncrementLayoutVersion();
            grid.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "Grid child placement changed");
        }
    }

    private static int ValidateIndex(int value, string parameterName)
    {
        return value >= 0 ? value : throw new ArgumentOutOfRangeException(parameterName, "Grid index must be non-negative.");
    }

    private static int ValidateSpan(int value, string parameterName)
    {
        return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName, "Grid span must be positive.");
    }

    private sealed class GridPlacement
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public int RowSpan { get; set; } = 1;

        public int ColumnSpan { get; set; } = 1;

        public void ClampToAtLeastOneCell()
        {
            RowSpan = Math.Max(1, RowSpan);
            ColumnSpan = Math.Max(1, ColumnSpan);
        }

        public void ClampToGrid(int columnCount, int rowCount)
        {
            columnCount = Math.Max(1, columnCount);
            rowCount = Math.Max(1, rowCount);
            Column = Math.Min(Column, columnCount - 1);
            Row = Math.Min(Row, rowCount - 1);
            ColumnSpan = Math.Min(Math.Max(1, ColumnSpan), columnCount - Column);
            RowSpan = Math.Min(Math.Max(1, RowSpan), rowCount - Row);
        }
    }
}
