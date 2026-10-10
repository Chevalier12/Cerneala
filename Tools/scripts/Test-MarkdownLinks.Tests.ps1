$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Test-MarkdownLinks.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cerneala-links-test-" + [System.Guid]::NewGuid().ToString('N'))
$repoRoot = Join-Path $tempRoot 'repo'
$baselinePath = Join-Path $tempRoot 'baseline.txt'

function Assert-True {
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Invoke-LinkCheck {
    param([string[]] $Arguments)

    $output = & pwsh -NoProfile -File $scriptPath -Root $repoRoot @Arguments
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = @($output)
    }
}

try {
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'docs/sub') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'docs/plans/evidence') -Force | Out-Null
    & git init --quiet $repoRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'git init failed.'
    }

    Set-Content -LiteralPath (Join-Path $repoRoot 'docs/a.md') -Value @'
# A
[ok](b.md)
[missing](nope.md)
[web](https://example.com)
[anchor](#section)
```
[fenced](fenced-missing.md)
```
[dir](sub)
[anchor-ok](b.md#x)
`[inline](inline-missing.md)`
![img](missing.png "title")
[ref]: ref-missing.md
'@
    Set-Content -LiteralPath (Join-Path $repoRoot 'docs/b.md') -Value '# B'
    Set-Content -LiteralPath (Join-Path $repoRoot 'docs/sub/c.md') -Value '[root](/docs/b.md)'
    Set-Content -LiteralPath (Join-Path $repoRoot 'docs/plans/evidence/e.md') -Value '[x](evidence-missing.md)'

    $result = Invoke-LinkCheck @()
    $text = $result.Output -join "`n"
    Assert-True ($result.ExitCode -eq 1) 'Broken links without a baseline must exit 1.'
    Assert-True ($result.Output -contains 'docs/a.md:3 -> docs/nope.md') 'Missing inline target was not reported with its line.'
    Assert-True ($result.Output -contains 'docs/a.md:12 -> docs/missing.png') 'Missing image target with title was not reported.'
    Assert-True ($result.Output -contains 'docs/a.md:13 -> docs/ref-missing.md') 'Missing reference-style target was not reported.'
    Assert-True (-not $text.Contains('example.com')) 'External link was checked.'
    Assert-True (-not $text.Contains('fenced-missing')) 'Link inside fenced code was checked.'
    Assert-True (-not $text.Contains('inline-missing')) 'Link inside inline code was checked.'
    Assert-True (-not $text.Contains('evidence-missing')) 'docs/plans/evidence was scanned.'
    Assert-True (-not $text.Contains('-> docs/b.md')) 'Existing file target or anchor link was reported.'
    Assert-True (-not $text.Contains('-> docs/sub')) 'Existing folder target was reported.'
    Assert-True ($result.Output -contains 'Scanned 3 Markdown file(s); 3 broken link(s) to 3 unique target(s).') 'Summary line is wrong.'

    $written = Invoke-LinkCheck @('-WriteBaseline', $baselinePath)
    Assert-True ($written.ExitCode -eq 0) 'Writing a baseline must exit 0.'
    Assert-True ((Get-Content -LiteralPath $baselinePath).Count -eq 3) 'Baseline must hold the 3 unique broken targets.'

    $accepted = Invoke-LinkCheck @('-Baseline', $baselinePath)
    Assert-True ($accepted.ExitCode -eq 0) 'Targets listed in the baseline must exit 0.'

    Set-Content -LiteralPath $baselinePath -Value 'docs/nope.md'
    $rejected = Invoke-LinkCheck @('-Baseline', $baselinePath)
    Assert-True ($rejected.ExitCode -eq 1) 'A broken target missing from the baseline must exit 1.'
    Assert-True ($rejected.Output -contains 'NEW docs/ref-missing.md') 'New broken target was not listed.'
    Assert-True (-not ($rejected.Output -contains 'NEW docs/nope.md')) 'Baseline target was listed as new.'

    Write-Output 'Test-MarkdownLinks tests passed.'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
