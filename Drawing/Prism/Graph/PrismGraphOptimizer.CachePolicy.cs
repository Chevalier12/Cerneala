using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

internal sealed partial class PrismGraphOptimizer
{
    private static PrismGraphUncacheableReason CalculateUncacheableReasons(
        PrismGraphNode node,
        PrismGraphScope scope,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans)
    {
        PrismGraphUncacheableReason reasons =
            OwnUncacheableReasons(node, scope);
        foreach (PrismGraphEdge input in inputs)
        {
            PrismGraphNodePlan source = plans[input.Source];
            if (source.IsCacheable)
            {
                continue;
            }

            reasons |= source.UncacheableReasons |
                PrismGraphUncacheableReason.UncacheableInput;
        }
        return reasons;
    }

    private static PrismGraphUncacheableReason OwnUncacheableReasons(
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        PrismGraphUncacheableReason reasons = PrismGraphUncacheableReason.None;
        if (node.Kind == PrismGraphNodeKind.BackdropInput &&
            !node.Dependencies.Any(
                dependency =>
                    dependency.Kind ==
                        PrismGraphDependencyKind.BackdropFrame &&
                    dependency.Key > 0))
        {
            reasons |= PrismGraphUncacheableReason.FrameBackdrop;
        }

        if (!HasAvailableResourceVersions(node, scope))
        {
            reasons |= PrismGraphUncacheableReason.ResourceVersionUnavailable;
        }

        if (node.Filter is PrismFilterId filter)
        {
            reasons |= CatalogReasons((int)filter);
        }
        if (node.Style is PrismStyleId style)
        {
            reasons |= CatalogReasons((int)style);
        }
        if (!HasRequiredDependencies(node))
        {
            reasons |= PrismGraphUncacheableReason.MissingRequiredDependency;
        }

        return reasons;
    }

    private static bool HasAvailableResourceVersions(
        PrismGraphNode node,
        PrismGraphScope scope)
    {
        if (node.Resource is PrismResourceId resource &&
            resource.Value > 0 &&
            !HasVersionedResource(resource))
        {
            return false;
        }

        foreach (PrismGraphParameter parameter in
            node.Parameters)
        {
            if (parameter.Kind !=
                    PrismGraphParameterValueKind.Resource ||
                parameter.ResourceValue.Value <= 0)
            {
                continue;
            }
            if (!HasVersionedResource(
                    parameter.ResourceValue))
            {
                return false;
            }
        }

        return true;

        bool HasVersionedResource(
            PrismResourceId resourceId)
        {
            if (!scope.Resources.TryGetDependency(
                    resourceId,
                    out long identity,
                    out long version) ||
                identity <= 0 ||
                version <= 0)
            {
                return false;
            }

            return node.Dependencies.Any(
                dependency =>
                    dependency.Kind ==
                        PrismGraphDependencyKind.Resource &&
                    dependency.Key == identity &&
                    dependency.Version == version);
        }
    }

    private static PrismGraphUncacheableReason CatalogReasons(int stableId)
    {
        PrismCatalogEntryDescriptor entry = PrismCatalogRuntime.GetEntry(stableId);
        PrismGraphUncacheableReason reasons = PrismGraphUncacheableReason.None;
        if (!entry.Deterministic)
        {
            reasons |= PrismGraphUncacheableReason.NonDeterministicOperation;
        }
        if (!entry.Cacheable)
        {
            reasons |= PrismGraphUncacheableReason.CatalogDisallowsCaching;
        }
        return reasons;
    }

