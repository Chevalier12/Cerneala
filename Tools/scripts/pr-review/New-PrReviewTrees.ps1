#requires -Version 7.0
<#
.SYNOPSIS
Creates (or refreshes, or removes) the three trees of a PR review: session tree on the PR head, a master tree, and a merge tree with the merge of master left in progress.

.DESCRIPTION
Run it from the session worktree (the current directory must be a linked worktree of the Cerneala repository).
Never run it in the main checkout: it checks out the PR head there.

Default mode:
  (a) gh pr checkout <Number> --detach in the current directory; HEAD must equal -Sha, else exit 1 with an error field;
  (b) git fetch origin master; base = origin/master;
  (c) <Root>\pr<Number>\master (detached at base) and <Root>\pr<Number>\merge (detached at Sha) are created; an existing
      registered worktree at either path is removed first (an unregistered leftover folder is deleted);
  (d) in the merge tree: git merge --no-commit --no-ff <base>; the merge stays in progress, nothing is committed;
  (e) the conflicted files (git diff --name-only --diff-filter=U) and, per file, the number of conflict hunks
      (lines that start with "<<<<<<< ").
-Refresh (master moved during the review): fetch again; in the merge tree git merge --abort when MERGE_HEAD exists, then
  git checkout --force --detach <Sha> (drops a local resolution commit, which the caller redoes), then the same merge with the
  new base; the master tree moves to the new base.
-Remove: removes both trees (git worktree remove --force; on failure the folder is emptied with robocopy /MIR and deleted,
  then git worktree prune). Only paths under <Root>\pr<Number> are touched. JSON: { removed: [paths] }.

JSON (default and -Refresh): number, sha, base, sessionTree, mergeTree, masterTree, conflicted, conflicts [{path, hunks}],
masterCommits (git rev-list --count <merge-base sha base>..<base>). Errors: JSON { number, error }, exit 1.

.PARAMETER Number
Pull request number.
.PARAMETER Sha
PR head commit (40 hex characters), from Get-PrReviewTarget.ps1.
.PARAMETER Root
Short parent folder for the trees (long scratchpad paths cause "Filename too long"). Default $env:TEMP\crn.
.PARAMETER Refresh
Re-merge the newest origin/master into the merge tree and move the master tree.
.PARAMETER Remove
Remove the merge and master trees.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/pr-review/New-PrReviewTrees.ps1 -Number 402 -Sha 73de8e08664c6094b17b6f4e9042e6302aefab3b
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int] $Number,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string] $Sha,
    [string] $Root = (Join-Path $env:TEMP 'crn'),
    [switch] $Refresh,
    [switch] $Remove
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'PrReview.Common.ps1')

$Sha = $Sha.ToLowerInvariant()
$Root = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
$prRoot = Join-Path $Root "pr$Number"
$masterTree = Join-Path $prRoot 'master'
$mergeTree = Join-Path $prRoot 'merge'

function Stop-WithError {
    param([string] $Message)
    Write-PrJson ([ordered]@{ number = $Number; error = $Message })
    exit 1
}

function Invoke-Git {
    <# git with long paths enabled; throws on a non-zero exit unless -AllowFail. #>
    param(
        [Parameter(Mandatory)][string[]] $Arguments,
        [switch] $AllowFail
    )
    $result = Invoke-PrNative git (@('-c', 'core.longpaths=true') + $Arguments)
    if ($result.ExitCode -ne 0 -and -not $AllowFail) {
        throw "git $($Arguments -join ' ') failed (exit $($result.ExitCode)): $($result.Error) $($result.Output)".Trim()
    }
    return $result
}

