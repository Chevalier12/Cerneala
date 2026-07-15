using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class MonoGameDrawingBackendStateTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void RenderWithBalancedClipsEndsWithEmptyClipStack()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushClip(new DrawRect(10, 10, 80, 80)));
        commands.Add(DrawCommand.PushClip(new DrawRect(20, 20, 10, 10)));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PopClip());

        InvokeClipOnlyRenderDiagnostics(backend, commands, new Rectangle(0, 0, 100, 100));

        Assert.Equal(0, backend.ClipStackDepth);
    }

    [Fact]
    public void RenderWithExtraPopDoesNotThrow()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushClip(new DrawRect(10, 10, 80, 80)));
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.PopClip());

        Exception? exception = Record.Exception(() =>
            InvokeClipOnlyRenderDiagnostics(backend, commands, new Rectangle(0, 0, 100, 100)));

        Assert.Null(exception);
        Assert.Equal(0, backend.ClipStackDepth);
    }

    [Fact]
    public void CoordinateScaleAppliesToBackendMapper()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        backend.CoordinateScale = 2;

        MonoGameDrawMapper mapper = GetBackendMapper(backend);

        Assert.Equal(new Rectangle(2, 4, 6, 8), mapper.MapRectangle(new DrawRect(1, 2, 3, 4)));
    }

    [Fact]
    public void TextTextureOriginMapsBaselineToTightTextureOrigin()
    {
        Vector2 origin = InvokeTextTexturePositionDiagnostics(
            new DrawPoint(20, 30),
            new DrawPoint(-10.5f, -16.25f),
            coordinateScale: 1);

        Assert.Equal(new Vector2(10, 14), origin);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        SetTextTextureCache(backend, CreateTextTextureCache());

        Exception? exception = Record.Exception(() =>
        {
            backend.Dispose();
            backend.Dispose();
        });

        Assert.Null(exception);
        Assert.Equal(0, backend.TextTextureCacheCount);
    }

    [Fact]
    public void DisposeClearsTextTextureCacheDiagnostics()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        SetTextTextureCache(backend, CreateTextTextureCacheWithUninitializedTexture());
        Assert.Equal(1, backend.TextTextureCacheCount);

        backend.Dispose();

        Assert.Equal(0, backend.TextTextureCacheCount);
    }

    [Fact]
    public void CompletingFrameEvictsTextTexturesNotUsedByThatFrame()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        object staleKey = AddTextTexture(cache, "old phase");
        object activeKey = AddTextTexture(cache, "current phase");
        SetTextTextureCache(backend, cache);
        MarkTextTextureActive(backend, activeKey);

        InvokeTextTextureCachePrune(backend);

        Assert.Equal(1, backend.TextTextureCacheCount);
        Assert.False(cache.Contains(staleKey));
        Assert.True(cache.Contains(activeKey));
        backend.Dispose();
    }

    [Fact]
    public void TextTextureKeySeparatesForegroundColorsWithDifferentGammaMasks()
    {
        Type keyType = TextTextureCacheField().FieldType.GetGenericArguments()[0];
        MethodInfo fromMethod = keyType.GetMethod(
            "FromWithRasterizationColor",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [typeof(DrawTextRun), typeof(float), typeof(DrawPoint), typeof(Cerneala.Drawing.Color)],
            modifiers: null)!;
        DrawTextRun textRun = new(new TestFont("Cascadia Mono", 10), "MOTION LAB", 10);

        object black = fromMethod.Invoke(null, [textRun, 1f, default(DrawPoint), Cerneala.Drawing.Color.Black])!;
        object slate = fromMethod.Invoke(null, [textRun, 1f, default(DrawPoint), new Cerneala.Drawing.Color(138, 147, 166)])!;

        Assert.NotEqual(black, slate);
    }

    private static MonoGameDrawingBackend CreateBackendShell()
    {
        MonoGameDrawingBackend backend = (MonoGameDrawingBackend)RuntimeHelpers.GetUninitializedObject(typeof(MonoGameDrawingBackend));
        backend.CoordinateScale = 1;
        return backend;
    }

    private static void InvokeClipOnlyRenderDiagnostics(MonoGameDrawingBackend backend, DrawCommandList commands, Rectangle viewport)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod(
            "RenderClipCommandsForDiagnostics",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DrawCommandList), typeof(Rectangle)],
            modifiers: null);

        Assert.True(
            method is not null,
            "Expected MonoGameDrawingBackend to expose an internal GPU-free clip render diagnostic seam.");

        method!.Invoke(backend, [commands, viewport]);
    }

    private static MonoGameDrawMapper GetBackendMapper(MonoGameDrawingBackend backend)
    {
        PropertyInfo? mapperProperty = typeof(MonoGameDrawingBackend).GetProperty("Mapper", NonPublicInstance);
        Assert.True(mapperProperty is not null, "Expected MonoGameDrawingBackend to keep mapping behind a testable Mapper seam.");

        return Assert.IsType<MonoGameDrawMapper>(mapperProperty!.GetValue(backend));
    }

    private static Vector2 InvokeTextTexturePositionDiagnostics(DrawPoint position, DrawPoint originOffset, float coordinateScale)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod(
            "MapTextTexturePositionForDiagnostics",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(DrawPoint), typeof(DrawPoint), typeof(float)],
            modifiers: null);

        Assert.True(
            method is not null,
            "Expected MonoGameDrawingBackend to expose a GPU-free text texture position diagnostic seam.");

        return Assert.IsType<Vector2>(method!.Invoke(null, [position, originOffset, coordinateScale]));
    }

    private static IDictionary CreateTextTextureCache()
    {
        return (IDictionary)Activator.CreateInstance(TextTextureCacheField().FieldType)!;
    }

    private static IDictionary CreateTextTextureCacheWithUninitializedTexture()
    {
        FieldInfo cacheField = TextTextureCacheField();
        IDictionary cache = (IDictionary)Activator.CreateInstance(cacheField.FieldType)!;
        Type keyType = cacheField.FieldType.GetGenericArguments()[0];
        Type valueType = cacheField.FieldType.GetGenericArguments()[1];
        object key = Activator.CreateInstance(keyType)!;
        object redTexture = RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object greenTexture = RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object blueTexture = RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object maskTexture = RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object value = Activator.CreateInstance(
            valueType,
            redTexture,
            greenTexture,
            blueTexture,
            maskTexture,
            default(DrawPoint))!;

        cache.Add(key, value);
        return cache;
    }

    private static object AddTextTexture(IDictionary cache, string text)
    {
        Type keyType = cache.GetType().GetGenericArguments()[0];
        MethodInfo fromMethod = keyType.GetMethod(
            "From",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [typeof(DrawTextRun), typeof(float), typeof(DrawPoint)],
            modifiers: null)!;
        DrawTextRun textRun = new(new TestFont("Arial", 16), text, 16);
        object key = fromMethod.Invoke(null, [textRun, 1f, default(DrawPoint)])!;
        Type valueType = cache.GetType().GetGenericArguments()[1];
        object value = Activator.CreateInstance(
            valueType,
            RuntimeHelpers.GetUninitializedObject(typeof(Texture2D)),
            RuntimeHelpers.GetUninitializedObject(typeof(Texture2D)),
            RuntimeHelpers.GetUninitializedObject(typeof(Texture2D)),
            RuntimeHelpers.GetUninitializedObject(typeof(Texture2D)),
            default(DrawPoint))!;
        cache.Add(key, value);
        return key;
    }

    private static void MarkTextTextureActive(MonoGameDrawingBackend backend, object key)
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField("activeTextTextureKeys", NonPublicInstance);
        Assert.True(field is not null, "Expected the backend to track text textures used by the current frame.");
        object activeKeys = Activator.CreateInstance(field!.FieldType)!;
        field.SetValue(backend, activeKeys);
        activeKeys.GetType().GetMethod("Add")!.Invoke(activeKeys, [key]);
    }

    private static void InvokeTextTextureCachePrune(MonoGameDrawingBackend backend)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod("PruneInactiveTextTextureCaches", NonPublicInstance);
        Assert.True(method is not null, "Expected the backend to evict text textures unused by the current frame.");
        method!.Invoke(backend, null);
    }

    private static void SetTextTextureCache(MonoGameDrawingBackend backend, IDictionary cache)
    {
        TextTextureCacheField().SetValue(backend, cache);
    }

    private static FieldInfo TextTextureCacheField()
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField("_textTextureCache", NonPublicInstance);
        Assert.True(field is not null, "Expected MonoGameDrawingBackend to keep an internal text texture cache.");
        return field!;
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;
}
