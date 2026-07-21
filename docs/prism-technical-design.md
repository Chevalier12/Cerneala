# Prism Technical Design Document

## Statut

Acest document descrie arhitectura tehnică implementată pentru Prism în Cerneala.
Compilatorul de markup, lifecycle-ul, graful de compoziție, executorul MonoGame,
pipeline-urile de culoare/blending/măști/styles, backdrop-ul, diagnostics și
cache-ul retained GPU cross-frame sunt implementate, testate și măsurate.
Rezultatele finale sunt în
[`2026-07-21-prism-integration-hardening.md`](../benchmarks/Cerneala.Benchmarks/results/2026-07-21-prism-integration-hardening.md),
iar contractul de utilizare este rezumat în [`prism-guide.md`](prism-guide.md).

Compozitoare Prism pentru alte backend-uri grafice, SDK-ul public pentru operații
third-party, compilarea shaderelor la runtime, adaptive quality, async compute și
un scheduler GPU generic sunt explicit amânate. Formulările despre ele sunt idei
de design, nu comportament livrat și nici muncă ascunsă a primei implementări.

Contractul de markup și modelul mental sunt definite în
[`prism-markup-syntax-proposal.md`](prism-markup-syntax-proposal.md). Catalogul
normativ de filtre, stiluri, blend modes, profiluri de culoare și sampling este
fișierul machine-readable
[`prism-catalog.json`](../Cerneala.SourceGen/Prism/Catalog/prism-catalog.json).
Acest TDD explică modul în care acel contract este compilat, executat și
diagnosticat.

În caz de contradicție:

- proposalul are prioritate pentru sintaxa și comportamentul observabil de autor;
- acest TDD are prioritate pentru separarea responsabilităților și implementarea
  internă;
- o contradicție trebuie rezolvată în ambele documente înainte de implementare.

## Clasificarea deciziilor

Documentul separă explicit patru tipuri de afirmații:

| Tip | Înseamnă |
| --- | --- |
| Cerință confirmată | comportament cerut de proposal și de modelul Photoshop |
| Decizie tehnică | soluție internă aleasă pentru cerințele confirmate |
| Ipoteză de validat | alegere care are nevoie de prototip, benchmark sau conformance test |
| Optimizare condiționată | nu se implementează până când profiling-ul nu demonstrează nevoia |

`BeginPrism`/`EndPrism`, definițiile immutable, render graph-ul, ownership-ul
MonoGame și cache-ul retained GPU cross-frame sunt decizii tehnice. Planning-ul
paralel, execuția asincronă și API-ul public pentru filtre third-party sunt
optimizări sau extensii condiționate, nu cerințe ale primei implementări.

Pentru prima implementare, scope-ul normativ este explicit: rezultatele GPU stabile
pot fi reutilizate între frame-uri pe baza unui dependency stamp complet, iar
catalogul built-in este închis la înregistrare publică. Forma semantică
`@filter Name` nu promite discovery sau kerneluri furnizate de aplicație. Aceste
decizii nu schimbă gramatica Prism.

O ipoteză nu devine default și o optimizare condiționată nu intră în criteriile de
acceptare fără dovadă măsurabilă și o decizie actualizată în acest document.

## Rezumat executiv

Prism este un compositor vizual declarativ pentru subtree-uri UI. Un control este
randat normal, capturat o singură dată ca imagine de bază, procesat printr-o
compoziție de layere și apoi desenat fără să schimbe layout-ul sau hitbox-ul.

Implementarea este împărțită în patru zone:

1. **Source generator**: parsează markupul, validează tipurile și generează
   definiții immutable și target-uri Motion tipizate.
2. **Runtime UI**: creează câte o instanță ușoară per element, păstrează parametrii,
   versionarea și lifecycle-ul, dar nu deține resurse GPU.
3. **Drawing composition**: transportă prin `DrawCommandList` scope-uri Prism
   backend-neutral și construiește un render graph ordonat.
4. **Backend MonoGame**: execută render graph-ul pe GPU, gestionează shader-ele,
   suprafețele temporare, cache-ul retained GPU, color management-ul și backdrop-ul.

Decizia structurală principală este folosirea a două comenzi balansate:

```text
BeginPrism
    comenzile normale ale elementului și ale subtree-ului său
EndPrism
```

Backend-ul poate astfel captura întregul rezultat vizual al controlului fără
`OnRender` custom, screenshot-uri, recursie în view sau cunoașterea arborelui UI.

## Obiective

- Implementarea completă a sintaxei `PrismComposition` și `@prism`.
- Model de layere compatibil mental cu Photoshop.
- Toate filtrele, stilurile și blend modes normative din proposal.
- Măști, clipping chains, groups, Blend If și advanced blending.
- Backdrop real pentru joc și UI-ul randat dedesubt.
- Integrare tipizată cu Motion și binding.
- Nicio modificare a măsurării, aranjării sau hit testing-ului.
- Nicio resursă GPU deținută de elemente UI.
- Zero CPU readback pentru capturi și backdrop.
- Comenzi locale retained reutilizabile în timpul animațiilor Prism.
- Diagnosticare suficientă pentru cost CPU, GPU, memorie și cache.
- Rezultate deterministe între frame-uri și predictibile între backend-uri.

## Non-obiective

- Prism nu este editor de imagini.
- Prism nu execută shader source introdus în markup.
- Prism nu implementează Neural Filters, Camera Raw, Digimarc sau operații cloud.
- Prism nu modifică layout-ul pentru a face loc umbrelor sau blurului.
- Prism nu creează elemente input independente pentru layere.
- Prism nu permite unui layer să citească arbitrar alt layer prin `Source`.
- Prism nu garantează rezultate identice byte-for-byte cu algoritmii privați Adobe.
- Prism nu mută responsabilități de rendering în controale sau code-behind.

## Suprafața de limbaj

TDD-ul implementează exact cele opt directive definite de proposal:

| Directivă | Rol tehnic |
| --- | --- |
| `@prism` | atașează o definiție reutilizabilă sau inline unui element |
| `@parameter` | declară un slot tipizat suprascriptibil și adresabil |
| `@layer` | declară un nod leaf care procesează rezultatul inferior |
| `@group` | declară un container explicit pentru layere și groups |
| `@filter` | adaugă o operație de procesare a pixelilor |
| `@style` | adaugă o decorație Photoshop derivată din content |
| `@mask` | limitează contribuția completă a unui scope |
| `@backdrop` | procesează jocul și UI-ul compus dedesubt |

`PrismComposition` este resursa reutilizabilă. Nicio directivă suplimentară nu este
necesară pentru implementarea descrisă aici.

## Constrângeri existente

Cerneala folosește în prezent:

- `ElementRenderCache` pentru comenzile locale ale fiecărui element;
- `DrawCommandListBuilder` pentru compunerea subtree-urilor într-o listă flat;
- `RetainedRenderer` pentru commit și submit;
- `IDrawingBackend` drept graniță backend-neutral;
- `MonoGameDrawingBackend` drept backend concret;
- `UiHost.Update` și `UiHost.Draw` drept contract de frame;
- Motion și generatorul de markup pentru proprietăți animate și target-uri statice.

Prism trebuie să extindă această arhitectură, nu să creeze un al doilea renderer UI.

## Contract semantic consumat de runtime

Contractul normativ complet este secțiunea
`Foundation Rendering Contract` din proposal. Runtime-ul, analyzer-ul și backend-ul
trebuie să păstreze aceleași reguli, fără interpretări locale:

- ordinea declarată este front-to-back ca în panoul Photoshop, iar evaluarea
  normală este bottom-up;
- sursa implicită este o singură captură immutable a subtree-ului controlului, iar
  numele nodurilor nu pot deveni surse;
- layer-ul este frunză, group-ul este singurul container, iar masca se aplică
  contribuției pregătite înainte de opacity și blend;
- `ClipToBelow`, `PassThrough`, `Visible`, `Fill`, `Opacity` și `BlendIf` păstrează
  exact ordinea și semantica definite în proposal;
- profilul implicit este `LinearSrgb`;
- Prism nu participă la layout, hit testing sau input.

Acest rezumat este o constrângere de implementare, nu o a doua definiție a
comportamentului. Orice schimbare semantică se face mai întâi în contractul normativ
din proposal și se reflectă aici în aceeași modificare.

## Principii obligatorii

### UI-ul descrie, backend-ul procesează

