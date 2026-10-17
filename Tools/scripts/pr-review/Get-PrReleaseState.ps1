#requires -Version 7.0
<#
.SYNOPSIS
Read-only: reports the reproduction-app release of a PR (repro-pr-<Number>) and the PR's GIFs in the permanent repro-gifs release.

.DESCRIPTION
Prints exactly one JSON object: tag ("repro-pr-<Number>"), releaseExists (gh release view <tag>; "release not found" gives
false), assets (asset names of that release), tagExists (git ls-remote origin refs/tags/<tag> prints something),
reproGifs (names of assets in the release repro-gifs that contain "pr<Number>-", for example Issue268-pr277-before.gif).
Run it before and after gh release delete. Exit code 0, or 2 when gh or git failed (JSON { error }).

.PARAMETER Number
Pull request number.
.PARAMETER Repository
owner/name. Default Chevalier12/Cerneala.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/pr-review/Get-PrReleaseState.ps1 -Number 277
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int] $Number,
    [string] $Repository = 'Chevalier12/Cerneala'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PrReview.Common.ps1')

function Stop-WithError {
    param([string] $Message)
    Write-PrJson ([ordered]@{ number = $Number; error = $Message })
    exit 2
}

function Get-ReleaseAssetNames {
    <# Asset names of a release, or $null when the release does not exist. #>
    param([string] $Tag)

    $view = Invoke-PrNative gh @('release', 'view', $Tag, '--repo', $Repository, '--json', 'tagName,assets')
    if ($view.ExitCode -ne 0) {
        if ("$($view.Error) $($view.Output)" -match 'release not found') { return $null }
        Stop-WithError "gh release view $Tag failed: $($view.Error)"
    }
    $release = $view.Output | ConvertFrom-Json
    return , @($release.assets | ForEach-Object { $_.name })
}

$tag = "repro-pr-$Number"
$assets = Get-ReleaseAssetNames -Tag $tag
$releaseExists = ($null -ne $assets)

$remote = Invoke-PrNative git @('ls-remote', 'origin', "refs/tags/$tag")
if ($remote.ExitCode -ne 0) { Stop-WithError "git ls-remote failed: $($remote.Error)" }

$gifAssets = Get-ReleaseAssetNames -Tag 'repro-gifs'
$gifPattern = "(?<![A-Za-z])pr$Number-"
$reproGifs = [string[]]@($gifAssets | Where-Object { $_ -match $gifPattern })

Write-PrJson ([ordered]@{
        tag           = $tag
        releaseExists = $releaseExists
        assets        = [string[]]@(if ($releaseExists) { $assets })
        tagExists     = [bool]$remote.Output
        reproGifs     = $reproGifs
    })
exit 0
