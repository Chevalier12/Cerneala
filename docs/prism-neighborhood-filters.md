# Prism blur, sharpen, and noise filters

The generated Prism catalog is the source of truth for property names, value
types, defaults, domains, capabilities, determinism, and coverage ownership.
This document describes the runtime semantics shared by the blur, sharpen, and
noise families; it does not duplicate the generated property table.

## Execution model

`PrismNeighborhoodPlanner` reads the typed parameter snapshot once while the
graph is built. It converts DIP radii and distances to device pixels, prepares
logical bounds radii separately, resolves symbolic modes, and stores immutable
pass settings on each filter node. The executor binds only those prepared
settings. The shader never reads markup defaults or converts public units.

Gaussian and box filters use horizontal and vertical graph passes. Filters
whose sampling path is directional, radial, resource-driven, or edge-aware use
a direct pass. A dimension with one pixel is omitted from a separable plan; a
one-by-one source becomes an exact no-op when the operation cannot change it.
Box `Iterations` widens the prepared convolution support explicitly rather than
introducing a device-dependent pass threshold. Referința CPU folosește o
summed-area table; shaderul folosește forma separabilă echivalentă și evaluează
toate cele `2 * radius + 1` tap-uri, fără plafon ascuns de calitate.

Sampling quality is fixed by the catalog symbol:

| Quality | Samples |
| --- | ---: |
| `Draft` / `Low` | 5 |
| `Good` / `Medium` | 9 |
| `Best` / `High` | 17 |

The fixed-quality filters do not reduce quality adaptively. Image size only
removes mathematically empty axes for those filters. `SpinBlur` is the
intentional exception: its planner prepares an odd maximum tap budget from the
rotation angle and the largest masked pixel radius, while each pixel reduces
that budget again from its own arc length.

## Color and alpha

Every neighborhood sample is converted from the composition's working profile
to linear sRGB through the same conversion helpers used by adjustment filters.
Convolution operates on associated RGBA, so transparent colored pixels cannot
create halos. The result is blended at the filter opacity and converted back to
the working profile once.

The edge modes are `Clamp`, `Transparent`, `Wrap`, and `Mirror` (with
`Reflect` mapped to `Mirror`). Transparent addressing contributes zero
associated RGBA outside the source; the other modes remap the coordinate before
sampling.

## Implemented families

The classic and specialized blur set is:

- `Average`, `Blur`, `BlurMore`, `BoxBlur`, and `GaussianBlur`
- `LensBlur`, `MotionBlur`, `RadialBlur`, `ShapeBlur`, `SmartBlur`, and
  `SurfaceBlur`
- `FieldBlur`, `IrisBlur`, `TiltShift`, `PathBlur`, and `SpinBlur`

`Average` is a distinct fixed 3x3 box convolution: all nine taps have weight
`1/9`, edges clamp to the source, and bounds do not expand. CPU reference and
GPU shader both evaluate the same kernel in linear premultiplied RGBA.

`SmartBlur` și `SurfaceBlur` folosesc un suport bilateral 2D decupat la disc,
nu un șir rar de puncte spirale. `Quality` alege rezoluția grilei 5x5, 9x9 sau
17x17. Ambele înmulțesc ponderile Gaussian spațiale și de range; SmartBlur
măsoară distanța RGB straight, iar SurfaceBlur măsoară diferența de luminanță
lineară. CPU și GPU normalizează aceeași grilă.

`FieldBlur` citește adâncimea normalizată din `BlurField`, aplică inversarea
opțională și calculează circle of confusion din distanța față de
`FocalDistance`. Raza aperturii este `Blur * CoC`; eșantioanele sunt distribuite
pe disc și ponderate opțional prin luminanță pentru highlights. CPU și GPU
folosesc aceeași formulă, același suport și alpha asociat.

`IrisBlur` construiește o mască de focalizare eliptică rotită în jurul lui
`Center`. Interiorul razelor rămâne clar, `Feather` produce tranziția smoothstep,
iar în exterior circle of confusion ajunge la raza `Blur`. Parametrii decorativi
fără efect au fost eliminați; CPU și GPU rotesc coordonata înainte de scalarea
elipsei.

`TiltShift` folosește distanța semnată față de un plan orientat de `Angle`.
Banda `FocusWidth` rămâne clară, `Feather` produce tranziția smoothstep, iar
raza ajunge la `Blur` în afara benzii. Opțiunile fără efect au fost eliminate,
iar CPU/GPU folosesc aceeași mască.

