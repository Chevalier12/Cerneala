using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.UI.Prism.Runtime;

public sealed class PrismInstance
{
    private PrismRuntimeGraph graph;
    private int generation = 1;

    public PrismInstance(PrismCompositionDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        graph = PrismRuntimeGraph.Create(this, definition, generation);
        StructuralVersion = new PrismStructuralVersion(1);
        ValueVersion = new PrismValueVersion(0);
    }

    public PrismCompositionDefinition Definition { get; private set; }

    public PrismCompositionState Composition => graph.Composition;

    public PrismBackdropState? Backdrop => graph.Backdrop;

    public PrismStructuralVersion StructuralVersion { get; private set; }

    public PrismValueVersion ValueVersion { get; private set; }

    public PrismNodeState GetNodeState(PrismNodeId id)
    {
        return graph.Nodes.TryGetValue(id, out PrismNodeState? state)
            ? state
            : throw new KeyNotFoundException($"Prism node '{id.Value}' does not exist.");
    }

    public PrismLayerState GetLayerState(PrismNodeId id)
    {
        return GetNodeState(id) as PrismLayerState
            ?? throw new InvalidOperationException($"Prism node '{id.Value}' is not a layer.");
    }

    public PrismGroupState GetGroupState(PrismNodeId id)
    {
        return GetNodeState(id) as PrismGroupState
            ?? throw new InvalidOperationException($"Prism node '{id.Value}' is not a group.");
    }

    public PrismBackdropState GetBackdropState(PrismNodeId id)
    {
        return GetNodeState(id) as PrismBackdropState
            ?? throw new InvalidOperationException($"Prism node '{id.Value}' is not a backdrop.");
    }

    public void ReplaceDefinition(PrismCompositionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (Definition.Equals(definition))
        {
            Definition = definition;
            return;
        }

        bool topologyChanged = !PrismTopologyComparer.Equals(Definition, definition);
        int nextGeneration = checked(generation + 1);
        PrismRuntimeGraph replacement = PrismRuntimeGraph.Create(this, definition, nextGeneration);
        bool dataChanged = !topologyChanged && !graph.Values.ContentEquals(replacement.Values);

        Definition = definition;
        graph = replacement;
        generation = nextGeneration;
        if (topologyChanged)
        {
            StructuralVersion = StructuralVersion.Next();
        }
        if (dataChanged)
        {
            ValueVersion = ValueVersion.Next();
        }
    }

    public void ResetToDefaults()
    {
        if (graph.Values.CopyFromIfDifferent(graph.Defaults))
        {
            ValueVersion = ValueVersion.Next();
        }
    }

    internal void MarkValueChanged(int stateGeneration)
    {
        EnsureCurrent(stateGeneration);
        ValueVersion = ValueVersion.Next();
    }

    internal void EnsureCurrent(int stateGeneration)
    {
        if (stateGeneration != generation)
        {
            throw new InvalidOperationException("This Prism state handle belongs to a replaced definition.");
        }
    }
}

internal sealed class PrismRuntimeGraph
{
    private PrismRuntimeGraph(
        PrismParameterStore defaults,
        PrismParameterStore values,
        PrismCompositionState composition,
        Dictionary<PrismNodeId, PrismNodeState> nodes,
        PrismBackdropState? backdrop)
    {
        Defaults = defaults;
        Values = values;
        Composition = composition;
        Nodes = nodes;
        Backdrop = backdrop;
    }

    public PrismParameterStore Defaults { get; }

    public PrismParameterStore Values { get; }

    public PrismCompositionState Composition { get; }

    public Dictionary<PrismNodeId, PrismNodeState> Nodes { get; }

    public PrismBackdropState? Backdrop { get; }

