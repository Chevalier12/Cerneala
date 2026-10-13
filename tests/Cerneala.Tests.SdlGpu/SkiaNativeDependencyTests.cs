using System.Diagnostics;

namespace Cerneala.Tests.SdlGpu;

public sealed class SkiaNativeDependencyTests
{
    // Text shaping, text rasterization and image decoding load these natives on every
    // runtime identifier that the SDL_GPU backend documents.
    [Theory]
    [InlineData("win-x64", "libSkiaSharp.dll", "libHarfBuzzSharp.dll")]
    [InlineData("win-arm64", "libSkiaSharp.dll", "libHarfBuzzSharp.dll")]
    [InlineData("linux-x64", "libSkiaSharp.so", "libHarfBuzzSharp.so")]
    [InlineData("linux-arm64", "libSkiaSharp.so", "libHarfBuzzSharp.so")]
    [InlineData("osx", "libSkiaSharp.dylib", "libHarfBuzzSharp.dylib")]
    public void BuildOutputCarriesSkiaAndHarfBuzzNatives(string runtimeFolder, string skia, string harfBuzz)
    {
        string native = Path.Combine(AppContext.BaseDirectory, "runtimes", runtimeFolder, "native");

        Assert.True(File.Exists(Path.Combine(native, skia)), $"Missing {runtimeFolder}/native/{skia}.");
        Assert.True(File.Exists(Path.Combine(native, harfBuzz)), $"Missing {runtimeFolder}/native/{harfBuzz}.");
    }

    [Fact]
    public async Task PublishedAssetValidatorRejectsLinuxOutputWithoutSkiaNatives()
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
            foreach ($rid in @('linux-x64', 'linux-arm64')) {
                $published = Join-Path ([IO.Path]::GetTempPath()) "cerneala-skia-assets-$rid-$([guid]::NewGuid())"
                New-Item -ItemType Directory -Path $published | Out-Null
                try {
                    New-Item -ItemType File -Path (Join-Path $published 'libHarfBuzzSharp.so') | Out-Null
                    try {
                        Assert-SkiaPublishedAssets -RuntimeIdentifier $rid -PublishedDirectory $published | Out-Null
                        Write-Output "ACCEPTED-INCOMPLETE $rid"
                    }
                    catch {
                        Write-Output "REJECTED $rid $($_.Exception.Message)"
                    }

                    New-Item -ItemType File -Path (Join-Path $published 'libSkiaSharp.so') | Out-Null
                    Assert-SkiaPublishedAssets -RuntimeIdentifier $rid -PublishedDirectory $published | Out-Null
                    Write-Output "ACCEPTED-COMPLETE $rid"
                }
                finally {
                    Remove-Item -LiteralPath $published -Recurse -Force
                }
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
            string[] lines = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (string rid in new[] { "linux-x64", "linux-arm64" })
            {
                Assert.Contains(lines, line => line.StartsWith($"REJECTED {rid} ", StringComparison.Ordinal)
                    && line.Contains("libSkiaSharp.so", StringComparison.Ordinal));
                Assert.Contains($"ACCEPTED-COMPLETE {rid}", lines);
            }
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
}
