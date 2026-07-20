# Filtrele Prism de ajustare

Acest document descrie convențiile matematice comune pentru filtrele de
ajustare. Lista de filtre, proprietățile, valorile implicite, domeniile,
plannerul, kernelul și proprietarii de conformance sunt generate din
`Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`; documentul nu este o a
doua sursă de adevăr pentru acele date.

## Conducta de culoare

Un filtru primește suprafața de lucru premultiplicată. Kernelul o
depremultiplică o singură dată, convertește culoarea prin conducta comună Prism
în linear sRGB, aplică ajustarea și modul de blend, apoi convertește rezultatul
înapoi în profilul de lucru și îl premultiplică cu același alpha. Pixelul cu
alpha zero rămâne zero. Conversiile gamma, matricile de profil și operațiile
alpha sunt cele comune din conducta Prism; filtrele nu au copii locale ale
acestor formule.

Toate ajustările folosesc bounds-ul exact al sursei. Ele nu schimbă layout-ul,
hitbox-ul sau dimensiunea suprafeței.

## Primitive comune

- **Matrix** aplică o matrice RGB 3x3 și o constantă per canal.
- **Curve** aplică o curbă catalogată unui canal sau tuturor canalelor.
- **LUT** citește un cub 3D împachetat într-o textură 2D și acceptă interpolare
  tetraedrică sau trilineară.
- **Channel mapping** selectează canalul înaintea curbei, nivelului sau
  transformării.
- **Threshold** compară luminanța lineară cu pragul catalogat.
- **Levels** remapează intervalul de intrare, gamma și intervalul de ieșire
  fără histogramă sau stare ascunsă.

## Semantica filtrelor

| Filtru | Semantica Prism |
| --- | --- |
| `BrightnessContrast` | deplasare de luminozitate urmată de contrast linear, cu varianta legacy selectabilă |
| `Levels` | remapare input/gamma/output pe RGB ori pe un canal |
| `Curves` | curbă catalogată pe RGB ori pe un canal |
| `Exposure` | expunere în stopuri, offset și gamma |
| `Vibrance` | saturație adaptivă la chroma plus ajustarea globală de saturație |
| `HueSaturation` | deplasare HSV ponderată pe intervalul de culoare sau colorize |
| `ColorBalance` | corecții ponderate pentru shadows, midtones și highlights, cu luminanță opțional păstrată |
| `BlackWhite` | mix monocrom ponderat pe șase sectoare de hue, cu tint opțional |
| `PhotoFilter` | multiplicare cu culoarea filtrului și density, cu luminanță opțional păstrată |
| `ChannelMixer` | matrice RGB plus constante, cu ieșire monocromă opțională |
| `ColorLookup` | LUT 3D din resursa versionată, amestecat prin intensity |
| `Invert` | complement RGB în spațiul linear |
| `Posterize` | cuantizare uniformă la numărul catalogat de niveluri |
| `Threshold` | alb/negru după luminanța lineară |
| `GradientMap` | mapare după luminanță, cu reverse și dithering determinist |
| `SelectiveColor` | corecție CMYK ponderată pe hues, whites, neutrals și blacks |

`ColorLookup` cere o resursă validă. Dacă textura lipsește sau forma cubului nu
poate fi folosită, executorul aplică politica de fallback Prism și publică un
diagnostic; nu înlocuiește LUT-ul pe furiș.

## Conformance

Vectorii analitici verifică pixeli opaci și transparenți, alpha asociat, valori
limită, canale individuale și toate profilurile de culoare selectabile.
Interacțiunile din această familie au rezultate analitice suficiente, deci nu
este necesar un golden raster separat pentru etapa de ajustări. Semantica de
mai sus este contractul Prism și nu pretinde compatibilitate byte-for-byte cu
implementări proprietare.
