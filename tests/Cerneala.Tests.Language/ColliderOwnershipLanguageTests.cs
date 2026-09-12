using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

public sealed class ColliderOwnershipLanguageTests
{
    private const string MarkupPath = "C:/collider-ownership-fixture/World.crn";
    private static readonly string[] Shapes = ["BoxCollider2D", "CircleCollider2D", "PolygonCollider2D", "SegmentCollider2D"];

    [Theory]
    [InlineData("<Scene2D><| /></Scene2D>", false)]
    [InlineData("<StackPanel><| /></StackPanel>", false)]
    [InlineData("<Border><| /></Border>", false)]
    [InlineData("<Scene2D><Sprite2D><| /></Sprite2D></Scene2D>", true)]
    [InlineData("<Scene2D><TileMap2D><TileLayer2D><TileInstance2D><| /></TileInstance2D></TileLayer2D></TileMap2D></Scene2D>", true)]
    [InlineData("<Scene2D><TileMap2D><Tile Image=\"$Wall\"><| /></Tile></TileMap2D></Scene2D>", true)]
    public void CompletionOffersColliderShapesOnlyAtValidOwners(string marked, bool expected)
    {
        string[] labels = Complete(marked);
        foreach (string shape in Shapes) { Assert.Equal(expected, labels.Contains(shape)); }
        if (expected)
        {
            Assert.DoesNotContain("Sprite2D", labels);
            Assert.DoesNotContain("Scene2D", labels);
        }
    }

    [Fact]
    public void StaticTileColliderCompletionOffersOnlyDescriptorProperties()
    {
        string[] labels = Complete("<Scene2D><TileMap2D><Tile Image=\"$Wall\"><BoxCollider2D | /></Tile></TileMap2D></Scene2D>");
        Assert.Equal(new[] { "CollisionLayer", "CollisionMask", "Height", "IsTrigger", "OffsetX", "OffsetY", "Width" },
            labels.OrderBy(value => value, StringComparer.Ordinal));
        Assert.Empty(Complete("<Scene2D><TileMap2D><Tile Image=\"$Wall\"><BoxCollider2D Width=\"$|\" /></Tile></TileMap2D></Scene2D>"));
        Assert.Empty(Complete("<Scene2D><TileMap2D><Tile Image=\"$Wall\"><BoxCollider2D>@|</BoxCollider2D></Tile></TileMap2D></Scene2D>"));
    }

    [Theory]
    [InlineData("<BoxCollider2D />")]
    [InlineData("<Scene2D><BoxCollider2D /></Scene2D>")]
    [InlineData("<Button><CircleCollider2D /></Button>")]
    [InlineData("<Border><PolygonCollider2D /></Border>")]
    [InlineData("<Scene2D><Sprite2D><BoxCollider2D><SegmentCollider2D /></BoxCollider2D></Sprite2D></Scene2D>")]
    public void DiagnosticsRejectColliderOwnershipOutsideSpritesAndTiles(string markup)
    {
        using CernealaCompilation compilation = Compile(markup);
        Assert.Contains(compilation.GetSemanticModel(MarkupPath).Diagnostics,
            diagnostic => diagnostic.Id == "CERNEALAUI005" && diagnostic.Message.Contains("Sprite2D", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("<BoxCollider2D Name=\"WallHitbox\" />")]
    [InlineData("<BoxCollider2D Width=\"$root.Width:OneWay\" />")]
    [InlineData("<BoxCollider2D Aspect=\"$ColliderAspect\" />")]
    [InlineData("<BoxCollider2D Enabled=\"false\" />")]
    [InlineData("<BoxCollider2D><BoxCollider2D.Aspect /></BoxCollider2D>")]
    [InlineData("<Sprite2D />")]
    public void StaticTileColliderDataCannotAcquireLiveNodeSemantics(string child)
    {
        string markup = "<Scene2D xmlns:r=\"clr-namespace:Cerneala.UI.Resources;assembly=Cerneala\"><Scene2D.Resources>" +
            "<r:ImageResource Name=\"Wall\" Source=\"wall.png\" /></Scene2D.Resources><TileMap2D><Tile Image=\"$Wall\">" +
            child + "</Tile></TileMap2D></Scene2D>";
        using CernealaCompilation compilation = Compile(markup);
        Assert.Contains(compilation.GetSemanticModel(MarkupPath).Diagnostics,
            diagnostic => diagnostic.Severity == LanguageDiagnosticSeverity.Error);
    }

    private static string[] Complete(string marked)
    {
        int offset = marked.IndexOf('|');
        string markup = marked.Remove(offset, 1);
        using CernealaCompilation compilation = Compile(markup);
        return new CernealaCompletionService().GetCompletions(new CernealaDocument(MarkupPath, LanguageSourceText.From(markup)),
            compilation.GetSemanticModel(MarkupPath), offset).Select(item => item.Label).ToArray();
    }

    private static CernealaCompilation Compile(string markup)
    {
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(typeof(UIElement).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        CSharpCompilation project = CSharpCompilation.Create("ColliderOwnershipCorpus", references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return new CernealaCompilation(new RoslynCompilationSymbols(project),
            [new CernealaDocument(MarkupPath, LanguageSourceText.From(markup))]);
    }
}
