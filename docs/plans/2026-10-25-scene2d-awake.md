# Plan: `IsAwake` și `Awake` pe `Sprite2D` și `Scene2D`

> Data: 2026-10-25
> Status: finalizat
> Index: [scene2d-autonomie-index](2026-10-25-scene2d-autonomie-index.md), planul 3
> Depinde de: nimic (`InputHeld` este deja pe master)
> Blocat de: nimic (D9 decisă pe 2026-10-26, vezi „Note de lucru”)

## Rezumat

Un obiect treaz primește un eveniment o dată pe frame. Un obiect care doarme nu primește nimic și nu ține fereastra trează.

```csharp
bee.IsAwake = true;
bee.Awake += (_, e) =>
{
    hunger -= (float)e.Elapsed.TotalSeconds;
    if (hunger < 25)
    {
        bee.IsAwake = false;    // de aici albina nu mai cere frame-uri
    }
};
```

Azi singurul cod per frame este `RenderSurface2D.Draw`, unul pe suprafață, nu pe obiect. Un `RenderSurface2D` în modul `Continuous` cere un frame la fiecare pump, chiar dacă nimic nu se schimbă (`RenderSurface2D.cs:249-259`).

## Baseline (verificat pe master `55c4ea20`)

- `SceneNode2D` nu are niciun hook public per frame. Singurul hook per nod este `AdvanceAnimation(TimeSpan)`, intern (`UI/Controls/SceneNode2D.cs:145-170`).
- `RenderSurface2D` ține lista internă `activeAnimations` (`RenderSurface2D.cs:76`) și o avansează în `ITimeSensitiveRenderElement.UpdateRenderTime` (`:184-260`), cu un `SpriteAnimationClock` per suprafață. Nodurile scoase din cadru sunt „parcate” (`TryParkAnimation`, `:366-377`).
- `UpdateRenderTime` vine din `UiHost.AdvanceRenderTime` (`UI/Hosting/UiHost.cs:122-128`) prin `TimeSensitiveRenderInvalidator`.
- `PumpOnce` cheamă `AdvanceRenderTime` la fiecare pump pentru o fereastră afișată (`WindowApplicationRuntime.cs:664`). Apoi randează numai dacă cineva cere: `RenderRequested`, Relay, `Scheduler.HasWork`, Motion, `HasActivePointerRepeat`, `HasActiveInputHold` (`:665-671`).
- Pentru o fereastră ascunsă sau cu viewport zero, `PumpOnce` nu avansează timpul (`:656-661`).
- `ServoContext.IsIdle` (`UI/Servo/ServoContext.cs:112-121`) nu știe de animațiile suprafeței.
- Nu există `FrameTimeEventArgs` sau un tip asemănător. Cel mai apropiat este `InputStateEventArgs.Elapsed` (`UI/Input/InputStateEventArgs.cs`), cu regula „0 pe primul frame, apoi timpul frame-ului”.
- Headless, `SceneSimulationContext2D.Update()` nu primește timp (`UI/Controls/SceneSimulationContext2D.cs:57-70`).

## Obiective

- `public bool IsAwake` (implicit `false`) și `public event EventHandler<FrameTimeEventArgs>? Awake` pe `Sprite2D` și `Scene2D`, nu pe `SceneNode2D`.
- `public sealed class FrameTimeEventArgs : EventArgs` cu `TimeSpan Elapsed`. `Elapsed` este 0 la primul `Awake` după ce obiectul devine treaz, apoi timpul frame-ului. E aceeași regulă ca `InputHeld`. Se confirmă în Etapa 0.
- `Awake` vine o dată pe frame, numai cât timp `IsAwake` este `true` și obiectul este într-o scenă atașată la un `RenderSurface2D` live.
- `Awake` vine și pentru obiectele scoase din cadru: jocul continuă și în afara ecranului. Parcarea animațiilor nu se aplică.
- Într-un handler:
  - `IsAwake = false` oprește evenimentele de la frame-ul următor;
  - un obiect trezit acum primește primul `Awake` la frame-ul următor, nu în același pas.
- Ordinea în frame: `Awake` vine la avansarea timpului, înainte de input (`InputHeld`). Ordinea între obiecte este deterministă.
- Trezirea ferestrei: cât timp există un obiect treaz, `PumpOnce` continuă să avanseze timpul, iar Servo `IsIdle` este `false`. Când nimic nu e treaz, nu se cer frame-uri.

