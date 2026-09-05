[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $LdtkSchemaFile,
    [Parameter(Mandatory)] [string] $TiledExecutable,
    [Parameter(Mandatory)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$expectedSchemaHash = '2AA84B0DB6E5EF1B530B1F557C5802DA1AA8BC62D10184D358C346734FB84893'
$schemaPath = (Resolve-Path -LiteralPath $LdtkSchemaFile).Path
$tiledPath = (Resolve-Path -LiteralPath $TiledExecutable).Path
if ((Get-FileHash -LiteralPath $schemaPath -Algorithm SHA256).Hash -ne $expectedSchemaHash) {
    throw 'The LDtk schema differs from the audited 1.5.3 schema. Audit the change; do not silently accept it.'
}
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$results = [Collections.Generic.List[object]]::new()

foreach ($name in @('ldtk-inline.ldtk', 'ldtk-separate.ldtk', 'invalid/ldtk-field.ldtk')) {
    $fixturePath = Join-Path $PSScriptRoot $name
    if (-not (Test-Json -LiteralPath $fixturePath -SchemaFile $schemaPath)) { throw "Schema validation failed: $name" }
    $results.Add([ordered]@{ file = $name; check = 'official-ldtk-1.5.3-schema'; passed = $true; sha256 = (Get-FileHash -LiteralPath $fixturePath).Hash })
}
$levelSchema = Get-Content -Raw -LiteralPath $schemaPath | ConvertFrom-Json -AsHashtable
$levelSchema['$ref'] = '#/otherTypes/Level'
$externalLevel = Join-Path $PSScriptRoot 'levels/Fixture.ldtkl'
$level = Get-Content -Raw -LiteralPath $externalLevel | ConvertFrom-Json -AsHashtable
# Official SeparateLevelFiles samples include __header__, although the Level schema
# has additionalProperties:false and does not define this transport header.
if ($level.__header__.app -ne 'LDtk' -or $level.__header__.appVersion -ne '1.5.3' -or $level.__header__.fileType -ne 'LDtk Project JSON') { throw 'External level header is invalid.' }
$level.Remove('__header__') | Out-Null
if (-not (Test-Json -Json ($level | ConvertTo-Json -Depth 100) -Schema ($levelSchema | ConvertTo-Json -Depth 100))) { throw 'External level payload schema validation failed.' }
$results.Add([ordered]@{ file = 'levels/Fixture.ldtkl'; check = 'official-ldtk-1.5.3-Level-payload-and-separate-header'; passed = $true; sha256 = (Get-FileHash -LiteralPath $externalLevel).Hash })

# The portable Windows release contains qwindows, not qoffscreen. Export mode does not create an editor window.
$previousQtPlatform = $env:QT_QPA_PLATFORM
try {
    $env:QT_QPA_PLATFORM = 'windows'
    foreach ($name in @('finite', 'external', 'infinite', 'flips', 'group', 'objects', 'empty-promotion', 'raw', 'zlib', 'gzip')) {
        $fixturePath = Join-Path $PSScriptRoot "tiled-$name.tmj"
        $exportPath = Join-Path $outputPath "tiled-$name-roundtrip.tmj"
        # Unique output per run prevents an old export from satisfying the existence assertion.
        $runExportPath = Join-Path $outputPath ("tiled-$name-" + [Guid]::NewGuid().ToString('N') + '.tmj')
        $process = Start-Process -FilePath $tiledPath -ArgumentList @('--export-map', 'json', ('"' + $fixturePath + '"'), ('"' + $runExportPath + '"')) -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(15000)) {
            $process.Kill()
            throw "Tiled export timed out after 15 seconds: $name"
        }
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $runExportPath)) { throw "Tiled rejected fixture: $name (exit $($process.ExitCode))" }
        if (-not (Test-Json -LiteralPath $runExportPath)) { throw "Tiled export is not valid JSON: $name" }
        Move-Item -LiteralPath $runExportPath -Destination $exportPath -Force
        $results.Add([ordered]@{ file = "tiled-$name.tmj"; check = 'official-Tiled-1.12.2-JSON-roundtrip'; passed = $true; sha256 = (Get-FileHash -LiteralPath $fixturePath).Hash })
    }
} finally { $env:QT_QPA_PLATFORM = $previousQtPlatform }

$inline = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'ldtk-inline.ldtk') | ConvertFrom-Json -AsHashtable
$separate = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'ldtk-separate.ldtk') | ConvertFrom-Json -AsHashtable
if ($null -ne $separate.levels[0].layerInstances -or -not $separate.externalLevels) { throw 'Separate-level fixture does not actually use external levels.' }
if ($inline.levels[0].iid -ne $level.iid -or $inline.levels[0].uid -ne $level.uid) { throw 'External level identity differs from inline level.' }
if (($inline.levels[0] | ConvertTo-Json -Depth 100 -Compress) -cne ($level | ConvertTo-Json -Depth 100 -Compress)) { throw 'Inline and separate level data are different.' }
$results.Add([ordered]@{ file = 'ldtk-inline.ldtk + levels/Fixture.ldtkl'; check = 'identical-level-data'; passed = $true })

$cases = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'diagnostic-cases.json') | ConvertFrom-Json -AsHashtable
foreach ($case in $cases) {
    if (-not (Test-Json -LiteralPath (Join-Path $PSScriptRoot $case.file))) { throw "Accidental JSON corruption in diagnostic fixture: $($case.file)" }
}
$results.Add([ordered]@{ file = 'diagnostic-cases.json'; check = 'all-intended-invalid-cases-have-valid-JSON-syntax'; count = $cases.Count; passed = $true })
$report = [ordered]@{
    ldtkSchemaVersion = '1.5.3'; ldtkSchemaSha256 = $expectedSchemaHash
    tiledEditorVersion = '1.12.2'; tiledExecutableSha256 = (Get-FileHash -LiteralPath $tiledPath).Hash
    checks = @($results)
    note = 'These checks validate fixtures, not the Cerneala importer or validator. Expected invalid data remains invalid by design.'
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputPath 'fixture-validation.json') -Encoding utf8
Write-Output ("Fixture verification: $($results.Count) checks passed; $($cases.Count) intentional diagnostic cases are syntactically valid.")
