using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Layers))]
public sealed partial class TileMap2D : SceneNode2D
{
    public static readonly UiProperty<TileMap2DModel?> ModelProperty =
        UiProperty<TileMap2DModel?>.Register(
            nameof(Model),
            typeof(TileMap2D),
            new UiPropertyMetadata<TileMap2DModel?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(TileMap2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    private readonly Dictionary<string, ResolvedAtlas> resolvedAtlases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DrawSize> resolvedAtlasSizes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DrawSize> validatedAtlasSizes = new(StringComparer.Ordinal);
    private TileMap2DModel? validatedAtlasModel;
    private bool synchronizingLayers;
    private int promotions;
    private int demotions;
    private int tileInvalidations;
    private TileMap2DDiagnosticsSnapshot diagnostics;

    public TileMap2D()
    {
        Layers = new LayerCollection(this);
    }

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

    public Collection<TileLayer2D> Layers { get; }

    public TileInstance2D Promote(TileCellKey2D key, int? tileId = null)
    {
        TileMap2DModel model = Model ??
            throw new InvalidOperationException("A tilemap model is required before promoting a cell.");
        if (!model.TryGetLayer(key.LayerId, out TileLayer2DModel? layer) || layer is null)
        {
            throw new ArgumentException($"Layer '{key.LayerId}' does not exist.", nameof(key));
        }
        TileLayer2D presentation = Layers.Single(candidate =>
            string.Equals(candidate.LayerId, key.LayerId, StringComparison.Ordinal));
        TileInstance2D? existing = presentation.PromotedTiles.FirstOrDefault(candidate =>
            candidate.X == key.Coordinate.X && candidate.Y == key.Coordinate.Y);
        if (existing is not null)
        {
            return existing;
        }

        if (!layer.TryGetCell(key.Coordinate, out TileCell2D cell))
        {
            throw new ArgumentOutOfRangeException(nameof(key), "The coordinate is not present in the target layer.");
        }
        if (cell.TileId == 0 && tileId is null)
        {
            throw new InvalidOperationException("Promoting an empty tile requires an explicit positive tile id.");
        }
        if (tileId is int overrideId && !model.TryResolveTile(overrideId, out _, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(tileId), $"Tile id {overrideId} has no tileset definition.");
        }

        TileInstance2D promoted = new()
        {
            X = key.Coordinate.X,
            Y = key.Coordinate.Y,
            TileId = tileId
        };
        presentation.PromotedTiles.Add(promoted);
        promotions++;
        InvalidateFrame("Tile promoted");
        return promoted;
    }

    public bool Demote(TileCellKey2D key)
    {
        TileLayer2D? layer = Layers.FirstOrDefault(candidate =>
            string.Equals(candidate.LayerId, key.LayerId, StringComparison.Ordinal));
        TileInstance2D? promoted = layer?.PromotedTiles.FirstOrDefault(candidate =>
            candidate.X == key.Coordinate.X && candidate.Y == key.Coordinate.Y);
        if (layer is null || promoted is null)
        {
            return false;
        }

        bool removed = layer.PromotedTiles.Remove(promoted);
        if (removed)
        {
            demotions++;
            InvalidateFrame("Tile demoted");
        }
        return removed;
    }

    public bool TryGetPromoted(TileCellKey2D key, out TileInstance2D? tile)
    {
        tile = Layers
            .FirstOrDefault(candidate => string.Equals(candidate.LayerId, key.LayerId, StringComparison.Ordinal))?
            .PromotedTiles
            .FirstOrDefault(candidate => candidate.X == key.Coordinate.X && candidate.Y == key.Coordinate.Y);
        return tile is not null;
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
            SynchronizeLayers();
            SynchronizeCollisionAdaptersAndNotify();
        }
        else if (SceneGeometry2D.IsSceneTransformProperty(args.Property) ||
                 ReferenceEquals(args.Property, TransformOriginProperty))
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
        foreach (TileLayer2D layer in Layers)
        {
            layer.AttachSurface(surface);
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
            Opacity <= 0)
        {
            diagnostics = default;
            return;
        }

        SynchronizeLayers();
        ResolveAtlases(model);
        Matrix3x2 localTransform = GetLocalTransform();
        bool hasTransform = localTransform != Matrix3x2.Identity;
        bool hasOpacity = Opacity < 1;
        if (hasTransform)
        {
            context.Frame.PushTransform(localTransform);
        }
        if (hasOpacity)
        {
            context.Frame.PushOpacity(Opacity);
        }

        diagnostics = new TileMap2DDiagnosticsSnapshot(
            TotalChunks: model.Layers.Sum(static layer => layer.Chunks.Count),
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
            TileInvalidations: tileInvalidations,
            PromotedInstancesVisible: 0,
            PromotedInstancesCulled: 0,
            Promotions: promotions,
            Demotions: demotions,
            BatchSplits: 0);
        tileInvalidations = 0;
        BeginCacheFrame();
        Scene2DRecordContext childContext = context.WithLocalTransform(localTransform);
        try
        {
            using ScenePrismScope prism = childContext.HasPrism(this)
                ? childContext.BeginPrism(this, GetVisibleLocalBounds())
                : default;
            foreach (TileLayer2DModel layerModel in model.Layers
                .Select((layer, sourceIndex) => (layer, sourceIndex))
                .OrderBy(static entry => entry.layer.Order)
                .ThenBy(static entry => entry.sourceIndex)
                .Select(static entry => entry.layer))
            {
                TileLayer2D layer = Layers.Single(candidate =>
                    string.Equals(candidate.LayerId, layerModel.Id, StringComparison.Ordinal));
                layer.Record(childContext);
            }
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

    internal override Matrix3x2 GetLocalTransform() =>
        SceneGeometry2D.CreateLocalTransform(this, TransformOrigin);

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        TileMap2DModel? model = Model;
        if (model is null || Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        SceneBounds2D result = SceneBounds2D.Empty;
        foreach (TileLayer2DModel layerModel in model.Layers)
        {
            TileLayer2D? layer = Layers.FirstOrDefault(candidate =>
                string.Equals(candidate.LayerId, layerModel.Id, StringComparison.Ordinal));
            if (layer is null)
            {
                continue;
            }

            Matrix3x2 transform = layer.GetLocalTransform() *
                Matrix3x2.CreateTranslation(
                    layerModel.Offset.X + layer.Offset.X,
                    layerModel.Offset.Y + layer.Offset.Y);
            result = SceneGeometry2D.Union(
                result,
                SceneGeometry2D.TransformBounds(layer.GetVisibleLocalBounds(), transform));
        }
        return result;
    }

    internal void RecordLayer(
        TileLayer2D presentation,
        TileLayer2DModel layer,
        Color tint,
        Scene2DRecordContext context)
        => RecordCachedLayer(presentation, layer, tint, context);

    internal void RecordPromotedTile(
        TileLayer2D layerPresentation,
        TileInstance2D instance,
        Scene2DRecordContext context)
    {
        TileMap2DModel model = Model!;
        if (!model.TryGetLayer(layerPresentation.LayerId, out TileLayer2DModel? layer) || layer is null)
        {
            return;
        }
        TileCoordinate2D coordinate = new(instance.X, instance.Y);
        TileCell2D cell = ValidatePromotedInstance(layer, coordinate, instance);
        if (!UIElementVisibility.ParticipatesInRendering(instance) || instance.Opacity <= 0)
        {
            return;
        }

        int tileId = instance.TileId ?? cell.TileId;
        if (!TryResolveVisual(tileId, out IDrawImage? image, out TileDefinition2D? definition))
        {
            throw new InvalidOperationException(
                $"Tile id {tileId} at ({coordinate.X},{coordinate.Y}) in layer '{layer.Id}' has no resolvable atlas visual.");
        }

        TileFlip2D flip = instance.Flip ?? cell.Flip;
        DrawRect source = instance.SourceRect ?? definition!.SourceRect;
        instance.ResolveAnimatedVisual(source, flip, out source, out TileFlip2D effectiveFlip);
        Matrix3x2 localTransform = instance.GetLocalTransform();
        Scene2DRecordContext tileContext = context.WithLocalTransform(localTransform);
        if (!tileContext.IntersectsVisibleLocalBounds(instance.GetVisibleLocalBounds()))
        {
            diagnostics = diagnostics with
            {
                PromotedInstancesCulled = diagnostics.PromotedInstancesCulled + 1
            };
            return;
        }
        bool hasTransform = localTransform != Matrix3x2.Identity;
        if (hasTransform)
        {
            context.Frame.PushTransform(localTransform);
        }
        bool hasOpacity = instance.Opacity < 1;
        if (hasOpacity)
        {
            context.Frame.PushOpacity(instance.Opacity);
        }

        try
        {
            using ScenePrismScope prism = tileContext.HasPrism(instance)
                ? tileContext.BeginPrism(instance, instance.GetVisibleLocalBounds())
                : default;
            DrawSprite2D sprite = TileFlipGeometry2D.Sprite(
                new DrawRect(0, 0, model.TileSize.Width, model.TileSize.Height), source,
                Multiply(Multiply(layer.Tint, layerPresentation.Tint), instance.Tint), effectiveFlip);
            context.Frame.DrawSprite(image!, sprite.Destination, source, sprite.Options.Tint,
                sprite.Options.Rotation, sprite.Options.Origin, (RenderSurface2DSpriteFlip)sprite.Options.Flip, layerDepth: 0);
            diagnostics = diagnostics with
            {
                DrawnTiles = diagnostics.DrawnTiles + 1,
                DrawCommands = diagnostics.DrawCommands + 1,
                PromotedInstancesVisible = diagnostics.PromotedInstancesVisible + 1
            };
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

    internal TileMap2DDiagnosticsSnapshot GetDiagnosticsSnapshot() => diagnostics;

    internal override void ReleaseRenderCaches()
    {
        ClearTileCache();
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
        validatedAtlasSizes.Clear();
        validatedAtlasModel = null;
        diagnostics = diagnostics with { RetainedBytes = 0, RetainedObjects = 0 };
    }

    internal static Color Multiply(Color left, Color right) =>
        new(
            (byte)((left.R * right.R + 127) / 255),
            (byte)((left.G * right.G + 127) / 255),
            (byte)((left.B * right.B + 127) / 255),
            (byte)((left.A * right.A + 127) / 255));

    private void SynchronizeLayers()
    {
        if (synchronizingLayers)
        {
            return;
        }

        synchronizingLayers = true;
        try
        {
            TileMap2DModel? model = Model;
            for (int index = Layers.Count - 1; index >= 0; index--)
            {
                TileLayer2D existing = Layers[index];
                if (existing.IsGenerated &&
                    (model is null || !model.Layers.Any(layer =>
                        string.Equals(layer.Id, existing.LayerId, StringComparison.Ordinal))))
                {
                    Layers.RemoveAt(index);
                }
            }

            if (model is null)
            {
                return;
            }

            string? duplicate = Layers
                .Where(static layer => !string.IsNullOrWhiteSpace(layer.LayerId))
                .GroupBy(static layer => layer.LayerId, StringComparer.Ordinal)
                .Where(static group => group.Count() > 1)
                .Select(static group => group.Key)
                .FirstOrDefault();
            if (duplicate is not null)
            {
                throw new InvalidOperationException($"Tile layer presentation '{duplicate}' is declared more than once.");
            }

            foreach (TileLayer2D existing in Layers.Where(static layer => !layer.IsGenerated))
            {
                if (string.IsNullOrWhiteSpace(existing.LayerId) ||
                    !model.Layers.Any(layer => string.Equals(layer.Id, existing.LayerId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        $"Tile layer presentation '{existing.LayerId}' does not match a model layer.");
                }
            }

            foreach (TileLayer2DModel layer in model.Layers)
            {
                if (Layers.Any(existing => string.Equals(existing.LayerId, layer.Id, StringComparison.Ordinal)))
                {
                    continue;
                }
                Layers.Add(new TileLayer2D
                {
                    LayerId = layer.Id,
                    IsGenerated = true
                });
            }
        }
        finally
        {
            synchronizingLayers = false;
        }
    }

    private void ResolveAtlases(TileMap2DModel model)
    {
        resolvedAtlases.Clear();
        resolvedAtlasSizes.Clear();
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
            resolvedAtlases.Add(tileSet.Id, new ResolvedAtlas(resolution.Image, resolution.Version));
            if (resolution.Image is IDrawImage image)
            {
                resolvedAtlasSizes[tileSet.AtlasResourceId.Key] = new DrawSize(image.Width, image.Height);
            }
            combinedVersion = unchecked((combinedVersion * 397) ^ tileSet.Version ^ resolution.Version);
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
            .WithResourceIdentity(string.Join("|", model.TileSets.Select(static tileSet => tileSet.AtlasResourceId.ToString())))
            .WithResourceVersion(combinedVersion));
    }

    private bool TryResolveVisual(
        int tileId,
        out IDrawImage? image,
        out TileDefinition2D? definition)
    {
        if (Model!.TryResolveTile(tileId, out TileSet2D? tileSet, out definition) &&
            tileSet is not null &&
            resolvedAtlases.TryGetValue(tileSet.Id, out ResolvedAtlas atlas) &&
            atlas.Image is not null)
        {
            image = atlas.Image;
            return true;
        }

        image = null;
        definition = null;
        return false;
    }

    private TileCell2D ValidatePromotedInstance(
        TileLayer2DModel layer,
        TileCoordinate2D coordinate,
        TileInstance2D instance)
    {
        if (!layer.TryGetCell(coordinate, out TileCell2D cell))
        {
            throw new InvalidOperationException(
                $"Promoted tile ({coordinate.X},{coordinate.Y}) does not exist in layer '{layer.Id}'.");
        }

        int tileId = instance.TileId ?? cell.TileId;
        if (tileId == 0)
        {
            throw new InvalidOperationException(
                $"Promoting empty tile ({coordinate.X},{coordinate.Y}) in layer '{layer.Id}' requires an explicit positive tile id.");
        }
        if (!Model!.TryResolveTile(tileId, out _, out _))
        {
            throw new InvalidOperationException(
                $"Promoted tile id {tileId} at ({coordinate.X},{coordinate.Y}) in layer '{layer.Id}' has no tileset definition.");
        }

        return cell;
    }

    private void InvalidateFrame(string reason)
    {
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, reason);
        Surface?.InvalidateFrame();
    }

    private readonly record struct ResolvedAtlas(IDrawImage? Image, long Version);

    private sealed class LayerCollection(TileMap2D owner) : Collection<TileLayer2D>
    {
        protected override void InsertItem(int index, TileLayer2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.LogicalChildren.Insert(index, item);
            base.InsertItem(index, item);
            item.OwnerMap = owner;
            item.AttachSurface(owner.Surface);
            if (!owner.synchronizingLayers)
            {
                owner.SynchronizeLayers();
                owner.SynchronizeCollisionAdaptersAndNotify();
            }
            owner.Surface?.InvalidateFrame();
        }

        protected override void SetItem(int index, TileLayer2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            TileLayer2D previous = this[index];
            previous.OwnerMap = null;
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            owner.LogicalChildren.Insert(index, item);
            base.SetItem(index, item);
            item.OwnerMap = owner;
            item.AttachSurface(owner.Surface);
            if (!owner.synchronizingLayers)
            {
                owner.SynchronizeLayers();
                owner.SynchronizeCollisionAdaptersAndNotify();
            }
            owner.Surface?.InvalidateFrame();
        }

        protected override void RemoveItem(int index)
        {
            TileLayer2D previous = this[index];
            previous.OwnerMap = null;
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            base.RemoveItem(index);
            if (!owner.synchronizingLayers)
            {
                owner.SynchronizeCollisionAdaptersAndNotify();
            }
            owner.Surface?.InvalidateFrame();
        }

        protected override void ClearItems()
        {
            foreach (TileLayer2D layer in this)
            {
                layer.OwnerMap = null;
                layer.AttachSurface(null);
                owner.LogicalChildren.Remove(layer);
            }
            base.ClearItems();
            if (!owner.synchronizingLayers)
            {
                owner.SynchronizeCollisionAdaptersAndNotify();
            }
            owner.Surface?.InvalidateFrame();
        }
    }
}
