# Plan: API complet de desenare 2D pentru RenderSurface2D

Data: 2026-08-24
Status: finalizat

## Obiectiv

Extinderea completă a nucleului `Cerneala.Drawing` și expunerea lui prin `RenderSurface2DFrame`, astfel încât utilizatorul să poată construi orice grafică 2D rezonabilă fără workaround-uri, fără tipuri MonoGame în API-ul public și fără să scrie manual tessellatoare, management de render targets sau HLSL.

Planul acoperă toate capabilitățile identificate, nu doar subsetul recomandat:

- paths tipizate și SVG;
- fill rules și strokes complete;
- transformări și stivă de stare;
- clipping geometric;
- opacity, blend modes și layere;
- toate formele vectoriale uzuale;
- imagini avansate, quad-uri și nine-slice;
- triunghiuri, mesh-uri și batches;
- text layout complet și text cu runs stilizate;
- integrarea cu retained rendering, cache, damage tracking, `OnDemand`, Prism și device lifecycle;
- documentație publică, demo-uri, teste, benchmark-uri și diagnosticare.

## Principii obligatorii

- `RenderSurface2DFrame` rămâne o fațadă subțire peste `DrawingContext`; nu conține algoritmi de geometrie sau backend logic.
- Fiecare capabilitate fundamentală intră mai întâi în `DrawCommand`, `DrawingContext` și backend, apoi este delegată prin frame.
- API-ul public din `Cerneala.Drawing` folosește numai tipuri platform-neutral; nu expune `Texture2D`, `SpriteBatch`, `GraphicsDevice` sau alte tipuri MonoGame.
- Paths tipizate reprezintă substratul comun pentru formele compuse; nu se implementează un tessellator separat pentru fiecare helper.
- Payload-urile păstrate în command list sunt immutable sau versionate explicit, pentru equality retained, cache și damage tracking corecte.
- Drawing oferă compoziția 2D de bază; filtrele, style-urile, măștile și graph composition avansată rămân responsabilitatea Prism.
- Orice public API nou este documentat în `docs-site/documentation/classes/` folosind skill-ul `writing-api-documentation`, iar `manifest.json` este sincronizat.
- După fiecare batch de cod sau proiect se reindexează soluția cu RoslynIndexer.
- Nu se adaugă workaround-uri în demo sau UI pentru defecte care aparțin nucleului Drawing/backend.

## Baseline curent

- `DrawCommandKind` acoperă fill/draw rectangle, ellipse, line, SVG `FillPath`, text simplu, image, nested surface, clip dreptunghiular și scope Prism.
- `DrawingContext` reflectă aproape același set de comenzi.
- `RenderSurface2DFrame` validează lifetime-ul frame-ului, deleagă comenzile și urmărește dependențele de imagini; `DrawSprite` este helper peste image drawing.
- `SvgPathFlattener` înțelege `M/L/H/V/C/S/Q/T/A/Z`, dar rezultatul actual pierde distincția open/closed și este orientat spre fill.
- `MonoGamePathMeshBuilder` construiește în prezent mesh-uri de fill SVG cu `NonZero`.
- `Pen` conține doar brush și thickness; nu există caps, joins, dashes sau alignment.
- `PathGeometry` simulează stroke-ul prin secvențe de `DrawLine`, ceea ce nu poate produce joins/caps corecte.
- Clipping-ul explicit este doar dreptunghiular.
- Nu există transform stack, group opacity, blend state sau layer generic în drawing API.
- `DrawTextRun` conține text, font și size, fără layout multi-line/runs stilizate.
- Session-ul `RenderSurface2D` are retained comparison, damage tracking și `PreserveContents`; clipurile și Prism sunt deja tratate drept context-sensitive.

## Matricea completă a API-ului țintă

