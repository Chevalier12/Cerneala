[CmdletBinding()]
param(
    [string]$RootSuffix = 'Stage4',
    [string]$ReportPath,
    [switch]$SkipExtensionDeploy,
    [switch]$SkipIntegrationHostDeploy,
    [switch]$KeepWorkspace
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$fixtureRoot = Split-Path -Parent $PSCommandPath
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $fixtureRoot '..\..\..'))
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $env:TEMP 'Cerneala.VisualStudio.Stage4.report.json'
}

$workspaceRoot = Join-Path $env:TEMP ("Cerneala.VisualStudio.Stage4.{0}" -f [Guid]::NewGuid().ToString('N'))
$workspace = Join-Path $workspaceRoot 'VisualStudioConsumer'
$solutionPath = Join-Path $workspace 'VisualStudioConsumer.slnx'
$presentationRoot = Join-Path $repoRoot 'CernealaPresentation'
$presentationProjectPath = Join-Path $workspaceRoot 'CernealaPresentation.Integration.csproj'
$presentationSolutionPath = Join-Path $workspaceRoot 'CernealaPresentation.slnx'
$requestPath = Join-Path $workspaceRoot 'request.json'
$activityLogPath = Join-Path $workspaceRoot 'ActivityLog.xml'
$devenvProcess = $null
$previousNugetPackages = $env:NUGET_PACKAGES
$userNugetPackages = Join-Path $env:USERPROFILE '.nuget\packages'
if (Test-Path -LiteralPath $userNugetPackages) {
    $env:NUGET_PACKAGES = $userNugetPackages
}
$rootSuffixMutex = [System.Threading.Mutex]::new(
    $false,
    "Local\Cerneala.VisualStudio.Integration.$RootSuffix")
$ownsRootSuffixMutex = $false

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

function Wait-ForReport {
    param([System.Diagnostics.Process]$Process, [int]$TimeoutSeconds = 300)

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
            throw "Visual Studio exited with code $($Process.ExitCode) before the integration host wrote its report."
        }

        Start-Sleep -Milliseconds 250
    }

    throw 'The in-process Visual Studio integration host did not finish within five minutes.'
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public static class CernealaStage4Rot
{
    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(uint reserved, out IBindCtx context);

    public static object GetVisualStudioDte(int processId)
    {
        IBindCtx context;
        CreateBindCtx(0, out context);
        IRunningObjectTable table;
        context.GetRunningObjectTable(out table);
        IEnumMoniker enumerator;
        table.EnumRunning(out enumerator);
        IMoniker[] monikers = new IMoniker[1];
        while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
        {
            string name;
            monikers[0].GetDisplayName(context, null, out name);
            if (name.StartsWith("!VisualStudio.DTE.", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(":" + processId, StringComparison.Ordinal))
            {
                object value;
                table.GetObject(monikers[0], out value);
                return value;
            }
        }

        return null;
    }
}
'@

try {
    $installation = Get-VisualStudioInstallation
    $ownsRootSuffixMutex = $rootSuffixMutex.WaitOne(0)
    if (-not $ownsRootSuffixMutex) {
        throw "Another Cerneala integration run owns /RootSuffix $RootSuffix; refusing to disturb it."
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

    $escapedPresentationProjectPath = [System.Security.SecurityElement]::Escape($presentationProjectPath)
    $escapedPresentationRoot = [System.Security.SecurityElement]::Escape($presentationRoot)
    $escapedRuntimeProjectPath = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'Cerneala.csproj'))
    $escapedGeneratorProjectPath = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'Cerneala.SourceGen\Cerneala.SourceGen.csproj'))
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>CernealaPresentation</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$escapedPresentationRoot\**\*.cs"
             Exclude="$escapedPresentationRoot\bin\**;$escapedPresentationRoot\obj\**" />
    <ProjectReference Include="$escapedRuntimeProjectPath" />
    <ProjectReference Include="$escapedGeneratorProjectPath"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <AdditionalFiles Include="$escapedPresentationRoot\**\*.crn"
                     Exclude="$escapedPresentationRoot\bin\**;$escapedPresentationRoot\obj\**" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $presentationProjectPath -Encoding UTF8

    @"
