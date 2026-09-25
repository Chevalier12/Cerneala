using System.Reflection;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

public sealed class TileMapSourceSemanticTests
{
    private const string MarkupPath = "C:/tile-map-source-contract/World.crn";

    [Fact]
    public void FixtureUsesTheCurrentPublicCoreSurface()
    {
        Assert.Null(typeof(TileMap2D).GetProperty("Source", BindingFlags.Public | BindingFlags.Instance));
        Assert.NotNull(typeof(Image).GetProperty("Source", BindingFlags.Public | BindingFlags.Instance));
    }

    [Theory]
    [InlineData("<TileMap2D Source=\"removed\" />")]
    [InlineData("<TileMap2D><TileMap2D.Source /></TileMap2D>")]
    [InlineData("<TileMap2D><TileMap2D.Aspect>@default { Source = null; }</TileMap2D.Aspect></TileMap2D>")]
    public void RemovedMapSourceIsDiagnosedAsAnUnknownMemberWithoutBindingAdvice(string markup)
    {
        using CernealaCompilation compilation = Compile(markup);
        IReadOnlyList<LanguageDiagnostic> diagnostics = compilation.GetSemanticModel(MarkupPath).Diagnostics;

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI003" &&
            diagnostic.Message.Contains("Source", StringComparison.Ordinal));
        Assert.DoesNotContain(diagnostics, diagnostic =>
            diagnostic.Message.Contains("Source binding", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidMapSourceAttributeDoesNotHideStaticTileSemantics()
    {
        using CernealaCompilation compilation = Compile(
            "<TileMap2D Source=\"removed\"><Tile Image=\"$Grass\" Width=\"16\" Height=\"16\" /></TileMap2D>");
        CernealaSemanticModel model = compilation.GetSemanticModel(MarkupPath);

        Assert.Contains(model.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI003" &&
            diagnostic.Message.Contains("Source", StringComparison.Ordinal));
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Element &&
            symbol.Name == "Tile");
    }

    [Fact]
    public void CompletionNeverOffersRemovedMapSourceButKeepsStaticTile()
    {
        string[] mapAttributes = Complete("<TileMap2D | />");
        string[] mapProperties = Complete("<TileMap2D><TileMap2D.| /></TileMap2D>");
        string[] tiles = Complete("<TileMap2D><Ti| /></TileMap2D>");

        Assert.DoesNotContain("Source", mapAttributes);
        Assert.DoesNotContain("TileMap2D.Source", mapProperties);
        Assert.Contains("Tile", tiles);
    }

    [Fact]
    public void OrdinaryImageSourceBindingStillResolves()
    {
        using CernealaCompilation compilation = Compile(
            "<Window DataType=\"Test.ViewModel\"><Image Source=\"$DataContext.Picture:OneWay\" /></Window>");
        CernealaSemanticModel model = compilation.GetSemanticModel(MarkupPath);

        Assert.DoesNotContain(model.Diagnostics, diagnostic =>
            diagnostic.Severity == LanguageDiagnosticSeverity.Error);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Property &&
            symbol.Name == "Source" && symbol.IsWritable);
    }

    private static string[] Complete(string marked)
    {
        int offset = marked.IndexOf('|');
        Assert.True(offset >= 0);
        string markup = marked.Remove(offset, 1);
        using CernealaCompilation compilation = Compile(markup);
        CernealaDocument document = new(MarkupPath, LanguageSourceText.From(markup));
        return new CernealaCompletionService().GetCompletions(
            document, compilation.GetSemanticModel(MarkupPath), offset)
            .Select(item => item.Label).ToArray();
    }

    private static CernealaCompilation Compile(string markup)
    {
        const string code = """
            using System.ComponentModel;
            using Cerneala.Drawing;

            namespace Test;

            public sealed class ViewModel : INotifyPropertyChanged
            {
                public IDrawImage? Picture { get; set; }
                public event PropertyChangedEventHandler? PropertyChanged;
            }
            """;
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(typeof(UIElement).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
        CSharpCompilation project = CSharpCompilation.Create("TileMapSourceContractCorpus",
            [CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return new CernealaCompilation(new RoslynCompilationSymbols(project),
            [new CernealaDocument(MarkupPath, LanguageSourceText.From(markup))], AnalysisMode.Build);
    }
}
