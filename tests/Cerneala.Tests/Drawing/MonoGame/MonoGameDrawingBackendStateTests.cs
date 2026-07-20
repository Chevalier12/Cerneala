using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class MonoGameDrawingBackendStateTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Fact]
    public void RenderOwnsSpriteBatchAcrossConsecutiveFramesWithoutPrism()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        Cerneala.Drawing.Color expected = new(42, 96, 173);
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 96, 64), expected));

        for (int frame = 0; frame < 2; frame++)
        {
            fixture.Session.BeginFrame(Cerneala.Drawing.Color.Black);
            RenderBackend(fixture.Session.DrawingBackend, commands);
            fixture.Session.Present();
        }

        PresentationParameters parameters = fixture.Session.GraphicsDevice.PresentationParameters;
        Microsoft.Xna.Framework.Color[] pixels =
            new Microsoft.Xna.Framework.Color[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        Microsoft.Xna.Framework.Color actual =
            pixels[((parameters.BackBufferHeight / 2) * parameters.BackBufferWidth) + (parameters.BackBufferWidth / 2)];
        Assert.InRange(Math.Abs(actual.R - expected.R), 0, 2);
        Assert.InRange(Math.Abs(actual.G - expected.G), 0, 2);
        Assert.InRange(Math.Abs(actual.B - expected.B), 0, 2);
    }

    [Fact]
    public void RenderRestoresCompleteDeviceStateAfterSuccess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using DeviceStateFixture state = new(fixture.Session.GraphicsDevice);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 16, 16),
            new Cerneala.Drawing.Color(20, 40, 80)));

        RenderBackend(fixture.Session.DrawingBackend, commands);

        state.AssertRestored();
    }

    [Fact]
    public void RenderRestoresCompleteDeviceStateWhenACommandThrows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using DeviceStateFixture state = new(fixture.Session.GraphicsDevice);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 16, 16),
            new Cerneala.Drawing.Color(20, 40, 80)));
        commands.Add(DrawCommand.DrawImage(
            new UnsupportedImage(),
            new DrawRect(0, 0, 8, 8),
            Cerneala.Drawing.Color.White));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RenderBackend(fixture.Session.DrawingBackend, commands));

        state.AssertRestored();
        Assert.Contains("DrawImage requires a MonoGameImage", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BackendDisposeDoesNotDisposeBorrowedGraphicsResources()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using SpriteBatch spriteBatch = new(fixture.Session.GraphicsDevice);
        using Texture2D whitePixel = new(fixture.Session.GraphicsDevice, 1, 1);
        whitePixel.SetData([Microsoft.Xna.Framework.Color.White]);
        MonoGameDrawingBackend backend = new(spriteBatch, whitePixel);

        backend.Dispose();

        Assert.False(spriteBatch.IsDisposed);
        Assert.False(whitePixel.IsDisposed);
        Assert.False(fixture.Session.GraphicsDevice.IsDisposed);
    }

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
    public void CompletingFrameRetainsTextTexturesNotUsedByThatFrame()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture retained = AddTextTexture(cache, "retained");
        TextTextureFixture active = AddTextTexture(cache, "active");
        SetTextTextureCache(backend, cache);
        MarkTextTextureActive(backend, active.Key);

        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);

        Assert.Equal(2, backend.TextTextureCacheCount);
        Assert.True(cache.Contains(retained.Key));
        Assert.True(cache.Contains(active.Key));
        backend.Dispose();
    }

    [Fact]
    public void TextTextureCacheReturnsToAWithoutMissAfterABASwitch()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture first = AddTextTexture(cache, "A");
        SetTextTextureCache(backend, cache);

        Assert.True(UseTextTextureForDiagnostics(backend, first.Key));
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);
        ClearActiveTextTextureKeys(backend);
        TextTextureFixture second = AddTextTexture(cache, "B");
        Assert.True(UseTextTextureForDiagnostics(backend, second.Key));
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);
        ClearActiveTextTextureKeys(backend);
        Assert.True(UseTextTextureForDiagnostics(backend, first.Key));
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);

        Assert.True(cache.Contains(first.Key));
        Assert.True(cache.Contains(second.Key));
        Assert.Equal(2, backend.TextTextureCacheCount);
        Assert.Equal(3, backend.TextTextureCacheDiagnostics.Hits);
        backend.Dispose();
    }

    [Fact]
    public void TextTextureCacheEvictsLeastRecentlyUsedEntryAtBound()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture first = AddTextTexture(cache, "A");
        TextTextureFixture second = AddTextTexture(cache, "B");
        SetTextTextureCache(backend, cache);

        MarkTextTextureActive(backend, first.Key);
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);
        ClearActiveTextTextureKeys(backend);
        MarkTextTextureActive(backend, second.Key);
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);
        ClearActiveTextTextureKeys(backend);
        MarkTextTextureActive(backend, first.Key);
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);
        ClearActiveTextTextureKeys(backend);
        TextTextureFixture third = AddTextTexture(cache, "C");
        MarkTextTextureActive(backend, third.Key);
        CompleteTextTextureFrame(backend, maximumEntries: 2, maximumBytes: long.MaxValue);

        Assert.True(cache.Contains(first.Key));
        Assert.False(cache.Contains(second.Key));
        Assert.True(cache.Contains(third.Key));
        Assert.Equal(2, backend.TextTextureCacheCount);
        Assert.True(second.Red.IsDisposed);
        Assert.True(second.Green.IsDisposed);
        Assert.True(second.Blue.IsDisposed);
        Assert.True(second.Mask.IsDisposed);
        backend.Dispose();
    }

    [Fact]
    public void TextTextureEvictionDisposesRgbMaskAndDependentBrushTexture()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture first = AddTextTexture(cache, "A");
        TextTextureFixture second = AddTextTexture(cache, "B");
        SetTextTextureCache(backend, cache);
        Texture2D dependentBrushTexture = AddTextBrushTexture(backend, first.Key);
        MarkTextTextureActive(backend, second.Key);

        CompleteTextTextureFrame(backend, maximumEntries: 1, maximumBytes: long.MaxValue);

        Assert.False(cache.Contains(first.Key));
        Assert.True(first.Red.IsDisposed);
        Assert.True(first.Green.IsDisposed);
        Assert.True(first.Blue.IsDisposed);
        Assert.True(first.Mask.IsDisposed);
        Assert.True(dependentBrushTexture.IsDisposed);
        Assert.Empty(TextBrushTextureCache(backend));
        backend.Dispose();
    }

    [Fact]
    public void SharedTextAtlasIsDisposedOnlyAfterItsLastCacheEntryIsEvicted()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        Texture2D atlas = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object firstKey = AddTextTextureWithSharedRgb(cache, "atlas-A", atlas);
        object secondKey = AddTextTextureWithSharedRgb(cache, "atlas-B", atlas);
        SetTextTextureCache(backend, cache);
        SetSharedTextTextureReferenceCount(backend, atlas, references: 2);
        MarkTextTextureActive(backend, secondKey);

        CompleteTextTextureFrame(backend, maximumEntries: 1, maximumBytes: long.MaxValue);

        Assert.False(cache.Contains(firstKey));
        Assert.True(cache.Contains(secondKey));
        Assert.False(atlas.IsDisposed);

        ClearActiveTextTextureKeys(backend);
        TextTextureFixture third = AddTextTexture(cache, "atlas-C");
        MarkTextTextureActive(backend, third.Key);
        CompleteTextTextureFrame(backend, maximumEntries: 1, maximumBytes: long.MaxValue);

        Assert.False(cache.Contains(secondKey));
        Assert.True(atlas.IsDisposed);
        backend.Dispose();
    }

    [Fact]
    public void CoordinateScaleChangeClearsTextAndBrushTextureCachesImmediately()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture text = AddTextTexture(cache, "scale");
        SetTextTextureCache(backend, cache);
        Texture2D brush = AddTextBrushTexture(backend, text.Key);

        backend.CoordinateScale = 1.25f;

        Assert.Equal(0, backend.TextTextureCacheCount);
        Assert.Empty(TextBrushTextureCache(backend));
        Assert.True(text.Red.IsDisposed);
        Assert.True(text.Green.IsDisposed);
        Assert.True(text.Blue.IsDisposed);
        Assert.True(text.Mask.IsDisposed);
        Assert.True(brush.IsDisposed);
        backend.Dispose();
    }

    [Fact]
    public void DeviceResetClearsTextAndBrushTextureCachesImmediately()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture text = AddTextTexture(cache, "device");
        SetTextTextureCache(backend, cache);
        Texture2D brush = AddTextBrushTexture(backend, text.Key);

        InvokeDeviceReset(backend);

        Assert.Equal(0, backend.TextTextureCacheCount);
        Assert.Empty(TextBrushTextureCache(backend));
        Assert.True(text.Red.IsDisposed);
        Assert.True(text.Green.IsDisposed);
        Assert.True(text.Blue.IsDisposed);
        Assert.True(text.Mask.IsDisposed);
        Assert.True(brush.IsDisposed);
        backend.Dispose();
    }

    [Fact]
    public void TextTextureCacheDiagnosticsExposeHitsMissesEvictionsAndEstimatedBytes()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();

        MonoGameDrawingBackend.TextTextureCacheDiagnosticSnapshot diagnostics = backend.TextTextureCacheDiagnostics;

        Assert.Equal(0, diagnostics.Hits);
        Assert.Equal(0, diagnostics.Misses);
        Assert.Equal(0, diagnostics.Evictions);
        Assert.Equal(0, diagnostics.EstimatedBytes);
    }

    [Fact]
    public void WarmStaticTextCacheAccessDoesNotAllocatePerLookup()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture text = AddTextTexture(cache, "static");
        SetTextTextureCache(backend, cache);
        Assert.True(backend.UseTextTextureForDiagnostics(text.Key));

        long allocated = MeasureAllocations(
            () => backend.UseTextTextureForDiagnostics(text.Key),
            iterations: 1_000);

        Assert.InRange(allocated, 0, 1_024);
        backend.Dispose();
    }

    [Fact]
    public void WarmAnimatedTextCacheAccessDoesNotAllocateAfterCanonicalVariantsExist()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture[] variants = Enumerable.Range(0, 64)
            .Select(index => AddTextTexture(cache, $"animated-{index}"))
            .ToArray();
        SetTextTextureCache(backend, cache);
        foreach (TextTextureFixture variant in variants)
        {
            Assert.True(backend.UseTextTextureForDiagnostics(variant.Key));
        }

        int index = 0;
        long allocated = MeasureAllocations(
            () =>
            {
                backend.UseTextTextureForDiagnostics(variants[index].Key);
                index = (index + 1) % variants.Length;
            },
            iterations: 1_000);

        Assert.InRange(allocated, 0, 1_024);
        backend.Dispose();
    }

    [Fact]
    public void WarmABASwitchDoesNotAllocateOrMiss()
    {
        MonoGameDrawingBackend backend = CreateBackendShell();
        IDictionary cache = CreateTextTextureCache();
        TextTextureFixture first = AddTextTexture(cache, "A");
        TextTextureFixture second = AddTextTexture(cache, "B");
        SetTextTextureCache(backend, cache);
        Assert.True(backend.UseTextTextureForDiagnostics(first.Key));
        Assert.True(backend.UseTextTextureForDiagnostics(second.Key));

        int index = 0;
        object[] sequence = [first.Key, second.Key, first.Key];
        long missesBefore = backend.TextTextureCacheDiagnostics.Misses;
        long allocated = MeasureAllocations(
            () =>
            {
                backend.UseTextTextureForDiagnostics(sequence[index]);
                index = (index + 1) % sequence.Length;
            },
            iterations: 1_000);

        Assert.InRange(allocated, 0, 1_024);
        Assert.Equal(missesBefore, backend.TextTextureCacheDiagnostics.Misses);
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

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.0624f, 0f)]
    [InlineData(0.0626f, 0.125f)]
    [InlineData(0.9999f, 0f)]
    [InlineData(-0.0001f, 0f)]
    [InlineData(-0.0624f, 0f)]
    [InlineData(-0.0626f, 0.875f)]
    public void CanonicalPixelPhaseHandlesNegativeAndBoundaryPositions(float position, float expected)
    {
        DrawPoint phase = MonoGameDrawingBackend.GetCanonicalPixelPhaseForDiagnostics(
            new DrawPoint(position, position),
            coordinateScale: 1);

        Assert.Equal(expected, phase.X);
        Assert.Equal(expected, phase.Y);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void LongTranslationProducesAtMostEightPhasesPerAxis(float coordinateScale)
    {
        DrawTextRun textRun = new(new TestFont("Arial", 16), "animated", 16);
        HashSet<object> keys = [];
        int cacheHits = 0;
        for (int step = -4_096; step <= 4_096; step++)
        {
            DrawPoint position = new(step / 97f, step / 89f);
            object key = MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
                textRun,
                coordinateScale,
                position,
                Cerneala.Drawing.Color.Black);
            if (!keys.Add(key))
            {
                cacheHits++;
            }
        }

        Assert.InRange(keys.Count, 2, 64);
        Assert.True(cacheHits > 8_000);
    }

    [Fact]
    public void SolidAndBrushTextKeysUseTheSameCanonicalTextPhase()
    {
        DrawTextRun textRun = new(new TestFont("Arial", 16), "animated", 16);
        TestBrush brush = new();
        DrawPoint first = new(10.01f, -2.01f);
        DrawPoint second = new(10.02f, -2.02f);

        object solidFirst = MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
            textRun,
            1.25f,
            first,
            Cerneala.Drawing.Color.White);
        object solidSecond = MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
            textRun,
            1.25f,
            second,
            Cerneala.Drawing.Color.White);
        object brushFirst = MonoGameDrawingBackend.CreateTextBrushTextureKeyForDiagnostics(
            textRun,
            1.25f,
            first,
            brush,
            1);
        object brushSecond = MonoGameDrawingBackend.CreateTextBrushTextureKeyForDiagnostics(
            textRun,
            1.25f,
            second,
            brush,
            1);

        Assert.Equal(solidFirst, solidSecond);
        Assert.Equal(brushFirst, brushSecond);
    }

    [Fact]
    public void TextTextureKeyStillSeparatesFontSizeScaleAndColor()
    {
        IDrawFont firstFont = new TestFont("Arial", 16);
        IDrawFont secondFont = new TestFont("Consolas", 16);
        DrawPoint position = new(10.02f, 20.02f);

        object baseline = MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
            new DrawTextRun(firstFont, "key", 16),
            1,
            position,
            Cerneala.Drawing.Color.Black);

        Assert.NotEqual(
            baseline,
            MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
                new DrawTextRun(secondFont, "key", 16),
                1,
                position,
                Cerneala.Drawing.Color.Black));
        Assert.NotEqual(
            baseline,
            MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
                new DrawTextRun(firstFont, "key", 17),
                1,
                position,
                Cerneala.Drawing.Color.Black));
        Assert.NotEqual(
            baseline,
            MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
                new DrawTextRun(firstFont, "key", 16),
                1.25f,
                position,
                Cerneala.Drawing.Color.Black));
        Assert.NotEqual(
            baseline,
            MonoGameDrawingBackend.CreateTextTextureKeyForDiagnostics(
                new DrawTextRun(firstFont, "key", 16),
                1,
                position,
                Cerneala.Drawing.Color.White));
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

    private static TextTextureFixture AddTextTexture(IDictionary cache, string text)
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
        Texture2D red = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        Texture2D green = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        Texture2D blue = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        Texture2D mask = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        object value = Activator.CreateInstance(
            valueType,
            red,
            green,
            blue,
            mask,
            default(DrawPoint))!;
        cache.Add(key, value);
        return new TextTextureFixture(key, red, green, blue, mask);
    }

    private static object AddTextTextureWithSharedRgb(
        IDictionary cache,
        string text,
        Texture2D sharedTexture)
    {
        Type keyType = cache.GetType().GetGenericArguments()[0];
        MethodInfo fromMethod = keyType.GetMethod(
            "From",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: [typeof(DrawTextRun), typeof(float), typeof(DrawPoint)],
            modifiers: null)!;
        object key = fromMethod.Invoke(
            null,
            [new DrawTextRun(new TestFont("Arial", 16), text, 16), 1f, default(DrawPoint)])!;
        Type valueType = cache.GetType().GetGenericArguments()[1];
        object value = Activator.CreateInstance(
            valueType,
            sharedTexture,
            sharedTexture,
            sharedTexture,
            null,
            default(DrawPoint))!;
        cache.Add(key, value);
        return key;
    }

    private static void SetSharedTextTextureReferenceCount(
        MonoGameDrawingBackend backend,
        Texture2D texture,
        int references)
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField(
            "sharedTextTextureReferenceCounts",
            NonPublicInstance);
        Assert.True(field is not null, "Expected shared text atlases to have deterministic reference tracking.");
        IDictionary counts = (IDictionary)Activator.CreateInstance(field!.FieldType)!;
        counts.Add(texture, references);
        field.SetValue(backend, counts);
    }

    private static void MarkTextTextureActive(MonoGameDrawingBackend backend, object key)
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField("activeTextTextureKeys", NonPublicInstance);
        Assert.True(field is not null, "Expected the backend to track text textures used by the current frame.");
        object activeKeys = Activator.CreateInstance(field!.FieldType)!;
        field.SetValue(backend, activeKeys);
        activeKeys.GetType().GetMethod("Add")!.Invoke(activeKeys, [key]);
    }

    private static void ClearActiveTextTextureKeys(MonoGameDrawingBackend backend)
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField("activeTextTextureKeys", NonPublicInstance);
        Assert.True(field is not null, "Expected the backend to track text textures used by the current frame.");
        field!.GetValue(backend)!.GetType().GetMethod("Clear")!.Invoke(field.GetValue(backend), null);
    }

    private static bool UseTextTextureForDiagnostics(MonoGameDrawingBackend backend, object key)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod(
            "UseTextTextureForDiagnostics",
            NonPublicInstance);
        Assert.True(
            method is not null,
            "Expected a diagnostic lookup to exercise real cache-hit and generation tracking.");
        return Assert.IsType<bool>(method!.Invoke(backend, [key]));
    }

    private static void CompleteTextTextureFrame(
        MonoGameDrawingBackend backend,
        int maximumEntries,
        long maximumBytes)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod(
            "CompleteTextTextureFrameForDiagnostics",
            NonPublicInstance);
        Assert.True(
            method is not null,
            "Expected bounded text-cache completion to retain entries and evict only deterministic LRU overflow.");
        method!.Invoke(backend, [maximumEntries, maximumBytes]);
    }

    private static Texture2D AddTextBrushTexture(MonoGameDrawingBackend backend, object textKey)
    {
        IDictionary cache = TextBrushTextureCache(backend);
        Type keyType = cache.GetType().GetGenericArguments()[0];
        ConstructorInfo constructor = keyType.GetConstructors(NonPublicInstance | BindingFlags.Public).Single();
        IDrawBrush brush = new TestBrush();
        object key = constructor.Invoke([textKey, brush, 1f]);
        Texture2D texture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        cache.Add(key, texture);
        return texture;
    }

    private static IDictionary TextBrushTextureCache(MonoGameDrawingBackend backend)
    {
        FieldInfo? field = typeof(MonoGameDrawingBackend).GetField("textBrushTextureCache", NonPublicInstance);
        Assert.True(field is not null, "Expected dependent text-brush textures to have an internal cache.");
        IDictionary? cache = (IDictionary?)field!.GetValue(backend);
        if (cache is null)
        {
            cache = (IDictionary)Activator.CreateInstance(field.FieldType)!;
            field.SetValue(backend, cache);
        }

        return cache;
    }

    private static void InvokeDeviceReset(MonoGameDrawingBackend backend)
    {
        MethodInfo? method = typeof(MonoGameDrawingBackend).GetMethod("OnDeviceReset", NonPublicInstance);
        Assert.True(method is not null, "Expected device reset to clear GPU caches immediately.");
        method!.Invoke(backend, [null, EventArgs.Empty]);
    }

    private static long MeasureAllocations(Action action, int iterations)
    {
        action();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            action();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
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

    private static void RenderBackend(IDrawingBackend backend, DrawCommandList commands)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);
        backend.Render(commands, in frameContext);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;

    private sealed record TextTextureFixture(
        object Key,
        Texture2D Red,
        Texture2D Green,
        Texture2D Blue,
        Texture2D Mask);

    private sealed class TestBrush : IDrawBrush
    {
        public DrawBrushKind Kind => DrawBrushKind.SolidColor;

        public float Opacity => 1;

        public Cerneala.Drawing.Color? SolidColor => Cerneala.Drawing.Color.White;

        public DrawBrushDescriptor CreateDescriptor() =>
            new SolidDrawBrushDescriptor(Cerneala.Drawing.Color.White, 1);
    }

    private sealed class UnsupportedImage : IDrawImage
    {
        public int Width => 1;

        public int Height => 1;
    }

    private sealed class DeviceStateFixture : IDisposable
    {
        private readonly GraphicsDevice device;
        private readonly RenderTarget2D target;
        private readonly BlendState blendState;
        private readonly DepthStencilState depthStencilState;
        private readonly RasterizerState rasterizerState;
        private readonly SamplerState samplerState;
        private readonly Texture2D texture;
        private readonly IndexBuffer indexBuffer;
        private readonly Viewport viewport = new(3, 4, 32, 20);
        private readonly Rectangle scissor = new(5, 6, 20, 10);
        private readonly Microsoft.Xna.Framework.Color blendFactor = new(17, 31, 47, 63);

        public DeviceStateFixture(GraphicsDevice device)
        {
            this.device = device;
            target = new RenderTarget2D(
                device,
                48,
                32,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8,
                0,
                RenderTargetUsage.PreserveContents);
            blendState = new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha
            };
            depthStencilState = new DepthStencilState
            {
                DepthBufferEnable = true,
                DepthBufferWriteEnable = false
            };
            rasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullClockwiseFace,
                ScissorTestEnable = true
            };
            samplerState = new SamplerState
            {
                Filter = TextureFilter.Point,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Mirror
            };
            texture = new Texture2D(device, 2, 2);
            texture.SetData(Enumerable.Repeat(Microsoft.Xna.Framework.Color.White, 4).ToArray());
            indexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, 3, BufferUsage.None);
            indexBuffer.SetData(new ushort[] { 0, 1, 2 });

            device.SetRenderTarget(target);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            device.BlendState = blendState;
            device.BlendFactor = blendFactor;
            device.DepthStencilState = depthStencilState;
            device.RasterizerState = rasterizerState;
            device.SamplerStates[0] = samplerState;
            device.Textures[0] = texture;
            device.Indices = indexBuffer;
        }

        public void AssertRestored()
        {
            RenderTargetBinding binding = Assert.Single(device.GetRenderTargets());
            Assert.Same(target, binding.RenderTarget);
            Assert.Equal(viewport, device.Viewport);
            Assert.Equal(scissor, device.ScissorRectangle);
            Assert.Same(blendState, device.BlendState);
            Assert.Equal(blendFactor, device.BlendFactor);
            Assert.Same(depthStencilState, device.DepthStencilState);
            Assert.Same(rasterizerState, device.RasterizerState);
            Assert.Same(samplerState, device.SamplerStates[0]);
            Assert.Same(texture, device.Textures[0]);
            Assert.Same(indexBuffer, device.Indices);
        }

        public void Dispose()
        {
            device.SetRenderTarget(null);
            device.BlendState = BlendState.Opaque;
            device.BlendFactor = Microsoft.Xna.Framework.Color.White;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.LinearClamp;
            device.Textures[0] = null;
            device.Indices = null;
            indexBuffer.Dispose();
            texture.Dispose();
            samplerState.Dispose();
            rasterizerState.Dispose();
            depthStencilState.Dispose();
            blendState.Dispose();
            target.Dispose();
        }
    }

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title = $"Cerneala backend state {Guid.NewGuid():N}",
                    Width = 96,
                    Height = 64
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = Assert.IsType<WindowsDxWindowGraphicsSession>(window.GraphicsSession);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }

        public void RenderRequested() { }
    }
}