Elementul UI știe ce `PrismComposition` are atașat și care sunt valorile curente.
Nu știe ce este un `RenderTarget2D`, `Effect`, shader pass sau texture pool.

### Definiție partajată, stare per instanță

`PrismCompositionDefinition` este immutable și poate fi partajată de toate
elementele care folosesc aceeași resursă. Fiecare element primește propriul
`PrismInstance` și propriile valori de parametri.

### Nicio recapturare per layer

Subtree-ul controlului este executat o singură dată într-o suprafață de bază.
Layerele procesează rezultate intermediare, nu redesenează controlul.

### GPU-only pentru pixeli

Capturile, filtrele, măștile, blend modes și backdrop-ul rămân pe GPU. Citirea
pixelilor înapoi pe CPU este interzisă în calea normală de rendering.

### Fără lookup textual per frame

Generatorul transformă numele în identificatori numerici și chei tipizate.
Stringurile rămân doar pentru diagnostics și tooling.

### Degradare sigură

O capabilitate indisponibilă nu trebuie să facă UI-ul invizibil. În cel mai rău caz,
Prism este bypass-uit, iar controlul este desenat normal.

## Aplicarea DRY, YAGNI și SOLID

### DRY

- Catalogul machine-readable este singura sursă pentru identificatori, proprietăți,
  default-uri, limite și capabilități. Generatorul, runtime-ul, backend-ul și
  documentația consumă artefacte generate din el.
- Visibility, culling, bounds structurale și cerința de backdrop sunt calculate o
  singură dată în `PrismFrameAnalysis`. Graph builder-ul consumă rezultatul; nu
  repetă analiza.
- Regulile de fallback sunt centralizate într-un `PrismFallbackPolicy`, nu
  împrăștiate în kernels, planner și host.

### YAGNI

Implementarea obligatorie conține numai mecanisme cerute de sintaxa confirmată și
de backend-ul MonoGame actual. Nu include:

- discovery public pentru filtre third-party;
- planning paralel;
- un model generic de GPU fences sau execuție asincronă;
- degradare adaptivă automată a calității.

Acestea rămân posibile prin identificatori stabili și frontiere interne, dar se
proiectează numai după apariția unui caz real și a unor măsurători. Nu se construiește
o gară pentru trenul care poate, cândva, să treacă prin comună.

Cache-ul retained cross-frame nu este flexibilitate ipotetică: este o cerință
confirmată pentru ca un Prism static să nu recaptureze și să nu refacă aceiași pixeli
în fiecare frame. Implementarea sa rămâne strictă și specializată pentru rezultatele
GPU Prism; nu justifică un framework generic de caching sau scheduling.

### SOLID fără interface mania

- **SRP**: analyzer-ul analizează, graph builder-ul construiește, optimizer-ul
  optimizează, executorul execută, iar pool-ul deține suprafețe.
- **OCP**: operațiile built-in sunt înregistrate prin descriptori și planificatoare
  specializate; adăugarea uneia nu extinde un switch central gigantic.
- **LSP**: orice backend fără Prism poate ignora scope-urile și desena conținutul
  normal, fără să-i schimbe semantica.
- **ISP**: backdrop-ul rămâne un contract separat; `IDrawingBackend` nu primește
  metode pentru fiecare filtru, stil sau resursă.
- **DIP**: UI-ul și drawing composition depind de contracte backend-neutral;
  MonoGame implementează aceste contracte și deține detaliile GPU.

Se introduc interfețe numai la granițe cu substituție reală, lifecycle diferit sau
nevoie clară de test doubles. Clasele interne simple nu primesc câte o interfață de
dragul costumului și cravatei.

## Arhitectura de nivel înalt

```text
.cui.xml
    |
    v
Prism parser + semantic binder
    |
    v
cod generat
    |
    +--> PrismCompositionDefinition partajată
    +--> factory PrismInstance
    +--> chei Motion tipizate
    |
    v
UIElement + PrismAttachment
    |
    v
BeginPrism / comenzi subtree / EndPrism
    |
    v
PrismFrameAnalyzer
    |
    v
PrismGraphBuilder
    |
    v
PrismGraphOptimizer
    |
    v
PrismGraphExecutor
    |
    +--> shader registry
    +--> transient surface pool
    +--> diagnostics
    |
    v
GraphicsDevice
```

## Organizarea codului

Structura recomandată este:

```text
UI/Prism/
    Definitions/
    Runtime/
    Motion/
    Diagnostics/

Drawing/Prism/
    Commands/
    Graph/
    Catalog/
    Color/
    Hosting/

Drawing/MonoGame/Prism/
    Execution/
    Kernels/
    Shaders/
    Surfaces/
    Diagnostics/

Cerneala.SourceGen/Prism/
    Syntax/
    Parsing/
    Binding/
    Emission/

tests/
    Cerneala.Tests/Prism/
    Cerneala.Tests.SourceGen/Prism/
    Cerneala.Tests.MonoGame/Prism/
```

Responsabilitățile nu trebuie mutate între aceste directoare doar pentru comoditate.
În special, `UI/Prism` nu poate referenția MonoGame.

## Modelul de definiții

### PrismCompositionDefinition

Definiția reutilizabilă conține:

- identificator stabil;
- parametrii declarați și valorile default;
- working color profile;
- global light angle și altitude;
- lista ordonată de layere și groups;
- backdrop-ul opțional;
- tabela numelor adresabile;
- tabela sloturilor de proprietăți;
- hash structural pentru pipeline cache;
- bounds expansion maxim static, când poate fi calculat.

Definiția este immutable după creare. Nu conține referință la elementul care o
folosește și nu implementează `IDisposable`.

### Noduri

Arborele definiției folosește tipuri distincte:

- `PrismLayerDefinition`;
- `PrismGroupDefinition`;
- `PrismBackdropDefinition`;
- `PrismFilterDefinition`;
- `PrismStyleDefinition`;
- `PrismMaskDefinition`.

`PrismLayerDefinition` este întotdeauna leaf. Numai `PrismGroupDefinition` poate
conține layere sau alte groups.

Fiecare nod primește un `PrismNodeId` numeric stabil în cadrul definiției. Numele
opțional este păstrat separat pentru Motion și diagnostics.

### Proprietăți și parametri

Fiecare valoare este reprezentată printr-un slot tipizat:

```text
PrismPropertyKey<T>
PrismParameterKey<T>
```

Sloturile sunt dense și grupate pe tipuri uzuale:

- `bool`;
- `int`;
- `float`;
- vectori și matrici;
- culori;
- enum-uri;
- referințe immutable la imagini, gradients, patterns, LUT-uri sau curves.

Parametrii nu sunt dicționare de `string -> object`. Valorile complexe sunt
referințe tipizate și validate.

## PrismInstance

O instanță este creată pentru fiecare aplicare `@prism`.

Conține:

- referința la definiția partajată;
- stările tipizate și valorile parametrilor suprascriși;
- versiunile structurale și de valori.

`PrismAttachment` deține starea de atașare și subscriptions create de binding
factories. `MarkupMotionSession` deține execuțiile și handles Motion. Separarea
asta face ca `PrismInstance` să rămână un model de valori ușor, fără delegate sau
referințe la element.

Nu conține:

- textures;
- render targets;
- shader instances;
- rezultate filtrate;
- referințe către backend-ul MonoGame.

### PrismRenderState

`PrismRenderState` este handle-ul backend-neutral referit de comanda `BeginPrism`.
Este stabil pe durata atașării și conține:

- definiția immutable;
- bufferul dens de valori;
- `ValueVersion`;
- `VisibilityVersion`;
- `ResourceVersion`.

Valorile pot fi modificate numai pe thread-ul UI între update și draw. Backend-ul
le citește sincron în `Render`. Implementarea curentă planifică și execută pe
thread-ul de draw; nu introduce un pipeline CPU asincron sau stare mutabilă
partajată între frame-uri.

Această separare permite animațiilor Prism să schimbe parametri fără regenerarea
comenzilor locale ale controlului.

## Atașare și lifecycle

`PrismAttachment` implementează comportamentul standard de lifecycle al elementului.

### Attach

La atașare:

1. înlocuiește determinist și dispune orice attachment Prism anterior;
2. înregistrează un singur `PrismAttachment` ca lifecycle behavior;
3. creează `PrismInstance` din fabrica generată când elementul intră în arbore;
4. conectează binding factories numai dacă elementul este efectiv randabil;
5. revine curat dacă fabrica instanței sau un binding factory eșuează.

