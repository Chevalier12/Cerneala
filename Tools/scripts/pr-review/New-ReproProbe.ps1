#requires -Version 7.0
<#
.SYNOPSIS
Creates a console probe project per worktree tree (merge, master, ...) that references the tree's Cerneala.csproj, and optionally runs it.

.DESCRIPTION
For each -Tree, creates <Root>\probe\<Name>-<label>\ with Probe.csproj (net8.0 console app with a ProjectReference to
<tree>\Cerneala.csproj) and Program.cs (a copy of -Source, a .cs file with top-level statements). label is "merge" when the tree path
ends with \merge, "master" when it ends with \master, otherwise the leaf folder name. A normal project is used instead of CSI
because CSI could not load the SkiaSharp and HarfBuzz natives; here the NuGet native assets are copied next to the exe.

With -Run: dotnet run --project <probe>\Probe.csproj -c Release per probe; the output is captured and build warning lines are dropped.
The script prints exactly one JSON object { probes: [{ label, path, exitCode, output }] } (exitCode and output are $null without -Run).
It exits 0 when every probe was created (and, with -Run, started); the probe's own exit code is only reported, not turned into a script failure.
Recreating a probe replaces Program.cs and Probe.csproj; the folder is left in place (delete <Root>\probe yourself when done).

.PARAMETER Name
Probe name; the folder is <Name>-<label>.
.PARAMETER Source
Path to the .cs file with top-level statements.
.PARAMETER Tree
One or more worktree roots that contain Cerneala.csproj. With pwsh -File, pass them comma-separated: -Tree "<a>,<b>".
.PARAMETER Root
Short parent folder. Default $env:TEMP\crn.
.PARAMETER Run
Run each probe in Release.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/pr-review/New-ReproProbe.ps1 -Name rows -Source $env:TEMP\rows.cs -Tree $env:TEMP\crn\pr402\merge,$env:TEMP\crn\pr402\master -Run
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Name,
    [Parameter(Mandatory)][string] $Source,
    [Parameter(Mandatory)][string[]] $Tree,
    [string] $Root = (Join-Path $env:TEMP 'crn'),
    [switch] $Run
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PrReview.Common.ps1')

function Stop-WithError {
    param([string] $Message)
    Write-PrJson ([ordered]@{ error = $Message })
    exit 1
}

if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) { Stop-WithError "Source '$Source' does not exist." }
$Source = (Resolve-Path -LiteralPath $Source).ProviderPath
$Root = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')

# pwsh -File passes "a,b" as one string, so commas also separate trees.
$trees = foreach ($t in @($Tree | ForEach-Object { $_ -split ',' } | Where-Object { $_.Trim() })) {
    $full = [IO.Path]::GetFullPath($t).TrimEnd('\', '/')
    if (-not (Test-Path -LiteralPath (Join-Path $full 'Cerneala.csproj'))) { Stop-WithError "No Cerneala.csproj in '$full'." }
    [pscustomobject]@{ Path = $full; Label = Get-ProbeLabel -Tree $full }
}
$duplicate = $trees | Group-Object Label | Where-Object Count -gt 1 | Select-Object -First 1
if ($duplicate) { Stop-WithError "Two trees have the label '$($duplicate.Name)'." }

$projectTemplate = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><ProjectReference Include="{0}\Cerneala.csproj" /></ItemGroup></Project>'

$probes = @()
foreach ($t in $trees) {
    $dir = Join-Path $Root "probe\$Name-$($t.Label)"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Set-Content -LiteralPath (Join-Path $dir 'Probe.csproj') -Value ($projectTemplate -f $t.Path) -Encoding utf8NoBOM
    Copy-Item -LiteralPath $Source -Destination (Join-Path $dir 'Program.cs') -Force

    $exitCode = $null
    $output = $null
    if ($Run) {
        $result = Invoke-PrNative dotnet @('run', '--project', (Join-Path $dir 'Probe.csproj'), '-c', 'Release', '--nologo')
        $exitCode = $result.ExitCode
        $lines = @("$($result.Output)`n$($result.Error)" -split "\r?\n") |
            Where-Object { $_.Trim() -ne '' -and $_ -notmatch '\bwarning\b\s+[A-Za-z]+\d+\s*:' -and $_ -notmatch '^\s*\d+ Warning\(s\)' }
        $output = ($lines -join "`n")
    }
    $probes += [ordered]@{ label = $t.Label; path = $dir; exitCode = $exitCode; output = $output }
}

Write-PrJson ([ordered]@{ probes = $probes })
exit 0
