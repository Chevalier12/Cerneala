namespace Cerneala.Tests.Language;

// docs/plans/2026-10-08-timbre-prism-in-aspect.md: @timbre and @prism are
// written in an Aspect body, a TimbreClip is a set of named @sound nodes and
// sounds are always addressed as $element.timbre.Sound. Frozen in Etapa 0.
public sealed class TimbreAspectSemanticTests
{
    private static readonly Lazy<IReadOnlyList<TimbreCorpusCase>> cases = new(() => TimbreCorpus.Parse("timbre-aspect-corpus.json"));

    public static IEnumerable<object[]> CorpusCases() =>
        cases.Value.Select(item => new object[] { item.Id });

    [Fact]
    public void CorpusCoversEveryFrozenFamily()
    {
        IReadOnlyList<TimbreCorpusCase> all = cases.Value;

        Assert.Equal(all.Count, all.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (string family in new[] { "Attachment", "Command", "Motion", "Reference", "Syntax", "Context" })
        {
            Assert.Contains(all, item => item.Family == family);
        }

        Assert.All(all.Where(item => !item.Valid), item => Assert.NotEmpty(item.Expect));
    }

    // Language and SourceGen agree on every case: identical diagnostics and,
    // for valid cases, generated C# that compiles in an ordinary consumer.
    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void LanguageAndSourceGeneratorAgreeOnCorpus(string id)
    {
        TimbreCorpusCase item = cases.Value.Single(candidate => candidate.Id == id);

        string path = item.Path ?? id + ".crn";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(path, item.Markup);
        IReadOnlyList<HarnessDiagnostic> generator = item.Companion is null
            ? result.SourceGeneratorDiagnostics
            : LanguagePipelineHarness.AnalyzePairedSourceGenerator(path, item.Markup, path + ".cs", item.Companion);

        Assert.Empty(result.Syntax.Diagnostics);
        Assert.Equal(Ordered(result.SemanticDiagnostics), Ordered(generator));
        if (item.Valid)
        {
            Assert.Empty(result.SemanticDiagnostics);
            Assert.Empty(LanguagePipelineHarness.GeneratedCompilationErrors(path, item.Markup, path + ".cs", item.Companion));
            return;
        }

        foreach (TimbreCorpusExpectation expectation in item.Expect)
        {
            Assert.Contains(result.SemanticDiagnostics, diagnostic =>
                diagnostic.Id == expectation.Id &&
                diagnostic.Message.Contains(expectation.Message, StringComparison.Ordinal));
        }
    }

    private static IEnumerable<HarnessDiagnostic> Ordered(IEnumerable<HarnessDiagnostic> diagnostics) => diagnostics
        .OrderBy(diagnostic => diagnostic.StartLine)
        .ThenBy(diagnostic => diagnostic.StartCharacter)
        .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
        .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal);
}