Integrarea ulterioară cu drawing atașează aceluiași transition
`PrismRenderState`, alocă `PrismCacheOwnerToken` fără referință inversă la element
și invalidează numai structura de compunere a root-ului. Aceste responsabilități
nu sunt mutate în `PrismAttachment`.

### Detach

La detașare:

1. sesiunea Motion anulează execuțiile asociate subtree-ului;
2. attachment-ul elimină binding-urile și subscriptions în ordine inversă;
3. elimină instanța curentă și toate referințele de lifecycle la disposal;
4. integrarea drawing publică tokenul într-o coadă backend-neutrală de invalidare
   consumată la următorul submit;
5. nu contactează direct backend-ul și nu eliberează direct resurse GPU.

Backend-ul indexează intrările retained după token/generație și nu ține o referință
puternică la `PrismInstance` sau `UIElement`. Dacă nu mai există un submit, dispose-ul
backendului eliberează oricum întregul cache.

### Visibility

Când elementul sau un ancestor devine `Hidden`, `Collapsed` ori
`IsVisible=false`:

- Motion pentru Prism este anulat sincron și o singură dată prin lifecycle-ul
  existent al subtree-ului;
- subscriptions generate de binding factories sunt deconectate;
- nicio comandă Prism nu este executată;
- nu se achiziționează backdrop;
- nu se alocă suprafețe;
- tokenul cache este marcat o singură dată ca imediat evictable;
- instanța veche poate rămâne doar ca stare inertă și nu mai primește scrieri.

Când elementul redevine efectiv randabil:

- fabrica generată creează o instanță nouă;
- binding factories se reconectează și reaplică valorile de bază și valorile
  surselor curente;
- execuțiile Motion anulate nu reînvie și trebuie declanșate din nou explicit.

Când `Visible=false` pe un layer sau group:

- nodul și subtree-ul său sunt bypass-uite;
- filtrele, stilurile și masca nu rulează;
- Motion activ pentru proprietățile acelui scope este anulat;
- o scriere externă sau un binding poate seta din nou `Visible=true`.

Un filter sau style cu `Visible=false` este eliminat din plan fără pass și fără
suprafață intermediară. Un backdrop cu `Visible=false` nu contribuie la cerințele
frame-ului pentru achiziția backdrop.

`Visible=false` nu este echivalent cu `Opacity=0`. Primul oprește munca; al doilea
păstrează semantica de compunere și poate fi folosit pentru tranziții.

## Invalidation

Prism introduce o invalidare de prezentare care nu reconstruiește comenzile locale.

| Schimbare | Efect |
| --- | --- |
| Atașare, detașare sau altă definiție | recompunere structurală a root command list |
| Parametru numeric, culoare, `Opacity`, `Visible` | increment `ValueVersion`, redraw |
| Resursă auxiliară schimbată | increment `ResourceVersion`, redraw |
| Bounds sau transform element | recompunere normală prin layout/render scope |
| Modificare conținut control | rebuild local cache existent |

Nu se folosește invalidarea `Render` obișnuită pentru fiecare tick al unui parametru
Prism, deoarece aceasta ar reconstrui inutil `ElementRenderCache`.

Se adaugă o categorie `Composition` în scheduler sau un semnal echivalent de
presentation-only. Hosturile care desenează fiecare frame o pot trata ca statistică;
hosturile on-demand o folosesc pentru a programa draw.

## Integrarea în DrawCommandList

### Comenzi noi

`DrawCommandKind` primește:

```text
BeginPrism
EndPrism
```

`BeginPrism` conține un `PrismDrawScope` backend-neutral:

```text
PrismRenderState
PrismCacheOwnerToken
ControlBounds
EffectiveTransform
PixelScale
StructuralVersion
ValueVersion
VisualContentVersion
```

`EndPrism` nu are payload. Scope-urile trebuie să fie balansate și pot fi nested.

### Compunerea subtree-ului

`DrawCommandListBuilder.AppendElement` emite:

```text
clip-uri de ancestor deja active
PushClip al elementului, dacă există
BeginPrism, dacă elementul are Prism
comenzile locale
copiii vizuali
copiii Presence aflați în exit
EndPrism
PopClip
```

Astfel:

- Prism capturează controlul împreună cu copiii săi;
- clip-urile explicite limitează rezultatul final;
- efectele pot extinde rezultatul dincolo de arranged bounds dacă nu există clip;
- nested Prism funcționează natural;
- backend-ul nu trebuie să cunoască `UIElement`.

Un backend fără suport Prism ignoră `BeginPrism` și `EndPrism`, dar execută comenzile
dintre ele. Controlul rămâne vizibil fără efecte.

## Contractul de frame

`IDrawingBackend` primește context explicit:

```csharp
public interface IDrawingBackend
{
    void Render(
        DrawCommandList commands,
        in DrawingFrameContext frame);
}
```

`DrawingFrameContext` conține:

```text
UiFrameId
ViewportSize
CoordinateScale
OutputColorProfile
PrismFrameAnalysis?
PrismCacheInvalidations
BackdropFrameLease?
DiagnosticsSink
```

Semnătura context-free existentă este înlocuită. Toate backend-urile și test
doubles trebuie actualizate în aceeași schimbare.

Înainte de achiziția backdrop, `PrismFrameAnalyzer` face singura trecere structurală
peste comenzi, clip stack și stările Prism. Produce un `PrismFrameAnalysis`
immutable care conține scope-urile vizibile, rezultatele de culling, bounds-urile
structurale, dependency stamp-urile, nodurile cacheable și `RequiresBackdrop`.

Hostul folosește `RequiresBackdrop` pentru achiziție, apoi transmite aceeași analiză
prin `DrawingFrameContext`. Graph builder-ul o consumă fără să recalculeze
visibility, culling sau cerința de backdrop. În debug, versiunile listei de comenzi
și analizei sunt verificate pentru a preveni folosirea unei analize stale.
Invalidările de owner/resource sunt drenate o singură dată în contextul frame-ului
și consumate de backend înainte de lookup.

## Ownership-ul SpriteBatch

`MonoGameUiHost` nu mai deschide un singur `SpriteBatch.Begin` în jurul întregului
UI. Prism trebuie să poată:

- schimba render target-ul;
- termina un batch înainte de un filter pass;
- executa unul sau mai multe full-screen passes;
- relua desenarea comenzilor normale;
- restaura state-ul GraphicsDevice.

Prin urmare, `MonoGameDrawingBackend.Render` devine proprietarul complet al
`SpriteBatch.Begin/End` și al tranzițiilor dintre passes.

Backend-ul salvează și restaurează starea pe care contractul hostului o declară:

- render targets;
- viewport;
- scissor rectangle;
- blend, rasterizer, depth-stencil și sampler states.

Restaurarea se face în `finally`, inclusiv când un kernel eșuează.

## Construirea render graph-ului

Pipeline-ul are responsabilități separate:

1. `PrismFrameAnalyzer` parsează structura scope-urilor, rezolvă visibility,
   culling și cerințele frame-ului.
2. `PrismGraphBuilder` transformă comenzile și analiza într-un graph semantic
   immutable.
3. `PrismGraphOptimizer` elimină no-op-uri și fuzionează passes fără să schimbe
   ordinea semantică.
4. `PrismGraphExecutor` execută graph-ul și gestionează exclusiv resursele GPU.

Semantica layer styles este tradusă de `PrismStylePlanner` într-un plan comun de
sampling și compoziție. Descriptorul generat al catalogului furnizează sloturile,
default-urile, determinismul, cacheability și versiunea dependenței. Backend-ul
consumă planul printr-o singură tehnică `LayerStyle`; cele zece familii nu au
surse shader copiate separat.

Nodurile principale sunt:

- primitive batch;
- clip;
- control capture;
- filter pass;
- style pass;
- mask pass;
- blend pass;
- color conversion;
- backdrop input;
- final composite.

Analyzer-ul, builder-ul și optimizer-ul sunt backend-neutral. Lucrează cu
descriptorii catalogului și nu referențiază MonoGame.

### Scope Prism

La `BeginPrism`, graph builder-ul memorează nodul de compoziție aflat dedesubt și începe
captura controlului. La `EndPrism`:

1. finalizează captura controlului;
2. pregătește backdrop-ul, dacă există;
3. evaluează stack-ul controlului bottom-up;
4. compune controlul procesat peste backdrop;
5. înlocuiește scope-ul cu rezultatul final.

