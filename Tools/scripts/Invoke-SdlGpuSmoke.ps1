[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string] $RuntimeIdentifier,

    [Parameter(Mandatory)]
    [string] $PublishedDirectory,

    [ValidateSet('single-window', 'multi-window', 'input', 'resize', 'drawing', 'rendersurface2d', 'prism', 'screenshot')]
    [string] $Mode = 'single-window',

    [string] $ArtifactDirectory = 'artifacts/ci/sdlgpu-smoke'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SdlGpuSmoke.Common.ps1')

$assetReport = Assert-SdlGpuPublishedAssets `
    -PublishedDirectory $PublishedDirectory `
    -RuntimeIdentifier $RuntimeIdentifier
$published = $assetReport.PublishedDirectory
$artifacts = [IO.Path]::GetFullPath($ArtifactDirectory)
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$log = Join-Path $artifacts "$RuntimeIdentifier-$Mode.log"

$appHostName = if ($RuntimeIdentifier.StartsWith('win-', [StringComparison]::Ordinal)) {
    'Cerneala.SdlGpuSmoke.exe'
}
else {
    'Cerneala.SdlGpuSmoke'
}
$appHost = Join-Path $published $appHostName
$arguments = @('--mode', $Mode, '--artifacts', $artifacts)
if (Test-Path -LiteralPath $appHost -PathType Leaf) {
    & $appHost @arguments 2>&1 | Tee-Object -FilePath $log
}
else {
    $managedAssembly = Join-Path $published 'Cerneala.SdlGpuSmoke.dll'
    if (-not (Test-Path -LiteralPath $managedAssembly -PathType Leaf)) {
        throw "Published SDL_GPU smoke entry point was not found in '$published'."
    }

    dotnet $managedAssembly @arguments 2>&1 | Tee-Object -FilePath $log
}

if ($LASTEXITCODE -ne 0) {
    throw "SDL_GPU smoke mode '$Mode' failed for '$RuntimeIdentifier' with exit code $LASTEXITCODE. See '$log'."
}
