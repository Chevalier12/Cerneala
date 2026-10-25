# Plan: `Collider2D.Entered` și `Exited`

> Data: 2026-10-25
> Status: finalizat
> Index: [scene2d-autonomie-index](2026-10-25-scene2d-autonomie-index.md), planul 1
> Depinde de: nimic
> Blocat de: nimic (D1, D2 și D3 decise pe 2026-10-25, vezi indexul)

## Rezumat

Un collider anunță când alt collider începe să se suprapună cu el (`Entered`) și când nu se mai suprapune (`Exited`). Argumentul spune cine: `Other`.

```csharp
coin.Collider.Entered += (_, e) =>
{
    if (e.Other == fox.Collider)
    {
        scene.Children.Remove(coin);   // moneda dispare când vulpea o atinge
    }
};
```

Azi un joc trebuie să întrebe singur lumea în fiecare frame (`Overlap`, `MoveAndCollide(...).TriggerHits`) și să țină minte cine era deja înăuntru.

## Baseline (verificat pe master `55c4ea20`)

- `Collider2D` este `public abstract class Collider2D : SceneNode2D` (`UI/Controls/Collider2D.cs:8`):
  - nu are evenimente publice;
  - are proprietățile `Enabled`, `IsTrigger`, `IsSimulated`, `OffsetX`/`OffsetY`, `CollisionLayer` și `CollisionMask`.
- Un collider viu poate fi deținut numai de un `Sprite2D`, prin `Sprite2D.Collider` (`Collider2D.cs:13-26`, `ColliderSlot2D.cs`). Excepție: `TileStaticCollider2D`, intern, sub `TileMap2D`.
- `CollisionWorld2D` (`UI/Controls/CollisionWorld2D.cs:8`):
  - există una per scenă rădăcină, creată de `Scene2D` (`Scene2D.cs:49`, accesor la `:61-62`);
  - metode publice: `Intersects`, `Overlap`, `Raycast`, `MoveAndCollide`, `PrepareRegionAsync`, `GetDiagnosticsSnapshot`.
- **Nu există nicio stare de contact.** Nu există enter, exit sau stay. `CollisionNarrowPhase2D` este static și fără stare. `MoveAndCollide` raportează triggerele numai pentru mișcarea cerută (`CollisionWorld2D.cs:206-237`, `MoveCollisionResult2D.TriggerHits`).
- Orice schimbare de geometrie trece prin `Scene2D.NotifyCollisionMutation(node, kind)` (`Scene2D.cs:94-107`). Aceasta crește versiunea rădăcinii și cheamă `CollisionWorld2D.ApplyMutation`. Cine o cheamă:
  - `Collider2D.OnPropertyChanged`;
  - `Sprite2D` pentru X, Y, Rotation și vizibilitate;
  - `Scene2D` pentru X/Y și colecția `Children`;
  - `ColliderSlot2D` la înlocuirea colliderului.
- Filtrul de pereche este `PassesPairFilter` (`CollisionWorld2D.cs:507-513`): layer-ele și măștile nenule, și se potrivesc în ambele sensuri.
- Ordinea unui `UiHost.Update` (`UI/Hosting/UiHost.cs:163-241`):
  1. avansarea timpului de randare (animații sprite);
  2. munca programată;
  3. `InputBridge.Dispatch` (inclusiv `InputHeld`);
  4. procesarea input-ului;
  5. commit.
- Teste existente pentru coliziuni: `tests/Cerneala.Tests/Controls/CollisionStage*Tests.cs`, `ColliderOwnershipTests`, `InitialContactMovementTests`, `SceneCollisionStreamingTests`.

## Obiective

- `Collider2D.Entered` și `Collider2D.Exited`, cu argumente care au `Other` (forma exactă după D1 și D2).
- Un contact există când perechea trece filtrul layer/mask și formele se suprapun după regula din D3.
- Ambele părți primesc evenimentul dacă ambele au handler. Exemplu: A primește `Entered` cu `Other = B`, iar B primește `Entered` cu `Other = A`.
- `Exited` vine și când contactul dispare fără mișcare:
  - `Enabled = false`, `CollisionLayer = 0` sau collider invizibil;
  - colliderul sau proprietarul lui este scos din scenă;
  - `Sprite2D.Collider` este înlocuit.