### Nested Prism

Un scope interior este rezolvat înaintea scope-ului exterior. Backdrop-ul interior
vede nodul compus imediat înaintea controlului său, inclusiv jocul, conținutul
părintelui și sibling-urile inferioare. Nu vede propriul rezultat sau UI-ul superior.

## Evaluarea compoziției

Ordinea din markup este ordinea panoului Photoshop:

- prima declarație este vizual în față;
- ultima declarație este vizual în spate;
- execuția începe de jos și urcă.

Pentru un layer:

```text
rezultatul acumulat dedesubt
    -> filtre bottom-up
    -> prepared content
    -> Fill
    -> styles bottom-up
    -> mask
    -> ClipToBelow
    -> Opacity
    -> Blend If
    -> blend cu rezultatul inferior
```

### Groups

`PassThrough` permite copiilor să interacționeze direct cu rezultatul exterior.
Orice alt blend mode izolează grupul într-o suprafață, aplică filtrele, stilurile,
masca și opacity-ul grupului, apoi îl blenduiește ca o singură imagine.

### Clipping chains

Un layer cu `ClipToBelow=true` folosește alpha layerului-base inferior din același
scope. Lanțul se termină la primul sibling inferior neclipped. Generatorul respinge
lanțurile fără bază.

### Fill și Opacity

`Fill` reduce doar conținutul pregătit. Stilurile rămân vizibile.
`Opacity` se aplică rezultatului complet content-plus-styles.

### Blend If

`ThisLayerRange` și `UnderlyingRange` sunt transformate în rampe feathered. Evaluarea
se face în working color profile, pe canalul selectat, înaintea blend-ului final.

## Catalogul de operații

Lista completă și default-urile sunt păstrate în proposal și nu sunt duplicate în
acest document.

Implementarea internă grupează operațiile după primitive GPU reutilizabile:

- conversii de culoare și LUT;
- color matrix și curves;
- convoluții;
- blur separabil;
- neighborhood și morphology;
- resampling și transform;
- displacement și distortion;
- noise și procedural generation;
- edge detection;
- alpha derivation;
- distance field pentru styles;
- blend kernels.

Un filtru semantic poate produce unul sau mai multe passes. Markupul nu expune
această diferență.

### Registrul built-in

`PrismBuiltinCatalog` este sursa unică pentru:

- numele semantic;
- identificatorul stabil;
- categoria filter sau style;
- proprietățile și tipurile lor;
- default-urile;
- intervalele valide;
- capabilitățile necesare;
- strategia de bounds expansion;
- cheia kernelului backend.

Catalogul este descris într-un fișier JSON validat prin JSON Schema și inclus ca
`AdditionalFile` pentru source generator. Din el se generează descriptorii runtime
tipizați, registrul backend și tabelele catalogului din proposal. CI regenerează
artefactele și eșuează dacă există diff. Nu se mențin manual liste separate în
documentație, generator și backend.

### Extensibilitate publică amânată

Sintaxa rezervă forma simplă:

```text
@filter ChromaticAberration
```

Prima implementare nu publică atribute, discovery de assembly, kernel factories sau
un SDK third-party. Registrul intern deservește exclusiv catalogul built-in.
Identificatorii stabili și sintaxa nu blochează o extensie viitoare, dar API-ul
public se proiectează abia când există cel puțin un filtru real din afara
framework-ului și îi cunoaștem cerințele de packaging, backend și lifecycle.

Markupul nu acceptă shader source, `Program`, effect filenames sau `$Filter`.

## Compilarea markupului

Pipeline-ul source generator este:

```text
XML + directive text
    -> Prism lexer/parser
    -> Prism syntax tree
    -> semantic binder
    -> catalog validation
    -> symbol table
    -> C# emitter
```

### Parser

Parserul Prism este separat de parserul Motion, dar reutilizează infrastructura
existentă pentru:

- locații în fișier;
- valori literale;
- referințe `$`;
- assignments cu `=`;
- blocuri cu `{}`;
- diagnostic reporting.

AST-ul păstrează exact locația fiecărei directive, proprietăți și valori.

### Binder

Binder-ul rezolvă:

- resurse `PrismComposition`;
- parametri și overrides;
- tipuri filter și style;
- proprietăți și tipurile lor;
- namescope-ul layer/group/backdrop;
- ordinea și legalitatea nodurilor;
- referințe la imagini, masks, LUT-uri, gradients și patterns;
- target-uri Motion `.prism`.

Toată validarea structurală din proposal este build-time.

### Cod generat

Pentru o resursă reutilizabilă se generează:

- o definiție statică partajată;
- un factory de instanțe;
- o structură tipizată pentru overrides;
- identificatori numerici pentru noduri și parametri.

Pentru `@prism $Name(...)` se generează:

1. crearea instanței;
2. aplicarea overrides tipizate;
3. atașarea la element;
4. înregistrarea cleanup-ului în lifecycle.

Nu se generează reflection, parsare de stringuri sau dicționare per frame.

## Integrarea Motion

Target-ul:

```text
$self.prism.Highlights.SoftGlow.GlowRadius
```

este rezolvat la build-time în:

```text
element target
PrismNodeId path
PrismPropertyKey<float>
```

Generatorul validează:

- existența Prism pe element;
- fiecare segment de group/layer/backdrop;
- proprietatea sau parametrul final;
- tipul valorii;
- existența mixerului Motion.

Prefixele `$self`, `$owner` și `$Name` sunt rezolvate static. `$owner` este valid
numai într-un component template și folosește elementul owner păstrat de contextul
de emission; `$Name` trebuie să fie în același namescope. Codul emis accesează
direct instanța, `PrismNodeId` și slotul tipizat, fără reflection sau dispatch
textual.

### Scrieri discrete

`Visible`, valorile booleene, întregii și enum-urile suportate folosesc scrieri
discrete. Ele pot fi setate de Motion, dar nu sunt interpolate. Numerele și
culorile folosesc mixerele Motion continue existente. Pentru fade se animează
`Opacity`.

Bindingul Motion Prism este identificat de element și property ID-ul generat.
Înlocuirea instanței elimină bindingul vechi și creează unul pentru instanța
curentă. O scriere identică nu schimbă `ValueVersion` și produce zero invalidări
de prezentare.

### Anulare

- detașarea elementului anulează toate target-urile Prism;
- `Hidden`, `Collapsed` și `IsVisible=false`, inclusiv pe un ancestor, anulează
  sincron Motion pentru subtree;
- revenirea la starea randabilă nu repornește o execuție anulată;
- ascunderea unui group anulează Motion pentru descendenții săi;
- ascunderea unui layer sau backdrop anulează Motion din acel scope;
- Motion nu este păstrat în viață doar pentru că definiția este reutilizabilă.

## Color management

Default-ul este `LinearSrgb`.

Pipeline-ul este:

```text
profil sursă
    -> working profile Prism
    -> filtre, styles și blending
    -> output profile al hostului
```

Conversia finală se face o singură dată.

### Reprezentarea pixelilor

- alpha premultiplied;
- calcule floating point în intermediare;
- zero-alpha guard la unpremultiply;
- HSL blend modes lucrează pe culoare neasociată;
- masks sunt tratate ca date scalare, nu ca imagini color reinterpretate.

Contractul implementat folosește input și output sRGB la granița hostului.
`LinearSrgb`, `Srgb`, `LinearDisplayP3`, `DisplayP3` și `ScRgb` au câte un
kernel de intrare și unul de prezentare generate din simbolurile catalogului.
O compoziție nested este prezentată o dată în sRGB, apoi conversia de intrare a
părintelui rulează o dată; nu se aplică gamma de două ori.

Toate pass-urile primesc și produc RGBA premultiplicat. Conversiile fac
unpremultiply numai cât aplică transferul sau matricea, iar alpha zero produce
obligatoriu RGB zero. `Fill` scalează conținutul înaintea layer styles;
`Opacity` scalează rezultatul complet după styles și mask.

### Formate MonoGame

Ordinea implementată este:

1. `HalfVector4` pentru toate intermediarele Prism;
2. `Color` pentru output-ul SDR compatibil cu hostul;
3. format scalar pentru masks când platforma îl suportă;
4. `Color` pentru masks când nu există format scalar renderable.

`ScRgb` necesită un format floating-point. Dacă backend-ul nu îl suportă, Prism
raportează capabilitatea lipsă și bypass-uiește compoziția; nu comprimă silențios HDR
în SDR.

