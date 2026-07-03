using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cerneala.SourceGen;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed class UiMarkupGeneratorTests
{
    [Fact]
    public void SupportedMarkupEmitsCompilableFactory()
    {
        const string markup = """
            <StackPanel>
              <TextBlock Text="Hello" FontSize="18" />
              <Button Content="Go" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("Sample.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public static partial class SampleFactory", generatedSource);
        Assert.Contains("global::Cerneala.UI.Controls.StackPanel", generatedSource);
        Assert.Contains(".Text = \"Hello\";", generatedSource);
        Assert.Contains(".Content = \"Go\";", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        UIElement root = InvokeCreate(stream, "Cerneala.GeneratedUi.SampleFactory");
        StackPanel panel = Assert.IsType<StackPanel>(root);
        Assert.Equal(2, panel.VisualChildren.Count);
        TextBlock text = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Assert.Equal("Hello", text.Text);
        Assert.Equal(18, text.FontSize);
        Button button = Assert.IsType<Button>(panel.VisualChildren[1]);
        Assert.Equal("Go", button.Content);
    }

    [Fact]
    public void GeneratedSourceUsesPublicTypedPropertiesWithoutRuntimeMarkupParser()
    {
        const string markup = """
            <Border BorderColor="white" BorderThickness="1" Padding="4">
              <TextBlock Text="Typed" Foreground="0, 0, 0" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("typed-view.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.Contains("public static partial class TypedViewFactory", generatedSource);
        Assert.Contains(".BorderColor = global::Cerneala.Drawing.DrawColor.White;", generatedSource);
        Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
        Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(4f);", generatedSource);
        Assert.Contains(".Foreground = new global::Cerneala.Drawing.DrawColor(0, 0, 0);", generatedSource);
        Assert.DoesNotContain("UiMarkupReader", generatedSource);
        Assert.DoesNotContain("UiMarkupParser", generatedSource);
        Assert.DoesNotContain("UiMarkupSerializer", generatedSource);
        Assert.DoesNotContain("SetValue(", generatedSource);
        Assert.DoesNotContain("propertyStore", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.TypedViewFactory"));
        Assert.Equal(1, border.BorderThickness.Left);
        Assert.Equal(4, border.Padding.Left);
        TextBlock child = Assert.IsType<TextBlock>(border.Child);
        Assert.Equal("Typed", child.Text);
    }

    [Fact]
    public void MalformedMarkupReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Broken.cui.xml", "<StackPanel>", out _);

        AssertDiagnostic(result, "CERNEALAUI001", "Broken.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedElementReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Unsupported.cui.xml", "<Grid />", out _);

        AssertDiagnostic(result, "CERNEALAUI002", "Unsupported.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedPropertyReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("UnsupportedProperty.cui.xml", "<TextBlock Width=\"12\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedProperty.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ControlPropertiesOnStackPanelReportDiagnosticInsteadOfGeneratingInvalidCode()
    {
        GeneratorRunResult result = RunGenerator("BadPanel.cui.xml", "<StackPanel Padding=\"4\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "BadPanel.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void InvalidRuntimeValidatedValuesReportDiagnostics()
    {
        GeneratorRunResult fontResult = RunGenerator("BadFont.cui.xml", "<TextBlock FontSize=\"0\" />", out _);
        GeneratorRunResult paddingResult = RunGenerator("BadPadding.cui.xml", "<Border Padding=\"-1\" />", out _);

        AssertDiagnostic(fontResult, "CERNEALAUI004", "BadFont.cui.xml");
        AssertDiagnostic(paddingResult, "CERNEALAUI004", "BadPadding.cui.xml");
    }

    [Fact]
    public void DistinctMarkupFilesWithSameBaseNameEmitUniqueFactories()
    {
        GeneratorRunResult result = RunGenerator(
            new MarkupFile("Views/Main.cui.xml", "<TextBlock Text=\"View\" />"),
            new MarkupFile("Dialogs/Main.cui.xml", "<TextBlock Text=\"Dialog\" />"));

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2, result.GeneratedSources.Length);
        Assert.Equal(2, result.GeneratedSources.Select(source => source.HintName).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("ViewsMainFactory"));
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("DialogsMainFactory"));
    }

    private static GeneratorRunResult RunGenerator(string fileName, string markup, out Compilation outputCompilation)
    {
        return RunGenerator(new[] { new MarkupFile(fileName, markup) }, out outputCompilation);
    }

    private static GeneratorRunResult RunGenerator(params MarkupFile[] files)
    {
        return RunGenerator(files, out _);
    }

    private static GeneratorRunResult RunGenerator(MarkupFile[] files, out Compilation outputCompilation)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            "namespace TestInput { public static class Anchor { } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { syntaxTree },
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new UiMarkupGenerator().AsSourceGenerator() },
            files.Select(file => new InMemoryAdditionalText(file.Path, file.Text)).ToArray(),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
        return driver.GetRunResult().Results.Single();
    }

    private static MetadataReference[] References()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .ToArray();
    }

    private static string SingleGeneratedSource(GeneratorRunResult result)
    {
        return result.GeneratedSources.Single().SourceText.ToString();
    }

    private static void AssertDiagnostic(GeneratorRunResult result, string id, string path)
    {
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(path, diagnostic.Location.GetLineSpan().Path);
    }

    private static UIElement InvokeCreate(MemoryStream stream, string typeName)
    {
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        MethodInfo method = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Generated factory Create method was not found.");
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, null));
    }

    private readonly record struct MarkupFile(string Path, string Text);

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            this.text = SourceText.From(text, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return text;
        }
    }
}
