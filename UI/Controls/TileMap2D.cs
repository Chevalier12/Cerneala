using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Model))]
public sealed partial class TileMap2D : SceneNode2D
{
    public static readonly UiProperty<TileMap2DModel?> ModelProperty =
        UiProperty<TileMap2DModel?>.Register(
            nameof(Model),
            typeof(TileMap2D),
            new UiPropertyMetadata<TileMap2DModel?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> OffsetProperty =
        UiProperty<DrawPoint>.Register(
            nameof(Offset),
            typeof(TileMap2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Color> TintProperty =
        UiProperty<Color>.Register(
            nameof(Tint),
            typeof(TileMap2D),
            new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(TileMap2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    private readonly Dictionary<TileImageKey, ResolvedAtlas> resolvedAtlases = [];
    private readonly Dictionary<string, DrawSize> resolvedAtlasSizes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DrawSize> validatedAtlasSizes = new(StringComparer.Ordinal);
    private readonly Dictionary<ImageReference, DrawSize> placementImageSizes = [];
    private TileMap2DModel? validatedAtlasModel;
    private int tileInvalidations;
    private TileMap2DDiagnosticsSnapshot diagnostics;

    public TileMap2DModel? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public DrawPoint TransformOrigin
    {
        get => GetValue(TransformOriginProperty);
        set => SetValue(TransformOriginProperty, value);
    }

    public DrawPoint Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public Color Tint
    {
        get => GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ModelProperty))
        {
            if (args is UiPropertyChangedEventArgs<TileMap2DModel?> change &&
                change.OldValue is not null)
            {
                tileInvalidations++;
            }
            SynchronizeCollisionAdaptersAndNotify();
        }
        else if (SceneGeometry2D.IsSceneTransformProperty(args.Property) ||
                 ReferenceEquals(args.Property, TransformOriginProperty) ||
                 ReferenceEquals(args.Property, OffsetProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
                this,
                SceneCollisionMutationKind.Geometry);
        }
        else if (ReferenceEquals(args.Property, UIElement.IsVisibleProperty) ||
                 ReferenceEquals(args.Property, UIElement.VisibilityProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(
                this,
                SceneCollisionMutationKind.Participation);
        }
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        if (!ReferenceEquals(Surface, surface))
        {
            ReleaseRenderCaches();
        }
        base.AttachSurface(surface);
        foreach (TileStaticCollider2D collider in LogicalChildren.OfType<TileStaticCollider2D>())
        {
            collider.AttachSurface(surface);
        }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        TileMap2DModel? model = Model;
        if (model is null)
        {
            ReleaseRenderCaches();
            diagnostics = default;
            return;
        }
        if (!UIElementVisibility.ParticipatesInRendering(this) ||
            !model.IsVisible || Opacity <= 0 || model.Opacity <= 0)
        {
            diagnostics = default;
            return;
        }

        ResolveAtlases(model);
        Matrix3x2 localTransform = GetLocalTransform();
        bool hasTransform = localTransform != Matrix3x2.Identity;
        float opacity = Opacity * model.Opacity;
        bool hasOpacity = opacity < 1;
        if (hasTransform)
        {
            context.Frame.PushTransform(localTransform);
        }
        if (hasOpacity)
        {
            context.Frame.PushOpacity(opacity);
        }

        diagnostics = new TileMap2DDiagnosticsSnapshot(
            TotalChunks: model.IsFreePlacement ? (model.Tiles.Count + PlacementChunkSize - 1) / PlacementChunkSize : model.Chunks.Count,
            CandidateChunks: 0,
            VisibleChunks: 0,
            CandidateTiles: 0,
            DrawnTiles: 0,
            BatchesBuilt: 0,
            BatchesRebuilt: 0,
            BatchesReused: 0,
            DrawCommands: 0,
            RetainedBytes: 0,
            RetainedObjects: 0,
            TileInvalidations: tileInvalidations);
        tileInvalidations = 0;
        BeginCacheFrame();
        Scene2DRecordContext childContext = context.WithLocalTransform(localTransform);
        try
        {
            using ScenePrismScope prism = childContext.HasPrism(this)
                ? childContext.BeginPrism(this, GetVisibleLocalBounds())
                : default;
            RecordCachedMap(Multiply(model.Tint, Tint), childContext);
            CompleteCacheFrame();
        }
        finally
        {
            if (hasOpacity)
            {
                context.Frame.PopOpacity();
            }
            if (hasTransform)
            {
                context.Frame.PopTransform();
            }
        }
    }

    internal override Matrix3x2 GetLocalTransform()
    {
        DrawPoint modelOffset = Model?.Offset ?? default;
        return SceneGeometry2D.CreateLocalTransform(this, TransformOrigin) *
            Matrix3x2.CreateTranslation(Offset.X + modelOffset.X, Offset.Y + modelOffset.Y);
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        TileMap2DModel? model = Model;
        if (model is null || !model.IsVisible || Opacity <= 0 || model.Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }
        return GetMapBounds();
    }

    internal TileMap2DDiagnosticsSnapshot GetDiagnosticsSnapshot() => diagnostics;

    internal override void ReleaseRenderCaches()
    {
        ClearTileCache();
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
        validatedAtlasSizes.Clear();
        placementImageSizes.Clear();
        validatedAtlasModel = null;
        diagnostics = diagnostics with { RetainedBytes = 0, RetainedObjects = 0 };
    }

    internal static Color Multiply(Color left, Color right) =>
        new(
            (byte)((left.R * right.R + 127) / 255),
            (byte)((left.G * right.G + 127) / 255),
            (byte)((left.B * right.B + 127) / 255),
            (byte)((left.A * right.A + 127) / 255));

    private void ResolveAtlases(TileMap2DModel model)
    {
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
        if (!ReferenceEquals(validatedAtlasModel, model)) { placementImageSizes.Clear(); }
        long combinedVersion = model.Version;
        foreach (TileSet2D tileSet in model.TileSets)
        {
            ImageResourceResolution resolution = ImageResourceResolver.Resolve(
                this,
                tileSet.AtlasResourceId,
                explicitProvider: null,
                explicitTracker: null,
                InvalidationFlags.Render,
                affectsIntrinsicSize: false);
            resolvedAtlases.Add(new TileImageKey(tileSet.Id, null), new ResolvedAtlas(resolution.Image, resolution.Version, tileSet.Version));
            if (resolution.Image is IDrawImage image)
            {
                resolvedAtlasSizes[tileSet.AtlasResourceId.Key] = new DrawSize(image.Width, image.Height);
            }
            combinedVersion = unchecked((combinedVersion * 397) ^ tileSet.Version ^ resolution.Version);
        }

        foreach (ImageReference reference in model.PlacementImages)
        {
            ImageResourceResolution resolution = reference.ResourceId is ResourceId<ImageResource> resourceId
                ? ImageResourceResolver.Resolve(this, resourceId, null, null, InvalidationFlags.Render, affectsIntrinsicSize: false)
                : default;
            IDrawImage? image = reference.DirectImage ?? resolution.Image;
            resolvedAtlases.Add(new TileImageKey(null, reference), new ResolvedAtlas(image, resolution.Version, 1));
            DrawSize currentSize = image is null ? default : new DrawSize(image.Width, image.Height);
            if (!placementImageSizes.TryGetValue(reference, out DrawSize previousSize) || previousSize != currentSize)
            {
                indexedModel = null;
                placementImageSizes[reference] = currentSize;
            }
            if (image is not null && reference.ResourceId is ResourceId<ImageResource> id)
            {
                resolvedAtlasSizes[id.Key] = new DrawSize(image.Width, image.Height);
            }
            combinedVersion = unchecked((combinedVersion * 397) ^ resolution.Version ^ reference.GetHashCode());
        }

        bool needsValidation = !ReferenceEquals(validatedAtlasModel, model) ||
            resolvedAtlasSizes.Count != validatedAtlasSizes.Count;
        if (!needsValidation)
        {
            foreach ((string key, DrawSize size) in resolvedAtlasSizes)
            {
                if (!validatedAtlasSizes.TryGetValue(key, out DrawSize previous) || previous != size)
                {
                    needsValidation = true;
                    break;
                }
            }
        }
        if (needsValidation)
        {
            if (model.IsFreePlacement) { indexedModel = null; }
            // Validate every resolved definition before any chunk can publish
            // commands. Missing runtime resources retain deferred resolution;
            // import documents instead require all atlas declarations.
            Scene2DDiagnosticCollector validation = new();
            Scene2DModelValidator.ValidateMap(model, resolvedAtlasSizes, validation, "$", requireAllAtlases: false);
            Scene2DModelValidator.ThrowIfInvalid(validation.Complete(), nameof(Model));
            validatedAtlasSizes.Clear();
            foreach ((string key, DrawSize size) in resolvedAtlasSizes) { validatedAtlasSizes.Add(key, size); }
            validatedAtlasModel = model;
        }

        SetRenderDependencies(RenderDependency.None
            .WithResourceIdentity(string.Join("|", model.TileSets.Select(static tileSet => tileSet.AtlasResourceId.ToString())
                .Concat(model.PlacementImages.Select(static image => image.ResourceId?.ToString() ?? "direct"))))
            .WithResourceVersion(combinedVersion));
    }

    private readonly record struct TileImageKey(string? TileSetId, ImageReference? PlacementImage);

    private readonly record struct ResolvedAtlas(IDrawImage? Image, long Version, long DefinitionVersion)
    {
        internal DrawSize Size => Image is null ? default : new DrawSize(Image.Width, Image.Height);
    }
}
