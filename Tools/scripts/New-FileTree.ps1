param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$OutputPath = (Join-Path $Root "FileTree.md"),
    [string[]]$ExcludeDirectories = @(
        ".git",
        ".artifacts",
        ".codex",
        ".vs",
        ".idea",
        ".vscode",
        ".roslyn-index",
        "bin",
        "obj",
        "node_modules",
        "packages",
        "TestResults",
        "artifacts"
    ),
    [string[]]$ExcludeFiles = @(
        "FileTree.md"
    )
)

$ErrorActionPreference = "Stop"

function Get-DisplayPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $relativePath = [System.IO.Path]::GetRelativePath($RootPath, $Path)
    if ($relativePath -eq ".") {
        return "."
    }

    return $relativePath.Replace([System.IO.Path]::DirectorySeparatorChar, "/")
}

function Write-TreeEntries {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.DirectoryInfo]$Directory,

        [string]$Prefix
    )

    $children = Get-ChildItem -LiteralPath $Directory.FullName -Force |
        Where-Object {
            if ($_.PSIsContainer) {
                return $ExcludeDirectories -notcontains $_.Name
            }

            return $ExcludeFiles -notcontains $_.Name
        } |
        Sort-Object @{ Expression = { -not $_.PSIsContainer } }, Name

    for ($index = 0; $index -lt $children.Count; $index++) {
        $child = $children[$index]
        $isLast = $index -eq ($children.Count - 1)
        $branch = if ($isLast) { "+-- " } else { "|-- " }
        $nextPrefix = if ($isLast) { "    " } else { "|   " }
        $suffix = if ($child.PSIsContainer) { "/" } else { "" }

        $script:Lines.Add("$Prefix$branch$($child.Name)$suffix") | Out-Null

        if ($child.PSIsContainer) {
            Write-TreeEntries -Directory $child -Prefix "$Prefix$nextPrefix"
        }
    }
}

$rootInfo = Get-Item -LiteralPath $Root
if (-not $rootInfo.PSIsContainer) {
    throw "Root must be a directory: $Root"
}

$resolvedRoot = $rootInfo.FullName
$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutput

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$script:Lines = [System.Collections.Generic.List[string]]::new()
$script:Lines.Add("# File Tree") | Out-Null
$script:Lines.Add("") | Out-Null
$script:Lines.Add("Generated from ``$(Get-DisplayPath -Path $resolvedRoot -RootPath $resolvedRoot)``.") | Out-Null
$script:Lines.Add("") | Out-Null
$script:Lines.Add("``````text") | Out-Null
$script:Lines.Add("./") | Out-Null

Write-TreeEntries -Directory $rootInfo -Prefix ""

$script:Lines.Add("``````") | Out-Null
$script:Lines.Add("") | Out-Null

Set-Content -LiteralPath $resolvedOutput -Value $script:Lines -Encoding UTF8
Write-Output "Wrote $resolvedOutput"
