using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;
using LanguageTextSpan = Cerneala.Language.Text.TextSpan;

namespace Cerneala.Benchmarks;

public enum CernealaLanguageDocumentSize
{
    Small,
    Medium,
    Large
}

[MemoryDiagnoser]
public sealed class CernealaLanguageBenchmarks
{
    private LanguageBenchmarkFixture fixture = null!;

    [ParamsAllValues]
    public CernealaLanguageDocumentSize DocumentSize { get; set; }

    [GlobalSetup]
    public void Setup() => fixture = LanguageBenchmarkFixture.Create(DocumentSize);

    [GlobalCleanup]
    public void Cleanup() => fixture.Dispose();

    [Benchmark]
    public int ParseCold() => fixture.ParseCold();

    [Benchmark]
    public int ParseWarm() => fixture.ParseWarm();

    [Benchmark]
    public int ApplyIncrementalEdit() => fixture.ApplyIncrementalEdit();

    [Benchmark]
    public int BindSemanticModel() => fixture.BindSemanticModel();

    [Benchmark]
    public int QuerySymbolWarm() => fixture.QuerySymbolWarm();
}

internal static class CernealaLanguageBenchmarkGate
{
    private const int WarmupIterations = 8;
    private const int SampleIterations = 40;

    public static void Run()
    {
        Console.WriteLine("Cerneala language core performance gate");
        Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Logical processors: {Environment.ProcessorCount}; GC: {(GCSettings.IsServerGC ? "server" : "workstation")}");
        Console.WriteLine();

        foreach (CernealaLanguageDocumentSize size in Enum.GetValues<CernealaLanguageDocumentSize>())
        {
            using LanguageBenchmarkFixture fixture = LanguageBenchmarkFixture.Create(size);
            Console.WriteLine($"[{size}] {fixture.CharacterCount:N0} UTF-16 characters");
            Measurement cold = Measure(fixture.ParseCold);
            Measurement warm = Measure(fixture.ParseWarm);
            Measurement edit = Measure(fixture.ApplyIncrementalEdit);
            Measurement bind = Measure(fixture.BindSemanticModel);
            Measurement query = Measure(fixture.QuerySymbolWarm);
            Print("parse cold", cold);
            Print("parse warm", warm);
            Print("incremental edit", edit);
            Print("semantic bind", bind);
            Print("warm query", query);

            Require(cold.MaxMilliseconds < 100, size, "parse cold max", cold.MaxMilliseconds, 100);
            Require(warm.MaxMilliseconds < 100, size, "parse warm max", warm.MaxMilliseconds, 100);
            Require(edit.MaxMilliseconds < 100, size, "incremental edit max", edit.MaxMilliseconds, 100);
            Require(bind.MaxMilliseconds < 100, size, "semantic bind max", bind.MaxMilliseconds, 100);
            Require(query.MaxMilliseconds < 100, size, "warm query max", query.MaxMilliseconds, 100);

            if (size == CernealaLanguageDocumentSize.Large)
            {
                Require(cold.P95Milliseconds < 50, size, "parse cold p95", cold.P95Milliseconds, 50);
                Require(warm.P95Milliseconds < 50, size, "parse warm p95", warm.P95Milliseconds, 50);
                Require(edit.P95Milliseconds < 50, size, "incremental edit p95", edit.P95Milliseconds, 50);
                Require(query.P95Milliseconds < 25, size, "warm query p95", query.P95Milliseconds, 25);
            }

            Console.WriteLine();
        }
    }

    private static Measurement Measure(Func<int> operation)
    {
        int checksum = 0;
        for (int i = 0; i < WarmupIterations; i++)
        {
            checksum ^= operation();
        }

        double[] milliseconds = new double[SampleIterations];
        long allocated = 0;
        for (int i = 0; i < SampleIterations; i++)
        {
            long beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            checksum ^= operation();
            long end = Stopwatch.GetTimestamp();
            allocated += GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            milliseconds[i] = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
        }

        GC.KeepAlive(checksum);
        Array.Sort(milliseconds);
        int p95Index = (int)Math.Ceiling(milliseconds.Length * 0.95) - 1;
        return new Measurement(
            milliseconds[p95Index],
            milliseconds[milliseconds.Length - 1],
            allocated / SampleIterations);
    }

    private static void Print(string name, Measurement measurement) =>
        Console.WriteLine($"  {name,-18} p95 {measurement.P95Milliseconds,8:F3} ms | max {measurement.MaxMilliseconds,8:F3} ms | {measurement.AllocatedBytes,10:N0} B/op");