| Familie | Capabilități obligatorii |
|---|---|
| Path | path builder tipizat, move/line/quadratic/cubic/arc/close, open/closed contours, parser SVG, fill rule `NonZero` și `EvenOdd` |
| Stroke | brush, thickness, caps, joins, miter limit, dash pattern, dash offset, inside/center/outside alignment |
| State | transform push/pop, clip rect/path, opacity de grup, blend mode, isolated layer, scope-uri ergonomice |
| Rectangular | rectangle, rounded rectangle cu raze independente |
| Curves | ellipse/circle, arc, pie, chord |
| Polygonal | point, line, polyline, polygon, triangle, regular polygon, star |
| Images | source rect, tint, opacity, rotation, origin/pivot, flip, depth/order, point/linear sampling, address mode, arbitrary quad, nine-slice |
| Geometry GPU | triangle list/strip, textured/colored mesh 2D, point batch, line batch, sprite batch |
| Text | wrapping, max width/height, alignment, line spacing, max lines, trimming/ellipsis, styled runs, clipping și transform/rotation |
| Integration | retained equality, cache invalidation, damage bounds, `OnDemand`, Prism scopes/images, resource lifetime și device reset |

## Contracte publice propuse

### Paths și fill

- `DrawPath`: geometrie immutable, reutilizabilă, cu contours open/closed păstrate.
- `DrawPathBuilder`: `MoveTo`, `LineTo`, `QuadraticTo`, `CubicTo`, `ArcTo`, `Close`, `Build`.
- `DrawPathParser.ParseSvg(string)` pentru compatibilitate și migrare de la SVG string.
- `DrawFillRule`: `NonZero`, `EvenOdd`.
- `DrawingContext.FillPath(DrawPath, IDrawBrush, DrawFillRule)` și overload-uri color.
- API-ul SVG existent rămâne compatibil și este coborât către aceeași reprezentare tipizată/cache-uită.

### Stroke

- `DrawPen`: brush, thickness și `DrawStrokeStyle`.
- `DrawStrokeStyle`: start/end cap, join, miter limit, dash pattern, dash offset, alignment.
- `DrawLineCap`: `Flat`, `Square`, `Round`, `Triangle`.
- `DrawLineJoin`: `Miter`, `Bevel`, `Round`.
- `DrawStrokeAlignment`: `Inside`, `Center`, `Outside`.
- `DrawPath`, `DrawRectangle`, `DrawRoundedRectangle`, `DrawEllipse` și toate formele stroke-able acceptă `DrawPen`.

### Stare de desenare

- `PushTransform(System.Numerics.Matrix3x2)` / `PopTransform()`.
- `PushClip(DrawRect)` și `PushClip(DrawPath, DrawFillRule)` / `PopClip()`.
- `PushOpacity(float)` / `PopOpacity()` cu semantică de grup reală.
- `PushBlend(DrawBlendMode)` / `PopBlend()`.
- `PushLayer(DrawLayerOptions)` / `PopLayer()`.
- `DrawBlendMode`: cel puțin `Normal`, `Opaque`, `Additive`, `Multiply`, `Screen`.
- Scope-uri `ref struct` pentru `Transform`, `Clip`, `Opacity`, `Blend` și `Layer`, păstrând și API-ul raw push/pop.

### Forme

- `Fill/DrawRoundedRectangle`, cu `DrawCornerRadius` independent pe fiecare colț.
- `Fill/DrawPolygon`, `DrawPolyline`.
- `DrawArc`, `Fill/DrawPie`, `Fill/DrawChord`.
- `DrawPoint`, `FillCircle`.
- `Fill/DrawTriangle`, `Fill/DrawRegularPolygon`, `Fill/DrawStar`.
- `DrawPathFactory` produce paths reutilizabile pentru formele compuse.

### Imagini

- Păstrarea overload-urilor actuale `DrawImage` / `DrawSprite`.
- `DrawImageOptions`: source rect, tint, opacity, rotation, origin, flip, depth/order, sampling și address mode.
- `DrawSamplingMode`: `Point`, `Linear`.
- `DrawAddressMode`: `Clamp`, `Wrap`, unde backend-ul permite semantică stabilă.
- `DrawImageQuad` cu patru poziții și coordonate UV/source corespunzătoare.
- `DrawNineSlice` cu `DrawInsets`, validare și stretch determinist pentru cele nouă regiuni.

