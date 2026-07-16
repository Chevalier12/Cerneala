# Plan index: Motion markup

> Data: 2026-07-15
> Status: finalizat
> Sursa de decizie: `docs/motion-markup-syntax-proposal.md`
> Scop: implementam limbajul Motion declarativ in verticale livrabile, fara reflection, fara al doilea motor de animatie si fara sa ascundem lipsurile runtime sub workaround-uri.

## 1. Rezumat

Propunerea este prea mare pentru un singur checklist sanatos. Implementarea este impartita in sase planuri dependente, fiecare cu propriul contract, teste RED/GREEN si gate. Fiecare etapa trebuie sa lase produsul compilabil si verificabil; nu aruncam tot generatorul intr-un blender si speram ca iese IntelliSense.

## 2. Decizii comune

- Markup-ul este analizat si validat de Roslyn la build; runtime-ul nu cauta proprietati, events sau resources prin reflection.
- `Aspect` detine activarea, observarea, events si lifecycle-ul. `MotionClip` ramane o reteta generator-owned, fara clasa runtime omonima.
- Unqualified properties tintesc elementul Aspectului; `$Name.Property` este rezolvat static pentru fiecare loc in care Aspectul este aplicat.
- `@on` se rezolva exclusiv la un `IEventSymbol` de pe `TargetType` sau tipurile sale de baza. Generatorul nu injecteaza cod in metode cu acelasi nume.
- Toate subscriptions, observations si handles sunt per instanta de Aspect si sunt eliberate la detach/replacement.
- Generatorul coboara sintaxa la API-urile Motion existente. Cand contractul runtime nu poate sustine lifecycle-ul cerut, se scrie mai intai un test RED si se repara contractul real; nu se mascheaza defectul in cod generat.
- Sintaxa CSS-like Motion nu accepta binding modes. Nu exista `$event`, `@else`, transactions sau layout sequences programabile.

## 3. Planuri si dependente

1. `docs/plans/2026-07-15-motion-markup-foundation.md` DONE
2. `docs/plans/2026-07-15-motion-markup-composition-and-clips.md`, dependent de planul 1 DONE
3. `docs/plans/2026-07-15-motion-markup-timelines-and-specs.md`, dependent de planurile 1-2 DONE
4. `docs/plans/2026-07-15-motion-markup-presence-and-layout.md`, dependent de planul 1 DONE
5. `docs/plans/2026-07-15-motion-markup-scroll-and-input.md`, dependent de planul 1 DONE
6. `docs/plans/2026-07-15-motion-markup-integration-and-hardening.md`, dependent de planurile 1-5 DONE

Planurile 4 si 5 pot rula dupa foundation fara sa astepte timelines/clips. Planul 6 este gate-ul final si nu incepe pana cand suprafetele acceptate din celelalte planuri sunt GREEN.

## 4. Stop conditions

- Nu se implementeaza scrubbing, seek, reverse sau progress extern pentru keyframes.
- Nu se extind Presence, Layout, Scroll, Drag ori Gesture peste capabilitatile runtime documentate in proposal.
- Nu se adauga `Motion` ca resource sau obiect atasabil; resursele reutilizabile sunt specs, `Aspect` si `MotionClip`.
- Nu se construieste in acest set de planuri o extensie Visual Studio/LSP completa. Generatorul trebuie insa sa ofere diagnostics si source locations suficient de bune pentru tooling ulterior.
- Decay nu primeste sintaxa de executie inventata in implementare. Planul de timelines il accepta numai dupa ce defineste un contract declarativ care nu pretinde ca `@to` este folosit de sampler cand runtime-ul il ignora.

## 5. Gate-uri globale

- [x] Dupa fiecare modificare C# sau de proiect, ruleaza indexarea `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`.
- [x] Orice API public nou sau modificat are pagina sincronizata in `docs-site/documentation/classes/`, creata cu skill-ul `writing-api-documentation`; manifestul este actualizat cand o pagina este adaugata sau redenumita.
- [x] Fiecare plan dependent incepe numai dupa ce gate-ul final al dependentei este GREEN.
- [x] Niciun test de lifecycle nu se limiteaza la primul attach: fiecare controller nou este verificat prin attach/detach/reattach si replacement.
- [x] Full suite ramane GREEN cu `dotnet test .\Cerneala.slnx`.

## 6. Definitia de gata

- [x] Toate cele sase planuri sunt finalizate si bifate pe baza dovezilor, nu a optimismului.
- [x] Exemplele acceptate din proposal compileaza si au comportamentul documentat; exemplele deliberate invalide produc diagnostics precise.
- [x] Markup-ul generat nu foloseste reflection si nu face lookup dupa string in hot path.
- [x] Detach, cancel, replacement si repeated execution nu lasa subscriptions, handles sau Motion graph nodes active.
- [x] Documentatia conceptuala, API docs, sample-ul real si proposal-ul descriu aceeasi suprafata implementata.
