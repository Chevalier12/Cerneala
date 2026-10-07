#Requires -Version 7
<#
.SYNOPSIS
Host-side distribution check of the Timbre decoders for the six desktop RIDs.

.DESCRIPTION
Restores and publishes -Project framework-dependent for win-x64, win-arm64,
linux-x64, linux-arm64, osx-x64 and osx-arm64, then verifies in each output
that the managed decoder assemblies are present, that no native codec library
(opus, vorbis, ogg, mp3, mpg123, lame, speex, SDL_mixer) is shipped, and,
with -NoticeFile, that the third-party notice is published. This validates
packaging only; it executes nothing on the target RIDs.
#>
param(
    [Parameter(Mandatory)] [string] $Project,
    [Parameter(Mandatory)] [string] $OutputRoot,
    [string[]] $RuntimeIdentifiers = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64'),
    [string[]] $RequiredAssemblies = @('NVorbis.dll', 'Concentus.dll'),
    [string] $NoticeFile
)

$ErrorActionPreference = 'Stop'
$forbidden = '(?i)(opus|vorbis|libogg|mpg123|mp3lame|lame|speex|sdl3?_mixer)[^\\/]*\.(dll|so|dylib)$'
$report = [System.Collections.Generic.List[object]]::new()
foreach ($rid in $RuntimeIdentifiers) {
    $output = Join-Path $OutputRoot $rid
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }

    & dotnet publish $Project -c Release -r $rid --self-contained false -o $output -nologo -v:q | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $rid (exit $LASTEXITCODE)."
    }

    $files = Get-ChildItem -LiteralPath $output -Recurse -File
    $missing = @($RequiredAssemblies | Where-Object { -not ($files.Name -contains $_) })
    $native = @($files | Where-Object { $_.Name -notin $RequiredAssemblies -and $_.FullName -match $forbidden } | ForEach-Object { $_.FullName.Substring($output.Length + 1) })
    $notice = if ($NoticeFile) { [bool]($files.Name -contains (Split-Path $NoticeFile -Leaf)) } else { $null }
    $decoders = foreach ($name in $RequiredAssemblies) {
        $file = $files | Where-Object Name -eq $name | Select-Object -First 1
        if ($file) {
            $assembly = [System.Reflection.AssemblyName]::GetAssemblyName($file.FullName)
            [ordered]@{ name = $name; version = $assembly.Version.ToString(); sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
        }
    }

    $entry = [ordered]@{
        rid = $rid
        missingAssemblies = $missing
        nativeCodecFiles = $native
        noticePresent = $notice
        decoders = @($decoders)
        fileCount = $files.Count
    }
    $report.Add($entry)
    if ($missing.Count -gt 0 -or $native.Count -gt 0 -or $notice -eq $false) {
        $report | ConvertTo-Json -Depth 6 | Out-Host
        throw "Decoder asset check failed for $rid."
    }
}

$report | ConvertTo-Json -Depth 6
