namespace Cerneala.Tests.Drawing;

internal static class SdlGpuConformanceArtifacts
{
    private const string ArtifactDirectoryEnvironmentVariable =
        "CERNEALA_SDL_CONFORMANCE_ARTIFACTS";

    public static void PreserveRequested(string sourceDirectory)
    {
        string? configuredRoot = Environment.GetEnvironmentVariable(
            ArtifactDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return;
        }

        CopyDirectory(
            sourceDirectory,
            Path.Combine(Path.GetFullPath(configuredRoot), Path.GetFileName(sourceDirectory)));
    }

    public static void PreserveFailure(string sourceDirectory)
    {
        CopyDirectory(
            sourceDirectory,
            Path.Combine(
                AppContext.BaseDirectory,
                "TestResults",
                Path.GetFileName(sourceDirectory)));
        PreserveRequested(sourceDirectory);
    }

    private static void CopyDirectory(string sourceDirectory, string artifactDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(artifactDirectory);
        foreach (string source in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(
                source,
                Path.Combine(artifactDirectory, Path.GetFileName(source)),
                overwrite: true);
        }
    }
}
