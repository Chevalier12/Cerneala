# Plan: unificarea completă a runtime-ului Aspect

> Data: 2026-08-27
>
> Status: finalizat
> Scop: eliminarea căii runtime paralele `MarkupAspectResource` și coborârea tuturor formelor de authoring Aspect într-un singur model de reguli rezolvat de `AspectEngine`, fără shims de compatibilitate.

## 1. Decizii aprobate

- `AspectPackage` / `AspectCatalog` / `AspectEngine` devin singurul owner pentru matching, cascade, condiții, dependencies, diagnostics și aplicarea valorilor Aspect reutilizabile.
- Markup-ul compilează în modelul Aspect comun; nu mai emite un executor paralel bazat pe `Action<UIElement>`.
- `ElementAspect` rămâne adaptorul per-inst​​anță pentru aspectele named/inline și pentru `ItemsControl.ItemContainerAspect`, dar valorile lui sunt rezolvate de engine, nu aplicate direct de `UIElement`.
- Motion, bindings, event handlers și presence/layout behaviors rămân la subsistemele lor. Ele pot fi generate ca sidecars explicite, dar nu pot scrie valori de stil printr-un al doilea resolver Aspect.
- Breaking changes sunt permise. `MarkupAspectResource` și benzile de precedență create numai pentru calea paralelă se elimină; nu se păstrează wrappers sau shims permanente.
- Sintaxa `.crn` existentă și capabilitățile utilizatorului rămân funcționale, cu excepția API-ului runtime `MarkupAspectResource`, aprobat explicit pentru eliminare.

## 2. Baseline și problema actuală

Runtime-ul are în prezent trei căi de aplicare:

1. `AspectRegistry` -> `AspectCatalog` -> `AspectEngine`, care deține modelul modern de reguli, layere, specificitate, tokens, states, variants, slots, dependencies și diagnostics.
2. `ElementAspect`, aplicat direct de `UIElement` prin `LocalAspectBase` și `LocalAspectConditional`.
3. `MarkupAspectResource`, executat o singură dată prin `Action<UIElement>`, descoperit separat de `UIRoot.ApplyApplicationAspects` și `UIRoot.ApplyLocalAspects` și susținut de `ApplicationAspectBase` / `ApplicationAspectVisualState`.

Source generatorul are propriul model intern `AspectResource`, propriul matching `TargetType`, propria emisie de condiții și propria aplicare `SetValue`. Nu emite `AspectPackage`, `AspectRuleSet`, `AspectDeclaration` sau `AspectCondition`.

Înaintea unificării trebuie reparate patru încălcări confirmate ale contractului engine-ului:

- o condiție poate fi evaluată de două ori și înaintea filtrării tipului/slotului;
- `AspectSlot<TOwner,TTarget>` și `AspectVariantKey<TOwner,TValue>` nu impun tipurile declarate;
- `AspectRegistry.Packages` permite mutarea listei interne fără incrementarea versiunii;
- construirea unui catalog mută `AspectRuleSet.PackageName`, deci un catalog vechi nu este snapshot stabil.

## 3. Obiective observabile

- Orice Aspect implicit, named, inline, application-level, element-scoped sau code-first este reprezentat prin declarații și reguli comune.
- `AspectEngine` efectuează o singură dată target filtering, condition evaluation, cascade și dependency capture pentru fiecare regulă relevantă.
- Scopurile application/element și aspectul per-inst​​anță sunt intrări explicite în rezoluția engine-ului, nu executori direcți.
- Precedența este una singură și documentată: theme/framework < application < scoped outer-to-inner < named/inline element Aspect < markup/local/animation conform contractului `UiPropertyStore`.
- Schimbarea unui resource scope, a unui `ElementAspect`, a unei stări, a unui token sau a unei dependențe reactive programează recomputarea prin coada Aspect și revine la idle după procesare.
- Generated markup nu conține `MarkupAspectResource`, `ApplyTo`, `ApplicationAspectBase`, `LocalAspectBase` sau assignments Aspect directe.
- `MarkupAspectResource` dispare din assembly, documentație și manifest.
- Toate scenariile markup existente pentru defaults, named aspects, inline aspects, templates, conditions și Motion păstrează comportamentul verificat.

