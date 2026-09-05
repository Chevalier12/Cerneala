using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

/// <summary>
/// Executable Stage 0 contracts. Reflection allows the missing API to be the RED
/// assertion rather than a compilation failure. Import cases move to the optional
/// importer test project when that project is introduced in Stage 2.
/// </summary>
public sealed class SceneImportStageZeroContractTests
{
    [Fact]
    [Trait("SceneImportStage", "0")]
    public void OriginalFixtureCorpusHasIndependentGoldenAndEveryDiagnosticCategory()
    {
        using JsonDocument golden = JsonDocument.Parse(File.ReadAllText(Fixture("common.golden.json")));
        Assert.Equal([16, 16], golden.RootElement.GetProperty("tileSize").EnumerateArray().Select(static value => value.GetInt32()));
        Assert.Equal(2, golden.RootElement.GetProperty("layers").GetArrayLength());
        object[][] cases = DiagnosticCases().ToArray();
        Assert.Equal(15, cases.Length);
        Assert.Equal(["Error", "Fatal", "Unsupported", "Warning"], cases.Select(static value => (string)value[3]).Distinct().Order());
        Assert.All(cases, static value => Assert.True(File.Exists(Fixture((string)value[0]))));
        Assert.True(File.Exists(Fixture("atlas.svg")));
    }

    [Theory]
    [InlineData(false, "SCN2D007")]
    [InlineData(true, "SCN2D010")]
    [Trait("SceneImportStage", "0")]
    public void ProgrammaticModelsUseTheSameAtlasValidator(bool missingAtlas, string code)
    {
        Type validator = CoreType("Scene2DModelValidator");
        TileDefinition2D tile = new(1, new DrawRect(16, 0, 32, 16));
        TileSet2D tileset = new("Atlas", new ResourceId<ImageResource>("Atlas"), [tile]);
        TileMap2DModel model = new(new DrawSize(16, 16), [tileset], []);
        IReadOnlyDictionary<string, DrawSize> atlases = missingAtlas
            ? new Dictionary<string, DrawSize>()
            : new Dictionary<string, DrawSize> { ["Atlas"] = new DrawSize(32, 16) };
        object result = InvokeStatic(validator, "Validate", model, atlases);
        Assert.False(Value<bool>(result, "Success"));
        Assert.Contains(Items(result, "Diagnostics"), item => Value<string>(item, "Code") == code);
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void DebugOverlayDoesNotChangeCollisionOrRealPointerRouting()
    {
        SceneNode2D overlay = Assert.IsAssignableFrom<SceneNode2D>(Activator.CreateInstance(CoreType("Scene2DDebugOverlay")));
        UIRoot root = new(100, 100);
        RenderSurface2D surface = new();
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        Scene2D scene = new();
        BoxCollider2D box = new() { Width = 20, Height = 20, TranslateX = 10, TranslateY = 10 };
        scene.Children.Add(box);
        scene.Children.Add(overlay);
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        int presses = 0;
        box.AddHandler(InputEvents.MouseDownEvent, (_, _) => presses++);
        ElementInputBridge bridge = new();
        CollisionHit2D[] before = scene.CollisionWorld.Raycast(new Vector2(0, 20), Vector2.UnitX, 50);
        // Hits are immutable reference objects, not value-equality records.
        // Even an unchanged repeated query returns fresh hit objects.
        CollisionHit2D[] repeated = scene.CollisionWorld.Raycast(new Vector2(0, 20), Vector2.UnitX, 50);
        Assert.NotSame(before[0], repeated[0]);
        Assert.Equal(before.Select(HitValue), repeated.Select(HitValue));
        bridge.Dispatch(root, PointerFrame(false, true));
        Assert.Equal(1, presses);
        bridge.Dispatch(root, PointerFrame(true, false));
        Set(overlay, "Flags", Enum.Parse(CoreType("Scene2DDebugFlags"), "All"));
        CollisionHit2D[] after = scene.CollisionWorld.Raycast(new Vector2(0, 20), Vector2.UnitX, 50);
        bridge.Dispatch(root, PointerFrame(false, true));
        Assert.Equal(before.Select(HitValue), after.Select(HitValue));
        Assert.Equal(2, presses);
        Assert.False(overlay.IsHitTestVisible);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(1f)]
    [Trait("SceneImportStage", "0")]
    public void SegmentColliderIsTwoSidedAndDoesNotPermitFastTraversal(float direction)
    {
        Collider2D segment = Assert.IsAssignableFrom<Collider2D>(Activator.CreateInstance(CoreType("SegmentCollider2D")));
        Set(segment, "EndY", 20f);
        Set(segment, "EndX", 0f);
        segment.TranslateY = -10;
        CircleCollider2D mover = new() { Radius = 1, TranslateX = -20 * direction };
        Scene2D scene = new();
        scene.Children.Add(segment);
        scene.Children.Add(mover);
        MoveCollisionResult2D movement = scene.CollisionWorld.MoveAndCollide(mover, new Vector2(40 * direction, 0));
        Assert.NotNull(movement.Collision);
        Assert.Same(segment, movement.Collision.Collider);
        Assert.InRange(MathF.Abs(movement.Travel.X), 18.99f, 19.01f);
    }

    [Fact]
    [Trait("SceneImportStage", "0")]
    public void CoreDataAndValidationTypesNeverBecomeUiObjectsOrOwnEffects()
    {
        foreach (string name in new[] { "Scene2DDocument", "Scene2DLevel", "Scene2DAsset", "Scene2DEntity", "TilePromotion2D", "Scene2DModelValidator", "Scene2DDiagnostic" })
        {
            Type type = CoreType(name);
            Assert.False(typeof(UIElement).IsAssignableFrom(type));
            Assert.Null(type.GetProperty("Aspect"));
            Assert.Null(type.GetProperty("Motion"));
            Assert.Null(type.GetProperty("Prism"));
        }
    }

    public static IEnumerable<object[]> DiagnosticCases()
    {
        using JsonDocument cases = JsonDocument.Parse(File.ReadAllText(Fixture("diagnostic-cases.json")));
        return cases.RootElement.EnumerateArray().Select(static item => new object[]
        {
            item.GetProperty("file").GetString()!, item.GetProperty("format").GetString()!,
            item.GetProperty("code").GetString()!, item.GetProperty("category").GetString()!
        }).ToArray();
    }

    private static object InvokeStatic(Type type, string name, params object[] arguments)
    {
        MethodInfo? method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => candidate.Name == name && candidate.GetParameters().Length >= arguments.Length)
            .FirstOrDefault(candidate => arguments.Select((argument, index) => candidate.GetParameters()[index].ParameterType.IsInstanceOfType(argument)).All(static valid => valid));
        Assert.NotNull(method);
        object?[] invocation = method.GetParameters().Select(static parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
        Array.Copy(arguments, invocation, arguments.Length);
        try { return method.Invoke(null, invocation)!; }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw(); throw; }
    }

