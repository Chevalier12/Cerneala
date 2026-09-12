using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string HouseComponentCode = """
        using Cerneala.UI.Controls;
        using Cerneala.UI.Core;
        using Cerneala.UI.Input;
        namespace Game;
        public partial class HouseView : Scene2D
        {
            public static readonly UiProperty<float> DoorXProperty = UiProperty<float>.Register(
                nameof(DoorX), typeof(HouseView), new UiPropertyMetadata<float>(4));
            public float DoorX { get => GetValue(DoorXProperty); set => SetValue(DoorXProperty, value); }
            public Sprite2D DoorNode => Door;
            public int LoadCount { get; private set; }
            private void OnHouseLoaded(UiElementId sender, RoutedEventArgs args) => LoadCount++;
        }
        """;

    private const string HouseComponentMarkup = """
        <Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala"
                 Loaded="OnHouseLoaded">
            <Scene2D.Resources>
                <resources:ImageResource Name="HouseArt" Source="Assets/house.png" />
            </Scene2D.Resources>
            <Sprite2D Image="$HouseArt" Width="32" Height="24" />
            <Sprite2D Name="Door" X="$root.DoorX:OneWay" Y="16" Width="8" Height="8">
                <Sprite2D.Aspect>
                    @default { Opacity = 0.75; }
                </Sprite2D.Aspect>
            </Sprite2D>
        </Scene2D>
        """;

    [Fact]
    public void SceneComponentPairsMarkupWithSceneClassAndCreatesIndependentLogicalInstances()
    {
        const string world = """
            <RenderSurface2D xmlns:local="clr-namespace:Game;assembly=GeneratorTests" RedrawMode="OnDemand">
                <RenderSurface2D.Scene>
                    <Scene2D OrderMode="Layer">
                        <local:HouseView TranslateX="30" TranslateY="20" Scale="2" Layer="10" DoorX="6" />
                        <local:HouseView TranslateX="90" TranslateY="40" Layer="20" DoorX="12" />
                    </Scene2D>
                </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        Assembly assembly = CompileSceneComponent(HouseComponentMarkup, world);
        RenderSurface2D surface = (RenderSurface2D)assembly.GetType("Cerneala.GeneratedUi.WorldFactory")!
            .GetMethod("Create", Type.EmptyTypes)!.Invoke(null, null)!;
        Scene2D first = Assert.IsAssignableFrom<Scene2D>(surface.Scene!.Children[0]);
        Scene2D second = Assert.IsAssignableFrom<Scene2D>(surface.Scene.Children[1]);
        Assert.Equal("Game.HouseView", first.GetType().FullName);
        Assert.Equal(30, first.TranslateX);
        Assert.Equal(20, first.TranslateY);
        Assert.Equal(2, first.Scale);
        Assert.Equal(10, first.Layer);
        Assert.NotSame(first, second);
        Assert.Equal(2, first.Children.Count);
        Assert.Empty(first.VisualChildren);
        Assert.Equal(first.Children, first.LogicalChildren.Cast<SceneNode2D>());
        Assert.NotSame(first.Children[0], second.Children[0]);
        Sprite2D firstDoor = (Sprite2D)first.GetType().GetProperty("DoorNode")!.GetValue(first)!;
        Sprite2D secondDoor = (Sprite2D)second.GetType().GetProperty("DoorNode")!.GetValue(second)!;
        Assert.Same(first.Children[1], firstDoor);
        Assert.NotSame(firstDoor, secondDoor);
        Assert.True(first.Resources.TryGetResource(new ResourceId<ImageResource>("HouseArt"), out var art));
        Assert.Equal("Assets/house.png", art!.Path);
        Assert.True(second.Resources.TryGetResource(new ResourceId<ImageResource>("HouseArt"), out var secondArt));
        Assert.NotSame(art, secondArt);

        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Assert.Same(root, firstDoor.Root);
            Assert.Same(first, firstDoor.LogicalParent);
            Assert.Equal(6, firstDoor.X);
            Assert.Equal(12, secondDoor.X);
            Assert.Equal(0.75f, firstDoor.Opacity);
            Assert.Equal(1, first.GetType().GetProperty("LoadCount")!.GetValue(first));
            first.GetType().GetProperty("DoorX")!.SetValue(first, 18f);
            Assert.Equal(18, firstDoor.X);
            Assert.Equal(12, secondDoor.X);

            surface.Scene.Children.Remove(first);
            first.GetType().GetProperty("DoorX")!.SetValue(first, 22f);
            Assert.Null(firstDoor.Root);
            Assert.Equal(18, firstDoor.X);
            surface.Scene.Children.Add(first);
            root.ProcessFrame();
            Assert.Same(firstDoor, first.Children[1]);
            Assert.Equal(22, firstDoor.X);
            Assert.Equal(2, first.GetType().GetProperty("LoadCount")!.GetValue(first));
        }
        finally { root.VisualChildren.Remove(surface); }
    }

    [Fact]
    public void SceneComponentCanBeTheSurfacesSingleSceneRoot()
    {
        const string world = """
            <RenderSurface2D xmlns:local="clr-namespace:Game;assembly=GeneratorTests">
                <RenderSurface2D.Scene>
                    <local:HouseView TranslateX="30" />
                </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        Assembly assembly = CompileSceneComponent(HouseComponentMarkup, world);
        RenderSurface2D surface = (RenderSurface2D)assembly.GetType("Cerneala.GeneratedUi.WorldFactory")!
            .GetMethod("Create", Type.EmptyTypes)!.Invoke(null, null)!;
        Assert.Equal("Game.HouseView", surface.Scene!.GetType().FullName);
        Assert.Equal(2, surface.Scene.Children.Count);
        Assert.Equal(30, surface.Scene.TranslateX);
    }

    [Fact]
    public void SceneComponentResolvesTypedDataContextThroughLogicalAncestorsAndRebindsOnReplacement()
    {
        const string markup = """
            <Scene2D DataType="Game.HouseData">
                <Sprite2D Name="Door" X="$DataContext.X:OneWay" Width="8" Height="8" />
            </Scene2D>
            """;
        const string code = HouseComponentCode + """

            public sealed class HouseData : UiObject
            {
                public static readonly UiProperty<float> XProperty = UiProperty<float>.Register(
                    nameof(X), typeof(HouseData), new UiPropertyMetadata<float>(0));
                public float X { get => GetValue(XProperty); set => SetValue(XProperty, value); }
            }
            """;
        Assembly assembly = CompileSceneComponent(markup,
            "<RenderSurface2D xmlns:local=\"clr-namespace:Game;assembly=GeneratorTests\"><RenderSurface2D.Scene><local:HouseView /></RenderSurface2D.Scene></RenderSurface2D>", code);
        RenderSurface2D surface = (RenderSurface2D)assembly.GetType("Cerneala.GeneratedUi.WorldFactory")!
            .GetMethod("Create", Type.EmptyTypes)!.Invoke(null, null)!;
        Type dataType = assembly.GetType("Game.HouseData")!;
        object firstData = Activator.CreateInstance(dataType)!;
        dataType.GetProperty("X")!.SetValue(firstData, 5f);
        surface.DataContext = firstData;
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Sprite2D door = (Sprite2D)surface.Scene!.Children[0];
            // Generated bindings resolve context through logical ancestors, not
            // by copying the ancestor's DataContext onto each logical node.
            Assert.Equal(5, door.X);
            dataType.GetProperty("X")!.SetValue(firstData, 9f);
            Assert.Equal(9, door.X);
            object secondData = Activator.CreateInstance(dataType)!;
            dataType.GetProperty("X")!.SetValue(secondData, 12f);
            surface.DataContext = secondData;
            Assert.Equal(12, door.X);
            dataType.GetProperty("X")!.SetValue(firstData, 20f);
            Assert.Equal(12, door.X);
            surface.Scene.DataContext = firstData;
            Assert.Equal(20, door.X);
            surface.Scene.ClearValue(UIElement.DataContextProperty);
            Assert.Equal(12, door.X);
        }
        finally { root.VisualChildren.Remove(surface); }
    }

    [Fact]
    public void SceneComponentSupportsRootAspectMotionPrismAndSceneTemplates()
    {
        const string markup = """
            <Scene2D Loaded="OnHouseLoaded">
                <Scene2D.Aspect>
                    @default { DoorX = 7; }
                    @on Loaded {
                        @animate with Tween(100ms) { @to { TranslateY = 8; } }
                    }
                </Scene2D.Aspect>
                @prism { @layer HouseContent { Opacity = 1; @filter Blur { Radius = 1; } } }
                <Sprite2D Name="Door" X="$root.DoorX:OneWay" Width="8" Height="8" />
                <SceneItems2D>
                    @templates {
                        <ContentTemplate DataType="System.String">
                            <Sprite2D Width="4" Height="4" />
                        </ContentTemplate>
                    }
                </SceneItems2D>
            </Scene2D>
            """;
        Assembly assembly = CompileSceneComponent(markup, "<Scene2D />");
        Scene2D house = (Scene2D)Activator.CreateInstance(assembly.GetType("Game.HouseView")!)!;
        SceneItems2D items = (SceneItems2D)house.Children[1];
        items.ItemsSource = new[] { "first", "second" };
        RenderSurface2D surface = new() { Scene = house };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        try
        {
            Assert.Equal(7, ((Sprite2D)house.Children[0]).X);
            Assert.Equal(1, house.GetType().GetProperty("LoadCount")!.GetValue(house));
            Assert.Equal(2, items.LogicalChildren.Count);
            Assert.All(items.LogicalChildren, item => Assert.IsType<Sprite2D>(item));
        }
        finally { root.VisualChildren.Remove(surface); }
    }

    [Theory]
    [InlineData("public class HouseView : Scene2D { }", "partial")]
    [InlineData("public abstract partial class HouseView : Scene2D { }", "concrete")]
    [InlineData("public partial class HouseView<T> : Scene2D { }", "non-generic")]
    [InlineData("public partial class HouseView : UserControl { }", "Scene2D")]
    [InlineData("public partial class HouseView : Scene2D { public HouseView() { } }", "constructors")]
    public void SceneComponentRejectsInvalidCompanionDeclarations(string declaration, string message)
    {
        GeneratorRunResult result = RunGenerator(
            [new MarkupFile("HouseView.crn", "<Scene2D />")], out _,
            "using Cerneala.UI.Controls; namespace Game; " + declaration, "HouseView.crn.cs");
        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI017", diagnostic.Id);
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("<Scene2D Name=\"Root\" />", "Name")]
    [InlineData("<Scene2D><Sprite2D Name=\"Door\" /></Scene2D>", "Door")]
    public void SceneComponentRejectsRootNamesAndConflictingGeneratedMembers(string markup, string message)
    {
        GeneratorRunResult result = RunGenerator(
            [new MarkupFile("HouseView.crn", markup)], out _,
            "using Cerneala.UI.Controls; namespace Game; public partial class HouseView : Scene2D { private int Door; }",
            "HouseView.crn.cs");
        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI017", diagnostic.Id);
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    private static Assembly CompileSceneComponent(string markup, string world, string? code = null)
    {
        GeneratorRunResult result = RunGenerator(
            [new MarkupFile("HouseView.crn", markup), new MarkupFile("World.crn", world)],
            out Compilation compilation, code ?? HouseComponentCode, "HouseView.crn.cs");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Assert.DoesNotContain(result.GeneratedSources, source => source.SourceText.ToString().Contains("HouseViewFactory", StringComparison.Ordinal));
        return Assembly.Load(stream.ToArray());
    }
}
