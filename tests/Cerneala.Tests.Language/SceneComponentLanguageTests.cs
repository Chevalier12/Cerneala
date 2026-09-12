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

public sealed class SceneComponentLanguageTests
{
    private const string CodePath = "C:/scene-component-fixture/HouseView.crn.cs";
    private const string MarkupPath = "C:/scene-component-fixture/HouseView.crn";
    private const string WorldPath = "C:/scene-component-fixture/World.crn";
    private const string Code = """
        using Cerneala.UI.Controls;
        using Cerneala.UI.Core;
        namespace Game;
        public partial class HouseView : Scene2D
        {
            public static readonly UiProperty<float> DoorXProperty = UiProperty<float>.Register(
                nameof(DoorX), typeof(HouseView), new UiPropertyMetadata<float>(4));
            public float DoorX { get => GetValue(DoorXProperty); set => SetValue(DoorXProperty, value); }
        }
        """;

    [Fact]
    public void PairedRootAndImportedComponentResolveToTheAuthoredType()
    {
        const string markup = "<Scene2D DoorX=\"6\"><Sprite2D X=\"$root.DoorX:OneWay\" /></Scene2D>";
        const string world = "<Scene2D xmlns:local=\"clr-namespace:Game;assembly=SceneComponentCorpus\"><local:HouseView DoorX=\"8\" /></Scene2D>";
        using CernealaCompilation compilation = CreateCompilation(markup, world);
        CernealaSemanticModel model = compilation.GetSemanticModel(MarkupPath);
        CernealaSemanticModel consumer = compilation.GetSemanticModel(WorldPath);
        Assert.DoesNotContain(model.Diagnostics.Concat(consumer.Diagnostics), d => d.Severity == LanguageDiagnosticSeverity.Error);
        CernealaNavigationService navigation = new();
        Assert.Equal(CodePath, Assert.Single(navigation.GetDefinitions(model, 2)).Path.Replace('\\', '/'));
        Assert.Equal(CodePath, Assert.Single(navigation.GetDefinitions(model,
            markup.LastIndexOf("DoorX", StringComparison.Ordinal) + 1)).Path.Replace('\\', '/'));
        Assert.Equal(CodePath, Assert.Single(navigation.GetDefinitions(consumer,
            world.IndexOf("HouseView", StringComparison.Ordinal) + 1)).Path.Replace('\\', '/'));
    }

    [Theory]
    [InlineData("<Scene2D Do| />", "DoorX")]
    [InlineData("<Scene2D><Sprite2D X=\"$root.Do|\" /></Scene2D>", "DoorX")]
    public void PairedSceneRootCompletionIncludesCustomProperties(string marked, string expected)
    {
        int offset = marked.IndexOf('|');
        string markup = marked.Remove(offset, 1);
        using CernealaCompilation compilation = CreateCompilation(markup, "<Scene2D />");
        CernealaDocument document = new(MarkupPath, LanguageSourceText.From(markup));
        Assert.Contains(new CernealaCompletionService().GetCompletions(document,
            compilation.GetSemanticModel(MarkupPath), offset), item => item.Label == expected);
    }

    [Fact]
    public void SceneChildCompletionIncludesImportedComponents()
    {
        const string marked = "<Scene2D xmlns:local=\"clr-namespace:Game;assembly=SceneComponentCorpus\"><local:Ho| /></Scene2D>";
        int offset = marked.IndexOf('|');
        string markup = marked.Remove(offset, 1);
        using CernealaCompilation compilation = CreateCompilation("<Scene2D />", markup);
        CernealaDocument document = new(WorldPath, LanguageSourceText.From(markup));
        Assert.Contains(new CernealaCompletionService().GetCompletions(document,
            compilation.GetSemanticModel(WorldPath), offset), item => item.Label == "local:HouseView");
    }

    private static CernealaCompilation CreateCompilation(string markup, string world)
    {
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(typeof(UIElement).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        CSharpCompilation project = CSharpCompilation.Create("SceneComponentCorpus",
            [CSharpSyntaxTree.ParseText(Code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest), CodePath)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return new CernealaCompilation(new RoslynCompilationSymbols(project),
            [new CernealaDocument(MarkupPath, LanguageSourceText.From(markup)),
             new CernealaDocument(WorldPath, LanguageSourceText.From(world))]);
    }
}
