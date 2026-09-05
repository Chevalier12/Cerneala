# Original deterministic sample data and pixel art; no external artwork is copied.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

function Write-Json([string] $Name, $Value) {
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $Name), (ConvertTo-Json -InputObject $Value -Depth 80) + "`n")
}
function Property([string] $Name, [string] $Type, $Value) {
    [ordered]@{ name = $Name; type = $Type; value = $Value }
}
function Iid([int] $Number) { '{0:x8}-1234-4234-8234-123456789abc' -f $Number }

$width = 64; $height = 32; $tileSize = 16
$ground = [int[]]::new($width * $height)
$buildings = [int[]]::new($width * $height)
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $ground[$y * $width + $x] = if ($y -in 11,12 -or $x -eq 14) { 2 } else { 1 }
        if (($x -lt 8 -or $x -gt 34) -and $x % 3 -eq 2 -and $y % 3 -eq 2) {
            $buildings[$y * $width + $x] = 3
        }
    }
}
foreach ($house in @(@(12,5), @(23,4), @(47,5))) {
    for ($dy = 0; $dy -lt 5; $dy++) {
        for ($dx = 0; $dx -lt 6; $dx++) {
            $buildings[($house[1]+$dy)*$width+$house[0]+$dx] = if ($dy -lt 2) { 5 } else { 4 }
        }
    }
    $buildings[($house[1]+4)*$width+$house[0]+2] = 7
}
for ($x = 8; $x -le 25; $x++) { $buildings[15*$width+$x] = 6 }
$buildings[10*$width+10] = 15

$objects = [Collections.Generic.List[object]]::new()
$objects.Add([ordered]@{ Label='Player'; Role='Spawn'; X=226; Y=192; Width=12; Height=14; State='Idle' })
$objects.Add([ordered]@{ Label='Door'; Role='Promote'; X=224; Y=144; Width=16; Height=16; State='Closed' })
foreach ($box in @(@('Fence',128,242,288,4), @('HouseBack',192,112,96,4),
    @('HouseLeft',192,112,4,48), @('HouseRight',284,112,4,48),
    @('HouseFrontLeft',192,156,32,4), @('HouseFrontRight',240,156,48,4))) {
    $objects.Add([ordered]@{ Label=$box[0]; Role='Collider'; X=$box[1]; Y=$box[2]; Width=$box[3]; Height=$box[4]; State='' })
}
$tiledLayers = [Collections.Generic.List[object]]::new()
foreach ($layerInfo in @(@(1,'Terrain',$ground), @(2,'Buildings',$buildings))) {
    $chunks = [Collections.Generic.List[object]]::new()
    for ($cy=0; $cy -lt $height; $cy+=8) {
        for ($cx=0; $cx -lt $width; $cx+=8) {
            $cells = [Collections.Generic.List[int]]::new()
            for ($y=0; $y -lt 8; $y++) { for ($x=0; $x -lt 8; $x++) { $cells.Add($layerInfo[2][($cy+$y)*$width+$cx+$x]) } }
            $chunks.Add([ordered]@{ x=$cx; y=$cy; width=8; height=8; data=$cells.ToArray() })
        }
    }
    $tiledLayers.Add([ordered]@{ type='tilelayer'; id=$layerInfo[0]; name=$layerInfo[1]; width=$width; height=$height;
        x=0; y=0; opacity=1; visible=$true; chunks=$chunks.ToArray() })
}
$tiledObjects = [Collections.Generic.List[object]]::new()
for ($index=0; $index -lt $objects.Count; $index++) {
    $item=$objects[$index]
    $properties=@((Property 'Label' 'string' $item.Label), (Property 'CernealaRole' 'string' $item.Role),
        (Property 'InitialState' 'string' $item.State))
    if ($item.Role -eq 'Collider') { $properties+=@((Property 'CollisionLayer' 'int' 2), (Property 'CollisionMask' 'int' 1)) }
    if ($item.Role -eq 'Promote') { $properties+=@((Property 'TileLayer' 'string' '2'), (Property 'TileX' 'int' 14), (Property 'TileY' 'int' 9)) }
    $obj=[ordered]@{ id=$index+1; name=$item.Label; type=''; x=$item.X; y=$item.Y; width=$item.Width; height=$item.Height;
        rotation=0; visible=$true; properties=$properties }
    if ($item.Role -ne 'Collider') { $obj.point=$true }
    $tiledObjects.Add($obj)
}
$tiledLayers.Add([ordered]@{ type='objectgroup'; id=3; name='Objects'; draworder='index'; x=0; y=0;
    opacity=1; visible=$true; objects=$tiledObjects.ToArray() })
