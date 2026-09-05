using System.Numerics;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class CollisionStageZeroContractTests
{
    private const string Namespace = "Cerneala.UI.Controls.";

    [Fact]
    [Trait("CollisionStage", "0")]
    public void PublicColliderAndQueryContractExists()
    {
        string[] expectedTypes =
        [
            "Collider2D",
            "BoxCollider2D",
            "CircleCollider2D",
            "PolygonCollider2D",
            "CollisionWorld2D",
            "CollisionQuery2D",
            "CollisionHit2D",
            "MoveCollisionResult2D",
            "CollisionWorld2DDiagnosticsSnapshot"
        ];
        IReadOnlyDictionary<string, Type?> resolved = expectedTypes.ToDictionary(static name => name, Resolve);
        string[] missing = resolved.Where(static pair => pair.Value is null).Select(static pair => pair.Key).ToArray();

        Assert.True(missing.Length == 0, "RED: approved collision API is absent: " + string.Join(", ", missing));
        Assert.True(typeof(SceneNode2D).IsAssignableFrom(resolved["Collider2D"]));
        Assert.True(resolved["Collider2D"]!.IsAssignableFrom(resolved["BoxCollider2D"]));
        Assert.True(resolved["Collider2D"]!.IsAssignableFrom(resolved["CircleCollider2D"]));
        Assert.True(resolved["Collider2D"]!.IsAssignableFrom(resolved["PolygonCollider2D"]));
        RequireProperties(resolved["Collider2D"]!, "Enabled", "IsTrigger", "OffsetX", "OffsetY", "CollisionLayer", "CollisionMask");
        RequireProperties(resolved["BoxCollider2D"]!, "Width", "Height");
        RequireProperties(resolved["CircleCollider2D"]!, "Radius");
        RequireProperties(resolved["PolygonCollider2D"]!, "Points", "Vertices");
        RequireProperties(resolved["CollisionHit2D"]!, "Collider", "Entity", "Point", "Normal", "Distance", "Fraction", "IsTrigger");
        RequireProperties(resolved["MoveCollisionResult2D"]!, "RequestedDisplacement", "Travel", "Remainder", "Collision", "TriggerHits");
        RequireMethods(resolved["CollisionWorld2D"]!, "Intersects", "Overlap", "Raycast", "MoveAndCollide", "GetDiagnosticsSnapshot");
        Assert.NotNull(typeof(Scene2D).GetProperty("CollisionWorld", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void ColliderUiPropertiesFreezeBindableAspectMotionAndDiscretePolicy()
    {
        Type collider = RequireType("Collider2D");
        foreach (string name in new[] { "Enabled", "IsTrigger", "OffsetX", "OffsetY", "CollisionLayer", "CollisionMask" })
        {
            Assert.NotNull(collider.GetField(name + "Property", BindingFlags.Public | BindingFlags.Static));
        }
        Type box = RequireType("BoxCollider2D");
        Assert.NotNull(box.GetField("WidthProperty", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(box.GetField("HeightProperty", BindingFlags.Public | BindingFlags.Static));
        Type circle = RequireType("CircleCollider2D");
        Assert.NotNull(circle.GetField("RadiusProperty", BindingFlags.Public | BindingFlags.Static));

        Assert.True(
            Resolve("CollisionWorld2D")!.GetProperty("Version", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is not null,
            "The shared mutation path must expose an internal version for query/hit-test coherence tests.");
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void ShapeValidationAndContactRulesAreExecutableContracts()
    {
        UIElement box = CreateCollider("BoxCollider2D", ("Width", 10f), ("Height", 10f));
        UIElement touching = CreateCollider("CircleCollider2D", ("Radius", 2f), ("TranslateX", 12f), ("TranslateY", 5f));
        Scene2D scene = SceneWith(box, touching);
        object world = GetWorld(scene);

        Assert.True((bool)Invoke(world, "Intersects", box, touching)!);

        UIElement zeroRadius = CreateCollider("CircleCollider2D");
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Set(zeroRadius, "Radius", 0f));

        UIElement concave = CreateCollider("PolygonCollider2D");
        Assert.ThrowsAny<ArgumentException>(() => Set(concave, "Points", "0,0 10,0 5,5 10,10 0,10"));
        Assert.ThrowsAny<ArgumentException>(() => Set(concave, "Points", "0,0 1,1 2,2"));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Set(box, "Width", float.NaN));
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void BilateralLayerMaskZeroAllTriggerAndStableOrderAreFrozen()
    {
        UIElement source = CreateCollider(
            "BoxCollider2D",
            ("Width", 10f), ("Height", 10f), ("CollisionLayer", 1u), ("CollisionMask", 2u));
        UIElement accepted = CreateCollider(
            "BoxCollider2D",
            ("Width", 10f), ("Height", 10f), ("CollisionLayer", 2u), ("CollisionMask", 1u));
        UIElement rejected = CreateCollider(
            "BoxCollider2D",
            ("Width", 10f), ("Height", 10f), ("CollisionLayer", 4u), ("CollisionMask", uint.MaxValue));
        UIElement trigger = CreateCollider(
            "BoxCollider2D",
            ("Width", 10f), ("Height", 10f), ("CollisionLayer", 2u), ("CollisionMask", 1u), ("IsTrigger", true));
        Scene2D scene = SceneWith(source, accepted, rejected, trigger);
        object world = GetWorld(scene);

        Array overlap = Assert.IsAssignableFrom<Array>(Invoke(world, "Overlap", source)!);
        UIElement[] colliders = overlap.Cast<object>().Select(GetHitCollider).ToArray();
        Assert.Equal([accepted, trigger], colliders);

        Set(accepted, "CollisionLayer", 0u);
        overlap = Assert.IsAssignableFrom<Array>(Invoke(world, "Overlap", source)!);
        Assert.Equal([trigger], overlap.Cast<object>().Select(GetHitCollider));

        object move = Invoke(world, "MoveAndCollide", source, new Vector2(20, 0))!;
        Assert.Same(trigger, Assert.Single(GetEnumerableProperty(move, "TriggerHits").Cast<object>()).GetType()
            .GetProperty("Collider")!.GetValue(Assert.Single(GetEnumerableProperty(move, "TriggerHits").Cast<object>())));
        Assert.Null(move.GetType().GetProperty("Collision")!.GetValue(move));
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void NestedTransformOffsetMutationAndLifecycleUpdateOneWorld()
    {
        Scene2D group = new() { TranslateX = 20, TranslateY = -10, ScaleX = 2, ScaleY = 2 };
        UIElement collider = CreateCollider("BoxCollider2D", ("Width", 5f), ("Height", 6f), ("OffsetX", 3f));
        group.Children.Add((SceneNode2D)collider);
        Scene2D scene = SceneWith(group);
        object world = GetWorld(scene);

        Array initial = Assert.IsAssignableFrom<Array>(Invoke(world, "Raycast", new Vector2(25, -4), Vector2.UnitX, 30f)!);
        Assert.NotEmpty(initial.Cast<object>());
        long version = Convert.ToInt64(world.GetType().GetProperty("Version", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(world));

        Set(collider, "OffsetX", 10f);
        Assert.True(Convert.ToInt64(world.GetType().GetProperty("Version", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(world)) > version);
        group.Children.Remove((SceneNode2D)collider);
        Assert.Empty(Assert.IsAssignableFrom<Array>(Invoke(world, "Raycast", new Vector2(25, -4), Vector2.UnitX, 30f)!).Cast<object>());
        group.Children.Add((SceneNode2D)collider);
        Assert.NotEmpty(Assert.IsAssignableFrom<Array>(Invoke(world, "Raycast", new Vector2(25, -4), Vector2.UnitX, 60f)!).Cast<object>());
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void RaycastMoveAndCollideAndFastMotionExposeDeterministicContactData()
    {
        UIElement mover = CreateCollider("CircleCollider2D", ("Radius", 2f), ("TranslateX", -100f));
        UIElement wall = CreateCollider("BoxCollider2D", ("Width", 2f), ("Height", 20f));
        Scene2D scene = SceneWith(mover, wall);
        object world = GetWorld(scene);

        Array rays = Assert.IsAssignableFrom<Array>(Invoke(world, "Raycast", new Vector2(-20, 5), Vector2.UnitX, 40f)!);
        object ray = Assert.Single(rays.Cast<object>());
        Assert.Same(wall, GetHitCollider(ray));
        Assert.InRange(Convert.ToSingle(ray.GetType().GetProperty("Fraction")!.GetValue(ray)), 0f, 1f);

        object movement = Invoke(world, "MoveAndCollide", mover, new Vector2(200, 0))!;
        object? collisionValue = movement.GetType().GetProperty("Collision")!.GetValue(movement);
        Assert.NotNull(collisionValue);
        object collision = collisionValue;
        Assert.Same(wall, GetHitCollider(collision));
        Vector2 travel = Assert.IsType<Vector2>(movement.GetType().GetProperty("Travel")!.GetValue(movement));
        Assert.True(travel.X < 200, "Continuous movement must not tunnel through the wall.");
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void PromotedTileColliderHasReplacementLifecycleAndDoorStateContract()
    {
        Type tile = typeof(TileInstance2D);
        RequireProperties(tile, "Colliders", "ReplacesImportedColliders");
        RequireMethods(typeof(TileMap2D), "Promote", "Demote");
        Type descriptor = RequireType("TileColliderDescriptor2D");
        RequireProperties(descriptor, "Shape", "OffsetX", "OffsetY", "CollisionLayer", "CollisionMask", "IsTrigger");

        UIElement door = CreateCollider("BoxCollider2D", ("Width", 16f), ("Height", 4f), ("Enabled", true));
        Set(door, "Enabled", false);
        Assert.False((bool)door.GetType().GetProperty("Enabled")!.GetValue(door)!);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void SceneNodeUsesRealUiRouteForTunnelBubbleHoverCaptureFocusCursorAndCommand()
    {
        UIRoot root = new(200, 200);
        RenderSurface2D surface = new();
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        Scene2D scene = new();
        UIElement collider = CreateCollider("BoxCollider2D", ("Width", 30f), ("Height", 30f), ("TranslateX", 10f), ("TranslateY", 10f));
        scene.Children.Add((SceneNode2D)collider);
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        List<string> route = [];
        root.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => route.Add("preview-root"));
        surface.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => route.Add("preview-surface"));
        scene.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => route.Add("preview-scene"));
        collider.AddHandler(InputEvents.PreviewMouseDownEvent, (_, _) => route.Add("preview-node"));
        collider.AddHandler(InputEvents.MouseDownEvent, (_, _) => route.Add("bubble-node"));
        scene.AddHandler(InputEvents.MouseDownEvent, (_, _) => route.Add("bubble-scene"));
        surface.AddHandler(InputEvents.MouseDownEvent, (_, _) => route.Add("bubble-surface"));
        root.AddHandler(InputEvents.MouseDownEvent, (_, _) => route.Add("bubble-root"));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 20, down: true));

        Assert.Equal(
            ["preview-root", "preview-surface", "preview-scene", "preview-node", "bubble-node", "bubble-scene", "bubble-surface", "bubble-root"],
            route);
        Assert.True(collider.IsPointerOver);
        ElementInputRouteMap map = root.InputCache.EnsureCurrent(root);
        Assert.True(map.TryGetId(collider, out UiElementId id));
        Assert.False(string.IsNullOrEmpty(id.Value));
        bridge.PointerCaptureManager.Capture(collider, map);
        scene.Children.Remove((SceneNode2D)collider);
        bridge.Dispatch(root, PointerFrame(20, 20));
        Assert.Null(bridge.PointerCaptureManager.CapturedElement);
        Assert.False(collider.IsPointerOver);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void SceneNodeUsesExistingWheelHandledEnterLeaveAndCursorServices()
    {
        UIRoot root = SceneRootWithCollider(out _, out Scene2D scene, out UIElement collider);
        bool sceneWheel = false;
        bool rootHandledToo = false;
        int enters = 0;
        int leaves = 0;
        collider.Cursor = Cursor.Crosshair;
        collider.AddHandler(InputEvents.MouseWheelEvent, (_, args) => args.Handled = true);
        scene.AddHandler(InputEvents.MouseWheelEvent, (_, _) => sceneWheel = true);
        root.AddHandler(InputEvents.MouseWheelEvent, (_, _) => rootHandledToo = true, handledEventsToo: true);
        collider.AddHandler(InputEvents.MouseEnterEvent, (_, _) => enters++);
        collider.AddHandler(InputEvents.MouseLeaveEvent, (_, _) => leaves++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 20));
        bridge.Dispatch(root, PointerWheelFrame(20, 20, 120));
        Assert.Equal(Cursor.Crosshair, new CursorService().Resolve(root, 20, 20));
        bridge.Dispatch(root, PointerFrame(150, 150));

        Assert.False(sceneWheel);
        Assert.True(rootHandledToo);
        Assert.Equal(1, enters);
        Assert.Equal(1, leaves);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void FocusKeyboardTextAndAncestorCommandUseTheSameSceneRoute()
    {
        UIRoot root = SceneRootWithCollider(out _, out Scene2D scene, out UIElement collider);
        collider.Focusable = true;
        int commandExecutions = 0;
        string? text = null;
        scene.InputBindings.Add(new KeyBinding(new ActionCommand(_ => commandExecutions++), InputKey.Enter));
        collider.AddHandler(InputEvents.TextInputEvent, (_, args) => text = ((TextCompositionEventArgs)args).Text);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(20, 20, down: true));
        bridge.Dispatch(root, KeyPressFrame(InputKey.Enter));
        bridge.Dispatch(root, TextFrame("door"));

        Assert.Same(collider, bridge.FocusManager.FocusedElement);
        Assert.Equal(1, commandExecutions);
        Assert.Equal("door", text);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void ExistingVisualOverlayStillWinsBeforeSceneGeometry()
    {
        UIRoot root = new(100, 100);
        RenderSurface2D surface = new();
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        Button overlay = new();
        overlay.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(surface);
        root.VisualChildren.Add(overlay);
        int overlayClicks = 0;
        int surfaceClicks = 0;
        overlay.AddHandler(InputEvents.MouseDownEvent, (_, _) => overlayClicks++);
        surface.AddHandler(InputEvents.MouseDownEvent, (_, _) => surfaceClicks++);

        new ElementInputBridge().Dispatch(root, PointerFrame(10, 10, down: true));

        Assert.Equal(1, overlayClicks);
        Assert.Equal(0, surfaceClicks);
    }

    [Fact]
    [Trait("CollisionStage", "0")]
    public void SharedMouseCoordinatesAndNonInvertibleViewBoxHaveOnePublicPath()
    {
        MethodInfo? getPosition = typeof(MouseEventArgs).GetMethod("GetPosition", [typeof(UIElement)]);
        Assert.NotNull(getPosition);
        Assert.Equal(typeof(Vector2), getPosition.ReturnType);
        RequireMethods(typeof(RenderSurface2D), "TryRootToScene", "SceneToRoot");
    }

    private static Scene2D SceneWith(params UIElement[] nodes)
    {
        Scene2D scene = new();
        foreach (UIElement node in nodes)
        {
            scene.Children.Add((SceneNode2D)node);
        }
        return scene;
    }

    private static UIRoot SceneRootWithCollider(
        out RenderSurface2D surface,
        out Scene2D scene,
        out UIElement collider)
    {
        UIRoot root = new(200, 200);
        surface = new RenderSurface2D();
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        scene = new Scene2D();
        collider = CreateCollider("BoxCollider2D", ("Width", 30f), ("Height", 30f), ("TranslateX", 10f), ("TranslateY", 10f));
        scene.Children.Add((SceneNode2D)collider);
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        return root;
    }

    private static object GetWorld(Scene2D scene)
    {
        object? world = typeof(Scene2D).GetProperty("CollisionWorld")!.GetValue(scene);
        Assert.NotNull(world);
        return world;
    }

    private static UIElement CreateCollider(string name, params (string Name, object Value)[] properties)
    {
        Type type = RequireType(name);
        UIElement element = Assert.IsAssignableFrom<UIElement>(Activator.CreateInstance(type));
        foreach ((string property, object value) in properties)
        {
            Set(element, property, value);
        }
        return element;
    }

    private static object? Invoke(object target, string method, params object[] arguments)
    {
        MethodInfo? selected = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(candidate => candidate.Name == method)
            .Where(candidate => candidate.GetParameters().Length >= arguments.Length)
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();
        Assert.NotNull(selected);
        object?[] invocation = new object?[selected.GetParameters().Length];
        Array.Copy(arguments, invocation, arguments.Length);
        ParameterInfo[] parameters = selected.GetParameters();
        for (int index = arguments.Length; index < invocation.Length; index++)
        {
            invocation[index] = parameters[index].HasDefaultValue ? parameters[index].DefaultValue : null;
        }
        return selected.Invoke(target, invocation);
    }

    private static void Set(object target, string property, object value)
    {
        try
        {
            target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.SetValue(target, value);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static UIElement GetHitCollider(object hit) =>
        Assert.IsAssignableFrom<UIElement>(hit.GetType().GetProperty("Collider")!.GetValue(hit));

    private static System.Collections.IEnumerable GetEnumerableProperty(object target, string property) =>
        Assert.IsAssignableFrom<System.Collections.IEnumerable>(target.GetType().GetProperty(property)!.GetValue(target));

    private static Type RequireType(string name)
    {
        Type? type = Resolve(name);
        Assert.NotNull(type);
        return type;
    }

    private static Type? Resolve(string name) => typeof(SceneNode2D).Assembly.GetType(Namespace + name);

    private static void RequireProperties(Type type, params string[] names)
    {
        string[] missing = names.Where(name => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is null).ToArray();
        Assert.True(missing.Length == 0, $"{type.Name} is missing properties: {string.Join(", ", missing)}");
    }

    private static void RequireMethods(Type type, params string[] names)
    {
        string[] missing = names.Where(name => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(method => method.Name != name)).ToArray();
        Assert.True(missing.Length == 0, $"{type.Name} is missing methods: {string.Join(", ", missing)}");
    }

    private static InputFrame PointerFrame(float x, float y, bool down = false)
    {
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (down)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }
        return new InputFrame(PointerSnapshot.Empty.WithPosition(x, y), current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerWheelFrame(float x, float y, int wheelValue) => new(
        PointerSnapshot.Empty.WithPosition(x, y),
        PointerSnapshot.Empty.WithPosition(x, y).WithWheelValue(wheelValue),
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.Empty,
        []);

    private static InputFrame KeyPressFrame(InputKey key) => new(
        PointerSnapshot.Empty,
        PointerSnapshot.Empty,
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.FromDownKeys([key]),
        []);

    private static InputFrame TextFrame(string text) => new(
        PointerSnapshot.Empty,
        PointerSnapshot.Empty,
        KeyboardSnapshot.Empty,
        KeyboardSnapshot.Empty,
        [new TextInputSnapshotEvent(text)]);
}