### Toleranța numerică

Referința CPU folosește `double`; shader-ele folosesc `float`, intermediarele
`HalfVector4`, iar output-ul golden este `R8G8B8A8_UNorm`. Gate-ul WindowsDX
acceptă maximum `2/255` pe fiecare canal. Pragul acoperă rotunjirea half-float,
evaluarea transferului pe GPU și cuantizarea UNorm finală, dar este suficient de
strict pentru a detecta halo-uri, o conversie lipsă sau aplicarea dublă a gamma.
Alpha zero și curățarea RGB asociată rămân exacte, nu doar în toleranță.

## Blend modes

Registrul construiește câte o tehnică `${BlendMode}Blend` pentru fiecare simbol
generat din catalog. Lipsa tehnicii este eroare de inițializare a pachetului de
shader-e; executorul nu remapează niciun mod necunoscut la `Normal`.

Shaderul folosește primitive comune pentru modurile separabile, luminozitate,
saturație, `ClipColor`, `SetLuminosity` și `SetSaturation`. Wrapper-ele tehnicilor
aleg primitiva, fără copii independente ale întregului shader. Modurile HSL fac
unpremultiply cu gardă la alpha zero și reasociază rezultatul înainte de scriere.

Pentru sursa și fundalul premultiplicate, cu valorile straight `Cs`, `Cb` și
alpha `As`, `Ab`, compoziția comună este:

```text
Ao = As + Ab - As * Ab
Co = Cs * As * (1 - Ab)
   + Cb * Ab * (1 - As)
   + B(Cb, Cs) * As * Ab
```

Intermediarele rămân `HalfVector4`, iar pass-ul scrie tot RGBA premultiplicat.
Setul complet de blend kernels cere profilul `ps_4_0` și feature level `10_0`;
aceste valori sunt parte din manifestul conformance WindowsDX.

`BlendChannels` selectează independent canalele straight RGB și alpha dintre
rezultatul compus și fundal. `Knockout` înlocuiește contribuția suprapusă cu
culoarea straight a sursei; diferența structurală dintre `Shallow` și `Deep` este
păstrată în graph și devine observabilă la traversarea grupurilor. Flagurile care
afectează styles, masks și clipping sunt snapshot-uite pe nodul final de
compoziție, nu recitite din starea UI în executor.

`BlendIf` produce două rampe lineare pentru fiecare interval
`(blackStart, blackEnd, whiteStart, whiteEnd)`: urcare de la zero la unu între
pragurile negre, platou, apoi coborâre de la unu la zero între pragurile albe.
Rampele `ThisLayerRange` și `UnderlyingRange`, evaluate pe canalul selectat în
working profile, se înmulțesc și scalează contribuția sursei înainte de
compoziția finală.

Toate modurile trebuie testate cu:

- alpha zero și unu;
- alpha parțial;
- negru, alb și valori peste unu în HDR;
- source și destination cu profile diferite;
- `Fill`, `Opacity`, masks și clipping chains.

`Dissolve` folosește hash determinist din coordonata pixelului, identificatorul
layerului și `DissolveSeed`. Seed-ul normalizat este trimis explicit shaderului,
iar aceeași intrare produce același pattern între frame-uri. Nu are voie să
pâlpâie.

Ordinea rămâne bottom-up. Un group cu `PassThrough` transmite fundalul exterior
copiilor; orice alt blend mode este o frontieră de izolare și compune grupul ca o
singură imagine. `Fill` scalează conținutul înaintea styles, iar `Opacity`
scalează contribuția completă înainte de blend.

## Styles

Styles folosesc alpha prepared content și nu recapturează controlul. Graph-ul
păstrează acest input pre-`Fill` prin muchia `StyleSource`, separat de `Content`,
astfel încât `Fill=0` ascunde conținutul, nu și stilurile. `Opacity` rămâne după
rezultatul complet content-plus-styles.

`PrismStylePlanner` consumă direct sloturile tipizate generate și produce planuri
pentru `DropShadow`, `InnerShadow`, `OuterGlow`, `InnerGlow`, `BevelEmboss`,
`Satin`, `ColorOverlay`, `GradientOverlay`, `PatternOverlay` și `Stroke`.
Executorul împachetează planul pentru o singură tehnică GPU `LayerStyle`, iar
registrul validează toate cele zece identificatoare de catalog la același kernel.

Primitivele interne includ:

- alpha dilation și erosion;
- un edge/distance field aproximativ comun;
- blur și offset sampling comune;
- contour lookup;
- gradient și pattern sampling;
- highlight/shadow lighting;
- compoziție RGBA premultiplicată cu blend modes.

`BevelEmboss` rămâne un singur style semantic; Contour și Texture sunt
subcomponente ale planului, nu layere ascunse. Resursele gradient/pattern intră în
dependency stamp cu versiunea lor; o resursă activă fără versiune stabilă face
nodul necacheable.

Aceeași funcție `PrismStylePlanner.ExpandBounds` este consumată de optimizer pentru
shadow, glow, bevel și stroke, iar executorul folosește aceeași geometrie de
sampling. Formulele nu sunt duplicate în analyzer sau backend.

Mai multe instanțe ale aceluiași style sunt păstrate și executate bottom-up în
ordinea declarată.

## Masks

Masca este evaluată după content și styles, înainte de opacity și blend.

Pașii sunt:

1. rezolvarea imaginii;
2. conversia canalului `Alpha` sau `Luminance`;
3. `Invert`;
4. feather;
5. density;
6. multiplicarea contribuției complete.

Feather mărește bounds-ul de sampling, dar nu layout-ul.

Resursa lipsă produce diagnostic și o mască complet opacă, astfel încât controlul
nu dispare accidental.

## Backdrop

Generatorul permite cel mult un `@backdrop`, numai ca ultim copil direct al
compoziției. Poziția lui în markup exprimă faptul că este planul vizual cel mai din
spate; executorul îl pregătește înainte de compunerea controlului.

### Contract de host

Hostul expune:

```csharp
public interface IBackdropFrameSource
{
    bool TryAcquire(
        in BackdropFrameRequest request,
        out BackdropFrameLease frame);
}
```

Lease-ul conține:

- `IBackdropSurface`;
- `ContentVersion`;
- dimensiunea în pixeli;
- transformarea screen-to-surface;
- profilul de culoare.

`MonoGameUiHostOptions` primește un `IBackdropFrameSource?`.

### Achiziție

`UiHost.Draw`:

1. rulează o singură dată `PrismFrameAnalyzer`;
2. dacă analiza cere backdrop, apelează `TryAcquire` cel mult o dată;
3. introduce lease-ul în `DrawingFrameContext`;
4. execută backend-ul;
5. eliberează lease-ul în `finally`.

Un backdrop ascuns, clipped-out sau aparținând unui element invizibil nu declanșează
achiziție.

### Compoziție

Suprafața hostului este importată read-only în render graph. UI-ul inferior este
adăugat în paint order. Fiecare backdrop citește nodul inferior exact din punctul
scope-ului său.

Feedback-ul este imposibil deoarece un nod poate depinde numai de noduri create
anterior.

### Host fără backdrop

Când sursa lipsește sau refuză achiziția:

- se omite numai planul backdrop;
- stack-ul controlului rulează normal;
- se emite `BackdropUnavailable` o singură dată per definiție și stare de host;
- nu se folosesc pixeli din frame-ul anterior.

## Render targets și pooling

`PrismSurfacePool` este deținut de backend.

Cheia unei suprafețe include:

- width și height;
- format;
- mip count;
- sample count;
- usage flags;
- color profile class.

Suprafețele sunt returnate la pool după ultima utilizare din graph. Reutilizarea se
face numai după ce backend-ul MonoGame garantează că GPU-ul nu le mai folosește,
folosind politica de reciclare sigură oferită de capabilitățile MonoGame curente.
TDD-ul nu introduce o abstracție generică de fences pentru backend-uri ipotetice.

`PrismRendererOptions` expune configurația implementată pentru:

- bugetul hard total al tuturor suprafețelor Prism;
- bugetul soft și numărul maxim de intrări pentru cache-ul retained;
- activarea dependency-diff diagnostics de development.

