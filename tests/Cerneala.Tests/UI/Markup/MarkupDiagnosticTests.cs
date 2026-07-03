using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Markup;

public sealed class MarkupDiagnosticTests
{
    [Fact]
    public void DiagnosticReportsSourceLocationWhenPresent()
    {
        MarkupDiagnostic diagnostic = MarkupDiagnostic.Error("X", "Nope", 3, 9);

        Assert.True(diagnostic.HasSourceLocation);
        Assert.Equal(MarkupDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(3, diagnostic.Line);
        Assert.Equal(9, diagnostic.Column);
    }

    [Fact]
    public void ResultHasErrorsOnlyForErrorSeverity()
    {
        MarkupResult<object> warningOnly = new(new object(), [MarkupDiagnostic.Warning("W", "Careful")]);
        MarkupResult<object> error = new(new object(), [MarkupDiagnostic.Error("E", "Broken")]);

        Assert.False(warningOnly.HasErrors);
        Assert.True(error.HasErrors);
    }
}
