using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Text;
using SolidColorBrush = Cerneala.UI.Media.SolidColorBrush;

namespace Cerneala.UI.Controls;

[Flags]
public enum Scene2DDebugFlags
{
    None = 0,
    Colliders = 1,
    ChunkBounds = 2,
    TileCoordinates = 4,
    TileIds = 8,
    Order = 16,
    Navigation = 32,
    PromotedTiles = 64,
    All = Colliders | ChunkBounds | TileCoordinates | TileIds | Order | Navigation | PromotedTiles
}

/// <summary>A read-only, scene-parent-space grid supplied by the application.</summary>
public interface IScene2DDebugNavigationGrid
{
    TileMapBounds2D Bounds { get; }
    DrawPoint Origin { get; }
    DrawSize CellSize { get; }
    bool TryGetCell(int x, int y, out bool blocked);
}

public readonly record struct Scene2DDebugOverlayDiagnostics(
    int CandidateChunks, int VisitedTiles, int Colliders, int PromotedTiles,
    int NavigationCells, int Primitives);

/// <summary>Presentation-only diagnostics for the containing scene subtree.</summary>
public sealed class Scene2DDebugOverlay : SceneNode2D
{
    public static readonly UiProperty<Scene2DDebugFlags> FlagsProperty =
        UiProperty<Scene2DDebugFlags>.Register(nameof(Flags), typeof(Scene2DDebugOverlay),
            new UiPropertyMetadata<Scene2DDebugFlags>(Scene2DDebugFlags.None, UiPropertyOptions.AffectsRender,
                validateValue: static value => (value & ~Scene2DDebugFlags.All) == 0));
    public static readonly UiProperty<float> LineThicknessProperty =
        UiProperty<float>.Register(nameof(LineThickness), typeof(Scene2DDebugOverlay),
            new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender,
                validateValue: static value => float.IsFinite(value) && value > 0));
    public static readonly UiProperty<float> FontSizeProperty =
        UiProperty<float>.Register(nameof(FontSize), typeof(Scene2DDebugOverlay),
            new UiPropertyMetadata<float>(10, UiPropertyOptions.AffectsRender,
                validateValue: static value => float.IsFinite(value) && value > 0));
    public static readonly UiProperty<IScene2DDebugNavigationGrid?> NavigationGridProperty =
        UiProperty<IScene2DDebugNavigationGrid?>.Register(nameof(NavigationGrid), typeof(Scene2DDebugOverlay),
            new UiPropertyMetadata<IScene2DDebugNavigationGrid?>(null, UiPropertyOptions.AffectsRender));

    private static readonly Color ChunkColor = new(40, 210, 255);
    private static readonly Color PromotionColor = new(255, 80, 220);
    private static readonly Color TriggerColor = new(255, 180, 40);
    private readonly List<ColliderGeometry2D> colliders = [];
    private readonly Dictionary<Color, DrawPen> pens = [];
    private IDrawFont? font;
    private Scene2DDebugOverlayDiagnostics diagnostics;

    public Scene2DDebugOverlay() { IsHitTestVisible = false; }

    public Scene2DDebugFlags Flags { get => GetValue(FlagsProperty); set => SetValue(FlagsProperty, value); }
    public float LineThickness { get => GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }
    public float FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public IScene2DDebugNavigationGrid? NavigationGrid { get => GetValue(NavigationGridProperty); set => SetValue(NavigationGridProperty, value); }
    public Scene2DDebugOverlayDiagnostics GetDiagnosticsSnapshot() => diagnostics;

    internal override bool ParticipatesInInputRoute => false;
    internal override Matrix3x2 GetLocalTransform() => SceneGeometry2D.CreateLocalTransform(this, default);
    // Debug ink must not change a group's gameplay Y anchor or hit bounds.
    internal override SceneBounds2D GetVisibleLocalBounds() => SceneBounds2D.Empty;

    internal override void Record(Scene2DRecordContext context)
    {
        diagnostics = default;
        if (Flags == Scene2DDebugFlags.None || Opacity <= 0 ||
            !UIElementVisibility.ParticipatesInRendering(this) || LogicalParent is not Scene2D owner)
        {
            return;
        }

        Matrix3x2 transform = GetLocalTransform();
        Scene2DRecordContext debugContext = context.WithLocalTransform(transform);
        SceneBounds2D visible = debugContext.GetConservativeVisibleLocalBounds();
        // Singular presentation transforms have no finite viewport to inspect.
        if (visible.Kind != SceneBoundsKind.Known) { return; }
        using DrawTransformScope transformed = context.Frame.Transform(transform);
        bool hasOpacity = Opacity < 1;
        if (hasOpacity) { context.Frame.PushOpacity(Opacity); }
        try
        {
            RecordDebugContent(owner, context, debugContext, visible);
        }
        finally
        {
            if (hasOpacity) { context.Frame.PopOpacity(); }
        }
    }

    private void RecordDebugContent(Scene2D owner, Scene2DRecordContext context,
        Scene2DRecordContext debugContext, SceneBounds2D visible)
    {
        using ScenePrismScope prism = debugContext.HasPrism(this)
            ? debugContext.BeginPrism(this, visible) : default;
        if (Has(Scene2DDebugFlags.Colliders) && Matrix3x2.Invert(context.LocalToSceneTransform, out Matrix3x2 sceneToParent))
        {
            SceneBounds2D sceneViewport = SceneGeometry2D.TransformBounds(visible, context.LocalToSceneTransform);
            owner.CollisionWorld.CollectDebugGeometry(sceneViewport.Bounds, colliders);
            foreach (ColliderGeometry2D geometry in colliders)
            {
                if (BelongsTo(geometry.Collider, owner))
                {
                    DrawCollider(context.Frame, geometry, sceneToParent);
                }
            }
        }
        if ((Flags & (Scene2DDebugFlags.ChunkBounds | Scene2DDebugFlags.TileCoordinates |
            Scene2DDebugFlags.TileIds | Scene2DDebugFlags.Order | Scene2DDebugFlags.PromotedTiles)) != 0)
        {
            DrawChildren(owner, debugContext);
        }
        if (Has(Scene2DDebugFlags.Navigation)) { DrawNavigation(debugContext, visible.Bounds); }
    }

    private bool Has(Scene2DDebugFlags flag) => (Flags & flag) != 0;

    private static bool BelongsTo(SceneNode2D node, Scene2D owner)
    {
        for (UIElement? current = node.LogicalParent; current is not null; current = current.LogicalParent)
        {
            if (ReferenceEquals(current, owner)) { return true; }
        }
        return false;
    }

    private void DrawCollider(RenderSurface2DFrame frame, ColliderGeometry2D geometry, Matrix3x2 sceneToParent)
    {
        Collider2D collider = geometry.Collider;
        Color color = collider.CollisionMask == 0 ? new Color(150, 150, 150)
            : collider.IsTrigger ? TriggerColor : LayerColor(collider.CollisionLayer);
        using DrawTransformScope scope = frame.Transform(geometry.ShapeToSceneTransform * sceneToParent);
        ColliderLocalShape2D shape = geometry.LocalShape;
        if (shape.Kind == ColliderShapeKind2D.Box)
        {
            Rectangle(frame, new DrawRect(0, 0, shape.Width, shape.Height), color);
        }
        else if (shape.Kind == ColliderShapeKind2D.Circle)
        {
            frame.DrawEllipse(new DrawRect(-shape.Radius, -shape.Radius, shape.Radius * 2, shape.Radius * 2), Pen(color));
            CountPrimitive();
        }
        else
        {
            int edges = shape.Kind == ColliderShapeKind2D.Segment ? shape.Vertices.Count - 1 : shape.Vertices.Count;
            for (int index = 0; index < edges; index++)
            {
                Vector2 a = shape.Vertices[index], b = shape.Vertices[(index + 1) % shape.Vertices.Count];
                Line(frame, new DrawPoint(a.X, a.Y), new DrawPoint(b.X, b.Y), color);
            }
        }
        Label(frame, $"L{collider.CollisionLayer:X8} M{collider.CollisionMask:X8} {(collider.CollisionMask == 0 ? "masked" : collider.IsTrigger ? "trigger" : "solid")}", default, color);
        diagnostics = diagnostics with { Colliders = diagnostics.Colliders + 1 };
    }

    private void DrawChildren(SceneNode2D owner, Scene2DRecordContext context)
    {
        // Only presentation nodes are walked. Static cells and collision adapters
        // are queried through their existing spatial indexes, never this tree.
        if (owner is Scene2D scene)
        {
            // Observe the gameplay pass; recomputing with the debug transform
            // would change Y anchors and rescan distant map bounds.
            IReadOnlyList<SceneOrderEntry> order = scene.RecordedOrder;
            for (int index = 0; index < order.Count; index++)
            {
                SceneOrderEntry entry = order[index];
                DrawNode(entry.Node, context, FormattableString.Invariant($"{index}: L{entry.Layer} S{entry.SourceIndex} Y{entry.YAnchor:0.##}"));
            }
        }
        else
        {
            for (int index = 0; index < owner.LogicalChildren.Count; index++)
            {
                if (owner.LogicalChildren[index] is SceneNode2D child)
                {
                    DrawNode(child, context, $"S{index} L{child.Layer}");
                }
            }
        }
    }

    private void DrawNode(SceneNode2D node, Scene2DRecordContext context, string order)
    {
        if (node is Scene2DDebugOverlay or Collider2D || !UIElementVisibility.ParticipatesInRendering(node) || node.Opacity <= 0) { return; }
        Matrix3x2 transform = node.GetLocalTransform();
        using DrawTransformScope scope = context.Frame.Transform(transform);
        Scene2DRecordContext childContext = context.WithLocalTransform(transform);
        if (node is TileMap2D map) { DrawMap(map, childContext); return; }
        if (node is Scene2D or SceneItems2D) { DrawChildren(node, childContext); return; }
        SceneBounds2D bounds = node.GetLocalBounds();
        if (Has(Scene2DDebugFlags.Order) && bounds.Kind == SceneBoundsKind.Known && childContext.IntersectsVisibleLocalBounds(bounds))
        {
            Label(context.Frame, order, new DrawPoint(bounds.Bounds.X, bounds.Bounds.Y), Color.White);
        }
    }

    private void DrawMap(TileMap2D map, Scene2DRecordContext context)
    {
        if (map.Model is not TileMap2DModel model) { return; }
        foreach (TileLayer2D presentation in map.Layers)
        {
            if (!model.TryGetLayer(presentation.LayerId, out TileLayer2DModel? layer) || layer is null ||
                !layer.IsVisible || layer.Opacity <= 0 || presentation.Opacity <= 0 ||
                !UIElementVisibility.ParticipatesInRendering(presentation)) { continue; }
            Matrix3x2 transform = presentation.GetLocalTransform();
            using DrawTransformScope scope = context.Frame.Transform(transform);
            Scene2DRecordContext childContext = context.WithLocalTransform(transform);
            SceneBounds2D visible = childContext.GetConservativeVisibleLocalBounds();
            if (visible.Kind != SceneBoundsKind.Known) { continue; }
            DrawSize size = model.TileSize;
            IReadOnlyList<TileChunk2D> chunks = map.GetDebugChunks(layer, visible);
            diagnostics = diagnostics with { CandidateChunks = diagnostics.CandidateChunks + chunks.Count };
            foreach (TileChunk2D chunk in chunks)
            {
                DrawRect bounds = new(chunk.Origin.X * size.Width, chunk.Origin.Y * size.Height, chunk.Width * size.Width, chunk.Height * size.Height);
                if (!childContext.IntersectsVisibleLocalBounds(SceneBounds2D.Known(bounds))) { continue; }
                if (Has(Scene2DDebugFlags.ChunkBounds)) { Rectangle(context.Frame, bounds, ChunkColor); }
                if (Has(Scene2DDebugFlags.Order)) { Label(context.Frame, $"{layer.Id}: order {layer.Order} chunk {chunk.Origin.X},{chunk.Origin.Y}", new DrawPoint(bounds.X, bounds.Y), ChunkColor); }
                if (!Has(Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds)) { continue; }
                TileRange(visible.Bounds, default, size, new TileMapBounds2D(chunk.Origin.X, chunk.Origin.Y, chunk.Width, chunk.Height), out int minX, out int minY, out int maxX, out int maxY);
                for (int y = minY; y < maxY; y++)
                for (int x = minX; x < maxX; x++)
                {
                    TileCell2D cell = chunk.GetCell(new TileCoordinate2D(x, y));
                    diagnostics = diagnostics with { VisitedTiles = diagnostics.VisitedTiles + 1 };
                    string text = Has(Scene2DDebugFlags.TileCoordinates) ? $"{x},{y}" : string.Empty;
                    if (Has(Scene2DDebugFlags.TileIds)) { text += $" #{cell.TileId}"; }
                    Label(context.Frame, text, new DrawPoint(x * size.Width, y * size.Height), Color.White);
                }
            }
            if (!Has(Scene2DDebugFlags.PromotedTiles)) { continue; }
            foreach (TileInstance2D tile in presentation.PromotedTiles)
            {
                DrawRect slot = new(tile.X * size.Width, tile.Y * size.Height, size.Width, size.Height);
                SceneBounds2D actual = SceneGeometry2D.TransformBounds(tile.GetLocalBounds(), tile.GetLocalTransform());
                if (!childContext.IntersectsVisibleLocalBounds(SceneBounds2D.Known(slot)) && !childContext.IntersectsVisibleLocalBounds(actual)) { continue; }
                Rectangle(context.Frame, slot, PromotionColor);
                if (actual.Kind == SceneBoundsKind.Known)
                {
                    using (context.Frame.Transform(tile.GetLocalTransform())) { Rectangle(context.Frame, new DrawRect(0, 0, size.Width, size.Height), PromotionColor); }
                    Line(context.Frame, Center(slot), Center(actual.Bounds), PromotionColor);
                }
                Label(context.Frame, $"P {layer.Id}:{tile.X},{tile.Y}", new DrawPoint(slot.X, slot.Y), PromotionColor);
                diagnostics = diagnostics with { PromotedTiles = diagnostics.PromotedTiles + 1 };
            }
        }
    }

    private void DrawNavigation(Scene2DRecordContext context, DrawRect visible)
    {
        if (NavigationGrid is not IScene2DDebugNavigationGrid grid) { return; }
        DrawSize size = grid.CellSize;
        DrawPoint origin = grid.Origin;
        if (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width <= 0 || size.Height <= 0 ||
            !float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || grid.Bounds.Width <= 0 || grid.Bounds.Height <= 0)
        {
            throw new InvalidOperationException("Debug navigation grid must have finite positive cells and valid bounds.");
        }
        TileRange(visible, origin, size, grid.Bounds, out int minX, out int minY, out int maxX, out int maxY);
        for (int y = minY; y < maxY; y++)
        for (int x = minX; x < maxX; x++)
        {
            diagnostics = diagnostics with { NavigationCells = diagnostics.NavigationCells + 1 };
            if (!grid.TryGetCell(x, y, out bool blocked)) { continue; }
            Rectangle(context.Frame, new DrawRect(origin.X + x * size.Width, origin.Y + y * size.Height, size.Width, size.Height),
                blocked ? new Color(255, 90, 90) : new Color(100, 230, 130));
        }
    }

    private static void TileRange(DrawRect visible, DrawPoint origin, DrawSize size, TileMapBounds2D bounds,
        out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = (int)Math.Clamp(Math.Floor(((double)visible.X - origin.X) / size.Width), bounds.X, bounds.Right);
        minY = (int)Math.Clamp(Math.Floor(((double)visible.Y - origin.Y) / size.Height), bounds.Y, bounds.Bottom);
        maxX = (int)Math.Clamp(Math.Ceiling(((double)visible.Right - origin.X) / size.Width), bounds.X, bounds.Right);
        maxY = (int)Math.Clamp(Math.Ceiling(((double)visible.Bottom - origin.Y) / size.Height), bounds.Y, bounds.Bottom);
    }

    private DrawPen Pen(Color color)
    {
        if (!pens.TryGetValue(color, out DrawPen? pen) || pen.Thickness != LineThickness)
        {
            pen = new DrawPen(pen?.Brush ?? new SolidColorBrush(color), LineThickness);
            pens[color] = pen;
        }
        return pen;
    }
    private void Rectangle(RenderSurface2DFrame frame, DrawRect bounds, Color color) { frame.DrawRectangle(bounds, Pen(color)); CountPrimitive(); }
    private void Line(RenderSurface2DFrame frame, DrawPoint a, DrawPoint b, Color color) { frame.DrawLine(a, b, Pen(color)); CountPrimitive(); }
    private void Label(RenderSurface2DFrame frame, string text, DrawPoint point, Color color)
    {
        font ??= FontResolver.Default.Resolve("Consolas", 10).Font;
        frame.DrawText(new DrawTextRun(font, text, FontSize), point, color);
        CountPrimitive();
    }
    private void CountPrimitive() => diagnostics = diagnostics with { Primitives = diagnostics.Primitives + 1 };
    private static DrawPoint Center(DrawRect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    private static Color LayerColor(uint layer)
    {
        uint hash = unchecked(layer * 2654435761u);
        return new Color((byte)(80 + (hash & 127)), (byte)(100 + ((hash >> 8) & 127)), (byte)(80 + ((hash >> 16) & 127)));
    }
}
