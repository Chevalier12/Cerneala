using Cerneala.SourceGen;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.Tests.SourceGen;

public sealed class UiMarkupIncrementalTests
{
    [Theory]
    [InlineData("/// <summary>Old documentation.</summary>\n", "/// <summary>New documentation.</summary>\n")]
    [InlineData("", "\n\n  \n")]
    public void DocumentationAndDefinitionPositionEditsCacheGeneratorOutputs(string beforePrefix, string afterPrefix)
    {
        const string source = "public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; public string Caption { get; set; } = \"\"; }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(beforePrefix + source, path: "Model.cs");
        CSharpCompilation compilation = CreateCompilation(tree);
        GeneratorDriver driver = CreateDriver(new MarkupText("Bound.crn",
            "<TextBlock DataType=\"Model\" Text=\"$DataContext.Caption:OneWay\" />")).RunGenerators(compilation);
        GeneratorRunResult initial = driver.GetRunResult().Results.Single();
        Assert.Null(initial.Exception);
        Assert.Empty(initial.Diagnostics);
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(afterPrefix + source, path: "Model.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        AssertEquivalent(initial, result);
        AssertCached(result, "CernealaLanguageSemanticModel", 1);
        var outputs = result.TrackedOutputSteps.SelectMany(pair => pair.Value).SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConsumedCSharpDiagnosticLocationsMoveWithoutReanalyzingMarkup(bool backendError)
    {
        string source = "[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(Input.Backend))]\n" +
            "namespace Input { public partial class MainWindow : Cerneala.UI.Controls.Window { } " +
            "public static class Backend { " + (backendError ? "" : "public static void EnsureRegistered() { }") +
            " } public static class App { public static int ConfigureServices() => 0; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "MainWindow.crn.cs");
        CSharpCompilation compilation = CreateCompilation(tree).WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        MarkupText markup = new("MainWindow.crn", "<Window><Button /></Window>");
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        Diagnostic before = Assert.Single(driver.GetRunResult().Results.Single().Diagnostics);
        Assert.Equal(backendError ? "CERNEALAUI015" : "CERNEALAUI011", before.Id);
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText("\n\n" + source, path: "MainWindow.crn.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Diagnostic after = Assert.Single(result.Diagnostics);
        Assert.Equal(before.Location.GetLineSpan().StartLinePosition.Line + 2,
            after.Location.GetLineSpan().StartLinePosition.Line);
        AssertCached(result, "CernealaLanguageSemanticModel", 1);
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Fact]
    public void MemberDocumentationEditCachesGeneratorOutputs()
    {
        const string source = "public class Model {\n/// <summary>Old member documentation.</summary>\npublic string Caption { get; set; } = \"\";\n}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "Model.cs");
        CSharpCompilation compilation = CreateCompilation(tree);
        GeneratorDriver driver = CreateDriver(new MarkupText("Bound.crn", "<Button DataType=\"Model\" />"))
            .RunGenerators(compilation);
        GeneratorRunResult initial = driver.GetRunResult().Results.Single();
        Assert.Empty(initial.Diagnostics);
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(
            source.Replace("Old member", "New member"), path: "Model.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        AssertEquivalent(initial, result);
        AssertCached(result, "CernealaLanguageSemanticModel", 1);
        Assert.All(result.TrackedOutputSteps.SelectMany(pair => pair.Value).SelectMany(step => step.Outputs),
            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedNullableTypeFactsAreIndependentOfDocumentOrder(bool reverse)
    {
        const string source = "#nullable enable\nnamespace Input { public class Model { public Child? Value { get; set; } } public class Child { public string Caption { get; set; } = \"\"; } public class Unrelated { public int Value => 1; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "Models.cs");
        CSharpCompilation compilation = CreateCompilation(tree);
        MarkupText model = new("Model.crn", "<Button DataType=\"Input.Model\" />");
        MarkupText child = new("Child.crn", "<Button DataType=\"Input.Child\" />");
        AdditionalText[] files = reverse ? [child, model] : [model, child];
        GeneratorDriver driver = CreateDriver(files).RunGenerators(compilation);
        Assert.Empty(driver.GetRunResult().Results.Single().Diagnostics);
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(
            source.Replace("Value => 1", "Value => 2"), path: "Models.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        AssertCached(result, "CernealaLanguageSemanticModel", 2);
        AssertEquivalent(CreateDriver(files).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Theory]
    [InlineData("namespace Unrelated { public class Other { public int Value => 2; } }")]
    [InlineData("namespace Unrelated { public class Other { public string Renamed => \"new\"; } }")]
    [InlineData("namespace Unrelated { public class App { public string Renamed => \"new\"; } public class Control { } }")]
    public void UnrelatedCSharpEditCachesSemanticAnalysisAndEmission(string editedSource)
    {
        SyntaxTree unrelated = CSharpSyntaxTree.ParseText(
            "namespace Unrelated { public class Other { public int Value => 1; } }", path: "Other.cs");
        CSharpCompilation compilation = CreateCompilation(unrelated);
        GeneratorDriver driver = CreateDriver(
            new MarkupText("First.crn", "<Button Content=\"First\" />"),
            new MarkupText("Second.crn", "<Button Content=\"Second\" />"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult before = driver.GetRunResult().Results.Single();
        Assert.Null(before.Exception);
        Assert.Empty(before.Diagnostics);
        Assert.Equal(2, before.GeneratedSources.Length);

        compilation = compilation.ReplaceSyntaxTree(unrelated,
            CSharpSyntaxTree.ParseText(editedSource, path: "Other.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult after = driver.GetRunResult().Results.Single();
        Assert.Null(after.Exception);
        Assert.Empty(after.Diagnostics);
        Assert.Equal(before.GeneratedSources.Select(source => source.SourceText.ToString()),
            after.GeneratedSources.Select(source => source.SourceText.ToString()));
        AssertCached(after, "CernealaLanguageSemanticModel", 2);
        var outputSteps = after.TrackedOutputSteps.SelectMany(pair => pair.Value)
            .SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputSteps);
        Assert.All(outputSteps, output =>
            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
    }

    private static void AssertCached(GeneratorRunResult result, string name, int count)
    {
        var outputs = result.TrackedSteps[name].SelectMany(step => step.Outputs).ToArray();
        Assert.Equal(count, outputs.Length);
        Assert.All(outputs, output => Assert.True(
            output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
            $"{name}: expected Cached/Unchanged, got {output.Reason}"));
    }

    [Fact]
    public void ReferencedMemberEditInvalidatesOnlyDependentSemanticModel()
    {
        SyntaxTree model = CSharpSyntaxTree.ParseText(
            "namespace Input { public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; public string Caption { get; set; } = \"old\"; } }",
            path: "Model.cs");
        CSharpCompilation compilation = CreateCompilation(model);
        GeneratorDriver driver = CreateDriver(
            new MarkupText("Bound.crn", "<TextBlock DataType=\"Input.Model\" Text=\"$DataContext.Caption:OneWay\" />"),
            new MarkupText("Independent.crn", "<Button Content=\"Independent\" />"));
        driver = driver.RunGenerators(compilation);
        Assert.Empty(driver.GetRunResult().Results.Single().Diagnostics);

        compilation = compilation.ReplaceSyntaxTree(model, CSharpSyntaxTree.ParseText(
            "namespace Input { public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; public string Renamed { get; set; } = \"new\"; } }",
            path: "Model.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Null(result.Exception);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var outputs = result.TrackedSteps["CernealaLanguageSemanticModel"]
            .SelectMany(step => step.Outputs).ToArray();
        Assert.Single(outputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Single(outputs, output => output.Reason == IncrementalStepRunReason.Cached);
        Assert.Single(result.GeneratedSources);
        Assert.Contains("IndependentFactory", result.GeneratedSources.Single().SourceText.ToString());
    }

    [Fact]
    public void NewMarkupAfterCachedCSharpEditUsesCurrentCompilation()
    {
        SyntaxTree model = CSharpSyntaxTree.ParseText("namespace Input { public class Model { } }", path: "Model.cs");
        CSharpCompilation compilation = CreateCompilation(model);
        GeneratorDriver driver = CreateDriver(new MarkupText("First.crn", "<Button />"));
        driver = driver.RunGenerators(compilation);
        compilation = compilation.ReplaceSyntaxTree(model, CSharpSyntaxTree.ParseText(
            "namespace Input { public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; public string Caption { get; set; } = \"new\"; } }",
            path: "Model.cs"));
        driver = driver.RunGenerators(compilation);
        driver = driver.AddAdditionalTexts([new MarkupText("New.crn",
            "<TextBlock DataType=\"Input.Model\" Text=\"$DataContext.Caption:OneWay\" />")]).RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Null(result.Exception);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedSources.Length);
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains(".Caption"));
    }

    [Theory]
    [InlineData("public bool Flag { get; set; }", "public string Flag { get; set; } = \"\";")]
    [InlineData("public bool Flag { get; set; }", "public bool Flag { private get; set; }")]
    [InlineData("public bool Flag { get; set; }", "public bool Flag { get; private set; }")]
    [InlineData("public bool Flag { get; set; }", "public bool Flag { get; init; }")]
    public void MemberTypeAndAccessorChangesInvalidateBinding(string before, string after)
    {
        const string prefix = "namespace Input { public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; ";
        AssertEditMatchesFreshRun(prefix + before + " } }", prefix + after + " } }", "Model.cs",
            new MarkupText("Bound.crn", "<Button DataType=\"Input.Model\" IsEnabled=\"$DataContext.Flag:TwoWay\" />"),
            beforeHasErrors: false, afterHasErrors: true);
    }

    [Fact]
    public void TransitiveMemberChangesInvalidateBinding()
    {
        const string prefix = "namespace Input { public class Model : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; public Child Value { get; } = new(); } public class Child : System.ComponentModel.INotifyPropertyChanged { public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; ";
        AssertEditMatchesFreshRun(prefix + "public string Caption { get; set; } = \"\"; } }",
            prefix + "public string Renamed { get; set; } = \"\"; } }", "Model.cs",
            new MarkupText("Bound.crn", "<TextBlock DataType=\"Input.Model\" Text=\"$DataContext.Value.Caption:OneWay\" />"),
            beforeHasErrors: false, afterHasErrors: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddingOrRemovingPreviouslyUnresolvedTypeInvalidatesMarkup(bool remove)
    {
        const string defined = "namespace Input { public class Widget : Cerneala.UI.Controls.Button { } }";
        const string missing = "namespace Input { public class Unrelated { } }";
        AssertEditMatchesFreshRun(remove ? defined : missing, remove ? missing : defined, "Widget.cs",
            new MarkupText("Custom.crn", "<local:Widget xmlns:local=\"clr-namespace:Input\" />"),
            beforeHasErrors: !remove, afterHasErrors: remove);
    }

    [Theory]
    [InlineData("public partial class View : Cerneala.UI.Controls.UserControl { }",
        "public class View : Cerneala.UI.Controls.UserControl { }")]
    [InlineData("public partial class View : Cerneala.UI.Controls.UserControl { }",
        "public partial class View : Cerneala.UI.Controls.Button { }")]
    public void CompanionDeclarationChangesInvalidateEmission(string before, string after)
    {
        AssertEditMatchesFreshRun("namespace Input { " + before + " }", "namespace Input { " + after + " }",
            "View.crn.cs", new MarkupText("View.crn", "<UserControl><Button /></UserControl>"),
            beforeHasErrors: false, afterHasErrors: true);
    }

    [Fact]
    public void EventHandlerSignatureChangeInvalidatesEmission()
    {
        const string prefix = "using Cerneala.UI.Controls; using Cerneala.UI.Input; namespace Input { public partial class View : UserControl { ";
        AssertEditMatchesFreshRun(prefix + "private void OnSave(UiElementId sender, RoutedEventArgs args) { } } }",
            prefix + "private void OnSave(string sender, RoutedEventArgs args) { } } }", "View.crn.cs",
            new MarkupText("View.crn", "<UserControl><Button Click=\"OnSave\" /></UserControl>"),
            beforeHasErrors: false, afterHasErrors: true);
    }

    [Fact]
    public void OutputKindChangeInvalidatesStartupEmission()
    {
        CSharpCompilation compilation = CreateCompilation(CSharpSyntaxTree.ParseText(
            "namespace Input { public partial class MainWindow : Cerneala.UI.Controls.Window { } }",
            path: "MainWindow.crn.cs"));
        MarkupText markup = new("MainWindow.crn", "<Window><Button /></Window>");
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        Assert.Empty(driver.GetRunResult().Results.Single().Diagnostics);
        compilation = compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.ConsoleApplication));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI015");
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Fact]
    public void BackendRegistrationSignatureChangeInvalidatesStartupEmission()
    {
        const string prefix = "[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(Input.Backend))] namespace Input { public partial class MainWindow : Cerneala.UI.Controls.Window { } public static class Backend { ";
        AssertEditMatchesFreshRun(prefix + "public static void EnsureRegistered() { } } }",
            prefix + "public static void EnsureRegistered(int value) { } } }", "MainWindow.crn.cs",
            new MarkupText("MainWindow.crn", "<Window><Button /></Window>"),
            beforeHasErrors: false, afterHasErrors: true, OutputKind.ConsoleApplication);
    }

    [Fact]
    public void BackendAttributeChangeUpdatesGeneratedRegistration()
    {
        const string source = "[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(Input.BackendA))] namespace Input { public partial class MainWindow : Cerneala.UI.Controls.Window { } public static class BackendA { public static void EnsureRegistered() { } } public static class BackendB { public static void EnsureRegistered() { } } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "MainWindow.crn.cs");
        CSharpCompilation compilation = CreateCompilation(tree).WithOptions(
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        MarkupText markup = new("MainWindow.crn", "<Window><Button /></Window>");
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        Assert.Contains(driver.GetRunResult().Results.Single().GeneratedSources,
            source => source.SourceText.ToString().Contains("BackendA.EnsureRegistered()"));
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(
            source.Replace("typeof(Input.BackendA)", "typeof(Input.BackendB)"), path: "MainWindow.crn.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("BackendB.EnsureRegistered()"));
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Fact]
    public void AddingEntryPointChangesGeneratedStartupToModuleInitializer()
    {
        const string source = "[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(Input.Backend))] namespace Input { public partial class MainWindow : Cerneala.UI.Controls.Window { } public static class Backend { public static void EnsureRegistered() { } } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "MainWindow.crn.cs");
        CSharpCompilation compilation = CreateCompilation(tree).WithOptions(
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        MarkupText markup = new("MainWindow.crn", "<Window><Button /></Window>");
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        GeneratorRunResult initial = driver.GetRunResult().Results.Single();
        Assert.Empty(initial.Diagnostics);
        Assert.Contains(initial.GeneratedSources, source => source.SourceText.ToString().Contains(" Main()"));
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "public static class Program { public static void Main() { } }", path: "Program.cs"));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("ModuleInitializerAttribute"));
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Fact]
    public void ReferenceChangeInvalidatesMarkup()
    {
        CSharpCompilation compilation = CreateCompilation();
        MarkupText markup = new("Button.crn", "<Button />");
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        Assert.Empty(driver.GetRunResult().Results.Single().Diagnostics);
        compilation = compilation.RemoveReferences(compilation.References.OfType<PortableExecutableReference>()
            .Where(reference => reference.FilePath == typeof(UIElement).Assembly.Location));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    [Fact]
    public void RecursiveGenericMemberGraphIsBounded()
    {
        const string source = "namespace Input { public class Model { public Node<string> Child { get; } = new(); } public class Node<T> { public Node<Node<T>> Next { get; } = null!; } }";
        CSharpCompilation compilation = CreateCompilation(CSharpSyntaxTree.ParseText(source));
        GeneratorDriver driver = CreateDriver(new MarkupText("Bound.crn", "<Button DataType=\"Input.Model\" />"))
            .RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Null(result.Exception);
        Assert.Empty(result.Diagnostics);
        Assert.Single(result.GeneratedSources);
    }

    private static void AssertEditMatchesFreshRun(string before, string after, string path, MarkupText markup,
        bool beforeHasErrors, bool afterHasErrors, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(before, path: path);
        CSharpCompilation compilation = CreateCompilation(tree).WithOptions(new CSharpCompilationOptions(outputKind));
        GeneratorDriver driver = CreateDriver(markup).RunGenerators(compilation);
        GeneratorRunResult initial = driver.GetRunResult().Results.Single();
        Assert.Null(initial.Exception);
        Assert.True(beforeHasErrors == initial.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            string.Join(Environment.NewLine, initial.Diagnostics));
        compilation = compilation.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(after, path: path));
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        Assert.Null(result.Exception);
        Assert.Equal(afterHasErrors, result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        AssertEquivalent(CreateDriver(markup).RunGenerators(compilation).GetRunResult().Results.Single(), result);
    }

    private static void AssertEquivalent(GeneratorRunResult expected, GeneratorRunResult actual)
    {
        Assert.Null(expected.Exception);
        Assert.Null(actual.Exception);
        Assert.Equal(expected.GeneratedSources.Select(source => (source.HintName, source.SourceText.ToString())),
            actual.GeneratedSources.Select(source => (source.HintName, source.SourceText.ToString())));
        Assert.Equal(expected.Diagnostics.Select(diagnostic => diagnostic.ToString()),
            actual.Diagnostics.Select(diagnostic => diagnostic.ToString()));
    }

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] trees) => CSharpCompilation.Create(
        "IncrementalTests", trees,
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static GeneratorDriver CreateDriver(params AdditionalText[] files) => CSharpGeneratorDriver.Create(
        [new UiMarkupGenerator().AsSourceGenerator()], files,
        parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true));

    private sealed class MarkupText(string path, string text) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}