    public static PrismRuntimeGraph Create(
        PrismInstance owner,
        PrismCompositionDefinition definition,
        int generation)
    {
        PrismValueCounts counts = Measure(definition);
        PrismParameterStore defaults = new(counts);
        PrismParameterStore values = new(counts);
        PrismValueAllocator allocator = new(defaults);

        PrismValueSlice compositionSlice = allocator.Allocate(
            PrismCatalogGenerated.CommonCompositionProperties);
        defaults.Set(
            compositionSlice,
            PrismCatalogGenerated.PrismCompositionPropertyKeys.WorkingColorProfileKey,
            (int)definition.WorkingColorProfile);
        defaults.Set(
            compositionSlice,
            PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAngleKey,
            definition.GlobalLightAngle);
        defaults.Set(
            compositionSlice,
            PrismCatalogGenerated.PrismCompositionPropertyKeys.GlobalLightAltitudeKey,
            definition.GlobalLightAltitude);

        Dictionary<PrismNodeId, PrismNodeState> nodes = new(definition.Nodes.Length);
        PrismNodeState[] roots = new PrismNodeState[definition.Nodes.Length];
        for (int index = 0; index < definition.Nodes.Length; index++)
        {
            roots[index] = BuildNode(
                owner,
                definition.Nodes[index],
                defaults,
                values,
                allocator,
                generation,
                nodes);
        }

        values.CopyFromIfDifferent(defaults);
        PrismCompositionState composition = new(
            new PrismStateAccess(owner, values, compositionSlice, generation, entryStableId: 0));
        PrismBackdropState? backdrop = roots[^1] as PrismBackdropState;
        return new PrismRuntimeGraph(defaults, values, composition, nodes, backdrop);
    }

