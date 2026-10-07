#requires -Version 7
# Timbre core cost campaign: 5 sequential processes of the frozen fixture
# (see cost-protocol.md). Run from the repository root after a Release build:
#   dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'campaign'),
    [int]$Processes = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$project = Join-Path (Get-Location) 'benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj'
for ($process = 1; $process -le $Processes; $process++) {
    $env:TIMBRE_BENCH_PROCESS = "$process"
    try {
        $report = Join-Path $OutputDirectory "process-$process.json"
        dotnet run --project $project -c Release --no-build -- --timbre-core $report | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Benchmark process $process exited with $LASTEXITCODE."
        }
    }
    finally {
        Remove-Item Env:TIMBRE_BENCH_PROCESS -ErrorAction SilentlyContinue
    }
}

$reports = Get-ChildItem $OutputDirectory -Filter 'process-*.json' | Sort-Object Name | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
$rows = foreach ($report in $reports) {
    $gate = $report.Gate
    [pscustomobject]@{
        Process = $report.Process
        P50 = $report.MixP50Milliseconds
        P95 = $report.MixP95Milliseconds
        P99 = $report.MixP99Milliseconds
        Max = $report.MixMaxMilliseconds
        AllocatedBytes = $report.MeasuredAllocatedBytes
        MaxQueued = $report.MaxQueuedFrames
        DeviceUnderrun = $report.DeviceUnderrunFramesMeasured
        VoiceUnderrun = $report.VoiceUnderrunFramesMeasured
        LatencyP95 = $report.PreparedPlayToFirstQueuedP95Milliseconds
        Pass = ($report.MixP99Milliseconds -lt $gate.MaxP99MixMilliseconds) -and
            ($report.MeasuredAllocatedBytes -le $gate.MaxSteadyStateAllocatedBytes) -and
            ($report.MaxQueuedFrames -le $gate.MaxQueuedFrames) -and
            ($report.DeviceUnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.VoiceUnderrunFramesMeasured -le $gate.MaxUnderrunFrames) -and
            ($report.PreparedPlayToFirstQueuedP95Milliseconds -lt $gate.MaxPreparedPlayToFirstQueuedP95Milliseconds)
    }
}

$summary = [pscustomobject]@{
    Processes = $rows
    P99VariationMaxOverMin = (($rows.P99 | Measure-Object -Maximum).Maximum / ($rows.P99 | Measure-Object -Minimum).Minimum)
    P95VariationMaxOverMin = (($rows.P95 | Measure-Object -Maximum).Maximum / ($rows.P95 | Measure-Object -Minimum).Minimum)
    LatencyP95VariationMaxOverMin = (($rows.LatencyP95 | Measure-Object -Maximum).Maximum / ($rows.LatencyP95 | Measure-Object -Minimum).Minimum)
    Verdict = if (($rows | Where-Object { -not $_.Pass }).Count -eq 0 -and $rows.Count -eq $Processes) { 'PASS' } else { 'FAIL' }
}
$summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDirectory 'summary.json')
$rows | Format-Table -AutoSize | Out-String -Width 200
"Verdict: $($summary.Verdict)"
