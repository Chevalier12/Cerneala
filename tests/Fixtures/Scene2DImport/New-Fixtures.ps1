# Original, deterministic test data. No external map, artwork or gameplay data is copied.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Copy-Data($Value) {
    ConvertFrom-Json -InputObject (ConvertTo-Json -InputObject $Value -Depth 100) -AsHashtable
}

function Sort-FixtureValue($Value) {
    if ($Value -is [Collections.IDictionary]) {
        $ordered = [ordered]@{}
        $keys = [string[]]@($Value.Keys)
        [Array]::Sort($keys, [StringComparer]::Ordinal)
        foreach ($key in $keys) { $ordered[$key] = Sort-FixtureValue $Value[$key] }
        return $ordered
    }
    if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
        $items = [Collections.Generic.List[object]]::new()
        foreach ($item in $Value) { $items.Add((Sort-FixtureValue $item)) }
        return ,$items.ToArray()
    }
    return $Value
}

function Write-Fixture([string] $Name, $Value) {
    $fixturePath = Join-Path $PSScriptRoot $Name
    $fixtureDirectory = Split-Path -Parent $fixturePath
    New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
    [IO.File]::WriteAllText($fixturePath, (ConvertTo-Json -InputObject (Sort-FixtureValue $Value) -Depth 100) + "`n")
}

function Property([string] $Name, [string] $Type, $Value) {
    @{ name = $Name; type = $Type; value = $Value }
}

$tileset = @{
    name = 'Atlas'; tilewidth = 16; tileheight = 16; tilecount = 2; columns = 2
    spacing = 0; margin = 0; image = 'atlas.svg'; imagewidth = 32; imageheight = 16
    tiles = @(@{ id = 1; properties = @((Property 'InitialState' 'string' 'Closed')) })
}
$embeddedTileset = Copy-Data $tileset
$embeddedTileset.firstgid = 1
$tiled = @{
    type = 'map'; version = '1.11'; tiledversion = '1.12.2'; orientation = 'orthogonal'
    renderorder = 'right-down'; width = 2; height = 2; tilewidth = 16; tileheight = 16
    infinite = $false; nextlayerid = 3; nextobjectid = 1; compressionlevel = -1
    layers = @(
        @{
            type = 'tilelayer'; id = 1; name = 'Ground'; width = 2; height = 2
            x = 0; y = 0; offsetx = 2; offsety = 3; opacity = 1; visible = $true
            data = @(1, 2147483650, 1073741825, 0)
        },
        @{
            type = 'tilelayer'; id = 2; name = 'Roof'; width = 2; height = 2
            x = 0; y = 0; offsetx = 2; offsety = 3; opacity = 0.5; visible = $true
            data = @(0, 0, 0, 2)
        }
    )
    tilesets = @($embeddedTileset)
}
Write-Fixture 'tiled-finite.tmj' $tiled

$externalTileset = Copy-Data $tileset
$externalTileset.type = 'tileset'; $externalTileset.version = '1.11'; $externalTileset.tiledversion = '1.12.2'
$externalTileset.image = '../atlas.svg'
Write-Fixture 'tilesets/atlas.tsj' $externalTileset
$external = Copy-Data $tiled
$external.tilesets = @(@{ firstgid = 1; source = 'tilesets/atlas.tsj' })
Write-Fixture 'tiled-external.tmj' $external

$sparse = Copy-Data $tiled
$sparse.infinite = $true; $sparse.width = 0; $sparse.height = 0
$sparse.layers = @($sparse.layers[0])
$sparse.layers[0].Remove('data') | Out-Null
$sparse.layers[0].width = 18; $sparse.layers[0].height = 2
$sparse.layers[0].startx = -2; $sparse.layers[0].starty = -1
$sparse.layers[0].chunks = @(
    @{ x = -2; y = -1; width = 2; height = 2; data = @(1, 2147483650, 1073741825, 0) },
    @{ x = 14; y = -1; width = 2; height = 2; data = @(0, 2, 0, 0) }
)
Write-Fixture 'tiled-infinite.tmj' $sparse

$flips = Copy-Data $tiled
$flips.width = 4; $flips.layers = @($flips.layers[0]); $flips.layers[0].width = 4
$flips.layers[0].data = @(1, 2147483649, 1073741825, 3221225473, 536870913, 2684354561, 1610612737, 3758096385)
Write-Fixture 'tiled-flips.tmj' $flips

