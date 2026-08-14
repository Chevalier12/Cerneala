using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Protocol;

namespace Cerneala.LanguageServer;

internal static class Program
{
    public static async Task<int> Main()
    {
        StructuredServerLogger logger = StructuredServerLogger.CreateForStandardError();

        try
        {
            return await LanguageServerHost.RunAsync(
                Console.OpenStandardInput(),
                Console.OpenStandardOutput(),
                logger,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.Critical("server.crashed", ("exceptionType", exception.GetType().FullName));
            CrashReporter.TryWrite(exception, Environment.GetEnvironmentVariable("CERNEALA_LSP_CRASH_DIRECTORY"));
            return 1;
        }
    }
}