### Mesh și batches

- `DrawVertex2D`: position, color și UV.
- `DrawMesh2D`: vertices/indices immutable, topology validată și cel mult o imagine/textură platform-neutral per mesh.
- `DrawPrimitiveTopology`: `TriangleList`, `TriangleStrip`.
- `DrawTriangles` ca helper peste mesh.
- `DrawPointBatch`, `DrawLineBatch`, `DrawSpriteBatch` cu payload-uri immutable/versionate.
- Sprite batch-ul custom folosește o singură sursă/atlas per comandă; gruparea cross-texture rămâne responsabilitatea command list/backend.

### Text layout

- `DrawTextSpan`: text, font, size, brush/color și opțiuni tipografice relevante.
- `DrawTextLayoutOptions`: constraints, wrapping, alignment, line spacing, max lines, trimming și direcție.
- `DrawTextWrapping`, `DrawTextAlignment`, `DrawTextTrimming` și tipurile drawing-neutral necesare.
- `DrawTextLayoutBuilder` construiește un rezultat immutable, reutilizabil și măsurabil.
- `DrawingContext.DrawTextLayout(layout, origin)` înregistrează o singură comandă logică.
- Rotirea și transformarea textului folosesc transform stack-ul comun; clipping-ul folosește state API-ul comun.

## Ordinea și dependențele etapelor

1. Etapa 0 fixează baseline-ul și contractele.
2. Etapa 1 introduce modelul typed path și fill rules.
3. Etapa 2 depinde de etapa 1 și livrează strokes complete.
4. Etapa 3 depinde de etapele 1–2 și livrează starea de desenare.
5. Etapa 4 depinde de etapele 1–3 și livrează toate formele de conveniență.
6. Etapa 5 depinde de etapa 3 și livrează imaginile, mesh-urile și batches.
7. Etapa 6 depinde de etapa 3 și livrează text layout.
8. Etapa 7 integrează toate familiile cu retained rendering, Prism și lifecycle.
9. Etapa 8 finalizează documentația, demo-urile și validarea completă.

Etapele 4, 5 și 6 pot fi implementate separat după gate-ul etapei 3, dar toate trebuie finalizate înainte de etapa 7.

## Fișiere și zone estimate

### Nucleu Drawing

- `src/Cerneala/Drawing/DrawCommandKind.cs`
- `src/Cerneala/Drawing/DrawCommand.cs`
- `src/Cerneala/Drawing/DrawingContext.cs`
- tipuri noi în `src/Cerneala/Drawing/`
- paths și tessellation în `src/Cerneala/Drawing/Paths/`
- text layout în `src/Cerneala/Drawing/Text/`

### Backend și integrare

