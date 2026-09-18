using Cerneala.Drawing;
using System.Numerics;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Elements;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.UI.Controls;

// Surface preparation and collision readiness coordinate materializers, not
// their storage or templates. Static maps do not contribute simulated actors.
internal interface ISceneSpatialParticipant2D
{
    SceneNode2D Node { get; }
    IReadOnlyList<SceneSpatialEntry2D>? SimulationCatalog { get; }
    Task Preparation { get; }
    Task GetCollisionPreparation(DrawRect sceneBounds) => Preparation;
    void UpdateSpatialInterest(SceneBounds2D visibleBounds, IReadOnlyList<SceneBounds2D> collisionInterest,
        DrawRect? surfaceBounds = null);
    string? GetUnpreparedCollisionEntry(DrawRect sceneBounds);
}

internal static class SceneSpatialInterest2D
{
    internal static SceneBounds2D ResolveInputBounds(SceneNode2D node, SceneBounds2D viewport,
        bool includeSelf = true, DrawRect? surfaceBounds = null) =>
        viewport.Kind == SceneBoundsKind.Empty ? viewport :
            TryGetExpandedInputBounds(node, viewport, includeSelf, surfaceBounds, out SceneBounds2D input) ? input : viewport;

    internal static bool TryGetExpandedInputBounds(SceneNode2D node, bool includeSelf, out SceneBounds2D input,
        DrawRect? surfaceBounds = null)
    {
        SceneBounds2D viewport = SceneBounds2D.Unknown;
        if (node.Surface is RenderSurface2D surface)
        {
            DrawRect bounds = surfaceBounds ?? surface.GetPresentationBounds();
            viewport = bounds.Width <= 0 || bounds.Height <= 0 ? SceneBounds2D.Empty :
                SceneGeometry2D.TryTransformBoundsToLocal(bounds,
                    SceneGeometry2D.GetLocalToSceneTransform(node) * surface.GetSceneToFrameTransform(bounds), out DrawRect local)
                    ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
        }
        if (TryGetExpandedInputBounds(node, viewport, includeSelf, surfaceBounds, out input)) { return true; }
        // A pointwise streamed composition still has a cropped physical input.
        // Preserve its full logical paint coordinates independently of that crop.
        input = viewport;
        return node.SimulationContext is { SpatialItems.Count: > 0 };
    }

    private static bool TryGetExpandedInputBounds(SceneNode2D node, SceneBounds2D viewport,
        bool includeSelf, DrawRect? surfaceBounds, out SceneBounds2D input)
    {
        SceneNode2D? first = includeSelf ? node : node.LogicalParent as SceneNode2D;
        bool found = false;
        input = SceneBounds2D.Unknown;
        // Preserve the ordinary no-effect/pointwise path: no geometry planning,
        // parameter snapshots or cache allocation is needed there.
        for (SceneNode2D? owner = first; owner is not null; owner = owner.LogicalParent as SceneNode2D)
        {
            if (PrismAttachment.TryGetRenderState(owner, out PrismInstance? instance, out _) &&
                PrismInputDependency.RequiresWholeInput(instance!)) { found = true; break; }
        }
        if (!found) { return false; }
        if (viewport.Kind == SceneBoundsKind.Empty) { input = viewport; return true; }
        Matrix3x2 nodeToScene = SceneGeometry2D.GetLocalToSceneTransform(node);
        bool sceneInvertible = Matrix3x2.Invert(nodeToScene, out Matrix3x2 sceneToNode);
        Matrix3x2 sceneToFrame = node.Surface is RenderSurface2D surface
            ? surface.GetSceneToFrameTransform(surfaceBounds ?? surface.GetPresentationBounds()) : Matrix3x2.Identity;
        bool frameInvertible = Matrix3x2.Invert(nodeToScene * sceneToFrame, out Matrix3x2 frameToNode);
        bool missingDomain = false;
        input = ApplyAncestorInput(first, node, viewport, sceneInvertible, sceneToNode,
            frameInvertible, frameToNode, ref missingDomain);
        return true;
    }

    private static SceneBounds2D ApplyAncestorInput(SceneNode2D? owner, SceneNode2D target, SceneBounds2D input,
        bool sceneInvertible, Matrix3x2 sceneToTarget, bool frameInvertible, Matrix3x2 frameToTarget, ref bool missingDomain)
    {
        if (owner is null) { return input; }
        // Work backwards from displayed output through outer and then inner
        // compositions. Keep domains in target-local coordinates so a rotated
        // declaration is not inflated by a frame-space AABB round trip.
        input = ApplyAncestorInput(owner.LogicalParent as SceneNode2D, target, input,
            sceneInvertible, sceneToTarget, frameInvertible, frameToTarget, ref missingDomain);
        if (missingDomain) { return SceneBounds2D.Empty; }
        if (!PrismAttachment.TryGetRenderState(owner, out PrismInstance? instance, out _) ||
            !PrismInputDependency.RequiresWholeInput(instance!)) { return input; }
        bool streamed = target.SimulationContext is { SpatialItems.Count: > 0 };
        if (owner.PrismInputDomain is DrawRect domain)
        {
            return ReferenceEquals(owner, target) ? SceneBounds2D.Known(domain) : sceneInvertible
                ? SceneGeometry2D.TransformBounds(SceneBounds2D.Known(domain),
                    SceneGeometry2D.GetLocalToSceneTransform(owner) * sceneToTarget) : SceneBounds2D.Unknown;
        }
        if (!streamed) { return SceneBounds2D.Unknown; }
        if (!owner.TryGetLocalPrismInputOutset(instance!, out Vector2 support))
        {
            missingDomain = true;
            return SceneBounds2D.Empty;
        }
        if (!frameInvertible || input.Kind != SceneBoundsKind.Known) { return SceneBounds2D.Unknown; }
        // Kernel support is in frame raster axes, not rotated scene axes.
        float x = MathF.Abs(frameToTarget.M11) * support.X + MathF.Abs(frameToTarget.M21) * support.Y;
        float y = MathF.Abs(frameToTarget.M12) * support.X + MathF.Abs(frameToTarget.M22) * support.Y;
        DrawRect bounds = input.Bounds;
        return SceneBounds2D.Known(new(bounds.X - x, bounds.Y - y, bounds.Width + 2 * x, bounds.Height + 2 * y));
    }
}
