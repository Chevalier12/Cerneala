# Prism — culoare, blending și stiluri Photoshop

## Scop

Livrează semantica de culoare, blending, măști și cele zece familii de layer
styles aprobate, peste compozitorul GPU funcțional.

**Dependențe:** `2026-07-18-prism-markup-motion-and-lifecycle.md` și
`2026-07-18-prism-monogame-compositor.md`.

## Etapa 0 — matricea de acoperire

- [x] Generează din catalog matricea completă pentru color profiles, blend
  modes, advanced blending, `BlendIf`, masks, clipping și style types.
- [x] Adaugă pentru fiecare intrare cel puțin un test semantic RED și un caz
  vizual reprezentativ; testele nu repetă manual defaults din catalog.
- [x] Definește golden contractul: dimensiune, format, alpha premultiplicat,
  profil de culoare, seed, toleranță per canal și hardware/driver suportat.
- [x] Adaugă imagini analitice mici pentru cazurile în care rezultatul poate fi
  calculat exact, nu doar screenshot-uri „arată bine”.

### Gate etapa 0

- [x] Matricea eșuează automat dacă o intrare din catalog nu are kernel, test și
  documentație asociată.

## Etapa 1 — pipeline de culoare și alpha

- [x] Implementează conversiile generate pentru `LinearSrgb` implicit și
  profilurile selectabile aprobate, la intrarea și ieșirea compoziției.
- [x] Definește o singură convenție internă pentru alpha premultiplicat și
  aplic-o în capture, filters, styles, masks, blends și present.
- [x] Separă `Opacity` de `Fill` astfel încât layer styles să urmeze semantica
  Photoshop.
- [x] Testează culori transparente, edge pixels, zero/unu alpha, round-trip,
  compoziții nested cu profil diferit și lipsa double conversion.
- [x] Verifică diferențele CPU/GPU pe vectori de referință și documentează
  toleranța numerică justificată.

### Gate etapa 1

- [x] Toate kernelurile fundamentale au aceeași convenție de culoare/alpha, iar
  testele detectează halo-uri și aplicarea dublă a gamma.

## Etapa 2 — blending Photoshop

- [x] Implementează toate blend modes declarate în catalog, grupate prin
  primitive comune generate, nu prin shader separat copiat pentru fiecare mod.
- [x] Implementează advanced blending și `BlendIf` cu canalele, pragurile și
  tranzițiile definite în proposal/catalog.
- [x] Respectă izolarea grupului și `PassThrough`, ordinea bottom-up și
  combinația distinctă dintre layer opacity și fill.
- [x] Adaugă teste analitice pentru fiecare blend mode pe pixeli opaci,
  transparenți și parțial transparenți.
- [x] Adaugă conformance vizual pentru combinații de grup, mask, clipping chain,
  styles și blend modes nontriviale.

### Gate etapa 2

- [x] Fiecare blend mode din catalog are kernel și test verde, fără fallback
  silențios la `Normal`.

## Etapa 3 — măști și clipping

- [x] Implementează masca reală de layer, transformul ei, opacity/density,
  invert și feather conform proprietăților catalogului.
- [x] Implementează `ClipToBelow` ca lanț de alpha al layerului inferior,
  independent de mask și fără a transforma layerul într-un container.
- [x] Optimizează cazurile mask identitate/zero și clipping absent numai după
  testele de echivalență.
- [x] Extinde bounds pentru feather și verifică sampling la margini fără a
  schimba layout-ul sau hitbox-ul.
- [x] Adaugă golden-uri pentru mask + style, mask + transform, clipping chain și
  grupuri nested.

### Gate etapa 3

- [x] Mask și clipping au rezultate distincte, corecte și stabile, inclusiv la
  alpha parțial și bounds extinse.

## Etapa 4 — layer styles

- [x] Implementează primitive interne comune pentru distance/edge field,
  contour, gradient/pattern sampling și compositing; fiecare style își declară
  doar planul specific.
- [x] Declară determinismul, cacheability și versiunile resurselor pentru fiecare
  style/primitive, generate din catalog și consumate de dependency stamp.
- [x] Implementează din catalog `DropShadow`, `InnerShadow`, `OuterGlow`,
  `InnerGlow`, `BevelEmboss`, `Satin`, `ColorOverlay`, `GradientOverlay`,
  `PatternOverlay` și `Stroke`.
- [x] Respectă ordinea Photoshop dintre style-uri, `Fill`, layer content și
  opacity, inclusiv style-urile multiple de același tip dacă proposal-ul le
  permite.
- [x] Calculează bounds pentru shadow/glow/bevel/stroke prin aceleași primitive
  folosite de optimizer; nu dubla formulele în backend și analyzer.
- [x] Adaugă teste pentru toate proprietățile/defaults generate și golden-uri
  pentru fiecare familie, plus combinații mask/clipping/blend.

### Gate etapa 4

- [x] Toate cele zece familii din catalog sunt implementate, animate prin
  sloturile tipate și acoperite de teste fără shader/source duplicat inutil.

## Etapa 5 — performanță și verificare

- [x] Profilează scene cu multe layer styles și dovedește reuse-ul suprafețelor,
  absența readbackului și zero alocări managed după warmup.
- [x] Verifică optimizerul: elimină styles invizibile/no-op, dar păstrează
  ordinea și alpha pentru toate combinațiile testate.
- [x] Actualizează documentația publică cu skill-ul
  `writing-api-documentation` și manifestul pentru tipurile/proprietățile
  expuse.
- [x] Rulează reindexarea după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "PrismColor|PrismBlend|PrismStyle|PrismMask"`
  și `dotnet test .\Cerneala.slnx`.
- [x] Rulează toate capturile prin API-ul automatizat și `git diff --check`.

## Definiția de gata

- [x] Matricea catalogului este completă pentru culoare, blending, masks,
  clipping și toate style-urile.
- [x] Conformance analitic și vizual, performanța, API docs și gate-urile sunt
  verzi.
