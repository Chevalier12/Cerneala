#requires -Version 7
# Attribution campaign for device/engine underruns of the visible-root Motion
# scenario: per round, the audio-Motion process and two controls with the same
# 32 voices and 60 Hz UI loop (one visual animation; no Motion), interleaved so
# host conditions affect all three alike. Run with nothing else on the host.
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'attribution'),
    [int]$Rounds = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$project = Join-Path (Get-Location) 'benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj'
for ($round = 1; $round -le $Rounds; $round++) {
    foreach ($control in 'audio', 'visual', 'none') {
        $env:TIMBRE_BENCH_PROCESS = "$round"
        $env:TIMBRE_MOTION_CONTROL = $control
        try {
            dotnet run --project $project -c Release --no-build -- --timbre-motion (Join-Path $OutputDirectory "$control-round-$round.json") | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "Process $control/$round exited with $LASTEXITCODE." }
        }
        finally {
            Remove-Item Env:TIMBRE_BENCH_PROCESS, Env:TIMBRE_MOTION_CONTROL -ErrorAction SilentlyContinue
        }
    }
}

Get-ChildItem $OutputDirectory -Filter '*-round-*.json' | Sort-Object Name | ForEach-Object {
    $r = Get-Content $_.FullName -Raw | ConvertFrom-Json
    [pscustomobject]@{
        Run = $_.BaseName
        MixP99 = $r.MixP99Milliseconds
        MixMax = $r.MixMaxMilliseconds
        Underrun = $r.UnderrunFramesMeasured
        DeviceUnderrun = $r.DeviceUnderrunFramesMeasured
        UiP99 = $r.UiFrameP99Milliseconds
        UiBytesPerFrame = [math]::Round($r.UiMeasuredAllocatedBytes / [math]::Max(1, $r.MeasuredUiFrames), 1)
        Gen0 = $r.Gen0Collections
        Publications = $r.AnimatedPublicationsMeasured
    }
} | Format-Table -AutoSize | Out-String -Width 200
