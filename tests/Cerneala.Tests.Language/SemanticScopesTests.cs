using System.Text;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class SemanticScopesTests
{
    private const string ViewModels = """
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Runtime.CompilerServices;
        using Cerneala.UI.Controls;
        namespace Demo;
        public sealed class Details : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Title { get; set; } = string.Empty;
        }
        public sealed class Row : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name { get; set; } = string.Empty;
        }
        public sealed class ViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Title { get; set; } = string.Empty;
            public Details Details { get; set; } = new();
            public List<Row> Rows { get; set; } = new();
        }
        public partial class MainView : UserControl
        {
            public string Caption { get; set; } = string.Empty;
            public List<Row> Rows { get; set; } = new();
        }
        """;

    [Fact]
    public void BindingQueriesResolveTypedSourcesSegmentsModesAndLocalDataContext()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <StackPanel Name="Host" DataContext="$DataContext.Details">
                <TextBlock Text="$DataContext.Title:TwoWay" />
                <TextBlock Text="$root.Caption" />
                <TextBlock Text="$Host.IsEnabled" />
                <TextBlock Text="$self.IsEnabled" />
              </StackPanel>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        Assert.Equal("Demo.ViewModel", SymbolAt(model, markup, "ViewModel").ValueType);
        Assert.Equal("Demo.Details", SymbolAt(model, markup, "Details").ValueType);
        Assert.Equal("string", SymbolAt(model, markup, "Title:TwoWay").ValueType);
        Assert.Equal(CernealaSemanticSymbolKind.BindingMode, SymbolAt(model, markup, "TwoWay").Kind);
        Assert.Equal("Demo.MainView", SymbolAt(model, markup, "$root").ValueType);
        Assert.Equal("Cerneala.UI.Controls.StackPanel", SymbolAt(model, markup, "$Host").ValueType);
        Assert.Equal("Cerneala.UI.Controls.TextBlock", SymbolAt(model, markup, "$self").ValueType);
    }

    [Fact]
    public void UnknownBindingSourceProducesOneDiagnosticWithoutSegmentCascade()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <TextBlock Text="$Missing.First.Second.Third" />
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        LanguageDiagnostic diagnostic = Assert.Single(model.Diagnostics);
        Assert.Equal("CERNEALAUI007", diagnostic.Id);
        Assert.DoesNotContain(model.Symbols, symbol => symbol.Name is "First" or "Second" or "Third");
    }

    [Fact]
    public void NameScopesSeparateTemplatePartsAndResolvePartQueries()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <StackPanel>
                <Button Name="Card">
                  @template {
                <Border Name="Part" IsEnabled="$owner.IsEnabled" />
                  }
                </Button>
                <TextBlock Text="$Card.parts.$Part.IsEnabled" />
              </StackPanel>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol owner = SymbolAt(model, markup, "$owner");
        CernealaSemanticSymbol part = SymbolAt(model, markup, "$Part");
        Assert.Equal("Cerneala.UI.Controls.Button", owner.ValueType);
        Assert.Equal(CernealaSemanticSymbolKind.TemplatePart, part.Kind);
        Assert.Equal("Cerneala.UI.Controls.Border", part.ValueType);
        Assert.NotNull(part.DefinitionLocation);
    }

    [Fact]
    public void ResourcesShadowApplicationResourcesAndDuplicateNamesAreReported()
    {
        const string application = """
            <Application>
              <Application.Resources>
                <SolidColorBrush Name="Accent" Color="#112233" />
              </Application.Resources>
            </Application>
            """;
        const string local = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="#445566" />
              </StackPanel.Resources>
              <Border Background="$Accent" />
            </StackPanel>
            """;
        CernealaSemanticModel model = Model(
            "Local.crn",
            local,
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol reference = SymbolAt(model, local, "$Accent");
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, reference.Kind);
        Assert.Equal("Local.crn", reference.DefinitionLocation?.Path);

        const string duplicate = """
            <StackPanel><StackPanel.Resources>
              <SolidColorBrush Name="Accent" Color="#112233" />
              <SolidColorBrush Name="Accent" Color="#445566" />
            </StackPanel.Resources></StackPanel>
            """;
        Assert.Contains(Model("Duplicate.crn", duplicate, string.Empty).Diagnostics,
            diagnostic => diagnostic.Id == "CERNEALAUI005" && diagnostic.Message.Contains("Duplicate resource Name", StringComparison.Ordinal));
    }

    [Fact]
    public void ItemsTemplatesInferOrOverrideDataTypeAndModelContentOwnership()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <ItemsControl ItemsSource="$DataContext.Rows">
                <ItemsControl.Templates>
                  <ContentTemplate DataType="d:Row" Key="row" Priority="1">
                    <TextBlock Text="$DataContext.Name" />
                  </ContentTemplate>
                </ItemsControl.Templates>
                <ItemsControl.ItemsPanel><StackPanel /></ItemsControl.ItemsPanel>
              </ItemsControl>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        Assert.Equal("Demo.Row", SymbolAt(model, markup, "Row\" Key").ValueType);
        Assert.Equal("string", SymbolAt(model, markup, "Name\"").ValueType);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.ContentTemplate && symbol.ValueType == "Demo.Row");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PropertyElement && symbol.Name == "Templates");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PropertyElement && symbol.Name == "ItemsPanel");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.ContentOwner && symbol.Name == "Content");
    }

    [Fact]
    public void AspectResourcesAssignmentsConditionsTemplatesAndApplicationSiteAreBound()
    {
        const string application = """
            <Application>
              <Application.Resources>
                <Aspect Name="Primary" TargetType="Button">
                  @default { IsEnabled = true; }
                  @when ($self.IsEnabled) { IsEnabled = false; }
                  @template { <Border Name="Chrome" /> }
                </Aspect>
              </Application.Resources>
            </Application>
            """;
        const string local = "<Button Aspect=\"$Primary\" />";
        CernealaSemanticModel applicationModel = Model("App.crn", application, string.Empty);
        CernealaSemanticModel localModel = Model(
            "Local.crn",
            local,
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));

        Assert.Empty(applicationModel.Diagnostics);
        Assert.Empty(localModel.Diagnostics);
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Aspect && symbol.Name == "Primary");
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectAssignment && symbol.Name == "IsEnabled");
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectCondition);
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.TemplatePart && symbol.Name == "Chrome");
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, SymbolAt(localModel, local, "$Primary").Kind);

        CernealaSemanticModel invalid = Model(
            "Invalid.crn",
            "<TextBlock Aspect=\"$Primary\" />",
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));
        Assert.Contains(invalid.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI004");
    }

    [Fact]
    public void ValidBindingTemplateAndAspectCorpusMatchesSourceGeneratorDiagnostics()
    {
        (string Path, string Markup)[] cases =
        [
            ("Bindings.crn", "<StackPanel><Button Name=\"Source\" IsEnabled=\"true\"/><TextBlock Text=\"$Source.IsEnabled\" /></StackPanel>"),
            ("Templates.crn", "<Button>@template { <Border Name=\"Chrome\" IsEnabled=\"$owner.IsEnabled\" /> }</Button>"),
            ("Aspects.crn", "<StackPanel><StackPanel.Resources><Aspect Name=\"Primary\" TargetType=\"Button\">@default { IsEnabled = true; }</Aspect></StackPanel.Resources><Button Aspect=\"$Primary\" /></StackPanel>")
        ];

        foreach ((string path, string markup) in cases)
        {
            CernealaSemanticModel model = Model(path, markup, string.Empty);
            LanguagePipelineResult sourceGenerator = LanguagePipelineHarness.Analyze(path, markup);
            Assert.Empty(model.Diagnostics);
            Assert.Empty(sourceGenerator.SourceGeneratorDiagnostics);
        }
    }

    [Fact]
    public void RealPresentationBindingTemplateAndAspectMarkupHasNoCommonSemanticDivergences()
    {
        string root = CorpusCatalog.RepositoryRoot();
        string presentation = Path.Combine(root, "CernealaPresentation");
        string[] paths = Directory.EnumerateFiles(presentation, "*.crn")
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return text.Contains('$') || text.Contains("@template", StringComparison.Ordinal) || text.Contains("<Aspect", StringComparison.Ordinal);
            })
            .ToArray();
        Assert.NotEmpty(paths);

        SyntaxTree[] trees = Directory.EnumerateFiles(presentation, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path, encoding: Encoding.UTF8))
            .ToArray();
        List<MetadataReference> references = PlatformReferences().ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location));
        CSharpCompilation project = CSharpCompilation.Create(
            "CernealaPresentation.LanguageParity",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CernealaDocument[] documents = Directory.EnumerateFiles(presentation, "*.crn")
            .Select(path => new CernealaDocument(path, SourceText.From(File.ReadAllText(path))))
            .ToArray();
        using CernealaCompilation workspace = new(new RoslynCompilationSymbols(project), documents, AnalysisMode.Build);
        foreach (string path in paths)
        {
            Assert.All(workspace.GetSemanticModel(path).Diagnostics,
                diagnostic => Assert.DoesNotContain(diagnostic.Id, new[] { "CERNEALAUI006", "CERNEALAUI007", "CERNEALAUI012" }));
        }
    }

    private static CernealaSemanticSymbol SymbolAt(CernealaSemanticModel model, string markup, string needle)
    {
        int offset = markup.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, "Needle not found: " + needle);
        offset += needle.StartsWith("$", StringComparison.Ordinal) ? 1 : 0;
        return Assert.IsType<CernealaSemanticSymbol>(model.GetSymbolAt(offset));
    }

    private static CernealaSemanticModel Model(
        string path,
        string markup,
        string source,
        params CernealaDocument[] additionalDocuments)
    {
        CSharpCompilation project = CreateCompilation(source, path + ".cs");
        CernealaDocument document = new(path, SourceText.From(markup));
        CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(project),
            additionalDocuments.Prepend(document),
            AnalysisMode.Build);
        return workspace.GetSemanticModel(path);
    }

    private static CSharpCompilation CreateCompilation(string source, string path)
    {
        List<MetadataReference> references = PlatformReferences().ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location));
        SyntaxTree[] trees = source.Length == 0
            ? []
            : [CSharpSyntaxTree.ParseText(source, path: path, encoding: Encoding.UTF8)];
        return CSharpCompilation.Create(
            "SemanticScopeTests",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
    }
}
