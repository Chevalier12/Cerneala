#requires -Version 7
# Timbre decoding cost campaign: 5 sequential processes, each running
#  1. the approved core cost gate (index §7: 32 voices, LowPass + Delay,
#     1000 warmup + 10000 measured 480-frame blocks, paced device) with its
#     4 streaming voices decoding real corpus files, and
#  2. the decoding runner (per-block decode-worker cost, throughput,
#     allocations, live memory, streaming start latency).
# Run from the repository root after a Release build:
#   dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'campaign'),
    [int]$Processes = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$project = Join-Path (Get-Location) 'benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj'
$corpus = Join-Path (Get-Location) 'tests\Cerneala.Tests.Timbre\Corpus'
for ($process = 1; $process -le $Processes; $process++) {
    $env:TIMBRE_BENCH_PROCESS = "$process"
    try {
        $env:TIMBRE_BENCH_DECODED_CORPUS = $corpus
        dotnet run --project $project -c Release --no-build -- --timbre-core (Join-Path $OutputDirectory "process-$process-gate.json") | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Gate process $process exited with $LASTEXITCODE." }
        Remove-Item Env:TIMBRE_BENCH_DECODED_CORPUS
        dotnet run --project $project -c Release --no-build -- --timbre-decoding (Join-Path $OutputDirectory "process-$process-decoding.json") $corpus | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Decoding process $process exited with $LASTEXITCODE." }
    }
    finally {
        Remove-Item Env:TIMBRE_BENCH_PROCESS -ErrorAction SilentlyContinue
        Remove-Item Env:TIMBRE_BENCH_DECODED_CORPUS -ErrorAction SilentlyContinue
    }
}

$gates = Get-ChildItem $OutputDirectory -Filter 'process-*-gate.json' | Sort-Object Name | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
$rows = foreach ($report in $gates) {
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

$decoding = Get-ChildItem $OutputDirectory -Filter 'process-*-decoding.json' | Sort-Object Name | ForEach-Object { Get-Content $_.FullName -Raw | ConvertFrom-Json }
$perFile = $decoding | ForEach-Object { $_.results } | Group-Object file | ForEach-Object {
    [pscustomobject]@{
        File = $_.Name
        BlockP99MaxUs = ($_.Group.timing.blockMicroseconds.p99 | Measure-Object -Maximum).Maximum
        BlockP99MinUs = ($_.Group.timing.blockMicroseconds.p99 | Measure-Object -Minimum).Minimum
        RealtimeFactorMin = ($_.Group.timing.fullDecodeRealtimeFactor | Measure-Object -Minimum).Minimum
        OpenMsMax = ($_.Group.timing.openMs | Measure-Object -Maximum).Maximum
        SteadyAllocatedBytesPerBlockMax = ($_.Group.timing.steadyAllocatedBytesPerBlock | Measure-Object -Maximum).Maximum
        LivePeakMax = ($_.Group.liveHeap.peak | Measure-Object -Maximum).Maximum
        Reserved = ($_.Group.reservedBytes | Measure-Object -Maximum).Maximum
    }
}

$startup = $decoding | ForEach-Object { $_.streamingStartup } | Group-Object file | ForEach-Object {
    [pscustomobject]@{
        File = $_.Name
        P95MaxMs = ($_.Group.p95 | Measure-Object -Maximum).Maximum
        P95MinMs = ($_.Group.p95 | Measure-Object -Minimum).Minimum
    }
}

$summary = [pscustomobject]@{
    Gate = $rows
    GateP99VariationMaxOverMin = (($rows.P99 | Measure-Object -Maximum).Maximum / ($rows.P99 | Measure-Object -Minimum).Minimum)
    GateVerdict = if (($rows | Where-Object { -not $_.Pass }).Count -eq 0 -and $rows.Count -eq $Processes) { 'PASS' } else { 'FAIL' }
    Decoding = $perFile
    StreamingStartup = $startup
}
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutputDirectory 'summary.json')
$rows | Format-Table -AutoSize | Out-String -Width 200
$perFile | Format-Table -AutoSize | Out-String -Width 200
$startup | Format-Table -AutoSize | Out-String -Width 200
"Gate verdict: $($summary.GateVerdict)"
