[CmdletBinding()]
param(
    [string]$RootSuffix = 'Stage4',
    [string]$ReportPath,
    [string]$ResultsPath,
    [switch]$ResetExperimentalInstance,
    [switch]$KeepWorkspace
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$fixtureRoot = Split-Path -Parent $PSCommandPath
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $fixtureRoot '..\..\..'))
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $repoRoot 'benchmarks\Cerneala.Benchmarks\results\2026-08-15-visual-studio-community-extension.json'
}
if ([string]::IsNullOrWhiteSpace($ResultsPath)) {
    $ResultsPath = Join-Path $repoRoot 'benchmarks\Cerneala.Benchmarks\results\2026-08-15-visual-studio-community-extension.md'
}

$workspaceRoot = Join-Path $env:TEMP ("Cerneala.VisualStudio.Stage5.{0}" -f [Guid]::NewGuid().ToString('N'))
$workspace = Join-Path $workspaceRoot 'VisualStudioConsumer'
$solutionPath = Join-Path $workspace 'VisualStudioConsumer.slnx'
$fullSolutionPath = Join-Path $workspaceRoot 'Cerneala.slnx'
$requestPath = Join-Path $workspaceRoot 'request.json'
$activityLogPath = Join-Path $workspaceRoot 'ActivityLog.xml'
$disabledActivityLogPath = Join-Path $workspaceRoot 'ActivityLog.disabled.xml'
$unavailableActivityLogPath = Join-Path $workspaceRoot 'ActivityLog.unavailable.xml'
$extensionStateReportPath = Join-Path $workspaceRoot 'extension-state.txt'
$devenvProcess = $null
$serverBackupPath = $null
$previousStage5Request = $env:CERNEALA_STAGE5_REQUEST
$previousExtensionState = $env:CERNEALA_STAGE5_EXTENSION_STATE
$previousExtensionStateReport = $env:CERNEALA_STAGE5_EXTENSION_STATE_REPORT
$previousResilience = $env:CERNEALA_STAGE5_RESILIENCE
$previousResilienceView = $env:CERNEALA_STAGE5_RESILIENCE_VIEW
$previousResilienceReport = $env:CERNEALA_STAGE5_RESILIENCE_REPORT
$previousResilienceSeconds = $env:CERNEALA_STAGE5_RESILIENCE_SECONDS
$previousNugetPackages = $env:NUGET_PACKAGES
$userNugetPackages = Join-Path $env:USERPROFILE '.nuget\packages'
if (Test-Path -LiteralPath $userNugetPackages) {
    $env:NUGET_PACKAGES = $userNugetPackages
}
$rootSuffixMutex = [System.Threading.Mutex]::new(
    $false,
    "Local\Cerneala.VisualStudio.Integration.$RootSuffix")
$ownsRootSuffixMutex = $false
$externalChecks = [Collections.Generic.List[object]]::new()

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

function Get-VisualStudioInstanceId {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $instanceId = (& $vswhere -latest -products Microsoft.VisualStudio.Product.Community -property instanceId).Trim()
    if ([string]::IsNullOrWhiteSpace($instanceId)) {
        throw 'The Visual Studio Community instance id is unavailable.'
    }
    return "18.0_$instanceId"
}

function Get-ServerProcessIds {
    return @(Get-Process -Name 'Cerneala.LanguageServer' -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
}

function Stop-HiddenVisualStudio {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process -or $Process.HasExited) {
        return
    }

    $Process.WaitForExit(30000) | Out-Null

    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force
        $Process.WaitForExit(10000) | Out-Null
    }
}

function Wait-ForReport {
    param([System.Diagnostics.Process]$Process, [int]$TimeoutSeconds = 900)

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if (Test-Path -LiteralPath $ReportPath) {
            try {
                return Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
            }
            catch {
            }
        }

        if ($Process.HasExited) {
            throw "Visual Studio exited with code $($Process.ExitCode) before the Stage 5 report was written."
        }

        Start-Sleep -Milliseconds 250
    }

    throw 'The hidden Stage 5 integration run did not finish within fifteen minutes.'
}

