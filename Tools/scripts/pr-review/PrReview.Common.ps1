#requires -Version 7.0
<#
Shared helpers of the cerneala-pr-review-merge scripts (dot-source this file).

Pure functions (testable without gh or git; see PrReview.Tests.ps1):
  ConvertTo-PrDate, Get-PrValidationComment, Test-PrReviewAnswered, Get-PrStopReason, Get-ProbeLabel
Process helpers (call gh / git):
  Invoke-PrNative, Write-PrJson, Test-PrPathUnder, Get-PrRegisteredWorktrees
#>

Set-StrictMode -Version Latest

$script:PrAnsweredMarkerPattern = '^<!-- cerneala-pr-(?:merged|not-merged) head=(?<sha>[0-9a-fA-F]{40}) -->'

function ConvertTo-PrDate {
    <# Converts a gh date (string, DateTime or DateTimeOffset) to a DateTimeOffset; $null stays $null. #>
    param($Value)

    if ($null -eq $Value -or ($Value -is [string] -and [string]::IsNullOrWhiteSpace($Value))) { return $null }
    if ($Value -is [DateTimeOffset]) { return $Value }
    if ($Value -is [DateTime]) { return [DateTimeOffset]$Value }
    return [DateTimeOffset]::Parse(
        [string]$Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal)
}

function Format-PrDate {
    param($Value)
    $date = ConvertTo-PrDate $Value
    if ($null -eq $date) { return $null }
    return $date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
}

function Get-PrValidationComment {
    <#
    Picks the newest valid "Validated." comment that is newer than the head commit.
    A comment counts when its body, trimmed, is exactly "Validated." or starts with "Validated." followed by whitespace
    (case-sensitive). Returns $null or [pscustomobject]@{ id; url; createdAt; body; extraInstructions }.
    #>
    param(
        [AllowNull()] $Comments,
        [AllowNull()] $HeadCommittedDate
    )

    $head = ConvertTo-PrDate $HeadCommittedDate
    if ($null -eq $head) { return $null }

    $best = $null
    $bestDate = $null
    foreach ($comment in @($Comments)) {
        if ($null -eq $comment) { continue }
        $text = ([string]$comment.body).Trim()
        if ($text -cnotmatch '^Validated\.(?:\s|$)') { continue }
        $created = ConvertTo-PrDate $comment.createdAt
        if ($null -eq $created -or $created -le $head) { continue }
        if ($null -eq $best -or $created -gt $bestDate) {
            $best = $comment
            $bestDate = $created
        }
    }
    if ($null -eq $best) { return $null }

    $trimmed = ([string]$best.body).Trim()
    return [pscustomobject]@{
        id                = $best.id
        url               = $best.url
        createdAt         = Format-PrDate $best.createdAt
        body              = [string]$best.body
        extraInstructions = $trimmed.Substring('Validated.'.Length).Trim()
    }
}

function Test-PrReviewAnswered {
    <#
    $true when a comment newer than the validation has a trimmed first line that starts with
    "<!-- cerneala-pr-merged head=<Sha> -->" or "<!-- cerneala-pr-not-merged head=<Sha> -->".
    #>
    param(
        [AllowNull()] $Comments,
        [AllowNull()] $Validation,
        [Parameter(Mandatory)][string] $Sha
    )

    if ($null -eq $Validation) { return $false }
    $validationDate = ConvertTo-PrDate $Validation.createdAt
    $shaLower = $Sha.ToLowerInvariant()
    foreach ($comment in @($Comments)) {
        if ($null -eq $comment) { continue }
        $created = ConvertTo-PrDate $comment.createdAt
        if ($null -eq $created -or $created -le $validationDate) { continue }
        $firstLine = (([string]$comment.body).Trim() -split "\r?\n", 2)[0].Trim()
        if ($firstLine -match $script:PrAnsweredMarkerPattern -and $Matches['sha'].ToLowerInvariant() -eq $shaLower) {
            return $true
        }
    }
    return $false
}

function Get-PrStopReason {
    <# First applicable reason why the PR is not reviewable, or $null. #>
    param(
        [Parameter(Mandatory)] $Pr,
        [Parameter(Mandatory)][string] $Sha,
        [AllowNull()] $Validation
    )

    if ($Pr.state -ne 'OPEN') { return "not OPEN ($($Pr.state))" }
    if ($Pr.isDraft) { return 'draft' }
    if ($Pr.baseRefName -ne 'master') { return "base is $($Pr.baseRefName), not master" }
    if ($Pr.headRefOid -ne $Sha) { return "head is $($Pr.headRefOid), not $Sha" }
    if ($null -eq $Validation) { return 'no valid Validated. comment newer than the head commit' }
    return $null
}

function Get-ProbeLabel {
    <# "merge" for a tree path ending in \merge, "master" for \master, otherwise the leaf folder name. #>
    param([Parameter(Mandatory)][string] $Tree)

    $trimmed = $Tree.TrimEnd('\', '/')
    $leaf = ($trimmed -split '[\\/]')[-1]
    if ($leaf -ieq 'merge') { return 'merge' }
    if ($leaf -ieq 'master') { return 'master' }
    return $leaf
}

function Write-PrJson {
    <# Prints exactly one JSON object to stdout. #>
    param([Parameter(Mandatory)] $Object)
    $Object | ConvertTo-Json -Depth 6
}

function Invoke-PrNative {
    <#
    Runs a native command; returns [pscustomobject]@{ ExitCode; Output (stdout text, trimmed); Error (stderr text, trimmed) }.
    Nothing reaches the script's stdout.
    #>
    param(
        [Parameter(Mandatory)][string] $Command,
        [string[]] $Arguments = @()
    )

    $errorFile = [IO.Path]::GetTempFileName()
    try {
        $stdout = & $Command @Arguments 2>$errorFile
        $exit = $LASTEXITCODE
        $stderr = Get-Content -LiteralPath $errorFile -Raw -ErrorAction SilentlyContinue
    }
    finally {
        Remove-Item -LiteralPath $errorFile -Force -ErrorAction SilentlyContinue
    }
    [pscustomobject]@{
        ExitCode = $exit
        Output   = (($stdout | Out-String)).Trim()
        Error    = if ($null -eq $stderr) { '' } else { $stderr.Trim() }
    }
}

function Test-PrPathUnder {
    <# $true when Path is strictly inside Parent (full paths, case-insensitive). #>
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $Parent
    )

    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd('\', '/') + '\'
    return $full.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase)
}

function Get-PrRegisteredWorktrees {
    <# Full paths of the worktrees git knows (run from any worktree of the repository). #>
    $result = Invoke-PrNative git @('worktree', 'list', '--porcelain')
    if ($result.ExitCode -ne 0) { throw "git worktree list failed: $($result.Error)" }
    @($result.Output -split "\r?\n" |
        Where-Object { $_ -like 'worktree *' } |
        ForEach-Object { [IO.Path]::GetFullPath($_.Substring('worktree '.Length)).TrimEnd('\', '/') })
}
