using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Layout.Panels;

public class Grid : Panel
{
    private const int MaxCyclicMeasurePasses = 5;
    private static readonly ConditionalWeakTable<UIElement, StrongBox<GridPlacement>> placements = new();
    private float[] measuredColumnSizes = [0];
    private float[] measuredRowSizes = [0];
    private bool measuredColumnsUnbounded;
    private bool measuredRowsUnbounded;

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
        return placements.TryGetValue(element, out StrongBox<GridPlacement>? placement) ? placement.Value.Column : 0;
    }

    public static void SetColumn(UIElement element, int column)
    {
        SetPlacement(element, placement => placement with { Column = ValidateIndex(column, nameof(column)) });
    }

    public static int GetRow(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out StrongBox<GridPlacement>? placement) ? placement.Value.Row : 0;
    }

    public static void SetRow(UIElement element, int row)
    {
        SetPlacement(element, placement => placement with { Row = ValidateIndex(row, nameof(row)) });
    }

    public static int GetColumnSpan(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out StrongBox<GridPlacement>? placement) ? placement.Value.ColumnSpan : 1;
    }

    public static void SetColumnSpan(UIElement element, int columnSpan)
    {
        SetPlacement(element, placement => placement with { ColumnSpan = ValidateSpan(columnSpan, nameof(columnSpan)) });
    }

    public static int GetRowSpan(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return placements.TryGetValue(element, out StrongBox<GridPlacement>? placement) ? placement.Value.RowSpan : 1;
    }

    public static void SetRowSpan(UIElement element, int rowSpan)
    {
        SetPlacement(element, placement => placement with { RowSpan = ValidateSpan(rowSpan, nameof(rowSpan)) });
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return MeasureGrid(context, reconcileVisibilityChanges: true);
    }

    private LayoutSize MeasureGrid(MeasureContext context, bool reconcileVisibilityChanges)
    {
        Span<Visibility> initialVisibilities = reconcileVisibilityChanges
            ? VisualChildren.Count <= 128
                ? stackalloc Visibility[VisualChildren.Count]
                : new Visibility[VisualChildren.Count]
            : [];
        for (int index = 0; index < initialVisibilities.Length; index++)
        {
            initialVisibilities[index] = VisualChildren[index].Visibility;
        }

        GridLength[] columns = GetColumnLengths();
        GridLength[] rows = GetRowLengths();
        float[] columnSizes = ResolveFixedSizes(columns);
        float[] rowSizes = ResolveFixedSizes(rows);

        bool columnsDependOnStarRows = false;
        bool rowsDependOnStarColumns = false;
        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new MeasureContext(LayoutSize.Zero, context.Rounding));
                continue;
            }

            GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
            columnsDependOnStarRows |=
                RequiresContentMeasure(columns, placement.Column, placement.ColumnSpan, context.AvailableSize.Width) &&
                SpansResolvedStar(rows, placement.Row, placement.RowSpan, context.AvailableSize.Height);
            rowsDependOnStarColumns |=
                RequiresContentMeasure(rows, placement.Row, placement.RowSpan, context.AvailableSize.Height) &&
                SpansResolvedStar(columns, placement.Column, placement.ColumnSpan, context.AvailableSize.Width);
        }

        // The first axis is content-sized and resolved before the second, so the second axis measures its
        // content-sized children at final sizes. Columns go first unless only the columns depend on star rows.
        bool rowsFirst = columnsDependOnStarRows && !rowsDependOnStarColumns;
        bool firstAxisMeasuredBeforeCrossFinal = MeasureContentSizedDefinitions(
            context, columnAxis: !rowsFirst, columns, columnSizes, rows, rowSizes, crossAxisResolved: false);
        MeasureContentSizedDefinitions(context, columnAxis: rowsFirst, columns, columnSizes, rows, rowSizes, crossAxisResolved: true);

        // When both axes depend on each other, the columns were content-sized against unresolved star rows.
        // Content-size them again against the resolved rows, then the rows against the re-resolved columns,
        // until the columns stop changing. As in WPF, the number of passes is bounded.
        if (columnsDependOnStarRows && rowsDependOnStarColumns)
        {
            for (int pass = 0; pass < MaxCyclicMeasurePasses; pass++)
            {
                float[] previousColumnSizes = columnSizes;
                columnSizes = ResolveFixedSizes(columns);
                MeasureContentSizedDefinitions(context, columnAxis: true, columns, columnSizes, rows, rowSizes, crossAxisResolved: true);
                if (columnSizes.AsSpan().SequenceEqual(previousColumnSizes))
                {
                    break;
                }

                rowSizes = ResolveFixedSizes(rows);
                MeasureContentSizedDefinitions(context, columnAxis: false, columns, columnSizes, rows, rowSizes, crossAxisResolved: true);
            }
        }
        else if (firstAxisMeasuredBeforeCrossFinal)
        {
            // Some first-axis children were measured before their cross size was final. Size the first axis
            // again from their measures at the resolved cross sizes, so its stars share what the final measure leaves.
            // When that changes the first axis, the second axis was measured against stale sizes: size it again
            // too, then the first axis against it, until the first axis stops changing. The sizes that children
            // in one definition gave an axis stay minimums and only grow, as in the final measure: the other axis
            // was already measured against them. The shares of spanning children and of stars on a bounded axis
            // are dropped and shared again from the new sizes. The cyclic passes above already start with a
            // re-size of the columns, which are then the first axis.
            for (int pass = 0; pass < MaxCyclicMeasurePasses; pass++)
            {
                float[] previousFirstSizes = rowsFirst ? rowSizes : columnSizes;
                if (rowsFirst)
                {
                    rowSizes = ResolveSingleDefinitionSizes(columnAxis: false, columns, rows, context.AvailableSize.Height);
                }
                else
                {
                    columnSizes = ResolveSingleDefinitionSizes(columnAxis: true, columns, rows, context.AvailableSize.Width);
                }

                MeasureContentSizedDefinitions(context, columnAxis: !rowsFirst, columns, columnSizes, rows, rowSizes, crossAxisResolved: true);
                if ((rowsFirst ? rowSizes : columnSizes).AsSpan().SequenceEqual(previousFirstSizes))
                {
                    break;
                }

                if (rowsFirst)
                {
                    columnSizes = ResolveSingleDefinitionSizes(columnAxis: true, columns, rows, context.AvailableSize.Width);
                }
                else
                {
                    rowSizes = ResolveSingleDefinitionSizes(columnAxis: false, columns, rows, context.AvailableSize.Height);
                }

                MeasureContentSizedDefinitions(context, columnAxis: rowsFirst, columns, columnSizes, rows, rowSizes, crossAxisResolved: true);
            }
        }

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
            child.Measure(new MeasureContext(
                new LayoutSize(
                    GetChildConstraint(columns, placement.Column, placement.ColumnSpan, Sum(columnSizes, placement.Column, placement.ColumnSpan)),
                    GetChildConstraint(rows, placement.Row, placement.RowSpan, Sum(rowSizes, placement.Row, placement.RowSpan))),
                context.Rounding));
        }

        ExpandContentSizedDefinitions(columnAxis: true, columns, rows, columnSizes, context.AvailableSize.Width);
        ExpandContentSizedDefinitions(columnAxis: false, columns, rows, rowSizes, context.AvailableSize.Height);

        if (reconcileVisibilityChanges && VisualChildren.Count == initialVisibilities.Length)
        {
            for (int index = 0; index < initialVisibilities.Length; index++)
            {
                if (VisualChildren[index].Visibility != initialVisibilities[index])
                {
                    return MeasureGrid(context, reconcileVisibilityChanges: false);
                }
            }
        }

        measuredColumnSizes = columnSizes;
        measuredRowSizes = rowSizes;
        measuredColumnsUnbounded = float.IsPositiveInfinity(context.AvailableSize.Width);
        measuredRowsUnbounded = float.IsPositiveInfinity(context.AvailableSize.Height);
        return new LayoutSize(columnSizes.Sum(), rowSizes.Sum());
    }

    // A child sizes the definitions it spans when all of them are content-sized: Auto definitions, and star
    // definitions on an unbounded axis. A pixel definition keeps its size, and a star definition on a bounded
    // axis gets a share of what is left, so a child that spans either of them sizes no definition.
    private static bool RequiresContentMeasure(
        GridLength[] lengths,
        int index,
        int span,
        float availableSize)
    {
        for (int i = index; i < index + span; i++)
        {
            if (lengths[i].IsPixel || (lengths[i].IsStar && !float.IsPositiveInfinity(availableSize)))
            {
                return false;
            }
        }

        return true;
    }

    // As in WPF, a child that spans only Auto definitions is measured with positive infinity.
    private static float GetChildConstraint(
        GridLength[] lengths,
        int index,
        int span,
        float size)
    {
        return SpansOnlyAuto(lengths, index, span) ? float.PositiveInfinity : size;
    }

    private static bool SpansOnlyAuto(GridLength[] lengths, int index, int span)
    {
        for (int i = index; i < index + span; i++)
        {
            if (!lengths[i].IsAuto)
            {
                return false;
            }
        }

        return true;
    }

    // Returns true when a child was measured with a cross constraint that is not yet its final one.
    private bool MeasureContentSizedDefinitions(
        MeasureContext context,
        bool columnAxis,
        GridLength[] columns,
        float[] columnSizes,
        GridLength[] rows,
        float[] rowSizes,
        bool crossAxisResolved)
    {
        GridLength[] lengths = columnAxis ? columns : rows;
        float[] sizes = columnAxis ? columnSizes : rowSizes;
        float availableSize = columnAxis ? context.AvailableSize.Width : context.AvailableSize.Height;
        GridLength[] crossLengths = columnAxis ? rows : columns;
        float[] crossSizes = columnAxis ? rowSizes : columnSizes;
        float crossAvailableSize = columnAxis ? context.AvailableSize.Height : context.AvailableSize.Width;
        bool measuredBeforeCrossFinal = false;

        foreach (UIElement child in VisualChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
            int index = columnAxis ? placement.Column : placement.Row;
            int span = columnAxis ? placement.ColumnSpan : placement.RowSpan;
            if (!RequiresContentMeasure(lengths, index, span, availableSize))
            {
                continue;
            }

            int crossIndex = columnAxis ? placement.Row : placement.Column;
            int crossSpan = columnAxis ? placement.RowSpan : placement.ColumnSpan;
            float constraint = GetChildConstraint(lengths, index, span, availableSize);
            bool crossFinal = crossAxisResolved || IsChildConstraintFinalBeforeResolve(crossLengths, crossIndex, crossSpan);
            measuredBeforeCrossFinal |= !crossFinal;
            float crossConstraint = GetChildConstraint(
                crossLengths,
                crossIndex,
                crossSpan,
                crossFinal ? Sum(crossSizes, crossIndex, crossSpan) : crossAvailableSize);
            child.Measure(new MeasureContext(
                columnAxis ? new LayoutSize(constraint, crossConstraint) : new LayoutSize(crossConstraint, constraint),
                context.Rounding));
        }

        ExpandContentSizedDefinitions(columnAxis, columns, rows, sizes, availableSize);
        ResolveStarSizes(lengths, sizes, availableSize);
        return measuredBeforeCrossFinal;
    }

    // Before an axis is resolved, only its pixel definitions already have their final size. A child that
    // spans only Auto definitions is measured with positive infinity either way.
    private static bool IsChildConstraintFinalBeforeResolve(GridLength[] lengths, int index, int span)
    {
        if (SpansOnlyAuto(lengths, index, span))
        {
            return true;
        }

        for (int i = index; i < index + span; i++)
        {
            if (!lengths[i].IsPixel)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SpansResolvedStar(GridLength[] lengths, int index, int span, float availableSize)
    {
        if (float.IsPositiveInfinity(availableSize))
        {
            return false;
        }

        for (int i = index; i < index + span; i++)
        {
            if (lengths[i].IsStar)
            {
                return true;
            }
        }

        return false;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        GridLength[] columns = GetColumnLengths();
        GridLength[] rows = GetRowLengths();
        float[] columnSizes = ResolveArrangeSizes(columns, measuredColumnSizes, measuredColumnsUnbounded, context.FinalRect.Width);
        float[] rowSizes = ResolveArrangeSizes(rows, measuredRowSizes, measuredRowsUnbounded, context.FinalRect.Height);
        RoundSizes(columnSizes, context.Rounding);
        RoundSizes(rowSizes, context.Rounding);

        // Offsets start at the Grid's rounded origin, where its own bounds start, as WPF offsets children from
        // the parent: from an unrounded origin such as 0.5, each child would round on its own and leave a gap.
        float originX = context.Rounding.Round(context.FinalRect.X);
        float originY = context.Rounding.Round(context.FinalRect.Y);
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
                    originX + Sum(columnSizes, 0, placement.Column),
                    originY + Sum(rowSizes, 0, placement.Row),
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

    // The pixel sizes of an axis, and the sizes its children in one definition give it at their current measures.
    private float[] ResolveSingleDefinitionSizes(bool columnAxis, GridLength[] columns, GridLength[] rows, float availableSize)
    {
        float[] sizes = ResolveFixedSizes(columnAxis ? columns : rows);
        ExpandContentSizedDefinitions(columnAxis, columns, rows, sizes, availableSize, maxSpan: 1);
        return sizes;
    }

    // As in WPF, children in one definition size it first. Spanning children follow, narrowest span first,
    // and each adds only the size that the definitions it spans still miss.
    private void ExpandContentSizedDefinitions(
        bool columnAxis,
        GridLength[] columns,
        GridLength[] rows,
        float[] sizes,
        float availableSize,
        int maxSpan = int.MaxValue)
    {
        GridLength[] lengths = columnAxis ? columns : rows;
        int span = 1;
        while (span <= Math.Min(maxSpan, lengths.Length))
        {
            int nextSpan = int.MaxValue;
            foreach (UIElement child in VisualChildren)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                GridPlacement placement = GetPlacement(child, columns.Length, rows.Length);
                int childSpan = columnAxis ? placement.ColumnSpan : placement.RowSpan;
                if (childSpan == span)
                {
                    ExpandContentSizedDefinition(
                        lengths,
                        sizes,
                        columnAxis ? placement.Column : placement.Row,
                        span,
                        availableSize,
                        columnAxis ? child.DesiredSize.Width : child.DesiredSize.Height);
                }
                else if (childSpan > span)
                {
                    nextSpan = Math.Min(nextSpan, childSpan);
                }
            }

            span = nextSpan;
        }
    }

    // The missing size is shared equally. As in WPF, star definitions in the span take it before Auto
    // definitions, which only grow when the span has no star definition.
    private static void ExpandContentSizedDefinition(
        GridLength[] lengths,
        float[] sizes,
        int index,
        int span,
        float availableSize,
        float desiredSize)
    {
        if (!RequiresContentMeasure(lengths, index, span, availableSize))
        {
            return;
        }

        float missing = desiredSize - Sum(sizes, index, span);
        if (missing <= 0)
        {
            return;
        }

        bool spansStar = false;
        for (int i = index; i < index + span; i++)
        {
            spansStar |= lengths[i].IsStar;
        }

        int receiverCount = 0;
        for (int i = index; i < index + span; i++)
        {
            if (lengths[i].IsStar == spansStar)
            {
                receiverCount++;
            }
        }

        for (int i = index; i < index + span; i++)
        {
            if (lengths[i].IsStar == spansStar)
            {
                sizes[i] += missing / receiverCount;
            }
        }
    }

    private static float[] ResolveArrangeSizes(
        GridLength[] lengths,
        float[] measuredSizes,
        bool measuredUnbounded,
        float finalSize)
    {
        float[] sizes = ResolveFixedSizes(lengths);
        for (int i = 0; i < sizes.Length; i++)
        {
            if (lengths[i].IsAuto && i < measuredSizes.Length)
            {
                sizes[i] = measuredSizes[i];
            }
        }

        // Star definitions measured against an unbounded axis were sized to their content;
        // that content size stays a minimum when the final size is shared by weight.
        ResolveStarSizes(lengths, sizes, finalSize, measuredUnbounded ? measuredSizes : null);
        return sizes;
    }

    private static void ResolveStarSizes(
        GridLength[] lengths,
        float[] sizes,
        float availableSize,
        float[]? minimumStarSizes = null)
    {
        if (float.IsPositiveInfinity(availableSize))
        {
            return;
        }

        float used = sizes.Sum();
        float remaining = MathF.Max(0, availableSize - used);
        float totalStar = lengths.Where(length => length.IsStar).Sum(length => MathF.Max(0, length.Value));
        bool[]? sizedToMinimum = null;
        if (minimumStarSizes is not null)
        {
            sizedToMinimum = new bool[lengths.Length];
            // A definition whose weighted share is below its minimum keeps the minimum and leaves
            // the share computation; repeat until every remaining share meets its minimum.
            bool clamped = true;
            while (clamped)
            {
                clamped = false;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (!lengths[i].IsStar || sizedToMinimum[i] || i >= minimumStarSizes.Length)
                    {
                        continue;
                    }

                    float weight = MathF.Max(0, lengths[i].Value);
                    float share = totalStar > 0 ? remaining * (weight / totalStar) : 0;
                    if (share < minimumStarSizes[i])
                    {
                        sizes[i] = minimumStarSizes[i];
                        sizedToMinimum[i] = true;
                        remaining = MathF.Max(0, remaining - minimumStarSizes[i]);
                        totalStar -= weight;
                        clamped = true;
                    }
                }
            }
        }

        if (totalStar <= 0)
        {
            return;
        }

        for (int i = 0; i < lengths.Length; i++)
        {
            if (lengths[i].IsStar && sizedToMinimum?[i] != true)
            {
                sizes[i] = remaining * (MathF.Max(0, lengths[i].Value) / totalStar);
            }
        }
    }

    // With layout rounding, each definition gets a whole number of device pixels, and the rounded sizes add up to
    // the rounded total, so neighbouring cells meet and the last one ends on the Grid's edge. As in WPF, the
    // definitions that rounding moved furthest take up the difference, one device pixel each.
    private static void RoundSizes(float[] sizes, LayoutRounding rounding)
    {
        if (!rounding.IsEnabled)
        {
            return;
        }

        float[] errors = new float[sizes.Length];
        float total = 0;
        float roundedTotal = 0;
        for (int i = 0; i < sizes.Length; i++)
        {
            float scaled = sizes[i] * rounding.Scale;
            float rounded = MathF.Round(scaled);
            errors[i] = rounded - scaled;
            total += scaled;
            roundedTotal += rounded;
            sizes[i] = rounded;
        }

        float discrepancy = float.IsFinite(total) ? MathF.Round(total) - roundedTotal : 0;
        if (discrepancy != 0)
        {
            // Rounded down the most first; equal errors keep definition order, so earlier definitions grow first
            // and later ones shrink first.
            int[] order = Enumerable.Range(0, sizes.Length).ToArray();
            Array.Sort(order, (left, right) =>
            {
                int comparison = errors[left].CompareTo(errors[right]);
                return comparison != 0 ? comparison : left.CompareTo(right);
            });

            for (int i = 0; discrepancy > 0 && i < order.Length; i++)
            {
                sizes[order[i]]++;
                discrepancy--;
            }

            for (int i = order.Length - 1; discrepancy < 0 && i >= 0; i--)
            {
                if (sizes[order[i]] >= 1)
                {
                    sizes[order[i]]--;
                    discrepancy++;
                }
            }
        }

        for (int i = 0; i < sizes.Length; i++)
        {
            sizes[i] /= rounding.Scale;
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
        GridPlacement placement = placements.TryGetValue(element, out StrongBox<GridPlacement>? existing)
            ? existing.Value
            : new GridPlacement();
        placement.ClampToAtLeastOneCell();
        return placement;
    }

    private static GridPlacement GetPlacement(UIElement element, int columnCount, int rowCount)
    {
        // GridPlacement is a value type: clamping changes this copy, not the stored placement.
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

    private static void SetPlacement(UIElement element, Func<GridPlacement, GridPlacement> update)
    {
        ArgumentNullException.ThrowIfNull(element);
        StrongBox<GridPlacement> stored = placements.GetValue(element, static _ => new StrongBox<GridPlacement>(new GridPlacement()));

        GridPlacement placement = update(stored.Value);
        placement.ClampToAtLeastOneCell();

        if (placement == stored.Value)
        {
            return;
        }

        stored.Value = placement;
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

    private record struct GridPlacement
    {
        public GridPlacement()
        {
        }

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
