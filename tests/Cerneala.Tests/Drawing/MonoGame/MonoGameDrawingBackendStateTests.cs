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

    private static IDictionary CreateTextTextureCache()
    {
        return (IDictionary)Activator.CreateInstance(TextTextureCacheField().FieldType)!;
    }

    private static IDictionary CreateTextTextureCacheWithUninitializedTexture()
    {
        FieldInfo cacheField = TextTextureCacheField();
        IDictionary cache = (IDictionary)Activator.CreateInstance(cacheField.FieldType)!;
        Type keyType = cacheField.FieldType.GetGenericArguments()[0];
        object key = Activator.CreateInstance(keyType)!;
        object texture = RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));

        cache.Add(key, texture);
        return cache;
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
}