## 4. Non-obiective

- Nu se schimbă sintaxa `.crn` doar pentru a simplifica implementarea.
- Nu se mută event routing, bindings, Motion, presence, layout sau input în `AspectEngine`.
- Nu se introduce runtime parsing pentru markup.
- Nu se adaugă CSS selectors, `ResourceDictionary` WPF-like, `BasedOn` sau string selector soup.
- Nu se păstrează două runtime-uri sub forma unei „migrări temporare” la final.
- Nu se optimizează pe intuiție; orice cache sau index nou trebuie justificat de măsurători.

## 5. Arhitectura țintă

### 5.1 Model comun

`AspectRuleSet`, `AspectTarget`, `AspectCondition`, `AspectDeclaration`, `AspectValue`, `AspectMotion` și `AspectPackage` rămân IR-ul runtime canonic. Generatorul poate emite cod care construiește aceste tipuri sau un adaptor public minim care produce exact aceste tipuri; nu se creează o a doua ierarhie publică de declarații.

### 5.2 Surse de reguli

Pentru fiecare element, `AspectProcessor` compune determinist:

1. catalogul root (`DefaultAspectPackage` și packages înregistrate explicit);
2. packages din resursele aplicației;
3. packages din scopes ancestrale, de la exterior spre cel mai apropiat scope;
4. `ElementAspect` al instanței.

Scope-ul și originea devin metadata de rezoluție deținute de catalog/engine. Nu se scriu pe obiecte `AspectRuleSet` reutilizabile.

### 5.3 Cascade

Cascade key-ul trebuie să includă numai coordonate canonice și stabile: layer, rangul sursei/scope-ului, specificitate și declaration order. Un scope mai apropiat bate unul exterior la aceeași categorie; `ElementAspect` bate packages scoped; markup attributes și valori locale continuă să fie rezolvate de `UiPropertyStore` deasupra valorii Aspect efective.

Authoring origin nu mai primește benzi proprii în `UiPropertyValueSource`. Engine-ul publică rezultatul Aspect prin sursa/sursele canonice necesare semanticii, indiferent dacă regula a provenit din C# sau markup.

### 5.4 `ElementAspect`

`ElementAspect` devine o sursă locală de reguli/declarations pentru engine. `SetValue` păstrează contractul de editare live, dar modifică declarația locală și programează invalidarea Aspect a consumatorilor; nu mai cheamă direct `SetValueUntyped` pe elemente.

### 5.5 Markup și sidecars

- `@default`, assignment-urile condiționale și `@template` se coboară în reguli/declarations comune.
- Referințele la resources devin literal values, token/resource-backed values sau computed values cu dependencies explicite, după contractul existent.
- Event subscriptions și activările Motion rămân behaviors generate cu attach/detach explicit.
- Un sidecar poate invalida engine-ul sau porni Motion, dar nu poate implementa un cascade concurent prin scrieri directe la surse Aspect private.

## 6. Suprafață publică și compatibilitate

- Se elimină `Cerneala.UI.Markup.MarkupAspectResource` și pagina sa canonică.
- Se redesenează constructorul/suprafața `ElementAspect` numai cât este necesar pentru modelul comun și editarea incrementală.
- Se elimină valorile publice `UiPropertyValueSource` care există numai pentru separarea după authoring path, după migrarea tuturor call-site-urilor.
- Orice API public nou sau modificat primește documentație în `docs-site/documentation/classes/` în aceeași etapă și manifestul se sincronizează pentru pagini adăugate/șterse/redenumite.
- API diff-ul final trebuie să conțină numai eliminările și modificările aprobate de acest plan.

