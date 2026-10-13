using System.Xml.Linq;

namespace Cerneala.Tests.VisualStudio;

public sealed class VisualStudioBuildGraphTests
{
    [Fact]
    public void ExtensionBuildTargetBuildsOnlyProjectsTheSolutionRestores()
    {
        string repositoryRoot = VisualStudioPackageTests.RepositoryRoot();
        string testProjectDirectory = Path.Combine(repositoryRoot, "tests", "Cerneala.Tests.VisualStudio");
        XDocument testProject = XDocument.Load(Path.Combine(testProjectDirectory, "Cerneala.Tests.VisualStudio.csproj"));
        XElement target = Assert.Single(testProject.Descendants("Target").Where(element =>
            (string?)element.Attribute("Name") == "BuildVisualStudioExtension"));
        string[] builtProjects = target.Elements("MSBuild")
            .SelectMany(task => ((string?)task.Attribute("Projects") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(path => Path.GetFullPath(Path.Combine(testProjectDirectory, path)))
            .ToArray();
        Assert.NotEmpty(builtProjects);

        // NuGet restores the solution's projects and their ProjectReference graph,
        // not projects reached through an <MSBuild> task.
        HashSet<string> solutionProjects = XDocument.Load(Path.Combine(repositoryRoot, "Cerneala.slnx"))
            .Descendants("Project")
            .Select(project => Path.GetFullPath(Path.Combine(repositoryRoot, (string?)project.Attribute("Path") ?? string.Empty)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string builtProject in builtProjects)
        {
            Assert.True(
                solutionProjects.Contains(builtProject),
                $"'{Path.GetRelativePath(repositoryRoot, builtProject)}' is built by BuildVisualStudioExtension but is not in Cerneala.slnx, so 'dotnet restore Cerneala.slnx' does not restore it.");
        }
    }

    [Fact]
    public void BundledPublishesBuildTheirProjectGraphInTheirOwnArtifactsTree()
    {
        string projectDirectory = Path.Combine(VisualStudioPackageTests.RepositoryRoot(), "Cerneala.VisualStudio");
        XDocument project = XDocument.Load(Path.Combine(projectDirectory, "Cerneala.VisualStudio.csproj"));
        XElement[] publishes = project.Descendants("MSBuild")
            .Where(task => ((string?)task.Attribute("Targets") ?? string.Empty).Contains("Publish", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, publishes.Length);

        // Global properties flow to every referenced project. Without an own artifacts
        // tree, the publish builds Cerneala.csproj, SdlGpu and the rest a second time
        // into the solution's obj folders, concurrently with the solution build.
        List<string> artifactsPaths = [];
        foreach (XElement publish in publishes)
        {
            Dictionary<string, string> properties = ((string?)publish.Attribute("Properties") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(property => property.Split('=', 2))
                .ToDictionary(pair => pair[0], pair => pair.Length == 2 ? pair[1] : string.Empty, StringComparer.OrdinalIgnoreCase);
            string projects = (string?)publish.Attribute("Projects") ?? string.Empty;

            Assert.True(
                properties.TryGetValue("UseArtifactsOutput", out string? useArtifactsOutput) && useArtifactsOutput == "true",
                $"The publish of '{projects}' does not set UseArtifactsOutput=true.");
            Assert.True(
                properties.TryGetValue("ArtifactsPath", out string? artifactsPath),
                $"The publish of '{projects}' does not set ArtifactsPath.");
            Assert.StartsWith("$(", artifactsPath, StringComparison.Ordinal);
            string artifactsProperty = artifactsPath[2..^1];
            string definition = Assert.Single(project.Descendants(artifactsProperty)).Value;
            Assert.StartsWith(@"$(MSBuildProjectDirectory)\obj\", definition, StringComparison.Ordinal);
            artifactsPaths.Add(definition);
        }

        Assert.Equal(artifactsPaths.Count, artifactsPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