$grouped = Copy-Data $tiled
$grouped.layers = @(@{
    type = 'group'; id = 3; name = 'Village'; x = 0; y = 0
    offsetx = 10; offsety = -4; opacity = 0.5; tintcolor = '#80ffffff'; visible = $true
    layers = $grouped.layers
})
$grouped.nextlayerid = 4
Write-Fixture 'tiled-group.tmj' $grouped

# Original object fixture: a spawn, a rotated ellipse, an open chain and sparse door promotion.
$objects = Copy-Data $tiled
$objects.layers += @(@{
    type = 'objectgroup'; id = 3; name = 'Objects'; draworder = 'index'; x = 0; y = 0; opacity = 1; visible = $true
    objects = @(
        @{ id = 1; name = 'Player'; type = 'Player'; x = 8; y = 24; width = 0; height = 0; rotation = 0; visible = $true; point = $true
           properties = @((Property 'CernealaRole' 'string' 'Spawn'), (Property 'InitialState' 'string' 'Idle')) },
        @{ id = 2; name = 'Pond'; type = ''; x = 4; y = 4; width = 16; height = 8; rotation = 30; visible = $true; ellipse = $true
           properties = @((Property 'CernealaRole' 'string' 'Collider'), (Property 'CollisionLayer' 'int' 2), (Property 'CollisionMask' 'int' 1), (Property 'IsTrigger' 'bool' $true)) },
        @{ id = 3; name = 'Fence'; type = ''; x = 0; y = 28; width = 0; height = 0; rotation = 0; visible = $true
           polyline = @(@{ x = 0; y = 0 }, @{ x = 16; y = 0 }, @{ x = 16; y = -8 })
           properties = @((Property 'CernealaRole' 'string' 'Collider'), (Property 'CollisionLayer' 'int' 2)) },
        @{ id = 4; name = 'Door'; type = ''; x = 16; y = 0; width = 0; height = 0; rotation = 0; visible = $true; point = $true
           properties = @((Property 'CernealaRole' 'string' 'Promote'), (Property 'TileLayer' 'string' '1'), (Property 'TileX' 'int' 1), (Property 'TileY' 'int' 0), (Property 'InitialState' 'string' 'Closed')) }
    )
})
$objects.nextlayerid = 4; $objects.nextobjectid = 5
Write-Fixture 'tiled-objects.tmj' $objects

foreach ($compression in @('raw', 'zlib', 'gzip')) {
    $encoded = Copy-Data $tiled
    foreach ($layer in $encoded.layers) {
        $bytes = [byte[]]::new($layer.data.Count * 4)
        for ($cellIndex = 0; $cellIndex -lt $layer.data.Count; $cellIndex++) {
            $gid = [uint32]$layer.data[$cellIndex]
            for ($byteIndex = 0; $byteIndex -lt 4; $byteIndex++) {
                $bytes[($cellIndex * 4) + $byteIndex] = [byte](($gid -shr ($byteIndex * 8)) -band 255)
            }
        }
        if ($compression -ne 'raw') {
            $memory = [IO.MemoryStream]::new()
            $stream = if ($compression -eq 'zlib') {
                [IO.Compression.ZLibStream]::new($memory, [IO.Compression.CompressionLevel]::Optimal, $true)
            } else { [IO.Compression.GZipStream]::new($memory, [IO.Compression.CompressionLevel]::Optimal, $true) }
            try { $stream.Write($bytes) } finally { $stream.Dispose() }
            $bytes = $memory.ToArray(); $memory.Dispose()
        }
        $layer.data = [Convert]::ToBase64String($bytes)
        $layer.encoding = 'base64'
        $layer.compression = if ($compression -eq 'raw') { '' } else { $compression }
    }
    Write-Fixture "tiled-$compression.tmj" $encoded
}

