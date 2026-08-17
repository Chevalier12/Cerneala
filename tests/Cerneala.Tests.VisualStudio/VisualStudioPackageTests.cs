using System.IO.Compression;
using System.Xml.Linq;

namespace Cerneala.Tests.VisualStudio;

public sealed class VisualStudioPackageTests
{
    private static readonly XNamespace Vsix = "http://schemas.microsoft.com/developer/vsx-schema/2011";

    [Fact]
    public void ManifestTargetsOnlyCommunity18AndDeclaresStableIdentity()
    {
        XDocument manifest = XDocument.Load(Path.Combine(ProjectDirectory(), "source.extension.vsixmanifest"));
        XElement identity = Assert.Single(manifest.Descendants(Vsix + "Identity"));
        Assert.Equal("Cerneala.Cerneala.VisualStudio", (string?)identity.Attribute("Id"));
        Assert.Equal("Cerneala", (string?)identity.Attribute("Publisher"));
        Assert.Equal("|%CurrentProject%;GetVsixVersion|", (string?)identity.Attribute("Version"));

        XElement target = Assert.Single(manifest.Descendants(Vsix + "InstallationTarget"));
        Assert.Equal("Microsoft.VisualStudio.Community", (string?)target.Attribute("Id"));
        Assert.Equal("[18.0,19.0)", (string?)target.Attribute("Version"));
        Assert.Equal("amd64", target.Element(Vsix + "ProductArchitecture")?.Value);

        XElement prerequisite = Assert.Single(manifest.Descendants(Vsix + "Prerequisite"));
        Assert.Equal("Microsoft.VisualStudio.Component.CoreEditor", (string?)prerequisite.Attribute("Id"));
        Assert.Equal("[18.0,19.0)", (string?)prerequisite.Attribute("Version"));
    }

    [Fact]
    public void VsixContainsHostServerGrammarConfigurationAndAssets()
    {
        string expectedVersion = ProjectVersion();
        string vsixPath = FindVsix();
        using ZipArchive package = ZipFile.OpenRead(vsixPath);
        HashSet<string> entries = package.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Cerneala.VisualStudio.dll", entries);
        Assert.Contains("Cerneala.pkgdef", entries);
        Assert.Contains("Cerneala.VisualStudio.pkgdef", entries);
        Assert.Contains("Grammars/cerneala.tmLanguage.json", entries);
        Assert.Contains("language-configuration.json", entries);
        Assert.Contains("Assets/cerneala.png", entries);
        Assert.Contains("LICENSE", entries);
        Assert.Contains("THIRD-PARTY-NOTICES.txt", entries);
        Assert.Contains(entries, entry => entry.Equals(
            $"Server/{expectedVersion}/Cerneala.LanguageServer.exe",
            StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Equals(
            $"Server/{expectedVersion}/coreclr.dll",
            StringComparison.OrdinalIgnoreCase));

        ZipArchiveEntry manifestEntry = Assert.Single(package.Entries.Where(entry => entry.FullName.Equals(
            "extension.vsixmanifest",
            StringComparison.OrdinalIgnoreCase)));
        using Stream manifestStream = manifestEntry.Open();
        XDocument packagedManifest = XDocument.Load(manifestStream);
        XElement packagedIdentity = Assert.Single(packagedManifest.Descendants(Vsix + "Identity"));
        Assert.Equal(expectedVersion, (string?)packagedIdentity.Attribute("Version"));
    }

    [Fact]
    public void CrnRegistrationDoesNotClaimXmlOrCompoundExtensions()
    {
        string registration = File.ReadAllText(Path.Combine(ProjectDirectory(), "Cerneala.pkgdef"));
        Assert.Contains("\"crn\"=dword:00000064", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("\"xml\"=", registration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cui.xml", registration, StringComparison.OrdinalIgnoreCase);

        string contentType = File.ReadAllText(Path.Combine(ProjectDirectory(), "CernealaContentType.cs"));
        Assert.Contains("[FileExtension(\".crn\")]", contentType, StringComparison.Ordinal);
        Assert.DoesNotContain(".xml", contentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PackageRegistrationIsGeneratedWithoutAnAutoLoadRule()
    {
        string vsixPath = FindVsix();
        using ZipArchive package = ZipFile.OpenRead(vsixPath);
        ZipArchiveEntry registration = Assert.Single(package.Entries.Where(entry => entry.FullName.Equals(
            "Cerneala.VisualStudio.pkgdef",
            StringComparison.OrdinalIgnoreCase)));
        using StreamReader reader = new(registration.Open());
        string content = reader.ReadToEnd();

        Assert.Contains("f7d79e1c-8074-46ec-80ca-79347f6d896a", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cerneala.VisualStudio.dll", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AutoLoadPackages", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindVsix()
    {
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string path = Path.Combine(
            ProjectDirectory(),
            "bin",
            configuration,
            "net472",
            "Cerneala.VisualStudio.vsix");
        Assert.True(File.Exists(path), $"Expected VSIX '{path}' was not built.");
        return path;
    }

    private static string ProjectDirectory() => Path.Combine(RepositoryRoot(), "Cerneala.VisualStudio");

    private static string ProjectVersion()
    {
        XDocument project = XDocument.Load(Path.Combine(ProjectDirectory(), "Cerneala.VisualStudio.csproj"));
        return Assert.Single(project.Descendants("Version")).Value;
    }

    internal static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
