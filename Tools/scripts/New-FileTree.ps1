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
        "bin",
        "obj",
        "node_modules",
        "packages",
        "TestResults",
        "artifacts"
    ),
    [string[]]$ExcludeRelativePaths = @(
        "benchmarks/Cerneala.Benchmarks/results",
        "benchmarks/results",
        "docs/plans/evidence"
    ),
    [string[]]$ExcludeFiles = @(
        "FileTree.md"
    )
)

$ErrorActionPreference = "Stop"

# Lists the repository's own files: tracked files plus new files that .gitignore does not ignore.
# Ignored content (worktrees under .claude/worktrees, build outputs, temporary files) never appears.
function Get-RepositoryFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $previousEncoding = [Console]::OutputEncoding
    try {
        [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $output = & git -C $RootPath ls-files --cached --others --exclude-standard -z
        if ($LASTEXITCODE -ne 0) {
            throw "git ls-files failed in $RootPath (exit code $LASTEXITCODE)."
        }
    }
    finally {
        [Console]::OutputEncoding = $previousEncoding
    }

    return ($output -join "") -split "`0" | Where-Object { $_.Length -gt 0 }
}

function Test-IncludedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $segments = $RelativePath.Split("/")
    for ($index = 0; $index -lt $segments.Count - 1; $index++) {
        if ($ExcludeDirectories -contains $segments[$index]) {
            return $false
        }

        $directoryPath = $segments[0..$index] -join "/"
        if ($ExcludeRelativePaths -contains $directoryPath) {
            return $false
        }
    }

    if ($ExcludeFiles -contains $segments[-1]) {
        return $false
    }

    # A tracked file deleted from the working tree is not part of the current tree.
    return Test-Path -LiteralPath (Join-Path $resolvedRoot $RelativePath) -PathType Leaf
}

function New-TreeNode {
    return @{
        Directories = @{}
        Files = [System.Collections.Generic.List[string]]::new()
    }
}

function Add-TreePath {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Node,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $segments = $RelativePath.Split("/")
    $current = $Node
    for ($index = 0; $index -lt $segments.Count - 1; $index++) {
        $name = $segments[$index]
        if (-not $current.Directories.ContainsKey($name)) {
            $current.Directories[$name] = New-TreeNode
        }

        $current = $current.Directories[$name]
    }

    $current.Files.Add($segments[-1]) | Out-Null
}

function Write-TreeEntries {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Node,

        [string]$Prefix
    )

    $children = @(
        $Node.Directories.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{ Name = $_; IsDirectory = $true } }
        $Node.Files | Sort-Object | ForEach-Object { [pscustomobject]@{ Name = $_; IsDirectory = $false } }
    )

    for ($index = 0; $index -lt $children.Count; $index++) {
        $child = $children[$index]
        $isLast = $index -eq ($children.Count - 1)
        $branch = if ($isLast) { "+-- " } else { "|-- " }
        $nextPrefix = if ($isLast) { "    " } else { "|   " }
        $suffix = if ($child.IsDirectory) { "/" } else { "" }

        $script:Lines.Add("$Prefix$branch$($child.Name)$suffix") | Out-Null

        if ($child.IsDirectory) {
            Write-TreeEntries -Node $Node.Directories[$child.Name] -Prefix "$Prefix$nextPrefix"
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

$tree = New-TreeNode
foreach ($relativePath in Get-RepositoryFiles -RootPath $resolvedRoot) {
    if (Test-IncludedPath -RelativePath $relativePath) {
        Add-TreePath -Node $tree -RelativePath $relativePath
    }
}

$script:Lines = [System.Collections.Generic.List[string]]::new()
$script:Lines.Add("# File Tree") | Out-Null
$script:Lines.Add("") | Out-Null
$script:Lines.Add("Generated from ``.`` (repository files only: tracked files and new files not ignored by ``.gitignore``).") | Out-Null
$script:Lines.Add("") | Out-Null
$script:Lines.Add("``````text") | Out-Null
$script:Lines.Add("./") | Out-Null

Write-TreeEntries -Node $tree -Prefix ""

$script:Lines.Add("``````") | Out-Null
$script:Lines.Add("") | Out-Null

if (Test-Path -LiteralPath $resolvedOutput) {
    $existingLines = Get-Content -LiteralPath $resolvedOutput
    if (($existingLines -join "`n") -ceq ($script:Lines -join "`n")) {
        Write-Output "Unchanged $resolvedOutput"
        return
    }
}

Set-Content -LiteralPath $resolvedOutput -Value $script:Lines -Encoding UTF8
Write-Output "Wrote $resolvedOutput"
