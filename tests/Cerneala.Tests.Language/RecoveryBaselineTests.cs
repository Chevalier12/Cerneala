namespace Cerneala.Tests.Language;

public sealed class RecoveryBaselineTests
{
    public static IEnumerable<object[]> RecoveryCases()
    {
        return CorpusCatalog.Load()
            .Where(item => item.Recovery)
            .Select(item => new object[] { item });
    }

    [Theory]
    [MemberData(nameof(RecoveryCases))]
    public void IncompleteDocumentsRequireTolerantRecovery(CorpusCase item)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(item.Id + ".cui.xml", item.Invalid);

        Assert.True(
            result.Syntax.Succeeded,
            item.Id + " was not recovered by the tolerant parser.");
        Assert.True(result.Syntax.Diagnostics.Count <= 1, item.Id + " produced cascading syntax diagnostics.");
        if (item.FollowingSibling is not null)
        {
            Assert.Contains(item.FollowingSibling, result.Syntax.ElementNames);
        }
    }

    [Theory]
    [MemberData(nameof(RecoveryCases))]
    public void UnrecoverableSyntaxSuppressesSemanticCascades(CorpusCase item)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(item.Id + ".cui.xml", item.Invalid);

        if (result.Syntax.Diagnostics.Count > 0)
        {
            Assert.Empty(result.SemanticDiagnostics);
        }

        Assert.True(result.Syntax.Diagnostics.Count <= 1);
    }
}
