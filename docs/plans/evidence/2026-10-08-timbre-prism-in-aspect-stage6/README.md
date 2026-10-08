# Etapa 6 — SourceGen

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 6.

## Schimbări

- `UiMarkupTimbreEmitter.cs`: `<TimbreClip>` → `TimbreClipDefinition` (parametrii clipului o dată, câte un `TimbreSound` per `@sound` cu parametrii folosiți, `TimbreClipSound(..., autoPlay: true)`); `@timbre { … }` inline → câmp static al tipului generat (o definiție per Aspect); `@timbre` → `global::System.IDisposable timbreAttachmentN = GeneratedMarkup.AttachTimbre(target, …, argumente)` în behavior-ul Aspect-ului; comenzile → `PlayTimbre/StopTimbre/PauseTimbre/ResumeTimbre/SeekTimbre(<țintă>, "Sunet")`, cu ținta `target` / `GetTemplateOwner(target)` sau contextul template-ului / variabila elementului numit. Sesiunea Timbre (handler-e `@on`, Motion audio) se creează doar dacă există comenzi sau Motion audio.
- `UiMarkupTimbreMotionEmitter.cs`, `UiMarkupMotionResolver.cs`, `UiMarkupMotionActivationEmitter.cs`: țintele `$self|$owner|$Name.timbre.Sunet.Proprietate` → `StartTimbreMotionProperty(<țintă>, "Sunet", "Proprietate", …)`.
- `UiMarkupDirectiveParser*.cs`, `UiMarkupAspectReader.cs`: `@timbre` (nod de atașare, numai la începutul Aspect-ului) și `@prism` acceptate în corpul Aspect-ului; comenzile `@play/@stop/@pause/@resume/@seek`.
- Prism: `@prism`-ul unui Aspect se leagă o dată (cheie = corpul Aspect-ului; resursele se caută din `DeclaringElement`), fiecare element primește Prism-ul Aspect-ului lui static (pentru țintele Motion), iar `AttachPrism(target, …)` e un lifetime al behavior-ului (`EmitAspectPrism`). `$self.prism` într-un Aspect cu `@prism` propriu folosește direct acel `@prism`.
- Emisia veche eliminată: sloturile/`timbreCancels`, `PlayTimbre(session, ResourceId<TimbreSound>, …)`, legarea și emisia `@prism` din conținutul elementului (`EmitPrismApplication` per element, bucla din `BindPrism`, `DirectiveContentKind.Prism` în conținut).
- Reparat (necesar migrării): Aspect-ul inline pe un control cu prefix CLR (`<local:X.Aspect>`) — generatorul rezolvă acum tipul prin `xmlns` înainte să citească Aspect-ul (decizia 14).

## Teste migrate / noi

- `UiMarkupGeneratorTimbreTests` (constructorii exacți `TimbreClipDefinition/TimbreClipSound/TimbreSound/...`, `SetResource<TimbreClipDefinition>`, definițiile la runtime identice cu C#, helper-ele `GeneratedMarkup` legate semantic, argumentele ca `Dictionary<string, float>`, fără `TimbreStartOptions`, fără `RegisterLifetime`), `UiMarkupGeneratorTimbreMotionTests` (inclusiv țintă numită), `UiMarkupGeneratorTimbreAttachmentTests` (RED din Etapa 0), `PrismMarkupContractTests` și testele RenderSurface2D/3D, SceneComponent, Sprite, TileMap cu `@prism` mutat în Aspect.
- Paritatea markup ↔ C# (`TimbreMarkupParityTests`, factory și partial-paired) și fixture-urile template/Scene2D din corpus: GREEN (Etapa 5 / corpus Language).
- Diagnosticele Language ↔ SourceGen coincid pe toate corpusurile (testele `*AgreeOnCorpus` din `Cerneala.Tests.Language`).

## Verificare

TRX în acest director, după ultima schimbare de cod: `Cerneala.Tests` 4200 passed / 2 skipped (preexistente); `Cerneala.Tests.SourceGen` 647 passed; `Cerneala.Tests.Timbre` 391 passed; `Cerneala.Tests.Language` 408 passed / 1 skipped (preexistent). `dotnet build .\Cerneala.slnx -c Release -m:1`: 0 erori (inclusiv Playground, Tetrisish, fixture-urile VS și smoke, migrate la sintaxa nouă).
