[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$Version,
    [string]$OutputDirectory,
    [switch]$SkipSigning,
    [string]$SigningThumbprint = $env:CERNEALA_VSIX_SIGNING_THUMBPRINT,
    [string]$TimestampUrl = 'https://timestamp.acs.microsoft.com/'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectPath = Join-Path $repoRoot 'Cerneala.VisualStudio\Cerneala.VisualStudio.csproj'
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$project = Get-Content -LiteralPath $projectPath
    $Version = $project.Project.PropertyGroup.Version | Select-Object -First 1
    if ($Version -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?$') {
        throw 'Cerneala.VisualStudio.csproj does not declare a valid release Version.'
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\visual-studio'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Get-VisualStudioInstallation {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw 'vswhere.exe was not found.'
    }

    $installation = (& $vswhere -latest -products Microsoft.VisualStudio.Product.Community -property installationPath).Trim()
    if ([string]::IsNullOrWhiteSpace($installation)) {
        throw 'Visual Studio Community is not installed.'
    }

    return $installation
}

function Normalize-Vsix {
    param([Parameter(Mandatory)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $stableExtensionDirectory = '[installdir]\Common7\IDE\Extensions\cerneala.visualstudio'
    $items = [System.Collections.Generic.List[object]]::new()
    $inputArchive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        foreach ($entry in $inputArchive.Entries) {
            $memory = [System.IO.MemoryStream]::new()
            try {
                $stream = $entry.Open()
                try {
                    $stream.CopyTo($memory)
                }
                finally {
                    $stream.Dispose()
                }

                $bytes = $memory.ToArray()
                if ($entry.FullName -eq 'catalog.json' -or $entry.FullName -eq 'manifest.json') {
                    $document = [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
                    if ($entry.FullName -eq 'catalog.json') {
                        $vsixPackages = @($document.packages | Where-Object {
                            $_.type -eq 'Vsix' -and $_.vsixId -eq 'Cerneala.Cerneala.VisualStudio'
                        })
                        if ($vsixPackages.Count -ne 1) {
                            throw 'catalog.json does not contain exactly one Cerneala VSIX package.'
                        }

                        $vsixPackages[0].extensionDir = $stableExtensionDirectory
                    }
                    else {
                        if ($document.vsixId -ne 'Cerneala.Cerneala.VisualStudio') {
                            throw 'manifest.json does not describe the Cerneala VSIX package.'
                        }

                        $document.extensionDir = $stableExtensionDirectory
                    }

                    $json = $document | ConvertTo-Json -Depth 100 -Compress
                    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
                }

                $items.Add([pscustomobject]@{
                    Name = $entry.FullName.Replace('\', '/')
                    Bytes = $bytes
                })
            }
            finally {
                $memory.Dispose()
            }
        }
    }
    finally {
        $inputArchive.Dispose()
    }

    $normalizedPath = "$Path.normalized"
    Remove-Item -LiteralPath $normalizedPath -Force -ErrorAction SilentlyContinue
    $outputArchive = [System.IO.Compression.ZipFile]::Open(
        $normalizedPath,
        [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $timestamp = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
        foreach ($item in @($items | Sort-Object -Property Name -CaseSensitive)) {
            $entry = $outputArchive.CreateEntry(
                $item.Name,
                [System.IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $timestamp
            $stream = $entry.Open()
            try {
                $stream.Write($item.Bytes, 0, $item.Bytes.Length)
            }
            finally {
                $stream.Dispose()
            }
        }
    }
    finally {
        $outputArchive.Dispose()
    }

    Move-Item -LiteralPath $normalizedPath -Destination $Path -Force
}

function Assert-VsixContents {
    param([Parameter(Mandatory)][string]$Path)

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        $required = @(
            'Cerneala.VisualStudio.dll',
            'Cerneala.pkgdef',
            'Cerneala.VisualStudio.pkgdef',
            'Grammars/cerneala.tmLanguage.json',
            'language-configuration.json',
            'Assets/cerneala.png',
            'LICENSE',
            'THIRD-PARTY-NOTICES.txt',
            "Server/$Version/Cerneala.LanguageServer.exe",
            "Server/$Version/coreclr.dll"
        )
        foreach ($name in $required) {
            if (-not ($entries -contains $name)) {
                throw "Release VSIX is missing required entry '$name'."
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-SigningCertificateFingerprint {
    param([Parameter(Mandatory)][string]$Thumbprint)

    $normalized = $Thumbprint.Replace(' ', '').ToUpperInvariant()
    $now = Get-Date
    $certificate = Get-ChildItem -Path Cert:\CurrentUser\My,Cert:\LocalMachine\My |
        Where-Object {
            $_.Thumbprint.Replace(' ', '').ToUpperInvariant() -eq $normalized -and
            $_.HasPrivateKey -and
            $_.NotBefore -le $now -and
            $_.NotAfter -gt $now -and
            ($_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3')
        } |
        Select-Object -First 1
    if ($null -eq $certificate) {
        throw 'The requested valid Code Signing certificate with a private key was not found in the Windows certificate stores.'
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha256.ComputeHash($certificate.RawData))).Replace('-', '')
    }
    finally {
        $sha256.Dispose()
    }
}

$installation = Get-VisualStudioInstallation
$msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
$builtVsix = Join-Path $repoRoot 'Cerneala.VisualStudio\bin\Release\net472\Cerneala.VisualStudio.vsix'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

& $msbuild $projectPath /restore /t:Rebuild /nologo /v:minimal `
    /p:Configuration=Release `
    /p:Version=$Version `
    /p:ContinuousIntegrationBuild=true `
    /p:Deterministic=true `
    /p:DebugSymbols=false `
    /p:DebugType=None | Out-Host
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $builtVsix)) {
    throw 'The deterministic Release VSIX build failed.'
}

$unsignedPath = Join-Path $OutputDirectory "Cerneala.VisualStudio.$Version.unsigned.vsix"
Copy-Item -LiteralPath $builtVsix -Destination $unsignedPath -Force
Normalize-Vsix $unsignedPath
Assert-VsixContents $unsignedPath
$unsignedHash = (Get-FileHash -LiteralPath $unsignedPath -Algorithm SHA256).Hash

$signedPath = $null
$checksumPath = "$unsignedPath.sha256"
$checksumHash = $unsignedHash
if (-not $SkipSigning) {
    if ([string]::IsNullOrWhiteSpace($SigningThumbprint)) {
        throw 'Set CERNEALA_VSIX_SIGNING_THUMBPRINT to the SHA-1 thumbprint of the release Code Signing certificate.'
    }

    $certificateFingerprint = Get-SigningCertificateFingerprint $SigningThumbprint
    & dotnet tool restore --tool-manifest (Join-Path $repoRoot '.config\dotnet-tools.json') | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'The local Sign CLI tool could not be restored.'
    }

    $signedPath = Join-Path $OutputDirectory "Cerneala.VisualStudio.$Version.vsix"
    Copy-Item -LiteralPath $unsignedPath -Destination $signedPath -Force
    & dotnet tool run sign -- code certificate-store $signedPath `
        -cfp $certificateFingerprint `
        -d 'Cerneala for Visual Studio' `
        -u 'https://github.com/Chevalier12/Cerneala' `
        -fd sha256 `
        -t $TimestampUrl `
        -td sha256 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'Sign CLI failed to sign the VSIX.'
    }

    $signedArchive = [System.IO.Compression.ZipFile]::OpenRead($signedPath)
    try {
        if (-not ($signedArchive.Entries.FullName -match 'digital-signature')) {
            throw 'The signed VSIX does not contain an OPC digital signature.'
        }
    }
    finally {
        $signedArchive.Dispose()
    }

    $checksumPath = "$signedPath.sha256"
    $checksumHash = (Get-FileHash -LiteralPath $signedPath -Algorithm SHA256).Hash
}

"$checksumHash *$([System.IO.Path]::GetFileName(($signedPath ?? $unsignedPath)))" |
    Set-Content -LiteralPath $checksumPath -Encoding ASCII

[pscustomobject]@{
    Version = $Version
    UnsignedPath = $unsignedPath
    UnsignedSha256 = $unsignedHash
    SignedPath = $signedPath
    ChecksumPath = $checksumPath
    Sha256 = $checksumHash
}
