using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

// Markup contract for Sprite2D.Vision and Scene2D.Vision (docs/plans/2026-10-25-scene2d-vision.md):
// - <Sprite2D.Vision> and <Scene2D.Vision> each hold exactly one ConeCollider2D, which the generator assigns to the
//   Vision property of the owner (a Scene2D's cone is never one of its Children, a Sprite2D's never its Collider);
// - a body collider and a Vision can sit together in one Sprite2D, in either order;
// - a ConeCollider2D anywhere that is not a Vision, and anything but one cone in a Vision, is CERNEALAUI005 with a
//   message that names Vision, and nothing is generated.
// Vision and ConeCollider2D are read through reflection (ReadVision, ReadFloat); the tests exercise the generated code.
public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void SpriteVisionMarkupAssignsAConeToTheVisionSlotAndLeavesTheBodySlotEmpty()
    {
        const string markup = """
            <Scene2D>
                <Sprite2D>
                    <Sprite2D.Vision>
                        <ConeCollider2D Radius="150" Angle="90" Direction="0" />
                    </Sprite2D.Vision>
                </Sprite2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));

        UIElement vision = ReadVision(sprite);

        Assert.Equal("ConeCollider2D", vision.GetType().Name);
        Assert.Equal(150f, ReadFloat(vision, "Radius"));
        Assert.Equal(90f, ReadFloat(vision, "Angle"));
        Assert.Equal(0f, ReadFloat(vision, "Direction"));
        Assert.Null(sprite.Collider);
        Assert.Same(vision, Assert.Single(sprite.LogicalChildren));
        Assert.Same(sprite, vision.LogicalParent);
        Assert.Empty(sprite.VisualChildren);
    }

    [Theory]
    [InlineData(true)] // the body is declared before the Vision
    [InlineData(false)] // the body is declared after the Vision
    public void SpriteVisionMarkupKeepsTheBodyAndTheVisionInSeparateSlots(bool bodyFirst)
    {
        const string body = "<BoxCollider2D Width=\"10\" Height=\"10\" />";
        const string vision = "<Sprite2D.Vision><ConeCollider2D Radius=\"150\" /></Sprite2D.Vision>";
        string markup = $"<Scene2D><Sprite2D>{(bodyFirst ? body + vision : vision + body)}</Sprite2D></Scene2D>";
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));

        UIElement coneVision = ReadVision(sprite);

        Assert.IsType<BoxCollider2D>(sprite.Collider);
        Assert.Equal("ConeCollider2D", coneVision.GetType().Name);
        Assert.Equal(150f, ReadFloat(coneVision, "Radius"));
        Assert.Equal(2, sprite.LogicalChildren.Count);
        Assert.Contains(sprite.Collider, sprite.LogicalChildren);
        Assert.Contains(coneVision, sprite.LogicalChildren);
    }

    [Fact]
    public void SceneVisionMarkupAssignsAConeToTheVisionOfTheNestedGroupOnly()
    {
        const string markup = """
            <Scene2D>
                <Scene2D>
                    <Scene2D.Vision>
                        <ConeCollider2D Radius="80" />
                    </Scene2D.Vision>
                </Scene2D>
            </Scene2D>
            """;
        Scene2D root = (Scene2D)CompileColliderMarkup(markup);
        Scene2D group = Assert.IsType<Scene2D>(Assert.Single(root.Children));

        UIElement vision = ReadVision(group);

        Assert.Equal("ConeCollider2D", vision.GetType().Name);
        Assert.Equal(80f, ReadFloat(vision, "Radius"));
        Assert.Equal(90f, ReadFloat(vision, "Angle")); // the defaults apply to what the markup leaves out
        Assert.Equal(0f, ReadFloat(vision, "Direction"));
        Assert.Same(group, vision.LogicalParent);
        Assert.Empty(group.Children);
        Assert.Null(root.GetType().GetProperty("Vision")!.GetValue(root));
    }

    [Fact]
    public void SceneVisionMarkupOnTheRootSceneKeepsItOutOfItsChildren()
    {
        const string markup = """
            <Scene2D>
                <Scene2D.Vision>
                    <ConeCollider2D Radius="120" Angle="300" />
                </Scene2D.Vision>
                <Sprite2D />
            </Scene2D>
            """;
        Scene2D root = (Scene2D)CompileColliderMarkup(markup);

        UIElement vision = ReadVision(root);

        Assert.Equal(120f, ReadFloat(vision, "Radius"));
        Assert.Equal(300f, ReadFloat(vision, "Angle"));
        Assert.IsType<Sprite2D>(Assert.Single(root.Children));
        Assert.Contains(vision, root.LogicalChildren);
    }

    // A cone is a sensor, not a body: it can only be declared in Vision (user decision, 2026-10-25).
    [Theory]
    [InlineData("<Scene2D><Sprite2D><ConeCollider2D /></Sprite2D></Scene2D>")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Collider><ConeCollider2D /></Sprite2D.Collider></Sprite2D></Scene2D>")]
    public void ConeInTheSpriteBodySlotMarkupIsRejectedWithAMessageThatNamesVision(string markup)
    {
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("Vision", StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    // Stage 4 additions. The Language semantic model decides placement and type; the emitter decides the shape of the
    // property element itself (empty, declared twice), which the editor cannot see as a collider placement error.
    [Theory]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision><BoxCollider2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision accepts a ConeCollider2D only")]
    [InlineData("<Scene2D><Scene2D.Vision><CircleCollider2D /></Scene2D.Vision></Scene2D>", "Scene2D.Vision accepts a ConeCollider2D only")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision><Sprite2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision accepts a ConeCollider2D only")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision><ConeCollider2D /><ConeCollider2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision accepts exactly one ConeCollider2D")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision /></Sprite2D></Scene2D>", "Sprite2D.Vision requires exactly one ConeCollider2D child")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision>text<ConeCollider2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision requires exactly one ConeCollider2D child")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision Radius=\"5\"><ConeCollider2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision requires exactly one ConeCollider2D child")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision><ConeCollider2D /></Sprite2D.Vision><Sprite2D.Vision><ConeCollider2D /></Sprite2D.Vision></Sprite2D></Scene2D>", "Sprite2D.Vision may be declared only once")]
    public void VisionMarkupRejectsAnythingButOneConeInTheSlot(string markup, string expected)
    {
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains(expected, StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    // Whitespace, comments and namespace declarations are not content of the Vision element.
    [Theory]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision>  <!-- looks right -->  <ConeCollider2D Radius=\"7\" /></Sprite2D.Vision></Sprite2D></Scene2D>")]
    [InlineData("<Scene2D><Sprite2D><Sprite2D.Vision xmlns:x=\"clr-namespace:Cerneala.UI.Controls;assembly=Cerneala\"><ConeCollider2D Radius=\"7\" /></Sprite2D.Vision></Sprite2D></Scene2D>")]
    public void VisionMarkupAllowsWhitespaceCommentsAndNamespaceDeclarationsOnTheVisionElement(string markup)
    {
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));

        Assert.Equal(7f, ReadFloat(ReadVision(sprite), "Radius"));
    }

    [Theory]
    [InlineData("<Scene2D><ConeCollider2D /></Scene2D>")]
    [InlineData("<Button><ConeCollider2D /></Button>")]
    public void ConeOutsideAVisionSlotMarkupIsRejectedWithAMessageThatNamesVision(string markup)
    {
        GeneratorRunResult result = RunGenerator([new MarkupFile("Ownership.crn", markup)], out _, "");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI005" &&
            diagnostic.GetMessage().Contains("Vision", StringComparison.Ordinal));
        Assert.Empty(result.GeneratedSources);
    }

    // The markup and the message that docs-site/documentation/classes/Cerneala.UI.Controls.ConeCollider2D.md quotes.
    [Fact]
    public void TheDocumentedSpriteMarkupGivesTheSpriteABodyAndAVision()
    {
        const string markup = """
            <Scene2D>
                <Sprite2D Width="32" Height="32">
                    <BoxCollider2D Width="10" Height="10" OffsetX="-12" OffsetY="-5" />
                    <Sprite2D.Vision>
                        <ConeCollider2D Radius="150" Angle="90" Direction="0" />
                    </Sprite2D.Vision>
                </Sprite2D>
            </Scene2D>
            """;
        Scene2D scene = (Scene2D)CompileColliderMarkup(markup);
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(scene.Children));

        UIElement vision = ReadVision(sprite);

        BoxCollider2D body = Assert.IsType<BoxCollider2D>(sprite.Collider);
        Assert.Equal(-12f, body.OffsetX);
        Assert.Equal(150f, ReadFloat(vision, "Radius"));
        Assert.Equal(90f, ReadFloat(vision, "Angle"));
        Assert.Equal(0f, ReadFloat(vision, "Direction"));
        Assert.Equal(2, sprite.LogicalChildren.Count);
    }

    [Fact]
    public void TheDocumentedGroupMarkupGivesTheInnerGroupAVisionAndOneChild()
    {
        const string markup = """
            <Scene2D>
                <Scene2D X="100" Y="50">
                    <Scene2D.Vision>
                        <ConeCollider2D Radius="150" Angle="90" Direction="0" />
                    </Scene2D.Vision>
                    <Sprite2D X="-40" Y="-5" Width="10" Height="10">
                        <BoxCollider2D Width="10" Height="10" />
                    </Sprite2D>
                </Scene2D>
            </Scene2D>
            """;
        Scene2D root = (Scene2D)CompileColliderMarkup(markup);
        Scene2D group = Assert.IsType<Scene2D>(Assert.Single(root.Children));

        UIElement vision = ReadVision(group);

        Assert.Equal(150f, ReadFloat(vision, "Radius"));
        Assert.Equal(100f, group.X);
        Assert.IsType<Sprite2D>(Assert.Single(group.Children));
        Assert.Null(root.GetType().GetProperty("Vision")!.GetValue(root));
    }

    [Fact]
    public void TheDocumentedVisionBuildMessageForAConeAsTheBodyIsTheOneTheDocsQuote()
    {
        GeneratorRunResult result = RunGenerator(
            [new MarkupFile("World.crn", "<Scene2D><Sprite2D><ConeCollider2D /></Sprite2D></Scene2D>")], out _, "");
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Id == "CERNEALAUI005");
        Assert.Equal(
            "Markup file 'World.crn' has invalid document shape: " +
            "ConeCollider2D can only be a Vision; put it inside <Sprite2D.Vision> or <Scene2D.Vision>.",
            diagnostic.GetMessage());
    }

    [Fact]
    public void SceneVisionMarkupOnASceneComponentRootAssignsTheConeToTheComponent()
    {
        const string code = """
            using Cerneala.UI.Controls;
            namespace Game;
            public partial class HouseView : Scene2D { }
            """;
        const string markup = """
            <Scene2D>
                <Scene2D.Vision>
                    <ConeCollider2D Radius="60" Angle="120" Direction="270" />
                </Scene2D.Vision>
                <Sprite2D />
            </Scene2D>
            """;
        Assembly assembly = CompileSceneComponent(markup, "<Scene2D />", code);
        Scene2D house = (Scene2D)Activator.CreateInstance(assembly.GetType("Game.HouseView")!)!;

        UIElement vision = ReadVision(house);

        Assert.Equal(60f, ReadFloat(vision, "Radius"));
        Assert.Equal(120f, ReadFloat(vision, "Angle"));
        Assert.Equal(270f, ReadFloat(vision, "Direction"));
        Assert.IsType<Sprite2D>(Assert.Single(house.Children));
        Assert.Same(house, vision.LogicalParent);
    }

    private static UIElement ReadVision(UIElement owner)
    {
        PropertyInfo? property = owner.GetType().GetProperty("Vision");
        Assert.True(property is not null, owner.GetType().Name + " has no Vision property.");
        object? vision = property.GetValue(owner);
        Assert.NotNull(vision);
        return Assert.IsAssignableFrom<UIElement>(vision);
    }

    private static float ReadFloat(object target, string name)
    {
        PropertyInfo? property = target.GetType().GetProperty(name);
        Assert.True(property is not null, target.GetType().Name + " has no " + name + " property.");
        return Assert.IsType<float>(property.GetValue(target));
    }
}
