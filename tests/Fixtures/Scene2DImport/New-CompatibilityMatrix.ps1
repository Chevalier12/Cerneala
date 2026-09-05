[CmdletBinding()]
param([Parameter(Mandatory)] [string] $LdtkSchemaFile)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$schemaHash = '2AA84B0DB6E5EF1B530B1F557C5802DA1AA8BC62D10184D358C346734FB84893'
if ((Get-FileHash -LiteralPath $LdtkSchemaFile).Hash -ne $schemaHash) { throw 'Unexpected LDtk schema; audit before changing the matrix.' }
$schema = Get-Content -Raw -LiteralPath $LdtkSchemaFile | ConvertFrom-Json -AsHashtable
$scopes = [ordered]@{}

function Fields([string] $Names) { @($Names.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)) }
function Add-Tiled([string] $Name, [string] $Mapped, [string] $Metadata = '', [string] $Editor = '', [string] $Conditional = '', [string] $Unsupported = '') {
    $actions = @{}
    foreach ($group in @(@('mapped', $Mapped), @('metadata', $Metadata), @('editor', $Editor), @('conditional', $Conditional), @('unsupported', $Unsupported))) {
        foreach ($field in (Fields $group[1])) {
            if ($actions.ContainsKey($field)) { throw "Duplicate field disposition: Tiled.$Name.$field" }
            $actions[$field] = $group[0]
        }
    }
    $ordered = [ordered]@{}
    $names = [string[]]@($actions.Keys); [Array]::Sort($names, [StringComparer]::Ordinal)
    foreach ($field in $names) { $ordered[$field] = $actions[$field] }
    $scopes["Tiled.$Name"] = $ordered
}
function Add-Ldtk([string] $Name, [string] $Mapped, [string] $Metadata = '', [string] $Editor = '', [string] $Conditional = '', [switch] $EditorRemainder) {
    $definition = if ($Name -eq 'Root') { $schema.LdtkJsonRoot } else { $schema.otherTypes[$Name] }
    if ($null -eq $definition) { throw "Unknown schema type: $Name" }
    $actions = @{}
    foreach ($field in $definition.properties.Keys) { $actions[$field] = if ($EditorRemainder) { 'editor' } else { 'unsupported' } }
    foreach ($group in @(@('mapped', $Mapped), @('metadata', $Metadata), @('editor', $Editor), @('conditional', $Conditional))) {
        foreach ($field in (Fields $group[1])) {
            if (-not $actions.ContainsKey($field)) { throw "Field missing from official schema: $Name.$field" }
            $actions[$field] = $group[0]
        }
    }
    if ($Name -in @('Root', 'Level')) { $actions['__header__'] = 'conditional' }
    $ordered = [ordered]@{}
    $names = [string[]]@($actions.Keys); [Array]::Sort($names, [StringComparer]::Ordinal)
    foreach ($field in $names) { $ordered[$field] = $actions[$field] }
    $scopes["LDtk.$Name"] = $ordered
}

Add-Tiled 'Map' 'type version width height tilewidth tileheight infinite layers tilesets properties' 'tiledversion class backgroundcolor' 'compressionlevel nextlayerid nextobjectid editorsettings' 'orientation renderorder parallaxoriginx parallaxoriginy' 'hexsidelength staggeraxis staggerindex skewx skewy'
Add-Tiled 'Layer' 'id name type layers objects chunks data width height offsetx offsety opacity tintcolor visible properties' 'class' 'locked startx starty' 'encoding compression draworder x y parallaxx parallaxy mode' 'image imagewidth imageheight transparentcolor repeatx repeaty'
Add-Tiled 'Tileset' 'firstgid source type version name tilewidth tileheight tilecount columns spacing margin image imagewidth imageheight tiles properties' 'tiledversion class backgroundcolor' 'editorsettings transformations wangsets terrains' 'objectalignment tilerendersize fillmode grid tileoffset' 'transparentcolor'
Add-Tiled 'Tile' 'id objectgroup properties' 'type class' 'probability terrain' '' 'animation image imagewidth imageheight x y width height'
Add-Tiled 'Chunk' 'x y width height data'
Add-Tiled 'Object' 'id name type class x y width height rotation visible ellipse polygon polyline point properties opacity' '' '' '' 'gid template text capsule'
Add-Tiled 'Property' 'name type value' '' '' 'propertytype'
Add-Tiled 'Point' 'x y'
Add-Tiled 'TileOffset' 'x y'
Add-Tiled 'Grid' '' '' 'width height' 'orientation'