Write-Json 'village.tmj' ([ordered]@{ type='map'; version='1.11'; tiledversion='1.12.2'; orientation='orthogonal';
    renderorder='right-down'; width=0; height=0; tilewidth=16; tileheight=16; infinite=$true; layers=$tiledLayers.ToArray();
    tilesets=@([ordered]@{ firstgid=1; name='WorldAtlas'; tilewidth=16; tileheight=16; tilecount=16; columns=8;
        spacing=0; margin=0; image='world-atlas.png'; imagewidth=128; imageheight=32 }) })

$fieldSpecs=@(@('Label','String'), @('CernealaRole','String'), @('InitialState','String'), @('ColliderShape','String'),
    @('CollisionLayer','Int'), @('CollisionMask','Int'), @('TileLayer','String'), @('TileX','Int'), @('TileY','Int'))
$fieldDefs=@(for ($i=0; $i -lt $fieldSpecs.Count; $i++) {
    [ordered]@{ uid=200+$i; identifier=$fieldSpecs[$i][0]; __type=$fieldSpecs[$i][1]; type=('F_'+$fieldSpecs[$i][1]); isArray=$false; canBeNull=$false }
})
$entityInstances=@(for ($index=0; $index -lt $objects.Count; $index++) {
    $item=$objects[$index]
    $values=@($item.Label,$item.Role,$item.State,'Box',2,1,'2',14,9)
    $fields=@(for ($i=0; $i -lt $fieldSpecs.Count; $i++) {
        [ordered]@{ __identifier=$fieldSpecs[$i][0]; __type=$fieldSpecs[$i][1]; __value=$values[$i]; defUid=200+$i; realEditorValues=@() }
    })
    [ordered]@{ __identifier='WorldObject'; __grid=@([int][Math]::Floor($item.X/16),[int][Math]::Floor($item.Y/16));
        __pivot=@(0,0); __tags=@(); __tile=$null; __worldX=$item.X; __worldY=$item.Y;
        defUid=100; iid=(Iid (1000+$index)); px=@($item.X,$item.Y); width=$item.Width; height=$item.Height; fieldInstances=$fields }
})
$layerDefs=[Collections.Generic.List[object]]::new()
$layerInstances=[Collections.Generic.List[object]]::new()
foreach ($layerInfo in @(@(3,'Objects',$null), @(2,'Buildings',$buildings), @(1,'Terrain',$ground))) {
    $uid=[int]$layerInfo[0]; $isEntities=$uid -eq 3; $kind=if ($isEntities) { 'Entities' } else { 'Tiles' }
    $tiles=@(if (-not $isEntities) {
        for ($y=0; $y -lt $height; $y++) { for ($x=0; $x -lt $width; $x++) {
            $id=$layerInfo[2][$y*$width+$x]
            if ($id -ne 0) { [ordered]@{ a=1; f=0; px=@(($x*16),($y*16)); src=@(((($id-1)%8)*16),([int][Math]::Floor(($id-1)/8)*16)); t=$id-1; d=@($y*$width+$x) } }
        } }
    })
    $tilesetUid=if ($isEntities) { $null } else { 10 }
    $layerDefs.Add([ordered]@{ __type=$kind; type=$kind; uid=$uid; identifier=$layerInfo[1]; gridSize=16;
        pxOffsetX=0; pxOffsetY=0; parallaxFactorX=0; parallaxFactorY=0; tilesetDefUid=$tilesetUid;
        intGridValues=@(); intGridValuesGroups=@(); autoRuleGroups=@() })
    $layerInstances.Add([ordered]@{ __cHei=$height; __cWid=$width; __gridSize=16; __identifier=$layerInfo[1];
        __opacity=1; __pxTotalOffsetX=0; __pxTotalOffsetY=0; __type=$kind; __tilesetDefUid=$tilesetUid;
        __tilesetRelPath=$(if ($isEntities) { $null } else { 'world-atlas.png' });
        iid=(Iid $uid); layerDefUid=$uid; levelId=20; pxOffsetX=0; pxOffsetY=0; visible=$true;
        autoLayerTiles=@(); entityInstances=@(if ($isEntities) { $entityInstances }); gridTiles=$tiles; intGridCsv=@() })
}
Write-Json 'village.ldtk' ([ordered]@{ jsonVersion='1.5.3'; iid=(Iid 30); externalLevels=$false; worldLayout='Free'; worlds=@();
    defs=[ordered]@{ layers=$layerDefs.ToArray(); entities=@([ordered]@{ uid=100; identifier='WorldObject'; width=16; height=16;
        pivotX=0; pivotY=0; fieldDefs=$fieldDefs }); enums=@(); externalEnums=@(); levelFields=@();
        tilesets=@([ordered]@{ uid=10; identifier='WorldAtlas'; tileGridSize=16; pxWid=128; pxHei=32; __cWid=8; __cHei=2;
            padding=0; spacing=0; relPath='world-atlas.png'; customData=@(); enumTags=@(); tags=@() }) };
    levels=@([ordered]@{ identifier='Village'; iid=(Iid 20); uid=20; pxWid=$width*16; pxHei=$height*16;
        worldX=0; worldY=0; worldDepth=0; externalRelPath=$null; fieldInstances=@(); layerInstances=$layerInstances.ToArray() }) })

