# Scene Village artwork

All artwork in this directory is redistributed under Creative Commons Zero
([CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)). The original
files are kept unmodified; runtime scaling and source rectangles are applied
by the app.

| File | Source and creator | Source version | SHA-256 of included file |
| --- | --- | --- | --- |
| `tiny-town.png` | [Tiny Town](https://kenney.nl/assets/tiny-town) by Kenney | 1.1, `Tilemap/tilemap_packed.png` from the official archive | `3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902` |
| `ch003.png` | [Character sprite + walk animation](https://opengameart.org/content/character-sprite-walk-animation) by Belohlavek | Original `ch003.png` from the OpenGameArt upload | `02AC85F7A6DD90A486ED4126CC1CE80D482F6D9D17C02AD42DBAAC1C80B479DB` |

The downloaded Tiny Town 1.1 ZIP had SHA-256
`9768692DCCFF1D706408A5AEDD6CA4F6CD1409506CBC84CB2F862919764BE977`.
Its original `License.txt` is preserved here as `Kenney-TinyTown-License.txt`.
The packed sheet is 192 x 176 pixels, containing 12 columns and 11 rows of
16 x 16 tiles. Tile index `n` begins at `(16 * (n % 12), 16 * (n / 12))`.

Belohlavek's upload explicitly marks the PNG [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)
and calls it a walk animation; it supplies no separate license file. The
unmodified `ch003.png` is a transparent 128 x 128 sheet of 32 x 32 cells.
Image inspection maps rows 0–3 to down/front, up/back, left and right.
Each row contains alternating walking steps in columns 0 and 2, with
neutral-foot poses in columns 1 and 3. The app reads rows 0, 1 and 3 for
down, up and right. It deliberately reuses the right row with a per-frame
horizontal flip for left, rather than reading the sheet's own left row. This
exercises Cerneala's existing sprite mirroring; the included PNG remains
unmodified. The app uses column 1 for idle and all four columns in order for
walking. The upload does not publish a cell-by-cell map; the row labels are
an inference from the included pixels.

The app selects Point sampling for the player and stress villagers so a
cropped frame does not blend with opaque pixels in the vertically adjacent
atlas cell. It does not alter the source PNG. The dark shoulder/arm band in
the up-facing neutral frame is present inside the original source cell and
is retained; sampling cannot remove authored pixels.

The green tree combines adjacent atlas cells 4 and 16 into one unchanged
`(64, 0, 16, 32)` source region. It is rendered at 16 x 32 world units, with
the same authored lower-cell site; the trunk collider scales with the art.
Flower 2, rounded foliage 5, and small trees 27/28 use one 16 x 16 cell each.
Cell 5 is rendered exactly as supplied, including its flat lower edge. The
official archive has no metadata naming grouped objects or proving an
intended extension below it. Cell 17 is not used by this scene.

Source pages and original files were checked on 2026-09-24. No Tiny Dungeon
artwork is included.