- Evaluarea este reactivă:
  - se verifică numai colliderele care s-au schimbat de la ultima evaluare și care au cel puțin un handler, sau au în contact un collider cu handler;
  - o scenă fără handlere nu face nicio muncă nouă.
- Evenimentele nu se ridică din interiorul setter-ului care a schimbat geometria. Ele vin o dată pe update, într-un punct fix, în ordine deterministă.
- Ce schimbă un handler (de exemplu mută alt sprite) se evaluează la următorul punct de livrare, nu recursiv în același punct.
- Contactele în așteptare țin fereastra trează până sunt livrate: `PumpOnce` și Servo `IsIdle`.

## Non-obiective

- `Stay` (un eveniment per frame cât timp contactul durează).
- Contacte cu `TileStaticCollider2D` pentru handlere puse pe tile-uri: tile-urile nu au obiect public pe care să pui handler. Un `Sprite2D` cu handler primește totuși `Entered` cu `Other` = colliderul tile-ului, dacă perechea se potrivește. Etapa 0 caracterizează dacă `Other` poate fi un collider intern.
- Schimbarea `MoveAndCollide`, `Overlap` sau `Raycast`.
- `Vision` (planul 2).

## Arhitectura

- **Owner:** `CollisionWorld2D` deține starea de contact a scenei rădăcină:
  - perechile care se ating acum, cu cheie stabilă de pereche;
  - mulțimea colliderelor „murdare” de la ultima livrare.
- **Intrare:** `ApplyMutation` deja primește fiecare schimbare. El marchează colliderele afectate ca murdare numai dacă urmărirea contactelor este activă pentru ele. Pentru un `Sprite2D` sau `Scene2D` mutat, sunt afectate colliderele din subarbore.
- **Activare:** când se adaugă primul handler `Entered`/`Exited` pe un collider, acesta intră în urmărire, cu un contor intern. Contactele lui actuale se calculează la următoarea livrare. Ele se anunță ca `Entered` dacă există deja o suprapunere.
- **Livrare:** o metodă internă a lumii calculează diferența pentru colliderele murdare:
  - candidații vin din `SparseCollisionGrid2D`;
  - testul exact folosește `CollisionNarrowPhase2D`, cu aceeași regulă ca `Intersects`;
  - întâi se actualizează starea, apoi se ridică evenimentele.
- **Punctul de livrare** în `UiHost.Update` se stabilește în Etapa 0, printr-un test de caracterizare. Ipoteza: după `InputBridge.Dispatch` și înainte de procesarea input-ului, ca schimbările din `InputHeld` și din avansarea timpului să fie văzute în același frame. Headless: `SceneSimulationContext2D.Update()` livrează și el.
- **Trezire:** o proprietate internă `HasPendingContacts`, pe lume sau agregată pe root, intră în condiția de randare din `PumpOnce` (`WindowApplicationRuntime.cs:665-671`) și în `ServoContext.IsIdle` (`UI/Servo/ServoContext.cs:112-121`). Urmează modelul `HasActiveInputHold`.

## Fișiere estimate

- `UI/Controls/Collider2D.cs`: evenimentele și contorul de urmărire.
- `UI/Controls/CollisionWorld2D.cs` și o parte nouă `UI/Controls/CollisionWorld2D.Contacts.cs`.
- `UI/Controls/ColliderContactEventArgs.cs`, nou (numele după D2).
- `UI/Controls/Scene2D.cs`: legătura cu livrarea.
- `UI/Controls/SceneSimulationContext2D.cs`: livrarea headless.
- `UI/Hosting/UiHost.cs`: punctul de livrare.
- `UI/Hosting/Windowing/WindowApplicationRuntime.cs`, `UI/Servo/ServoContext.cs`, `UI/Servo/RetainedServoInputDriver.cs` (sau un echivalent): trezirea.
- Teste noi: `tests/Cerneala.Tests/Controls/ColliderContactEventTests.cs`. Trezirea se testează în `tests/Cerneala.Tests/UI/Hosting/WindowRuntimeTests.cs` și `tests/Cerneala.Tests/UI/Servo/ServoSynchronizationTests.cs`.
- Docs:
  - `docs-site/documentation/classes/Cerneala.UI.Controls.Collider2D.md`;
  - pagina nouă pentru clasa de argumente și intrarea ei în `docs-site/documentation/manifest.json`;
  - `docs/architecture/scene2d.md`, `docs/architecture/hosting-and-platform.md`, `docs/architecture/servo.md`.

