using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public enum RenderSurface2DPresentationState
{
    Loading,
    Ready,
    Error
}

public partial class RenderSurface2D
{
    private static readonly UiPropertyKey<RenderSurface2DPresentationState> PresentationStatePropertyKey =
        UiProperty<RenderSurface2DPresentationState>.RegisterReadOnly(nameof(PresentationState), typeof(RenderSurface2D),
            new UiPropertyMetadata<RenderSurface2DPresentationState>(RenderSurface2DPresentationState.Ready));

    public static readonly UiProperty<RenderSurface2DPresentationState> PresentationStateProperty =
        PresentationStatePropertyKey.Property;

    private static readonly UiPropertyKey<Exception?> PresentationErrorPropertyKey =
        UiProperty<Exception?>.RegisterReadOnly(nameof(PresentationError), typeof(RenderSurface2D),
            new UiPropertyMetadata<Exception?>(null));

    public static readonly UiProperty<Exception?> PresentationErrorProperty = PresentationErrorPropertyKey.Property;

    private bool checkingPresentation;

    public RenderSurface2DPresentationState PresentationState => GetValue(PresentationStateProperty);
    public Exception? PresentationError => GetValue(PresentationErrorProperty);

    internal bool CanRouteSceneInput => !checkingPresentation && CheckPresentation(GetPresentationBounds());

    internal DrawRect GetPresentationBounds(float? width = null, float? height = null)
    {
        float logicalWidth = width ?? ArrangedBounds.Width, logicalHeight = height ?? ArrangedBounds.Height;
        if (logicalWidth <= 0 || logicalHeight <= 0) { return default; }
        (int pixelWidth, int pixelHeight) = RenderSurface2DGeometry.GetPixelSize(logicalWidth, logicalHeight, Root?.Scale ?? 1);
        return new(0, 0, pixelWidth, pixelHeight);
    }

    internal Matrix3x2 GetSceneToFrameTransform(DrawRect bounds) => ViewBox is DrawRect viewBox
        ? CreateViewBoxTransform(viewBox, bounds, Stretch) : Matrix3x2.Identity;

    private bool CheckPresentation(DrawRect bounds)
    {
        if (checkingPresentation) { return false; }
        checkingPresentation = true;
        try
        {
            Matrix3x2 transform = GetSceneToFrameTransform(bounds);
            ScenePresentationContext2D context = new(bounds, transform);
            Scene?.CheckPresentation(context);
            SetPresentation(context.State, context.Error);
            return context.State == RenderSurface2DPresentationState.Ready;
        }
        finally { checkingPresentation = false; }
    }

    private void SetPresentation(RenderSurface2DPresentationState state, Exception? error = null)
    {
        bool changed = PresentationState != state || !ReferenceEquals(PresentationError, error);
        if (!changed) { return; }
        SetValue(PresentationErrorPropertyKey, error);
        SetValue(PresentationStatePropertyKey, state);
        // Input maps also contain previously focused/captured scene nodes.
        Root?.InputCache.Invalidate("Scene presentation readiness changed");
        AdvanceFrameVersion();
        Invalidate(InvalidationFlags.Render, "Scene presentation readiness changed");
    }
}

internal sealed class ScenePresentationContext2D(DrawRect surfaceBounds, Matrix3x2 sceneToSurface)
{
    internal RenderSurface2DPresentationState State { get; private set; } = RenderSurface2DPresentationState.Ready;
    internal Exception? Error { get; private set; }

    internal void RequireImage(ImageResourceResolution image)
    {
        if (image.IsPending || image.Error is not null) { Require(image.Error); }
    }

    internal void Require(Exception? error = null)
    {
        if (error is not null)
        {
            Error ??= error;
            State = RenderSurface2DPresentationState.Error;
        }
        else if (State == RenderSurface2DPresentationState.Ready)
        {
            State = RenderSurface2DPresentationState.Loading;
        }
    }

    internal SceneBounds2D GetVisibleBounds(SceneNode2D node)
    {
        if (surfaceBounds.Width <= 0 || surfaceBounds.Height <= 0) { return SceneBounds2D.Empty; }
        SceneBounds2D viewport = SceneGeometry2D.TryTransformBoundsToLocal(surfaceBounds,
            SceneGeometry2D.GetLocalToSceneTransform(node) * sceneToSurface, out DrawRect local)
                ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
        return SceneSpatialInterest2D.ResolveInputBounds(node, viewport, surfaceBounds: surfaceBounds);
    }

    internal static bool Intersects(SceneBounds2D visible, DrawRect content) =>
        visible.Kind != SceneBoundsKind.Empty && (visible.Kind == SceneBoundsKind.Unknown ||
            content.X <= visible.Bounds.Right && content.Right >= visible.Bounds.X &&
            content.Y <= visible.Bounds.Bottom && content.Bottom >= visible.Bounds.Y);
}
