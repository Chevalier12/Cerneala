using System.Text.Json;

namespace Cerneala.LanguageServer.Logging;

internal static class CrashReporter
{
    public static string? TryWrite(Exception exception, string? crashDirectory)
    {
        if (string.IsNullOrWhiteSpace(crashDirectory))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(crashDirectory);
            string path = Path.Combine(
                crashDirectory,
                $"cerneala-lsp-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfff}-{Environment.ProcessId}.json");
            var report = new
            {
                timestampUtc = DateTimeOffset.UtcNow,
                processId = Environment.ProcessId,
                exceptionType = exception.GetType().FullName,
                stackTrace = exception.StackTrace
            };

            File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return path;
        }
        catch
        {
            return null;
        }
    }
}
