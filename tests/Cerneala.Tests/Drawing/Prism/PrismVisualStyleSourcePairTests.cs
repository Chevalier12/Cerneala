using System.Text.RegularExpressions;
using System.Text.Json;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismVisualStyleSourcePairTests
{
    [Fact]
    public void EveryAuditedVisualStyleHasShaderAndOwnedSourceAlgorithm()
    {
        string repositoryRoot = FindRepositoryRoot();
        string checklistPath = Path.Combine(
            repositoryRoot,
            "docs",
            "audits",
            "prism-visual-style-algorithm-checklist-2026-08-02.md");
        string shaderRoot = Path.Combine(
            repositoryRoot,
            "Drawing",
            "MonoGame",
            "Prism",
            "Shaders");
        string sourceRoot = Path.Combine(
            repositoryRoot,
            "Drawing",
            "Prism");
        string catalogPath = Path.Combine(
            repositoryRoot,
            "Cerneala.SourceGen",
            "Prism",
            "Catalog",
            "prism-catalog.json");
        string[] styleNames = File.ReadLines(checklistPath)
            .Select(line => Regex.Match(
                line,
                @"^\[[ xX]\]\s+([A-Za-z0-9]+)\s+(?:true|false)$"))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToArray();
        using JsonDocument catalog = JsonDocument.Parse(
            File.ReadAllText(catalogPath));
        HashSet<string> blendNames = catalog.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .Where(entry => string.Equals(
                entry.GetProperty("kind").GetString(),
                "blend-mode",
                StringComparison.Ordinal))
            .Select(entry => entry.GetProperty("symbol").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(47, styleNames.Length);
        Assert.Equal(styleNames.Length, styleNames.Distinct().Count());
        Assert.Equal(28, blendNames.Count);
        Assert.All(
            blendNames,
            blendName => Assert.Contains(blendName, styleNames));

        foreach (string styleName in styleNames)
        {
            string sourceKind = blendNames.Contains(styleName)
                ? "Blend"
                : "Style";
            string sourceType = $"Prism{styleName}{sourceKind}";
            string[] shaderPaths = Directory.EnumerateFiles(
                shaderRoot,
                $"{styleName}.fx",
                SearchOption.AllDirectories).ToArray();
            string sourcePath = Assert.Single(Directory.EnumerateFiles(
                sourceRoot,
                $"{sourceType}.cs",
                SearchOption.AllDirectories));
            string source = File.ReadAllText(sourcePath);

            Assert.NotEmpty(shaderPaths);
            Assert.All(
                shaderPaths,
                shaderPath => Assert.EndsWith($"{styleName}.fx", shaderPath));
            Assert.Contains(
                $"internal static class {sourceType}",
                source,
                StringComparison.Ordinal);
            Assert.True(
                Regex.Matches(
                    source,
                    @"\b(?:public|internal|private) static\b").Count >= 2,
                $"{sourceType} must own at least one static algorithm method.");
        }
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
