#requires -Version 7
# Timbre Motion cost campaign: 5 sequential processes per scenario (visible
# root, hidden control) of TimbreMotionBenchmarkRunner. Run from the
# repository root after a Release build:
#   dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'campaign'),
    [int]$Processes = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$project = Join-Path (Get-Location) 'benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj'
foreach ($scenario in 'visible', 'hidden') {
    for ($process = 1; $process -le $Processes; $process++) {
        $env:TIMBRE_BENCH_PROCESS = "$process"
        $env:TIMBRE_MOTION_HIDDEN = if ($scenario -eq 'hidden') { '1' } else { '0' }
        try {
            $report = Join-Path $OutputDirectory "$scenario-process-$process.json"
            dotnet run --project $project -c Release --no-build -- --timbre-motion $report | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Benchmark process $scenario/$process exited with $LASTEXITCODE."
            }
        }
        finally {
            Remove-Item Env:TIMBRE_BENCH_PROCESS -ErrorAction SilentlyContinue
            Remove-Item Env:TIMBRE_MOTION_HIDDEN -ErrorAction SilentlyContinue
        }
    }
}

$rows = Get-ChildItem $OutputDirectory -Filter '*-process-*.json' | Sort-Object Name | ForEach-Object {
    $report = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $gate = $report.Gate
    [pscustomobject]@{
        Scenario = $report.Scenario
        Process = $report.Process
        MixP50 = $report.MixP50Milliseconds
        MixP95 = $report.MixP95Milliseconds
        MixP99 = $report.MixP99Milliseconds
        MixAllocated = $report.MixMeasuredAllocatedBytes
        MaxQueued = $report.MaxQueuedFrames
        Underrun = $report.UnderrunFramesMeasured
        DeviceUnderrun = $report.DeviceUnderrunFramesMeasured
        UiP50 = $report.UiFrameP50Milliseconds
        UiP95 = $report.UiFrameP95Milliseconds
        UiP99 = $report.UiFrameP99Milliseconds
        UiBytesPerFrame = [math]::Round($report.UiMeasuredAllocatedBytes / [math]::Max(1, $report.MeasuredUiFrames), 1)
        Publications = $report.AnimatedPublicationsMeasured
        Layout = $report.MeasureCalls + $report.ArrangeCalls
        Rendered = $report.RenderedElements
        Rejected = $report.MotionSamplesRejected
        Pass = ($report.MixP99Milliseconds -lt $gate.MaxP99MixMilliseconds) -and
            ($report.MixMeasuredAllocatedBytes -le $gate.MaxSteadyStateAllocatedBytes) -and
            ($report.MaxQueuedFrames -le $gate.MaxQueuedFrames) -and
            ($report.UnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.DeviceUnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.MeasureCalls + $report.ArrangeCalls + $report.RenderedElements + $report.MotionRenderInvalidations + $report.MotionLayoutInvalidations -eq 0) -and
            ($report.MotionSamplesRejected -eq 0)
    }
}

$summary = [pscustomobject]@{
    Rows = $rows
    Verdict = if (($rows | Where-Object { -not $_.Pass }).Count -eq 0 -and $rows.Count -eq 2 * $Processes) { 'PASS' } else { 'FAIL' }
}
$summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDirectory 'summary.json')
$rows | Format-Table -AutoSize | Out-String -Width 240
"Verdict: $($summary.Verdict)"