    private static bool HasRequiredDependencies(PrismGraphNode node)
    {
        bool Has(PrismGraphDependencyKind kind) =>
            node.Dependencies.Any(dependency => dependency.Kind == kind);

        bool requiresValues = node.Kind is not (
            PrismGraphNodeKind.ControlCapture or
            PrismGraphNodeKind.ColorConversion);
        if (!Has(PrismGraphDependencyKind.Structure) ||
            (requiresValues && !Has(PrismGraphDependencyKind.Values)) ||
            !Has(PrismGraphDependencyKind.Descendants))
        {
            return false;
        }

        return node.Kind switch
        {
            PrismGraphNodeKind.ControlCapture =>
                Has(PrismGraphDependencyKind.VisualContent) &&
                Has(PrismGraphDependencyKind.Bounds) &&
                Has(PrismGraphDependencyKind.PixelScale) &&
                Has(PrismGraphDependencyKind.Transform),
            PrismGraphNodeKind.BackdropInput =>
                Has(PrismGraphDependencyKind.BackdropFrame),
            PrismGraphNodeKind.BackdropCrop =>
                Has(PrismGraphDependencyKind.Bounds),
            PrismGraphNodeKind.ColorConversion =>
                Has(PrismGraphDependencyKind.ColorProfile),
            PrismGraphNodeKind.Filter or PrismGraphNodeKind.Style =>
                Has(PrismGraphDependencyKind.CatalogEntry) &&
                HasRequiredCatalogPolicy(node),
            _ => true
        };
    }

    private static bool HasRequiredCatalogPolicy(
        PrismGraphNode node)
    {
        int stableId = node.Filter is PrismFilterId filter
            ? (int)filter
            : node.Style is PrismStyleId style
                ? (int)style
                : throw new InvalidOperationException(
                    "A catalog cache policy requires a filter or style node.");
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry(stableId);
        const PrismCatalogCacheDependency known =
            PrismCatalogCacheDependency.InputPixels |
            PrismCatalogCacheDependency.ParameterValues |
            PrismCatalogCacheDependency.ExplicitSeed |
            PrismCatalogCacheDependency.VersionedResources;
        PrismCatalogCacheDependency policy =
            entry.CacheDependencies;
        if ((policy & ~known) != 0 ||
            (policy & PrismCatalogCacheDependency.InputPixels) == 0)
        {
            return false;
        }

        bool hasParameters = entry.Properties.Length > 0;
        bool hasSeed = entry.Properties.Any(
            property => string.Equals(
                property.Name,
                "Seed",
                StringComparison.Ordinal));
        bool hasResources = entry.Properties.Any(
            property =>
                property.ValueType ==
                PrismCatalogValueType.Resource);
        if (hasParameters != policy.HasFlag(
                PrismCatalogCacheDependency.ParameterValues) ||
            hasSeed != policy.HasFlag(
                PrismCatalogCacheDependency.ExplicitSeed) ||
            hasResources != policy.HasFlag(
                PrismCatalogCacheDependency.VersionedResources) ||
            node.Parameters.Length != entry.Properties.Length)
        {
            return false;
        }

        for (int index = 0; index < node.Parameters.Length; index++)
        {
            if (node.Parameters[index].Index != index)
            {
                return false;
            }
        }

        return true;
    }

    private static PrismRetainedCacheCandidateKind SelectCacheCandidate(
        PrismGraphNode node,
        PrismGraphScope scope,
        PrismGraphUncacheableReason reasons)
    {
        if (reasons != PrismGraphUncacheableReason.None)
        {
            return PrismRetainedCacheCandidateKind.None;
        }
        if (scope.Output == node.Id)
        {
            return PrismRetainedCacheCandidateKind.Final;
        }
        if (node.Kind == PrismGraphNodeKind.ControlCapture)
        {
            return PrismRetainedCacheCandidateKind.Capture;
        }
        if (node.Kind is
                PrismGraphNodeKind.Filter or
                PrismGraphNodeKind.Style or
                PrismGraphNodeKind.Mask or
                PrismGraphNodeKind.ColorConversion or
                PrismGraphNodeKind.BackdropCrop ||
            node.Kind == PrismGraphNodeKind.Group &&
            node.IsIsolationBoundary)
        {
            return PrismRetainedCacheCandidateKind.Intermediate;
        }

        return PrismRetainedCacheCandidateKind.None;
    }

}
