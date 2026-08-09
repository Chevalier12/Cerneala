[CmdletBinding()]
param(
    [ValidateRange(1, 1000)]
    [int] $Iterations = 20,

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateRange(1, 10000)]
    [int] $WindowWidth = 1280,

    [ValidateRange(1, 10000)]
    [int] $WindowHeight = 800,

    [ValidateRange(0.01, 60000)]
    [double] $BudgetMilliseconds = 16.67,

    [ValidateRange(1, 300)]
    [int] $ProcessTimeoutSeconds = 30,

    [switch] $NoBuild,

    [switch] $AllowFewerThanTwenty
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'CernealaPresentation\CernealaPresentation.csproj'
$executablePath = Join-Path $repoRoot "CernealaPresentation\bin\$Configuration\net8.0-windows\CernealaPresentation.exe"

if ($Iterations -lt 20 -and -not $AllowFewerThanTwenty) {
    throw 'The cold-start gate requires at least 20 fresh processes. Use -AllowFewerThanTwenty only for local smoke measurements.'
}

if (-not $NoBuild) {
    & dotnet build $projectPath -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "The $Configuration build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Presentation executable not found: $executablePath"
}

$samples = [System.Collections.Generic.List[object]]::new($Iterations)
$temporaryReports = [System.Collections.Generic.List[string]]::new($Iterations)

try {
    for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
        $reportPath = Join-Path ([System.IO.Path]::GetTempPath()) (
            'cerneala-prism-outer-glow-cold-{0}.json' -f [guid]::NewGuid().ToString('N'))
        $temporaryReports.Add($reportPath)

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $executablePath
        $startInfo.WorkingDirectory = Split-Path -Parent $executablePath
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        $startInfo.Environment['CERNEALA_PRISM_OUTER_GLOW_LAB_REPORT'] = $reportPath
        $startInfo.Environment['CERNEALA_PRISM_OUTER_GLOW_LAB_COLD_ONLY'] = '1'
        $startInfo.Environment['CERNEALA_PRESENTATION_WIDTH'] = [string]$WindowWidth
        $startInfo.Environment['CERNEALA_PRESENTATION_HEIGHT'] = [string]$WindowHeight

        $process = [System.Diagnostics.Process]::Start($startInfo)
        try {
            if (-not $process.WaitForExit($ProcessTimeoutSeconds * 1000)) {
                $process.Kill($true)
                $process.WaitForExit()
                throw "Iteration $iteration timed out after $ProcessTimeoutSeconds seconds."
            }

            if ($process.ExitCode -ne 0) {
                throw "Iteration $iteration exited with code $($process.ExitCode)."
            }
        }
        finally {
            $process.Dispose()
        }

        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
            throw "Iteration $iteration did not produce a report: $reportPath"
        }

        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        $sample = [pscustomobject]@{
            Iteration = $iteration
            WallToFrameMs = [double]$report.ColdAdd.WallToFrameMilliseconds
            HandlerMs = [double]$report.ColdAdd.HandlerMilliseconds
            FrameProcessingMs = [double]$report.ColdAdd.FrameProcessingMilliseconds
            FrameElapsedMs = [double]$report.ColdAdd.FrameElapsedMilliseconds
            RetainedUpdateMs = [double]$report.ColdAdd.Timing.RetainedUpdateMs
            ScheduledProcessingMs = [double]$report.ColdAdd.Timing.ScheduledProcessingMs
            CommandRenderingMs = [double]$report.ColdAdd.Timing.CommandRenderingMs
            AllocatedBytes = [long]$report.ColdAdd.AllocatedBytes
        }
        $samples.Add($sample)
        Write-Host (
            '[{0,2}/{1}] wall={2,8:N2} ms handler={3,7:N2} ms frame={4,8:N2} ms draw={5,8:N2} ms' -f
            $iteration,
            $Iterations,
            $sample.WallToFrameMs,
            $sample.HandlerMs,
            $sample.FrameProcessingMs,
            $sample.CommandRenderingMs)
    }

    $orderedWall = @($samples.WallToFrameMs | Sort-Object)
    $p95Index = [Math]::Ceiling(0.95 * $orderedWall.Count) - 1
    $p95 = $orderedWall[$p95Index]
    $medianIndex = [Math]::Floor(($orderedWall.Count - 1) / 2)
    $median = if (($orderedWall.Count % 2) -eq 0) {
        ($orderedWall[$medianIndex] + $orderedWall[$medianIndex + 1]) / 2
    }
    else {
        $orderedWall[$medianIndex]
    }

    $summary = [pscustomobject]@{
        Configuration = $Configuration
        Iterations = $Iterations
        Window = "${WindowWidth}x${WindowHeight}"
        BudgetMilliseconds = $BudgetMilliseconds
        MinimumMilliseconds = ($orderedWall | Measure-Object -Minimum).Minimum
        MedianMilliseconds = $median
        P95Milliseconds = $p95
        MaximumMilliseconds = ($orderedWall | Measure-Object -Maximum).Maximum
        Passed = $p95 -le $BudgetMilliseconds
    }

    Write-Host ''
    $summary | Format-List
    Write-Output $samples

    if (-not $summary.Passed) {
        Write-Error ('Prism OuterGlow cold-start P95 {0:N2} ms exceeds the {1:N2} ms budget.' -f $p95, $BudgetMilliseconds)
        exit 1
    }
}
finally {
    foreach ($reportPath in $temporaryReports) {
        if (Test-Path -LiteralPath $reportPath) {
            Remove-Item -LiteralPath $reportPath -Force
        }
    }
}