Add-Ldtk 'Root' 'jsonVersion iid defs externalLevels levels worlds' 'bgColor defaultLevelBgColor' 'toc __FORCED_REFS' 'worldLayout worldGridWidth worldGridHeight' -EditorRemainder
Add-Ldtk 'Definitions' 'entities layers tilesets' 'enums externalEnums levelFields'
Add-Ldtk 'World' 'iid identifier levels' 'worldGridWidth worldGridHeight' '' 'worldLayout' -EditorRemainder
Add-Ldtk 'Level' 'identifier iid uid worldX worldY worldDepth pxWid pxHei layerInstances externalRelPath fieldInstances' '__bgColor __smartColor __neighbours bgColor' 'bgPivotX bgPivotY useAutoIdentifier' 'bgRelPath bgPos __bgPos'
Add-Ldtk 'LayerInstance' '__cHei __cWid __gridSize __identifier __opacity __pxTotalOffsetX __pxTotalOffsetY __type __tilesetDefUid __tilesetRelPath autoLayerTiles entityInstances gridTiles iid intGridCsv layerDefUid levelId visible overrideTilesetUid pxOffsetX pxOffsetY' '' 'optionalRules seed' 'intGrid'
Add-Ldtk 'LayerDef' '__type type uid identifier gridSize pxOffsetX pxOffsetY tilesetDefUid autoTilesetDefUid' 'intGridValues intGridValuesGroups' '' 'parallaxFactorX parallaxFactorY parallaxScaling' -EditorRemainder
Add-Ldtk 'Tile' 'f px src t' '' 'd' 'a'
Add-Ldtk 'TilesetDef' '__cHei __cWid uid identifier relPath tileGridSize pxWid pxHei padding spacing' 'customData enumTags tags' '' 'embedAtlas' -EditorRemainder
Add-Ldtk 'EntityInstance' '__identifier iid defUid px width height __pivot fieldInstances' '__grid __smartColor __tags __tile' '' '' -EditorRemainder
Add-Ldtk 'FieldInstance' '__identifier __type __value defUid' '__tile' 'realEditorValues'
Add-Ldtk 'EntityDef' 'uid identifier fieldDefs' 'color width height pivotX pivotY tileRect tileRenderMode' '' '' -EditorRemainder
Add-Ldtk 'FieldDef' 'uid identifier __type type isArray canBeNull' '' '' '' -EditorRemainder
Add-Ldtk 'IntGridValueDef' 'value' 'identifier color tile groupUid'
Add-Ldtk 'IntGridValueGroupDef' 'uid' 'identifier color'
Add-Ldtk 'TileCustomMetadata' 'tileId data'
Add-Ldtk 'TilesetRect' 'tilesetUid x y w h'

$matrix = [ordered]@{
    version = 1
    status = 'Stage 0 contract selected under explicit user delegation; implementation and gates remain separate'
    tiledFormatVersions = @('1.11')
    tiledReferenceEditor = '1.12.2'
    ldtkFormatVersions = @('1.5.3')
    ldtkSchemaSha256 = $schemaHash
    unknownFieldPolicy = 'Unsupported diagnostic and no published document; never silently discard a new gameplay field'
    dispositions = [ordered]@{
        mapped = 'Read and map to core data, or validate as a structural reference.'
        metadata = 'Preserve as source metadata; not an instruction to construct UI or load GPU resources.'
        editor = 'Known authoring-only data, not used at runtime; aggregate SCN2D017 warnings deterministically per source file.'
        conditional = 'Accept only the explicit values and representation constraints in compatibility-matrix.md.'
        unsupported = 'Recognized outside v1; report SCN2D004, never publish a partial document.'
    }
    scopes = $scopes
}
$matrix | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'compatibility-matrix.json') -Encoding utf8
$fieldCount = ($scopes.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
Write-Output "Compatibility inventory: $($scopes.Count) scopes, $fieldCount field dispositions."
