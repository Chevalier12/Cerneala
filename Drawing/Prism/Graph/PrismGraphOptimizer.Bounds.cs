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
    private static BoundsCalculation CalculateBounds(
        PrismGraphNode node,
        PrismGraphScope scope,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans)
    {
        if (node.Kind == PrismGraphNodeKind.Composite &&
            inputs.Any(
                input => input.Kind == PrismGraphEdgeKind.MaskAlpha))
        {
            return BoundsFromContentInput(
                node,
                inputs,
                plans);
        }
        if (node.Kind == PrismGraphNodeKind.ClipToBelow)
        {
            return BoundsFromContentInput(
                node,
                inputs,
                plans);
        }

        DrawRect bounds = scope.Bounds;
        PrismGraphBoundsStatus status = PrismGraphBoundsStatus.Exact;
        bool hasInput = false;
        foreach (PrismGraphEdge input in inputs)
        {
            if (!plans.TryGetValue(input.Source, out PrismGraphNodePlan sourcePlan))
            {
                throw new InvalidOperationException(
                    $"Prism graph node '{node.Id}' is not in topological order.");
            }

            bounds = hasInput
                ? Union(bounds, sourcePlan.Bounds)
                : sourcePlan.Bounds;
            status = WorstBoundsStatus(status, sourcePlan.BoundsStatus);
            hasInput = true;
        }

        return node.Kind switch
        {
            PrismGraphNodeKind.Filter => ExpandFilterBounds(node, bounds, status),
            PrismGraphNodeKind.Style => ExpandStyleBounds(node, scope, bounds, status),
            PrismGraphNodeKind.Mask =>
                ExpandMaskBounds(node, scope, bounds, status),
            PrismGraphNodeKind.PassThroughComposite =>
                ConservativeBounds(bounds, status),
            _ => new BoundsCalculation(bounds, status)
        };
    }

    private static BoundsCalculation BoundsFromContentInput(
        PrismGraphNode node,
        ImmutableArray<PrismGraphEdge> inputs,
        IReadOnlyDictionary<PrismGraphNodeId, PrismGraphNodePlan> plans)
    {
        PrismGraphEdge content = inputs.FirstOrDefault(
            input => input.Kind == PrismGraphEdgeKind.Content);
        if (content == default ||
            !plans.TryGetValue(
                content.Source,
                out PrismGraphNodePlan contentPlan))
        {
            throw new InvalidOperationException(
                $"Prism graph node '{node.Id}' has no content bounds.");
        }

        PrismGraphBoundsStatus status = contentPlan.BoundsStatus;
        foreach (PrismGraphEdge input in inputs)
        {
            status = WorstBoundsStatus(
                status,
                plans[input.Source].BoundsStatus);
        }
        return ConservativeBounds(contentPlan.Bounds, status);
    }

    private static BoundsCalculation ExpandMaskBounds(
        PrismGraphNode node,
        PrismGraphScope scope,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        if (node.MaskPass is not (
            PrismMaskPass.FeatherHorizontal or
            PrismMaskPass.FeatherVertical))
        {
            return new BoundsCalculation(bounds, inputStatus);
        }

        float feather = node.Feather ?? 0;
        float transformScale = MathF.Max(
            MathF.Sqrt(
                (scope.EffectiveTransform.M11 *
                    scope.EffectiveTransform.M11) +
                (scope.EffectiveTransform.M12 *
                    scope.EffectiveTransform.M12)),
            MathF.Sqrt(
                (scope.EffectiveTransform.M21 *
                    scope.EffectiveTransform.M21) +
                (scope.EffectiveTransform.M22 *
                    scope.EffectiveTransform.M22)));
        float support = checked(feather * transformScale);
        DrawRect expanded =
            node.MaskPass == PrismMaskPass.FeatherHorizontal
                ? Inflate(bounds, support, 0)
                : Inflate(bounds, 0, support);
        return ConservativeBounds(expanded, inputStatus);
    }

    private static BoundsCalculation ExpandFilterBounds(
        PrismGraphNode node,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        if (node.ResamplingPlan is PrismResamplingPlan resamplingPlan)
        {
            if (resamplingPlan.BoundsOutset != Vector2.Zero &&
                node.ResamplingPassIndex == 0)
            {
                return ConservativeBounds(
                    Inflate(
                        bounds,
                        resamplingPlan.BoundsOutset.X,
                        resamplingPlan.BoundsOutset.Y),
                    inputStatus);
            }
            return resamplingPlan.TransformsBounds
                ? ExpandTransformBounds(
                    node,
                    resamplingPlan,
                    bounds,
                    inputStatus)
                : new BoundsCalculation(bounds, inputStatus);
        }
        if (node.CatalogFilterPlan is PrismCatalogFilterPlan)
        {
            PrismCatalogFilterPass pass =
                GetCatalogFilterPass(node);
            if (pass.BoundsRadiusX == 0 &&
                pass.BoundsRadiusY == 0)
            {
                return new BoundsCalculation(
                    bounds,
                    inputStatus);
            }

            return ConservativeBounds(
                Inflate(
                    bounds,
                    pass.BoundsRadiusX,
                    pass.BoundsRadiusY),
                inputStatus);
        }
        if (node.Filter is PrismFilterId filter &&
            string.Equals(
                PrismCatalogRuntime.GetEntry((int)filter)
                    .Execution?.Bounds,
                "source",
                StringComparison.Ordinal))
        {
            return new BoundsCalculation(bounds, inputStatus);
        }
        if (node.NeighborhoodPlan is PrismNeighborhoodPlan)
        {
            PrismNeighborhoodPass pass =
                GetNeighborhoodPass(node);
            if (pass.BoundsRadiusX == 0 &&
                pass.BoundsRadiusY == 0)
            {
                return new BoundsCalculation(
                    bounds,
                    inputStatus);
            }

            return ConservativeBounds(
                Inflate(
                    bounds,
                    pass.BoundsRadiusX,
                    pass.BoundsRadiusY),
                inputStatus);
        }

        return new BoundsCalculation(
            bounds,
            PrismGraphBoundsStatus.Unknown);
    }

    private static BoundsCalculation ExpandStyleBounds(
        PrismGraphNode node,
        PrismGraphScope scope,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        if (node.Style is null)
        {
            return new BoundsCalculation(
                bounds,
                PrismGraphBoundsStatus.Unknown);
        }

        PrismStylePlan plan =
            PrismStylePlanner.Create(node, scope);
        DrawRect expanded =
            PrismStylePlanner.ExpandBounds(plan, scope, bounds);
        return expanded == bounds
            ? new BoundsCalculation(bounds, inputStatus)
            : ConservativeBounds(expanded, inputStatus);
    }

    private static BoundsCalculation ConservativeBounds(
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus) =>
        new(
            bounds,
            WorstBoundsStatus(
                inputStatus,
                PrismGraphBoundsStatus.Conservative));

    private static PrismGraphBoundsStatus WorstBoundsStatus(
        PrismGraphBoundsStatus left,
        PrismGraphBoundsStatus right)
    {
        if (left == PrismGraphBoundsStatus.Unknown ||
            right == PrismGraphBoundsStatus.Unknown)
        {
            return PrismGraphBoundsStatus.Unknown;
        }
        return left == PrismGraphBoundsStatus.Conservative ||
            right == PrismGraphBoundsStatus.Conservative
            ? PrismGraphBoundsStatus.Conservative
            : PrismGraphBoundsStatus.Exact;
    }

    private static DrawRect TransformBounds(
        PrismResamplingPlan plan,
        DrawRect bounds)
    {
        Vector2 pivot = new(
            bounds.X + (bounds.Width * plan.BoundsOrigin.X),
            bounds.Y + (bounds.Height * plan.BoundsOrigin.Y));
        Matrix3x2 skewMatrix = new(
            1f,
            plan.BoundsSkew.Y,
            plan.BoundsSkew.X,
            1f,
            0f,
            0f);
        Matrix3x2 transform =
            Matrix3x2.CreateTranslation(-pivot) *
            Matrix3x2.CreateScale(plan.BoundsScale) *
            skewMatrix *
            Matrix3x2.CreateRotation(plan.BoundsRotation) *
            Matrix3x2.CreateTranslation(
                pivot + plan.BoundsTranslation);
        return Transform(bounds, transform);
    }

    private static BoundsCalculation ExpandTransformBounds(
        PrismGraphNode node,
        PrismResamplingPlan plan,
        DrawRect bounds,
        PrismGraphBoundsStatus inputStatus)
    {
        DrawRect transformed = TransformBounds(plan, bounds);
        if (node.Amount == 1f &&
            node.BlendMode == PrismBlendMode.Normal)
        {
            return new BoundsCalculation(transformed, inputStatus);
        }

        return ConservativeBounds(Union(bounds, transformed), inputStatus);
    }

    private static DrawRect Inflate(
        DrawRect bounds,
        float horizontal,
        float vertical)
    {
        if (!float.IsFinite(horizontal) ||
            !float.IsFinite(vertical) ||
            horizontal < 0 ||
            vertical < 0)
        {
            throw new InvalidOperationException(
                "Prism bounds expansion must be finite and non-negative.");
        }
        return CreateBounds(
            bounds.X - horizontal,
            bounds.Y - vertical,
            bounds.Right + horizontal,
            bounds.Bottom + vertical);
    }

    private static DrawRect Union(DrawRect left, DrawRect right)
    {
        return CreateBounds(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y),
            MathF.Max(left.Right, right.Right),
            MathF.Max(left.Bottom, right.Bottom));
    }

    private static DrawRect Transform(DrawRect bounds, Matrix3x2 transform)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), transform);
        Vector2 topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Y), transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Bottom), transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transform);
        return CreateBounds(
            MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X)),
            MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y)),
            MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X)),
            MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y)));
    }

    private static DrawRect CreateBounds(float left, float top, float right, float bottom)
    {
        try
        {
            return new DrawRect(
                left,
                top,
                MathF.Max(0, right - left),
                MathF.Max(0, bottom - top));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new InvalidOperationException(
                "A Prism operation produced non-finite or unsupported expanded bounds.",
                exception);
        }
    }

    private readonly record struct BoundsCalculation(
        DrawRect Bounds,
        PrismGraphBoundsStatus Status);

}