`SpinBlur` integrează un arc centrat pe poziția pixelului, în coordonate de
pixel pentru a păstra traiectoria circulară pe suprafețe dreptunghiulare.
`Center` și `Radius` sunt normalizate, iar `Feather` este fracția interioară
`0..1` a razei elipsei. Numărul de tap-uri este impar, crește aproximativ cu
lungimea arcului și este plafonat la 65; aproape de centru se reduce automat.
Calea obișnuită folosește o recurență trigonometrică și sampling biliniar.
`StrobeStrength` combină expunerea continuă cu ferestrele periodice definite de
`StrobeFlashes` și `StrobeDuration`, iar `Noise` perturbă determinist numai
tap-urile interioare. CPU și GPU acumulează și normalizează RGBA asociat în
același pass direct, fără resurse auxiliare.

The sharpening set is `Sharpen`, `SharpenMore`, `SharpenEdges`, `UnsharpMask`,
`SmartSharpen`, and `HighPass`.

`Sharpen` folosește un kernel radial cu cinci tap-uri în cruce. Lobul negativ
este calculat separat pe fiecare canal din minimul, maximul și headroom-ul local,
iar `Amount` controlează monoton intensitatea în intervalul `0..1`. Filtrul
rulează într-un singur pass pe RGB liniar straight, păstrează alpha pixelului
central și repremultiplică rezultatul; vecinii complet transparenți moștenesc
culoarea centrală pentru calcul, evitând franjurile de transparent black.
CPU și GPU folosesc aceeași formulă, clamp la margine și aceleași cinci tap-uri.

`SharpenMore` folosește un high-boost 3x3 cu blur binomial
`[1 2 1; 2 4 2; 1 2 1] / 16`. La valoarea implicită `Amount = 0.5`,
formula este `2 * centru - blur`; intervalul `0..1` scalează monoton reziduul
până la intensitate dublă. Cele nouă tap-uri sunt evaluate explicit într-un
singur pass, cu clamp la margine, pe RGB liniar straight. Alpha central rămâne
neschimbat, vecinii complet transparenți folosesc culoarea centrală în calcul,
iar CPU și GPU repremultiplică același rezultat.

`SharpenEdges` evaluează un gradient Sobel normalizat pe o vecinătate 3x3 și
folosește magnitudinea lui drept poartă pentru același sharpen limitat de
contrast local ca `Sharpen`. `Threshold` stabilește centrul unei tranziții
smoothstep cu lățime proporțională pragului, în locul unei întreruperi binare,
iar `Amount` controlează intensitatea maximă. Implementarea CPU/GPU este un
singur pass determinist cu nouă tap-uri, clamp la margine și RGB liniar straight.
Alpha central rămâne neschimbat, iar vecinii complet transparenți moștenesc
culoarea centrală în calcul ca să nu producă franjuri sau muchii false.

`UnsharpMask` construiește masca dintr-un Gaussian separabil cu suport finit la
`Radius` și sigma `Radius / 3`, apoi recombină textura originală cu reziduul
`original - blur`. `Amount` scalează explicit reziduul, iar `Threshold`
controlează o poartă smoothstep centrată pe diferența de luminanță, cu o bandă
minimă de un nivel pe 8 biți ca să evite o tăietură vizibilă. Cele două pass-uri
Gaussian folosesc câte 17 tap-uri și sunt urmate de un pass fără sampling de
vecinătate care citește atât blurul, cât și originalul păstrat de graful Prism.
Calculul high-boost rulează pe RGB liniar straight, păstrează alpha original și
repremultiplică rezultatul. CPU și GPU folosesc aceeași formulă, iar `Amount = 0`,
`Radius = 0` și sursa 1x1 sunt no-op-uri exacte.

`SmartSharpen` folosește deconvoluție Richardson-Lucy cu patru iterații fixe,
care funcționează și ca regularizare. Fiecare iterație execută convoluția
estimării, raportul față de original, back-projection cu PSF-ul oglindit și
actualizarea multiplicativă; un al șaptesprezecelea pass aplică `Amount` și
protecțiile tonale. `Remove` selectează PSF Gaussian, disc pentru lens blur sau
segment orientat de `Angle` pentru motion blur. `ReduceNoise` amortizează
corecția multiplicativă către identitate, iar controalele shadow/highlight
reduc efectul folosind luminanța locală și razele lor configurate. CPU și GPU
lucrează pe RGB liniar straight, păstrează alpha original și repremultiplică
rezultatul. Graful păstrează separat observația și estimarea iterației, iar GPU
folosește suprafețe ping-pong; valorile corecției sunt codificate reversibil pe
suprafețele normalizate intermediare. Pass-urile de sampling nu extind limitele
logice ale imaginii.

The noise and cleanup set is `AddNoise`, `Despeckle`, `DustScratches`, `Median`,
and `ReduceNoise`. `AddNoise` preserves its explicit 32-bit catalog seed as two
prepared 16-bit halves and combines them with pixel coordinates and channel in
a stateless integer permutation. `Uniform` maps one permuted sample to `[-1, 1]`;
`Gaussian` transforms paired samples into a zero-mean, unit-variance normal
deviate instead of averaging uniforms. Both CPU and GPU run
in one direct pass, apply the delta to linear straight RGB, clamp, repremultiply,
and preserve source alpha. The implementation never reads time or global random
state, and monochromatic mode applies the same deviate to all color channels.

