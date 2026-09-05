using System.Collections.ObjectModel;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(PromotedTiles))]
public sealed class TileLayer2D : SceneNode2D
{
    public static readonly UiProperty<string> LayerIdProperty =
        UiProperty<string>.Register(
            nameof(LayerId),
            typeof(TileLayer2D),
            new UiPropertyMetadata<string>(string.Empty, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> OffsetProperty =
        UiProperty<DrawPoint>.Register(
            nameof(Offset),
            typeof(TileLayer2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Color> TintProperty =
        UiProperty<Color>.Register(
            nameof(Tint),
            typeof(TileLayer2D),
            new UiPropertyMetadata<Color>(Color.White, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<DrawPoint> TransformOriginProperty =
        UiProperty<DrawPoint>.Register(
            nameof(TransformOrigin),
            typeof(TileLayer2D),
            new UiPropertyMetadata<DrawPoint>(default, UiPropertyOptions.AffectsRender));

    public TileLayer2D()
    {
        PromotedTiles = new PromotedTileCollection(this);
    }

    internal TileMap2D? OwnerMap { get; set; }

    internal bool IsGenerated { get; set; }

    public string LayerId
    {
        get => GetValue(LayerIdProperty);
        set => SetValue(LayerIdProperty, value ?? string.Empty);
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

    public DrawPoint TransformOrigin
    {
        get => GetValue(TransformOriginProperty);
        set => SetValue(TransformOriginProperty, value);
    }

    public Collection<TileInstance2D> PromotedTiles { get; }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        foreach (TileInstance2D tile in PromotedTiles)
        {
            tile.AttachSurface(surface);
        }
        foreach (Collider2D collider in LogicalChildren.OfType<TileStaticCollider2D>())
        {
            collider.AttachSurface(surface);
        }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        TileMap2D? map = OwnerMap;
        if (map?.Model is not TileMap2DModel model ||
            !model.TryGetLayer(LayerId, out TileLayer2DModel? layer) ||
            layer is null ||
            !layer.IsVisible ||
            !UIElementVisibility.ParticipatesInRendering(this) ||
            Opacity <= 0 ||
            layer.Opacity <= 0)
        {
            return;
        }

        Matrix3x2 localTransform = GetLocalTransform();
        bool hasTransform = localTransform != Matrix3x2.Identity;
        float opacity = Opacity * layer.Opacity;
        bool hasOpacity = opacity < 1;
        if (hasTransform)
        {
            context.Frame.PushTransform(localTransform);
        }
        if (hasOpacity)
        {
            context.Frame.PushOpacity(opacity);
        }

        Scene2DRecordContext childContext = context.WithLocalTransform(localTransform);
        try
        {
            using ScenePrismScope prism = childContext.HasPrism(this)
                ? childContext.BeginPrism(this, GetVisibleLocalBounds())
                : default;
            map.RecordLayer(this, layer, TileMap2D.Multiply(layer.Tint, Tint), childContext);
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
        Matrix3x2 transform = SceneGeometry2D.CreateLocalTransform(this, TransformOrigin);
        DrawPoint offset = Offset;
        if (OwnerMap?.Model is TileMap2DModel model &&
            model.TryGetLayer(LayerId, out TileLayer2DModel? layer) &&
            layer is not null)
        {
            offset = new DrawPoint(offset.X + layer.Offset.X, offset.Y + layer.Offset.Y);
        }

        return transform * Matrix3x2.CreateTranslation(offset.X, offset.Y);
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        TileMap2D? map = OwnerMap;
        if (map?.Model is not TileMap2DModel model ||
            !model.TryGetLayer(LayerId, out TileLayer2DModel? layer) ||
            layer is null ||
            !layer.IsVisible ||
            Opacity <= 0 ||
            layer.Opacity <= 0)
        {
            return SceneBounds2D.Empty;
        }

        SceneBounds2D result = SceneBounds2D.Empty;
        foreach (TileChunk2D chunk in layer.Chunks)
        {
            result = SceneGeometry2D.Union(
                result,
                SceneBounds2D.Known(new DrawRect(
                    chunk.Origin.X * model.TileSize.Width,
                    chunk.Origin.Y * model.TileSize.Height,
                    chunk.Width * model.TileSize.Width,
                    chunk.Height * model.TileSize.Height)));
        }
        return result;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, LayerIdProperty))
        {
            OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
        }
        else if (ReferenceEquals(args.Property, OffsetProperty) ||
                 ReferenceEquals(args.Property, TransformOriginProperty) ||
                 SceneGeometry2D.IsSceneTransformProperty(args.Property))
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

    private sealed class PromotedTileCollection(TileLayer2D owner) : Collection<TileInstance2D>
    {
        protected override void InsertItem(int index, TileInstance2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (this.Any(existing => existing.X == item.X && existing.Y == item.Y))
            {
                throw new InvalidOperationException(
                    $"Tile ({item.X},{item.Y}) is already promoted in layer '{owner.LayerId}'.");
            }

            owner.LogicalChildren.Insert(index, item);
            base.InsertItem(index, item);
            item.OwnerLayer = owner;
            item.AttachSurface(owner.Surface);
            owner.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void SetItem(int index, TileInstance2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (this.Where((_, current) => current != index).Any(existing => existing.X == item.X && existing.Y == item.Y))
            {
                throw new InvalidOperationException(
                    $"Tile ({item.X},{item.Y}) is already promoted in layer '{owner.LayerId}'.");
            }

            TileInstance2D previous = this[index];
            previous.OwnerLayer = null;
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            owner.LogicalChildren.Insert(index, item);
            base.SetItem(index, item);
            item.OwnerLayer = owner;
            item.AttachSurface(owner.Surface);
            owner.OwnerMap?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void RemoveItem(int index)
        {
            TileInstance2D previous = this[index];
            TileMap2D? map = owner.OwnerMap;
            previous.OwnerLayer = null;
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            base.RemoveItem(index);
            map?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }

        protected override void ClearItems()
        {
            TileMap2D? map = owner.OwnerMap;
            foreach (TileInstance2D tile in this)
            {
                tile.OwnerLayer = null;
                tile.AttachSurface(null);
                owner.LogicalChildren.Remove(tile);
            }
            base.ClearItems();
            map?.SynchronizeCollisionAdaptersAndNotify();
            owner.Surface?.InvalidateFrame();
        }
    }
}
