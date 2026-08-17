[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$Version = '0.1.0',
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$PreviousVersion = '0.0.9',
    [string]$RootSuffix = 'Stage6RC',
    [string]$ReportPath,
    [switch]$SkipSigning,
    [switch]$KeepWorkspace
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$fixtureRoot = Split-Path -Parent $PSCommandPath
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $fixtureRoot '..\..\..'))
$buildScript = Join-Path $repoRoot 'Tools\scripts\Build-CernealaVisualStudioRelease.ps1'
$integrationScript = Join-Path $fixtureRoot 'Invoke-CommunityIntegration.ps1'
$workspaceRoot = Join-Path $env:TEMP ("Cerneala.VisualStudio.Stage6.{0}" -f [Guid]::NewGuid().ToString('N'))
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $repoRoot 'artifacts\visual-studio\release-smoke.json'
}
$ReportPath = [System.IO.Path]::GetFullPath($ReportPath)
$releaseMutex = [System.Threading.Mutex]::new(
    $false,
    "Local\Cerneala.VisualStudio.Release.$RootSuffix")
$ownsReleaseMutex = $false
$installation = $null
$instanceId = $null
$experimentalRoot = $null
$vsixInstaller = $null
$devenvPath = $null
$createExpInstance = $null
$vsRegEdit = $null
$candidateInstalled = $false
$integrationHostInstalled = $false

function Get-VisualStudioInfo {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw 'vswhere.exe was not found.'
    }

    $json = & $vswhere -latest -products Microsoft.VisualStudio.Product.Community -format json
    if ($LASTEXITCODE -ne 0) {
        throw 'vswhere.exe could not query Visual Studio Community.'
    }

    $instances = @($json | ConvertFrom-Json)
    $instance = $instances | Select-Object -First 1
    if ($null -eq $instance -or [string]::IsNullOrWhiteSpace($instance.installationPath)) {
        throw 'Visual Studio Community is not installed.'
    }

    return $instance
}

function Invoke-HiddenProcess {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$ArgumentList,
        [switch]$AllowFailure
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in $ArgumentList) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Could not start '$FilePath'."
    }

    try {
        $process.WaitForExit()
        if (-not $AllowFailure -and $process.ExitCode -ne 0) {
            throw "'$([System.IO.Path]::GetFileName($FilePath))' exited with code $($process.ExitCode)."
        }

        return $process.ExitCode
    }
    finally {
        $process.Dispose()
    }
}

function Get-TestVisualStudioProcesses {
    $escapedSuffix = [Regex]::Escape($RootSuffix)
    return @(Get-CimInstance Win32_Process -Filter "Name = 'devenv.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $_.CommandLine -match "(?i)/RootSuffix(?:\s+|:|=)`"?$escapedSuffix(?:`"|\s|$)"
        })
}

function Assert-NoVisualStudioProcesses {
    $processes = @(Get-CimInstance Win32_Process -Filter "Name = 'devenv.exe'" -ErrorAction SilentlyContinue)
    if ($processes.Count -ne 0) {
        throw "Visual Studio must be closed before the release lifecycle test; active PID(s): $($processes.ProcessId -join ', ')."
    }
}

function Assert-NoTestVisualStudio {
    $processes = @(Get-TestVisualStudioProcesses)
    if ($processes.Count -ne 0) {
        throw "The isolated /RootSuffix $RootSuffix is already running (PID $($processes.ProcessId -join ', '))."
    }
}