function Remove-PrTree {
    <# Removes one tree under $prRoot; returns $true when something was removed. #>
    param([Parameter(Mandatory)][string] $Path)

    if (-not (Test-PrPathUnder -Path $Path -Parent $prRoot)) { throw "Refusing to touch '$Path': not under '$prRoot'." }
    $registered = Get-PrRegisteredWorktrees | Where-Object { $_ -ieq [IO.Path]::GetFullPath($Path).TrimEnd('\', '/') }
    $exists = Test-Path -LiteralPath $Path
    if (-not $registered -and -not $exists) { return $false }

    if ($registered) {
        # On failure (for example long paths) the folder is deleted below and the registration pruned.
        Invoke-Git -Arguments @('worktree', 'remove', '--force', $Path) -AllowFail | Out-Null
    }
    if (Test-Path -LiteralPath $Path) {
        # Long-path-safe deletion: mirror an empty folder over it, then delete the empty folder.
        $empty = Join-Path $Root ("empty-" + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $empty | Out-Null
        try {
            & robocopy $empty $Path /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 *> $null
            if ($LASTEXITCODE -ge 8) { throw "robocopy could not empty '$Path' (exit $LASTEXITCODE)." }
            Remove-Item -LiteralPath $Path -Force -Recurse -ErrorAction Stop
        }
        finally {
            Remove-Item -LiteralPath $empty -Force -Recurse -ErrorAction SilentlyContinue
        }
    }
    Invoke-Git -Arguments @('worktree', 'prune') -AllowFail | Out-Null
    return $true
}

function Start-PrMerge {
    <# Merges $Base into the merge tree without committing; returns the conflict list. #>
    param([Parameter(Mandatory)][string] $Base)

    $merge = Invoke-Git -Arguments @('-C', $mergeTree, 'merge', '--no-commit', '--no-ff', $Base) -AllowFail
    $conflicts = @()
    $listed = Invoke-Git -Arguments @('-C', $mergeTree, '-c', 'core.quotepath=false', 'diff', '--name-only', '--diff-filter=U')
    foreach ($path in @($listed.Output -split "\r?\n" | Where-Object { $_ })) {
        $full = Join-Path $mergeTree $path
        $hunks = 0
        if (Test-Path -LiteralPath $full -PathType Leaf) {
            $text = [IO.File]::ReadAllText($full, [Text.Encoding]::Latin1)
            $hunks = [regex]::Matches($text, '(?m)^<<<<<<< ').Count
        }
        $conflicts += [ordered]@{ path = $path; hunks = $hunks }
    }
    if ($merge.ExitCode -ne 0 -and $conflicts.Count -eq 0) {
        throw "git merge $Base failed without conflicts (exit $($merge.ExitCode)): $($merge.Error) $($merge.Output)".Trim()
    }
    return , $conflicts
}

function Write-PrTreesResult {
    param([string] $Base, $Conflicts)
    $mergeBase = (Invoke-Git -Arguments @('merge-base', $Sha, $Base)).Output
    $count = [int](Invoke-Git -Arguments @('rev-list', '--count', "$mergeBase..$Base")).Output
    Write-PrJson ([ordered]@{
            number        = $Number
            sha           = $Sha
            base          = $Base
            sessionTree   = (Get-Location).ProviderPath
            mergeTree     = $mergeTree
            masterTree    = $masterTree
            conflicted    = (@($Conflicts).Count -gt 0)
            conflicts     = @($Conflicts)
            masterCommits = $count
        })
}

try {
    if ($Remove) {
        $removed = @()
        foreach ($path in @($mergeTree, $masterTree)) {
            if (Remove-PrTree -Path $path) { $removed += $path }
        }
        if ((Test-Path -LiteralPath $prRoot) -and -not (Get-ChildItem -LiteralPath $prRoot -Force)) {
            Remove-Item -LiteralPath $prRoot -Force
        }
        Write-PrJson ([ordered]@{ removed = $removed })
        exit 0
    }

    if ($Refresh) {
        foreach ($path in @($mergeTree, $masterTree)) {
            if (-not (Test-Path -LiteralPath $path)) { Stop-WithError "-Refresh needs the existing tree '$path'." }
        }
        Invoke-Git -Arguments @('fetch', 'origin', 'master') | Out-Null
        $base = (Invoke-Git -Arguments @('rev-parse', 'origin/master')).Output
        $mergeHead = Invoke-Git -Arguments @('-C', $mergeTree, 'rev-parse', '-q', '--verify', 'MERGE_HEAD') -AllowFail
        if ($mergeHead.ExitCode -eq 0) {
            Invoke-Git -Arguments @('-C', $mergeTree, 'merge', '--abort') | Out-Null
        }
        Invoke-Git -Arguments @('-C', $mergeTree, 'checkout', '--force', '--detach', $Sha) | Out-Null
        Invoke-Git -Arguments @('-C', $masterTree, 'checkout', '--force', '--detach', $base) | Out-Null
        $conflicts = Start-PrMerge -Base $base
        Write-PrTreesResult -Base $base -Conflicts $conflicts
        exit 0
    }

    # (a) session tree on the PR head; never in the main checkout
    $gitDir = (Invoke-Git -Arguments @('rev-parse', '--path-format=absolute', '--git-dir')).Output
    $commonDir = (Invoke-Git -Arguments @('rev-parse', '--path-format=absolute', '--git-common-dir')).Output
    if ($gitDir -eq $commonDir) { Stop-WithError 'The current directory is the main checkout; run this from a linked session worktree.' }
    $checkout = Invoke-PrNative gh @('pr', 'checkout', "$Number", '--detach')
    if ($checkout.ExitCode -ne 0) { Stop-WithError "gh pr checkout failed: $($checkout.Error)" }
    $head = (Invoke-Git -Arguments @('rev-parse', 'HEAD')).Output
    if ($head -ne $Sha) { Stop-WithError "HEAD is $head after gh pr checkout, not $Sha." }

    # (b) newest master
    Invoke-Git -Arguments @('fetch', 'origin', 'master') | Out-Null
    $base = (Invoke-Git -Arguments @('rev-parse', 'origin/master')).Output

    # (c) master and merge trees
    New-Item -ItemType Directory -Force -Path $Root | Out-Null
    Invoke-Git -Arguments @('worktree', 'prune') -AllowFail | Out-Null
    foreach ($path in @($mergeTree, $masterTree)) { Remove-PrTree -Path $path | Out-Null }
    New-Item -ItemType Directory -Force -Path $prRoot | Out-Null
    Invoke-Git -Arguments @('worktree', 'add', '--detach', $masterTree, $base) | Out-Null
    Invoke-Git -Arguments @('worktree', 'add', '--detach', $mergeTree, $Sha) | Out-Null

    # (d) + (e) merge stays in progress
    $conflicts = Start-PrMerge -Base $base
    Write-PrTreesResult -Base $base -Conflicts $conflicts
    exit 0
}
catch {
    Stop-WithError $_.Exception.Message
}