Benchmarkul de referință fixează 512 MiB hard, 256 MiB retained soft și 256 de
intrări. Testele unitare folosesc limite mici injectate. Presiunea transient
evacuează mai întâi intrările retained nepin-uite; dacă hard limit-ul tot nu poate
admite suprafața necesară, executorul raportează `PRISM7006` cu
`SurfaceAllocationFailed`, restaurează target-ul și state-ul hostului, eliberează
lease-urile și continuă comenzile interioare brute rămase. Nu există depășire
ascunsă sau sistem adaptiv de calitate.

## Cache-uri

### Pipeline cache

Cheia conține:

- hash-ul structural al definiției;
- backend și capability set;
- working/output profile class;
- quality level;
- shader package version.

Schimbarea valorilor parametrilor nu recompilă pipeline-ul.

### Partajare backdrop în frame

În interiorul unui singur frame, cheia de partajare conține:

- identitatea nodului inferior;
- `ContentVersion`;
- versiunile UI inferioare;
- regiunea extinsă;
- pixel scale și downsample level;
- profilele de culoare;
- prefixul de filtre și valorile sale;
- masca, dacă afectează rezultatul cache-uit.

Se poate reutiliza un downsample pyramid sau un prefix blur comun. Tint-ul, masca
sau opacity-ul diferit nu sunt împinse greșit în aceeași intrare. Toate intrările
și lease-urile acestei structuri expiră la sfârșitul draw-ului curent.

### Cache retained cross-frame

Prima implementare păstrează rezultate GPU Prism între frame-uri atunci când toate
dependențele care pot schimba pixelii sunt stabile. Cache-ul este backend-owned și
separat de pool-ul transient:

- pool-ul transient reciclează suprafețe scratch fără a păstra conținutul;
- cache-ul retained păstrează conținutul unei suprafețe și dependency stamp-ul său;
- o suprafață poate fi promovată din transient în retained numai după finalizarea
  cu succes a nodului;
- eviction-ul întoarce suprafața în pool sau o eliberează conform bugetului.

`PrismFrameAnalyzer` produce un `PrismDependencyStamp` compact, fără referințe la
elemente UI. Stamp-ul include:

- versiunea structurală a compoziției și identitatea stabilă a nodului;
- versiunea valorilor Prism sau fingerprint-ul valorilor pixel-affecting;
- tokenul unic, nerefolosit, al attachment-ului și versiunea agregată a rezultatului
  vizual al subtree-ului capturat;
- pentru backdrop, identitatea providerului, `ContentVersion` și versiunile tuturor
  nodurilor UI inferioare;
- identitățile și versiunile imaginilor, măștilor, LUT-urilor, patternurilor și
  resurselor auxiliare;
- bounds rasterizate, pixel scale și transformările care schimbă sampling-ul;
- working/output color profile, formatul suprafeței și sampling quality;
- backend capability set și versiunea pachetului de shader-e.

Versiunea vizuală agregată a subtree-ului se întreține incremental în retained UI:
o proprietate render-affecting, Motion, o resursă sau o schimbare de copil
incrementează generația locală și propagă invalidarea minimă spre scope-ul Prism.
Analyzer-ul nu traversează întregul subtree doar pentru a calcula cheia.

`PrismGraphOptimizer` marchează explicit nodurile cacheable. Un nod este eligibil
numai dacă operația este deterministă, toate resursele sale au versiuni și cheia
conține toate dependențele. Timpul curent, un seed implicit variabil, un provider
fără `ContentVersion` stabil sau o capabilitate necunoscută fac nodul necacheable.

Executorul verifică mai întâi rezultatul final. Un hit final sare capturarea
controlului și toate passes acoperite. La miss, verifică nodurile intermediare și
poate elimina numai prefixele acoperite de hit-uri valide. Un rezultat nou este
promovat după execuția completă; un frame eșuat nu poluează cache-ul.

Eviction-ul este LRU byte-budgeted și rulează numai pentru intrări care nu sunt
pin-uite de draw-ul curent. Intrările nu conțin owner, binding, Motion handle sau
delegate. Detach-ul și replacement-ul compoziției invalidează generația ownerului;
device loss, shader package, viewport, output profile și resource replacement
invalidează intrările afectate. `Hidden`/`Collapsed` fac zero lookup și zero
promotion, iar intrările lor devin imediat evictable.

Un singur accountant aplică bugetul hard cumulat pentru transient și retained.
Cache-ul retained are un soft cap și este primul evacuat când un pass corect are
nevoie de memorie transient. Corectitudinea frame-ului are prioritate față de
hit-rate, fără depășirea hard cap-ului.

Nu se introduce o abstracție generică de cache, task graph sau fences. Cache-ul este
specializat pentru suprafețe GPU Prism și respectă modelul sincron al backend-ului
MonoGame actual.

### Implementare și bugete confirmate

Implementarea MonoGame separă `PrismRetainedSurfaceCache` de `PrismSurfacePool` și
folosește un accountant comun pentru suprafețele transient și retained. Configurația
publică este `PrismRendererOptions`, transmisă direct constructorului
`MonoGameDrawingBackend` sau prin `MonoGameUiHostOptions.PrismRendererOptions`.
Valorile implicite măsurate sunt:

- 512 MiB pentru `SurfaceHardByteLimit`, aplicat tuturor suprafețelor Prism;
- 256 MiB pentru `RetainedCacheSoftByteLimit`;
- 256 pentru `RetainedCacheEntryLimit`;
- dependency-diff diagnostics oprite implicit.

Limitele rămân configurabile și sunt validate înainte de crearea executorului.
Limita retained sau limita de intrări setată la zero împiedică promovarea. Modul
cache-off există numai intern pentru conformance, diagnostics și benchmark; nu
adaugă directivă, proprietate de layer sau dialect markup.

`PrismRendererDiagnostics` expune snapshot-uri immutable cu hit-uri finale și
intermediare, miss/promotion/eviction și motivele lor, bytes și intrări curente,
peak bytes, intrări pin-uite și capturi/passes economisite. Clasificarea diferenței
de dependency stamp este calculată numai când development diagnostics sunt pornite;
calea implicită nu construiește diff-uri și nu alocă pentru ele per frame.

Benchmark-ul Release WindowsDX a rulat de trei ori pe NVIDIA RTX 2000 Ada la
256 x 144 și 640 x 360, pentru 12 scenarii cu cache on/off și câte 96 de frame-uri
măsurate după warmup. Toate contoarele de lucru, cache, suprafețe, alocări și
eviction au fost identice între rulări. Hit-urile statice au `0 B` managed,
controlul static a redus limita superioară GPU de la 2.332 ms la 0.340 ms, iar
24 de instanțe comune de la 46.503 ms la 0.991 ms la rezoluția medium. Cazurile
dinamice raportează explicit alocările de bookkeeping și nu pretind hit-uri.
Matricea completă, scaling-ul, justificarea bugetelor și dogfood gate-ul sunt în
[`2026-07-21-prism-integration-hardening.md`](../benchmarks/Cerneala.Benchmarks/results/2026-07-21-prism-integration-hardening.md).

## Bounds și clipping

Fiecare kernel declară o funcție de expansion:

```text
Expand(inputBounds, parameterValues) -> outputBounds
```

Exemple:

- shadow: offset plus spread și blur radius;
- Gaussian blur: support radius;
- transform: bounds-ul colțurilor transformate;
- displacement: deplasarea maximă;
- color adjustment: zero expansion.

Operation planners declară expansion-ul, iar graph builder-ul propagă bounds
bottom-up. Executorul alocă numai regiunea rezultată. Clamping-ul la viewport se
face după expansion.

Prism nu schimbă:

- `DesiredSize`;
- `ArrangedBounds`;
- hitbox-ul;
- route-ul input;
- focus sau accessibility.

Clip-urile explicite ale elementului și ancestorilor se aplică rezultatului final.

## Optimizări obligatorii

### Pass fusion

Se combină când rezultatul rămâne identic:

- color matrices consecutive;
- opacity și color multiply;
- conversii de culoare adiacente;
- anumite blend și mask operations;
- filtre no-op cu valori default.

Nu se schimbă ordinea semantică pentru a economisi passes.

### Blur

- kernel separabil;
- downsample controlat pentru raze mari;
- padding corect;
- reuse al pyramid-ului;
- quality level inclus în cheia cache.

### Batching

Comenzile primitive consecutive care nu traversează un scope, clip sau dependency
barrier rămân batch-uibile.

### Calea no-op

O compoziție în care toate nodurile sunt hidden sau no-op trebuie să se reducă la
desenarea normală a controlului fără captură offscreen.