## 7. Fișiere estimate

Producție/runtime:

- `UI/Aspect/AspectEngine.cs`
- `UI/Aspect/AspectCatalog.cs`
- `UI/Aspect/AspectRegistry.cs`
- `UI/Aspect/AspectRuleSet.cs`
- `UI/Aspect/AspectTarget.cs`
- `UI/Aspect/AspectProcessor.cs`
- `UI/Aspect/ElementAspect.cs`
- `UI/Elements/UIElement.cs`
- `UI/Elements/UIElement.Events.cs`
- `UI/Elements/UIRoot.cs`
- `UI/Elements/ElementLifecycle.cs`
- `UI/Core/UiPropertyStore.cs`
- `UI/Core/UiPropertyValueSource.cs`
- `UI/Markup/MarkupAspectResource.cs` (șters)

Generator/language:

- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`
- `Cerneala.SourceGen/UiMarkupMotionResolver.cs`, numai dacă sidecar ownership cere adaptare
- modelele comune din `Cerneala.Language`, numai dacă AST-ul trebuie să păstreze metadata necesară coborârii; sintaxa nu se schimbă

Teste și măsurători:

- `tests/Cerneala.Tests/UI/Aspect/AspectEngineTests.cs`
- `tests/Cerneala.Tests/UI/Aspect/AspectRootRegistryTests.cs`
- `tests/Cerneala.Tests/UI/Aspect/AspectTemplateCatalogIntegrationTests.cs`
- un test dedicat pentru contractul de unificare sub `tests/Cerneala.Tests/UI/Aspect/`
- `tests/Cerneala.Tests/Controls/ElementAspectTests.cs`
- `tests/Cerneala.Tests/UI/Resources/ApplicationResourceIntegrationTests.cs`
- testele Aspect din `tests/Cerneala.Tests.SourceGen/`
- testele Aspect din `tests/Cerneala.Tests.Language/`
- `benchmarks/Cerneala.Benchmarks/AspectResolutionBenchmarks.cs`
- rezultate baseline/finale sub `benchmarks/results/2026-08-27-aspect-unification/`

Documentație:

- `docs/aspect-system.md`
- paginile `ElementAspect`, `ElementAspectValue`, `UiPropertyValueSource` și orice alt API modificat
- ștergerea paginii `Cerneala.UI.Markup.MarkupAspectResource.md`
- `docs-site/documentation/manifest.json`
- note de supersedare în specul/planul istoric markup din 2026-07-09, fără rescrierea istoriei lor

Lista este estimativă. Nu justifică abstractions decorative sau modificări în fișiere neatinse de contract.

## 8. Etape de implementare

### Etapa 0 - Baseline, RED și buget măsurabil

- [x] Adaugă teste RED pentru evaluarea unică a condițiilor, filtrarea tipului/slotului înaintea condițiilor, enforcement-ul slot/variant owner types, imutabilitatea registry-ului și stabilitatea originii catalogului.
- [x] Adaugă teste RED de integrare care cer ca application, scoped, named și inline Aspects să ajungă prin `AspectProcessor`/`AspectEngine`, să raporteze `AspectBase` ca sursă canonică și să revină la idle după o schimbare.
- [x] Adaugă teste RED source-generator care cer generated `AspectPackage`/`ElementAspect` common declarations și interzic `MarkupAspectResource`, `ApplyTo` și benzile de source specifice authoring-ului.
- [x] Adaugă un test reflection/architecture RED care cere absența tipului public `Cerneala.UI.Markup.MarkupAspectResource` după migrare.
- [x] Inventariază testele existente pentru defaults, named, inline, derived `TargetType`, templates, reactive conditions, application resources, runtime-created controls, ItemsControl realization și Motion; fiecare comportament trebuie să aibă un gate ulterior explicit. (Inventar salvat în `benchmarks/results/2026-08-27-aspect-unification/README.md`.)
- [x] Adaugă `AspectResolutionBenchmarks` cu scenarii warm pentru catalog code-first, application package, scope nesting și element-local Aspect; benchmarkul trebuie să raporteze timp și alocări, nu doar număr de reguli.
- [x] Rulează benchmarkul și Presentation frame budget în starea baseline și salvează comenzile, configurația și rezultatele în directorul rezultat al planului. (BDN GREEN; frame-budget gate RED în baseline, raportul brut și valorile sunt păstrate pentru comparația din etapa 6.)
- [x] Confirmă că noile teste sunt RED din motivele contractuale așteptate, nu din fixture/build/environment, iar testele Aspect existente rămân GREEN separat. (11 runtime + 3 source-generator RED intenționat; 141 runtime + 36 source-generator + 14 language Aspect existente GREEN.)
- [x] Reindexează soluția după modificările de teste/benchmark.

**Gate etapa 0**

- [x] Fiecare încălcare confirmată și fiecare cale runtime paralelă are un test permanent RED.
- [x] Există baseline reproductibil pentru timp, alocări și frame budget.
- [x] Nu a fost modificat codul de producție.

### Etapa 1 - Întărirea engine-ului canonic

- [x] Refactorizează matchingul astfel încât tipul și slotul să fie filtrate înaintea condițiilor, iar fiecare condiție să fie evaluată exact o dată și același rezultat să alimenteze matchingul, dependencies și diagnostics.
- [x] Impune `AspectSlot.OwnerType`/`TargetType` la înregistrare și matching și impune owner type pentru `AspectVariantKey`; adaugă excepții/diagnostics deterministe pentru utilizări invalide.
- [x] Fă snapshoturile `AspectRegistry`, `AspectPackage` și `AspectCatalog` realmente nemutabile din exterior.
- [x] Elimină mutarea `AspectRuleSet.PackageName`; păstrează package/scope/name origin în entries de catalog sau în metadata de rezoluție deținută de catalog. (Catalogul creează proiecții imutabile cu origine; source rules rămân reutilizabile.)
- [x] Actualizează diagnostics și documentația API afectată fără să schimbi încă source-generatorul.
- [x] Rulează testele RED de core, întregul proiect de teste Aspect și testele de diagnostics. (8 contracte core GREEN; 150 teste Aspect incluzând contractele etapei + 26 template/slot/variant/trace GREEN.)
- [x] Reindexează soluția și verifică faptul că noile snapshoturi nu expun colecții mutabile prin cast.

**Gate etapa 1**

- [x] Cele patru defecte confirmate sunt GREEN și nu există evaluări duplicate sau origin mutation.
- [x] Testele Aspect code-first existente sunt GREEN.
- [x] API/docs schimbate în această etapă sunt sincronizate.

### Etapa 2 - Compoziția unică a surselor runtime și `ElementAspect`

- [x] Extinde `AspectProcessor`/`AspectEngine` astfel încât rezoluția să primească root catalog, application packages, packages din scopes ancestrale și `ElementAspect` într-o singură operație de cascade.
- [x] Introdu metadata stabilă pentru source rank/scope distance și testează precedența theme < application < outer scope < inner scope < element Aspect.
- [x] Redefinește `ElementAspect` ca sursă de declarations/rules comune și păstrează editarea incrementală prin invalidarea consumatorilor atașați, fără scrieri directe în property store.
- [x] Conectează schimbarea/replacement/removal de `ElementAspect` la `AspectQueue`, inclusiv attach, detach, reattach și utilizare partajată de mai multe elemente.
- [x] Permite resurselor application și element-scoped să furnizeze `AspectPackage`; schimbarea resurselor trebuie să invalideze numai subtree/root-urile afectate și să reconstruiască snapshoturile necesare.
- [x] Migrează `ItemsControl.ItemContainerAspect` și call-site-urile code-first la noul contract local. (Call-site-ul existent atribuie `UIElement.AspectProperty`; după refactor, aceeași cale este rezolvată exclusiv de engine.)
- [x] Păstrează template resolution prin engine pentru packages scoped și local Aspect, inclusiv template swap și cleanup la detach.
- [x] Actualizează în aceeași etapă documentația canonică pentru `ElementAspect`, `ElementAspectValue` și orice API public schimbat.
- [x] Rulează testele runtime pentru scope shadowing, derived targets, runtime-created template/items controls, mutation, detach/reattach, template replacement și idle frames. (32 focused GREEN; 3.123 broader runtime GREEN + 2 skip-uri vizuale declarate. Cele 4 teste RED rămase sunt contractele deliberate pentru markup named/application și ștergerea legacy din etapele 3/5.)
- [x] Reindexează soluția.

**Gate etapa 2**

- [x] Nicio valoare `ElementAspect` sau package scoped nu este aplicată direct în afara engine-ului.
- [x] Precedența și lifecycle-ul sunt GREEN pentru cod creat manual și pentru elemente realizate ulterior.
- [x] Un frame idle după stabilizare nu mai programează muncă Aspect.

### Etapa 3 - Coborârea markup-ului static și a template-urilor în modelul comun

- [x] Schimbă modelul intern source-generator astfel încât `@default` și `@template` să emită `AspectDeclaration`/`AspectRuleSet` în packages sau `ElementAspect`, nu assignments directe.
- [x] Generează application defaults și unnamed scoped defaults ca `AspectPackage` resources consumate de compoziția runtime unică.
- [x] Generează named Aspect resources și inline `<Control.Aspect>` ca `ElementAspect`; `Aspect="$Name"` trebuie să rezolve aceeași instanță/definiție fără `ApplyTo`.
- [x] Păstrează validarea compile-time pentru `TargetType`, assignability, proprietăți, resource references și duplicate names; validarea nu devine runtime parsing.
- [x] Păstrează template precedence și template owner bindings, dar valoarea `ComponentTemplateProperty` trebuie să fie o declarație câștigătoare a engine-ului.
- [x] Elimină emisia directă `SetValue` pentru declarations Aspect necondiționate și verifică generated source prin assertions structurale.
- [x] Migrează testele source-generator și runtime pentru unnamed/named/inline aspects, custom/derived controls, templates, nested resources și code-created descendants.
- [x] Rulează testele complete `Cerneala.Tests.SourceGen` filtrate pe Aspect/Template/Resource și testele runtime dependente. (122 teste statice/template/resource aplicabile etapei GREEN; 467 teste SourceGen mai largi GREEN; 160 runtime Aspect GREEN. Două contracte reactive rămân RED exclusiv pentru etapa 4.)
- [x] Reindexează soluția.

**Gate etapa 3**

- [x] Toate aspectele markup necondiționate și template declarations intră în engine.
- [x] Generated source nu mai conține un applicator Aspect direct pentru aceste cazuri.
- [x] Sintaxa și diagnostics existente rămân GREEN.

### Etapa 4 - Condiții, dependencies și sidecars Motion/event

- [x] Coboară assignments din `@when`/`@if` în `AspectCondition` și rules comune, cu dependencies explicite pentru proprietăți, states, data și resources observabile. (`AspectConditionKey` este starea per element; `AspectCondition.Signal` este condiția evaluată de engine.)
- [x] Elimină scrierile stilistice din `UiMarkupReactiveEmitter`; subscriptions generate pot numai actualiza dependency state/invalida engine-ul sau executa behavior-ul non-Aspect pe care îl dețin.
- [x] Separă clar generated sidecars pentru events, Motion, presence, layout, scroll, drag și gestures de declarations Aspect; sidecars trebuie să aibă attach/detach idempotent și cleanup complet.
- [x] Integrează `@template` owner/self conditions cu același evaluator Aspect fără subscriptions stilistice duplicate.
- [x] Verifică schimbările de DataContext, resource, state, property și token: recomputare o singură dată, eliminarea valorii când condiția devine falsă și zero callbacks după detach.
- [x] Rulează suitele source-generator reactive/Motion și testele runtime Aspect/Motion relevante. (472 SourceGen GREEN; 427 runtime Aspect/Motion GREEN; Presentation rebuild GREEN.)
- [x] Reindexează soluția.

**Gate etapa 4**

- [x] Nicio condiție markup nu implementează un cascade prin `SetValue` direct la o sursă Aspect paralelă.
- [x] Motion și events păstrează comportamentul, dar ownership-ul lor rămâne în subsistemele dedicate.
- [x] Dependency invalidation și detach cleanup sunt GREEN.

### Etapa 5 - Ștergerea completă a runtime-ului paralel

- [x] Șterge `UI/Markup/MarkupAspectResource.cs` și migrează toate call-site-urile, testele și generated ABI la `AspectPackage`/`ElementAspect`. (`AspectBehavior` este sidecar-ul target-typed din package; lifetime-ul este sincronizat de `AspectProcessor`, nu de un executor paralel.)
- [x] Elimină `UIRoot.applicationAspects`, `ApplyApplicationAspects`, `ApplyLocalAspects`, `InheritanceDistance` și apelurile lor din lifecycle.
- [x] Elimină `ApplicationAspectBase`, `ApplicationAspectVisualState`, `LocalAspectBase` și `LocalAspectConditional` după ce RoslynIndexer confirmă zero call-site-uri legitime.
- [x] Simplifică `UiPropertyStore.EffectiveOrder` la sursele deținute de subsisteme reale și rulează toate testele de precedence/property store. (`TemplateOwnerBinding` aparține template subsystem-ului și păstrează contractul owner-to-part demonstrat de Presentation; 534 teste runtime Aspect/Motion/store/template/control relevante GREEN.)
- [x] Elimină helpers și branches source-generator rămase exclusiv pentru applicatorul vechi. (Sidecar-urile Motion/event care cer contextul site-ului rămân generate la site; condițiile context-free sunt package behaviors. 474/474 SourceGen GREEN.)
- [x] Șterge pagina API `Cerneala.UI.Markup.MarkupAspectResource.md`, scoate intrarea din manifest și actualizează toate paginile care menționează vechiul tip/sursele eliminate. (Manifest: 1.072 entries, 0 duplicate, 0 pagini/surse lipsă; testul oficial de manifest GREEN.)
- [x] Adaugă/actualizează testele architecture/reflection care interzic reintroducerea tipului, executorului și benzilor de precedență șterse.
- [x] Rulează RoslynIndexer search/reference pentru toate simbolurile eliminate și confirmă că rămân doar mențiuni istorice marcate ca supersedate. (Zero simboluri și zero text de producție; șirurile rămase sunt assertions de interdicție, planul activ și planul istoric 2026-07-10 marcat explicit superseded.)
- [x] Rulează API diff și justifică fiecare eliminare/modificare publică prin decizia aprobată în acest plan. (Raw strict ApiCompat și clasificarea completă: `benchmarks/results/2026-08-27-aspect-unification/stage5-api-diff.*`.)
- [x] Reindexează soluția.

**Gate etapa 5**

- [x] Assembly-ul și generated source nu mai conțin `MarkupAspectResource` sau value sources specifice authoring path-ului.
- [x] `UIRoot` nu mai face target matching/cascade pentru markup resources.
- [x] Nu există shim, wrapper sau executor compatibil rămas.

### Etapa 6 - Diagnostics, performanță și conformance vizual

- [x] Extinde trace-ul canonic astfel încât o valoare provenită din markup să raporteze package/document, scope, named/inline origin, layer, specificity/order, condiții și dependencies fără metadata mutabilă. (`AspectOrigin` este metadata diagnostică imutabilă; pașii păstrează rezultatele exacte ale condițiilor.)
- [x] Verifică deterministic că diagnostics pentru C# și markup descriu același winner și aceleași motive de respingere. (31 teste runtime diagnostics/Aspect + 2 teste source-generator de origin/parity GREEN.)
- [x] Rulează `AspectResolutionBenchmarks` în aceleași condiții ca baseline-ul și salvează timpul, alocările, rule evaluations, condition evaluations și invalidation counts. (BDN final și `final-aspect-metrics.json` arhivate.)
- [x] Dacă există regresie măsurată relevantă, optimizează numai ownerul demonstrat și rerulează benchmarkul; nu introduce cache fără invalidare/versioning testate. (Eager public trace materialization a fost ownerul măsurat; `Resolve` nu mai capturează, iar `Apply` materializează public lazy fără reevaluare. Rerun-ul autoritativ este `final-bdn-optimized/`.)
- [x] Rulează Presentation frame budget pe scenariile Aspect și compară CPU frame cost, allocations și scheduled Aspect time cu baseline-ul. (SDL3, 8x45: Aspect mean 0.001877 ms, p99 0.0726 ms, max 0.1766 ms; mean allocation/frame -0.7%; baseline și final rămân sub același gate general RED al mașinii.)
- [x] Rulează scenariile vizuale deterministe pentru Aspect/templates și capturează exclusiv prin `Window.SaveScreenshot`; compară outputul cu referința/toleranța existentă și investighează orice diferență. (`CERNEALA_PRESENTATION_SETTLED_CAPTURE=1`; Aspect Studio și Build-Time Markup: 0/1.440.000 pixeli diferiți, MAE/P99/max 0.)
- [x] Reindexează după orice modificare de cod rezultată din măsurători.

**Gate etapa 6**

- [x] Diagnostics identifică aceeași cale runtime pentru code-first și markup.
- [x] Nu există regresie de performanță neexplicată sau gate vizual nerezolvat.
- [x] Rezultatele și configurația sunt reproductibile din repository. (`benchmarks/results/2026-08-27-aspect-unification/stage6-results.md`.)

### Etapa 7 - Documentație canonică și verificare finală

- [x] Rescrie `docs/aspect-system.md` cu un singur data flow runtime, contractul de scope/cascade, `ElementAspect`, generated sidecars și exemple care folosesc API-uri reale.
- [x] Marchează specul și planul markup din 2026-07-09 drept supersedate de runtime-ul unificat, păstrându-le ca istoric și eliminând orice recomandare activă de applicator direct. (Și planul istoric 2026-07-10 este marcat superseded.)
- [x] Rulează skill-ul `writing-api-documentation` pentru auditul final al tuturor API-urilor Aspect modificate și verifică `docs-site/documentation/manifest.json`. (1.075 entries, 0 duplicate, 0 pagini/surse lipsă; testul oficial de manifest GREEN.)
- [x] Rulează testele complete pentru `Cerneala.Tests.SourceGen`, `Cerneala.Tests.Language` și proiectul runtime relevant. (476/476, 181/181, respectiv 3.131 passed + 2 skip-uri native declarate.)
- [x] Rulează `dotnet test .\Cerneala.slnx` o singură dată în starea finală de cod. (Final-state run GREEN: runtime 3.131+2 skip, SourceGen 476, Language 181, LanguageServer 40, PreviewHost 12, VisualStudio 47, SDL GPU 63+4 skip. Un run anterior a expus și a reparat coliziunea de output a fixture-ului LanguageServer.)
- [x] Rulează build Release, formatter verification, documentația/manifest tests, API diff review și `git diff --check`. (Release 0 warnings/errors; goal-scoped formatter GREEN; full formatter rămâne RED exclusiv pe fișiere baseline neatinse; manifest GREEN; final strict ApiCompat complet clasificat; diff-check clean.)
- [x] Regenerează `FileTree.md`, reindexează soluția și rulează RoslynIndexer `doctor`/`status`; investighează orice warning sau stale state. (Forced full index: valid, 0 dirty; doctor PASS. Cele 7 warnings sunt cele 2 metadata-reference cunoscute și 5 fișiere oversized intenționat sărite.)
- [x] Revizuiește diff-ul complet pentru debug code, generated churn, worktree user changes, API-uri decorative și mențiuni active ale căii vechi. (0 fișiere temporare în `.agents`; 0 mențiuni legacy active; schimbările utilizatorului rămân neatinse.)
- [x] Confirmă explicit că nu s-a mutat Motion/input/binding ownership în engine și că nu a rămas un al doilea resolver Aspect. (`AspectEngine` păstrează bridge-ul Motion existent; generated Motion/event/binding/input sidecars rămân în subsistemele lor; `UIRoot` are zero matching/cascade markup.)

**Gate etapa 7**

- [x] Toate suitele și gate-urile obligatorii sunt GREEN în starea finală. (Formatterul Aspect-scoped este GREEN; failure-ul formatterului unrestricted este baseline extern, documentat în `final-verification.md`.)
- [x] Documentația, manifestul, API-ul și implementarea descriu aceeași arhitectură.
- [x] Worktree-ul nu conține experimente temporare sau modificări accidentale peste schimbările utilizatorului.

## 9. Ordinea de execuție

1. Etapa 0 fixează RED și baseline-ul; nicio producție înaintea gate-ului.
2. Etapa 1 face engine-ul sigur înainte să devină owner unic.
3. Etapa 2 introduce compoziția runtime și migrează adaptorul local.
4. Etapele 3 și 4 migrează markup-ul static, apoi partea reactivă/Motion.
5. Etapa 5 șterge complet calea veche numai după feature parity.
6. Etapa 6 măsoară și verifică runtime-ul real.
7. Etapa 7 sincronizează contractele și închide suita completă.

Nu se sare la o etapă ulterioară pentru că este mai comodă. Fiecare gate trebuie bifat imediat după verificare.

## 10. Stop conditions

- O capabilitate markup existentă nu poate fi reprezentată fără mutarea ownership-ului Motion/binding/input în engine.
- Un test existent dovedește o precedență incompatibilă cu ordinea aprobată și documentele nu stabilesc contractul corect.
- Un public API diferit de cele aprobate trebuie eliminat sau schimbat pentru a continua.
- Un gate vizual/performance indică regresie a cărei cauză nu este încă izolată.
- Modificările utilizatorului se suprapun peste unul dintre fișierele necesare și intentul nu poate fi separat în siguranță.

În aceste cazuri etapa curentă rămâne nebifată și se cere decizie; nu se introduce workaround.

## 11. Definiția de gata

- [x] Există un singur resolver/cascade Aspect în producție: `AspectEngine`.
- [x] Code-first, application, scoped, named, inline și item-container Aspects folosesc același model de rules/declarations și aceeași coadă de invalidare.
- [x] `ElementAspect` este un adaptor local peste modelul comun, nu un aplicator direct.
- [x] `MarkupAspectResource`, matcher-ele lui din `UIRoot` și value sources dedicate sunt eliminate fără shims.
- [x] Generatorul nu emite un al doilea runtime Aspect; sidecars rămase au ownership non-Aspect explicit.
- [x] Condițiile sunt evaluate o singură dată după prefiltrare, typed contracts sunt impuse și snapshoturile sunt stabile/imutabile.
- [x] Feature parity markup, lifecycle, detach, templates, Motion, diagnostics, idle frames, conformance vizual și performanță sunt verificate.
- [x] API docs, manifestul, `docs/aspect-system.md`, planurile supersedate și FileTree sunt sincronizate.
- [x] Suita completă, buildul Release, formatterul, API diff, RoslynIndexer și `git diff --check` sunt GREEN. (Formatter goal-scoped GREEN; baseline unrestricted failure documentat separat.)