function Ldtk-LayerDefinition([int] $Uid, [string] $Name) {
    @{
        __type = 'Tiles'; type = 'Tiles'; uid = $Uid; identifier = $Name; gridSize = 16; displayOpacity = 1
        pxOffsetX = 2; pxOffsetY = 3; parallaxFactorX = 0; parallaxFactorY = 0; parallaxScaling = $true
        intGridValues = @(); intGridValuesGroups = @(); autoRuleGroups = @(); excludedTags = @(); requiredTags = @(); uiFilterTags = @()
        canSelectWhenInactive = $true; guideGridHei = 0; guideGridWid = 0; hideFieldsWhenInactive = $true
        hideInList = $false; inactiveOpacity = 1; renderInWorldView = $true; tilePivotX = 0; tilePivotY = 0; useAsyncRender = $false
        tilesetDefUid = 3
    }
}
function Ldtk-Layer([int] $Uid, [string] $Name, [string] $Iid, [double] $Opacity, $Tiles) {
    @{
        __cHei = 2; __cWid = 2; __gridSize = 16; __identifier = $Name; __opacity = $Opacity
        __pxTotalOffsetX = 2; __pxTotalOffsetY = 3; __type = 'Tiles'; __tilesetDefUid = 3; __tilesetRelPath = 'atlas.svg'
        autoLayerTiles = @(); entityInstances = @(); gridTiles = @($Tiles); iid = $Iid; intGridCsv = @()
        layerDefUid = $Uid; levelId = 4; pxOffsetX = 0; pxOffsetY = 0; visible = $true; optionalRules = @(); seed = 1
    }
}
$groundTiles = @(
    @{ a = 1; f = 0; px = @(0, 0); src = @(0, 0); t = 0; d = @(0) },
    @{ a = 1; f = 1; px = @(16, 0); src = @(16, 0); t = 1; d = @(1) },
    @{ a = 1; f = 2; px = @(0, 16); src = @(0, 0); t = 0; d = @(2) }
)
$roofTiles = @(@{ a = 1; f = 0; px = @(16, 16); src = @(16, 0); t = 1; d = @(3) })
$ground = Ldtk-Layer 1 'Ground' '11111111-1111-4111-8111-111111111111' 1 $groundTiles
$roof = Ldtk-Layer 2 'Roof' '22222222-2222-4222-8222-222222222222' 0.5 $roofTiles
$level = @{
    __bgColor = '#000000'; __neighbours = @(); __smartColor = '#000000'; bgPivotX = 0.5; bgPivotY = 0.5; bgRelPath = $null
    fieldInstances = @(); identifier = 'Fixture'; iid = '44444444-4444-4444-8444-444444444444'
    pxHei = 32; pxWid = 32; uid = 4; worldDepth = 0; worldX = 0; worldY = 0; useAutoIdentifier = $false
    externalRelPath = $null; layerInstances = @($roof, $ground)
}
$ldtk = @{
    __header__ = @{ fileType = 'LDtk Project JSON'; app = 'LDtk'; appVersion = '1.5.3'; schema = 'https://ldtk.io/files/JSON_SCHEMA.json'; doc = 'https://ldtk.io/json'; url = 'https://ldtk.io' }
    jsonVersion = '1.5.3'; iid = '55555555-5555-4555-8555-555555555555'; bgColor = '#000000'; externalLevels = $false
    defs = @{
        entities = @(); enums = @(); externalEnums = @(); levelFields = @()
        layers = @((Ldtk-LayerDefinition 2 'Roof'), (Ldtk-LayerDefinition 1 'Ground'))
        tilesets = @(@{
            __cHei = 1; __cWid = 2; customData = @(); enumTags = @(); identifier = 'Atlas'; padding = 0
            pxHei = 16; pxWid = 32; spacing = 0; tags = @(); tileGridSize = 16; uid = 3; savedSelections = @(); relPath = 'atlas.svg'
        })
    }
    levels = @($level); toc = @(); worlds = @(); appBuildId = 0; backupLimit = 10; backupOnSave = $false
    customCommands = @(); defaultEntityHeight = 16; defaultEntityWidth = 16; defaultGridSize = 16; defaultLevelBgColor = '#000000'
    defaultPivotX = 0; defaultPivotY = 0; dummyWorldIid = '66666666-6666-4666-8666-666666666666'
    exportLevelBg = $false; exportTiled = $false; flags = @(); identifierStyle = 'Capitalize'; imageExportMode = 'None'
    levelNamePattern = 'Level_%idx'; minifyJson = $false; nextUid = 5; simplifiedExport = $false; worldLayout = 'Free'
}
Write-Fixture 'ldtk-inline.ldtk' $ldtk
$separate = Copy-Data $ldtk
$separate.externalLevels = $true
$separate.levels[0].externalRelPath = 'levels/Fixture.ldtkl'; $separate.levels[0].layerInstances = $null
Write-Fixture 'ldtk-separate.ldtk' $separate
$externalLevel = Copy-Data $level
$externalLevel.__header__ = Copy-Data $ldtk.__header__
Write-Fixture 'levels/Fixture.ldtkl' $externalLevel