- `src/Cerneala/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- `src/Cerneala/Drawing/MonoGame/MonoGamePathMeshBuilder.cs`
- `src/Cerneala/Drawing/MonoGame/MonoGameRenderSurface2DSession.cs`
- `src/Cerneala/Drawing/Prism/Graph/PrismFrameAnalyzer.cs`
- `src/Cerneala/UI/Controls/RenderSurface2DFrame.cs`
- `src/Cerneala/UI/Rendering/DrawCommandListBuilder.cs`
- adaptoare UI existente pentru geometry, transform și text

### Teste și benchmark-uri

- testele Drawing existente și fișiere noi focalizate în `tests/Cerneala.Tests/Drawing/`
- testele `RenderSurface2D` în `tests/Cerneala.Tests/UI/Controls/`
- testele Prism relevante
- benchmark-uri în `benchmarks/Cerneala.Benchmarks/`

### Documentație

- `docs-site/documentation/classes/`
- `docs-site/documentation/manifest.json`
- demo-urile relevante, fără workaround-uri locale

## Etapa 0 — Baseline, inventar și teste RED

- [x] Regenerează `FileTree.md`, citește-l și indexează `Cerneala.slnx`.
- [x] Folosește RoslynIndexer pentru toate referințele și switch-urile `DrawCommandKind`.
- [x] Inventariază exact payload-urile, equality-ul, hashing-ul, bounds și resursele fiecărei comenzi curente.
- [x] Mapează fluxul complet `RenderSurface2DFrame` → `DrawingContext` → command list → backend/session → Prism analyzer.
- [x] Fixează în teste comportamentul public existent pentru toate primitivele deja disponibile.
- [x] Adaugă teste RED pentru paths open/closed, fill rules și parser SVG.
- [x] Adaugă teste RED pentru stroke caps/joins/dashes/alignment.
- [x] Adaugă teste RED pentru transform, clip geometric, opacity, blend și layer nesting.
- [x] Adaugă teste RED pentru fiecare familie de forme din matrice.
- [x] Adaugă teste RED pentru image options, quad, nine-slice, mesh și batches.
- [x] Adaugă teste RED pentru wrapping, alignment, trimming și styled text runs.
- [x] Adaugă teste RED transversale pentru retained cache, damage, `OnDemand`, Prism și device reset.
- [x] Confirmă că testele noi eșuează din cauza capabilităților absente, nu din cauza harness-ului.

### Gate etapa 0

- [x] Contractele matematice și de lifetime sunt explicite în teste.
- [x] Niciun API public existent nu este schimbat accidental.
- [x] Toate punctele de integrare sunt identificate înaintea primei modificări de cod.

## Etapa 1 — Typed paths și fill rules

- [x] Adaugă segmentele tipizate move, line, quadratic, cubic, arc și close.
- [x] Implementează `DrawPathBuilder` cu validare finite și state corect al contour-ului.
- [x] Implementează `DrawPath` immutable cu contours open/closed, bounds și identitate stabilă.
- [x] Implementează `DrawPathParser.ParseSvg` reutilizând parserul existent, fără două gramatici SVG paralele.
- [x] Păstrează overload-ul SVG existent și coboară-l către typed path.
- [x] Adaugă `DrawFillRule` și propagă-l prin command list, tessellator și backend.
- [x] Extinde tessellarea pentru multiple contours, holes, `EvenOdd` și `NonZero`.
- [x] Adaugă `FillPath(DrawPath, ...)` în `DrawingContext` și îl deleagă prin `RenderSurface2DFrame`.
- [x] Include path-ul tipizat în equality, cache keys, damage bounds și analiza Prism.
- [x] Migrează `Shape`/`SvgGeometry` către substratul comun acolo unde nu rupe API-ul public.
- [x] Rulează testele focalizate și reindexează soluția.

### Gate etapa 1

- [x] Paths open și closed sunt păstrate corect.
- [x] `EvenOdd` și `NonZero` produc rezultatele așteptate pentru holes și self-intersections.
- [x] SVG-ul existent trece aceleași teste de compatibilitate.
- [x] Un path reutilizat nu este reparsat sau realocat în fiecare frame.

## Etapa 2 — Stroke complet

- [x] Adaugă `DrawPen`, caps, joins, miter limit, dash pattern/offset și alignment.
- [x] Definește validarea pentru thickness, dash values, miter și inputuri non-finite.
- [x] Implementează stroke tessellation comun pentru linii și paths open/closed.
- [x] Implementează caps diferite la capetele path-urilor deschise.
- [x] Implementează joins miter/bevel/round și fallback-ul la miter limit.
- [x] Implementează dashes cu offset și continuitate pe segmente/curbe.
- [x] Implementează inside/center/outside pentru contours closed și definește explicit comportamentul pentru paths open.
- [x] Înlocuiește simularea `PathGeometry` prin secvențe `DrawLine` cu stroke-ul nativ.
- [x] Extinde `DrawLine`, rectangle, ellipse și path cu overload-uri `DrawPen` coerente.
- [x] Calculează bounds conservatoare pentru caps, joins, dashes și alignment.
- [x] Propagă stroke-ul în retained equality, damage, Prism și `RenderSurface2DFrame`.
- [x] Adaugă benchmark-uri pentru paths mari, dashed strokes și round joins.
- [x] Rulează testele focalizate și reindexează soluția.

### Gate etapa 2

- [x] Nu mai există workaround-ul `PathGeometry` bazat pe linii independente.
- [x] Caps, joins, dashes și alignment sunt corecte pentru transformări și scale.
- [x] Geometry/stroke cache nu produce false hit după schimbarea stilului.

## Etapa 3 — Transformări, clipping, opacity, blend și layere

- [x] Adaugă comenzile push/pop pentru transform și compunerea documentată a matricelor.
- [x] Folosește `System.Numerics.Matrix3x2` în Drawing și adaptoare explicite din UI transforms.
- [x] Aplică transformarea tuturor comenzilor existente și calculează bounds world-space.
- [x] Păstrează fast path-ul scissor pentru clipurile rectangulare axis-aligned.
- [x] Adaugă clip geometric cu `DrawPath` și fill rule.
- [x] Implementează intersecția clipurilor geometrice imbricate prin stencil/mask când este necesar.
- [x] Adaugă opacitate de grup reală, nu simpla multiplicare independentă a alpha-ului copiilor.
- [x] Adaugă blend modes de bază și stări MonoGame cache-uite.
- [x] Adaugă layere izolate și un pool intern de render targets cu evacuare deterministă.
- [x] Adaugă analizorul comun pentru state stack, nesting, bounds și context sensitivity.
- [x] Folosește același rezultat de analiză în backend, session, damage tracking și Prism analyzer.
- [x] Adaugă scope-urile publice `ref struct` și validează LIFO/double-dispose.
- [x] Respinge command lists dezechilibrate cu diagnostice care indică push-ul neînchis.
- [x] Restaurează starea GPU după pop, excepție, resize, device reset și dispose.
- [x] Rulează teste de nesting mixt și benchmark-uri pentru layere/clipuri, apoi reindexează.

### Gate etapa 3

- [x] Există o singură interpretare a stivei pentru backend, damage și Prism.
- [x] Clipul rectangular existent nu regresează și rămâne pe fast path.
- [x] Group opacity este corect pentru primitive suprapuse.
- [x] Nu există render-target leak sau stare GPU rămasă activă.

## Etapa 4 — Toate formele vectoriale de conveniență

- [x] Adaugă `DrawCornerRadius` și normalizarea razelor supradimensionate.
- [x] Implementează comanda dedicată și fast path-ul pentru fill/draw rounded rectangle.
- [x] Adaugă `DrawArcDirection` și fixează convenția unghiurilor în radiani.
- [x] Implementează `DrawPathFactory.Polygon` și `Polyline`.
- [x] Implementează `Arc`, `Pie` și `Chord`, inclusiv sweep minor/major și cerc complet.
- [x] Implementează point, circle și triangle helpers.
- [x] Implementează regular polygon și star, inclusiv fill rules pentru self-intersections.
- [x] Adaugă overload-uri brush/color și `DrawPen` consecvente.
- [x] Delegă fiecare formă prin `RenderSurface2DFrame` fără logică duplicată.
- [x] Documentează și testează inputurile degenerate/non-finite.
- [x] Testează bounds pentru extrema arcurilor, stroke exterior și transformări.
- [x] Adaugă benchmark-uri pentru rounded rectangles, poligoane mari și paths reutilizate.
- [x] Rulează testele focalizate și reindexează soluția.

### Gate etapa 4

- [x] Toate formele din matrice sunt accesibile din `DrawingContext` și frame.
- [x] Rounded rectangle are fast path; celelalte forme nu au tessellatoare paralele.
- [x] Reutilizarea unui factory path evită alocări geometrice în steady state.

## Etapa 5 — Imagini avansate, mesh-uri și batches

- [x] Adaugă `DrawSamplingMode`, `DrawAddressMode`, flip și celelalte tipuri platform-neutral necesare.
- [x] Adaugă `DrawImageOptions` păstrând overload-urile actuale compatibile.
- [x] Implementează source rect, tint, opacity, rotation, origin/pivot, flip și depth/order.
- [x] Aplică point/linear sampling și address mode fără recrearea stărilor per command.
- [x] Implementează `DrawImageQuad` prin două triunghiuri, cu UV-uri explicite.
- [x] Documentează că quad-ul este 2D și nu promite corecție perspectivă 3D.
- [x] Adaugă `DrawInsets` și implementează `DrawNineSlice` cu validare pentru regiuni mici.
- [x] Adaugă `DrawVertex2D`, `DrawMesh2D` și topology triangle list/strip.
- [x] Validează indicii, dimensiunile, topologia și lifetime-ul imaginii unui mesh.
- [x] Adaugă `DrawTriangles` ca helper peste același mesh path.
- [x] Implementează payload-uri immutable/versionate pentru point, line și sprite batches.
- [x] Integrează toate dependențele de imagine cu tracking-ul existent al `RenderSurface2DFrame`.
- [x] Calculează bounds/damage pentru quad-uri, mesh-uri și batches.
- [x] Integrează comenzile cu retained comparison, Prism scopes și `OnDemand`.
- [x] Eliberează resursele GPU la image dispose, surface dispose și device reset.
- [x] Adaugă benchmark-uri comparative pentru comenzi individuale versus batches.
- [x] Rulează testele focalizate și reindexează soluția.

### Gate etapa 5

- [x] Niciun public API nu expune tipuri MonoGame.
- [x] Batches reduc semnificativ command/draw overhead în benchmark-ul stabilit.
- [x] Schimbarea unei imagini sau versiuni de batch invalidează exact frame-ul necesar.
- [x] Nine-slice și sampling produc rezultate deterministe la scale fracționare.

## Etapa 6 — Text layout complet

- [x] Inventariază și reutilizează `LineBreakService`, shaping-ul, fallback-ul și rendererul de text existente.
- [x] Extrage algoritmii comuni în `Cerneala.Drawing.Text` fără duplicarea pipeline-ului UI.
- [x] Adaugă enum-urile drawing-neutral pentru wrapping, alignment și trimming.
- [x] Adaugă `DrawTextSpan` pentru text cu font, size și paint diferite per run.
- [x] Adaugă `DrawTextLayoutOptions` cu width/height constraints, line spacing și max lines.
- [x] Implementează `DrawTextLayoutBuilder` și rezultatul immutable cu lines/runs poziționate și bounds.
- [x] Implementează wrapping pe cuvinte/caractere conform contractului stabilit.
- [x] Implementează horizontal alignment și comportamentul RTL corespunzător.
- [x] Implementează trimming/ellipsis și max-lines fără ruperea clusterelor Unicode.
- [x] Implementează multiple styled runs, font fallback, combining marks, emoji și bidi.
- [x] Adaugă `DrawTextLayout` ca o singură comandă logică în context și frame.
- [x] Folosește transform stack-ul pentru rotation/scale și state clip pentru clipping.
- [x] Reutilizează layout-ul în UI text acolo unde elimină duplicarea fără breaking changes.
- [x] Cache-uiește layout-ul după conținut, fonturi, opțiuni, constraints și scale relevante.
- [x] Integrează bounds, retained comparison, `OnDemand` și Prism.
- [x] Adaugă benchmark-uri pentru layout reutilizat versus reconstruit.
- [x] Rulează testele Unicode/layout și reindexează soluția.

### Gate etapa 6

- [x] Drawing și UI nu conțin două implementări divergente de line breaking/shaping.
- [x] Wrapping, alignment, trimming și styled runs sunt corecte pentru LTR și RTL.
- [x] Un layout reutilizat nu este reshaped/reflowed la fiecare frame.

## Etapa 7 — Integrare transversală și lifecycle

- [x] Actualizează fiecare switch exhaustiv pentru toate noile `DrawCommandKind`.
- [x] Centralizează metadata per command: bounds, resurse, context sensitivity și retained identity.
- [x] Verifică nested surfaces care folosesc noile comenzi.
- [x] Verifică toate noile primitive în scope-uri Prism și ca surse pentru `PrismImage` unde contractul permite.
- [x] Verifică retained hits/misses și motivul invalidării pentru fiecare familie.
- [x] Verifică damage rect minimal pentru transformări, strokes, layers, text și meshes.
- [x] Verifică `OnDemand`: schimbarea payload-ului sau resursei cere frame nou; lipsa schimbării nu îl cere.
- [x] Verifică `Continuous` fără creștere necontrolată de cache/memorie.
- [x] Verifică dispose imediat și determinist pentru imagini, meshes, layouts, layers și cache entries.
- [x] Verifică resize, device lost/reset și recrearea resurselor.
- [x] Adaugă diagnostice pentru stack imbalance, invalid geometry, cache miss și resurse disposed.
- [x] Elimină orice workaround temporar devenit inutil în `PathGeometry`, demo-uri sau controale.
- [x] Rulează testele de integrare și stress, apoi reindexează soluția.

### Gate etapa 7

- [x] Toate familiile folosesc același model de lifecycle, bounds și cache.
- [x] Prism și RenderSurface2D nu au căi speciale divergente pentru aceeași comandă.
- [x] Nu există leak-uri CPU/GPU în testele de stress și device lifecycle.

## Etapa 8 — Documentație, demo-uri și verificare finală

- [x] Folosește skill-ul `writing-api-documentation` pentru fiecare tip și membru public nou/modificat.
- [x] Actualizează numai `docs-site/documentation/classes/` ca sursă de adevăr API.
- [x] Actualizează `docs-site/documentation/manifest.json` pentru toate paginile noi/renumite.
- [x] Documentează modelul mental: command list, state scopes, retained rendering, resources și Prism composition.
- [x] Documentează costurile și calea recomandată pentru paths/layouts reutilizate și batches.
- [x] Adaugă exemple izolate pentru fiecare familie din matrice.
- [x] Adaugă un demo integrat care desenează toate formele, strokes, clips, images, mesh, batches și text.
- [x] Demo-ul folosește `OnDemand` unde este potrivit și nu conține workaround-uri de invalidare.
- [x] Adaugă o scenă care aplică Prism peste rezultate desenate manual.
- [x] Capturează screenshot-uri exclusiv prin `Window.SaveScreenshot`.
- [x] Validează vizual la mai multe scale/DPI, inclusiv alpha edges, seams, clips și text.
- [x] Rulează benchmark-urile și înregistrează baseline/threshold-uri acceptate.
- [x] Rulează testele focalizate, întreaga suită și build-ul soluției.
- [x] Regenerează `FileTree.md` dacă structura s-a schimbat.
- [x] Reindexează soluția și rulează RoslynIndexer `doctor`.
- [x] Rulează `git diff --check` pe toate fișierele modificate.
- [x] Confirmă că nu există API public nedocumentat sau pagină absentă din manifest.

### Gate etapa 8

- [x] Fiecare rând din matricea API este implementat, testat, demonstrat și documentat.
- [x] Build-ul, toate testele, benchmark-urile acceptate și verificările vizuale sunt verzi.
- [x] Demo-urile folosesc numai API-uri publice și nu maschează defecte ale framework-ului.

## Strategia de testare

### Unit

- path building/parsing și immutability;
- fill rules, winding, holes și degenerări;
- stroke geometry, caps, joins, dashes și alignment;
- matrix composition, stack balance și bounds;
- arc/polygon/star/nine-slice math;
- mesh/batch validation și versioning;
- wrapping, bidi, fallback, cluster safety și trimming.

### Integrare

- command recording și frame lifetime;
- backend state transitions și resource restoration;
- retained hit/miss, damage și `OnDemand`;
- nesting transform/clip/layer/Prism;
- image dependency tracking și dispose;
- device reset și surface resize.

### Vizual

- golden scenes pentru fill/stroke și toate formele;
- clipuri și layere cu alpha suprapus;
- point versus linear sampling și nine-slice seams;
- meshes colorate/texturate și batch parity;
- text LTR/RTL, emoji, combining marks și styled runs;
- screenshots obținute exclusiv prin `Window.SaveScreenshot`.

### Performanță

- path parse versus path reutilizat;
- tessellation cache hit/miss;
- individual primitives versus batches;
- layer/render-target pool în steady state;
- text layout reconstruit versus reutilizat;
- frame neschimbat în `OnDemand` și `Continuous`.

## Comenzi de verificare

```powershell
.\Tools\scripts\New-FileTree.ps1
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
dotnet test .\Cerneala.slnx --filter "FullyQualifiedName~Drawing|FullyQualifiedName~RenderSurface2D|FullyQualifiedName~Prism"
dotnet test .\Cerneala.slnx
dotnet build .\Cerneala.slnx
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- doctor
git diff --check
```

## Non-obiective

- Nu se construiește un scene graph retained paralel cu command list-ul.
- Nu se introduce un `Sprite2D` sau un sistem ECS.
- Nu se expun shader/effect sau render-target management brut în API-ul public.
- Nu se implementează perspectivă 3D ori perspective-correct texture mapping pentru quad-uri.
- Nu se mută filtrele, style-urile și graph composition avansată din Prism în Drawing.
- Nu se construiește un editor rich-text, caret, selection sau input method în acest plan.
- Nu se adaugă primitive cu un singur caz de utilizare dacă pot fi exprimate fără cost relevant prin typed path/mesh.

## Riscuri și mitigări

- Explozia de overload-uri: se folosesc option objects și tipuri coerente, păstrând overload-urile scurte uzuale.
- Divergența între API și backend: frame-ul rămâne delegator, iar command metadata este centralizată.
- Alocări per frame: geometria, layout-ul și batch payload-urile sunt reutilizabile/immutable.
- Stale retained cache: orice payload mutabil are versiune explicită sau este copiat într-o reprezentare immutable.
- Tessellation inconsistentă: fill, stroke, clips și shape helpers împart aceleași primitive geometrice.
- Prea multe render targets: layerele folosesc bounds minimale, pooling și evacuare deterministă.
- Blend/alpha incorect: matematica premultiplied alpha este fixată în teste, nu evaluată doar vizual.
- Dublarea text layout-ului UI: algoritmii existenți sunt extrași și reutilizați, nu copiați.
- API prea mare livrat monolitic: fiecare etapă are gate și poate fi verificată separat, deși rezultatul este urmărit într-un singur plan.

## Definition of Done

- [x] Toate capabilitățile din matricea completă sunt livrate; niciuna nu rămâne doar recomandare.
- [x] `DrawingContext` conține implementarea publică fundamentală, iar `RenderSurface2DFrame` este o fațadă subțire completă.
- [x] Utilizatorul poate construi forme arbitrare prin typed paths și grafice eficiente prin meshes/batches.
- [x] Transform, clip, opacity, blend, layer și Prism se compun determinist.
- [x] Retained rendering, caching, damage tracking și `OnDemand` sunt corecte pentru toate primitivele.
- [x] Resursele CPU/GPU au lifetime determinist și rezistă la resize/device reset.
- [x] Nu există workaround-uri în UI sau demo pentru defecte ale Drawing/backend.
- [x] Toate API-urile publice sunt documentate în locația canonică și manifestul este sincronizat.
- [x] Testele unitare, de integrare, vizuale și de stress sunt verzi.
- [x] Build-ul complet, RoslynIndexer `doctor`, benchmark-urile acceptate și `git diff --check` sunt verzi.
