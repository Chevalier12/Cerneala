# Prism — markup, Motion și lifecycle

## Scop

Compilează sintaxa Prism în definiții tipate, atașează instanța la element și
permite Motion să anime proprietăți Prism fără reflection ori lookup string la
runtime.

**Dependență:** `2026-07-18-prism-foundation-and-catalog.md`.

## Etapa 0 — contracte RED

- [x] Adaugă fixtures RED în `tests/Cerneala.Tests.SourceGen/Prism/` pentru
  resurse `<PrismComposition>`, `@prism $Resource(...)`, compoziții inline și
  exact cele opt directive aprobate.
- [x] Acoperă sintaxa completă: parametri tipați, layer/group bottom-up,
  filter/style/mask, backdrop ultim, proprietăți Photoshop și profil de culoare.
- [x] Adaugă cazuri RED pentru directive necunoscute, proprietăți greșite,
  nume duplicate, parametri lipsă, layer cu layer copil, mai multe backdrop-uri
  și backdrop care nu este ultimul.
- [x] Adaugă fixtures RED pentru căile Motion
  `$self.prism.Layer.Property`, `$owner.prism.Layer.Property` și
  `$Name.prism.Layer.Property`, inclusiv nume/proprietăți inexistente.

### Gate etapa 0

- [x] Testele separă erorile de parse, binding și emission, verifică span-uri
  exacte și nu eșuează din cauza unor fixtures CUI invalide fără legătură.

## Etapa 1 — parser și AST

- [x] Extinde infrastructura existentă `UiMarkupDirectiveParser` pentru
  delimitarea blocurilor, expresii și valori comune; ține gramatica Prism în
  `Cerneala.SourceGen/Prism/Syntax/`, nu într-un al doilea parser general.
- [x] Creează un AST Prism intern și mic care păstrează ordinea declarată,
  source spans și forma expresiilor, fără a importa tipurile runtime.
- [x] Acceptă numai directivele din catalogul limbajului Prism și numai copiii
  legali pentru fiecare context.
- [x] Adaugă diagnostice stabile `PRISM1xxx` pentru lexare/parse și teste
  snapshot pentru textul și locația fiecărui diagnostic.

### Gate etapa 1

- [x] Parserul recuperează după o eroare fără cascade inutile și refolosește
  sintaxa comună CUI în loc să dubleze reguli pentru `{}`, `=` și expresii.

## Etapa 2 — binder static

- [x] Adaugă în `Cerneala.SourceGen/Prism/Binding/` rezolvarea resurselor,
  parametrilor, numelor de layer/group/backdrop și proprietăților generate din
  catalog.
- [x] Validează tipurile și conversiile la compile time; nu emite reflection,
  dictionary string sau `dynamic` pentru cazurile valide.
- [x] Rezolvă proprietățile filter/style/mask direct la chei tipate și emite
  `PRISM2xxx` pentru simboluri, tipuri, domenii și capabilități invalide.
- [x] Validează nesting-ul, ordinea backdrop-ului, `ClipToBelow` fără layer
  inferior și coliziunile de nume înainte de emission.
- [x] Testează accesul din template/resource scope și două instanțe ale
  aceleiași resurse cu parametri diferiți.

### Gate etapa 2

- [x] Toate exemplele aprobate din proposal compilează, iar toate exemplele
  declarate ilegale primesc un singur diagnostic util la source span-ul corect.

## Etapa 3 — emission și atașare

- [x] Generează în `Cerneala.SourceGen/Prism/Emission/` definiții imuabile
  partajate și fabrici de `PrismInstance`; nu genera graf GPU sau cod MonoGame.
- [x] Adaugă un `PrismAttachment` intern în `UI/Prism/Runtime/` care implementează
  `IElementLifecycleBehavior`, după modelul `MarkupMotionSession`.
- [x] Atașează o singură instanță Prism per element, gestionează replacement-ul
  determinist și elimină toate referințele la detach/dispose.
- [x] Leagă expresiile dinamice la sloturile tipate și deconectează bindingurile
  când elementul sau template-ul este scos din arbore.
- [x] Adaugă teste generated-source și runtime pentru attach, detach, reattach,
  replacement, template recycling și două root-uri diferite.

### Gate etapa 3

- [x] Codul generat nu conține string dispatch în hot path, iar 10.000 de
  cicluri attach/detach nu păstrează elemente sau instanțe Prism vii.

## Etapa 4 — integrarea Motion

- [x] Extinde resolverul Motion existent cu segmentul `.prism` și generează
  acces static la instanță, nod numit și cheie de proprietate.
- [x] Refolosește schedulerul, specs, cancellation și lifecycle-ul Motion;
  Prism nu introduce un al doilea motor de animație.
- [x] Permite animația proprietăților numerice, culorilor, bool/Visible și
  enumurilor doar acolo unde catalogul definește interpolarea sau schimbarea
  discretă.
- [x] Invalidează doar prezentarea/compoziția pentru schimbările de valoare;
  nu reconstruieste layout-ul, hitbox-ul, `ElementRenderCache` ori topologia
  grafului dacă versiunea structurală nu s-a schimbat.
- [x] Adaugă teste RED/green pentru `$self`, `$owner`, element numit, anulare,
  replace, Hidden/Collapsed și scrierea unei valori deja curente.

### Gate etapa 4

- [x] O animație Prism rulează fără rebuild structural și este anulată o
  singură dată când owner-ul devine ne-randabil sau se detașează.

## Etapa 5 — vizibilitate și memorie

- [x] Conectează `Visible`, `Hidden`, `Collapsed`, detach și disposal la aceeași
  politică lifecycle; starea nerandabilă produce zero tick Motion și zero
  invalidare Prism.
- [x] Asigură reluarea deterministă la revenirea în arbore: valorile de bază și
  bindingurile sunt reaplicate, dar execuțiile anulate nu reînvie singure.
- [x] Adaugă teste de navigare repetată între chaptere, hide/unhide, resource
  replacement și GC cu `WeakReference`.
- [x] Măsoară alocările după warmup pentru un parametru animat și dovedește că
  nu apar closure-uri sau colecții per frame.

### Gate etapa 5

- [x] Testele nu găsesc Motion rămas activ, referințe reținute sau lucru Prism
  după ascunderea ori detașarea owner-ului.

## Etapa 6 — documentare și verificare

- [x] Actualizează proposal-ul și TDD-ul numai unde implementarea a clarificat
  semantică deja aprobată; orice schimbare de limbaj se întoarce la design,
  nu este strecurată prin cod.
- [x] Documentează toate API-urile publice noi/modificate în
  `docs-site/documentation/classes/` cu skill-ul
  `writing-api-documentation` și sincronizează manifestul.
- [x] Rulează reindexarea obligatorie după fiecare lot C#/proiect.
- [x] Rulează
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`,
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "Prism|Motion"`
  și `git diff --check`.

## Definiția de gata

- [x] Sintaxa aprobată compilează în cod tipat, cu diagnostics precise pentru
  toate formele invalide.
- [x] Motion, bindingurile și lifecycle-ul folosesc infrastructura existentă,
  fără motor paralel sau workaround în view.
- [x] Testele de source generation, invalidare, memorie și lifecycle sunt verzi.