    private static Type CoreType(string name)
    {
        Type? type = typeof(Scene2D).Assembly.GetType("Cerneala.UI.Controls." + name);
        Assert.True(type is not null, "RED: core scene import/validation/overlay contract is absent: " + name);
        return type;
    }

    private static object? Property(object value, string name)
    {
        PropertyInfo? property = value.GetType().GetProperty(name);
        Assert.NotNull(property);
        return property.GetValue(value);
    }

    private static object RequiredValue(object value, string name)
    {
        object? result = Property(value, name);
        Assert.NotNull(result);
        return result;
    }

    private static T Value<T>(object value, string name) => Assert.IsType<T>(RequiredValue(value, name));
    private static object[] Items(object value, string name) => Assert.IsAssignableFrom<IEnumerable>(RequiredValue(value, name)).Cast<object>().ToArray();
    private static string DiagnosticsText(object result) => string.Join("; ", Items(result, "Diagnostics").Select(static item => Value<string>(item, "Message")));

    private static void Set(object target, string propertyName, object value)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        property.SetValue(target, value);
    }

    private static InputFrame PointerFrame(bool previousDown, bool currentDown) => new(
        PointerSnapshot.Empty.WithPosition(20, 20).WithButton(InputMouseButton.Left, previousDown),
        PointerSnapshot.Empty.WithPosition(20, 20).WithButton(InputMouseButton.Left, currentDown),
        KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

    private static object HitValue(CollisionHit2D hit) =>
        (hit.Collider, hit.Entity, hit.Point, hit.Normal, hit.Distance, hit.Fraction, hit.IsTrigger);

    private static string Fixture(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { directory = directory.Parent; }
        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "tests", "Fixtures", "Scene2DImport", name.Replace('/', Path.DirectorySeparatorChar));
    }
}