    private static PrismNodeState BuildNode(
        PrismInstance owner,
        PrismNodeDefinition definition,
        PrismParameterStore defaults,
        PrismParameterStore values,
        PrismValueAllocator allocator,
        int generation,
        Dictionary<PrismNodeId, PrismNodeState> nodes)
    {
        PrismNodeState state;
        switch (definition)
        {
            case PrismLayerDefinition layer:
            {
                PrismValueSlice slice = allocator.Allocate(PrismCatalogGenerated.CommonLayerProperties);
                defaults.Set(slice, PrismCatalogGenerated.PrismLayerPropertyKeys.VisibleKey, layer.Visible);
                defaults.Set(slice, PrismCatalogGenerated.PrismLayerPropertyKeys.OpacityKey, layer.Opacity);
                defaults.Set(slice, PrismCatalogGenerated.PrismLayerPropertyKeys.FillKey, layer.Fill);
                defaults.Set(slice, PrismCatalogGenerated.PrismLayerPropertyKeys.BlendModeKey, (int)layer.BlendMode);
                defaults.Set(slice, PrismCatalogGenerated.PrismLayerPropertyKeys.ClipToBelowKey, layer.ClipToBelow);
                state = new PrismLayerState(
                    Access(owner, values, slice, generation),
                    layer.Id,
                    layer.Name,
                    BuildFilters(owner, layer.Filters, defaults, values, allocator, generation),
                    BuildStyles(owner, layer.Styles, defaults, values, allocator, generation),
                    BuildMask(owner, layer.Mask, defaults, values, allocator, generation));
                break;
            }
            case PrismGroupDefinition group:
            {
                PrismValueSlice slice = allocator.Allocate(PrismCatalogGenerated.CommonGroupProperties);
                defaults.Set(slice, PrismCatalogGenerated.PrismGroupPropertyKeys.VisibleKey, group.Visible);
                defaults.Set(slice, PrismCatalogGenerated.PrismGroupPropertyKeys.OpacityKey, group.Opacity);
                defaults.Set(slice, PrismCatalogGenerated.PrismGroupPropertyKeys.BlendModeKey, (int)group.BlendMode);
                PrismNodeState[] children = new PrismNodeState[group.Children.Length];
                for (int index = 0; index < group.Children.Length; index++)
                {
                    children[index] = BuildNode(
                        owner,
                        group.Children[index],
                        defaults,
                        values,
                        allocator,
                        generation,
                        nodes);
                }
                state = new PrismGroupState(
                    Access(owner, values, slice, generation),
                    group.Id,
                    group.Name,
                    children,
                    BuildFilters(owner, group.Filters, defaults, values, allocator, generation),
                    BuildStyles(owner, group.Styles, defaults, values, allocator, generation),
                    BuildMask(owner, group.Mask, defaults, values, allocator, generation));
                break;
            }
            case PrismBackdropDefinition backdrop:
            {
                PrismValueSlice slice = allocator.Allocate(PrismCatalogGenerated.CommonBackdropProperties);
                defaults.Set(slice, PrismCatalogGenerated.PrismBackdropPropertyKeys.VisibleKey, backdrop.Visible);
                defaults.Set(slice, PrismCatalogGenerated.PrismBackdropPropertyKeys.OpacityKey, backdrop.Opacity);
                state = new PrismBackdropState(
                    Access(owner, values, slice, generation),
                    backdrop.Id,
                    backdrop.Name,
                    BuildFilters(owner, backdrop.Filters, defaults, values, allocator, generation),
                    BuildStyles(owner, backdrop.Styles, defaults, values, allocator, generation),
                    BuildMask(owner, backdrop.Mask, defaults, values, allocator, generation));
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown Prism node definition '{definition.GetType().Name}'.");
        }

        nodes.Add(definition.Id, state);
        return state;
    }

    private static PrismFilterState[] BuildFilters(
        PrismInstance owner,
        IReadOnlyList<PrismFilterDefinition> definitions,
        PrismParameterStore defaults,
        PrismParameterStore values,
        PrismValueAllocator allocator,
        int generation)
    {
        PrismFilterState[] states = new PrismFilterState[definitions.Count];
        for (int index = 0; index < definitions.Count; index++)
        {
            PrismFilterDefinition definition = definitions[index];
            int stableId = (int)definition.Filter;
            PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
            PrismValueSlice commonSlice = allocator.Allocate(PrismCatalogGenerated.CommonFilterProperties);
            PrismValueSlice parameterSlice = allocator.Allocate(entry.Properties);
            defaults.Set(
                commonSlice,
                PrismCatalogGenerated.PrismFilterCommonParameterKeys.VisibleKey,
                definition.Visible);
            defaults.Set(
                commonSlice,
                PrismCatalogGenerated.PrismFilterCommonParameterKeys.OpacityKey,
                definition.Opacity);
            defaults.Set(
                commonSlice,
                PrismCatalogGenerated.PrismFilterCommonParameterKeys.BlendModeKey,
                (int)definition.BlendMode);
            states[index] = new PrismFilterState(
                definition.Filter,
                Access(owner, values, commonSlice, generation),
                new PrismStateAccess(owner, values, parameterSlice, generation, stableId));
        }
        return states;
    }

    private static PrismStyleState[] BuildStyles(
        PrismInstance owner,
        IReadOnlyList<PrismStyleDefinition> definitions,
        PrismParameterStore defaults,
        PrismParameterStore values,
        PrismValueAllocator allocator,
        int generation)
    {
        PrismStyleState[] states = new PrismStyleState[definitions.Count];
        for (int index = 0; index < definitions.Count; index++)
        {
            PrismStyleDefinition definition = definitions[index];
            int stableId = (int)definition.Style;
            PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
            PrismValueSlice commonSlice = allocator.Allocate(PrismCatalogGenerated.CommonStyleProperties);
            PrismValueSlice parameterSlice = allocator.Allocate(entry.Properties);
            defaults.Set(
                commonSlice,
                PrismCatalogGenerated.PrismStyleCommonParameterKeys.VisibleKey,
                definition.Visible);
            states[index] = new PrismStyleState(
                definition.Style,
                Access(owner, values, commonSlice, generation),
                new PrismStateAccess(owner, values, parameterSlice, generation, stableId));
        }
        return states;
    }

    private static PrismMaskState? BuildMask(
        PrismInstance owner,
        PrismMaskDefinition? definition,
        PrismParameterStore defaults,
        PrismParameterStore values,
        PrismValueAllocator allocator,
        int generation)
    {
        if (definition is null)
        {
            return null;
        }

        PrismValueSlice slice = allocator.Allocate(PrismCatalogGenerated.CommonMaskProperties);
        defaults.Set(slice, PrismCatalogGenerated.PrismMaskPropertyKeys.ImageKey, definition.Image);
        defaults.Set(slice, PrismCatalogGenerated.PrismMaskPropertyKeys.ChannelKey, (int)definition.Channel);
        defaults.Set(slice, PrismCatalogGenerated.PrismMaskPropertyKeys.FeatherKey, definition.Feather);
        defaults.Set(slice, PrismCatalogGenerated.PrismMaskPropertyKeys.DensityKey, definition.Density);
        defaults.Set(slice, PrismCatalogGenerated.PrismMaskPropertyKeys.InvertKey, definition.Invert);
        return new PrismMaskState(Access(owner, values, slice, generation));
    }

    private static PrismStateAccess Access(
        PrismInstance owner,
        PrismParameterStore values,
        PrismValueSlice slice,
        int generation) =>
        new(owner, values, slice, generation, entryStableId: 0);

    private static PrismValueCounts Measure(PrismCompositionDefinition definition)
    {
        PrismValueCounts counts = new();
        counts.Add(PrismCatalogGenerated.CommonCompositionProperties);
        foreach (PrismNodeDefinition node in definition.Nodes)
        {
            MeasureNode(node, ref counts);
        }
        return counts;
    }

    private static void MeasureNode(PrismNodeDefinition node, ref PrismValueCounts counts)
    {
        switch (node)
        {
            case PrismLayerDefinition layer:
                counts.Add(PrismCatalogGenerated.CommonLayerProperties);
                MeasureOperations(layer.Filters, layer.Styles, layer.Mask, ref counts);
                break;
            case PrismGroupDefinition group:
                counts.Add(PrismCatalogGenerated.CommonGroupProperties);
                MeasureOperations(group.Filters, group.Styles, group.Mask, ref counts);
                foreach (PrismNodeDefinition child in group.Children)
                {
                    MeasureNode(child, ref counts);
                }
                break;
            case PrismBackdropDefinition backdrop:
                counts.Add(PrismCatalogGenerated.CommonBackdropProperties);
                MeasureOperations(backdrop.Filters, backdrop.Styles, backdrop.Mask, ref counts);
                break;
            default:
                throw new InvalidOperationException($"Unknown Prism node definition '{node.GetType().Name}'.");
        }
    }

    private static void MeasureOperations(
        IReadOnlyList<PrismFilterDefinition> filters,
        IReadOnlyList<PrismStyleDefinition> styles,
        PrismMaskDefinition? mask,
        ref PrismValueCounts counts)
    {
        foreach (PrismFilterDefinition filter in filters)
        {
            counts.Add(PrismCatalogGenerated.CommonFilterProperties);
            counts.Add(PrismCatalogRuntime.GetEntry((int)filter.Filter).Properties);
        }
        foreach (PrismStyleDefinition style in styles)
        {
            counts.Add(PrismCatalogGenerated.CommonStyleProperties);
            counts.Add(PrismCatalogRuntime.GetEntry((int)style.Style).Properties);
        }
        if (mask is not null)
        {
            counts.Add(PrismCatalogGenerated.CommonMaskProperties);
        }
    }
}

internal static class PrismTopologyComparer
{
    public static bool Equals(
        PrismCompositionDefinition left,
        PrismCompositionDefinition right)
    {
        if (left.Nodes.Length != right.Nodes.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Nodes.Length; index++)
        {
            if (!EqualsNode(left.Nodes[index], right.Nodes[index]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EqualsNode(PrismNodeDefinition left, PrismNodeDefinition right)
    {
        if (left.GetType() != right.GetType() ||
            left.Id != right.Id ||
            !string.Equals(left.Name, right.Name, StringComparison.Ordinal))
        {
            return false;
        }

        return (left, right) switch
        {
            (PrismLayerDefinition a, PrismLayerDefinition b) =>
                EqualsOperations(a.Filters, b.Filters, a.Styles, b.Styles, a.Mask, b.Mask),
            (PrismBackdropDefinition a, PrismBackdropDefinition b) =>
                EqualsOperations(a.Filters, b.Filters, a.Styles, b.Styles, a.Mask, b.Mask),
            (PrismGroupDefinition a, PrismGroupDefinition b) =>
                EqualsOperations(a.Filters, b.Filters, a.Styles, b.Styles, a.Mask, b.Mask) &&
                EqualsChildren(a.Children, b.Children),
            _ => false
        };
    }

    private static bool EqualsChildren(
        IReadOnlyList<PrismNodeDefinition> left,
        IReadOnlyList<PrismNodeDefinition> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        for (int index = 0; index < left.Count; index++)
        {
            if (!EqualsNode(left[index], right[index]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EqualsOperations(
        IReadOnlyList<PrismFilterDefinition> leftFilters,
        IReadOnlyList<PrismFilterDefinition> rightFilters,
        IReadOnlyList<PrismStyleDefinition> leftStyles,
        IReadOnlyList<PrismStyleDefinition> rightStyles,
        PrismMaskDefinition? leftMask,
        PrismMaskDefinition? rightMask)
    {
        if (leftFilters.Count != rightFilters.Count ||
            leftStyles.Count != rightStyles.Count ||
            (leftMask is null) != (rightMask is null))
        {
            return false;
        }
        for (int index = 0; index < leftFilters.Count; index++)
        {
            if (leftFilters[index].Filter != rightFilters[index].Filter)
            {
                return false;
            }
        }
        for (int index = 0; index < leftStyles.Count; index++)
        {
            if (leftStyles[index].Style != rightStyles[index].Style)
            {
                return false;
            }
        }
        return true;
    }
}
