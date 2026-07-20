# Prism — catalogul complet de filtre

## Scop

Implementează toate filtrele Photoshop aprobate în catalogul Prism și le leagă
de parser, runtime, optimizer, kerneluri, diagnostics și teste fără liste
duplicate manual.

**Dependențe:** `2026-07-18-prism-markup-motion-and-lifecycle.md` și
`2026-07-18-prism-monogame-compositor.md`.

## Etapa 0 — contract de completitudine

- [x] Generează din `prism-catalog.json` matricea filtru → proprietăți/defaults
  → planner → kernel → test semantic → golden → documentație.
- [x] Adaugă un test RED care eșuează pentru fiecare filtru ori proprietate
  fără implementare; nu menține allowlist-uri paralele în teste.
- [x] Clasifică fiecare filtru după primitive reutilizabile, extinderea
  bounds-ului, sampling, format/color space, capabilități GPU, determinism,
  cacheability și resursele care trebuie versionate.
- [x] Definește pentru filtrele cu randomness un seed explicit și output
  determinist; interzice timpul curent sau RNG global ca input ascuns.
- [x] Definește politica pentru formate/capabilități indisponibile prin
  `PrismFallbackPolicy`, cu diagnostic observabil, fără substituție silențioasă.

### Gate etapa 0

- [x] Catalogul complet produce automat o listă finită de implementat și buildul
  nu poate deveni verde cu o intrare uitată.

## Etapa 1 — primitive și filtre de ajustare

- [x] Implementează primitive comune pentru matrix/curve/LUT, channel mapping,
  thresholds, histogram-free levels și conversii de culoare.
- [x] Implementează toate filtrele de ajustare/color declarate în catalog,
  folosind aceleași primitive și aceeași convenție linear/premultiplied.
- [x] Generează bindingul parametrilor și validarea domeniilor din catalog;
  plannerul nu repetă defaults.
- [x] Adaugă vectori analitici pentru pixeli opaci/transparenți, valori limită,
  canale individuale și profiluri de culoare selectabile.
- [x] Adaugă golden-uri numai pentru interacțiuni care nu pot fi validate
  suficient prin vectori. (Nu a fost necesar: toate interacțiunile acestei
  familii sunt acoperite suficient prin vectori analitici.)

### Gate etapa 1

- [x] Toate ajustările din catalog au implementare, test și documentație, fără
  conversii gamma sau alpha duplicate.

## Etapa 2 — blur, sharpen și noise

- [x] Implementează primitive comune separabile pentru blur, convolution,
  neighborhood sampling și noise determinist.
- [x] Implementează toate filtrele Blur, Sharpen și Noise declarate în catalog,
  inclusiv variantele care cer kernel specializat.
- [x] Calculează radius/bounds o singură dată în planner și transmite kernelului
  parametri pregătiți; shaderul nu reinterpretează semantica markup.
- [x] Alege strategia de sampling/passes după capabilități și dimensiune fără
  quality degradation ascuns ori praguri numerice nebenchmarkuite.
- [x] Testează edge sampling, alpha edges, raze zero/maxime, seed, imagini mici
  și nested color profiles.

### Gate etapa 2

- [x] Rezultatul este determinist, bounds nu taie pixeli, iar optimizerul
  elimină numai filtrele matematic no-op.

## Etapa 3 — distort, transform și resampling

- [x] Implementează primitive comune pentru coordinate mapping, displacement,
  polar/cartesian transform, wrap/clamp/mirror și sampling quality.
- [x] Implementează toate filtrele Distort și transformările din catalog,
  inclusiv intrările care necesită mai multe passes.
- [x] Păstrează transformul vizual în Prism: nu propagă dimensiuni noi în
  measure/arrange și nu modifică hitbox-ul.
- [x] Validează resursele auxiliare definite de sintaxa aprobată fără a
  reintroduce o proprietate generică `Source` ori shader filename.
- [x] Testează coordonate negative, scale extreme, margini, transparență,
  nested transforms și compoziții clipped/masked.

### Gate etapa 3

- [x] Toate filtrele de distorsiune au mapping și sampling verificat, iar inputul
  controlului rămâne sursa implicită.

## Etapa 4 — stylize, pixelate, render și restul catalogului

- [x] Implementează primitivele lipsă pentru edge detection, morphology,
  quantization, tiling, procedural patterns și operațiile multi-pass necesare.
- [x] Implementează toate filtrele Stylize, Pixelate, Render și orice altă
  familie aprobată în proposal/catalog; nicio intrare nu rămâne „TODO”.
- [x] Refolosește primitivele style/filter când operația matematică este
  identică, dar păstrează plannere separate când semantica publică diferă.
- [x] Testează determinismul procedural, alpha, bounds, ordinea chaining-ului,
  group isolation și interacțiunea cu mask/clipping/blend.
- [x] Generează o galerie de conformance din aceeași listă de catalog, fără
  view-uri scrise manual per filtru.

### Gate etapa 4

- [x] Matricea catalogului raportează zero filtre/proprietăți fără planner,
  kernel, test și documentație.

## Etapa 5 — optimizer și performanță

- [x] Marchează în catalog numai operațiile sigur fuzionabile și verifică
  diferențial outputul fuzionat față de passes separate.
- [x] Elimină filtrele no-op după valorile efective tipate și păstrează ordinea
  pentru operațiile necomutative.
- [x] Profilează scene reprezentative simple, chained și nested; măsoară passes,
  peak surfaces, CPU submit, GPU time, hit/miss retained și alocări după warmup.
- [x] Introdu praguri ori limite publice numai pe baza benchmarkurilor și
  documentează motivul; nu adăuga quality presets/adaptive quality necerute.
  (Nu a fost necesar: măsurătorile justifică gate-uri structurale, dar nu
  limite publice de timp portabile între GPU-uri.)
- [x] Verifică mii de frame-uri animate pentru bounded surface reuse și lipsa
  recompilării shaderelor ori construirii de graf per schimbare nonstructurală.

### Gate etapa 5

- [x] Optimizarea păstrează conformance-ul, iar scenariile statice și animate
  respectă bugetele stabilite prin măsurători.

## Etapa 6 — documentare și verificare

- [x] Generează referința filtrelor/proprietăților/defaults din catalog și
  păstrează explicațiile conceptuale scrise manual separat de datele generate.
- [x] Folosește skill-ul `writing-api-documentation` pentru orice tip public și
  sincronizează `docs-site/documentation/manifest.json`. (Audit efectuat cu
  skill-ul; nu a fost necesară o modificare: lotul nu schimbă API-ul public,
  iar manifestul are toate cele 926 de pagini existente.)
- [x] Rulează reindexarea după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj --filter Prism`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter PrismFilter`
  și `dotnet test .\Cerneala.slnx`.
- [x] Rulează galeria prin API-ul de captură automatizat și `git diff --check`.

## Definiția de gata

- [x] Fiecare filtru și proprietate aprobate în catalog au traseu complet de la
  markup la kernel, diagnostics, test și documentație.
- [x] Nicio listă paralelă, extensie publică terță sau runtime shader source nu
  a fost introdusă.
- [x] Conformance-ul și benchmarkurile sunt verzi.
