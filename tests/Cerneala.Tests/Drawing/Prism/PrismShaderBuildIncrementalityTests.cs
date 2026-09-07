using System.Xml.Linq;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismShaderBuildIncrementalityTests
{
    [Fact]
    public void ShaderVerificationTracksSharedStylesWrappersAndCompilerInputs()
    {
        XDocument project = XDocument.Load(Path.Combine(FindRepositoryRoot(),
            "Cerneala.Backends.SdlGpu", "Cerneala.Backends.SdlGpu.csproj"));
        XElement target = Assert.Single(project.Descendants("Target").Where(element =>
            (string?)element.Attribute("Name") == "VerifySdlShaderArtifacts"));
        Assert.Equal("@(SdlShaderInput)", (string?)target.Attribute("Inputs"));
        Assert.Equal("$(SdlShaderVerifyStamp)", (string?)target.Attribute("Outputs"));
        Assert.Equal("PrepareResources", (string?)target.Attribute("BeforeTargets"));

        string[] inputs = project.Descendants("SdlShaderInput")
            .Select(element => ((string?)element.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
            .ToArray();
        // SDL has one catalog package, not the retired adapter's separate MGFX
        // style package. Recursive shared-HLSL inputs still include every style.
        Assert.Contains("$(RepositoryRoot)/Drawing/Prism/Shaders/Hlsl/**/*.hlsl", inputs);
        Assert.Contains("$(MSBuildProjectDirectory)/Prism/Shaders/*.hlsl", inputs);
        Assert.Contains("$(MSBuildProjectDirectory)/Gpu/Shaders/*.hlsl", inputs);
        Assert.Contains("$(SdlShaderManifest)", inputs);
        Assert.Contains("$(SdlShaderCompilerProject)", inputs);
        Assert.Contains("$(RepositoryRoot)/Tools/Cerneala.SdlShaderCompiler/Program.cs", inputs);
        string command = (string?)Assert.Single(target.Elements("Exec")).Attribute("Command") ?? string.Empty;
        Assert.Contains("--verify --manifest", command, StringComparison.Ordinal);
        Assert.Equal("$(SdlShaderVerifyStamp)", (string?)Assert.Single(target.Elements("Touch")).Attribute("Files"));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) { return directory.FullName; }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
