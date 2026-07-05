$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Archive-Repo.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cerneala-archive-test-" + [System.Guid]::NewGuid().ToString('N'))
$repoRoot = Join-Path $tempRoot 'repo'
$outputRoot = Join-Path $tempRoot 'archives'

function Assert-True {
    param(
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

try {
    New-Item -ItemType Directory -Path $repoRoot | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'src') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'bin/Debug/net9.0') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'obj/Debug') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot 'artifacts/archives') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot '.git/objects') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot '.roslyn-index') | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $repoRoot '.vscode') | Out-Null

    Set-Content -LiteralPath (Join-Path $repoRoot 'src/Keep.cs') -Value 'public sealed class Keep {}'
    Set-Content -LiteralPath (Join-Path $repoRoot 'README.md') -Value '# keep'
    Set-Content -LiteralPath (Join-Path $repoRoot 'bin/Debug/net9.0/Cerneala.dll') -Value 'binary'
    Set-Content -LiteralPath (Join-Path $repoRoot 'obj/Debug/Cerneala.g.cs') -Value 'generated'
    Set-Content -LiteralPath (Join-Path $repoRoot 'artifacts/archives/old.zip') -Value 'old'
    Set-Content -LiteralPath (Join-Path $repoRoot '.git/config') -Value 'git'
    Set-Content -LiteralPath (Join-Path $repoRoot '.roslyn-index/index.json') -Value '{}'
    Set-Content -LiteralPath (Join-Path $repoRoot '.vscode/settings.json') -Value '{}'
    Set-Content -LiteralPath (Join-Path $repoRoot 'local.user') -Value 'user'

    & $scriptPath -RepoRoot $repoRoot -OutputDirectory $outputRoot -ArchiveName 'repo.zip' | Out-Null

    $archivePath = Join-Path $outputRoot 'repo.zip'
    Assert-True (Test-Path -LiteralPath $archivePath) 'Expected archive was not created.'

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })

        Assert-True ($entries -contains 'src/Keep.cs') 'Expected source file was not archived.'
        Assert-True ($entries -contains 'README.md') 'Expected root document was not archived.'
        Assert-True (-not ($entries -like 'bin/*')) 'bin output was archived.'
        Assert-True (-not ($entries -like 'obj/*')) 'obj output was archived.'
        Assert-True (-not ($entries -like 'artifacts/*')) 'artifacts output was archived.'
        Assert-True (-not ($entries -like '.git/*')) '.git metadata was archived.'
        Assert-True (-not ($entries -like '.roslyn-index/*')) 'Roslyn index was archived.'
        Assert-True (-not ($entries -like '.vscode/*')) 'Editor settings were archived.'
        Assert-True (-not ($entries -contains 'local.user')) 'User-local file was archived.'
    }
    finally {
        $zip.Dispose()
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
