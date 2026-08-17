using System.Text.Json;

namespace Cerneala.Tests.VisualStudio;

public sealed class Stage6ReleaseHarnessTests
{
    [Fact]
    public void ReleaseBuildDefinesDeterministicSigningAndChecksumContract()
    {
        string script = File.ReadAllText(Path.Combine(
            VisualStudioPackageTests.RepositoryRoot(),
            "Tools",
            "scripts",
            "Build-CernealaVisualStudioRelease.ps1"));

        foreach (string required in new[]
        {
            "/p:Configuration=Release",
            "/p:ContinuousIntegrationBuild=true",
            "/p:Deterministic=true",
            "Normalize-Vsix",
            "Sort-Object -Property Name -CaseSensitive",
            "cerneala.visualstudio",
            "THIRD-PARTY-NOTICES.txt",
            "CERNEALA_VSIX_SIGNING_THUMBPRINT",
            "EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3'",
            "certificate-store",
            "-t $TimestampUrl",
            "digital-signature",
            "Get-FileHash",
            ".sha256"
        })
        {
            Assert.Contains(required, script, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("EnhancedKeyUsageList.ObjectId.Value", script, StringComparison.Ordinal);
        Assert.DoesNotContain("-tr $TimestampUrl", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseHarnessCoversInstallUpgradeDowngradeAndCleanUninstall()
    {
        string script = File.ReadAllText(Path.Combine(
            FixtureDirectory(),
            "Invoke-CommunityRelease.ps1"));

        foreach (string required in new[]
        {
            "VSIXInstaller.exe",
            "CreateExpInstance.exe",
            "VSRegEdit.exe",
            "Assert-NoVisualStudioProcesses",
            "Initialize-ExperimentalProfile",
            "General.vssettings",
            "AutoSaveFileIsFromFirstLaunch",
            "/RootSuffix=",
            "/rootSuffix:",
            "/instanceIds:",
            "PreviousVersion = '0.0.9'",
            "candidateBuild.UnsignedSha256 -ne $determinismBuild.UnsignedSha256",
            "downgrade.log",
            "Assert-InstalledVersion $Version",
            "/uninstall:Cerneala.Cerneala.VisualStudio",
            "Assert-NoCernealaResidue",
            "settingsCompatibility",
            "-SkipExtensionDeploy",
            "-SkipIntegrationHostDeploy"
        })
        {
            Assert.Contains(required, script, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void VisualStudioAutomationIsIsolatedAndContainsNoGlobalInputOrShutdown()
    {
        string release = File.ReadAllText(Path.Combine(FixtureDirectory(), "Invoke-CommunityRelease.ps1"));
        string integration = File.ReadAllText(Path.Combine(FixtureDirectory(), "Invoke-CommunityIntegration.ps1"));
        string automation = release + integration;

        Assert.Contains("-WindowStyle Hidden", automation, StringComparison.Ordinal);
        Assert.Contains("/NoSigninPrompt", automation, StringComparison.Ordinal);
        Assert.Contains("/NoSplash", automation, StringComparison.Ordinal);
        Assert.Contains("$dte.Solution.Open($solutionPath)", integration, StringComparison.Ordinal);
        Assert.Contains("Local\\Cerneala.VisualStudio.Release.$RootSuffix", release, StringComparison.Ordinal);
        Assert.Contains("Get-TestVisualStudioProcesses", release, StringComparison.Ordinal);

        foreach (string forbidden in new[]
        {
            "SendKeys",
            "SetForegroundWindow",
            "SwitchToThisWindow",
            "System.Windows.Clipboard",
            "Clipboard.Set",
            "Set-Clipboard",
            "/shutdownprocesses",
            "/NewInstance",
            "Computer Use"
        })
        {
            Assert.DoesNotContain(forbidden, automation, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ApiDocumentationManifestIsValidAndReferencesExistingFiles()
    {
        string repository = VisualStudioPackageTests.RepositoryRoot();
        string docsSite = Path.Combine(repository, "docs-site");
        string manifestPath = Path.Combine(docsSite, "documentation", "manifest.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement[] entries = manifest.RootElement.EnumerateArray().ToArray();

        Assert.NotEmpty(entries);
        Assert.Equal(
            entries.Length,
            entries.Select(entry => entry.GetProperty("name").GetString()).Distinct(StringComparer.Ordinal).Count());
        foreach (JsonElement entry in entries)
        {
            string relativePath = Assert.IsType<string>(entry.GetProperty("file").GetString());
            Assert.True(
                File.Exists(Path.Combine(docsSite, relativePath.Replace('/', Path.DirectorySeparatorChar))),
                $"API documentation manifest references missing file '{relativePath}'.");
        }
    }

    private static string FixtureDirectory() => Path.Combine(
        VisualStudioPackageTests.RepositoryRoot(),
        "tests",
        "Fixtures",
        "VisualStudioConsumer");
}
