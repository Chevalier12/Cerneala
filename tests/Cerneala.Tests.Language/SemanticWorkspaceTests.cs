using System.Text;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class SemanticWorkspaceTests
{
    [Fact]
    public void RoslynAdapterResolvesPairedPartialRootMembersDocsAndLocations()
    {
        CSharpCompilation project = CreateCompilation(
        [
            new SourceFile("Views/MainView.cui.xml.cs", """
                using System;
                using Cerneala.UI.Controls;
                namespace Demo.Views;
                /// <summary>Paired view documentation.</summary>
                public partial class MainView : UserControl
                {
                    public int Count { get; set; }
                    public event EventHandler? Ready;
                }
                """),
            new SourceFile("Views/MainView.Part.cs", """
                namespace Demo.Views;
                public partial class MainView { public string Label { get; set; } = string.Empty; }
                """)
        ]);
        RoslynCompilationSymbols adapter = new(project, version: 4);
        CernealaDocument document = Document(
            "Views/MainView.cui.xml",
            "<UserControl Count=\"4\" Ready=\"OnReady\"><TextBlock Text=\"Hello\" /></UserControl>");
        using CernealaCompilation compilation = new(adapter, [document], AnalysisMode.Build);

        CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);
        CernealaSemanticSymbol root = Assert.Single(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.RootType);
        CernealaSemanticSymbol count = Assert.Single(model.Symbols, symbol => symbol.Name == "Count");
        CernealaSemanticSymbol ready = Assert.Single(model.Symbols, symbol => symbol.Name == "Ready");

        Assert.Equal("Demo.Views.MainView", root.ValueType);
        Assert.Equal("Content", root.ContentPropertyName);
        Assert.Contains("Paired view documentation", root.TypeSymbol!.DocumentationXml, StringComparison.Ordinal);
        Assert.Contains(root.TypeSymbol.Locations, location => location.Path.EndsWith("MainView.cui.xml.cs", StringComparison.Ordinal));
        Assert.Equal(4, count.Value);
        Assert.Equal(CernealaSemanticSymbolKind.Event, ready.Kind);
        Assert.Empty(model.Diagnostics);
    }

    [Theory]
    [InlineData("Application", "Cerneala.UI.Application")]
    [InlineData("Window", "Cerneala.UI.Controls.Window")]
    [InlineData("UserControl", "Cerneala.UI.Controls.UserControl")]
    public void BuiltInRootKindsResolveWithoutCompanionFiles(string rootName, string expectedType)
    {
        CSharpCompilation project = CreateCompilation([]);
        CernealaDocument document = Document(rootName + ".cui.xml", "<" + rootName + " />");
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document]);

        CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);

        Assert.Empty(model.Diagnostics);
        Assert.Equal(expectedType, Assert.Single(model.Symbols).ValueType);
    }

    [Fact]
    public void SemanticModelResolvesClrAliasesAttachedPropertiesPropertyElementsAndLiterals()
    {
        CSharpCompilation project = CreateCompilation(
        [
            new SourceFile("Widgets.cs", """
                using Cerneala.UI.Controls;
                namespace Widgets;
                public sealed class FancyButton : Button
                {
                    public int Count { get; set; }
                }
                """)
        ]);
        const string markup = """
            <StackPanel xmlns:w="clr-namespace:Widgets">
              <StackPanel.Resources />
              <w:FancyButton Count="7" Grid.Row="2" IsEnabled="false" />
            </StackPanel>
            """;
        CernealaDocument document = Document("Alias.cui.xml", markup);
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document], AnalysisMode.Build);

        CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);

        Assert.Empty(model.Diagnostics);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PropertyElement && symbol.Name == "Resources");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AttachedProperty && symbol.Name == "Grid.Row" && Equals(symbol.Value, 2));
        Assert.Contains(model.Symbols, symbol => symbol.Name == "Count" && Equals(symbol.Value, 7));
        Assert.Contains(model.Symbols, symbol => symbol.Name == "IsEnabled" && Equals(symbol.Value, false));
        Assert.Contains(model.Symbols, symbol => symbol.Name == "w:FancyButton" && symbol.ValueType == "Widgets.FancyButton");
    }

    [Fact]
    public void ProjectReferencesAndAssemblyQualifiedAliasesResolveWithoutLoadingAssemblies()
    {
        CSharpCompilation referenced = CreateCompilation(
            [new SourceFile("External.cs", """
                using Cerneala.UI.Controls;
                namespace External.Widgets;
                public sealed class ReferencedButton : Button { }
                """)],
            assemblyName: "ExternalWidgets");
        using MemoryStream image = new();
        Assert.True(referenced.Emit(image).Success);
        MetadataReference projectReference = MetadataReference.CreateFromImage(image.ToArray());
        CSharpCompilation project = CreateCompilation([], additionalReferences: [projectReference]);
        CernealaDocument document = Document(
            "Reference.cui.xml",
            "<ext:ReferencedButton xmlns:ext=\"clr-namespace:External.Widgets;assembly=ExternalWidgets\" />");
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document]);

        CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);

        Assert.Empty(model.Diagnostics);
        Assert.Contains(model.Symbols, symbol => symbol.ValueType == "External.Widgets.ReferencedButton");
    }

    [Fact]
    public void DuplicateSimpleTypesAndInvalidLiteralsProduceFocusedDiagnostics()
    {
        CSharpCompilation project = CreateCompilation(
        [new SourceFile("Duplicates.cs", """
            using Cerneala.UI.Controls;
            namespace One { public sealed class Twin : Button { } }
            namespace Two { public sealed class Twin : Button { } }
            """)]);
        CernealaDocument duplicate = Document("Duplicate.cui.xml", "<Twin />");
        CernealaDocument literal = Document("Literal.cui.xml", "<Button IsEnabled=\"perhaps\" />");
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [duplicate, literal], AnalysisMode.Build);

        Assert.Equal("CERNEALAUI002", Assert.Single(compilation.GetSemanticModel(duplicate.Path).Diagnostics).Id);
        Assert.Equal("CERNEALAUI004", Assert.Single(compilation.GetSemanticModel(literal.Path).Diagnostics).Id);
    }

    [Fact]
    public void IndependentCSharpErrorsDoNotPreventValidMarkupBinding()
    {
        CSharpCompilation project = CreateCompilation(
        [new SourceFile("Broken.cs", """
            using Cerneala.UI.Controls;
            namespace Demo;
            public sealed class GoodButton : Button { public int Level { get; set; } }
            public sealed class Broken { void Missing( }
            """)]);
        Assert.Contains(project.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        CernealaDocument document = Document(
            "Errors.cui.xml",
            "<g:GoodButton xmlns:g=\"clr-namespace:Demo\" Level=\"3\" />");
        using CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document]);

        CernealaSemanticModel model = compilation.GetSemanticModel(document.Path);

        Assert.Empty(model.Diagnostics);
        Assert.Contains(model.Symbols, symbol => symbol.Name == "Level" && Equals(symbol.Value, 3));
    }

    [Fact]
    public void SemanticResultsAreDeterministicAcrossEquivalentHosts()
    {
        CSharpCompilation project = CreateCompilation([]);
        CernealaDocument document = Document(
            "Stable.cui.xml",
            "<Button IsEnabled=\"true\" Content=\"Save\" />");
        using CernealaCompilation first = new(new RoslynCompilationSymbols(project, 1), [document], AnalysisMode.Build);
        using CernealaCompilation second = new(new RoslynCompilationSymbols(project, 1), [document], AnalysisMode.Build);

        string[] firstSymbols = Projection(first.GetSemanticModel(document.Path));
        string[] secondSymbols = Projection(second.GetSemanticModel(document.Path));

        Assert.Equal(firstSymbols, secondSymbols);
        Assert.Equal(
            first.GetSemanticModel(document.Path).Diagnostics.Select(DiagnosticProjection),
            second.GetSemanticModel(document.Path).Diagnostics.Select(DiagnosticProjection));
    }

    [Fact]
    public void VersionedCachesRetainOnlyUnaffectedDocuments()
    {
        CSharpCompilation project = CreateCompilation([]);
        CernealaDocument firstDocument = Document("First.cui.xml", "<Button />");
        CernealaDocument secondDocument = Document("Second.cui.xml", "<TextBlock />");
        CernealaCompilation original = new(new RoslynCompilationSymbols(project, version: 1), [firstDocument, secondDocument]);
        CernealaSemanticModel firstModel = original.GetSemanticModel(firstDocument.Path);
        CernealaSemanticModel secondModel = original.GetSemanticModel(secondDocument.Path);
        CernealaDocument changed = firstDocument.WithChange(new TextChange(new TextSpan(8, 0), " IsEnabled=\"true\""));
        using CernealaCompilation documentUpdate = original.WithDocument(changed);

        Assert.NotSame(firstModel, documentUpdate.GetSemanticModel(firstDocument.Path));
        Assert.Same(secondModel, documentUpdate.GetSemanticModel(secondDocument.Path));

        using CernealaCompilation projectUpdate = documentUpdate.WithProjectSymbols(new RoslynCompilationSymbols(project, version: 2));
        Assert.NotSame(secondModel, projectUpdate.GetSemanticModel(secondDocument.Path));

        original.Dispose();
        Assert.NotEmpty(documentUpdate.GetSemanticModel(secondDocument.Path).Symbols);
    }

    [Fact]
    public void LifecycleAndCancellationAreExplicit()
    {
        CSharpCompilation project = CreateCompilation([]);
        CernealaDocument document = Document("Cancel.cui.xml", "<Button />");
        CernealaCompilation compilation = new(new RoslynCompilationSymbols(project), [document]);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => compilation.GetSemanticModel(document.Path, cancellation.Token));
        compilation.Dispose();
        Assert.Throws<ObjectDisposedException>(() => compilation.GetSemanticModel(document.Path));
    }

    [Fact]
    public void SemanticCoreDoesNotLoadAssembliesOrUseReflection()
    {
        string root = Path.Combine(CorpusCatalog.RepositoryRoot(), "Cerneala.Language", "Semantics");
        string source = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("Assembly.Load", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetType(", source, StringComparison.Ordinal);
    }

    private static CernealaDocument Document(string path, string markup) =>
        new(path, SourceText.From(markup));

    private static string[] Projection(CernealaSemanticModel model) => model.Symbols
        .Select(symbol => $"{symbol.Span.Start}:{symbol.Kind}:{symbol.Name}:{symbol.ValueType}:{symbol.Value}")
        .ToArray();

    private static string DiagnosticProjection(LanguageDiagnostic diagnostic) =>
        $"{diagnostic.Span.Start}:{diagnostic.Id}:{diagnostic.Severity}:{diagnostic.Message}";

    private static CSharpCompilation CreateCompilation(
        IReadOnlyList<SourceFile> files,
        string assemblyName = "SemanticTests",
        IReadOnlyList<MetadataReference>? additionalReferences = null)
    {
        List<MetadataReference> references = PlatformReferences().ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location));
        if (additionalReferences is not null)
        {
            references.AddRange(additionalReferences);
        }

        return CSharpCompilation.Create(
            assemblyName,
            files.Select(file => CSharpSyntaxTree.ParseText(
                file.Text,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithDocumentationMode(DocumentationMode.Diagnose),
                file.Path,
                Encoding.UTF8)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
    }

    private readonly record struct SourceFile(string Path, string Text);
}
