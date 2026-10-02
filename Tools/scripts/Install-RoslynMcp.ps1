#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$toolRoot = Join-Path $repositoryRoot 'Tools/RoslynMcp'
$manifest = Get-Content -Raw -LiteralPath (Join-Path $toolRoot 'manifest.json') | ConvertFrom-Json
$patch = Join-Path $toolRoot 'roslyn-5.9.patch'
$patchHash = (Get-FileHash -LiteralPath $patch -Algorithm SHA256).Hash
$installation = Join-Path $env:LOCALAPPDATA "CernealaTools/mcpRoslyn/$($manifest.commit)-$($manifest.variant)"
$source = Join-Path $installation 'source'
$publish = Join-Path $installation 'publish'
$receiptPath = Join-Path $installation 'installation.json'

function Assert-NativeSuccess([string] $operation) {
    if ($LASTEXITCODE -ne 0) { throw "$operation failed (exit $LASTEXITCODE)." }
}

function Normalize-CompatibilityPatch([string] $text) {
    # Git may relocate a valid hunk. Compare all context/content and hunk sizes,
    # not line offsets or blob hashes; unrelated changes still fail comparison.
    $text = $text.Replace("`r`n", "`n").Trim()
    $text = ($text -split "`n" | Where-Object { -not $_.StartsWith('index ') }) -join "`n"
    return $text -replace '(?m)^@@ -\d+(,\d+)? \+\d+(,\d+)? @@', '@@ -x$1 +x$2 @@'
}

if (Test-Path -LiteralPath $receiptPath) {
    $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
    $exe = Join-Path $publish 'mcpRoslyn.exe'
    if ($receipt.commit -ne $manifest.commit -or $receipt.variant -ne $manifest.variant -or
        $receipt.patchHash -ne $patchHash -or -not (Test-Path -LiteralPath $exe) -or
        (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne $receipt.executableHash) {
        throw "Existing installation does not match the pinned receipt: $installation. Nothing overwritten."
    }
    foreach ($relative in @('BuildHost-netcore', 'BuildHost-net472')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $relative))) { throw "Missing published sidecar: $relative" }
    }
    Write-Output "Already installed: $exe"
    return
}

New-Item -ItemType Directory -Path $installation -Force | Out-Null
if (-not (Test-Path -LiteralPath $source)) {
    & git clone --depth 1 --branch $manifest.tag -- $manifest.repository $source
    Assert-NativeSuccess 'Clone'
}
$head = & git -C $source rev-parse HEAD
Assert-NativeSuccess 'Resolve commit'
if ($head -ne $manifest.commit) { throw "Refusing unexpected upstream commit: $head" }
$dirty = @(& git -C $source status --porcelain)
Assert-NativeSuccess 'Read checkout status'
if ($dirty.Count -eq 0) {
    & git -C $source apply --check -- $patch
    Assert-NativeSuccess 'Validate compatibility patch'
    & git -C $source apply -- $patch
    Assert-NativeSuccess 'Apply compatibility patch'
}
else {
    # A failed prior build may leave exactly our patch. Never reset user changes.
    $expected = Normalize-CompatibilityPatch (Get-Content -Raw -LiteralPath $patch)
    $diff = (& git -C $source diff --no-color --no-ext-diff --binary) -join "`n"
    Assert-NativeSuccess 'Read existing patch'
    $actual = Normalize-CompatibilityPatch $diff
    if ($dirty.Count -ne 1 -or $dirty[0] -ne ' M src/mcpRoslyn/mcpRoslyn.csproj' -or $actual -ne $expected) {
        throw "Unexpected existing source changes at $source. Nothing overwritten."
    }
}
[xml]$project = Get-Content -Raw -LiteralPath (Join-Path $source 'src/mcpRoslyn/mcpRoslyn.csproj')
$versions = @($project.Project.ItemGroup.PackageReference | Where-Object { $_.Include -like 'Microsoft.CodeAnalysis.*' })
if ($versions.Count -ne 2 -or @($versions | Where-Object Version -NE $manifest.roslynVersion).Count -ne 0) {
    throw 'Patched compiler versions disagree with manifest.'
}
if (Test-Path -LiteralPath $publish) { throw "Unreceipted publish directory exists: $publish. Nothing overwritten." }

$pending = Join-Path $installation ('publish-pending-' + [Guid]::NewGuid().ToString('N'))
Push-Location $repositoryRoot
try {
    # SDK selection follows the repository global.json; startup never performs this build.
    & dotnet publish (Join-Path $source 'src/mcpRoslyn/mcpRoslyn.csproj') -c Release -o $pending --nologo
    Assert-NativeSuccess 'Publish'
}
finally { Pop-Location }
foreach ($relative in @('mcpRoslyn.exe', 'BuildHost-netcore', 'BuildHost-net472')) {
    if (-not (Test-Path -LiteralPath (Join-Path $pending $relative))) { throw "Missing published asset: $relative" }
}
Move-Item -LiteralPath $pending -Destination $publish
Copy-Item -LiteralPath (Join-Path $source 'LICENSE') -Destination (Join-Path $installation 'LICENSE')
@{
    commit = $manifest.commit
    variant = $manifest.variant
    patchHash = $patchHash
    executableHash = (Get-FileHash -LiteralPath (Join-Path $publish 'mcpRoslyn.exe') -Algorithm SHA256).Hash
} | ConvertTo-Json | Set-Content -LiteralPath $receiptPath -Encoding utf8NoBOM
Write-Output "Installed: $(Join-Path $publish 'mcpRoslyn.exe')"
