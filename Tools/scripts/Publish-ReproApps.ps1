#requires -Version 7.0
<#
.SYNOPSIS
Publishes the reproduction apps of a cerneala-fix-bug fix (step 9): builds before and after, runs both in check mode, zips them, makes GIFs, and (with -Publish) writes the GitHub release and the pull-request section.

.DESCRIPTION
Run it from the fixed worktree, after the fix is committed and merged with the newest origin/master.

Local phase (always, nothing is written to GitHub):
  1. dotnet build-server shutdown, then publishes the app self-contained for win-x64 as variant "after"
     (-p:ReproVariant=after) into <WorkDir>\publish\after. Both publishes use -nodeReuse:false.
  2. Uses a detached worktree of the base commit as base tree, copies the app to
     <WorkDir>\before\Issue<Issue> (without bin and obj), and publishes it against that tree
     (-p:CernealaRoot=...) as variant "before". Which base tree depends on the folder the script runs from:
       - Normal case: a new tree in $env:TEMP\cb<Issue>, removed again at the end (cold build every time).
       - Warm case: the repository folder is named fix-slot-<n> (the issue watcher's reused fix windows under
         .claude\worktrees). The base tree is $env:TEMP\cbw-fix-slot-<n>. It is created once, never removed, and
         reused: the script runs "git checkout --detach --force <BaseCommit>" and "git clean -fd" in it (never -x,
         so bin and obj stay). Example: run from .claude\worktrees\fix-slot-1, the base tree is
         $env:TEMP\cbw-fix-slot-1; it is kept and reused, so the next fix window's before build is incremental.
         If that folder exists but is not a worktree of this repository, the script stops with an error.
  3. Runs each published Issue<Issue>.exe -Runs times with --check <report> [--frames <folder>] (frames only on the
     last run). Expected exit code: before = 1 (BUG), after = 0 (OK), on every run.
  4. Only when every run matches: zips each publish folder to <WorkDir>\Issue<Issue>-before.zip / -after.zip and,
     when -Pr is given, makes Issue<Issue>-pr<Pr>-before.gif / -after.gif with New-ReproGif.ps1.
  5. Writes <WorkDir>\summary.json and <WorkDir>\verdicts.md, prints a summary, and exits with 1 (and publishes
     nothing) when a run did not match.

With -AfterOnly (fixes that change only tests, so a before build would run identical Cerneala code) the base tree
and the before app are skipped; only the after app is published and run, and only the after zip and GIF exist.

GitHub phase (only with -Publish, which requires -Pr and -HowToCheckFile; -DryRun prints every gh write command
instead of running it, and writes the would-be PR body to <WorkDir>\pr-body.preview.md):
  - release repro-pr-<Pr>: created, or updated with --clobber and every other asset deleted; verified to hold exactly
    the zips (two, or one with -AfterOnly);
  - release repro-gifs (created once, never deleted from): the GIFs are uploaded with --clobber;
  - pull-request body: the content of -HowToCheckFile, with the tokens {{GifTable}}, {{Downloads}} and
    {{VerdictTable}} replaced, goes between <!-- repro-how-to-check:start --> and <!-- repro-how-to-check:end -->.
    An existing block is replaced; otherwise the block is inserted before the first "## User validation" line, or
    appended. The file should start with its own "## How to check" heading. Running twice gives the same body;
  - the public download URLs are checked with HEAD requests (a ranged GET when HEAD is refused), and each GIF URL
    with a ranged GET whose bytes must start with GIF87a or GIF89a (Test-ReproPublicUrl in ReproApps.Common.ps1).

