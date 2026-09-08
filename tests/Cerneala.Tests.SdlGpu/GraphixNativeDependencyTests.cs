using System.Diagnostics;
using SDL3;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class GraphixNativeDependencyTests
{
    [Fact]
    public async Task PublishedAssetValidatorAcceptsAllSixRestoredGraphixRuntimes()
    {
        DirectoryInfo? repository = new(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Cerneala.slnx")))
        {
            repository = repository.Parent;
        }
        Assert.NotNull(repository);

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("pwsh")
            {
                WorkingDirectory = repository.FullName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.StartInfo.ArgumentList.Add("-NoLogo");
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-NonInteractive");
        process.StartInfo.ArgumentList.Add("-Command");
        process.StartInfo.ArgumentList.Add("""
            $ErrorActionPreference = 'Stop'
            . ./Tools/scripts/SdlGpuSmoke.Common.ps1
            $assets = Get-Content ./tests/Cerneala.Tests.SdlGpu/obj/project.assets.json -Raw | ConvertFrom-Json
            $package = @($assets.libraries.PSObject.Properties | Where-Object Name -like 'Graphix.Native/*')
            if ($package.Count -ne 1) { throw 'Expected one restored Graphix.Native package.' }
            $packageDirectory = $assets.packageFolders.PSObject.Properties.Name |
                ForEach-Object { Join-Path $_ $package[0].Value.path } |
                Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
                Select-Object -First 1
            if (-not $packageDirectory) { throw 'The restored Graphix.Native package directory is missing.' }
            foreach ($rid in @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')) {
                Assert-SdlGpuPublishedAssets -RuntimeIdentifier $rid `
                    -PublishedDirectory (Join-Path $packageDirectory "runtimes/$rid/native") | Out-Null
                Write-Output "VALIDATED $rid"
            }
            """);

        Assert.True(process.Start());
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
            string output = await standardOutput;
            string error = await standardError;
            Assert.True(process.ExitCode == 0, output + Environment.NewLine + error);
            Assert.Equal(6, output.Split('\n').Count(line => line.StartsWith("VALIDATED ", StringComparison.Ordinal)));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
    }

    [SdlNativeFact]
    public void LoadedNativeRuntimeIdentifiesTheGraphixBuild()
    {
        // Query the loaded native library, not the managed binding's version.
        Assert.Contains(
            "Graphix 3.4.16-graphix.4 commit 7dcfac5a73007e72bd8fb060861e46fe07f55c46",
            SDL.GetRevision(),
            StringComparison.Ordinal);
    }
}
