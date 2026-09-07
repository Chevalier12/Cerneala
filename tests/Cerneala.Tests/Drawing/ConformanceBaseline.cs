namespace Cerneala.Tests.Drawing;

internal static class ConformanceBaseline
{
    public static string Resolve(string fileName)
    {
        string directory = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(directory, "Cerneala.slnx")))
        {
            directory = Directory.GetParent(directory)?.FullName
                ?? throw new InvalidOperationException("The conformance repository root was not found.");
        }

        string path = Path.Combine(
            directory, "tests", "Baselines", "Conformance", "RetiredWindowsDx", fileName);
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException("The historical conformance reference is missing.", path);
    }
}
