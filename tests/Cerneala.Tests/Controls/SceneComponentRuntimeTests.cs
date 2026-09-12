using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneComponentRuntimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DerivedScenesKeepTheSameOrderedTransformAndPrismCommands(bool withPrism)
    {
        TestImage image = new();
        RenderSurface2D ordinary = CreateSurface(image, derived: false);
        RenderSurface2D component = CreateSurface(image, derived: true);
        UIRoot root = new();
        using IDisposable? ordinaryPrism = withPrism ? AttachPrism(ordinary) : null;
        using IDisposable? componentPrism = withPrism ? AttachPrism(component) : null;
        ElementLifecycle.AttachSubtree(root, ordinary);
        ElementLifecycle.AttachSubtree(root, component);
        try
        {
            DrawCommandList expected = Record(ordinary);
            DrawCommandList actual = Record(component);
            Assert.Equal(expected.Select(command => (command.Kind, command.Rect, command.Transform, command.Image)),
                actual.Select(command => (command.Kind, command.Rect, command.Transform, command.Image)));
            Assert.Equal(2, actual.Count(command => command.Kind == DrawCommandKind.DrawImage));
            Assert.Equal(new[] { Matrix3x2.CreateTranslation(50, 15), Matrix3x2.CreateScale(2) * Matrix3x2.CreateTranslation(20, 10) },
                actual.Where(command => command.Kind == DrawCommandKind.PushTransform).Select(command => command.Transform));
            Assert.Equal(expected.Where(command => command.Kind == DrawCommandKind.BeginPrism)
                    .Select(command => Assert.IsType<PrismDrawScope>(command.PrismScope).ControlBounds),
                actual.Where(command => command.Kind == DrawCommandKind.BeginPrism)
                    .Select(command => Assert.IsType<PrismDrawScope>(command.PrismScope).ControlBounds));
            Assert.Equal(withPrism ? 1 : 0, actual.Count(command => command.Kind == DrawCommandKind.BeginPrism));
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, component);
            ElementLifecycle.DetachSubtree(root, ordinary);
        }
    }

    [Fact]
    public void DerivedSceneSharesWorldAndInvalidatesTheOwningOnDemandSurface()
    {
        RenderSurface2D surface = CreateSurface(new TestImage(), derived: true);
        Scene2D house = (Scene2D)surface.Scene!.Children[0];
        Scene2D sibling = (Scene2D)surface.Scene.Children[1];
        Assert.Same(surface.Scene.CollisionWorld, house.CollisionWorld);
        Assert.Same(house.CollisionWorld, sibling.CollisionWorld);
        long version = ((IRenderSurface2DFrameSource)surface).FrameVersion;
        house.TranslateX = 23;
        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > version);
        Assert.Equal(50, sibling.TranslateX);
        Assert.Empty(house.VisualChildren);
        Assert.Same(house, house.Children[0].LogicalParent);
    }

    private static RenderSurface2D CreateSurface(TestImage image, bool derived)
    {
        Scene2D root = new() { OrderMode = SceneOrderMode.Layer };
        for (int index = 0; index < 2; index++)
        {
            Scene2D house = derived ? new HouseScene() : new Scene2D();
            house.TranslateX = index == 0 ? 20 : 50;
            house.TranslateY = index == 0 ? 10 : 15;
            house.Scale = index == 0 ? 2 : 1;
            house.Layer = index == 0 ? 10 : 0;
            house.Children.Add(new Sprite2D { Image = new(image), X = 2, Y = 3, Width = 4, Height = 5 });
            root.Children.Add(house);
        }
        return new RenderSurface2D { Scene = root, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
    }

    private static IDisposable AttachPrism(RenderSurface2D surface) => GeneratedMarkup.AttachPrism(
        surface.Scene!.Children[0],
        () => new PrismInstance(PrismTestData.Composition("House", PrismTestData.Layer(1, "Content"))));

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 100, 100));
        return commands;
    }

    private sealed class HouseScene : Scene2D { }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }
}
