using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Theory]
    [InlineData("2.5", "2.5f")]
    [InlineData(" 1e2 ", "100f")]
    [InlineData("0", "0f")]
    public void ScalarAndPointFloatLiteralsUseTheSameInvariantFormatting(string value, string expected)
    {
        string markup = $"<Line Width=\"{value}\" StartPoint=\"{value},{value}\" />";
        GeneratorRunResult result = RunGenerator("FloatLiterals.crn", markup, out Compilation compilation);

        Assert.Empty(result.Diagnostics);
        string source = SingleGeneratedSource(result);
        Assert.Contains(".Width = " + expected + ";", source);
        Assert.Contains(".StartPoint = new global::Cerneala.Drawing.DrawPoint(" + expected + ", " + expected + ");", source);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e100")]
    public void NonFiniteScalarFloatLiteralsReportValueDiagnosticsWithoutSource(string value)
    {
        GeneratorRunResult result = RunGenerator("InvalidFloat.crn", $"<Line Width=\"{value}\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI004", "InvalidFloat.crn");
        Assert.Empty(result.GeneratedSources);
    }
}
