#Requires -Version 7
<#
.SYNOPSIS
Formats probe-results.jsonl as a Markdown table (one row per file and candidate).
#>
param([Parameter(Mandatory)] [string] $ResultFile)

$rows = Get-Content -LiteralPath $ResultFile | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json -AsHashtable }
'| File | Candidate | Frames / expected | Lag | SNR L / R dB | Open read | First output read | xRT | Seek max diff | Seek lag | Loop diff | Live heap after decode (KiB) | Result |'
'| --- | --- | --- | ---: | --- | --- | --- | ---: | ---: | --- | ---: | ---: | --- |'
foreach ($r in $rows) {
    if ($r.error) {
        "| $($r.file) | $($r.candidate) | | | | | | | | | | | $($r.error -replace '\|', '/') |"
        continue
    }

    $seekDiff = ($r.seeks | ForEach-Object { $_.maxDiffVsContinuous } | Measure-Object -Maximum).Maximum
    $seekLag = ($r.seeks | ForEach-Object { $_.lag }) -join ','
    $seekErrors = @($r.seeks | Where-Object { $_.error })
    $loopDiff = ($r.loops | ForEach-Object { $_.maxDiffVsFirstPass } | Measure-Object -Maximum).Maximum
    $declared = if ($null -eq $r.declaredLength) { '?' } else { $r.declaredLength }
    $status = @()
    if (-not $r.eofStable) { $status += 'EOF unstable' }
    if ($seekErrors.Count) { $status += "seek error: $($seekErrors[0].error)" }
    $result = if ($status) { $status -join '; ' } else { 'ok' }
    '| {0} | {1} | {2} ({3}) / {4} | {5} | {6} / {7} | {8:P0} | {9:P1} | {10} | {11:G3} | {12} | {13:G3} | {14:N0} | {15} |' -f `
        $r.file, $r.candidate, $r.frames, $declared, $r.expectedFrames, $r.oracle.lag, $r.oracle.snrDbLeft, $r.oracle.snrDbRight,
        ($r.open.bytesRead / $r.fileBytes), $r.firstOutput.fractionOfFile, $r.decode.realtimeFactor, $seekDiff, $seekLag, $loopDiff,
        ($r.retainedHeap.afterFullDecode / 1KB), ($result -replace '\|', '/')
}