`Despeckle` folosește detecție mediană comutată și restaurare progresivă. Trei
pass-uri de detecție compară luminanța liniară straight a centrului cu eșantionul
median și marchează numai diferențele mai mari decât `Threshold`. Trei pass-uri
de restaurare înlocuiesc pixelii marcați cu mediana calculată exclusiv din
vecinii deja considerați buni; un pass final rezolvă orice marcaj rămas și
aplică opacity/blend față de intrarea originală. `Radius` controlează efectiv
suportul izotrop: raza mică folosește vecinătatea 3x3, iar razele mai mari folosesc
21 de poziții distribuite pe un disc scalat. Starea intermediară păstrează RGB
linear straight și masca binară pe suprafețe `HalfVector4`; pass-ul final
restabilește alpha central original și repremultiplică RGB. CPU și GPU folosesc
aceeași ordine a eșantioanelor, aceleași trei iterații și clamp la margine;
`Radius = 0` este identitate exactă.

`DustScratches` folosește o mediană adaptivă comutată într-un singur pass direct.
Plannerul convertește `Radius` în pixeli, îl rotunjește în sus și limitează suportul
GPU la raza 3, adică maximum 7x7. Pentru fiecare pixel, filtrul încearcă în ordine
ferestrele 3x3, 5x5 și 7x7 până când mediana este strict între minimul și maximul
luminanței locale. În acea fereastră, centrul este înlocuit numai dacă este un
extrem local și diferența sa față de mediană depășește `Threshold`; dacă nicio
fereastră nu separă semnalul de impuls, se folosește mediana celei mai mari
ferestre, tot prin aceeași poartă de prag. Sortarea folosește luminanța RGB
linear straight, dar înlocuirea păstrează alpha centrului și repremultiplică
culoarea mediană. CPU și GPU folosesc aceeași extindere, același clamp la margine
și aceeași limită de rază; `Radius = 0` este identitate exactă.

`Median` folosește o rețea fixă de compare-exchange pentru cele nouă eșantioane
ale ferestrei 3x3, cu clamp la margine și fără ramificații dependente de date în
shader. Rangul este luminanța RGB linear straight, iar rezultatul este pixelul
RGBA asociat aflat pe poziția mediană, nu o culoare sintetizată separat pe
canale. CPU și GPU execută aceeași ordine de comparații și păstrează astfel
aceeași semantică pentru pixeli translucizi. `Radius` este un selector integer:
`0` este identitate exactă, `1` activează kernelul 3x3, iar alte valori sunt
respinse de domeniul catalogului.

`ReduceNoise` folosește normalized convolution din familia Domain Transform
descrisă de Gastal și Oliveira, adaptată backendului Prism bazat pe pixel
shadere. Plannerul emite trei iterații, fiecare cu un pass orizontal și unul
vertical, iar sigma fiecărei iterații urmează programarea din lucrare. Distanța
transformată combină variația luma cu variația chroma în YCoCg; `Strength` și
`SharpenDetails` controlează luma, `ReduceColorNoise` controlează chroma, iar
`PreserveDetails` restrânge domeniul de range și readaugă stratul de detaliu la
recombinare. `RemoveJpegArtifact` adaugă două pass-uri block-aware care operează
numai pe frontierele grilei JPEG 8x8 și sunt urmate de aceeași recombinare cu
originalul. CPU și GPU folosesc aceleași formule, clamp la margine și maximum
opt tap-uri pe fiecare parte. Toate pass-urile filtrează RGB liniar straight,
maschează contribuțiile cu diferența de alpha, păstrează alpha central
nefiltrat și repremultiplică rezultatul. Când toate controalele sunt zero și
eliminarea artefactelor JPEG este dezactivată, întregul plan este identitate
exactă.

## Auxiliary resources

`LensBlur.DepthMap` is optional. `ShapeBlur.Shape`, `FieldBlur.BlurField`, and
`PathBlur.Path` are required typed image resources. Their resource identifiers
participate in graph dependencies and versioning. A specified resource that is
missing, disposed, from another graphics device, or otherwise unavailable
causes the configured `PrismFallbackPolicy` action and an observable diagnostic;
the executor does not silently substitute another filter.

## Bounds and optimization

Prepared passes carry device sampling radii and logical bounds radii as distinct
values. Separable expansions accumulate along the graph, so the final surface
covers every sampled pixel. Document-space effects whose samples remain inside
the source keep source bounds.

The optimizer removes a neighborhood node only when its prepared pass is an
exact no-op and its opacity/blend state is neutral. Zero radius, zero amount,
and a degenerate one-pixel axis are evaluated in the planner. Nonzero filters,
resource-driven filters, and non-normal blend modes retain their ordering.
