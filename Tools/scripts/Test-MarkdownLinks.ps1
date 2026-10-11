[CmdletBinding()]
param(
    [string] $Root,
    [string] $Baseline,
    [string] $WriteBaseline
)

# Reports relative Markdown links whose target file or folder does not exist.
# External links (any URI scheme), anchor-only links, fenced code and inline
# code are ignored; anchors are stripped and not validated. Broken targets are
# compared by their repository-relative resolved path, so moving a document
# while keeping its links pointed at the same targets does not change the set.

$ErrorActionPreference = 'Stop'

if (-not $Root) {
    $Root = Join-Path $PSScriptRoot '..\..'
}

$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/')

$files = & git -c core.quotepath=off -C $Root ls-files --cached --others --exclude-standard -- '*.md'
if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed in '$Root'."
}

$files = @($files |
    Where-Object { $_ -notlike 'docs/plans/evidence/*' } |
    Sort-Object -Unique)

$inlineLink = [regex]'\[(?:[^\]\\]|\\.)*\]\(\s*(<[^>]*>|[^)\s]+)(?:\s+(?:"[^"]*"|''[^'']*''|\([^)]*\)))?\s*\)'
$referenceLink = [regex]'^\s{0,3}\[[^\]]+\]:\s*(<[^>]*>|\S+)'
$inlineCode = [regex]'`+[^`]*`+'
$fence = [regex]'^\s*(```|~~~)'
$scheme = [regex]'^[A-Za-z][A-Za-z0-9+.-]*:'

function Resolve-LinkTarget {
    param(
        [string] $SourceDirectory,
        [string] $Target
    )

    $value = $Target.Trim()
    if ($value.StartsWith('<') -and $value.EndsWith('>')) {
        $value = $value.Substring(1, $value.Length - 2)
    }

    if ($value.Length -eq 0 -or $value.StartsWith('#') -or $scheme.IsMatch($value)) {
        return $null
    }

    $cut = $value.IndexOfAny([char[]]@('#', '?'))
    if ($cut -ge 0) {
        $value = $value.Substring(0, $cut)
    }

    if ($value.Length -eq 0) {
        return $null
    }

    $value = [System.Uri]::UnescapeDataString($value)
    $base = if ($value.StartsWith('/')) { $Root } else { $SourceDirectory }
    return [System.IO.Path]::GetFullPath((Join-Path $base $value.TrimStart('/')))
}

function Get-RelativePath {
    param([string] $FullPath)

    if ($FullPath.StartsWith($Root + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullPath.Substring($Root.Length + 1).Replace('\', '/')
    }

    return $FullPath.Replace('\', '/')
}

$broken = [System.Collections.Generic.List[object]]::new()

foreach ($file in $files) {
    $sourcePath = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        continue
    }

    $sourceDirectory = Split-Path -Parent $sourcePath
    $inFence = $false
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines($sourcePath)) {
        $lineNumber++
        if ($fence.IsMatch($line)) {
            $inFence = -not $inFence
            continue
        }

        if ($inFence) {
            continue
        }

        $text = $inlineCode.Replace($line, '')
        $targets = @($inlineLink.Matches($text) | ForEach-Object { $_.Groups[1].Value })
        $reference = $referenceLink.Match($text)
        if ($reference.Success) {
            $targets += $reference.Groups[1].Value
        }

        foreach ($target in $targets) {
            $resolved = Resolve-LinkTarget -SourceDirectory $sourceDirectory -Target $target
            if ($null -ne $resolved -and -not (Test-Path -LiteralPath $resolved)) {
                $broken.Add([pscustomobject]@{
                    Source = $file
                    Line = $lineNumber
                    Target = Get-RelativePath $resolved
                })
            }
        }
    }
}

foreach ($entry in $broken) {
    Write-Output "$($entry.Source):$($entry.Line) -> $($entry.Target)"
}

$brokenTargets = @($broken | ForEach-Object { $_.Target } | Sort-Object -Unique)
Write-Output "Scanned $($files.Count) Markdown file(s); $($broken.Count) broken link(s) to $($brokenTargets.Count) unique target(s)."

if ($WriteBaseline) {
    $baselineDirectory = Split-Path -Parent $WriteBaseline
    if ($baselineDirectory -and -not (Test-Path -LiteralPath $baselineDirectory)) {
        New-Item -ItemType Directory -Path $baselineDirectory | Out-Null
    }

    Set-Content -LiteralPath $WriteBaseline -Value $brokenTargets -Encoding utf8
    Write-Output "Wrote baseline with $($brokenTargets.Count) target(s) to $WriteBaseline."
    exit 0
}

$known = @()
if ($Baseline) {
    $known = @(Get-Content -LiteralPath $Baseline | Where-Object { $_ })
}

$knownSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$known, [System.StringComparer]::Ordinal)
$new = @($brokenTargets | Where-Object { -not $knownSet.Contains($_) })
if ($new.Count -gt 0) {
    Write-Output "$($new.Count) broken target(s) not in the baseline:"
    foreach ($target in $new) {
        Write-Output "NEW $target"
    }

    exit 1
}

exit 0