function Stop-TestProcesses {
    foreach ($process in @(Get-TestVisualStudioProcesses)) {
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }

    if ($null -ne $experimentalRoot) {
        $rootPrefix = [System.IO.Path]::GetFullPath($experimentalRoot).TrimEnd('\') + '\'
        foreach ($process in @(Get-CimInstance Win32_Process -Filter "Name = 'Cerneala.LanguageServer.exe'" -ErrorAction SilentlyContinue)) {
            if (-not [string]::IsNullOrWhiteSpace($process.ExecutablePath) -and
                $process.ExecutablePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

function Reset-ExperimentalInstance {
    Assert-NoTestVisualStudio
    [void](Invoke-HiddenProcess $createExpInstance @(
        '/Reset',
        "/VSInstance=18.0_$instanceId",
        "/RootSuffix=$RootSuffix"))
    Initialize-ExperimentalProfile
}

function Initialize-ExperimentalProfile {
    $settingsDirectory = Join-Path $experimentalRoot 'Settings'
    $currentSettings = Join-Path $settingsDirectory 'CurrentSettings.vssettings'
    $generalSettings = Join-Path $installation 'Common7\IDE\Profiles\General.vssettings'
    New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null
    Copy-Item -LiteralPath $generalSettings -Destination $currentSettings -Force

    $versionParts = ([string]$visualStudio.installationVersion).Split('.')
    if ($versionParts.Length -lt 3) {
        throw "Visual Studio reported an invalid installation version '$($visualStudio.installationVersion)'."
    }

    $profileValues = @(
        @('BuildNum', 'string', "$($versionParts[0]).0.$($versionParts[2])"),
        @('Sku', 'string', "$($versionParts[0])00.0"),
        @('LastResetSettingsFile', 'string', '%vsspv_vs_install_directory%\Common7\IDE\Profiles\General.vssettings'),
        @('LastResetSettingsFile_NotEncoded', 'string', $generalSettings),
        @('AutoSaveFile', 'string', '%vsspv_vs_localappdata_dir%\settings\CurrentSettings.vssettings'),
        @('AutoSaveFile_NotEncoded', 'string', $currentSettings),
        @('AutoSaveFileIsFromFirstLaunch', 'dword', '0')
    )
    foreach ($value in $profileValues) {
        [void](Invoke-HiddenProcess $vsRegEdit @(
            'set',
            $installation,
            $RootSuffix,
            'HKCU',
            'Profile',
            $value[0],
            $value[1],
            $value[2]))
    }
}

function Update-ExperimentalConfiguration {
    Assert-NoTestVisualStudio
    $process = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/NoSigninPrompt', '/NoSplash', '/UpdateConfiguration') `
        -WindowStyle Hidden -PassThru -Wait
    try {
        if ($process.ExitCode -ne 0) {
            throw "Visual Studio failed to update the $RootSuffix configuration."
        }
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-VsixInstaller {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$LogPath,
        [switch]$AllowFailure
    )

    $allArguments = @('/quiet', "/rootSuffix:$RootSuffix", "/instanceIds:$instanceId", "/logFile:$LogPath") + $Arguments
    $exitCode = Invoke-HiddenProcess $vsixInstaller $allArguments -AllowFailure:$AllowFailure
    if (-not $AllowFailure -and $exitCode -ne 0) {
        $details = if (Test-Path -LiteralPath $LogPath) {
            (Get-Content -LiteralPath $LogPath -Tail 30) -join [Environment]::NewLine
        }
        else {
            'VSIXInstaller did not create its requested log.'
        }
        throw "VSIXInstaller failed with code $exitCode.`n$details"
    }

    return $exitCode
}

function Get-VsixIdentity {
    param([Parameter(Mandatory)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry('extension.vsixmanifest')
        if ($null -eq $entry) {
            throw "'$Path' does not contain extension.vsixmanifest."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            [xml]$manifest = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    $identity = $manifest.SelectSingleNode("//*[local-name()='Identity']")
    if ($null -eq $identity) {
        throw "'$Path' does not declare a VSIX identity."
    }

    return [pscustomobject]@{
        Id = $identity.Id
        Version = $identity.Version
    }
}

function Get-InstalledCernealaExtensions {
    if (-not (Test-Path -LiteralPath $experimentalRoot)) {
        return @()
    }

    $matches = [System.Collections.Generic.List[object]]::new()
    foreach ($manifestPath in @(Get-ChildItem -LiteralPath $experimentalRoot -Filter 'extension.vsixmanifest' -File -Recurse -ErrorAction SilentlyContinue)) {
        try {
            [xml]$manifest = Get-Content -LiteralPath $manifestPath.FullName -Raw
            $identity = $manifest.SelectSingleNode("//*[local-name()='Identity']")
            if ($null -ne $identity -and $identity.Id -eq 'Cerneala.Cerneala.VisualStudio') {
                $matches.Add([pscustomobject]@{
                    Path = $manifestPath.FullName
                    Version = [string]$identity.Version
                })
            }
        }
        catch {
        }
    }

    return @($matches)
}

function Assert-InstalledVersion {
    param([Parameter(Mandatory)][string]$ExpectedVersion)

    $installed = @(Get-InstalledCernealaExtensions)
    if ($installed.Count -eq 0) {
        throw "Cerneala $ExpectedVersion is not installed in /RootSuffix $RootSuffix."
    }
    $unexpected = @($installed | Where-Object { $_.Version -ne $ExpectedVersion })
    if ($unexpected.Count -ne 0) {
        throw "Expected only Cerneala $ExpectedVersion, but found: $($installed.Version -join ', ')."
    }

    return $installed
}

function Assert-NoCernealaResidue {
    $installed = @(Get-InstalledCernealaExtensions)
    if ($installed.Count -ne 0) {
        throw "Cerneala extension manifests remain after uninstall: $($installed.Path -join ', ')."
    }

    $rootPrefix = [System.IO.Path]::GetFullPath($experimentalRoot).TrimEnd('\') + '\'
    $servers = @(Get-CimInstance Win32_Process -Filter "Name = 'Cerneala.LanguageServer.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_.ExecutablePath) -and
            $_.ExecutablePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
        })
    if ($servers.Count -ne 0) {
        throw "Cerneala server processes remain in the test hive: $($servers.ProcessId -join ', ')."
    }
}

try {
    $ownsReleaseMutex = $releaseMutex.WaitOne(0)
    if (-not $ownsReleaseMutex) {
        throw "Another release run owns /RootSuffix $RootSuffix."
    }

    $visualStudio = Get-VisualStudioInfo
    $installation = [string]$visualStudio.installationPath
    $instanceId = [string]$visualStudio.instanceId
    $experimentalRoot = Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\18.0_$instanceId$RootSuffix"
    $vsixInstaller = Join-Path $installation 'Common7\IDE\VSIXInstaller.exe'
    $devenvPath = Join-Path $installation 'Common7\IDE\devenv.exe'
    $createExpInstance = Join-Path $installation 'VSSDK\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe'
    $vsRegEdit = Join-Path $installation 'Common7\IDE\VSRegEdit.exe'
    $msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
    foreach ($tool in $vsixInstaller, $devenvPath, $createExpInstance, $vsRegEdit, $msbuild) {
        if (-not (Test-Path -LiteralPath $tool)) {
            throw "Required Visual Studio tool '$tool' was not found."
        }
    }

    Assert-NoVisualStudioProcesses
    Assert-NoTestVisualStudio
    New-Item -ItemType Directory -Path $workspaceRoot -Force | Out-Null
    $previousOutput = Join-Path $workspaceRoot 'previous'
    $candidateOutput = Join-Path $workspaceRoot 'candidate-a'
    $determinismOutput = Join-Path $workspaceRoot 'candidate-b'

    $previousBuild = & $buildScript -Version $PreviousVersion -OutputDirectory $previousOutput -SkipSigning
    if ($SkipSigning) {
        $candidateBuild = & $buildScript -Version $Version -OutputDirectory $candidateOutput -SkipSigning
    }
    else {
        $candidateBuild = & $buildScript -Version $Version -OutputDirectory $candidateOutput
    }
    $determinismBuild = & $buildScript -Version $Version -OutputDirectory $determinismOutput -SkipSigning

    if ($candidateBuild.UnsignedSha256 -ne $determinismBuild.UnsignedSha256) {
        throw "Release VSIX is not deterministic: $($candidateBuild.UnsignedSha256) != $($determinismBuild.UnsignedSha256)."
    }

    $candidatePath = if ([string]::IsNullOrWhiteSpace($candidateBuild.SignedPath)) {
        $candidateBuild.UnsignedPath
    }
    else {
        $candidateBuild.SignedPath
    }
    $previousIdentity = Get-VsixIdentity $previousBuild.UnsignedPath
    $candidateIdentity = Get-VsixIdentity $candidatePath
    if ($previousIdentity.Id -ne 'Cerneala.Cerneala.VisualStudio' -or
        $candidateIdentity.Id -ne $previousIdentity.Id -or
        $previousIdentity.Version -ne $PreviousVersion -or
        $candidateIdentity.Version -ne $Version) {
        throw 'The release artifacts do not preserve the stable VSIX identity and requested versions.'
    }

    Reset-ExperimentalInstance
    [void](Invoke-VsixInstaller @($previousBuild.UnsignedPath) (Join-Path $workspaceRoot 'install-previous.log'))
    Update-ExperimentalConfiguration
    [void](Assert-InstalledVersion $PreviousVersion)

    [void](Invoke-VsixInstaller @($candidatePath) (Join-Path $workspaceRoot 'upgrade.log'))
    $candidateInstalled = $true
    Update-ExperimentalConfiguration
    [void](Assert-InstalledVersion $Version)

    $downgradeExitCode = Invoke-VsixInstaller @($previousBuild.UnsignedPath) (Join-Path $workspaceRoot 'downgrade.log') -AllowFailure
    Update-ExperimentalConfiguration
    [void](Assert-InstalledVersion $Version)

    $integrationHostProject = Join-Path $repoRoot 'tests\Fixtures\VisualStudioIntegrationHost\VisualStudioIntegrationHost.csproj'
    [void](Invoke-HiddenProcess $msbuild @(
        $integrationHostProject,
        '/t:Rebuild',
        '/p:Configuration=Debug',
        '/p:DeployExtension=false',
        '/nologo',
        '/v:minimal'))
    $integrationHostVsix = Join-Path $repoRoot 'tests\Fixtures\VisualStudioIntegrationHost\bin\Debug\net472\Cerneala.VisualStudio.IntegrationHost.vsix'
    [void](Invoke-VsixInstaller @($integrationHostVsix) (Join-Path $workspaceRoot 'install-integration-host.log'))
    $integrationHostInstalled = $true
    Update-ExperimentalConfiguration

    $integrationReport = Join-Path $workspaceRoot 'integration.json'
    & $integrationScript -RootSuffix $RootSuffix -ReportPath $integrationReport `
        -SkipExtensionDeploy -SkipIntegrationHostDeploy -KeepWorkspace:$KeepWorkspace
    if ($LASTEXITCODE -ne 0) {
        throw 'The hidden Visual Studio release integration matrix failed.'
    }
    $integration = Get-Content -LiteralPath $integrationReport -Raw | ConvertFrom-Json
    if (-not $integration.passed) {
        throw "The hidden Visual Studio release integration matrix failed: $($integration.failure)"
    }

    Assert-NoTestVisualStudio
    [void](Invoke-VsixInstaller @('/uninstall:Cerneala.Cerneala.VisualStudio') (Join-Path $workspaceRoot 'uninstall.log'))
    $candidateInstalled = $false
    [void](Invoke-VsixInstaller @('/uninstall:Cerneala.VisualStudio.IntegrationHost') (Join-Path $workspaceRoot 'uninstall-integration-host.log'))
    $integrationHostInstalled = $false
    Update-ExperimentalConfiguration
    Assert-NoCernealaResidue

    $report = [ordered]@{
        passed = $true
        visualStudioEdition = [string]$visualStudio.productId
        visualStudioVersion = [string]$visualStudio.installationVersion
        rootSuffix = $RootSuffix
        previousVersion = $PreviousVersion
        candidateVersion = $Version
        signed = -not [string]::IsNullOrWhiteSpace($candidateBuild.SignedPath)
        candidatePath = $candidatePath
        sha256 = $candidateBuild.Sha256
        deterministicUnsignedSha256 = $candidateBuild.UnsignedSha256
        downgradeInstallerExitCode = $downgradeExitCode
        settingsCompatibility = 'No user settings surface exists in 0.1.0; the stable identity remained enabled and functional across upgrade.'
        integrationChecks = @($integration.checks).Count
        uninstallResidue = 0
        automation = 'Hidden Experimental Instance; VSIXInstaller, CreateExpInstance, DTE and Visual Studio editor APIs only.'
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
    $report | ConvertTo-Json -Depth 6 | Write-Host
}
finally {
    Stop-TestProcesses
    if ($candidateInstalled -and $null -ne $vsixInstaller -and (Test-Path -LiteralPath $vsixInstaller)) {
        try {
            [void](Invoke-VsixInstaller @('/uninstall:Cerneala.Cerneala.VisualStudio') (Join-Path $workspaceRoot 'cleanup-uninstall.log') -AllowFailure)
        }
        catch {
        }
    }
    if ($integrationHostInstalled -and $null -ne $vsixInstaller -and (Test-Path -LiteralPath $vsixInstaller)) {
        try {
            [void](Invoke-VsixInstaller @('/uninstall:Cerneala.VisualStudio.IntegrationHost') (Join-Path $workspaceRoot 'cleanup-uninstall-integration-host.log') -AllowFailure)
        }
        catch {
        }
    }

    if ($null -ne $createExpInstance -and (Test-Path -LiteralPath $createExpInstance)) {
        try {
            Stop-TestProcesses
            Reset-ExperimentalInstance
        }
        catch {
        }
    }

    if (-not $KeepWorkspace -and (Test-Path -LiteralPath $workspaceRoot)) {
        $resolvedWorkspace = [System.IO.Path]::GetFullPath($workspaceRoot)
        $resolvedTemp = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
        if ($resolvedWorkspace.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and
            ([System.IO.Path]::GetFileName($resolvedWorkspace)).StartsWith('Cerneala.VisualStudio.Stage6.', [StringComparison]::Ordinal)) {
            [System.IO.Directory]::Delete($resolvedWorkspace, $true)
        }
    }

    if ($ownsReleaseMutex) {
        $releaseMutex.ReleaseMutex()
    }
    $releaseMutex.Dispose()
}