<Solution>
  <Project Path="$escapedPresentationProjectPath" />
</Solution>
"@ | Set-Content -LiteralPath $presentationSolutionPath -Encoding UTF8

    & dotnet sln $presentationSolutionPath list
    if ($LASTEXITCODE -ne 0) {
        throw 'The isolated CernealaPresentation solution could not be loaded by the .NET solution parser.'
    }

    & dotnet build $solutionPath --nologo --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) {
        throw 'The external consumer fixture did not build before Visual Studio automation.'
    }

    $msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
    if (-not $SkipExtensionDeploy) {
        & $msbuild (Join-Path $repoRoot 'Cerneala.VisualStudio\Cerneala.VisualStudio.csproj') /t:Build `
            /p:Configuration=Debug /p:DeployExtension=true /p:VSSDKTargetPlatformRegRootSuffix=$RootSuffix /nologo /v:minimal
        if ($LASTEXITCODE -ne 0) {
            throw 'The Cerneala VSIX could not be deployed to the Experimental Instance.'
        }
    }

    if (-not $SkipIntegrationHostDeploy) {
        & $msbuild (Join-Path $repoRoot 'tests\Fixtures\VisualStudioIntegrationHost\VisualStudioIntegrationHost.csproj') /t:Build `
            /p:Configuration=Debug /p:DeployExtension=true /p:VSSDKTargetPlatformRegRootSuffix=$RootSuffix /nologo /v:minimal
        $hostDeployment = Join-Path $env:LOCALAPPDATA (
            "Microsoft\VisualStudio\18.0_fe3e4311{0}\Extensions\Cerneala Tests\Cerneala Visual Studio Integration Host\0.1.0\Cerneala.VisualStudio.IntegrationHost.dll" -f $RootSuffix)
        if ($LASTEXITCODE -ne 0 -and -not (Test-Path -LiteralPath $hostDeployment)) {
            throw 'The headless integration host VSIX could not be deployed to the Experimental Instance.'
        }
    }

    $devenvPath = Join-Path $installation 'Common7\IDE\devenv.exe'
    $configurationProcess = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/NoSigninPrompt', '/NoSplash', '/UpdateConfiguration') `
        -WindowStyle Hidden -PassThru -Wait
    if ($configurationProcess.ExitCode -ne 0) {
        throw "Visual Studio failed to update the $RootSuffix extension configuration."
    }

    Remove-Item -LiteralPath $ReportPath -Force -ErrorAction SilentlyContinue
    $presentationPaths = @(Get-ChildItem -LiteralPath $presentationRoot -Filter '*.crn' -File |
        Sort-Object -Property Name |
        ForEach-Object { $_.FullName })
    [ordered]@{
        solutionPath = $solutionPath
        mainPath = Join-Path $workspace 'MainView.crn'
        authoringPath = Join-Path $workspace 'AuthoringView.crn'
        secondaryPath = Join-Path $workspace 'SecondaryView.crn'
        modelsPath = Join-Path $workspace 'DashboardModels.cs'
        projectPath = Join-Path $workspace 'VisualStudioConsumer.csproj'
        reportPath = [System.IO.Path]::GetFullPath($ReportPath)
        presentationSolutionPath = $presentationSolutionPath
        presentationPaths = $presentationPaths
        presentationBrandMarkPath = Join-Path $presentationRoot 'BrandMark.crn'
        presentationBrandMarkCodePath = Join-Path $presentationRoot 'BrandMark.crn.cs'
        presentationOpeningCodePath = Join-Path $presentationRoot 'OpeningView.crn.cs'
        presentationWindowPath = Join-Path $presentationRoot 'PresentationWindow.crn'
        presentationMotionPath = Join-Path $presentationRoot 'MotionChapterView.crn'
    } | ConvertTo-Json | Set-Content -LiteralPath $requestPath -Encoding UTF8

    $previousRequest = $env:CERNEALA_STAGE4_REQUEST
    $env:CERNEALA_STAGE4_REQUEST = $requestPath
    $devenvProcess = Start-Process -FilePath $devenvPath -ArgumentList @(
        '/RootSuffix', $RootSuffix, '/NoSigninPrompt', '/NoSplash', '/Log', $activityLogPath) `
        -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    $dte = $null
    while ([DateTime]::UtcNow -lt $deadline -and $null -eq $dte) {
        $dte = [CernealaStage4Rot]::GetVisualStudioDte($devenvProcess.Id)
        if ($null -eq $dte) {
            Start-Sleep -Milliseconds 250
        }
    }
    if ($null -eq $dte) {
        throw 'The hidden Experimental Instance did not publish its automation API.'
    }
    $dte.Solution.Open($solutionPath)
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    while ([DateTime]::UtcNow -lt $deadline -and
        (-not $dte.Solution.IsOpen -or
            -not $dte.Solution.FullName.Equals($solutionPath, [StringComparison]::OrdinalIgnoreCase))) {
        Start-Sleep -Milliseconds 250
    }
    if (-not $dte.Solution.IsOpen -or
        -not $dte.Solution.FullName.Equals($solutionPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The hidden Experimental Instance did not open the fixture through DTE.'
    }
    $invoked = $false
    $deadline = [DateTime]::UtcNow.AddMinutes(2)
    while ([DateTime]::UtcNow -lt $deadline -and -not $invoked) {
        try {
            $command = $dte.Commands.Item('Tools.CernealaRunStage4Integration', 0)
            if ($command.IsAvailable) {
                $dte.ExecuteCommand('Tools.CernealaRunStage4Integration')
                $invoked = $true
                break
            }
        }
        catch {
        }
        Start-Sleep -Milliseconds 250
    }
    if (-not $invoked) {
        throw 'The in-process Stage 4 command did not become available.'
    }
    $result = Wait-ForReport $devenvProcess
    $result | ConvertTo-Json -Depth 8 | Write-Host
    if (-not $result.passed) {
        throw "Visual Studio integration failed: $($result.failure)"
    }

    & dotnet build $solutionPath --nologo --ignore-failed-sources -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) {
        throw 'The edited external consumer fixture did not build after Visual Studio automation.'
    }
}
finally {
    if ($null -ne $previousRequest) {
        $env:CERNEALA_STAGE4_REQUEST = $previousRequest
    }
    else {
        Remove-Item Env:CERNEALA_STAGE4_REQUEST -ErrorAction SilentlyContinue
    }

    if ($null -ne $previousNugetPackages) {
        $env:NUGET_PACKAGES = $previousNugetPackages
    }
    else {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }

    if ($null -ne $devenvProcess -and -not $devenvProcess.HasExited) {
        try {
            $dte = [CernealaStage4Rot]::GetVisualStudioDte($devenvProcess.Id)
            if ($null -ne $dte) {
                $dte.Solution.Close($false)
                $dte.ExecuteCommand('File.Exit')
                $devenvProcess.WaitForExit(30000) | Out-Null
            }
        }
        catch {
        }

        if (-not $devenvProcess.HasExited) {
            Stop-Process -Id $devenvProcess.Id -Force
            $devenvProcess.WaitForExit(10000) | Out-Null
        }
    }

    if (-not $KeepWorkspace -and (Test-Path -LiteralPath $workspaceRoot)) {
        $resolvedWorkspace = [System.IO.Path]::GetFullPath($workspaceRoot)
        $resolvedTemp = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
        if ($resolvedWorkspace.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and
            ([System.IO.Path]::GetFileName($resolvedWorkspace)).StartsWith('Cerneala.VisualStudio.Stage4.', [StringComparison]::Ordinal)) {
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
