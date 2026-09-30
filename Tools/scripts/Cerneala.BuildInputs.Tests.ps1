$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$temporaryBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$fixtureName = 'cerneala-build-input-test-' + [System.Guid]::NewGuid().ToString('N')
$fixtureRoot = Join-Path $temporaryBase $fixtureName

try {
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'Cerneala.csproj') -Destination $fixtureRoot

    # Evaluate the actual project definition against both legitimate source and archived inputs.
    $fixtureFiles = @(
        'UI/BuildInputProbe.cs',
        'UI/BuildInputProbe.resx',
        'UI/BuildInputProbe.txt',
        'UI/BuildInputProbe.crn',
        'artifacts/snapshot/Archived.cs',
        'artifacts/snapshot/obj/Release/net8.0/Archived.AssemblyInfo.cs',
        'artifacts/snapshot/Archived.resx',
        'artifacts/snapshot/Archived.txt',
        'artifacts/snapshot/Archived.crn')
    foreach ($relativePath in $fixtureFiles) {
        $filePath = Join-Path $fixtureRoot $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $filePath) -Force | Out-Null
        Set-Content -LiteralPath $filePath -Value ''
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @(
        'msbuild',
        (Join-Path $fixtureRoot 'Cerneala.csproj'),
        '-getItem:Compile,EmbeddedResource,None,AdditionalFiles')) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    try {
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'Could not start MSBuild project evaluation.'
        }

        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw 'MSBuild project evaluation exceeded 60 seconds.'
        }

        $output = $outputTask.GetAwaiter().GetResult()
        $errorOutput = $errorTask.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) {
            throw "MSBuild evaluation failed with exit code $($process.ExitCode):`n$output`n$errorOutput"
        }
    }
    finally {
        $process.Dispose()
    }

    $evaluation = $output | ConvertFrom-Json
    $expectedInputs = @{
        Compile = @('UI/BuildInputProbe.cs')
        EmbeddedResource = @(
            'UI/BuildInputProbe.resx',
            'Drawing/Prism/Filters/Assets/bluenoise.bin',
            'Drawing/Prism/Blend/Assets/dissolve-fastnoise-ranks.bin')
        None = @('UI/BuildInputProbe.txt')
        AdditionalFiles = @(
            'UI/BuildInputProbe.crn',
            'Cerneala.SourceGen/Prism/Catalog/prism-catalog.json')
    }
    $leaks = @()
    foreach ($itemType in 'Compile', 'EmbeddedResource', 'None', 'AdditionalFiles') {
        $identities = @($evaluation.Items.$itemType | ForEach-Object { $_.Identity.Replace('\', '/') })
        foreach ($expectedInput in $expectedInputs[$itemType]) {
            if ($identities -notcontains $expectedInput) {
                throw "Expected $itemType input is missing: $expectedInput"
            }
        }

        $archivedInputs = @($identities | Where-Object { $_.StartsWith('artifacts/', [StringComparison]::OrdinalIgnoreCase) })
        Write-Output "${itemType}: legitimate inputs preserved; archived inputs=$($archivedInputs.Count)"
        $leaks += $archivedInputs | ForEach-Object { "${itemType}: $_" }
    }
    if ($leaks.Count -ne 0) {
        throw "Archived files must not be core project inputs:`n$($leaks -join "`n")"
    }
}
finally {
    # Never delete a computed path without checking its exact parent and unique fixture name.
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixtureRoot)
    if ((Split-Path -Parent $resolvedFixture) -ne $temporaryBase.TrimEnd('\', '/') -or
        (Split-Path -Leaf $resolvedFixture) -ne $fixtureName) {
        throw "Refusing to remove unexpected fixture path: $resolvedFixture"
    }
    if (Test-Path -LiteralPath $resolvedFixture) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
