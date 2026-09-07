[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Temporary, immutable dependency until Graphix.Native is published to a permanent feed.
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$packageDirectory = Join-Path $repositoryRoot 'artifacts/graphix-packages'
$packagePath = Join-Path $packageDirectory 'Graphix.Native.3.4.16-graphix.2.nupkg'
$expectedHash = '76CD49998C816A20D8D6B029553C3765AE7B920BE97130AC7157EE8A8E96912B'

if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    & gh run download 34020135259 --repo Chevalier12/Graphix `
        --name Graphix.Native-3.4.16-graphix.2 --dir $packageDirectory
    if ($LASTEXITCODE -ne 0) {
        throw 'Graphix download failed. Authenticate gh with Actions read access or supply the verified package directly. The temporary CI artifact expires after 30 days.'
    }
}

$actualHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
if ($actualHash -ne $expectedHash) {
    throw "Graphix package SHA256 mismatch: expected $expectedHash, found $actualHash. The existing package was not overwritten."
}

Write-Output "Verified Graphix.Native 3.4.16-graphix.2: $actualHash"
