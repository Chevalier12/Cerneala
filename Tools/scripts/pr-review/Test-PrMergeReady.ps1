#requires -Version 7.0
<#
.SYNOPSIS
Read-only check before a merge, and again after it: did the PR head or master move, and is the PR merged?

.DESCRIPTION
Runs git fetch origin master (in the current directory, any worktree of the repository), then gh pr view --json state,headRefOid,mergedAt.
Prints exactly one JSON object: state, headRefOid, mergedAt, headMatches (headRefOid equals -Head), masterMoved
(origin/master differs from -Base), newBase (the current origin/master). Exit code 0, or 2 when git or gh failed (JSON { error }).

.PARAMETER Number
Pull request number.
.PARAMETER Head
PR head commit the review was bound to (40 hex characters).
.PARAMETER Base
origin/master commit the merge tree was built on (40 hex characters).
.PARAMETER Repository
owner/name. Default Chevalier12/Cerneala.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/pr-review/Test-PrMergeReady.ps1 -Number 402 -Head 73de8e08664c6094b17b6f4e9042e6302aefab3b -Base (git rev-parse origin/master)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int] $Number,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string] $Head,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string] $Base,
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

$fetch = Invoke-PrNative git @('fetch', 'origin', 'master')
if ($fetch.ExitCode -ne 0) { Stop-WithError "git fetch origin master failed: $($fetch.Error)" }
$parse = Invoke-PrNative git @('rev-parse', 'origin/master')
if ($parse.ExitCode -ne 0) { Stop-WithError "git rev-parse origin/master failed: $($parse.Error)" }
$newBase = $parse.Output

$view = Invoke-PrNative gh @('pr', 'view', "$Number", '--repo', $Repository, '--json', 'state,headRefOid,mergedAt')
if ($view.ExitCode -ne 0) { Stop-WithError "gh pr view failed: $($view.Error)" }
try { $pr = $view.Output | ConvertFrom-Json -DateKind String } catch { $pr = $view.Output | ConvertFrom-Json }

Write-PrJson ([ordered]@{
        state        = $pr.state
        headRefOid   = $pr.headRefOid
        mergedAt     = Format-PrDate $pr.mergedAt
        headMatches  = ($pr.headRefOid -eq $Head.ToLowerInvariant())
        masterMoved  = ($newBase -ne $Base.ToLowerInvariant())
        newBase      = $newBase
    })
exit 0