Pentru layer styles, graph builder-ul nu emite stările `Visible=false`, iar
optimizer-ul elimină numai passes dovedite no-op de planificatorul comun generat
din catalog. `Opacity=0` este suficient pentru fiecare familie cu o singură
contribuție; `BevelEmboss` este no-op numai când atât highlight opacity, cât și
shadow opacity sunt zero. Aliasarea se face către inputul `Content`, elimină
`StyleSource`-ul rămas fără consumator și păstrează ordinea celorlalte styles,
`Fill`, layer opacity, mask, clipping și blend.

## Erori și fallback

| Situație | Comportament |
| --- | --- |
| Markup invalid | diagnostic build-time, fără cod Prism invalid |
| Kernel built-in lipsă | bypass operație, diagnostic runtime |
| Backdrop indisponibil | omit backdrop, control normal |
| Profil custom indisponibil | bypass compoziție, fără reinterpretare silențioasă |
| Hard limit suprafețe depășit | eviction retained nepin-uit; apoi `PRISM7006`/`SurfaceAllocationFailed`, restaurare host și reluarea comenzilor interioare brute rămase |
| Shader compilation/package lipsă | bypass operație și diagnostic o singură dată |
| Device reset/loss | golire resurse GPU și recreare lazy |
| Excepție în executor | restaurare state, control normal când poate fi reluat sigur |

Nu se afișează rezultate parțiale corupte și nu se reciclează un backdrop vechi.

## Diagnostics

### Build-time

Diagnosticile folosesc prefixul `PRISM`.

Grupele recomandate:

- `PRISM1xxx`: sintaxă;
- `PRISM2xxx`: structură layer/group/backdrop;
- `PRISM3xxx`: catalog și proprietăți;
- `PRISM4xxx`: resurse și color profiles;
- `PRISM5xxx`: target-uri Motion;
- `PRISM6xxx`: limite statice și capabilități cunoscute.

Fiecare diagnostic indică locația exactă și oferă un mesaj care descrie remedierea,
nu doar faptul că parserul s-a supărat.

### Diagnostics de execuție

Vederea operațională internă, activată numai pentru development diagnostics,
expune per frame:

- compoziții întâlnite și executate;
- layere/groups visible și bypass-uite;
- număr passes planificate, fuzionate și executate;
- capturi control;
- backdrop acquisitions;
- cache hits și misses;
- suprafețe în uz și peak bytes;
- pixels procesați;
- shader switches;
- fallback-uri și capabilități lipsă;
- timp CPU pentru planning;
- timp GPU când platforma oferă timestamp queries.

API-ul public `PrismRendererDiagnostics` expune snapshot-uri immutable cu
contoare de cache, capturi/passes economisite și utilizarea suprafețelor. Detaliile
de graph, Motion, backdrop și failure path rămân în vederea internă deterministă,
care redactează identificatorii GPU instabili și nu ține elemente UI în viață.
Calea implicită nu construiește dependency diff-uri și are zero alocări per frame
după warmup pentru Prism static.

### Graph dump

Diagnostics poate produce un dump textual determinist:

```text
Prism CardGlass
  Backdrop Glass
    Downsample x2
    GaussianBlur radius=24
    Color saturation=1.18
  Capture Control
  Layer SoftGlow
    OuterGlow size=18
  Composite
```

Dump-ul nu include pointere sau identificatori nondeterministici.

## Threading

- Markup definitions sunt immutable și thread-safe.
- `PrismInstance` se modifică numai pe UI thread.
- Source-ul backdrop este apelat numai în draw submission.
- Analyzer-ul, graph builder-ul și optimizer-ul rulează pe thread-ul de draw.
- Executorul și pool-ul respectă thread-ul GraphicsDevice.
- Nu se introduc lock-uri în hot path-ul UI curent.
- Nu se introduc task-uri de background sau ownership cross-thread în prima
  implementare.

## Testare

### Source generator

Teste obligatorii pentru:

- toate directivele și combinațiile legale;
- fiecare diagnostic structural din proposal;
- tipuri și default-uri din catalog;
- overrides și independența instanțelor;
- namescope și duplicate names;
- target-uri Motion valide și invalide;
- resurse application/window/template;
- cod generat fără reflection și lookup textual.

### Runtime UI

Teste unitare pentru:

- attach/detach idempotent;
- versiuni și sloturi tipizate;
- schimbări de parametri fără rebuild local;
- visibility și anulare Motion;
- resource invalidation;
- lipsa referințelor după detach.

### Analiză și render graph

Pipeline-ul backend-neutral trebuie testat fără GPU:

- analiza rulează o singură dată și este reutilizată de host și graph builder;
- ordine bottom-up;
- groups PassThrough și isolated;
- clipping chains;
- masks;
- Fill versus Opacity;
- Blend If;
- bounds expansion;
- nested Prism;
- backdrop dependency fără cicluri;
- pass fusion fără schimbare semantică;
- cache keys complete.

Un test de arhitectură verifică faptul că analyzer-ul, builder-ul și optimizer-ul nu
referențiază MonoGame. Un test de generare verifică faptul că descriptorii runtime,
registrul backend și tabelele documentate provin din același catalog.

### Backend MonoGame

Teste de integrare pentru:

- scope-uri balansate;
- restaurarea GraphicsDevice state;
- surface pooling;
- device reset;
- lipsa CPU readback;
- fallback pentru formate indisponibile;
- determinism pentru noise și Dissolve.

### Conformance vizual

Fiecare intrare din catalog are:

- o scenă minimă;
- o imagine de referință;
- profil și format declarate;
- toleranță numerică;
- cel puțin un caz cu alpha parțial.

Blend modes și styles au matrice de cazuri comune. Screenshot-urile sunt capturate
prin harness-ul/API-ul automat al repository-ului, nu manual.

### Teste backdrop

Golden tests acoperă:

- joc static;
- joc animat cu `ContentVersion` nou;
- UI inferior;
- nested Prism;
- mai multe controale care împart blur prefix;
- host fără source;
- source care schimbă profilul sau dimensiunea;
- viewport scale diferit.

### Teste cache retained

Testele compară întotdeauna outputul cache-on cu outputul cache-off și acoperă:

- al doilea frame identic produce hit final și sare capture/effect passes;
- schimbarea conținutului, parametrilor, resurselor, lower UI, pixel scale,
  profilului sau shader package produce miss;
- o scriere cu aceeași valoare nu invalidează intrarea;
- două controale cu definiție comună nu își împrumută rezultatul când stamp-urile
  diferă;
- hit intermediar și hit final păstrează același alpha, bounds și blend order;
- eviction LRU respectă byte budget-ul și nu evacuează o intrare pin-uită;
- hide/collapse fac zero lookup/promotion, iar detach/replacement/device reset
  invalidează corect;
- hash collision-ul nu poate valida singur un hit fără verificarea identității
  structurale și a dependency stamp-ului complet.

### Memory leak și stress

Testele repetă:

- 10.000 attach/detach;
- navigare între view cu Prism și view fără Prism;
- hide/unhide repetat;
- schimbare composition resource;
- device reset;
- backdrop source replacement.

După cleanup:

- elementele și instanțele trebuie colectabile;
- numărul de subscriptions revine la bază;
- cache-ul GPU respectă bugetul;
- pool-ul nu crește după warmup stabil;
- Motion diagnostics nu raportează noduri active orfane.

## Bugete de performanță

Gate-uri finale măsurate:

| Scenariu | Buget |
| --- | --- |
| Tree neschimbat, Prism static | `0 B` alocări managed per draw după warmup |
| Al doilea frame static identic | hit retained; zero capture și zero effect passes acoperite |
| Input pixel-affecting schimbat | miss obligatoriu și output identic cu cache-off |
| Parametru Prism animat | fără rebuild `ElementRenderCache` |
| Layer hidden | zero passes și zero surfaces pentru acel layer |
| Backdrop hidden | zero acquisition cauzată de acel backdrop |
| Planning pentru scenele standard | baseline înregistrat și prag aprobat înainte de merge |
| Pool stabil | memorie sub limita configurată după warmup |
| Cache retained stabil | byte budget respectat și hit rate raportat |
| Attach/detach stress | nicio creștere retained după GC și drain |
| Presentation Solar System | cold max 388.664 ms < 500 ms; warm p99 12.874 ms < 16.6667 ms |

