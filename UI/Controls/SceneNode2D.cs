using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public abstract class SceneNode2D : UIElement, IInputSubtreeHost, IInputCoordinateSpace
{
    public static readonly UiProperty<int> LayerProperty =
        UiProperty<int>.Register(
            nameof(Layer),
            typeof(SceneNode2D),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsRender));

    internal RenderSurface2D? Surface { get; private set; }

    // Scene nodes are rendered through their logical owner, not the layout tree.
    internal override UIElement? PrismVisualParent => base.PrismVisualParent ?? LogicalParent;

    internal int ActiveAnimationIndex { get; set; } = -1;

    internal virtual bool HasActiveAnimation => false;

    internal virtual bool AdvanceAnimation(TimeSpan frameTime) => false;

    internal void RefreshAnimationRegistration() => Surface?.RefreshAnimationRegistration(this);

    public int Layer
    {
        get => GetValue(LayerProperty);
        set => SetValue(LayerProperty, value);
    }

    internal virtual void AttachSurface(RenderSurface2D? surface)
    {
        Surface?.RemoveAnimationRegistration(this);
        Surface = surface;
        RefreshAnimationRegistration();
    }

    internal abstract void Record(Scene2DRecordContext context);

    internal virtual void ReleaseRenderCaches()
    {
    }

    internal SceneBounds2D GetLocalBounds()
    {
        return UIElementVisibility.ParticipatesInRendering(this)
            ? GetVisibleLocalBounds()
            : SceneBounds2D.Empty;
    }

    internal virtual System.Numerics.Matrix3x2 GetLocalTransform() =>
        System.Numerics.Matrix3x2.Identity;

    internal abstract SceneBounds2D GetVisibleLocalBounds();

    internal virtual SceneBounds2D GetHitTestLocalBounds() => SceneBounds2D.Empty;

    internal virtual bool ParticipatesInInputRoute => true;

    IEnumerable<UIElement> IInputSubtreeHost.GetInputSubtreeChildren() =>
        LogicalChildren
            .OfType<SceneNode2D>()
            .Where(static child => child.ParticipatesInInputRoute);

    LayoutRect IInputCoordinateSpace.GetRootBounds()
    {
        if (Surface is null)
        {
            return default;
        }

        SceneBounds2D bounds = SceneGeometry2D.GetInputBounds(
            this,
            SceneGeometry2D.GetLocalToSceneTransform(this) * Surface.GetSceneToRootTransform());
        return bounds.Kind == SceneBoundsKind.Known
            ? new LayoutRect(bounds.Bounds.X, bounds.Bounds.Y, bounds.Bounds.Width, bounds.Bounds.Height)
            : default;
    }

    bool IInputCoordinateSpace.TryRootToLocal(
        Vector2 rootPosition,
        out Vector2 localPosition)
    {
        if (Surface is null ||
            !Surface.TryRootToScene(rootPosition, out Vector2 scenePosition) ||
            !SceneGeometry2D.TryTransformToLocal(
                new DrawPoint(scenePosition.X, scenePosition.Y),
                SceneGeometry2D.GetLocalToSceneTransform(this),
                out DrawPoint localPoint))
        {
            localPosition = default;
            return false;
        }

        localPosition = new Vector2(localPoint.X, localPoint.Y);
        return true;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        ProcessPendingAspect();
        RefreshAnimationRegistration();
    }

    protected override void OnDetached()
    {
        Surface?.RemoveAnimationRegistration(this);
        base.OnDetached();
    }

    public override void Invalidate(InvalidationRequest request)
    {
        base.Invalidate(request);
        ProcessPendingAspect();
        Surface?.InvalidateFrame();
    }

    private void ProcessPendingAspect()
    {
        if (Root is not UIRoot root ||
            !DirtyState.Has(InvalidationFlags.Aspect))
        {
            return;
        }

        root.AspectProcessor.Process(this);
        root.AspectQueue.Remove(this);
        DirtyState.Clear(InvalidationFlags.Aspect);
    }
}
