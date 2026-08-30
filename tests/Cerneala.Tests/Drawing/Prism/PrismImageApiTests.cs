using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismImageApiTests
{
    [Fact]
    public void GeneratedOperationsExposeTypedCatalogDefaults()
    {
        BlurFilter blur = new();
        OuterGlowStyle glow = new();

        Assert.Equal(1, blur.Radius);
        Assert.Equal("Good", blur.Quality);
        Assert.Equal(5, glow.Size);
        Assert.Equal(0.75f, glow.Opacity);
        Assert.True(Color.TryParse("#FFFFFFBE", out Color expectedColor));
        Assert.Equal(expectedColor, glow.Color);
    }

    [Fact]
    public void GeneratedOperationsValidateCatalogDomainsAtAssignment()
    {
        BlurFilter blur = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => blur.Radius = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => blur.Quality = "Impossible");
    }

    [Fact]
    public void CatalogHasOneGeneratedOperationTypePerFilterAndStyle()
    {
        Type assemblyMarker = typeof(PrismImage);
        IEnumerable<PrismCatalogOperationInfo> operations =
            PrismCatalog.Filters.Concat(PrismCatalog.Styles);

        foreach (PrismCatalogOperationInfo operation in operations)
        {
            string suffix = operation.Kind == PrismCatalogOperationKind.Filter
                ? "Filter"
                : "Style";
            Type? generated = assemblyMarker.Assembly.GetType(
                $"Cerneala.Drawing.Prism.{operation.Symbol}{suffix}");

            Assert.NotNull(generated);
            Assert.True(typeof(PrismOperation).IsAssignableFrom(generated));
            object instance = Activator.CreateInstance(generated)!;
            foreach (System.Reflection.PropertyInfo property in generated
                .GetProperties()
                .Where(property => property.DeclaringType == generated))
            {
                object? value = property.GetValue(instance);
                property.SetValue(instance, value);
            }
        }
    }

    [Fact]
    public void DrawingPrismImageExpandsIntoNativePrismScope()
    {
        TestImage source = new(32, 24);
        BlurFilter blur = new() { Radius = 4 };
        OuterGlowStyle glow = new()
        {
            Size = 7,
            Opacity = 0.5f,
            Color = ParseColor("#70FF4890")
        };
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            blur,
            glow);
        DrawCommandList commands = new();

        new DrawingContext(commands).DrawImage(
            image,
            new DrawRect(10, 20, 64, 48),
            new DrawRect(1, 2, 16, 12),
            Color.White,
            rotation: 0.25f,
            origin: new DrawPoint(3, 4),
            flip: DrawImageFlip.Horizontal,
            layerDepth: 0.75f);

        Assert.Equal(3, commands.Count);
        Assert.Equal(DrawCommandKind.BeginPrism, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawImage, commands[1].Kind);
        Assert.Equal(DrawCommandKind.EndPrism, commands[2].Kind);
        Assert.Same(source, commands[1].Image);
        Assert.Equal(new DrawRect(1, 2, 16, 12), commands[1].ImageSource);
        Assert.Equal(0.25f, commands[1].ImageRotation);
        Assert.Equal(new DrawPoint(3, 4), commands[1].ImageOrigin);
        Assert.Equal(DrawImageFlip.Horizontal, commands[1].ImageFlip);
        Assert.Equal(0.75f, commands[1].LayerDepth);

        PrismLayerState layer = commands[0].PrismScope!.Value.Instance
            .GetLayerState(new Cerneala.UI.Prism.Definitions.PrismNodeId(1));
        Assert.Single(layer.Filters);
        Assert.Single(layer.Styles);
        Assert.Equal(4, layer.Filters[0].GetValue<float>(
            PrismCatalog.GetFilter(PrismFilterId.Blur).Parameters
                .Single(parameter => parameter.Name == "Radius")));
        Assert.Equal(7, layer.Styles[0].GetValue<float>(
            PrismCatalog.GetStyle(PrismStyleId.OuterGlow).Parameters
                .Single(parameter => parameter.Name == "Size")));
    }

    [Fact]
    public void OperationMutationInvalidatesTheNextDrawScope()
    {
        BlurFilter blur = new() { Radius = 2 };
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            new TestImage(8, 8),
            blur);
        DrawRect destination = new(0, 0, 8, 8);
        DrawCommandList first = new();
        new DrawingContext(first).DrawImage(image, destination, Color.White);
        long firstVersion = first[0].PrismScope!.Value.VisualContentVersion;

        blur.Radius = 6;
        DrawCommandList second = new();
        new DrawingContext(second).DrawImage(image, destination, Color.White);

        Assert.True(second[0].PrismScope!.Value.VisualContentVersion > firstVersion);
        PrismFilterState state = second[0].PrismScope!.Value.Instance
            .GetLayerState(new Cerneala.UI.Prism.Definitions.PrismNodeId(1))
            .Filters[0];
        Assert.Equal(6, state.GetValue<float>(
            PrismCatalog.GetFilter(PrismFilterId.Blur).Parameters
                .Single(parameter => parameter.Name == "Radius")));
    }

    [Fact]
    public void ObservedOperationMutationInvalidatesRetainedResultsForItsImageOwner()
    {
        BlurFilter blur = new() { Radius = 2 };
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            new TestImage(8, 8),
            blur);
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawImage(
            image,
            new DrawRect(0, 0, 8, 8),
            Color.White);
        PrismCacheOwnerToken ownerToken =
            commands[0].PrismScope!.Value.CacheOwnerToken;
        PrismCacheInvalidationQueue queue = new();

        blur.Radius = 6;
        DrawCommandList changedCommands = new();
        new DrawingContext(changedCommands).DrawImage(
            image,
            new DrawRect(0, 0, 8, 8),
            Color.White);

        AssertSingleOwnerInvalidation(queue, ownerToken);
    }

    [Fact]
    public void FillCanHideLayerContentWhilePreservingItsPrismStyles()
    {
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            new TestImage(8, 8),
            new OuterGlowStyle());
        IDrawImageInvalidationSource invalidationSource = image;
        int changeCount = 0;
        invalidationSource.ContentChanged += (_, _) => changeCount++;
        DrawRect destination = new(0, 0, 8, 8);
        DrawCommandList first = new();
        new DrawingContext(first).DrawImage(image, destination, Color.White);
        long firstVersion = first[0].PrismScope!.Value.VisualContentVersion;

        Assert.Equal(1, image.Fill);

        image.Fill = 0;
        DrawCommandList second = new();
        new DrawingContext(second).DrawImage(image, destination, Color.White);

        Assert.Equal(1, changeCount);
        Assert.True(second[0].PrismScope!.Value.VisualContentVersion > firstVersion);
        PrismLayerState layer = second[0].PrismScope!.Value.Instance
            .GetLayerState(new Cerneala.UI.Prism.Definitions.PrismNodeId(1));
        Assert.Equal(0, layer.Fill);
        Assert.Single(layer.Styles);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Fill = -0.01f);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Fill = 1.01f);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Fill = float.NaN);
    }

    [Fact]
    public void NestedPrismImagesComposeAsNestedNativeScopes()
    {
        TestImage source = new(12, 12);
        PrismImage blurred = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new BlurFilter());
        PrismImage glowing = global::Cerneala.Drawing.Prism.Prism.Apply(
            blurred,
            new OuterGlowStyle());
        DrawCommandList commands = new();

        new DrawingContext(commands).DrawImage(
            glowing,
            new DrawRect(0, 0, 12, 12),
            Color.White);

        Assert.Equal(
            [
                DrawCommandKind.BeginPrism,
                DrawCommandKind.BeginPrism,
                DrawCommandKind.DrawImage,
                DrawCommandKind.EndPrism,
                DrawCommandKind.EndPrism
            ],
            commands.Select(command => command.Kind));
        Assert.Same(source, commands[2].Image);
    }

    [Fact]
    public void EmptyPipelineCannotProduceOrDrawPrismImage()
    {
        Assert.Throws<ArgumentException>(() =>
            global::Cerneala.Drawing.Prism.Prism.Apply(
                new TestImage(1, 1),
                new PrismPipeline()));

        PrismPipeline pipeline = new([new BlurFilter()]);
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            new TestImage(1, 1),
            pipeline);
        pipeline.Clear();

        Assert.Throws<InvalidOperationException>(() =>
            new DrawingContext(new DrawCommandList()).DrawImage(
                image,
                new DrawRect(0, 0, 1, 1),
                Color.White));
    }

    [Fact]
    public void DisposeInvalidatesEveryRegisteredCacheQueueOnce()
    {
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            new TestImage(8, 8),
            new BlurFilter());
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawImage(
            image,
            new DrawRect(0, 0, 8, 8),
            Color.White);
        PrismCacheOwnerToken ownerToken =
            commands[0].PrismScope!.Value.CacheOwnerToken;
        PrismCacheInvalidationQueue first = new();
        PrismCacheInvalidationQueue second = new();

        image.Dispose();
        image.Dispose();

        AssertSingleOwnerInvalidation(first, ownerToken);
        AssertSingleOwnerInvalidation(second, ownerToken);
    }

    [Fact]
    public void DisposeStopsObservationAndRejectsFutureDraws()
    {
        ObservableTestImage source = new(8, 8);
        BlurFilter blur = new();
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            blur);
        IDrawImageInvalidationSource invalidationSource = image;
        int changeCount = 0;
        EventHandler handler = (_, _) => changeCount++;
        invalidationSource.ContentChanged += handler;

        source.RaiseContentChanged();
        image.Dispose();
        source.RaiseContentChanged();
        blur.Radius = 4;

        Assert.Equal(2, changeCount);
        Assert.Throws<ObjectDisposedException>(() =>
            new DrawingContext(new DrawCommandList()).DrawImage(
                image,
                new DrawRect(0, 0, 8, 8),
                Color.White));
        Assert.Throws<ObjectDisposedException>(() =>
            invalidationSource.ContentChanged += (_, _) => { });

        invalidationSource.ContentChanged -= handler;
    }

    [Fact]
    public void DisposeDoesNotTakeOwnershipOfTheSourceImage()
    {
        DisposableTestImage source = new(8, 8);
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new BlurFilter());

        image.Dispose();

        Assert.False(source.IsDisposed);
    }

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private sealed class ObservableTestImage(int width, int height) :
        IDrawImage,
        IDrawImageInvalidationSource
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public event EventHandler? ContentChanged;

        public void RaiseContentChanged() =>
            ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class DisposableTestImage(int width, int height) :
        IDrawImage,
        IDisposable
    {
        public int Width { get; } = width;

        public int Height { get; } = height;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private static void AssertSingleOwnerInvalidation(
        PrismCacheInvalidationQueue queue,
        PrismCacheOwnerToken expected)
    {
        int matchingInvalidations = 0;
        while (queue.TryDequeue(out PrismCacheInvalidation invalidation))
        {
            if (invalidation.Kind == PrismCacheInvalidationKind.Owner &&
                invalidation.OwnerToken == expected)
            {
                matchingInvalidations++;
            }
        }

        Assert.Equal(1, matchingInvalidations);
    }

    private static Color ParseColor(string value)
    {
        Assert.True(Color.TryParse(value, out Color color));
        return color;
    }
}