## Etapa 0 — contract și teste RED

- [x] Se confirmă cu utilizatorul D1, D2 și D3 și se trece răspunsul în index.
- [x] Un test de caracterizare (GREEN) fixează ordinea din `UiHost.Update`: avansarea timpului, input, procesare. Pe baza lui se alege punctul de livrare.
- [x] Se face tabelul apelanților `Scene2D.NotifyCollisionMutation` și `CollisionWorld2D.ApplyMutation`, cu ce collidere marchează fiecare ca murdare. Tabelul stă în notele de lucru.
- [x] Se caracterizează (GREEN) `Intersects` la atingerea pe muchie, cu triggere și cu layer/mask. Aceasta e regula pe care D3 o reutilizează.
- [x] Se caracterizează ce `Collider` raportează azi `Overlap` pentru un tile static: `TileStaticCollider2D` intern sau altceva. Pe baza rezultatului se decide dacă `Other` poate fi un collider de tile. Dacă `Other` ar expune un tip intern, se întreabă utilizatorul înainte de Etapa 1. (Nu a fost nevoie de întrebare: `Other` dă același obiect pe care `CollisionHit2D.Collider` îl dă deja public, vezi notele de lucru.)
- [x] Teste RED în `ColliderContactEventTests`:
  - un sprite mutat peste un trigger dă un singur `Entered`, cu `Other` corect;
  - încă o mutare înăuntru nu dă alt `Entered`;
  - ieșirea dă un singur `Exited`;
  - ambele părți primesc evenimentul când ambele au handler;
  - `Enabled = false`, `CollisionLayer = 0`, `IsVisible = false`, scoaterea din `Children` și înlocuirea `Sprite2D.Collider` dau `Exited`;
  - layer/mask care nu se potrivesc nu dau evenimente;
  - un handler adăugat peste o suprapunere existentă primește `Entered` la următoarea livrare;
  - un handler care mută alt sprite nu primește evenimente recursive în aceeași livrare;
  - mutarea unui `Scene2D` părinte mută contactele copiilor;
  - ordinea evenimentelor la mai multe perechi este deterministă.
- [x] Test RED: o scenă cu 1 000 de collidere fără handlere nu face teste exacte noi la mutări (`CollisionWorld2DDiagnosticsSnapshot.ExactTestCount` nu crește).

### Gate etapa 0

- [x] Testele RED pică pentru că lipsesc evenimentele sau tipul de argumente, nu din cauza unui fixture greșit. (Build-ul real: numai `CS0246` pentru `ColliderContactEventArgs`. Compilat separat, cu un tip-stub pentru argumente: numai `CS1061` pentru `Entered`/`Exited`.)
- [x] Testele de caracterizare sunt GREEN. (9/9, rulate înainte să existe fișierul RED; cu el, proiectul de teste nu compilează până la Etapa 1.)
- [x] Comanda: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~ColliderContact"`.

## Etapa 1 — starea de contact și evenimentele

- [x] Se adaugă evenimentele, clasa de argumente și contorul de urmărire pe `Collider2D`. (În locul contorului: `Collider2D.HasContactHandlers` și `trackedEntries` în lume, vezi „Unde stă starea de contact”.)
- [x] Se adaugă starea de contact în `CollisionWorld2D.Contacts.cs`. `ApplyMutation` marchează colliderele murdare numai cât timp urmărirea este activă. (Contactele raportate stau pe collidere; lumea ține colliderele urmărite și pe cele murdare. Marcajul stă în `AddOrUpdate` și `Remove`, la care ajunge `ApplyMutation`.)
- [x] Livrarea calculează diferența, actualizează starea, apoi ridică evenimentele în ordinea fixată în Etapa 0.
- [x] `Reset`, scoaterea din scenă și schimbarea rădăcinii scenei curăță starea și dau `Exited` unde este cazul.
- [x] Livrarea este legată în `UiHost.Update` și în `SceneSimulationContext2D.Update()`.

### Gate etapa 1

- [x] Testele din Etapa 0 sunt GREEN. (`FullyQualifiedName~ColliderContact`: 50/50.)
- [x] Testele de coliziune existente rămân GREEN: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~Collision|FullyQualifiedName~Collider|FullyQualifiedName~Scene2D|FullyQualifiedName~SceneSimulation"`. (224/224. Pentru codul atins de livrare, și filtrul `UiHost|UIRoot|Relay|RenderSurface|InputHeld|Servo|WindowRuntime`: 576/576. Suita completă nu a fost rulată.)

