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

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }

    private static Color ParseColor(string value)
    {
        Assert.True(Color.TryParse(value, out Color color));
        return color;
    }
}
