using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Drawing.Prism.Graph;

// Classifies source-pixel dependencies, not resource residency or capture bounds.
// A pointwise composition may omit offscreen source commands while retaining its
// original coordinate domain. Operations without a supported finite footprint
// need an explicit input domain in streamed scenes.
internal static class PrismInputDependency
{
    // Owned by one scene node, not a process-wide residency cache. A removed
    // attachment must not remain alive solely because it was once planned.
    internal sealed class LocalInputCache
    {
        private WeakReference<PrismInstance>? previous;
        private PrismStructuralVersion structure;
        private PrismValueVersion values;
        private float scale;
        private Matrix3x2 transform;
        private Vector2 outset;
        private bool local;

        internal bool TryGet(PrismInstance instance, float pixelScale, Matrix3x2 effectiveTransform, out Vector2 result)
        {
            if (previous is null || !previous.TryGetTarget(out PrismInstance? target) || !ReferenceEquals(target, instance) ||
                structure != instance.StructuralVersion || values != instance.ValueVersion || scale != pixelScale || transform != effectiveTransform)
            {
                local = TryGetLocalInputOutset(instance, pixelScale, effectiveTransform, out outset);
                if (previous is null) { previous = new(instance); } else { previous.SetTarget(instance); }
                structure = instance.StructuralVersion;
                values = instance.ValueVersion;
                scale = pixelScale;
                transform = effectiveTransform;
            }
            result = outset;
            return local;
        }
    }

    internal static bool TryGetLocalInputOutset(PrismInstance instance, float pixelScale,
        Matrix3x2 transform, out Vector2 outset)
    {
        outset = default;
        var definitions = instance.Definition.Nodes;
        for (int index = definitions.Length - 1; index >= 0; index--)
        {
            if (!TryGetNodeOutset(instance.GetNodeState(definitions[index].Id), outset, pixelScale, transform,
                out Vector2 node)) { return false; }
            outset = Vector2.Max(outset, node);
        }
        return true;
    }

    private static bool TryGetNodeOutset(PrismNodeState node, Vector2 background, float scale,
        Matrix3x2 transform, out Vector2 outset)
    {
        outset = default;
        switch (node)
        {
            case PrismLayerState layer:
                if (!layer.Visible || layer.Opacity <= 0) { return true; }
                return TryGetOperationOutset(layer.Filters, layer.Styles, scale, transform, out outset);
            case PrismGroupState group:
                if (!group.Visible || group.Opacity <= 0) { return true; }
                outset = group.BlendMode == PrismBlendMode.PassThrough ? background : default;
                for (int index = group.Children.Count - 1; index >= 0; index--)
                {
                    if (!TryGetNodeOutset(group.Children[index], outset, scale, transform, out Vector2 child)) { return false; }
                    outset = Vector2.Max(outset, child);
                }
                if (!TryGetOperationOutset(group.Filters, group.Styles, scale, transform, out Vector2 operations)) { return false; }
                outset += operations;
                return true;
            default: return false;
        }
    }

    private static bool TryGetOperationOutset(IReadOnlyList<PrismFilterState> filters,
        IReadOnlyList<PrismStyleState> styles, float scale, Matrix3x2 transform, out Vector2 outset)
    {
        outset = default;
        // Image-mask extraction and feathering sample the mask resource, not
        // neighboring scene pixels. Its acquisition remains owned by Prism.
        for (int index = 0; index < styles.Count; index++)
        {
            if (styles[index].Visible && !IsPointwise(styles[index])) { return false; }
        }
        for (int index = 0; index < filters.Count; index++)
        {
            PrismFilterState filter = filters[index];
            if (!filter.Visible || filter.Opacity <= 0 || IsPointwise(filter)) { continue; }
            if (!PrismNeighborhoodPlanner.TryGetInputOutset(filter, scale, transform, out Vector2 support)) { return false; }
            outset += support;
        }
        return true;
    }

    internal static bool RequiresWholeInput(PrismInstance instance)
    {
        var definitions = instance.Definition.Nodes;
        for (int index = 0; index < definitions.Length; index++)
        {
            if (RequiresInput(instance.GetNodeState(definitions[index].Id))) { return true; }
        }
        return false;
    }

    private static bool RequiresInput(PrismNodeState node)
    {
        switch (node)
        {
            case PrismLayerState layer:
                return layer.Visible && layer.Opacity > 0 &&
                    RequiresInput(layer.Filters, layer.Styles);
            case PrismGroupState group:
                if (!group.Visible || group.Opacity <= 0) { return false; }
                if (RequiresInput(group.Filters, group.Styles)) { return true; }
                for (int index = 0; index < group.Children.Count; index++)
                {
                    if (RequiresInput(group.Children[index])) { return true; }
                }
                return false;
            default:
                return true;
        }
    }

    private static bool RequiresInput(IReadOnlyList<PrismFilterState> filters,
        IReadOnlyList<PrismStyleState> styles)
    {
        for (int index = 0; index < styles.Count; index++)
        {
            if (styles[index].Visible && !IsPointwise(styles[index])) { return true; }
        }
        for (int index = 0; index < filters.Count; index++)
        {
            PrismFilterState filter = filters[index];
            if (!filter.Visible || filter.Opacity <= 0) { continue; }
            // Levels.Auto and Threshold read an input-wide distribution. LUTs
            // in Curves/ColorLookup/GradientMap instead index by this pixel's
            // color; coordinate-based dithering keeps the original capture basis.
            if (!IsPointwise(filter)) { return true; }
        }
        return false;
    }

    private static bool IsPointwise(PrismStyleState style) => style.Style is
        PrismStyleId.ColorOverlay or PrismStyleId.GradientOverlay or PrismStyleId.PatternOverlay;

    private static bool IsPointwise(PrismFilterState filter) => filter.Filter switch
    {
        PrismFilterId.Levels => !filter.GetValue(PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey),
        PrismFilterId.BrightnessContrast or PrismFilterId.Curves or PrismFilterId.Exposure or
        PrismFilterId.Vibrance or PrismFilterId.HueSaturation or PrismFilterId.ColorBalance or
        PrismFilterId.BlackWhite or PrismFilterId.PhotoFilter or PrismFilterId.ChannelMixer or
        PrismFilterId.ColorLookup or PrismFilterId.Invert or PrismFilterId.Posterize or
        PrismFilterId.GradientMap or PrismFilterId.SelectiveColor => true,
        _ => false
    };
}
