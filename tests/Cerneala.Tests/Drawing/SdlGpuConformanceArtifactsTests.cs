namespace Cerneala.Tests.Drawing;

[Collection(SdlGpuConformanceArtifactsTestCollection.Name)]
public sealed class SdlGpuConformanceArtifactsTests : IDisposable
{
    private const string ArtifactDirectoryEnvironmentVariable =
        "CERNEALA_SDL_CONFORMANCE_ARTIFACTS";

    private readonly string? originalArtifactDirectory =
        Environment.GetEnvironmentVariable(ArtifactDirectoryEnvironmentVariable);
    private readonly string sourceDirectory = Path.Combine(
        Path.GetTempPath(),
        $"cerneala-conformance-source-{Guid.NewGuid():N}");
    private readonly string configuredRoot = Path.Combine(
        Path.GetTempPath(),
        $"cerneala-conformance-artifacts-{Guid.NewGuid():N}");

    [Fact]
    public void PreserveRequestedCopiesArtifactsToConfiguredRoot()
    {
        string source = CreateSourceArtifact();
        Environment.SetEnvironmentVariable(
            ArtifactDirectoryEnvironmentVariable,
            configuredRoot);

        SdlGpuConformanceArtifacts.PreserveRequested(sourceDirectory);

        string copied = Path.Combine(
            configuredRoot,
            Path.GetFileName(sourceDirectory),
            Path.GetFileName(source));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(copied));
    }

    [Fact]
    public void PreserveFailureCopiesArtifactsToLegacyAndConfiguredRoots()
    {
        string source = CreateSourceArtifact();
        Environment.SetEnvironmentVariable(
            ArtifactDirectoryEnvironmentVariable,
            configuredRoot);

        SdlGpuConformanceArtifacts.PreserveFailure(sourceDirectory);

        string relativeArtifact = Path.Combine(
            Path.GetFileName(sourceDirectory),
            Path.GetFileName(source));
        string legacyCopy = Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            relativeArtifact);
        string configuredCopy = Path.Combine(configuredRoot, relativeArtifact);
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(legacyCopy));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(configuredCopy));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            ArtifactDirectoryEnvironmentVariable,
            originalArtifactDirectory);
        DeleteDirectory(sourceDirectory);
        DeleteDirectory(configuredRoot);
        DeleteDirectory(Path.Combine(
            AppContext.BaseDirectory,
            "TestResults",
            Path.GetFileName(sourceDirectory)));
    }

    private string CreateSourceArtifact()
    {
        Directory.CreateDirectory(sourceDirectory);
        string source = Path.Combine(sourceDirectory, "pixel-diff.txt");
        File.WriteAllText(source, "conformance evidence");
        return source;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SdlGpuConformanceArtifactsTestCollection
{
    public const string Name = "SDL GPU conformance artifacts";
}
