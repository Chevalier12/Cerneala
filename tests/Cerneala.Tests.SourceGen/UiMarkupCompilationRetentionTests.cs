using System.Runtime.CompilerServices;
using Cerneala.SourceGen;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.Tests.SourceGen;

// An IDE keeps one generator driver for the whole session. Cached pipeline
// values must not keep the Compilation that an unrelated C# edit replaced.
public sealed class UiMarkupCompilationRetentionTests
{
    private const string ModelSource =
        "public class Model : System.ComponentModel.INotifyPropertyChanged { " +
        "public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged; " +
        "public string Caption { get; set; } = \"\"; }";

    [Fact]
    public void DriverWithoutGeneratorReleasesReplacedCompilation()
        => AssertReplacedCompilationIsCollected([], keepDriver: true);

    [Fact]
    public void UiMarkupGeneratorReleasesReplacedCompilationAfterUnrelatedEdit()
        => AssertReplacedCompilationIsCollected([new UiMarkupGenerator().AsSourceGenerator()], keepDriver: true);

    [Fact]
    public void CurrentCompilationAloneDoesNotKeepReplacedCompilation()
        => AssertReplacedCompilationIsCollected([new UiMarkupGenerator().AsSourceGenerator()], keepDriver: false);

    private static void AssertReplacedCompilationIsCollected(ISourceGenerator[] generators, bool keepDriver)
    {
        (GeneratorDriver? driver, Compilation current, WeakReference replaced) = RunUnrelatedEdit(generators, keepDriver);
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(replaced.IsAlive, "The Compilation replaced by an unrelated C# edit is still reachable.");
        GC.KeepAlive(driver);
        GC.KeepAlive(current);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (GeneratorDriver? Driver, Compilation Current, WeakReference Replaced) RunUnrelatedEdit(
        ISourceGenerator[] generators, bool keepDriver)
    {
        SyntaxTree unrelated = CSharpSyntaxTree.ParseText(
            "namespace Unrelated { public class Other { public int Value => 1; } }", path: "Other.cs");
        CSharpCompilation first = CreateCompilation(CSharpSyntaxTree.ParseText(ModelSource, path: "Model.cs"), unrelated);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators,
            [new MarkupText("Bound.crn", "<TextBlock DataType=\"Model\" Text=\"$DataContext.Caption:OneWay\" />")],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: false));
        driver = driver.RunGenerators(first);

        CSharpCompilation second = first.ReplaceSyntaxTree(unrelated, CSharpSyntaxTree.ParseText(
            "namespace Unrelated { public class Other { public int Value => 2; } }", path: "Other.cs"));
        driver = driver.RunGenerators(second);

        foreach (GeneratorRunResult result in driver.GetRunResult().Results)
        {
            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.NotEmpty(result.GeneratedSources);
        }

        return (keepDriver ? driver : null, second, new WeakReference(first));
    }

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] trees) => CSharpCompilation.Create(
        "RetentionTests", trees,
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location)),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private sealed class MarkupText(string path, string text) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}