$cases = [Collections.Generic.List[object]]::new()
function Invalid-Tiled([string] $Name, [string] $Code, [string] $Category, [scriptblock] $Mutate, $Baseline = $tiled) {
    $invalid = Copy-Data $Baseline
    & $Mutate $invalid
    Write-Fixture "invalid/$Name.tmj" $invalid
    $cases.Add(@{ file = "invalid/$Name.tmj"; format = 'Tiled'; code = $Code; category = $Category })
}
# Invalid cases keep valid resource paths unless the resource itself is under test.
$invalidBase = Copy-Data $tiled; $invalidBase.tilesets[0].image = '../atlas.svg'
Invalid-Tiled 'version' 'SCN2D003' 'Fatal' { param($m) $m.version = '999.0' } $invalidBase
Invalid-Tiled 'tile-id' 'SCN2D006' 'Error' { param($m) $m.layers[0].data[0] = 99 } $invalidBase
Invalid-Tiled 'atlas-bounds' 'SCN2D007' 'Error' { param($m) $m.tilesets[0].imagewidth = 16 } $invalidBase
Invalid-Tiled 'missing-file' 'SCN2D001' 'Fatal' { param($m) $m.tilesets = @(@{ firstgid = 1; source = 'missing.tsj' }) } $invalidBase
Invalid-Tiled 'path-escape' 'SCN2D010' 'Fatal' { param($m) $m.tilesets[0].image = '../../../../outside.svg' } $invalidBase
Invalid-Tiled 'unsupported' 'SCN2D004' 'Unsupported' { param($m) $m.orientation = 'isometric' } $invalidBase
Invalid-Tiled 'unknown-field' 'SCN2D004' 'Unsupported' { param($m) $m.futureGameplayField = 1 } $invalidBase
Invalid-Tiled 'editor-warning' 'SCN2D017' 'Warning' { param($m) $m.editorsettings = @{ export = @{ target = 'unused.tmj'; format = 'json' } } } $invalidBase
$overlapBase = Copy-Data $sparse; $overlapBase.tilesets[0].image = '../atlas.svg'
Invalid-Tiled 'chunk-overlap' 'SCN2D011' 'Error' { param($m) $m.layers[0].chunks[1].x = -1 } $overlapBase
$invalidObjects = Copy-Data $objects; $invalidObjects.tilesets[0].image = '../atlas.svg'
Invalid-Tiled 'degenerate-collider' 'SCN2D008' 'Error' { param($m) $m.layers[2].objects[2].polyline[1] = @{ x = 0; y = 0 } } $invalidObjects
Invalid-Tiled 'layer-mask' 'SCN2D009' 'Error' { param($m) $m.layers[2].objects[1].properties[1].value = -1 } $invalidObjects
Invalid-Tiled 'duplicate-promotion' 'SCN2D012' 'Error' { param($m) $duplicate = Copy-Data $m.layers[2].objects[3]; $duplicate.id = 5; $m.layers[2].objects += @($duplicate); $m.nextobjectid = 6 } $invalidObjects
Invalid-Tiled 'absent-promotion' 'SCN2D012' 'Error' { param($m) $m.layers[2].objects[3].properties[2].value = 20 } $invalidObjects
Invalid-Tiled 'empty-promotion' 'SCN2D012' 'Error' { param($m) $m.layers[2].objects[3].properties[3].value = 1 } $invalidObjects
$emptyPromotion = Copy-Data $objects; $emptyPromotion.layers[2].objects[3].properties[3].value = 1
$emptyPromotion.layers[2].objects[3].properties += @((Property 'TileId' 'int' 2))
Write-Fixture 'tiled-empty-promotion.tmj' $emptyPromotion
$unknownField = Copy-Data $ldtk; $unknownField.defs.tilesets[0].relPath = '../atlas.svg'
$unknownField.levels[0].fieldInstances = @(@{ __identifier = 'Future'; __type = 'Array<Point>'; __value = @(@{ cx = 1; cy = 1 }); defUid = 9; realEditorValues = @() })
Write-Fixture 'invalid/ldtk-field.ldtk' $unknownField
$cases.Add(@{ file = 'invalid/ldtk-field.ldtk'; format = 'LDtk'; code = 'SCN2D004'; category = 'Unsupported' })
Write-Fixture 'diagnostic-cases.json' @($cases)
Write-Output ('Generated fixture corpus and ' + $cases.Count + ' diagnostic cases.')
