namespace Cerneala.Tests.PreviewHost
{
    using Cerneala.PreviewHost;

    [Collection(PreviewHostCollection.Name)]
    public sealed class PreviewCompilerFastPathTargetTypeTests
    {
        [Fact]
        public async Task CurrentBuildOutputUsesTheCompanionTypesQualifiedName()
        {
            Type first = typeof(FastPathFixture.View);
            Type second = typeof(CompetingFixture.View);
            Type target = first.MetadataToken > second.MetadataToken ? first : second;
            Type competing = ReferenceEquals(target, first) ? second : first;
            Assert.Equal("View", target.Name);
            Assert.Equal("View", competing.Name);
            Assert.NotEqual(target.Namespace, competing.Namespace);

            string fixtureRoot = Path.Combine(
                Path.GetTempPath(),
                "Cerneala",
                nameof(PreviewCompilerFastPathTargetTypeTests));
            string root = Path.GetFullPath(Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N")));
            string outputDirectory = Path.Combine(root, "bin", "Debug", "net10.0");
            try
            {
                Directory.CreateDirectory(outputDirectory);
                string projectPath = Path.Combine(root, "Sample.csproj");
                string documentPath = Path.Combine(root, "View.crn");
                await File.WriteAllTextAsync(projectPath, """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <AssemblyName>Sample</AssemblyName>
                      </PropertyGroup>
                      <ItemGroup>
                        <AdditionalFiles Include="*.crn" />
                      </ItemGroup>
                    </Project>
                    """);
                await File.WriteAllTextAsync(documentPath, "<UserControl />");
                await File.WriteAllTextAsync(
                    documentPath + ".cs",
                    $"namespace {target.Namespace}; internal partial class View {{ }}");

                string builtAssembly = Path.Combine(outputDirectory, "Sample.dll");
                File.Copy(typeof(FastPathFixture.View).Assembly.Location, builtAssembly);
                File.SetLastWriteTimeUtc(builtAssembly, DateTime.UtcNow.AddMinutes(1));

                using PreviewCompiler compiler = new(prewarmBuildOutput: false);
                string saved = await File.ReadAllTextAsync(documentPath);
                PreviewCompilation dynamic = await compiler.CompileAsync(documentPath, saved + Environment.NewLine);
                PreviewCompilation built = await compiler.CompileAsync(documentPath, saved);

                string expected = target.FullName!;
                Assert.Equal(expected, dynamic.TargetTypeName);
                Assert.Equal(await File.ReadAllBytesAsync(builtAssembly), built.AssemblyImage);
                Assert.Equal(dynamic.TargetTypeName, built.TargetTypeName);
                Assert.NotEqual(competing.FullName, built.TargetTypeName);
            }
            finally
            {
                string safeRoot = Path.GetFullPath(fixtureRoot).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (!root.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The PreviewCompiler test root escaped its dedicated temporary directory.");
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public async Task ConditionalCompanionFallsBackToProjectSemanticResolution()
        {
            string fixtureRoot = Path.Combine(
                Path.GetTempPath(),
                "Cerneala",
                nameof(PreviewCompilerFastPathTargetTypeTests));
            string root = Path.GetFullPath(Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N")));
            string outputDirectory = Path.Combine(root, "bin", "Debug", "net10.0");
            try
            {
                Directory.CreateDirectory(outputDirectory);
                string projectPath = Path.Combine(root, "Sample.csproj");
                string documentPath = Path.Combine(root, "View.crn");
                await File.WriteAllTextAsync(projectPath, """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <AssemblyName>Sample</AssemblyName>
                        <DefineConstants>PREVIEW_TARGET</DefineConstants>
                      </PropertyGroup>
                      <ItemGroup>
                        <AdditionalFiles Include="*.crn" />
                      </ItemGroup>
                    </Project>
                    """);
                await File.WriteAllTextAsync(documentPath, "<UserControl />");
                await File.WriteAllTextAsync(documentPath + ".cs", """
                    #if PREVIEW_TARGET
                    namespace Cerneala.Tests.PreviewHost.ConditionalTarget;
                    internal partial class View { }
                    #else
                    namespace Cerneala.Tests.PreviewHost.WrongConditionalTarget;
                    internal partial class View { }
                    #endif
                    """);

                string builtAssembly = Path.Combine(outputDirectory, "Sample.dll");
                File.Copy(typeof(FastPathFixture.View).Assembly.Location, builtAssembly);
                File.SetLastWriteTimeUtc(builtAssembly, DateTime.UtcNow.AddMinutes(1));

                using PreviewCompiler compiler = new(prewarmBuildOutput: false);
                string saved = await File.ReadAllTextAsync(documentPath);
                PreviewCompilation dynamic = await compiler.CompileAsync(
                    documentPath,
                    saved + Environment.NewLine);
                Assert.Equal("Cerneala.Tests.PreviewHost.ConditionalTarget.View", dynamic.TargetTypeName);
                PreviewCompilation compilation = await compiler.CompileAsync(
                    documentPath,
                    saved);

                Assert.Equal("Cerneala.Tests.PreviewHost.ConditionalTarget.View", compilation.TargetTypeName);
                Assert.NotEqual(await File.ReadAllBytesAsync(builtAssembly), compilation.AssemblyImage);
            }
            finally
            {
                string safeRoot = Path.GetFullPath(fixtureRoot).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (!root.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The PreviewCompiler test root escaped its dedicated temporary directory.");
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public async Task UnpairedSavedMarkupKeepsTheExistingBuildOutputReuseContract()
        {
            string fixtureRoot = Path.Combine(
                Path.GetTempPath(),
                "Cerneala",
                nameof(PreviewCompilerFastPathTargetTypeTests));
            string root = Path.GetFullPath(Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N")));
            string outputDirectory = Path.Combine(root, "bin", "Debug", "net10.0");
            try
            {
                Directory.CreateDirectory(outputDirectory);
                string projectPath = Path.Combine(root, "Sample.csproj");
                string documentPath = Path.Combine(root, "View.crn");
                await File.WriteAllTextAsync(projectPath, """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <AssemblyName>Sample</AssemblyName>
                      </PropertyGroup>
                      <ItemGroup>
                        <AdditionalFiles Include="*.crn" />
                      </ItemGroup>
                    </Project>
                    """);
                await File.WriteAllTextAsync(documentPath, "<UserControl />");

                string builtAssembly = Path.Combine(outputDirectory, "Sample.dll");
                File.Copy(typeof(PreviewCompilerFastPathTargetTypeTests).Assembly.Location, builtAssembly);
                File.SetLastWriteTimeUtc(builtAssembly, DateTime.UtcNow.AddMinutes(1));

                using PreviewCompiler compiler = new(prewarmBuildOutput: false);
                PreviewCompilation compilation = await compiler.CompileAsync(
                    documentPath,
                    await File.ReadAllTextAsync(documentPath));

                Assert.Equal(typeof(PreviewCompilerFastPathTargetTypeTests).Assembly.GetName().Name, compilation.AssemblyName);
                Assert.Equal(await File.ReadAllBytesAsync(builtAssembly), compilation.AssemblyImage);
                Assert.Equal("View", compilation.TargetTypeName);
            }
            finally
            {
                string safeRoot = Path.GetFullPath(fixtureRoot).TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (!root.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The PreviewCompiler test root escaped its dedicated temporary directory.");
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}

namespace Cerneala.Tests.PreviewHost.CompetingFixture
{
    internal partial class View { }
}

namespace Cerneala.Tests.PreviewHost.FastPathFixture
{
    internal partial class View { }
}
