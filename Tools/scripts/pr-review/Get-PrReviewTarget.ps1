#requires -Version 7.0
<#
.SYNOPSIS
Reads one pull request (read-only) and reports whether it is ready for cerneala-pr-review-merge.

.DESCRIPTION
Runs gh pr view and prints exactly one JSON object: number, title, state, isDraft, baseRefName, headRefName,
headRefOid, sha, headCommittedDate, body, files (paths), validation, answered, ready, stopReason.

validation is $null or { id, url, createdAt, body, extraInstructions }: the newest comment whose trimmed body is exactly
"Validated." or starts with "Validated." plus a whitespace character, created after the head commit (the commit whose oid
equals sha). extraInstructions is the trimmed text after "Validated.". "validated.", "Validated", "Validated.Escape",
"Validated: x" and "LGTM. Validated." do not count.

answered is $true when a comment newer than the validation starts (trimmed first line) with
"<!-- cerneala-pr-merged head=<sha> -->" or "<!-- cerneala-pr-not-merged head=<sha> -->".

stopReason is $null or the first of: "not OPEN (<state>)", "draft", "base is <base>, not master",
"head is <headRefOid>, not <sha>", "no valid Validated. comment newer than the head commit". ready = (stopReason is $null).

Exit code: 0 ready, 1 stopReason set, 2 gh failed (JSON { error }).

.PARAMETER Number
Pull request number.
.PARAMETER Sha
Commit the review is bound to (40 hex characters). Default: the current headRefOid.
.PARAMETER Repository
owner/name. Default Chevalier12/Cerneala.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/pr-review/Get-PrReviewTarget.ps1 -Number 402
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int] $Number,
    [ValidatePattern('^[0-9a-fA-F]{40}$')][string] $Sha,
    [string] $Repository = 'Chevalier12/Cerneala'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PrReview.Common.ps1')

$fields = 'state,isDraft,baseRefName,headRefName,headRefOid,title,body,comments,commits,files'
$view = Invoke-PrNative gh @('pr', 'view', "$Number", '--repo', $Repository, '--json', $fields)
if ($view.ExitCode -ne 0) {
    Write-PrJson ([ordered]@{ number = $Number; error = "gh pr view failed: $($view.Error)" })
    exit 2
}

try {
    $pr = $view.Output | ConvertFrom-Json -DateKind String
}
catch {
    $pr = $view.Output | ConvertFrom-Json
}

if (-not $Sha) { $Sha = $pr.headRefOid }
$Sha = $Sha.ToLowerInvariant()

$headCommit = @($pr.commits) | Where-Object { $_.oid -eq $Sha } | Select-Object -First 1
$headCommittedDate = if ($null -ne $headCommit) { $headCommit.committedDate } else { $null }

$validation = Get-PrValidationComment -Comments $pr.comments -HeadCommittedDate $headCommittedDate
$answered = Test-PrReviewAnswered -Comments $pr.comments -Validation $validation -Sha $Sha
$stopReason = Get-PrStopReason -Pr $pr -Sha $Sha -Validation $validation

Write-PrJson ([ordered]@{
        number            = $Number
        title             = $pr.title
        state             = $pr.state
        isDraft           = [bool]$pr.isDraft
        baseRefName       = $pr.baseRefName
        headRefName       = $pr.headRefName
        headRefOid        = $pr.headRefOid
        sha               = $Sha
        headCommittedDate = Format-PrDate $headCommittedDate
        body              = $pr.body
        files             = @($pr.files | ForEach-Object { $_.path })
        validation        = $validation
        answered          = $answered
        ready             = ($null -eq $stopReason)
        stopReason        = $stopReason
    })

if ($null -eq $stopReason) { exit 0 } else { exit 1 }