    private static void Require(
        bool condition,
        CernealaLanguageDocumentSize size,
        string metric,
        double displayedActual,
        double limit)
    {
        if (condition)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{size} {metric} was {displayedActual:F3} ms; the budget is below {limit:F0} ms.");
    }

    private readonly record struct Measurement(
        double P95Milliseconds,
        double MaxMilliseconds,
        long AllocatedBytes);
}

internal sealed class LanguageBenchmarkFixture : IDisposable
{
    private readonly string path;
    private readonly string markup;
    private readonly LanguageSourceText source;
    private readonly CernealaDocument document;
    private readonly TextChange edit;
    private readonly CSharpCompilation compilation;
    private readonly CernealaCompilation warmCompilation;
    private readonly CernealaSemanticModel warmModel;
    private readonly int queryOffset;

    private LanguageBenchmarkFixture(string path, string markup)
    {
        this.path = path;
        this.markup = markup;
        source = LanguageSourceText.From(markup);
        document = new CernealaDocument(path, source);
        int editOffset = FindEditOffset(markup);
        edit = new TextChange(new LanguageTextSpan(editOffset, 1), markup[editOffset] == 'X' ? "Y" : "X");
        compilation = CreateCompilation();
        warmCompilation = new CernealaCompilation(
            new RoslynCompilationSymbols(compilation),
            [document],
            AnalysisMode.Build);
        warmModel = warmCompilation.GetSemanticModel(path);
        queryOffset = FindQueryOffset(markup);
    }

    public int CharacterCount => markup.Length;

    public static LanguageBenchmarkFixture Create(CernealaLanguageDocumentSize size)
    {
        string repository = FindRepositoryRoot();
        return size switch
        {
            CernealaLanguageDocumentSize.Small => new LanguageBenchmarkFixture(
                "SmallBenchmark.cui.xml",
                "<StackPanel><TextBlock Text=\"Hello\" /><Button Content=\"Run\" /></StackPanel>"),
            CernealaLanguageDocumentSize.Medium => FromRepositoryFile(
                repository,
                "CernealaPresentation",
                "MarkupChapterView.cui.xml"),
            CernealaLanguageDocumentSize.Large => FromRepositoryFile(
                repository,
                "CernealaPresentation",
                "AspectChapterView.cui.xml"),
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
    }

    public int ParseCold()
    {
        DocumentSyntax syntax = MarkupParser.Parse(LanguageSourceText.From(markup));
        return syntax.Children.Count + syntax.Diagnostics.Count;
    }

    public int ParseWarm()
    {
        DocumentSyntax syntax = MarkupParser.Parse(source);
        return syntax.Children.Count + syntax.Diagnostics.Count;
    }

    public int ApplyIncrementalEdit()
    {
        CernealaDocument changed = document.WithChange(edit);
        return changed.Syntax.Children.Count + changed.Syntax.Diagnostics.Count;
    }

    public int BindSemanticModel()
    {
        using CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(compilation),
            [document],
            AnalysisMode.Build);
        CernealaSemanticModel model = workspace.GetSemanticModel(path);
        return model.Symbols.Count + model.Diagnostics.Count;
    }

    public int QuerySymbolWarm()
    {
        CernealaSemanticSymbol? symbol = warmModel.GetSymbolAt(queryOffset);
        return symbol?.Name.Length ?? 0;
    }

    public void Dispose() => warmCompilation.Dispose();

    private static LanguageBenchmarkFixture FromRepositoryFile(string repository, params string[] segments)
    {
        string path = Path.Combine([repository, .. segments]);
        return new LanguageBenchmarkFixture(path, File.ReadAllText(path));
    }

    private static int FindEditOffset(string text)
    {
        int offset = text.IndexOf("Text=", StringComparison.Ordinal);
        if (offset < 0)
        {
            offset = text.IndexOf("Name=", StringComparison.Ordinal);
        }

        return offset < 0 ? Math.Max(0, text.Length / 2) : offset;
    }

    private static int FindQueryOffset(string text)
    {
        int offset = text.IndexOf("TextBlock", StringComparison.Ordinal);
        return offset < 0 ? Math.Max(0, text.Length / 2) : offset + 1;
    }

    private static CSharpCompilation CreateCompilation() => CSharpCompilation.Create(
        "CernealaLanguageBenchmarks",
        [CSharpSyntaxTree.ParseText("namespace BenchmarkInput { public static class Anchor { } }")],
        References(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
