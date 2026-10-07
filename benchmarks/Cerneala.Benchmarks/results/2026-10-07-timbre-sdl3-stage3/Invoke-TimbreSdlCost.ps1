#requires -Version 7
# Timbre SDL3 backend cost campaign (cost-protocol.md): 5 sequential processes
# of the approved core gate with real decoded streaming voices, either on the
# SDL3 default playback device (-Output sdl) or on the device emulator.
# Run from the repository root after a Release build:
#   dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release
param(
    [ValidateSet('sdl', 'emulator')]
    [string]$Output = 'sdl',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "campaign-$Output"),
    [int]$Processes = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$project = Join-Path (Get-Location) 'benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj'
$corpus = Join-Path (Get-Location) 'tests\Cerneala.Tests.Timbre\Corpus'
for ($process = 1; $process -le $Processes; $process++) {
    $env:TIMBRE_BENCH_PROCESS = "$process"
    $env:TIMBRE_BENCH_DECODED_CORPUS = $corpus
    $env:TIMBRE_BENCH_OUTPUT = if ($Output -eq 'sdl') { 'sdl' } else { $null }
    try {
        dotnet run --project $project -c Release --no-build -- --timbre-core (Join-Path $OutputDirectory "process-$process.json") | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Process $process exited with $LASTEXITCODE." }
    }
    finally {
        $env:TIMBRE_BENCH_PROCESS = $null
        $env:TIMBRE_BENCH_DECODED_CORPUS = $null
        $env:TIMBRE_BENCH_OUTPUT = $null
    }
}

$reports = Get-ChildItem $OutputDirectory -Filter 'process-*.json' | Sort-Object Name | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
$rows = foreach ($report in $reports) {
    $gate = $report.Gate
    $resources = $Output -ne 'sdl' -or ($report.OutputOpens -eq 1 -and $report.LateCallbacks -eq 0)
    [pscustomobject]@{
        Process = $report.Process
        Output = $report.Output
        P50 = $report.MixP50Milliseconds
        P95 = $report.MixP95Milliseconds
        P99 = $report.MixP99Milliseconds
        Max = $report.MixMaxMilliseconds
        AllocatedBytes = $report.MeasuredAllocatedBytes
        MaxQueued = $report.MaxQueuedFrames
        DeviceUnderrun = $report.DeviceUnderrunFramesMeasured
        VoiceUnderrun = $report.VoiceUnderrunFramesMeasured
        LatencyP95 = $report.PreparedPlayToFirstQueuedP95Milliseconds
        Opens = $report.OutputOpens
        Late = $report.LateCallbacks
        Pass = $resources -and
            ($report.MixP99Milliseconds -lt $gate.MaxP99MixMilliseconds) -and
            ($report.MeasuredAllocatedBytes -le $gate.MaxSteadyStateAllocatedBytes) -and
            ($report.MaxQueuedFrames -le $gate.MaxQueuedFrames) -and
            ($report.DeviceUnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.VoiceUnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.PreparedPlayToFirstQueuedP95Milliseconds -lt $gate.MaxPreparedPlayToFirstQueuedP95Milliseconds)
    }
}

$summary = [pscustomobject]@{
    Output = $Output
    Gate = $rows
    P99VariationMaxOverMin = (($rows.P99 | Measure-Object -Maximum).Maximum / ($rows.P99 | Measure-Object -Minimum).Minimum)
    Verdict = if (($rows | Where-Object { -not $_.Pass }).Count -eq 0 -and $rows.Count -eq $Processes) { 'PASS' } else { 'FAIL' }
}
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutputDirectory 'summary.json')
$rows | Format-Table -AutoSize | Out-String -Width 220
"Verdict ($Output): $($summary.Verdict)"