## Etapa 2 — trezirea ferestrei

- [x] Test RED: o mutare făcută într-un frame care nu livrează contactele ține `PumpOnce` treaz până la livrare. După livrare, fără altă muncă, `PumpOnce` nu mai randează. (`WindowRuntimeTests.ContactChangesMadeByAContactHandlerKeepPumpOnceRenderingUntilTheyAreDelivered`. RED la `WindowRuntimeTests.cs:618`: `PumpOnce` dădea `false` după `Entered`. Suprafața are `RedrawMode = OnDemand`, vezi „Trezirea”.)
- [x] Test RED: Servo `WaitForIdle` nu se termină cât timp există contacte în așteptare. (`ServoSynchronizationTests.WaitForIdleIsNotIdleWhileContactChangesWaitForDelivery`. RED la `ServoSynchronizationTests.cs:313`: `WaitForIdleAsync` se termina fără timeout.)
- [x] Se adaugă `HasPendingContacts` în condiția din `PumpOnce` și în `ServoContext.IsIdle`. (`CollisionWorld2D.HasPendingContacts`, agregat în `UIRoot.HasPendingSceneContacts`; `WindowApplicationRuntime.cs:671-672`, `ServoContext.cs:121`. `RetainedServoInputDriver` nu a trebuit schimbat: `IsIdle` primește deja root-ul.)

### Gate etapa 2

- [x] `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~WindowRuntime|FullyQualifiedName~Servo|FullyQualifiedName~ColliderContact"` este GREEN. (300/300. Și `AspectTemplateCatalogIntegration`, care folosește tot `WaitForIdleAsync`: 24/24. Suita completă nu a fost rulată.)

## Etapa 3 — documentație și verificare

- [x] `Cerneala.UI.Controls.Collider2D.md` descrie `Entered`/`Exited` cu un exemplu. Se adaugă pagina clasei de argumente și intrarea ei în manifest, după `.claude/skills/writing-api-documentation/SKILL.md`. (Scrise în Etapele 1 și 2: exemplul cu moneda la `Collider2D.md:59` și secțiunea „Contact events” la `:91`; `ColliderContactEventArgs.md` are Definition, Examples și Remarks; intrarea din manifest la `manifest.json:1218-1220`. Testul `ApiDocumentationManifestIsValidAndReferencesExistingFiles`: 1/1, după ultima schimbare a manifestului.)
- [x] `docs/architecture/scene2d.md` descrie starea de contact și punctul de livrare. `hosting-and-platform.md` și `servo.md` listează noua condiție de trezire. (Vezi „Documentația (Etapa 3)”.)
- [x] Se rulează filtrele focalizate din gate-urile 1 și 2. Raportul spune că suita completă nu a fost rulată. (Pe codul final: `ColliderContact` 50/50, filtrul din gate-ul 1 224/224, filtrul `UiHost|UIRoot|Relay|RenderSurface|InputHeld|Servo|WindowRuntime` 578/578. Gate-ul 2 (300/300) și `AspectTemplateCatalogIntegration` (24/24) rulaseră deja pe același cod: build-ul din Etapa 3 nu a refăcut niciun DLL. Suita completă nu a fost rulată.)

## Ordinea recomandată

Etapa 0 → 1 → 2 → 3. Etapa 2 poate începe după Etapa 1. Ea are nevoie de livrarea legată în `UiHost`.

## Definiția de gata

- [x] Toate casetele de mai sus sunt bifate.
- [x] Exemplul cu moneda din Rezumat compilează într-un test și trece. (`ColliderContactEventTests.CoinExampleRemovesTheCoinWhenTheFoxTouchesIt`, cu handlerul din Rezumat la `:31-37`, în rularea `ColliderContact` 50/50.)
- [x] O scenă fără handlere de contact nu face muncă în plus la mutări (testul din Etapa 0). (`ColliderContactEventTests.ASceneWithoutContactHandlersDoesNoExactTestsWhenAThousandCollidersMove`, în aceeași rulare.)

## Note de lucru

### Cine anunță lumea de coliziuni (Etapa 0)

