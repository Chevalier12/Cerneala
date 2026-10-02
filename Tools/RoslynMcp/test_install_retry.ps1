#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string] $SourcePath)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$tokens = $null
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $root 'Tools/scripts/Install-RoslynMcp.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count -ne 0) { throw $errors }
$function = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -eq 'Normalize-CompatibilityPatch'
}, $true)
if ($null -eq $function) { throw 'Missing production comparison function' }
# Load only the pure comparison function, not installation/build operations.
. ([ScriptBlock]::Create($function.Extent.Text))
$expected = Normalize-CompatibilityPatch (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'roslyn-5.9.patch'))
$diff = (git -C $SourcePath diff --no-color --no-ext-diff --binary) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read pinned checkout diff' }
if ((Normalize-CompatibilityPatch $diff) -ne $expected) { throw 'Exact compatibility patch rejected' }
if ((Normalize-CompatibilityPatch ($diff.Replace('Version="5.9.0"', 'Version="5.8.0"'))) -eq $expected) {
    throw 'Different package versions accepted'
}
if ((Normalize-CompatibilityPatch ($diff + "`n+unrelated change")) -eq $expected) {
    throw 'Unrelated content accepted'
}
'PASS: retry comparison accepts relocated exact patch and rejects changed content.'