Valorile provin din trei rulări Release ale matricei cu 12 scenarii, două
rezoluții și cache on/off. Dogfood-ul WindowsDX a rulat opt cicluri a câte 45 de
frame-uri pentru fiecare dintre cele șapte capitole Presentation. Cele două
eșantioane Solar warm peste target și maximul warm de 49.363 ms rămân vizibile în
JSON; gate-ul folosește p99 și nu ascunde spike-urile prin adaptive quality.
Setup-ul și toate valorile sunt în benchmarkul de integration hardening, nu într-o
captură aleasă convenabil.

### Dovada curentă pentru layer styles

Gate-ul automat WindowsDX folosește o scenă cu 48 de passes `ColorOverlay`.
După opt frame-uri de warmup și un frame de stabilizare după GC, măsoară 16
frame-uri consecutive și cere simultan:

- `0 B` alocări managed pe thread-ul de draw;
- niciun render target nou după warmup;
- creșterea contorului de suprafețe reutilizate;
- zero lease-uri active după fiecare frame;
- un peak de suprafețe live mai mic decât numărul de styles.

Un test de arhitectură separat scanează calea de producție
`Drawing/MonoGame/Prism/**/*.cs` și respinge apelurile `GetData` și
`GetBackBufferData`. Astfel, măsurarea surface reuse și contractul fără CPU
readback rămân verificabile în CI, nu doar observații dintr-o sesiune de
profiling.

## Securitate și robustețe

- Niciun shader source din markup.
- Nicio cale de fișier arbitrară trimisă direct backend-ului.
- Dimensiunile, razele, numărul de passes și kernelurile sunt limitate.
- Matricile și valorile floating-point resping `NaN` și infinity.
- Catalogul built-in are identificatori stabili și schema validată.
- Analyzer-ul detectează overflow-ul bounds și costurile imposibile înainte de
  alocare.
- Backdrop surfaces sunt read-only pentru Prism.

## Rezultatul compatibilității API

Comparația finală cu [`prism-public-api-baseline.md`](prism-public-api-baseline.md)
clasifică schimbările astfel:

- `IDrawingBackend.Render(DrawCommandList)` devine
  `Render(DrawCommandList, in DrawingFrameContext)`. Este un breaking change
  necesar pentru contextul unic creat de host, lease-ul backdrop frame-scoped și
  analiza executată o singură dată.
- `IUiBackend.BackdropFrameSource` este un default interface member care întoarce
  `null`; backend-urile existente nu primesc o nouă obligație de implementare.
- `MonoGameUiHostOptions.BackdropFrameSource` și `PrismRendererOptions` sunt
  adăugări opționale.
- `BeginPrism`/`EndPrism`, tipurile publice de authoring/runtime/hosting și cheile
  Motion tipizate sunt aditive. Consumatorii care fac switch exhaustiv pe
  `DrawCommandKind` trebuie să aibă un caz implicit pentru valori noi.
- Tipurile de graph, analysis și planning, constructorii de context și requirement
  graph-bearing expuși temporar în timpul dezvoltării au fost internalizați. Este
  o rupere source/binary numai pentru consumatorii suprafeței Prism pre-release și
  este necesară pentru ca ownership-ul analizei să rămână în host/framework.

SDK ApiCompat rulat între assembly-ul de la `HEAD` și assembly-ul final raportează
exact 28 de `CP0001` pentru tipurile graph/planning internalizate și 5 `CP0002`
pentru constructorii/proprietățile/metoda graph-bearing eliminate. Nu există altă
rupere neclasificată; schimbarea mai veche a semnăturii `IDrawingBackend` este
acoperită separat de baseline-ul pre-Prism de mai sus.

Nu există API public pentru extensii third-party. Toate cele 56 de tipuri publice
Prism sau extinse de Prism au pagină în `docs-site/documentation/classes/` și
intrare în manifest.

## Ordine recomandată de implementare

1. Modelul immutable, catalogul și validarea catalogului.
2. Parser, binder, diagnostics și cod generat.
3. `PrismInstance`, lifecycle, parameter store și target-uri Motion.
4. `BeginPrism`/`EndPrism` și integrarea retained fără GPU.
5. Frame analyzer, graph builder, optimizer, validation și bounds propagation.
6. Ownership-ul SpriteBatch, surface pool și executorul MonoGame.
7. Color pipeline, Normal blend, masks și structura layer/group.
8. Toate blend modes și toate styles.
9. Catalogul complet de filtre și conformance images.
10. Backdrop source și ordered lower-UI composition.
11. Dependency stamps, cache retained GPU cross-frame, invalidare și eviction.
12. Diagnostics, benchmarks, stress, device loss și documentație publică.

O etapă nu este considerată terminată dacă lasă workarounds în view-uri sau resurse
fără ownership clar.

Planning-ul paralel și extensiile publice third-party nu sunt etape ascunse în
această listă. Fiecare cere un caz real, măsurători și o decizie separată. Cache-ul
cross-frame este etapă obligatorie și nu poate fi mutat în backlog.

## Criterii de acceptare

Prism este implementat complet când:

- sintaxa normativă compilează și toate exemplele proposalului funcționează;
- toate filtrele, stilurile și blend modes din catalog au implementare și tests;
- source generator-ul respinge toate structurile ilegale;
- Motion poate targeta static parametri și proprietăți Prism;
- hide/collapse/detach opresc munca și Motion-ul asociat;
- controlul este capturat o singură dată per evaluare;
- nicio cale normală nu face CPU readback;
- backdrop-ul vede jocul și UI-ul inferior fără feedback;
- layout-ul și hitbox-ul rămân neschimbate;
- custom backends pot face bypass sigur;
- analiza structurală este unică și reutilizată pentru backdrop și graph;
- un Prism static produce hit retained și sare capture/pass-urile acoperite, iar
  orice schimbare pixel-affecting produce miss și rezultat corect;
- cache-ul retained respectă bugetul, eviction-ul, lifecycle-ul și device reset fără
  a ține elemente UI în viață;
- catalogul generează descriptorii, registrul backend și documentația fără liste
  paralele;
- MonoGame respectă limitele validate prin benchmark și restaurează GraphicsDevice
  state;
- testele golden, stress, memory și device reset sunt verzi;
- diagnostics explică passes, cache și memorie;
- documentația publică este sincronizată.

## Riscuri principale

### Explozia numărului de passes

Catalogul este mare. Fără pass fusion, bounds regionale și downsampling, o
compoziție aparent simplă poate deveni prea scumpă.

### State management MonoGame

Schimbarea render targets și a `SpriteBatch` poate corupe rendering-ul jocului dacă
state-ul nu este restaurat riguros.

### Cache incorect

O cheie incompletă produce pixeli vechi sau împrumută rezultatul altui control.
Corectitudinea are prioritate față de hit rate.

### Diferențe între platforme

Formatele renderable, precision și shader profile diferă. Capability checks și
conformance tolerances trebuie să fie explicite.

### Catalog duplicat

Dacă generatorul, runtime-ul și backend-ul mențin liste separate, ele vor diverge.
Catalogul unic generat este obligatoriu.

### God object în planning

Dacă analiza, semantica operațiilor, optimizarea și execuția ajung într-o singură
clasă, orice filtru nou va modifica același nucleu fragil. Separarea analyzer,
builder, optimizer și operation planners este obligatorie.

### Supra-arhitecturare

Extensibilitatea publică și concurența pot produce mult cod fără valoare demonstrată.
Ele rămân în afara implementării până când un caz real și profiling-ul justifică
separat complexitatea. Cache-ul retained cerut rămâne specializat pentru Prism și nu
devine pretext pentru un framework generic.

### Lifecycle

Bindings, Motion handles sau cache-uri care țin elementul în viață ar recrea exact
genul de memory leak pe care arhitectura trebuie să îl prevină.

## Decizia finală

Prism este implementat ca extensie a pipeline-ului retained și a backend-ului de
drawing, nu ca efect atașat care se randează singur.

Markupul produce o definiție immutable. Elementul deține numai o instanță ușoară.
Lista de comenzi delimitează subtree-ul. Analyzer-ul produce o singură descriere a
frame-ului, graph builder-ul o transformă în graph, iar optimizer-ul îl simplifică.
Backend-ul procesează GPU-only și deține toate resursele temporare și retained.

Această separare păstrează sintaxa simplă, permite puterea modelului Photoshop și
evită să transformăm fiecare control într-un mic renderer improvizat.