Verificat pe master `55c4ea20` cu Roslyn `find_callers` și citirea surselor. `CollisionWorld2D.ApplyMutation` are un singur apelant: `Scene2D.NotifyCollisionMutation` (`Scene2D.cs:107`). Toate schimbările de mai jos ajung deci în lume pe același drum. Când lumea nu e încă inițializată (nicio interogare până acum), `ApplyMutation` doar ține minte versiunea (`CollisionWorld2D.cs:325-329`).

| Apelant | Unde | Mutația | Ce rulează lumea |
| --- | --- | --- | --- |
| `Collider2D.OnPropertyChanged` | `Collider2D.cs:177` | Geometry, Filter sau Participation | `ReconcileSubtree(collider)` |
| `Sprite2D.OnPropertyChanged`: `X`, `Y`, `Rotation` | `Sprite2D.cs:645` | Geometry | `ReconcileSubtree(sprite)` |
| `Sprite2D.OnPropertyChanged`: `IsVisible`, `Visibility` | `Sprite2D.cs:650` | Participation | `ReconcileSubtree(sprite)` |
| `Scene2D.OnPropertyChanged`: transformări, `TransformOrigin`, `X`, `Y` | `Scene2D.cs:421` | Geometry | `ReconcileSubtree(scene)` |
| `Scene2D.OnPropertyChanged`: `IsVisible`, `Visibility` | `Scene2D.cs:426` | Participation | `ReconcileSubtree(scene)` |
| `Scene2D.ChildCollection`: `InsertItem`, `SetItem`, `RemoveItem`, `ClearItems` | `Scene2D.cs:488`, `:512`, `:527`, `:545` | Structure | `ReconcileAll()` |
| `ColliderSlot2D.NotifyStructureChanged` (înlocuirea `Sprite2D.Collider`) | `ColliderSlot2D.cs:58` | Structure | `ReconcileAll()` |
| `SceneItems2D.Commit`, `SceneItems2D.RetireRealizations` | `SceneItems2D.cs:375`, `:550` | Structure | `ReconcileAll()` |
| `TileMap2D.OnPropertyChanged`: transformări, `Offset`, vizibilitate, `Opacity` | `TileMap2D.cs:81` | Geometry | `ReconcileSubtree(map)` |
| `TileMap2D.SynchronizeCollisionChunk`, `TileMap2D.RemoveCollisionNodes` | `TileMap2D.Collision.cs:54`, `:154` | Structure | `ReconcileAll()` |
| `TileMap2D.StopSource` | `TileMap2D.Streaming.cs:563` | Structure | `ReconcileAll()` |

Ce collidere devin murdare: toate drumurile ajung în `AddOrUpdate` sau `Remove` (`CollisionWorld2D.cs:417-484`). Marcajul stă acolo: o intrare nouă, schimbată sau scoasă devine murdară. Așa nu contează care apelant a pornit schimbarea.

### Cum se citește „marchează numai cât timp urmărirea este activă”

- Exemplu: moneda are handler, vulpea nu. Vulpea se mută peste monedă. Dacă s-ar marca numai colliderele cu handler, vulpea nu ar fi marcată și moneda nu ar afla niciodată.
- Regula aleasă: cât timp lumea are cel puțin o intrare urmărită (un collider cu handler), orice intrare schimbată devine murdară. La livrare, un collider murdar fără handler se testează numai față de vecinii urmăriți și față de contactele lui deja raportate.
- Un collider care are contacte raportate (de exemplu vulpea, raportată de monedă) se marchează și el la orice schimbare.
- Fără niciun handler în lume nu se marchează nimic și nu se face niciun test exact. O scenă fără context de simulare nu marchează nimic; la atașarea contextului, colliderele cu handler se marchează singure.

### Punctul de livrare

Testul `ColliderContactCharacterizationTests.UiHostUpdateAdvancesTimeThenDrainsRelayThenDispatchesInputThenProcessesLayout` fixează ordinea dintr-un `UiHost.Update`: timp (`UiHost.cs:163-167`), Relay (`UIRoot.BeginUpdate`, `UIRoot.cs:448`), input (`UiHost.cs:186`), procesare (`UiHost.cs:191-195`). Livrarea contactelor vine imediat după `InputBridge.Dispatch` și înainte de procesarea input-ului. Așa vede în același frame schimbările făcute din timp, din Relay și din `InputHeld`. Headless, `SceneSimulationContext2D.Update()` livrează după `Relay.Drain()` și `RefreshSpatialItems()`.

