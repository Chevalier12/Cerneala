Set-StrictMode -Version Latest

function Get-SdlGpuNativeAssetContract {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
        [string] $RuntimeIdentifier
    )

    if ($RuntimeIdentifier.StartsWith('win-', [StringComparison]::Ordinal)) {
        return @('SDL3.dll')
    }

    if ($RuntimeIdentifier.StartsWith('linux-', [StringComparison]::Ordinal)) {
        return @('libSDL3.so', 'libSDL3.so.0', 'libSDL3.so.0.4.14')
    }

    return @('libSDL3.0.dylib', 'libSDL3.dylib')
}

function Assert-SdlGpuPublishedAssets {
    param(
        [Parameter(Mandatory)]
        [string] $PublishedDirectory,

        [Parameter(Mandatory)]
        [ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')]
        [string] $RuntimeIdentifier
    )

    $resolvedDirectory = (Resolve-Path -LiteralPath $PublishedDirectory).Path
    $assetNames = @(Get-ChildItem -LiteralPath $resolvedDirectory -File -Recurse | ForEach-Object Name)
    $expected = @(Get-SdlGpuNativeAssetContract -RuntimeIdentifier $RuntimeIdentifier)
    $missing = @($expected | Where-Object { $_ -notin $assetNames })
    if ($missing.Count -ne 0) {
        throw "Published output '$resolvedDirectory' is missing SDL asset(s): $($missing -join ', ')."
    }

    $foreign = @()
    if ($RuntimeIdentifier.StartsWith('win-', [StringComparison]::Ordinal)) {
        $foreign = @($assetNames | Where-Object { $_ -like 'libSDL3.so*' -or $_ -like 'libSDL3*.dylib' })
    }
    elseif ($RuntimeIdentifier.StartsWith('linux-', [StringComparison]::Ordinal)) {
        $foreign = @($assetNames | Where-Object { $_ -eq 'SDL3.dll' -or $_ -like 'libSDL3*.dylib' })
    }
    else {
        $foreign = @($assetNames | Where-Object { $_ -eq 'SDL3.dll' -or $_ -like 'libSDL3.so*' })
    }

    if ($foreign.Count -ne 0) {
        throw "Published output '$resolvedDirectory' contains foreign SDL asset(s): $($foreign -join ', ')."
    }

    [pscustomobject]@{
        RuntimeIdentifier = $RuntimeIdentifier
        PublishedDirectory = $resolvedDirectory
        NativeAssets = $expected
    }
}
