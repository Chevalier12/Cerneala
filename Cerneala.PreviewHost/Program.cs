namespace Cerneala.PreviewHost;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 3 && string.Equals(args[0], "--smoke", StringComparison.OrdinalIgnoreCase))
            {
                return RunSmoke(args[1], args[2]);
            }

            using PreviewHostServer server = new();
            server.Run(Console.OpenStandardInput(), Console.OpenStandardOutput());
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int RunSmoke(string documentPath, string screenshotPath)
    {
        using PreviewCompiler compiler = new();
        PreviewCompilation compilation = compiler
            .CompileAsync(documentPath, File.ReadAllText(documentPath))
            .GetAwaiter()
            .GetResult();
        using PreviewRenderSession session = PreviewRenderSession.Create(compilation, 1200, 800);
        session.SaveScreenshot(screenshotPath);
        Console.Error.WriteLine(
            $"Previewed {compilation.TargetTypeName} in {compilation.CompileTime.TotalMilliseconds:F0} ms: {Path.GetFullPath(screenshotPath)}");
        return 0;
    }
}
