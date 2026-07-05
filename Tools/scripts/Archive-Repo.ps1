[CmdletBinding()]
param(
    [string] $RepoRoot = '',
    [string] $OutputDirectory = '',
    [string] $ArchiveName = ''
)

$ErrorActionPreference = 'Stop'

$excludedDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(
    '.git',
    '.roslyn-index',
    '.vs',
    '.idea',
    '.vscode',
    '.artifacts',
    'artifacts',
    'bin',
    'obj',
    'TestResults'
) | ForEach-Object { [void] $excludedDirectories.Add($_) }

$excludedExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(
    '.user',
    '.suo',
    '.log',
    '.zip'
) | ForEach-Object { [void] $excludedExtensions.Add($_) }

function Test-IsExcludedPath {
    param(
        [string] $RelativePath,
        [bool] $IsDirectory
    )

    $segments = $RelativePath -split '[\\/]+' | Where-Object { $_ -ne '' }
    foreach ($segment in $segments) {
        if ($excludedDirectories.Contains($segment)) {
            return $true
        }
    }

    if (-not $IsDirectory) {
        $extension = [System.IO.Path]::GetExtension($RelativePath)
        if ($excludedExtensions.Contains($extension)) {
            return $true
        }
    }

    return $false
}

function Get-RelativePath {
    param(
        [string] $Root,
        [string] $Path
    )

    $rootPath = $Root.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $rootUri = [System.Uri]::new($rootPath)
    $pathUri = [System.Uri]::new($Path)
    $relativeUri = $rootUri.MakeRelativeUri($pathUri)

    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Join-Path $PSScriptRoot '../..'
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $resolvedRepoRoot 'artifacts/archives'
}

if ([string]::IsNullOrWhiteSpace($ArchiveName)) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $ArchiveName = "Cerneala-repo-$timestamp.zip"
}

if ([System.IO.Path]::GetExtension($ArchiveName) -ne '.zip') {
    $ArchiveName = "$ArchiveName.zip"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$archivePath = Join-Path $OutputDirectory $ArchiveName

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$files = Get-ChildItem -LiteralPath $resolvedRepoRoot -Recurse -File -Force |
    Where-Object {
        $relativePath = Get-RelativePath -Root $resolvedRepoRoot -Path $_.FullName
        -not (Test-IsExcludedPath -RelativePath $relativePath -IsDirectory:$false)
    } |
    Sort-Object FullName

if ($files.Count -eq 0) {
    throw "No files matched archive rules under '$resolvedRepoRoot'."
}

$zip = [System.IO.Compression.ZipFile]::Open($archivePath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relativePath = Get-RelativePath -Root $resolvedRepoRoot -Path $file.FullName
        $entryName = $relativePath.Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $entryName) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

Write-Output $archivePath