### Ordinea evenimentelor

Întâi toate `Exited`, apoi toate `Entered`. În fiecare grup, evenimentele urmează ordinea de atașare a colliderului care primește evenimentul, apoi ordinea de atașare a lui `Other`. Ordinea de atașare este aceeași pe care o folosește `Overlap` (`CollisionWorld2D.cs:100-104`). Un collider care nu mai e în lume păstrează poziția de la cea mai recentă atașare, în orice lume.

### `Other` pentru un tile

Testul `ColliderContactCharacterizationTests.OverlapReportsTheInternalTileColliderWithTheMapAsEntity` arată că `Overlap` întoarce azi, pentru un tile static, un `TileStaticCollider2D` intern, cu `Entity` = `TileMap2D`, și același obiect la fiecare apel. `Other` are tipul public `Collider2D`. Obiectul din `Other` este același pe care `Overlap` și `Raycast` îl dau deja public prin `CollisionHit2D.Collider`. Deci `Other` nu expune un tip intern nou și nu e nevoie de o întrebare către utilizator.

### Unde stă starea de contact (Etapa 1)

Abatere de la „Arhitectura” de mai sus: lumea nu ține perechile care se ating. Fiecare collider ține colliderele cu care a raportat contact (`reportedContacts`) și colliderele care l-au raportat pe el (`reportedBy`), în `Collider2D.cs`. `CollisionWorld2D.Contacts.cs` ține numai colliderele urmărite (`trackedEntries`, în locul „contorului intern”) și lista celor murdare, și calculează diferența.

- Motiv: lumea unei scene se golește la `Reset` (scena devine imbricată sau iese din alt `Scene2D`, `Scene2D.cs:111-121`) și se reface la `Rebuild`. Dacă perechile ar sta în lume, s-ar pierde acolo. Exemplu: scena `g` raportează „coin entered fox”, contextul ei e închis, apoi `g` intră în scena `a`. Cu starea în lume, `a` ar da încă un `Entered` fără `Exited` înainte. Cu starea pe collider, nu vine niciun eveniment până când vulpea pleacă (testul `AContactReportedBeforeTheSceneWasNestedIsKeptWithoutNewEvents`).
- Regula care rezultă: pentru fiecare pereche (cine primește, `Other`), un `Entered` e urmat de exact un `Exited`, sau de niciunul cât timp contactul durează.
- Două rădăcini diferite: o pereche în care ambele collidere au ajuns sub aceeași altă rădăcină cu context de simulare este sărită; lumea aceea o evaluează (`BelongsToAnotherDeliveringScene`). Altfel colliderele nu se mai ating și vine `Exited`. Exemple: testele `MovingTheOtherSpriteToAnotherRootSceneRaisesExitedOnce` și `MovingAGroupThatHoldsBothCollidersToAnotherRootSceneRaisesNoEvents`.
- `Rebuild` nu marchează fiecare intrare: marchează la sfârșit numai colliderele cu stare de contact. Orice contact nou are cel puțin un collider cu handler, iar acela e marcat. `Reset` marchează colliderele cu stare de contact și golește `trackedEntries`.

### Excepții din handlere (Etapa 1)

Decizie după precedentul Relay (`UiRelayHostingIntegrationTests`: o greșeală din Relay iese ca `AggregateException`) și `SceneSimulationContext2D.Retire`: toate evenimentele livrării se ridică, excepțiile se adună și ies la sfârșit într-un singur `AggregateException`. Starea e deja actualizată, deci evenimentul care a aruncat nu se repetă (testul `AHandlerExceptionDoesNotStopTheOtherContactEventsAndIsRethrownAfterDelivery`). În `UiHost.Update`, excepția oprește restul acelui update, ca o excepție dintr-un handler de input.

### Limite cunoscute (Etapa 1)

- Timpul livrării intră în `UiFrameTiming.InputDispatch` (`UiHost.cs:186-190`, `:238`).
- Numai `UiHost.Update` livrează contactele unei scene de `RenderSurface2D`. Un `UIRoot.ProcessFrame()` simplu nu livrează.
- Cu cel puțin un handler în lume, fiecare collider schimbat face o interogare în broadphase la livrare. Costul pe frame și alocările nu au fost măsurate.
- O excepție internă din faza de calcul (de exemplu din `EnsureCurrent`) lasă evenimentele deja calculate pentru livrarea următoare.
- Lumea proprie a unei scene imbricate nu livrează. Marcajele puse acolo de `Reset` țin referințe la collidere până când scena redevine rădăcină cu context și livrează.
- Colliderele de tile sunt create din nou când harta reface un chunk de coliziune (`TileMap2D.Collision.cs:57-122`, `:142-168`). Un handler pus pe obiectul din `Other` se pierde atunci; handlerele se pun pe collidere de sprite.

