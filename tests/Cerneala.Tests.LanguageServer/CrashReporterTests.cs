using Cerneala.LanguageServer.Logging;

namespace Cerneala.Tests.LanguageServer;

public sealed class CrashReporterTests
{
    [Fact]
    public void CrashReportsAreOptInAndExcludeExceptionMessages()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"cerneala-lsp-crash-{Guid.NewGuid():N}");
        try
        {
            InvalidOperationException exception = new("private-document-secret");

            Assert.Null(CrashReporter.TryWrite(exception, null));
            string path = Assert.IsType<string>(CrashReporter.TryWrite(exception, directory));
            string report = File.ReadAllText(path);

            Assert.Contains(typeof(InvalidOperationException).FullName!, report, StringComparison.Ordinal);
            Assert.DoesNotContain("private-document-secret", report, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
