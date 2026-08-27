[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
    [string] $RuntimeIdentifier,

    [string] $OutputRoot = 'artifacts/publish/sdlgpu-smoke',

    [switch] $SelfContained
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'SdlGpuSmoke.Common.ps1')

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repositoryRoot 'tests\Cerneala.SdlGpuSmoke\Cerneala.SdlGpuSmoke.csproj'
$root = if ([IO.Path]::IsPathRooted($OutputRoot)) {
    [IO.Path]::GetFullPath($OutputRoot)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputRoot))
}
$output = Join-Path $root $RuntimeIdentifier

New-Item -ItemType Directory -Path $output -Force | Out-Null
dotnet publish $project `
    -c Release `
    -r $RuntimeIdentifier `
    --self-contained:$($SelfContained.IsPresent.ToString().ToLowerInvariant()) `
    -o $output
if ($LASTEXITCODE -ne 0) {
    throw "SDL_GPU smoke publish failed for '$RuntimeIdentifier' with exit code $LASTEXITCODE."
}

Assert-SdlGpuPublishedAssets `
    -PublishedDirectory $output `
    -RuntimeIdentifier $RuntimeIdentifier