function Invoke-ExternalResilienceProbe {
    param(
        [string]$Name,
        [string[]]$Arguments,
        [string]$ViewPath,
        [int]$ObservationSeconds
    )

    $probeReportPath = Join-Path $workspaceRoot ("resilience-{0}.txt" -f $Name)
    Remove-Item -LiteralPath $probeReportPath -Force -ErrorAction SilentlyContinue
    $env:CERNEALA_STAGE5_RESILIENCE = $Name
    $env:CERNEALA_STAGE5_RESILIENCE_VIEW = $ViewPath
    $env:CERNEALA_STAGE5_RESILIENCE_REPORT = $probeReportPath
    $env:CERNEALA_STAGE5_RESILIENCE_SECONDS = $ObservationSeconds
    $process = Start-Process -FilePath $devenvPath -ArgumentList @(
        $Arguments + @('/Command', 'Tools.CernealaRunStage4Integration')) -WindowStyle Hidden -PassThru
    try {
        $deadline = [DateTime]::UtcNow.AddMinutes(3)
        while ([DateTime]::UtcNow -lt $deadline -and -not (Test-Path -LiteralPath $probeReportPath)) {
            if ($process.HasExited) {
                throw "$Name exited before writing the resilience report."
            }
            Start-Sleep -Milliseconds 250
        }
        if (-not (Test-Path -LiteralPath $probeReportPath)) {
            throw "$Name did not write the resilience report."
        }
        $probe = @(Get-Content -LiteralPath $probeReportPath)
        $passed = $probe.Count -ge 3 -and [bool]::Parse($probe[1])
        $externalChecks.Add([ordered]@{
            name = $Name
            passed = $passed
            details = if ($probe.Count -ge 3) { $probe[2] } else { 'Malformed resilience report.' }
        })
        if (-not $passed) {
            throw "$Name failed: $($externalChecks[$externalChecks.Count - 1].details)"
        }
    }
    finally {
        Stop-HiddenVisualStudio $process
        Remove-Item Env:CERNEALA_STAGE5_RESILIENCE -ErrorAction SilentlyContinue
        Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_VIEW -ErrorAction SilentlyContinue
        Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_REPORT -ErrorAction SilentlyContinue
        Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_SECONDS -ErrorAction SilentlyContinue
    }
}

function Invoke-ExtensionStateAction {
    param([ValidateSet('disable', 'enable')][string]$Action)

    Remove-Item -LiteralPath $extensionStateReportPath -Force -ErrorAction SilentlyContinue
    $env:CERNEALA_STAGE5_EXTENSION_STATE = $Action
    $env:CERNEALA_STAGE5_EXTENSION_STATE_REPORT = $extensionStateReportPath
    $process = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/Log', $activityLogPath, $solutionPath,
        '/Command', 'Tools.CernealaRunStage4Integration') -WindowStyle Hidden -PassThru
    try {
        $deadline = [DateTime]::UtcNow.AddMinutes(2)
        while ([DateTime]::UtcNow -lt $deadline -and -not (Test-Path -LiteralPath $extensionStateReportPath)) {
            if ($process.HasExited) {
                throw "Visual Studio exited before the extension manager completed '$Action'."
            }
            Start-Sleep -Milliseconds 250
        }
        if (-not (Test-Path -LiteralPath $extensionStateReportPath)) {
            throw "The extension manager did not complete '$Action'."
        }
        $state = Get-Content -LiteralPath $extensionStateReportPath -Raw
        if (-not $state.StartsWith("${Action}:", [StringComparison]::OrdinalIgnoreCase)) {
            throw "Unexpected extension manager result: $state"
        }
    }
    finally {
        Stop-HiddenVisualStudio $process
        Remove-Item Env:CERNEALA_STAGE5_EXTENSION_STATE -ErrorAction SilentlyContinue
        Remove-Item Env:CERNEALA_STAGE5_EXTENSION_STATE_REPORT -ErrorAction SilentlyContinue
    }
}