## Non-obiective

- `Awake` headless (D9, recomandare: nu în prima livrare).
- Un `Awake` global sau pe `RenderSurface2D`.
- Limitarea lui `Elapsed` după o fereastră ascunsă mult timp. Etapa 0 caracterizează valoarea. Dacă apar salturi mari, se întreabă utilizatorul.
- Schimbarea modului `Continuous` al `RenderSurface2D`.

## Arhitectura

- **Starea:** o proprietate UI `IsAwake` pe fiecare clasă, cu un helper intern comun în `SceneNode2D` (registrare și timp), fără membru public pe `SceneNode2D`.
- **Owner:** `RenderSurface2D` ține o listă internă de noduri treze, separată de `activeAnimations`, pentru că nodurile treze nu se parchează.
  - Un nod intră în listă când `IsAwake` devine `true` și nodul este atașat la suprafață (`OnSimulationContextChanged`, atașare sau detașare).
  - Iese la `IsAwake = false` sau la detașare.
- **Livrare:** `UpdateRenderTime` ridică `Awake` pentru o copie a listei, după avansarea animațiilor:
  - se ia o copie, ca schimbările din handlere să conteze de la frame-ul următor;
  - nodurile nou trezite primesc `Elapsed = 0`.
- **Trezire:** o proprietate internă `HasAwakeNodes`, agregată pe root. Etapa 0 stabilește prin caracterizare dacă `PumpOnce` are nevoie de ea în condiția de randare: `AdvanceRenderTime` rulează oricum la fiecare pump. Servo `IsIdle` are sigur nevoie de ea.

## Fișiere estimate

- `UI/Controls/FrameTimeEventArgs.cs`, nou.
- `UI/Controls/SceneNode2D.cs`, `UI/Controls/Sprite2D.cs`, `UI/Controls/Scene2D.cs`, `UI/Controls/RenderSurface2D.cs`.
- `UI/Hosting/Windowing/WindowApplicationRuntime.cs` (dacă Etapa 0 cere), `UI/Servo/ServoContext.cs` și driverul Servo.
- Teste noi: `tests/Cerneala.Tests/Controls/SceneAwakeTests.cs`. Se extind `WindowRuntimeTests` și `ServoSynchronizationTests`.
- Docs:
  - pagina nouă `Cerneala.UI.Controls.FrameTimeEventArgs.md` și intrarea ei în manifest;
  - paginile `Sprite2D` și `Scene2D`;
  - `docs/architecture/scene2d.md`, `hosting-and-platform.md`, `servo.md`.

## Etapa 0 — contract și teste RED

- [x] Se confirmă D9 și regula `Elapsed` cu utilizatorul. (2026-10-26, vezi „Note de lucru”.)
- [x] Caracterizare (GREEN): un `PumpOnce` fără muncă tot avansează timpul pentru o fereastră afișată. O fereastră ascunsă nu avansează timpul. Se notează `frameTime` după revenirea din ascuns. (`WindowRuntimeTests.PumpOnceWithNothingToDoStillAdvancesRenderTimeForAShownWindowAndRendersNoFrame` și `PumpOnceDoesNotAdvanceRenderTimeForAHiddenWindowAndTheTimeIsNotCarriedOver`, 2/2 pe codul neschimbat. După revenirea din ascuns, primul `PumpOnce(16 ms)` dă exact 16 ms: timpul cât fereastra a fost ascunsă se pierde, deci nu apar salturi mari în `Elapsed`.)
- [x] Teste RED în `SceneAwakeTests`, pe `Sprite2D` și pe `Scene2D`: (plus cazurile din „Note de lucru”: adormit sau scos de alt handler, handler care aruncă, headless, suprafață scoasă din root, exemplul din Rezumat)
  - fără `IsAwake` nu vine niciun `Awake`;
  - cu `IsAwake = true` vine câte un `Awake` pe update, cu `Elapsed` 0, apoi timpul frame-ului;
  - după `IsAwake = false`, evenimentele se opresc;
  - un obiect scos din cadru primește în continuare `Awake`;
  - un obiect scos din scenă nu mai primește `Awake`;
  - un obiect trezit dintr-un handler începe la frame-ul următor;
  - ordinea între obiecte este deterministă;
  - `Awake` vine înaintea `InputHeld` din același update.