### Trezirea (Etapa 2)

- `UIRoot.HasPendingSceneContacts` verifică exact lumile pe care le pregătește `DeliverSceneContacts` (`UIRoot.SceneContacts.cs:16-29` și `:47`). Livrarea golește lista. Deci, după o livrare fără alte schimbări, fereastra se oprește singură.
- Un `RenderSurface2D` are implicit `RedrawMode = Continuous` (`RenderSurface2D.cs:36`). Atunci el cere un frame la fiecare pump (`RenderSurface2D.cs:249-259`), cu sau fără contacte. Trezirea nouă schimbă ceva numai pentru `OnDemand`; de aceea testul de fereastră folosește `OnDemand`.
- Trezirea e prudentă: un collider schimbat care nu dă niciun eveniment tot cere un frame în plus.
- Un `SceneSimulationContext2D` independent nu intră în trezire, pentru că aplicația îi cheamă singură `Update()`.
- Fără handlere de contact nu se marchează nimic (`CollisionWorld2D.Contacts.cs:39-46`), deci noua condiție e mereu falsă. Testele native SDL care folosesc `PumpOnce` nu au fost rulate: schimbarea nu atinge cod de platformă.
- Docs: `Servo.md:115` și `:179`, plus un paragraf în `Collider2D.md`, „Contact events”. Lista din `Servo.md:115` nu pomenea nici înainte input hold-ul pe care îl verifică `ServoContext.cs:120`. Lipsa aceasta nu vine din acest plan și nu a fost corectată aici.

### Documentația (Etapa 3)

- `scene2d.md`: secțiunea nouă „### Collider contact events”, la sfârșitul „## Frame Integration”. Ea descrie regula de contact, unde stă starea, marcajul, livrarea în două faze și trezirea.
- `hosting-and-platform.md`: lista condițiilor de randare din `PumpOnce` are un punct nou, `UIRoot.HasPendingSceneContacts`.
- `servo.md`: „Idle means all seven are true”, cu punctul nou înainte de poarta Servo.
- În afara listei de fișiere: `invalidation-and-frame.md` are pasul nou 5 (livrarea contactelor) în ordinea unui `UiHost.Update`, iar `UiHost.cs:145-244` a devenit `:145-247`. `rendering.md:61` și `:207` citează acum `UiHost.cs:223` și `:222-240`, pentru că livrarea a mutat liniile cu 3.
- Citările `drawing.md:32`, `:44`, `:49`, `:54` și `input.md:38` erau deja greșite pe master `55c4ea20`. De exemplu, `drawing.md:32` trimite pentru timpul de frame la `WindowApplicationRuntime.cs:799-801`, unde e `if (application is not null)`. Ele nu au fost atinse aici.

### Auditul final

Alte proiecte de teste folosesc tipurile schimbate. Filtrele lor focalizate, pe codul final:

- `Cerneala.Tests.Scene2DPackages` (`Stage0PackageContractTests`, `Scene2DPackageTests`, `Scene2DPackageGridGeometryTests`, `Scene2DPackageEntityTests`, `PackageValueCodecTests`): 84/84.
- `Cerneala.Tests.SceneVillage` (`VillageScaleContractTests`, `VillageModelTests`): 29/29.
- `Cerneala.Tests.Language` (`ColliderOwnershipLanguageTests`): 21/21.
- `Cerneala.Tests.SourceGen`, testele de collider, coliziune, scenă și Motion clip din clasa parțială `UiMarkupGeneratorTests`: 68/68.
- `Cerneala.Tests.SdlGpu`, clasele fără fereastră nativă care folosesc `PumpOnce` sau Servo (`ServoPlatformInputTests`, `SdlInputTransitionTests` și altele): 106 trecute, 3 sărite, care cer `CERNEALA_SDL_NATIVE_TESTS=1`.
- Testele native SDL nu au fost rulate. Suita completă nu a fost rulată.