.PARAMETER App
The app folder that contains Issue<Issue>.csproj, for example <scratchpad>\repro-369\Issue369.
.PARAMETER Issue
GitHub issue number (names Issue<Issue>.csproj, the zips and the GIFs).
.PARAMETER AlsoIssues
The other issues the group app checks (see New-ReproApp.ps1 -AlsoIssues). It only changes texts (release notes, release title, verdict lines, exit-code line); the app, the zips and the GIFs keep the name of -Issue. Expected exit codes stay before = 1 (every bug of the group reproduces) and after = 0; exit 4 (PARTIAL) is a mismatch like any other. Every number must be greater than 0, with no duplicates and none equal to -Issue.
.PARAMETER WorkDir
Folder for publish outputs, reports, frames, zips, GIFs and summaries. Default: the parent folder of -App.
.PARAMETER BaseCommit
Commit the before app is built against. Default: git merge-base origin/master HEAD.
.PARAMETER Runs
How many times each published exe runs; every run must give the expected exit code. Default 1.
.PARAMETER TimeoutSeconds
Per-run timeout; the process is killed and the run recorded as TIMEOUT. Default 120.
.PARAMETER Pr
Pull request number (GIF names, release name, PR edit).
.PARAMETER HowToCheckFile
Markdown file with the agent's "How to check" section, using the three tokens.
.PARAMETER Repository
owner/name of the GitHub repository. Default Chevalier12/Cerneala.
.PARAMETER Publish
Run the GitHub phase.
.PARAMETER DryRun
With -Publish: print the gh write commands and write the PR body preview, but write nothing to GitHub.
.PARAMETER AfterOnly
Test-only fix: publish and check only the after app.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/Publish-ReproApps.ps1 -App $env:TEMP/repro-369/Issue369 -Issue 369
.EXAMPLE
pwsh -NoProfile -File Tools/scripts/Publish-ReproApps.ps1 -App $env:TEMP/repro-369/Issue369 -Issue 369 -Pr 380 -HowToCheckFile $env:TEMP/repro-369/how-to-check.md -Runs 3 -Publish
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $App,
    [Parameter(Mandatory)][int] $Issue,
    [string[]] $AlsoIssues = @(),
    [string] $WorkDir,
    [string] $BaseCommit,
    [int] $Runs = 1,
    [int] $TimeoutSeconds = 120,
    [int] $Pr,
    [string] $HowToCheckFile,
    [string] $Repository = 'Chevalier12/Cerneala',
    [switch] $Publish,
    [switch] $DryRun,
    [switch] $AfterOnly
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReproApps.Common.ps1')
$startMarker = '<!-- repro-how-to-check:start -->'
$endMarker = '<!-- repro-how-to-check:end -->'

