#Requires -Version 7
<#
.SYNOPSIS
Runs every decoder-probe case in its own process with a timeout.

.DESCRIPTION
A library that hangs is recorded as a "timeout" result instead of stalling
the campaign. Writes one JSON object per line to -ResultFile.
#>
param(
    [string] $Corpus = 'tests/Cerneala.Tests.Timbre/Corpus',
    [Parameter(Mandatory)] [string] $ResultFile,
    [int] $TimeoutSeconds = 120,
    [string] $Filter
)

$ErrorActionPreference = 'Stop'
$probe = Join-Path $PSScriptRoot 'bin/Release/net8.0/TimbreDecoderProbe.dll'
& dotnet build (Join-Path $PSScriptRoot 'TimbreDecoderProbe.csproj') -c Release -nologo -v:q | Out-Host
$keys = & dotnet $probe $Corpus ([IO.Path]::GetTempFileName()) --list | Where-Object { -not $Filter -or $_ -like "*$Filter*" }
Set-Content -LiteralPath $ResultFile -Value $null
foreach ($key in $keys) {
    $stdout = [IO.Path]::GetTempFileName()
    $arguments = @($probe, $Corpus, [IO.Path]::GetTempFileName(), "=$key") | ForEach-Object { "`"$_`"" }
    $process = Start-Process dotnet -ArgumentList ($arguments -join " ") -NoNewWindow -PassThru -RedirectStandardOutput $stdout
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill($true)
        $file, $candidate = $key -split ' \| ', 2
        $line = [ordered]@{ file = $file; candidate = $candidate; error = "timeout: no result after $TimeoutSeconds s (process killed)" } | ConvertTo-Json -Compress
    }
    else {
        $line = Get-Content -LiteralPath $stdout | Where-Object { $_.StartsWith("{") } | Select-Object -Last 1
        if (-not $line) {
            $file, $candidate = $key -split " | ", 2
            $line = [ordered]@{ file = $file; candidate = $candidate; error = "no result (exit $($process.ExitCode))" } | ConvertTo-Json -Compress
        }
    }

    Add-Content -LiteralPath $ResultFile -Value $line
    Write-Host ($line.Substring(0, [Math]::Min(160, $line.Length)))
    Remove-Item -LiteralPath $stdout -Force
}
