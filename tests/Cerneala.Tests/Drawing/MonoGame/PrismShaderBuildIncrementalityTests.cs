using System.Xml.Linq;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismShaderBuildIncrementalityTests
{
    [Fact]
    public void StyleShadersCompileInAnIndependentIncrementalPackage()
    {
        string repositoryRoot = FindRepositoryRoot();
        string shaderRoot = Path.Combine(
            repositoryRoot,
            "Drawing",
            "MonoGame",
            "Prism",
            "Shaders");
        string copyComposite = File.ReadAllText(
            Path.Combine(shaderRoot, "CopyComposite.fx"));
        string stylesPackage = File.ReadAllText(
            Path.Combine(shaderRoot, "Styles.fx"));
        string sharedStyleRoot = Path.Combine(
            repositoryRoot,
            "Drawing",
            "Prism",
            "Shaders",
            "Hlsl",
            "Styles");

        Assert.DoesNotContain(
            "#include \"Styles/",
            copyComposite,
            StringComparison.Ordinal);

        foreach (string stylePath in Directory.EnumerateFiles(
            sharedStyleRoot,
            "*.hlsl",
            SearchOption.TopDirectoryOnly))
        {
            Assert.Contains(
                $"#include \"../../../Prism/Shaders/Hlsl/Styles/{Path.GetFileName(stylePath)}\"",
                stylesPackage,
                StringComparison.Ordinal);
        }

        XDocument project = XDocument.Load(
            Path.Combine(
                repositoryRoot,
                "Cerneala.Backends.MonoGame",
                "Cerneala.Backends.MonoGame.csproj"));
        XElement compileTarget = Assert.Single(
            project.Descendants("Target").Where(target =>
                (string?)target.Attribute("Name") == "CompilePrismShaders"));
        string inputs = Assert.IsType<string>(
            (string?)compileTarget.Attribute("Inputs"));
        XElement styleInclude = Assert.Single(project.Descendants("PrismStyleInclude"));
        string styleDependencies = Assert.IsType<string>(
            (string?)styleInclude.Attribute("Include"));

        Assert.Contains(
            "%(PrismShaderSource.Dependencies)",
            inputs,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "@(PrismShaderInclude)",
            inputs,
            StringComparison.Ordinal);
        Assert.Contains(
            @"Drawing\Prism\Shaders\Hlsl\Styles\*.hlsl",
            styleDependencies,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Cerneala repository root.");
    }
}