function Write-PerformanceMarkdown {
    param($Result)

    $culture = [Globalization.CultureInfo]::InvariantCulture
    $metricRows = foreach ($metric in $Result.metrics) {
        $budget = if ($null -eq $metric.budget) { '-' } else { ([double]$metric.budget).ToString('F3', $culture) }
        $status = if ($metric.passed) { 'GREEN' } else { 'RED' }
        "| $($metric.workspace) | $($metric.name) | $(([double]$metric.value).ToString('F3', $culture)) | $budget | $($metric.unit) | $status |"
    }
    $memoryRows = foreach ($sample in $Result.memorySamples) {
        $devenvMb = ([double]$sample.devenvPrivateBytes / 1MB).ToString('F2', $culture)
        $serverMb = ([double]$sample.serverPrivateBytes / 1MB).ToString('F2', $culture)
        "| $($sample.name) | $devenvMb | $serverMb | $($sample.serverProcessCount) |"
    }
    $externalRows = foreach ($check in $Result.externalChecks) {
        $status = if ($check.passed) { 'GREEN' } else { 'RED' }
        "| $($check.name) | $status | $($check.details) |"
    }
    $memoryGb = ([double]$Result.memoryBytes / 1GB).ToString('F2', $culture)
    $startedUtc = [DateTimeOffset]::Parse([string]$Result.startedUtc.DateTime, $culture)
    $finishedUtc = [DateTimeOffset]::Parse([string]$Result.finishedUtc.DateTime, $culture)
    $duration = ($finishedUtc - $startedUtc).TotalSeconds.ToString('F2', $culture)
    $overall = if ($Result.passed) { 'GREEN' } else { 'RED' }

    @"
# Visual Studio Community extension performance - 2026-08-15

## Environment

- Visual Studio: $($Result.hostEdition) $($Result.hostVersion)
- Installation: $($Result.visualStudioInstallation)
- Processor: $($Result.processorName) ($($Result.processorCount) logical processors)
- Memory: $memoryGb GiB
- Operating system: $($Result.operatingSystem)
- Runtime duration: $duration seconds
- Automation: hidden Experimental Instance, DTE/Visual Studio editor APIs only; no global keyboard, mouse, foreground or clipboard input.

## Budgets and measurements

| Workspace | Metric | Value | Budget | Unit | Result |
| --- | --- | ---: | ---: | --- | --- |
$($metricRows -join "`n")

Cold gates are provider activation under 100 ms CPU in `devenv`, server ready under 2,000 ms and first useful completion under 2,500 ms. The editor warm rows measure end-to-end Visual Studio presentation latency and are reported without reusing server-only budgets. The real JSON-RPC full-solution probe enforces the inherited LSP gates: completion p95 under 100 ms and diagnostics p95 under 200 ms.

## Soak memory

| Checkpoint | devenv private MiB | server private MiB | server processes |
| --- | ---: | ---: | ---: |
$($memoryRows -join "`n")

The soak ran 100 document open/close cycles and 1,000 editor changes. Plateau is evaluated over the second half with limits of 96 MiB for `devenv` and 32 MiB for the bundled server.

## Resilience

| Scenario | Result | Evidence |
| --- | --- | --- |
$($externalRows -join "`n")

The in-process matrix also covered a bounded server restart, solution close/reopen and an intentional C# build failure followed by repair. The disabled probe used Visual Studio's `IVsExtensionManager.Disable`/`Enable` API with the required restart; the unavailable-server probe temporarily removed only the isolated Experimental Instance server executable and restored it afterward.

## Result

**$overall** - $($Result.checks.Count) in-process checks and $($Result.externalChecks.Count) external resilience checks. Raw measurements are stored next to this report in `2026-08-15-visual-studio-community-extension.json`.
"@ | Set-Content -LiteralPath $ResultsPath -Encoding UTF8
}

