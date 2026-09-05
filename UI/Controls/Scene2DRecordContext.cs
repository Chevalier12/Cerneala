using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal readonly struct Scene2DRecordContext
{
    internal Scene2DRecordContext(
        RenderSurface2D surface,
        RenderSurface2DFrame frame,
        Matrix3x2 sceneToSurfaceTransform,
        DrawRect visibleSurfaceBounds)
        : this(
            surface,
            frame,
            Matrix3x2.Identity,
            sceneToSurfaceTransform,
            visibleSurfaceBounds,
            sourceIndex: 0)
    {
    }

    private Scene2DRecordContext(
        RenderSurface2D surface,
        RenderSurface2DFrame frame,
        Matrix3x2 localToSceneTransform,
        Matrix3x2 localToSurfaceTransform,
        DrawRect visibleSurfaceBounds,
        int sourceIndex)
    {
        Surface = surface ?? throw new ArgumentNullException(nameof(surface));
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        LocalToSceneTransform = localToSceneTransform;
        LocalToSurfaceTransform = localToSurfaceTransform;
        VisibleSurfaceBounds = visibleSurfaceBounds;
        SourceIndex = sourceIndex;
    }

    internal RenderSurface2D Surface { get; }

    internal RenderSurface2DFrame Frame { get; }

    internal Matrix3x2 LocalToSceneTransform { get; }

    internal Matrix3x2 LocalToSurfaceTransform { get; }

    internal DrawRect VisibleSurfaceBounds { get; }

    internal int SourceIndex { get; }

    internal Scene2DRecordContext WithLocalTransform(Matrix3x2 localTransform) =>
        new(
            Surface,
            Frame,
            localTransform * LocalToSceneTransform,
            localTransform * LocalToSurfaceTransform,
            VisibleSurfaceBounds,
            SourceIndex);

    internal Scene2DRecordContext WithSourceIndex(int sourceIndex) =>
        new(
            Surface,
            Frame,
            LocalToSceneTransform,
            LocalToSurfaceTransform,
            VisibleSurfaceBounds,
            sourceIndex);

    internal ScenePrismScope BeginPrism(
        SceneNode2D owner,
        SceneBounds2D localBounds)
    {
        ArgumentNullException.ThrowIfNull(owner);
        DrawRect prismBounds = ResolveConservativeLocalBounds(localBounds);
        return new ScenePrismScope(
            Frame,
            Frame.BeginPrism(owner, prismBounds));
    }

    internal bool HasPrism(SceneNode2D owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return Frame.HasPrism(owner);
    }

    internal SceneBounds2D GetConservativeVisibleLocalBounds()
    {
        if (VisibleSurfaceBounds.Width <= 0 || VisibleSurfaceBounds.Height <= 0)
        {
            return SceneBounds2D.Empty;
        }

        return SceneGeometry2D.TryTransformBoundsToLocal(
            VisibleSurfaceBounds,
            LocalToSurfaceTransform,
            out DrawRect visibleLocalBounds)
                ? SceneBounds2D.Known(visibleLocalBounds)
                : SceneBounds2D.Unknown;
    }

    internal bool IntersectsVisibleLocalBounds(SceneBounds2D localBounds)
    {
        SceneBounds2D visible = GetConservativeVisibleLocalBounds();
        if (localBounds.Kind == SceneBoundsKind.Empty ||
            visible.Kind == SceneBoundsKind.Empty)
        {
            return false;
        }
        if (localBounds.Kind == SceneBoundsKind.Unknown ||
            visible.Kind == SceneBoundsKind.Unknown)
        {
            return true;
        }

        DrawRect content = localBounds.Bounds;
        DrawRect viewport = visible.Bounds;
        return content.X <= viewport.Right &&
            content.Right >= viewport.X &&
            content.Y <= viewport.Bottom &&
            content.Bottom >= viewport.Y;
    }

    private DrawRect ResolveConservativeLocalBounds(SceneBounds2D localBounds)
    {
        if (localBounds.Kind == SceneBoundsKind.Known)
        {
            return localBounds.Bounds;
        }

        if (localBounds.Kind == SceneBoundsKind.Empty)
        {
            return default;
        }

        SceneBounds2D visible = GetConservativeVisibleLocalBounds();
        return visible.Kind == SceneBoundsKind.Known
            ? visible.Bounds
            : Frame.Bounds;
    }
}

internal readonly struct ScenePrismScope : IDisposable
{
    private readonly RenderSurface2DFrame? frame;
    private readonly bool active;

    internal ScenePrismScope(RenderSurface2DFrame frame, bool active)
    {
        this.frame = frame;
        this.active = active;
    }

    public void Dispose()
    {
        if (active)
        {
            frame!.EndPrism();
        }
    }
}
