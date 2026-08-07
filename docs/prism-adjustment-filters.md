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
- **Curve** compilează punctele `Composite`, `Red`, `Green` și `Blue` cu PCHIP
  shape-preserving într-un singur LUT RGB `1024x1`. Curba canalului se aplică
  prima, apoi curba composite.
- **LUT** citește un Hald CLUT 3D canonic: o imagine pătrată cu latura
  `level³`, care reprezintă un cub cu latura `level²`, cu roșul variind cel mai
  repede în ordinea row-major. Acceptă interpolare tetraedrică sau trilineară.
- **Channel mapping** selectează canalul înaintea curbei, nivelului sau
  transformării.
- **Threshold** calculează o histogramă globală a luminanței liniare, alege pragul Otsu cu varianță maximă între clase și folosește parametrul catalogat doar ca fallback pentru imagini degenerate sau complet transparente.
- **Levels** remapează intervalul de intrare, gamma și intervalul de ieșire.
  Cu `Auto=true`, construiește pe GPU o distribuție sincronizată pentru
  canalul selectat, elimină câte 0,1% din fiecare coadă și folosește limitele
  rezultate ca puncte de intrare. Analiza ignoră pixelii complet transparenți
  și nu citește textura înapoi pe CPU.

## Semantica filtrelor

| Filtru | Semantica Prism |
| --- | --- |
| `BrightnessContrast` | luminozitate în stopuri și contrast exponențial în jurul pivotului 0.18; varianta lineară veche rămâne selectabilă prin `UseLegacy` |
| `Levels` | remapare input/gamma/output pe RGB ori pe un canal; `Auto` alege limite robuste din percentila sincronizată a imaginii |
| `Curves` | PCHIP cubic C1 pentru puncte composite și per-canal, compilat într-un LUT RGB de 1024 eșantioane |
| `Exposure` | transformare exposure/contrast cu stiluri linear, video și logarithmic, direcții forward/inverse, pivot și parametri log configurabili |
| `Vibrance` | curbă polinomială în RGB perceptual pentru valori pozitive, desaturare globală pentru valori negative și mască opțională pentru tonurile pielii |
| `HueSaturation` | ajustare Okhsl punctuală, cu hue ponderat pe intervalul de culoare, saturație normalizată la gamutul sRGB și lightness perceptual; `Colorize` interpolează tot în același spațiu |
| `ColorBalance` | corecții ponderate pentru shadows, midtones și highlights, cu luminanță opțional păstrată |
| `BlackWhite` | mixer monocrom RGB configurabil (`Red·R + Green·G + Blue·B`), cu normalizare opțională prin `abs(1 / (Red + Green + Blue))`; valorile implicite sunt `0.333` pe fiecare canal |
| `PhotoFilter` | amestec liniar per canal între sursă și culoarea filtrului, controlat de `Density`, cu alpha copiat neschimbat |
| `ChannelMixer` | subset RGB al unei matrice 4x5: trei rânduri RGB plus constante, aplicate după unpremultiply și urmate de clamp/premultiply |
| `ColorLookup` | Hald CLUT 3D din resursa versionată, cu indexare canonică row-major și interpolare trilineară ca în `HaldClutImage` |
| `Invert` | complement RGB în spațiul linear |
| `Posterize` | cuantizare uniformă la numărul catalogat de niveluri |
| `Threshold` | alb/negru după pragul global Otsu al luminanței liniare; o histogramă uniformă returnează singurul nivel ocupat, iar una fără pixeli vizibili folosește fallback-ul catalogat |
| `GradientMap` | transfer LUT 1D linear-sRGB versionat după luminanța CIE lineară, cu interpolare, reverse și dithering Bayer 4x4 determinist |
| `SelectiveColor` | corecție CMYK Photoshop/FFmpeg pe Reds, Yellows, Greens, Cyans, Blues, Magentas, Whites, Neutrals și Blacks, în mod Relative sau Absolute |

`SelectiveColor` păstrează formula FFmpeg per interval și canal: combină
componenta CMY cu `K` prin `((-1 - adjustment) * K) - adjustment`, aplică
factorul `1 - value` în modul Relative, limitează contribuția la intervalul
valid al canalului și abia apoi o ponderază cu masca intervalului.

`GradientMap` cere un `PrismGradientMapResource` valid, cu puncte strict crescătoare care acoperă `[0, 1]`. Resursa este versionată și cache-uită ca LUT 1D de 256 eșantioane; lipsa ei produce fallback copy.

`Curves` cere un `PrismCurvesResource` valid. Punctele folosesc coordonate
normalizate, au input strict crescător și includ capetele zero și unu. LUT-ul
este versionat împreună cu resursa și cache-uit de executor; shaderul face câte
un sample pentru fiecare canal, fără pass suplimentar.

`Vibrance` convertește temporar culoarea lineară în sRGB perceptual și păstrează
alfa. `Amount>0` aplică un răspuns polinomial care scade spre zero pe măsură ce
culoarea este deja saturată; `Amount<0` folosește o cale separată de desaturare.
`AvoidSaturatingSkinTones=true` atenuează numai impulsul pozitiv în sectorul
tonurilor pielii. `GrayColorTransform` configurează axa de gri, iar
`Saturation` rămâne o ajustare globală aplicată după vibrance.

`ColorLookup` cere o imagine Hald CLUT validă, codificată în RGB linear:
latura ei trebuie să fie exact `level³` pentru un `level >= 2`. Executorul
validează această formă înainte de GPU, indexează texelul prin
`r + size * (g + size * b)` și depremultiplică texelul LUT la sample.
Interpolarea implicită este tetraedrică; `Linear`/`Trilinear` selectează
trilineara. `Intensity` amestecă rezultatul LUT cu sursa după conversia în
linear sRGB. Alfa sursei rămâne neschimbat și un pixel cu alfa zero rămâne zero.
Dacă resursa lipsește, are formă invalidă sau nu poate fi folosită, executorul
aplică politica de fallback Prism și publică un diagnostic; nu înlocuiește
LUT-ul pe furiș.

## Conformance

Vectorii analitici verifică pixeli opaci și transparenți, alpha asociat, valori
limită, canale individuale și toate profilurile de culoare selectabile.
Interacțiunile din această familie au rezultate analitice suficiente, deci nu
este necesar un golden raster separat pentru etapa de ajustări. Semantica de
mai sus este contractul Prism și nu pretinde compatibilitate byte-for-byte cu
implementări proprietare.
