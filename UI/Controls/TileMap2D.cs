using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public sealed partial class TileMap2D : SceneNode2D, ISceneSpatialParticipant2D, IAsyncDisposable
{
    public static readonly UiProperty<DrawPoint> OffsetProperty =
        UiProperty<DrawPoint>.Register(nameof(Offset), typeof(TileMap2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Color> TintProperty =
        UiProperty<Color>.Register(nameof(Tint), typeof(TileMap2D),
            new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(nameof(TransformOrigin), typeof(TileMap2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    private readonly Dictionary<ImageReference, ResolvedAtlas> resolvedAtlases = [];
    private readonly Dictionary<string, DrawSize> resolvedAtlasSizes = new(StringComparer.Ordinal);
    private readonly HashSet<ImageReference> requiredImageKeys = [];
    private TileMapSource2D? source;
    private int tileInvalidations;
    private TileMap2DDiagnosticsSnapshot diagnostics;

    /// <summary>Adapts a complete in-memory model to a detached map node with its composition order.</summary>
    public static TileMap2D FromModel(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize>? imageSizes = null)
    {
        TileMapSource2D source = TileMapSource2D.FromModel(model, imageSizes);
        return FromSource(source);
    }

    internal static TileMap2D FromSource(TileMapSource2D source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new TileMap2D { Source = source, Layer = source.Catalog.Order };
    }

    internal TileMapSource2D? Source
    {
        get => source;
        set
        {
            VerifyMapOwnerAccess();
            ObjectDisposedException.ThrowIf(terminalDisposal is not null, this);
            if (ReferenceEquals(source, value)) { return; }
            source = value;
            tileInvalidations++;
            StopSource();
            StartSource();
            Refresh();
        }
    }
    public DrawPoint TransformOrigin { get => GetValue(TransformOriginProperty); set => SetValue(TransformOriginProperty, value); }
    public DrawPoint Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
    public Color Tint { get => GetValue(TintProperty); set => SetValue(TintProperty, value); }

    // Presentation and adapters use the owner's applied header. A newly
    // published worker header is checked separately by readiness, before queries.
    internal TileMapCatalog2D? Catalog => appliedCatalog ?? Source?.Catalog;

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (SceneGeometry2D.IsSceneTransformProperty(args.Property) ||
                 ReferenceEquals(args.Property, TransformOriginProperty) ||
                 ReferenceEquals(args.Property, OffsetProperty) ||
                 ReferenceEquals(args.Property, UIElement.IsVisibleProperty) ||
                 ReferenceEquals(args.Property, UIElement.VisibilityProperty) ||
                 ReferenceEquals(args.Property, UIElement.OpacityProperty))
        {
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Geometry);
            SimulationContext?.RefreshSpatialItems();
        }
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        if (!ReferenceEquals(Surface, surface)) { ReleaseRenderCaches(); }
        base.AttachSurface(surface);
        foreach (TileStaticCollider2D collider in LogicalChildren.OfType<TileStaticCollider2D>())
        {
            collider.AttachSurface(surface);
        }
    }

    internal override void CheckPresentation(ScenePresentationContext2D context)
    {
        TileMapCatalog2D? catalog = Catalog;
        if (!IsPresentationVisible(catalog) || Source is null) { return; }
        SceneBounds2D visible = context.GetVisibleBounds(this);
        CheckPrismPresentation(context);
        if (visible.Kind == SceneBoundsKind.Empty) { return; }
        if (!ReferenceEquals(observedSource, Source) || !ReferenceEquals(appliedCatalog, Source.Catalog))
        {
            context.Require();
            return;
        }
        EnsureSpatialIndexes();
        foreach (TileRenderChunk chunk in spatialIndex!.Query(visible))
        {
            if (!IntersectsChunk(visible, chunk)) { continue; }
            if (!residentData.TryGetValue(chunk.GetKey(), out ResidentChunk? resident) ||
                resident.Info.Spatial.Version != chunk.Version || !ReferenceEquals(resident.ValidatedCatalog, catalog))
            {
                context.Require(ReferenceEquals(requestedCatalog, catalog) && requiredErrors.TryGetValue(chunk.GetKey(), out Exception? error) ? error : null);
            }
        }
        try { ResolveAtlases(catalog!, visible, ImageResourceAccess.Prepare, context); }
        catch (Exception failure) { context.Require(failure); }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        TileMapCatalog2D? catalog = Catalog;
        if (!IsPresentationVisible(catalog))
        {
            ReleaseRenderCaches();
            diagnostics = default;
            return;
        }
        Matrix3x2 localTransform = GetLocalTransform();
        Scene2DRecordContext childContext = context.WithLocalTransform(localTransform);
        SceneBounds2D visibleBounds = childContext.GetConservativeVisibleLocalBounds();
        visibleBounds = SceneSpatialInterest2D.ResolveInputBounds(this, visibleBounds, surfaceBounds: context.Frame.Bounds);
        ResolveAtlases(catalog!, visibleBounds, ImageResourceAccess.ResidentOnly);
        bool hasTransform = localTransform != Matrix3x2.Identity;
        float opacity = Opacity * catalog!.Opacity;
        bool hasOpacity = opacity < 1;
        if (hasTransform) { context.Frame.PushTransform(localTransform); }
        if (hasOpacity) { context.Frame.PushOpacity(opacity); }
        diagnostics = new TileMap2DDiagnosticsSnapshot(catalog.Chunks.Count, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, tileInvalidations);
        tileInvalidations = 0;
        BeginCacheFrame();
        try
        {
            using ScenePrismScope prism = childContext.HasPrism(this)
                ? childContext.BeginPrism(this, GetVisibleLocalBounds()) : default;
            Color tint = Multiply(catalog.Tint, Tint);
            RecordCachedMap(tint, childContext, visibleBounds);
            context.Frame.QueueWarmPreparation(context.Surface,
                new(this, cacheFrameVersion, RenderVersion, tint));
        }
        finally
        {
            if (hasOpacity) { context.Frame.PopOpacity(); }
            if (hasTransform) { context.Frame.PopTransform(); }
        }
    }

    private bool IsPresentationVisible(TileMapCatalog2D? catalog) =>
        catalog is { IsVisible: true, Opacity: > 0 } && Opacity > 0 &&
        UIElementVisibility.ParticipatesInRendering(this);

    internal override Matrix3x2 GetLocalTransform() => GetLocalTransform(Catalog);

    private Matrix3x2 GetLocalTransform(TileMapCatalog2D? catalog)
    {
        DrawPoint mapOffset = catalog?.Offset ?? default;
        return SceneGeometry2D.CreateLocalTransform(this, TransformOrigin) *
            Matrix3x2.CreateTranslation(Offset.X + mapOffset.X, Offset.Y + mapOffset.Y);
    }

    internal override SceneBounds2D GetVisibleLocalBounds() =>
        IsPresentationVisible(Catalog) ? GetMapBounds() : SceneBounds2D.Empty;

    internal TileMap2DDiagnosticsSnapshot GetDiagnosticsSnapshot() => diagnostics with
    {
        ResidentDataChunks = residentData.Count,
        PendingDataChunks = residency?.PendingLoadCount ?? 0
    };

    internal override void ReleaseRenderCaches()
    {
        long generation = sourceGeneration;
        List<Exception>? failures = null;
        try { ClearTileCache(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { if (generation == sourceGeneration) { ReleaseImageResources(); } }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        if (generation != sourceGeneration)
        {
            if (failures is not null) { throw new AggregateException(failures); }
            return;
        }
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
        diagnostics = diagnostics with
        {
            RetainedBytes = 0, RetainedObjects = 0, WarmChunks = 0, WarmRetainedBytes = 0,
            WarmChargedBytes = 0, WarmImageBytes = 0, WarmDataBytes = 0, WarmPendingChunks = 0
        };
        if (failures is not null) { throw new AggregateException(failures); }
    }

    internal static Color Multiply(Color left, Color right) => new(
        (byte)((left.R * right.R + 127) / 255), (byte)((left.G * right.G + 127) / 255),
        (byte)((left.B * right.B + 127) / 255), (byte)((left.A * right.A + 127) / 255));

    private void ResolveAtlases(TileMapCatalog2D catalog, SceneBounds2D visibleBounds,
        ImageResourceAccess access, ScenePresentationContext2D? presentation = null, bool validateGeometry = true)
    {
        using ImageResourceLeaseSet.Scope usage = SourceImageLeases.Begin();
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
        requiredImageKeys.Clear();
        EnsureSpatialIndexes();
        foreach (TileRenderChunk chunk in spatialIndex!.Query(visibleBounds))
        {
            if (IntersectsChunk(visibleBounds, chunk)) { requiredImageKeys.UnionWith(chunk.ImageKeys); }
        }
        long combinedVersion = catalog.Version;
        foreach (ImageReference key in requiredImageKeys)
        {
            ImageReference reference = key;
            ImageResourceResolution resolution = reference.ResourceId is { } resource
                ? ImageResourceResolver.Resolve(this, resource, null, null, InvalidationFlags.Render,
                    affectsIntrinsicSize: false, access: access) : default;
            presentation?.RequireImage(resolution);
            IDrawImage? image = reference.DirectImage ?? resolution.Image;
            ResolvedAtlas atlas = new(image, resolution.Version);
            resolvedAtlases[key] = atlas;
            if (image is not null && reference.ResourceId is { } id) { resolvedAtlasSizes[id.Key] = atlas.Size; }
            combinedVersion = unchecked((combinedVersion * 397) ^ resolution.Version);
        }
        if (validateGeometry)
        {
            foreach (TileRenderChunk chunk in spatialIndex!.Query(visibleBounds))
            {
                if (IntersectsChunk(visibleBounds, chunk)) { ValidateResidentAtlases(chunk); }
            }
        }
        SetRenderDependencies(RenderDependency.None.WithResourceIdentity(catalog.Id).WithResourceVersion(combinedVersion));
    }

    private void ValidateResidentAtlases(TileRenderChunk chunk)
    {
        if (!residentData.TryGetValue(chunk.GetKey(), out ResidentChunk? resident) ||
            !ReferenceEquals(resident.ValidatedCatalog, Catalog)) { return; }
        TileMapChunkData2D data = resident.Payload.Value;
        if (data.TileSets.Count == 0) { return; }
        DrawSize?[] sizes = resident.ValidatedAtlasSizes ??= new DrawSize?[data.TileSets.Count];
        bool changed = false;
        for (int index = 0; index < data.TileSets.Count; index++)
        {
            string id = data.TileSets[index].AtlasResourceId.Key;
            if (resolvedAtlasSizes.TryGetValue(id, out DrawSize size) && sizes[index] != size) { changed = true; break; }
        }
        if (!changed) { return; }
        Scene2DDiagnosticCollector validation = new();
        Scene2DModelValidator.ValidateTileSets(data.TileSets, resolvedAtlasSizes, validation, "$", requireAllAtlases: false);
        Scene2DModelValidator.ThrowIfInvalid(validation.Complete(), nameof(Source));
        for (int index = 0; index < data.TileSets.Count; index++)
        {
            if (resolvedAtlasSizes.TryGetValue(data.TileSets[index].AtlasResourceId.Key, out DrawSize size)) { sizes[index] = size; }
        }
    }

    private readonly record struct ResolvedAtlas
    {
        internal ResolvedAtlas(IDrawImage? image, long version)
        {
            Image = image;
            Version = version;
            Size = image is null ? default : new(image.Width, image.Height);
        }
        internal IDrawImage? Image { get; }
        internal long Version { get; }
        internal DrawSize Size { get; }
    }
}