$atlas=[System.Drawing.Bitmap]::new(128,32,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    for ($id=1; $id -le 16; $id++) {
        for ($y=0; $y -lt 16; $y++) { for ($x=0; $x -lt 16; $x++) {
            $color=[System.Drawing.Color]::Transparent
            switch ($id) {
                1 { $color=if (($x*7+$y*11)%23 -eq 0) { '#3C7144' } else { '#28563A' } }
                2 { $color=if (($x*3+$y*5)%19 -eq 0) { '#B49767' } else { '#C2AC7B' } }
                3 { if ($x -ge 7 -and $x -le 9 -and $y -gt 10) { $color='#71482E' }; if ($y -gt 1 -and $y -lt 13 -and [Math]::Abs($x-8) -le $y/2) { $color=if (($x+$y)%4 -eq 0) { '#448454' } else { '#183B2E' } } }
                4 { $color=if ($y%5 -eq 0 -or ($x+($y/5)*4)%8 -eq 0) { '#A57952' } else { '#D2B58A' } }
                5 { $color=if ($y%4 -eq 0) { '#673C38' } else { '#A05243' } }
                6 { if ($x -in 1,2,3,12,13,14 -and $y -ge 2 -and $y -le 14 -or $y -in 5,6,10,11) { $color=if ($y -in 5,10) { '#E1C09A' } else { '#9A704A' } } }
                7 { $color=if ($x -lt 2 -or $x -gt 13 -or $y -lt 2) { '#473229' } elseif ($x -eq 11 -and $y -eq 9) { '#F0CB70' } elseif ($x%4 -eq 0) { '#624432' } else { '#946642' } }
                8 { $color=if ($x -lt 3) { '#946642' } else { '#1B2729' } }
                { $_ -in 9,10,11,12,13 } {
                    if ($y -ge 1 -and $y -le 5 -and $x -ge 5 -and $x -le 10) { $color='#E5B785' }
                    if ($y -ge 5 -and $y -le 11 -and $x -ge 4 -and $x -le 11) { $color=if ($id -eq 13) { '#F0AA4B' } else { '#55C5D1' } }
                    $legShift=if ($id -in 10,12) { 1 } else { 0 }
                    if ($y -ge 11 -and $y -le 14 -and ($x -eq 5+$legShift -or $x -eq 10-$legShift)) { $color='#233744' }
                    if ($y -eq 3 -and $x -eq 9) { $color='#20313B' }
                }
                14 { $color=if ($y%4 -eq 0) { '#488BA0' } else { '#31596F' } }
                15 { if ($x -in 5,10 -and $y -ge 6) { $color='#77A456' }; if ($x -ge 3 -and $x -le 7 -and $y -ge 3 -and $y -le 6) { $color='#DB8C97' }; if ($x -ge 8 -and $x -le 12 -and $y -ge 5 -and $y -le 8) { $color='#E8C771' } }
                16 { $color='#85634D' }
            }
            if ($color -is [string]) { $color=[System.Drawing.ColorTranslator]::FromHtml($color) }
            $atlas.SetPixel((($id-1)%8)*16+$x,[int][Math]::Floor(($id-1)/8)*16+$y,$color)
        } }
    }
    $atlas.Save((Join-Path $PSScriptRoot 'world-atlas.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally { $atlas.Dispose() }
Write-Output 'Generated village.tmj, village.ldtk and original world-atlas.png.'