# ---------- inputs ----------
# -AlsoIssues is a string[] on purpose: with "pwsh -File", "-AlsoIssues 484,485" arrives as the single text "484,485",
# and an int[] parameter would turn it into the number 484485. Splitting on commas here makes both call styles work.
$otherIssues = [int[]]@($AlsoIssues | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ } | ForEach-Object {
    if ($_ -notmatch '^[0-9]+$' -or [int]$_ -le 0) { throw "-AlsoIssues: every issue number must be a whole number greater than 0 (got '$_')." }
    [int]$_
})
if ($otherIssues -contains $Issue) { throw "-AlsoIssues must not contain -Issue ($Issue)." }
if (@($otherIssues | Select-Object -Unique).Count -ne $otherIssues.Count) { throw "-AlsoIssues must not contain duplicates ($($otherIssues -join ', '))." }
$App = [IO.Path]::GetFullPath($App).TrimEnd('\', '/')
$csproj = Join-Path $App "Issue$Issue.csproj"
if (-not (Test-Path -LiteralPath $csproj)) { throw "No Issue$Issue.csproj in $App." }
if (-not $WorkDir) { $WorkDir = Split-Path -Parent $App }
$WorkDir = [IO.Path]::GetFullPath($WorkDir).TrimEnd('\', '/')
if ($Runs -lt 1) { throw '-Runs must be at least 1.' }
if ($Publish) {
    if ($Pr -le 0) { throw '-Publish needs -Pr.' }
    if (-not $HowToCheckFile -or -not (Test-Path -LiteralPath $HowToCheckFile)) { throw '-Publish needs an existing -HowToCheckFile.' }
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'gh is not on PATH.' }
}
if ($DryRun -and -not $Publish) { throw '-DryRun only makes sense with -Publish.' }

$repo = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or -not $repo) { throw 'Run this script from inside the fixed Cerneala worktree.' }
$repo = [IO.Path]::GetFullPath($repo).TrimEnd('\', '/')
$head = (& git -C $repo rev-parse HEAD).Trim()
if (-not $AfterOnly) {
    if (-not $BaseCommit) {
        $BaseCommit = (& git -C $repo merge-base origin/master HEAD 2>$null)
        if ($LASTEXITCODE -ne 0 -or -not $BaseCommit) { throw 'Could not compute git merge-base origin/master HEAD; pass -BaseCommit.' }
    }
    $BaseCommit = (& git -C $repo rev-parse $BaseCommit).Trim()
}
if (@(& git -C $repo status --porcelain).Count -gt 0) {
    Write-Warning 'The working tree has uncommitted changes: the after app is built from them, not from HEAD.'
}

New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null
$variants = if ($AfterOnly) { @('after') } else { @('before', 'after') }
$expectedExit = @{ before = 1; after = 0 }
$zip = @{}
$gif = @{}
foreach ($v in $variants) {
    $zip[$v] = Join-Path $WorkDir "Issue$Issue-$v.zip"
    $gif[$v] = Join-Path $WorkDir "Issue$Issue-pr$Pr-$v.gif"
}

# ---------- helpers ----------
function Clear-OwnFolder([string] $Path, [string] $ExpectedLeaf) {
    # Exact, literal path only: it must be a direct child of WorkDir (or WorkDir\publish) with the expected name.
    $full = [IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $full
    if ((Split-Path -Leaf $full) -ne $ExpectedLeaf -or ($parent -ne $WorkDir -and $parent -ne (Join-Path $WorkDir 'publish'))) {
        throw "Refusing to delete '$full'."
    }
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
}

function Invoke-DotnetPublish([string] $Project, [string[]] $ExtraArgs, [string] $Log) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & dotnet publish $Project -c Release -r win-x64 --self-contained true -nodeReuse:false @ExtraArgs *> $Log
    $code = $LASTEXITCODE
    Write-Host ("dotnet publish {0} variant: exit {1} in {2:N0} s (log {3})" -f (Split-Path -Leaf $Project), $code, $watch.Elapsed.TotalSeconds, $Log)
    if ($code -ne 0) {
        Get-Content -LiteralPath $Log | Select-String 'error' | Select-Object -Last 15 | ForEach-Object { Write-Host $_.Line }
        throw "dotnet publish failed (exit $code); see $Log."
    }
}

function Invoke-CheckRuns([string] $Variant, [string] $PublishDir) {
    $exe = Join-Path $PublishDir "Issue$Issue.exe"
    if (-not (Test-Path -LiteralPath $exe)) { throw "Missing $exe." }
    $frames = Join-Path $WorkDir "frames-$Variant"
    Clear-OwnFolder $frames "frames-$Variant"
    $results = @()
    for ($run = 1; $run -le $Runs; $run++) {
        $report = Join-Path $WorkDir "$Variant-$run.txt"
        if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }
        $exeArgs = @('--check', "`"$report`"")
        if ($run -eq $Runs) { $exeArgs += @('--frames', "`"$frames`"") }
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $process = Start-Process -FilePath $exe -ArgumentList $exeArgs -PassThru
        $exitCode = $null
        if ($process.WaitForExit($TimeoutSeconds * 1000)) {
            $exitCode = $process.ExitCode
        }
        else {
            try { $process.Kill($true) } catch { }
            $exitCode = -1
        }
        $verdict = if ($exitCode -eq -1) { 'TIMEOUT' }
        elseif (Test-Path -LiteralPath $report) { (Get-Content -LiteralPath $report -TotalCount 1) }
        else { '(no report file)' }
        Write-Host ("{0} run {1}/{2}: exit {3} (expected {4}) in {5:N1} s: {6}" -f $Variant, $run, $Runs, $(if ($exitCode -eq -1) { 'TIMEOUT' } else { $exitCode }), $expectedExit[$Variant], $watch.Elapsed.TotalSeconds, $verdict)
        $results += [pscustomobject]@{
            run      = $run
            exitCode = $exitCode
            expected = $expectedExit[$Variant]
            ok       = ($exitCode -eq $expectedExit[$Variant])
            verdict  = "$verdict"
            report   = $report
        }
    }
    $results
}

# ---------- local phase ----------
Write-Host "Issue $Issue, app $App, work dir $WorkDir"
Write-Host "head $head$(if (-not $AfterOnly) { ", base $BaseCommit" } else { ' (after only)' })"
& dotnet build-server shutdown *> $null

$publishAfter = Join-Path $WorkDir 'publish\after'
$publishBefore = Join-Path $WorkDir 'publish\before'
$logAfter = Join-Path $WorkDir 'publish-after.log'
$logBefore = Join-Path $WorkDir 'publish-before.log'

Clear-OwnFolder $publishAfter 'after'
Invoke-DotnetPublish $csproj @("-p:CernealaRoot=$repo\", '-p:ReproVariant=after', '-o', $publishAfter) $logAfter

if (-not $AfterOnly) {
    # Warm base tree: when the script runs from a reused fix-slot-<n> worktree, the base tree is $env:TEMP\cbw-fix-slot-<n>.
    # It is kept after the run, so its bin and obj folders stay and the next before build is incremental.
    $slot = Split-Path $repo -Leaf
    $warm = $slot -match '^fix-slot-\d+$'
    $baseTree = if ($warm) { Join-Path $env:TEMP "cbw-$slot" } else { Join-Path $env:TEMP "cb$Issue" }
    $beforeApp = Join-Path $WorkDir "before\Issue$Issue"
    try {
        $listed = (& git -C $repo worktree list --porcelain | Where-Object { $_ -like 'worktree *' }) -replace '^worktree ', ''
        $registered = [bool]($listed | Where-Object { ([IO.Path]::GetFullPath($_)).TrimEnd('\', '/') -ieq $baseTree })
        if ($warm) {
            if ($registered -and -not (Test-Path -LiteralPath $baseTree)) {
                # registered, but the folder is gone: forget it, then create it again below
                & git -C $repo worktree prune
                $registered = $false
            }
            if ($registered) {
                Write-Host "Reusing warm base tree $baseTree (keeps bin and obj)."
                & git -C $baseTree checkout --detach --force $BaseCommit *> $null
                if ($LASTEXITCODE -ne 0) { throw "git checkout --detach --force $BaseCommit in $baseTree failed." }
                & git -C $baseTree clean -fd *> $null
                if ($LASTEXITCODE -ne 0) { throw "git clean -fd in $baseTree failed." }
            }
            else {
                if (Test-Path -LiteralPath $baseTree) { throw "$baseTree exists and is not a worktree of this repository; remove it by hand." }
                Write-Host "Creating warm base tree $baseTree (cold build this time)."
                & git -C $repo worktree add --detach $baseTree $BaseCommit *> $null
                if ($LASTEXITCODE -ne 0) { throw "git worktree add $baseTree $BaseCommit failed." }
            }
        }
        else {
            if ($registered) {
                & git -C $repo worktree remove --force $baseTree
                & git -C $repo worktree prune
            }
            if (Test-Path -LiteralPath $baseTree) { throw "$baseTree exists and is not a worktree of this repository; remove it by hand." }
            & git -C $repo worktree add --detach $baseTree $BaseCommit *> $null
            if ($LASTEXITCODE -ne 0) { throw "git worktree add $baseTree $BaseCommit failed." }
        }

        New-Item -ItemType Directory -Force -Path $beforeApp | Out-Null
        & robocopy $App $beforeApp /E /XD bin obj /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy of the app failed (exit $LASTEXITCODE)." }
        $global:LASTEXITCODE = 0

        Clear-OwnFolder $publishBefore 'before'
        Invoke-DotnetPublish (Join-Path $beforeApp "Issue$Issue.csproj") @("-p:CernealaRoot=$baseTree\", '-p:ReproVariant=before', '-o', $publishBefore) $logBefore
    }
    finally {
        if (-not $warm) {
            & git -C $repo worktree remove --force $baseTree 2>$null
            & git -C $repo worktree prune
        }
        $global:LASTEXITCODE = 0
    }
}

$results = [ordered]@{}
foreach ($v in $variants) {
    $results[$v] = @(Invoke-CheckRuns $v (Join-Path $WorkDir "publish\$v"))
}

$mismatches = @()
foreach ($v in $variants) {
    foreach ($r in $results[$v]) {
        if (-not $r.ok) {
            $got = if ($r.exitCode -eq -1) { 'TIMEOUT' } else { "exit $($r.exitCode)" }
            $mismatches += "$v run $($r.run): $got, expected exit $($r.expected) ($($r.verdict))"
        }
    }
}

# zips and GIFs only when everything matched
if ($mismatches.Count -eq 0) {
    foreach ($v in $variants) {
        Compress-Archive -Path (Join-Path $WorkDir "publish\$v\*") -DestinationPath $zip[$v] -Force
        Write-Host ("zip: {0} ({1:N1} MB)" -f $zip[$v], ((Get-Item -LiteralPath $zip[$v]).Length / 1MB))
        if ($Pr -gt 0) {
            & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'New-ReproGif.ps1') -FramesDirectory (Join-Path $WorkDir "frames-$v") -OutputPath $gif[$v]
            if ($LASTEXITCODE -ne 0) { throw "New-ReproGif.ps1 failed for $v." }
        }
    }
}

# summary.json and verdicts.md
$commitOf = @{ before = $BaseCommit; after = $head }
$summary = [ordered]@{
    issue      = $Issue
    afterOnly  = [bool]$AfterOnly
    baseCommit = $BaseCommit
    headCommit = $head
    allMatched = ($mismatches.Count -eq 0)
    variants   = [ordered]@{}
    files      = [ordered]@{}
}
foreach ($v in $variants) {
    $summary.variants[$v] = [ordered]@{ commit = $commitOf[$v]; runs = $results[$v] }
    if ($mismatches.Count -eq 0) {
        $summary.files["$v-zip"] = $zip[$v]
        if ($Pr -gt 0) { $summary.files["$v-gif"] = $gif[$v] }
    }
}
$summaryPath = Join-Path $WorkDir 'summary.json'
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding utf8

$rows = [Collections.Generic.List[string]]::new()
$rows.Add('| Variant | Commit | Run | Exit code | Verdict |')
$rows.Add('| --- | --- | --- | --- | --- |')
foreach ($v in $variants) {
    foreach ($r in $results[$v]) {
        $code = if ($r.exitCode -eq -1) { 'TIMEOUT' } else { "$($r.exitCode)" }
        $rows.Add(('| {0} | `{1}` | {2} | {3} | {4} |' -f $v, $commitOf[$v].Substring(0, 8), $r.run, $code, ($r.verdict -replace '\|', '\|')))
    }
}
$verdictsPath = Join-Path $WorkDir 'verdicts.md'
$verdictTable = ($rows -join "`n")
[IO.File]::WriteAllText($verdictsPath, $verdictTable + "`n", [Text.UTF8Encoding]::new($false))

Write-Host ''
Write-Host $verdictTable
Write-Host "summary: $summaryPath"
Write-Host "verdicts: $verdictsPath"
if ($mismatches.Count -gt 0) {
    Write-Host ''
    Write-Host 'MISMATCH, nothing published (no zips, no GIFs):'
    $mismatches | ForEach-Object { Write-Host "  $_" }
    exit 1
}
if (-not $Publish) {
    Write-Host 'Local phase done. Nothing was written to GitHub (no -Publish).'
    exit 0
}

# ---------- GitHub phase ----------
$tag = "repro-pr-$Pr"
$releaseUrl = "https://github.com/$Repository/releases/tag/$tag"
$zipUrl = @{}
$gifUrl = @{}
foreach ($v in $variants) {
    $zipUrl[$v] = "https://github.com/$Repository/releases/download/$tag/Issue$Issue-$v.zip"
    $gifUrl[$v] = "https://github.com/$Repository/releases/download/repro-gifs/Issue$Issue-pr$Pr-$v.gif"
}

function Invoke-Gh([string[]] $GhArgs, [switch] $Write) {
    if ($Write -and $DryRun) {
        Write-Host "[dry-run] gh $($GhArgs -join ' ')"
        return
    }
    $output = & gh @GhArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh $($GhArgs -join ' ') failed (exit $LASTEXITCODE): $output" }
    $output
}

function Test-GhRelease([string] $Name) {
    & gh release view $Name --repo $Repository *> $null
    $LASTEXITCODE -eq 0
}

# 7. release repro-pr-<PR>
# Texts that name the bug: "bug #480", or for a group app "bugs #480, #484, #485" (wording of the app's verdict lines).
$isGroup = $otherIssues.Count -gt 0
$bugText = if ($isGroup) { 'bugs ' + (((@($Issue) + $otherIssues) | ForEach-Object { "#$_" }) -join ', ') } else { "bug #$Issue" }
$okText = if ($isGroup) { "$bugText do not reproduce so far." } else { "$bugText does not reproduce so far." }
$bugVerdictText = if ($isGroup) { "$bugText reproduce." } else { "$bugText reproduces." }
$exitCodeText = if ($isGroup) { '0 OK, 1 BUG, 2 NOT REACHED, 3 ERROR, 4 PARTIAL (group app).' } else { '0 OK, 1 BUG, 2 NOT REACHED, 3 ERROR.' }
$notesPath = Join-Path $WorkDir 'release-notes.md'
$notes = @"
Reproduction apps for pull request #$Pr ($bugText).

Commits
- base commit (before app): $(if ($AfterOnly) { 'none, test-only fix: there is no before app' } else { $BaseCommit })
- head commit (after app): $head

How to run
1. Unzip $(if ($AfterOnly) { 'Issue' + $Issue + '-after.zip' } else { "Issue$Issue-before.zip and Issue$Issue-after.zip" }) into folders.
2. Run Issue$Issue.exe from $(if ($AfterOnly) { 'the folder' } else { 'each folder' }). The apps are self-contained for Windows x64.
3. Do the numbered actions written in the window. The top line shows the live verdict.

Verdicts
- OK (green): $okText$(if (-not $AfterOnly) { "`n- BUG (red): $bugVerdictText Expected in the before app." })

Check mode: Issue$Issue.exe --check <report-file> [--frames <folder>]. Exit codes: $exitCodeText
"@
[IO.File]::WriteAllText($notesPath, $notes + "`n", [Text.UTF8Encoding]::new($false))

$zipFiles = @($variants | ForEach-Object { $zip[$_] })
$wantedAssets = @($variants | ForEach-Object { "Issue$Issue-$_.zip" })
if (Test-GhRelease $tag) {
    Write-Host "Release $tag exists: uploading with --clobber and removing other assets."
    Invoke-Gh (@('release', 'upload', $tag) + $zipFiles + @('--clobber', '--repo', $Repository)) -Write | Out-Null
    Invoke-Gh @('release', 'edit', $tag, '--notes-file', $notesPath, '--repo', $Repository) -Write | Out-Null
    $assets = @(Invoke-Gh @('release', 'view', $tag, '--repo', $Repository, '--json', 'assets', '--jq', '.assets[].name'))
    foreach ($name in $assets | Where-Object { $_ -and $wantedAssets -cnotcontains $_ }) {
        Invoke-Gh @('release', 'delete-asset', $tag, $name, '--yes', '--repo', $Repository) -Write | Out-Null
    }
}
else {
    Invoke-Gh (@('release', 'create', $tag) + $zipFiles + @('--repo', $Repository, '--target', $head, '--prerelease', '--title', "Reproduction apps: PR #$Pr ($bugText)", '--notes-file', $notesPath)) -Write | Out-Null
}
if ($DryRun) {
    Write-Host "[dry-run] would verify with: gh release view $tag --json assets that exactly $($wantedAssets -join ', ') exist."
}
else {
    $assets = @(Invoke-Gh @('release', 'view', $tag, '--repo', $Repository, '--json', 'assets', '--jq', '.assets[].name') | Sort-Object)
    if (($assets -join '|') -ne (($wantedAssets | Sort-Object) -join '|')) {
        throw "Release $tag holds [$($assets -join ', ')], expected exactly [$($wantedAssets -join ', ')]."
    }
    Write-Host "Release $tag holds exactly: $($assets -join ', ')"
}

# 8. release repro-gifs (never deleted from)
if (-not (Test-GhRelease 'repro-gifs')) {
    Invoke-Gh @('release', 'create', 'repro-gifs', '--repo', $Repository, '--target', 'master', '--prerelease', '--title', 'Reproduction GIFs', '--notes', "GIFs of the reproduction apps' check mode, shown in pull requests. Never delete this release.") -Write | Out-Null
}
Invoke-Gh (@('release', 'upload', 'repro-gifs') + @($variants | ForEach-Object { $gif[$_] }) + @('--clobber', '--repo', $Repository)) -Write | Out-Null

# 9. pull-request body
$baseShort = if ($AfterOnly) { '' } else { $BaseCommit.Substring(0, 8) }
$headShort = $head.Substring(0, 8)
if ($AfterOnly) {
    $gifTable = "| After the fix (head ``$headShort``) |`n| --- |`n| ![Issue $Issue after the fix]($($gifUrl['after'])) |"
    $downloads = "- Download: [Issue$Issue-after.zip]($($zipUrl['after']))`n- Release: $releaseUrl`n- Head commit: ``$head```n- The app is self-contained for Windows x64 (no .NET install needed).`n- Test-only fix: Cerneala runtime code is unchanged, so there is no before app."
}
else {
    $gifTable = "| Before the fix (base ``$baseShort``) | After the fix (head ``$headShort``) |`n| --- | --- |`n| ![Issue $Issue before the fix]($($gifUrl['before'])) | ![Issue $Issue after the fix]($($gifUrl['after'])) |"
    $downloads = "- Downloads: [Issue$Issue-before.zip]($($zipUrl['before'])) and [Issue$Issue-after.zip]($($zipUrl['after']))`n- Release: $releaseUrl`n- Base commit (before app): ``$BaseCommit```n- Head commit (after app): ``$head```n- The apps are self-contained for Windows x64 (no .NET install needed)."
}

$template = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $HowToCheckFile).Path)
$section = $template.Replace('{{GifTable}}', $gifTable).Replace('{{Downloads}}', $downloads).Replace('{{VerdictTable}}', $verdictTable)
foreach ($token in '{{GifTable}}', '{{Downloads}}', '{{VerdictTable}}') {
    if (-not $template.Contains($token)) { Write-Warning "$HowToCheckFile does not use $token." }
}

function Set-MarkedSection([string] $Body, [string] $Section) {
    $nl = if ($Body.Contains("`r`n")) { "`r`n" } else { "`n" }
    $block = $startMarker + $nl + (($Section.Trim() -replace "`r?`n", $nl)) + $nl + $endMarker
    $start = $Body.IndexOf($startMarker, [StringComparison]::Ordinal)
    if ($start -ge 0) {
        $end = $Body.IndexOf($endMarker, $start, [StringComparison]::Ordinal)
        if ($end -lt 0) { throw "The PR body has $startMarker but no $endMarker." }
        return $Body.Substring(0, $start) + $block + $Body.Substring($end + $endMarker.Length)
    }
    $match = [regex]::Match($Body, '(?im)^## User validation')
    if ($match.Success) {
        return $Body.Substring(0, $match.Index) + $block + $nl + $nl + $Body.Substring($match.Index)
    }
    if ($Body.Trim().Length -eq 0) { return $block + $nl }
    return $Body.TrimEnd() + $nl + $nl + $block + $nl
}

$currentBody = (Invoke-Gh @('pr', 'view', "$Pr", '--repo', $Repository, '--json', 'body', '--jq', '.body') | Out-String)
if ($null -eq $currentBody) { $currentBody = '' }
$currentBody = $currentBody.TrimEnd("`r", "`n")
$newBody = Set-MarkedSection $currentBody $section
if ($DryRun) {
    $previewPath = Join-Path $WorkDir 'pr-body.preview.md'
    [IO.File]::WriteAllText($previewPath, $newBody, [Text.UTF8Encoding]::new($false))
    Write-Host "[dry-run] gh pr edit $Pr --repo $Repository --body-file $previewPath"
    Write-Host "[dry-run] PR body preview written to $previewPath"
}
else {
    $bodyPath = Join-Path $WorkDir 'pr-body.md'
    [IO.File]::WriteAllText($bodyPath, $newBody, [Text.UTF8Encoding]::new($false))
    Invoke-Gh @('pr', 'edit', "$Pr", '--repo', $Repository, '--body-file', $bodyPath) | Out-Null
    $readBack = (Invoke-Gh @('pr', 'view', "$Pr", '--repo', $Repository, '--json', 'body', '--jq', '.body') | Out-String)
    if (-not $readBack.Contains($startMarker) -or -not $readBack.Contains($endMarker)) {
        throw "PR #$Pr body does not contain the repro-how-to-check markers after the edit."
    }
    Write-Host "PR #$Pr body updated; markers verified."
}

# 10. public URLs
if ($DryRun) {
    Write-Host '[dry-run] public URLs that would be checked:'
    foreach ($v in $variants) { Write-Host "  $($zipUrl[$v])"; Write-Host "  $($gifUrl[$v])" }
    exit 0
}
$urlProblems = @()
foreach ($v in $variants) {
    if (-not (Test-ReproPublicUrl -Url $zipUrl[$v])) { $urlProblems += $zipUrl[$v] }
    if (-not (Test-ReproPublicUrl -Url $gifUrl[$v] -Gif)) { $urlProblems += $gifUrl[$v] }
}
if ($urlProblems.Count -gt 0) { Write-Host "URL check failed for: $($urlProblems -join ', ')"; exit 1 }
Write-Host 'All public URLs answered.'
exit 0
