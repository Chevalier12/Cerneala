using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Prism.Runtime;
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
    // Advances with every frame change except a continuous-redraw tick.
    private long presentationVersion = 1;
    private long checkedPresentationVersion;
    private ImageResolutionStamp checkedPresentationStamp;
    private DrawRect checkedPresentationBounds;
    private bool checkedPresentationReady;
    private readonly ScenePresentationDependencies2D checkedPresentationDependencies = new();

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
        // A check visits every scene node. Its answer depends only on scene
        // content, camera, bounds, resource resolution and the unversioned
        // dependencies it consulted (Prism state, worker-replaced catalogs).
        // Input routing and recording reuse it until one of those changes.
        ImageResolutionStamp stamp = ImageResourceResolutionEpoch.Capture(Root);
        if (checkedPresentationVersion == presentationVersion &&
            stamp.IsSet && checkedPresentationStamp == stamp &&
            checkedPresentationBounds == bounds &&
            checkedPresentationDependencies.IsCurrent())
        {
            return checkedPresentationReady;
        }

        checkingPresentation = true;
        checkedPresentationVersion = 0;
        try
        {
            Matrix3x2 transform = GetSceneToFrameTransform(bounds);
            checkedPresentationDependencies.Clear();
            ScenePresentationContext2D context = new(bounds, transform, checkedPresentationDependencies);
            Scene?.CheckPresentation(context);
            SetPresentation(context.State, context.Error);
            bool ready = context.State == RenderSurface2DPresentationState.Ready;
            checkedPresentationVersion = presentationVersion;
            checkedPresentationStamp = stamp;
            checkedPresentationBounds = bounds;
            checkedPresentationReady = ready;
            return ready;
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

internal sealed class ScenePresentationContext2D(
    DrawRect surfaceBounds,
    Matrix3x2 sceneToSurface,
    ScenePresentationDependencies2D? dependencies = null)
{
    internal RenderSurface2DPresentationState State { get; private set; } = RenderSurface2DPresentationState.Ready;
    internal Exception? Error { get; private set; }

    // Records the state of a Prism instance whose operations the check consults.
    internal void ObservePrism(SceneNode2D node)
    {
        if (dependencies is not null && PrismAttachment.TryGetRenderState(node, out PrismInstance? instance, out _))
        {
            dependencies.ObservePrism(instance!);
        }
    }

    internal void ObservePrism(PrismInstance instance) => dependencies?.ObservePrism(instance);

    // Records the catalog a check compared against; a worker may replace it.
    internal void ObserveCatalog(TileMapSource2D source, TileMapCatalog2D catalog) =>
        dependencies?.ObserveCatalog(source, catalog);

    private void ObservePrismScope(SceneNode2D owner)
    {
        if (dependencies is null) { return; }
        for (UIElement? current = owner; current is SceneNode2D node; current = current.LogicalParent)
        {
            ObservePrism(node);
        }
    }

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

    // A check visits the children of one owner in turn and mutates nothing, so
    // the owner's frame transform and effect ancestry are derived once per owner.
    private SceneNode2D? cachedOwner;
    private Matrix3x2 cachedOwnerToSurface;
    private bool cachedOwnerHasWholeInput;

    internal SceneBounds2D GetVisibleBounds(SceneNode2D node)
    {
        if (surfaceBounds.Width <= 0 || surfaceBounds.Height <= 0) { return SceneBounds2D.Empty; }
        Matrix3x2 ownerToSurface = sceneToSurface;
        bool ownerHasWholeInput = false;
        if (node.LogicalParent is SceneNode2D owner)
        {
            if (!ReferenceEquals(owner, cachedOwner))
            {
                ObservePrismScope(owner);
                cachedOwner = owner;
                cachedOwnerToSurface = SceneGeometry2D.GetLocalToSceneTransform(owner) * sceneToSurface;
                cachedOwnerHasWholeInput = SceneSpatialInterest2D.HasWholeInputEffect(owner, includeAncestors: true);
            }
            ownerToSurface = cachedOwnerToSurface;
            ownerHasWholeInput = cachedOwnerHasWholeInput;
        }
        ObservePrism(node);
        SceneBounds2D viewport = SceneGeometry2D.TryTransformBoundsToLocal(surfaceBounds,
            node.GetLocalTransform() * ownerToSurface, out DrawRect local)
                ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
        return ownerHasWholeInput || SceneSpatialInterest2D.HasWholeInputEffect(node, includeAncestors: false)
            ? SceneSpatialInterest2D.ResolveInputBounds(node, viewport, surfaceBounds: surfaceBounds)
            : viewport;
    }

    // The viewport in an owner's local space, where its children's bounds lie.
    // Unknown when an enclosing effect can widen what its children must supply.
    internal SceneBounds2D GetChildVisibleBounds(SceneNode2D owner)
    {
        if (surfaceBounds.Width <= 0 || surfaceBounds.Height <= 0) { return SceneBounds2D.Empty; }
        ObservePrismScope(owner);
        if (SceneSpatialInterest2D.HasWholeInputEffect(owner, includeAncestors: true)) { return SceneBounds2D.Unknown; }
        return SceneGeometry2D.TryTransformBoundsToLocal(surfaceBounds,
            SceneGeometry2D.GetLocalToSceneTransform(owner) * sceneToSurface, out DrawRect local)
                ? SceneBounds2D.Known(local) : SceneBounds2D.Unknown;
    }

    internal static bool Intersects(SceneBounds2D visible, DrawRect content) =>
        visible.Kind != SceneBoundsKind.Empty && (visible.Kind == SceneBoundsKind.Unknown ||
            content.X <= visible.Bounds.Right && content.Right >= visible.Bounds.X &&
            content.Y <= visible.Bounds.Bottom && content.Bottom >= visible.Bounds.Y);
}

// State a presentation check consulted that changes without invalidating the
// scene: Prism instance state, and tile catalogs that workers may replace.
internal sealed class ScenePresentationDependencies2D
{
    private readonly List<(PrismInstance Instance, PrismStructuralVersion Structure, PrismValueVersion Values)> prisms = [];
    private readonly List<(TileMapSource2D Source, TileMapCatalog2D Catalog)> catalogs = [];

    internal void Clear()
    {
        prisms.Clear();
        catalogs.Clear();
    }

    internal bool IsCurrent()
    {
        foreach ((PrismInstance instance, PrismStructuralVersion structure, PrismValueVersion values) in prisms)
        {
            if (instance.StructuralVersion != structure || instance.ValueVersion != values) { return false; }
        }
        foreach ((TileMapSource2D source, TileMapCatalog2D catalog) in catalogs)
        {
            if (!ReferenceEquals(source.Catalog, catalog)) { return false; }
        }
        return true;
    }

    internal void ObservePrism(PrismInstance instance)
    {
        foreach ((PrismInstance observed, _, _) in prisms)
        {
            if (ReferenceEquals(observed, instance)) { return; }
        }
        prisms.Add((instance, instance.StructuralVersion, instance.ValueVersion));
    }

    internal void ObserveCatalog(TileMapSource2D source, TileMapCatalog2D catalog) => catalogs.Add((source, catalog));
}
