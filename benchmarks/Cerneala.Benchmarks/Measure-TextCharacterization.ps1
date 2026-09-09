[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,
    [ValidateRange(1, 5)]
    [int] $Runs = 3,
    [ValidateRange(30, 300)]
    [int] $CaseTimeoutSeconds = 120,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "Use a new output directory; refusing to mix or overwrite results: $outputPath"
}

Push-Location $repoPath
try {
    if (-not $NoBuild) {
        dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Benchmark build failed.' }
    }
    $benchmarkDll = Join-Path $PSScriptRoot 'bin\Release\net8.0-windows\Cerneala.Benchmarks.dll'
    if (-not (Test-Path -LiteralPath $benchmarkDll)) { throw "Missing benchmark: $benchmarkDll" }
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    $cases = @(
        'pipeline-32', 'pipeline-512',
        'cache-shape-32', 'cache-shape-512',
        'cache-blob-32', 'cache-blob-512',
        'cache-layout-32', 'cache-layout-512',
        'native-static', 'native-unique', 'native-cycle', 'native-phases', 'native-first-view'
    )
    $environmentRecord = [ordered]@{
        TimestampUtc = [DateTimeOffset]::UtcNow
        Commit = (git rev-parse HEAD)
        WorkingTreeStatus = @(git status --short)
        Processor = @(Get-CimInstance Win32_Processor | Select-Object Name, NumberOfCores, NumberOfLogicalProcessors)
        VideoControllers = @(Get-CimInstance Win32_VideoController | Select-Object Name, DriverVersion, CurrentRefreshRate)
        PowerScheme = (powercfg /getactivescheme | Out-String).Trim()
        Runs = $Runs
        CaseTimeoutSeconds = $CaseTimeoutSeconds
        Cases = $cases
        Order = 'Serialized; each case/repetition is a fresh process. No build or profiler during measurements.'
    }
    $environmentRecord | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $outputPath 'environment.json') -Encoding utf8

    for ($run = 1; $run -le $Runs; $run++) {
        foreach ($case in $cases) {
            $casePath = Join-Path $outputPath "$case-$run.json"
            $startInfo = [Diagnostics.ProcessStartInfo]::new('dotnet')
            $startInfo.WorkingDirectory = $repoPath
            $startInfo.UseShellExecute = $false
            $startInfo.CreateNoWindow = $true
            $startInfo.RedirectStandardOutput = $true
            $startInfo.RedirectStandardError = $true
            foreach ($argument in @($benchmarkDll, '--text-characterization', $case, $casePath)) {
                $startInfo.ArgumentList.Add($argument)
            }
            $process = [Diagnostics.Process]::new()
            $process.StartInfo = $startInfo
            $started = $false
            try {
                $started = $process.Start()
                if (-not $started) { throw "Could not start $case, run $run." }
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                if (-not $process.WaitForExit($CaseTimeoutSeconds * 1000)) {
                    $process.Kill($true)
                    $process.WaitForExit()
                    throw "$case, run $run exceeded $CaseTimeoutSeconds seconds."
                }
                $stdout.GetAwaiter().GetResult() |
                    Set-Content -LiteralPath (Join-Path $outputPath "$case-$run.stdout.txt") -Encoding utf8
                $stderr.GetAwaiter().GetResult() |
                    Set-Content -LiteralPath (Join-Path $outputPath "$case-$run.stderr.txt") -Encoding utf8
                if ($process.ExitCode -ne 0) { throw "$case, run $run exited $($process.ExitCode); see stderr." }
                if (-not (Test-Path -LiteralPath $casePath)) { throw "$case, run $run produced no report." }
                $report = Get-Content -LiteralPath $casePath -Raw | ConvertFrom-Json
                if ($report.Schema -ne 'cerneala-text-characterization-v1' -or $report.Scenario -ne $case) {
                    throw "Unexpected report for $case, run $run."
                }
                Write-Host "$case / run $run completed."
            }
            finally {
                if ($started -and -not $process.HasExited) {
                    $process.Kill($true)
                    $process.WaitForExit()
                }
                $process.Dispose()
            }
        }
    }
    Write-Host "Text characterization complete: $outputPath"
}
finally {
    Pop-Location
}
