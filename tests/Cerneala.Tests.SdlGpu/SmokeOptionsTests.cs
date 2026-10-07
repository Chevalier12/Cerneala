using System.Text.RegularExpressions;
using Cerneala.SdlGpuSmoke;

namespace Cerneala.Tests.SdlGpu;

// SmokeOptions.cs is compiled into this project from the smoke application.
[Collection(SmokeOptionsCollection.Name)]
public sealed class SmokeOptionsTests
{
    [Fact]
    public void TheTimbreModeParsesWithArtifactsAndWithoutScreenshots()
    {
        string relative = Path.Combine("artifacts", "timbre", "native");

        SmokeOptions.Initialize(["--mode", "Timbre", "--artifacts", relative, "--no-screenshot"]);

        Assert.Equal("timbre", SmokeOptions.Current.Mode);
        Assert.Equal(Path.GetFullPath(relative), SmokeOptions.Current.ArtifactDirectory);
        Assert.False(SmokeOptions.Current.CaptureScreenshots);
        Assert.False(SmokeOptions.Current.RequireInput);
    }

    [Fact]
    public void TheTimbreMarkupModeParses()
    {
        SmokeOptions.Initialize(["--mode", "timbre-markup", "--no-screenshot"]);

        Assert.Equal("timbre-markup", SmokeOptions.Current.Mode);
        Assert.False(SmokeOptions.Current.CaptureScreenshots);
    }

    [Fact]
    public void AnUnknownModeIsRejectedAndListsTimbre()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SmokeOptions.Initialize(["--mode", "sound"]));

        Assert.Contains("Unknown SDL_GPU smoke mode 'sound'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("timbre", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--mode")]
    [InlineData("--artifacts")]
    [InlineData("--sound")]
    public void MissingValuesAndUnknownArgumentsAreRejected(string argument)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SmokeOptions.Initialize(["--mode", "timbre", argument]));

        Assert.Contains($"Unknown or incomplete SDL_GPU smoke argument '{argument}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLauncherAcceptsTheTimbreMode()
    {
        string script = File.ReadAllText(Path.Combine(RepositoryRoot(), "Tools", "scripts", "Invoke-SdlGpuSmoke.ps1"));
        Match modes = Regex.Match(script, @"\[ValidateSet\(([^\]]*)\)\]\s*\[string\]\s*\$Mode");

        Assert.True(modes.Success);
        Assert.Contains("'timbre'", modes.Groups[1].Value, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("The repository root was not found.");
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SmokeOptionsCollection
{
    public const string Name = "SDL smoke options";
}
