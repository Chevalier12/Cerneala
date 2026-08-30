[CmdletBinding()]
param(
    [ValidateRange(1, 20)]
    [int] $Iterations = 3,

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateRange(1, 10000)]
    [int] $WindowWidth = 1280,

    [ValidateRange(1, 10000)]
    [int] $WindowHeight = 800,

    [ValidateRange(0.01, 1000)]
    [double] $BudgetMilliseconds = (1000.0 / 144.0),

    [ValidateRange(30, 600)]
    [int] $ProcessTimeoutSeconds = 180,

    [switch] $NoBuild,

    [switch] $KeepReports
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$projectPath = Join-Path $repoRoot 'CernealaPresentation\CernealaPresentation.csproj'
$executablePath = Join-Path $repoRoot (
    "CernealaPresentation\bin\$Configuration\net8.0-windows\CernealaPresentation.exe")
$temporaryReports = [System.Collections.Generic.List[string]]::new($Iterations)
$samples = [System.Collections.Generic.List[object]]::new($Iterations * 2)

function Get-Percentile {
    param(
        [Parameter(Mandatory)]
        [double[]] $Values,

        [Parameter(Mandatory)]
        [ValidateRange(0, 1)]
        [double] $Percentile
    )

    if ($Values.Count -eq 0) {
        throw 'Cannot calculate a percentile from an empty sample set.'
    }

    $ordered = @($Values | Sort-Object)
    $index = [Math]::Min(
        $ordered.Count - 1,
        [Math]::Ceiling($Percentile * $ordered.Count) - 1)
    return [double]$ordered[$index]
}

if (-not $NoBuild) {
    & dotnet build $projectPath `
        -c $Configuration `
        --no-restore `
        --nologo `
        '-p:CernealaDesktopBackend=SDL3'
    if ($LASTEXITCODE -ne 0) {
        throw "The SDL3 $Configuration build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Presentation executable not found: $executablePath"
}

try {
    for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
        $reportPath = Join-Path ([System.IO.Path]::GetTempPath()) (
            'cerneala-prism-motion-blur-144hz-{0}.json' -f
                [guid]::NewGuid().ToString('N'))
        $temporaryReports.Add($reportPath)
        if ($KeepReports) {
            Write-Host "[$iteration/$Iterations] Report: $reportPath"
        }

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $executablePath
        $startInfo.WorkingDirectory = Split-Path -Parent $executablePath
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        $startInfo.Environment['CERNEALA_PRISM_OUTER_GLOW_LAB_REPORT'] = $reportPath
        $startInfo.Environment['CERNEALA_PRESENTATION_WIDTH'] = [string]$WindowWidth
        $startInfo.Environment['CERNEALA_PRESENTATION_HEIGHT'] = [string]$WindowHeight
        $startInfo.Environment['SDL_VIDEO_WINDOW_POS'] = '-32000,-32000'

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

        $errorPath = $reportPath + '.error.txt'
        if (Test-Path -LiteralPath $errorPath -PathType Leaf) {
            throw "Iteration $iteration failed: $(Get-Content -LiteralPath $errorPath -Raw)"
        }
        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
            throw "Iteration $iteration did not produce a report: $reportPath"
        }

        $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        foreach ($phaseName in 'MotionBlurDistance', 'MotionBlurDistanceRepeat') {
            $phase = $report.$phaseName
            $commandTimes = [double[]]@(
                $phase.Samples |
                    ForEach-Object { [double]$_.Timing.CommandRenderingMs })
            $sample = [pscustomobject]@{
                Iteration = $iteration
                Phase = $phaseName
                Frames = $commandTimes.Count
                P50Milliseconds = Get-Percentile $commandTimes 0.50
                P95Milliseconds = Get-Percentile $commandTimes 0.95
                P99Milliseconds = Get-Percentile $commandTimes 0.99
                MaximumMilliseconds = ($commandTimes | Measure-Object -Maximum).Maximum
                AllocatedBytes = [long](
                    ($phase.Samples | Measure-Object -Property AllocatedBytes -Sum).Sum)
                Gen0Collections = [int](
                    ($phase.Samples | Measure-Object -Property Gen0Collections -Sum).Sum)
            }
            $samples.Add($sample)
            Write-Host (
                '[{0}/{1}] {2,-24} P50={3,7:N3} ms P95={4,7:N3} ms P99={5,7:N3} ms max={6,7:N3} ms' -f
                    $iteration,
                    $Iterations,
                    $phaseName,
                    $sample.P50Milliseconds,
                    $sample.P95Milliseconds,
                    $sample.P99Milliseconds,
                    $sample.MaximumMilliseconds)
        }
    }

    $worstP95 = ($samples.P95Milliseconds | Measure-Object -Maximum).Maximum
    $summary = [pscustomobject]@{
        Backend = 'SDL_GPU'
        Configuration = $Configuration
        Window = "${WindowWidth}x${WindowHeight}"
        Target = '320x220'
        MotionBlurQuality = 'Good'
        MotionBlurDistance = '5..40..5'
        Iterations = $Iterations
        Frames = ($samples.Frames | Measure-Object -Sum).Sum
        BudgetMilliseconds = $BudgetMilliseconds
        WorstP95Milliseconds = $worstP95
        Passed = $worstP95 -le $BudgetMilliseconds
    }

    Write-Host ''
    $summary | Format-List
    Write-Output $samples

    if (-not $summary.Passed) {
        Write-Error (
            'SDL_GPU MotionBlur worst P95 {0:N3} ms exceeds the 144 Hz budget of {1:N3} ms.' -f
                $worstP95,
                $BudgetMilliseconds)
        exit 1
    }
}
finally {
    if (-not $KeepReports) {
        foreach ($reportPath in $temporaryReports) {
            foreach ($path in $reportPath, ($reportPath + '.error.txt')) {
                if (Test-Path -LiteralPath $path) {
                    Remove-Item -LiteralPath $path -Force
                }
            }
        }
    }
}