- [x] Test RED: Servo `WaitForIdle` nu se termină cât timp un obiect este treaz. Se verifică întâi decizia din issue-ul [#762](https://github.com/Chevalier12/Cerneala/issues/762): ea poate schimba regula. (Decizia utilizatorului a schimbat regula: `ServoSynchronizationTests.WaitForIdleCompletesWhileASpriteOnASurfaceIsAwake` cere ca `WaitForIdleAsync` să se termine cât timp albina e trează.)

### Gate etapa 0

- [x] RED-urile pică pentru că lipsesc `IsAwake`, `Awake` sau `FrameTimeEventArgs`. (Vezi „Dovada RED”.)
- [x] Comanda: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~SceneAwake"`. (Nu compilează: o singură eroare, `CS0246` pentru `FrameTimeEventArgs`, `SceneAwakeTests.cs:491`.)

## Etapa 1 — `IsAwake`, `Awake` și registrul pe suprafață

- [x] Se adaugă `FrameTimeEventArgs`, proprietățile și evenimentele pe `Sprite2D` și `Scene2D`, și helper-ul intern în `SceneNode2D`. (`IsAwakeProperty` fără opțiuni de invalidare, deci schimbarea lui `IsAwake` nu cere singură un frame.)
- [x] Se adaugă lista de noduri treze în `RenderSurface2D`, cu atașare, detașare și livrare în `UpdateRenderTime`. (`awakeNodes`, în ordinea trezirii, cu tombstone la scoatere; livrarea `DeliverAwake` vine după animații și după decizia `InvalidateFrame`, pe o copie.)

### Gate etapa 1

- [x] Testele din `SceneAwakeTests` sunt GREEN. (28/28. Două dintre ele au fost adăugate la gate: ordinea când suprafața intră în root după scenă și validarea `FrameTimeEventArgs`.)
- [x] Testele de animație și suprafață existente rămân GREEN: `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~SpriteAnimation|FullyQualifiedName~RenderSurface2D|FullyQualifiedName~Scene2D|FullyQualifiedName~Sprite2D"`. (147/147.)

## Etapa 2 — trezirea ferestrei

- [x] Se adaugă `HasAwakeNodes` în Servo `IsIdle`, și în `PumpOnce` dacă Etapa 0 a arătat că trebuie. (Nu a fost necesar. Servo: utilizatorul a decis că `IsIdle` ignoră obiectele treze, vezi „Note de lucru”. `PumpOnce`: caracterizarea din Etapa 0 arată că timpul avansează la fiecare pump al unei ferestre afișate (`WindowApplicationRuntime.cs:664`), iar `WindowRuntimeTests.AnAwakeSpriteGetsAwakeOnEveryPumpWithoutRequestingFrames` arată `Awake` cu `Elapsed` 0, 16 ms, 16 ms pe trei pump-uri care nu randează. Deci `HasAwakeNodes` nu există.)
- [x] Test: după ce ultimul obiect adoarme, `PumpOnce` nu mai randează frame-uri fără altă muncă. (`WindowRuntimeTests.AfterTheLastAwakeObjectSleepsPumpOnceRendersNoMoreFrames`: handler-ul mută sprite-ul, deci primele trei pump-uri randează câte un frame; după ce adoarme, trei pump-uri întorc `false`, fără frame-uri și fără `Awake`.)

### Gate etapa 2

- [x] `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --filter "FullyQualifiedName~WindowRuntime|FullyQualifiedName~Servo|FullyQualifiedName~SceneAwake"` este GREEN. (286/286, inclusiv cele două teste de caracterizare din Etapa 0 și testul Servo.)

## Etapa 3 — documentație și verificare

- [x] Pagina `FrameTimeEventArgs`, intrarea în manifest și paginile `Sprite2D` și `Scene2D`, cu exemplu. (Secțiunile `### Awake`, rândurile din tabele și tabelul `## Events`. `ApiDocumentationManifestIsValidAndReferencesExistingFiles`: 1/1.)
- [x] `docs/architecture/scene2d.md` descrie registrul de noduri treze. `hosting-and-platform.md` și `servo.md` listează condiția nouă. (Nu există condiție nouă: cele două pagini spun explicit că obiectele treze nu sunt condiție de randare și nu blochează Servo idle, cu testele. `scene2d.md` are secțiunea `### Awake`, iar citările mutate de cod în `RenderSurface2D.cs`, `Sprite2D.cs`, `Scene2D.cs` și `SceneNode2D.cs` au fost actualizate.)
- [x] Raportul spune că suita completă nu a fost rulată. (Și secțiunea `### Awake` din `scene2d.md`, commit-ul și PR-ul.)

## Ordinea recomandată

Etapa 0 → 1 → 2 → 3.

## Definiția de gata

- [x] Toate casetele de mai sus sunt bifate.
- [x] Exemplul din Rezumat compilează într-un test și trece. (`SceneAwakeTests.TheSummaryExampleCompilesAndWorks`: 7 evenimente, `hunger` 24, apoi niciun `Awake`.)
- [x] O scenă fără obiecte treze nu cere frame-uri în plus (testul din Etapa 2). (`WindowRuntimeTests.AfterTheLastAwakeObjectSleepsPumpOnceRendersNoMoreFrames`.)

## Note de lucru

### Decizii ale utilizatorului (2026-10-26, Etapa 0)

- **D9:** `Awake` nu vine headless în prima livrare. Cu `SceneSimulationContext2D.Update()` și fără `RenderSurface2D`, `Awake` nu vine deloc.
- **`Elapsed`:** primul `Awake` după ce obiectul devine treaz are `Elapsed` 0, apoi timpul frame-ului, ca la `InputHeld` (`UI/Input/InputStateEventArgs.cs:25`).
- **Servo (issue [#762](https://github.com/Chevalier12/Cerneala/issues/762), întrebarea 1):** `ServoContext.IsIdle` ignoră obiectele treze. `bee.IsAwake = true; await servo.WaitForIdleAsync();` se termină când nu mai e muncă UI. Testul RED din Etapa 0 verifică asta, nu contrariul. Timpul fix pentru Servo (pași de 16 ms) rămâne în #762.
- **Ordinea între obiecte:** ordinea trezirii. Cine devine treaz primul primește `Awake` primul. Un obiect care adoarme și se trezește din nou trece la coadă. Obiectele deja treze dintr-o scenă pusă într-un `RenderSurface2D` intră în ordinea din scenă (părintele înaintea copiilor).

### Decizii de implementare

- **Schimbări din handlere (decide obiectivul „`Awake` vine ... numai cât timp `IsAwake` este `true` și obiectul este într-o scenă atașată”):** `Awake` vine numai cât timp obiectul este treaz și atașat. Un obiect adormit sau scos din scenă de handler-ul altui obiect, în același frame, nu mai primește `Awake` în acel frame. Un obiect trezit în timpul livrării începe la frame-ul următor, cu `Elapsed` 0.
- **Handler care aruncă:** ca la `CollisionWorld2D.RaiseContacts` (`UI/Controls/CollisionWorld2D.Contacts.cs:228-247`). Celelalte obiecte treze primesc `Awake` în același update. Apoi update-ul aruncă `AggregateException` cu excepțiile handlerelor.
- **Testele folosesc `VisionHostRig`** (`tests/Cerneala.Tests/Controls/VisionHostRig.cs`): un update = un `UiHost.Update` cu timpul dat, deci un `UpdateRenderTime` pe suprafață.
- **Registrul pe suprafață (Etapa 1):** `RenderSurface2D.awakeNodes` este o listă în ordinea trezirii. Scoaterea lasă un `null` (O(1), ordinea celorlalte rămâne). Livrarea compactează lista, iar scoaterea o compactează și ea când tombstone-urile trec de 32 și sunt mai multe decât nodurile vii, ca lista să nu crească fără livrări. Fiecare înregistrare crește `AwakeVersion`; copia livrării ține `(nod, versiune)`, așa că un nod adormit, scos sau re-trezit în timpul livrării este sărit.
- **Alocări:** argumentele sunt împărțite: o instanță `FrameTimeEventArgs.Zero` și una per suprafață, refolosită cât timp timpul frame-ului se repetă. Alocările nu au fost măsurate.
- **Limitare:** schimbarea lui `IsAwake` crește versiunea de proprietăți a nodului. Probabil `Sprite2D` își reface o dată snapshot-ul și comanda înregistrată la următorul frame desenat (neverificat: dedus din `recordedVersion` în `Sprite2D.Record`, fără test). Nu cere frame (testul din Etapa 2). Costul nu a fost măsurat.
- **Abatere de la arhitectură (Etapa 2):** proprietatea `HasAwakeNodes` din secțiunea „Arhitectura” nu a fost adăugată. Nici `PumpOnce`, nici Servo nu au nevoie de ea (vezi Etapa 2). Un obiect treaz nu face ca fereastra să randeze. Primește `Awake` la fiecare pump, chiar fără frame. Doar schimbările făcute de handler (de exemplu `sprite.X += 1`) cer frame-ul, ca orice altă schimbare.
- **Limitare (din sursă, netestată, Etapa 3):** o fereastră ascunsă nu avansează timpul în `PumpOnce`, cu o excepție. Cât timp o operație Servo mai are de trimis o eliberare (de tastă sau buton), fereastra ascunsă rulează `DrainServoCleanupWithoutPresentation` (`WindowApplicationRuntime.cs:660`). Acesta cheamă `Host.Update` cu timp zero (`:1370`), deci obiectele treze primesc un `Awake` cu `Elapsed` 0. Documentat în `scene2d.md` și pe pagina `Sprite2D`.
- **Documentație:** fără exemple de markup `IsAwake="True"`, pentru că markup-ul nu are test. Paginile din `docs-site` nu au link spre `docs/`, ca restul paginilor.
- **Netestat:** apărarea contra livrării imbricate (`InvalidOperationException("Scene awake delivery cannot be nested.")`) și pragurile compactării.

### Verificarea finală (2026-10-26)

Doar teste focusate; suita completă nu a fost rulată. Pe codul final:

- `Cerneala.Tests`, gate-urile etapelor: `SceneAwake` 28/28; `SpriteAnimation|RenderSurface2D|Scene2D|Sprite2D` 147/147; `WindowRuntime|Servo|SceneAwake` 286/286.
- `Cerneala.Tests`, `SceneVision|ColliderContact|SceneItems|TileMap|Collider2D|Collision|InputHeld`: 721/721.
- `Cerneala.Tests.Language`, clasele care folosesc tipurile schimbate: 138/138.
- `Cerneala.Tests.Scene2DImporters`: 173/173. `Cerneala.Tests.Scene2DPackages`: 104/104. `Cerneala.Tests.SceneVillage`: 29 trecute, 2 sărite (teste native fără `CERNEALA_SDL_NATIVE_TESTS`), 31 total.
- `Cerneala.Tests.SourceGen`, `Sprite|Scene`: 112/112. `Cerneala.Tests.Timbre`, `OwnerAccessTests`: 10/10.
- `Cerneala.Tests.SdlGpu`, nativ (`CERNEALA_SDL_NATIVE_TESTS=1`), `RenderSurface2DPresentationTests`: 8/8.
- `Cerneala.Tests.VisualStudio`, `ApiDocumentationManifestIsValidAndReferencesExistingFiles`: 1/1.

### Dovada RED (Etapa 0)

- Comanda din gate: exit code 1, o singură eroare, `SceneAwakeTests.cs(491,66): error CS0246: The type or namespace name 'FrameTimeEventArgs' could not be found`. Compilatorul se oprește la erorile din declarații.
- Cu un `FrameTimeEventArgs` gol, pus temporar în fișierul de teste și apoi șters (rulat de subagent): 45 de erori, toate simboluri lipsă: `IsAwakeProperty` (`CS0117`), `IsAwake` și `Awake` (`CS1061`), pe `Sprite2D` și `Scene2D`, în `SceneAwakeTests.cs` și `ServoSynchronizationTests.cs`.
- Caracterizarea a fost rulată de subagent pe codul neschimbat, înainte să existe fișierele RED: `Passed! - Failed: 0, Passed: 2, Total: 2 - Cerneala.Tests.dll (net8.0)`. Se rulează din nou la gate-ul etapei 2 (filtrul `WindowRuntime`).