try {
    $installation = Get-VisualStudioInstallation
    $devenvPath = Join-Path $installation 'Common7\IDE\devenv.exe'
    $msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
    $createExpInstance = Join-Path $installation 'VSSDK\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe'
    $ownsRootSuffixMutex = $rootSuffixMutex.WaitOne(0)
    if (-not $ownsRootSuffixMutex) {
        throw "Another Cerneala integration run owns /RootSuffix $RootSuffix."
    }

    if ($ResetExperimentalInstance) {
        & $createExpInstance /Reset "/VSInstance=$(Get-VisualStudioInstanceId)" "/RootSuffix=$RootSuffix"
        if ($LASTEXITCODE -ne 0) {
            throw "The isolated $RootSuffix Experimental Instance could not be reset."
        }
    }

    New-Item -ItemType Directory -Path $workspace -Force | Out-Null
    Get-ChildItem -LiteralPath $fixtureRoot -File |
        Where-Object { $_.Extension -in '.cs', '.crn', '.csproj', '.slnx' } |
        Copy-Item -Destination $workspace

    $escapedRepoRoot = [System.Security.SecurityElement]::Escape($repoRoot)
    @"
<Project>
  <PropertyGroup>
    <CernealaRepoRoot>$escapedRepoRoot</CernealaRepoRoot>
  </PropertyGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $workspace 'Directory.Build.props') -Encoding UTF8

    [xml]$fullSolution = Get-Content -LiteralPath (Join-Path $repoRoot 'Cerneala.slnx') -Raw
    foreach ($project in $fullSolution.SelectNodes('//Project')) {
        $projectPath = $project.Path
        if (-not [System.IO.Path]::IsPathRooted($projectPath)) {
            $project.Path = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $projectPath))
        }
    }
    $fullSolution.Save($fullSolutionPath)

    & dotnet build $solutionPath --nologo --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) {
        throw 'The external consumer fixture did not build before performance automation.'
    }
    & dotnet sln $fullSolutionPath list | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'The isolated Cerneala.slnx could not be parsed.'
    }

    & dotnet test (Join-Path $repoRoot 'tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj') `
        --filter 'FullyQualifiedName~FullSolutionIncrementalRequestsRespectWarmBudgets' `
        --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) {
        throw 'The real JSON-RPC full-solution warm LSP budgets failed.'
    }
    $externalChecks.Add([ordered]@{
        name = 'lsp-warm-budgets-full-solution'
        passed = $true
        details = 'real JSON-RPC: completion p95 < 100 ms; diagnostics p95 < 200 ms'
    })

    & $msbuild (Join-Path $repoRoot 'Cerneala.VisualStudio\Cerneala.VisualStudio.csproj') /t:Rebuild `
        /p:Configuration=Debug /p:DeployExtension=true /p:VSSDKTargetPlatformRegRootSuffix=$RootSuffix /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'The Cerneala VSIX could not be deployed for Stage 5.'
    }
    & $msbuild (Join-Path $repoRoot 'tests\Fixtures\VisualStudioIntegrationHost\VisualStudioIntegrationHost.csproj') /t:Rebuild `
        /p:Configuration=Debug /p:DeployExtension=true /p:VSSDKTargetPlatformRegRootSuffix=$RootSuffix /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'The Stage 5 integration host could not be deployed.'
    }

    $configurationProcess = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/UpdateConfiguration') -WindowStyle Hidden -PassThru -Wait
    if ($configurationProcess.ExitCode -ne 0) {
        throw "Visual Studio failed to update the $RootSuffix extension configuration."
    }

    Remove-Item Env:CERNEALA_STAGE5_REQUEST -ErrorAction SilentlyContinue
    Invoke-ExtensionStateAction 'enable'
    Invoke-ExtensionStateAction 'disable'
    try {
        Invoke-ExternalResilienceProbe `
            'extension-disabled-api' `
            @('/RootSuffix', $RootSuffix, '/Log', $disabledActivityLogPath, $solutionPath) `
            (Join-Path $workspace 'MainView.crn') `
            5
    }
    finally {
        Invoke-ExtensionStateAction 'enable'
        $configurationProcess = Start-Process -FilePath $devenvPath -ArgumentList @(
            '/RootSuffix', $RootSuffix, '/UpdateConfiguration') -WindowStyle Hidden -PassThru -Wait
        if ($configurationProcess.ExitCode -ne 0) {
            throw "Visual Studio failed to restore the enabled extension configuration for $RootSuffix."
        }
    }

    $processorName = (Get-CimInstance Win32_Processor | Select-Object -First 1 -ExpandProperty Name).Trim()
    $memoryBytes = [long](Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
    $devenvVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($devenvPath).ProductVersion
    Remove-Item -LiteralPath $ReportPath -Force -ErrorAction SilentlyContinue
    [ordered]@{
        fixtureSolutionPath = $solutionPath
        fixtureViewPath = Join-Path $workspace 'AuthoringView.crn'
        fixtureCodePath = Join-Path $workspace 'DashboardModels.cs'
        fullSolutionPath = $fullSolutionPath
        fullViewPath = Join-Path $repoRoot 'CernealaPresentation\PresentationWindow.crn'
        reportPath = [System.IO.Path]::GetFullPath($ReportPath)
        processStartedUtc = [DateTimeOffset]::UtcNow.ToString('O')
        processorName = $processorName
        memoryBytes = $memoryBytes
        operatingSystem = [Environment]::OSVersion.VersionString
        visualStudioInstallation = "$installation ($devenvVersion)"
    } | ConvertTo-Json | Set-Content -LiteralPath $requestPath -Encoding UTF8

    $env:CERNEALA_STAGE5_REQUEST = $requestPath
    $devenvProcess = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/Log', $activityLogPath, $solutionPath,
        '/Command', 'Tools.CernealaRunStage4Integration') -WindowStyle Hidden -PassThru
    $result = Wait-ForReport $devenvProcess
    if (-not $result.passed) {
        throw "Visual Studio Stage 5 integration failed: $($result.failure)"
    }
    Stop-HiddenVisualStudio $devenvProcess
    $devenvProcess = $null
    Remove-Item Env:CERNEALA_STAGE5_REQUEST -ErrorAction SilentlyContinue

    foreach ($serverPid in @($result.serverPids)) {
        if (Get-Process -Id $serverPid -ErrorAction SilentlyContinue) {
            throw "Cerneala language server PID $serverPid leaked after Visual Studio shutdown."
        }
    }
    $externalChecks.Add([ordered]@{
        name = 'shutdown-no-process-leak'
        passed = $true
        details = "observedServerPids=$(@($result.serverPids) -join ','); all exited"
    })

    $experimentalRoot = Get-ChildItem -LiteralPath (Join-Path $env:LOCALAPPDATA 'Microsoft\VisualStudio') -Directory |
        Where-Object { $_.Name -like "18.0_*$RootSuffix" } |
        Select-Object -First 1
    if ($null -eq $experimentalRoot) {
        throw "The $RootSuffix Experimental Instance directory was not found."
    }
    $serverPath = Get-ChildItem -LiteralPath $experimentalRoot.FullName -Filter 'Cerneala.LanguageServer.exe' -File -Recurse |
        Where-Object { $_.FullName -like '*\Extensions\*\Server\*\Cerneala.LanguageServer.exe' } |
        Select-Object -First 1 -ExpandProperty FullName
    if ([string]::IsNullOrWhiteSpace($serverPath)) {
        throw 'The deployed bundled server was not found for the unavailable-server probe.'
    }
    $resolvedServer = [System.IO.Path]::GetFullPath($serverPath)
    $resolvedExperimental = [System.IO.Path]::GetFullPath($experimentalRoot.FullName).TrimEnd('\') + '\'
    if (-not $resolvedServer.StartsWith($resolvedExperimental, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The unavailable-server target escaped the isolated Experimental Instance.'
    }
    $serverBackupPath = $resolvedServer + '.stage5-unavailable'
    Move-Item -LiteralPath $resolvedServer -Destination $serverBackupPath
    Invoke-ExternalResilienceProbe `
        'server-unavailable' `
        @('/RootSuffix', $RootSuffix, '/Log', $unavailableActivityLogPath, $solutionPath) `
        (Join-Path $workspace 'MainView.crn') `
        12
    Move-Item -LiteralPath $serverBackupPath -Destination $resolvedServer
    $serverBackupPath = $null

    $result | Add-Member -NotePropertyName externalChecks -NotePropertyValue @($externalChecks) -Force
    $result.passed = $result.passed -and (@($externalChecks | Where-Object { -not $_.passed }).Count -eq 0)
    $result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
    Write-PerformanceMarkdown $result
    $result | ConvertTo-Json -Depth 10 | Write-Host

    & dotnet build $solutionPath --nologo --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) {
        throw 'The external consumer fixture did not build after Stage 5 automation.'
    }
}
finally {
    if ($null -ne $serverBackupPath -and (Test-Path -LiteralPath $serverBackupPath)) {
        $originalServerPath = $serverBackupPath.Substring(0, $serverBackupPath.Length - '.stage5-unavailable'.Length)
        Move-Item -LiteralPath $serverBackupPath -Destination $originalServerPath -Force
    }

    if ($null -ne $devenvProcess -and -not $devenvProcess.HasExited) {
        Stop-HiddenVisualStudio $devenvProcess
    }

    if ($null -ne $previousStage5Request) {
        $env:CERNEALA_STAGE5_REQUEST = $previousStage5Request
    }
    else {
        Remove-Item Env:CERNEALA_STAGE5_REQUEST -ErrorAction SilentlyContinue
    }

    if ($null -ne $previousExtensionState) {
        $env:CERNEALA_STAGE5_EXTENSION_STATE = $previousExtensionState
    }
    else {
        Remove-Item Env:CERNEALA_STAGE5_EXTENSION_STATE -ErrorAction SilentlyContinue
    }
    if ($null -ne $previousExtensionStateReport) {
        $env:CERNEALA_STAGE5_EXTENSION_STATE_REPORT = $previousExtensionStateReport
    }
    else {
        Remove-Item Env:CERNEALA_STAGE5_EXTENSION_STATE_REPORT -ErrorAction SilentlyContinue
    }
    if ($null -ne $previousResilience) { $env:CERNEALA_STAGE5_RESILIENCE = $previousResilience }
    else { Remove-Item Env:CERNEALA_STAGE5_RESILIENCE -ErrorAction SilentlyContinue }
    if ($null -ne $previousResilienceView) { $env:CERNEALA_STAGE5_RESILIENCE_VIEW = $previousResilienceView }
    else { Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_VIEW -ErrorAction SilentlyContinue }
    if ($null -ne $previousResilienceReport) { $env:CERNEALA_STAGE5_RESILIENCE_REPORT = $previousResilienceReport }
    else { Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_REPORT -ErrorAction SilentlyContinue }
    if ($null -ne $previousResilienceSeconds) { $env:CERNEALA_STAGE5_RESILIENCE_SECONDS = $previousResilienceSeconds }
    else { Remove-Item Env:CERNEALA_STAGE5_RESILIENCE_SECONDS -ErrorAction SilentlyContinue }

    if ($null -ne $previousNugetPackages) {
        $env:NUGET_PACKAGES = $previousNugetPackages
    }
    else {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }

    if (-not $KeepWorkspace -and (Test-Path -LiteralPath $workspaceRoot)) {
        $resolvedWorkspace = [System.IO.Path]::GetFullPath($workspaceRoot)
        $resolvedTemp = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
        if ($resolvedWorkspace.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and
            ([System.IO.Path]::GetFileName($resolvedWorkspace)).StartsWith('Cerneala.VisualStudio.Stage5.', [StringComparison]::Ordinal)) {
            for ($attempt = 1; $attempt -le 20; $attempt++) {
                try {
                    [System.IO.Directory]::Delete($resolvedWorkspace, $true)
                    break
                }
                catch {
                    if ($attempt -eq 20) {
                        throw
                    }
                    Start-Sleep -Milliseconds 250
                }
            }
        }
    }

    if ($ownsRootSuffixMutex) {
        $rootSuffixMutex.ReleaseMutex()
    }
    $rootSuffixMutex.Dispose()
}
