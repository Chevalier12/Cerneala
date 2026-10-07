# Timbre markup/Aspect — etapa 2: evenimente, reactive și scope runtime

Snapshot: `master` @ `a838fc35` + modificările necomise ale planului. Host: Windows 11 Pro 10.0.26200 x64.

## Implementare

- Runtime (`UI/Markup/GeneratedMarkupSound.cs`): sesiune audio per aplicație concretă de Aspect (inclusiv ocurențe de
  template/item) = `ElementSoundOwner` (scope Timbre lazy din `UIRoot.SoundRuntime`) + sloturi nume→`SoundHandle`.
  Detach, înlocuirea Aspect-ului capturat și dispose (retragerea template-ului) fac `Dispose` scope-ului; renderability e
  ignorată. Helpers publici: `AttachSoundSession`, `AddSoundTrigger`, `PlaySound`, `GetSoundParameter`, `CancelSound`,
  `PauseSound`, `ResumeSound`, `SeekSound`, plus `CanStartMotionExecution` pentru corpurile mixte.
- Core: `SoundPlayback.TryPause/TryResume/TrySeekAsync` (internal) tratează atomic sub `runtime.Sync` un ocupant devenit
  terminal ca slot gol; API-ul public își păstrează excepțiile. `SoundRuntime.PlaybackAccepted` (instrumentare internă
  de test).
- Condiții: `MarkupConditionRule(…, Action<Action?>? soundActivated)` (ctor aditiv) și sidecar-ul `soundActiveRules` din
  `MarkupConditionController`: activare audio independentă de renderability, amânată în același `Relay.Post` ca cea
  vizuală, fără repetare la reevaluare sau hide→show, re-armată la true→false, golită la detach. Când ambele părți sunt
  datorate, activarea vizuală e invocată la poziția Motion din corp (ordinea sursei).
- SourceGen: `EmitSoundActivations` mapează nodurile audio (ordine ordinală a cursorului) pe acțiunile legate din
  `SoundMarkupModel`, emite câte o `Action` pe instrucțiune, handler-ele `@on` cu audio prin `AddSoundTrigger` (Motion
  păzit de `CanStartMotionExecution`), iar regulile reactive primesc `soundActivated`. `@cancel` pe handle audio este
  dispatch audio; Aspect-urile cu audio nu folosesc package behavior. Nepotrivirea nod/model raportează eroare, nu omite.
- Bug găsit și reparat în timpul etapei: Aspect-urile inline imbricate în `@template`-ul altui Aspect (și SoundClip-urile
  din acel conținut) nu erau legate de Language; `BindNestedSoundOwners` le leagă, iar SourceGen nu mai poate omite tăcut
  acțiuni fără model. Regresii: corpus `sound.action.componentTemplate`, `sound.ref.unknownClipInTemplate`, fixture
  SourceGen cu `@template` și testul runtime de retragere a template-ului.

## Harness

`tests/Cerneala.Tests.Timbre/Markup/`: `GeneratedSoundConsumer` rulează `UiMarkupGenerator` real, compilează ca
asamblare consumer obișnuită (fără acces intern, fără device) și o încarcă într-un `AssemblyLoadContext` colectibil;
`MarkupSoundFixture` scrie WAV float32/48 kHz deterministe, folosește `TimbreRig` (runtime real + sink determinist),
`UIRoot` cu ceas Motion manual, `UiHost` și Servo pentru input user-like, și curăță asamblarea și fișierele.

## Dovezi (Release)

| Suită | Conținut | Rezultat |
| --- | --- | --- |
| `SoundAspectIntegrationTests` | click Servo cu overlap, handle replacement/cancel fără efect asupra altor redări, initial true, reevaluare stabilă, false fără stop, Hidden/Collapsed + ancestor, detach/reattach | 7 passed |
| `SoundMarkupTransportTests` | slot gol no-op, Pause în Pending fără primul PCM până la Resume, latest pending seek (PCM de la 100 ms), cancel anulează seek pending, replacement fără seek stale, ocupant terminal = slot gol, Loop default/override fără mutarea clipului, două scheme pe un handle, redări pauzate ocupă quota și eroarea sincronă oprește corpul | 9 passed |
| `SoundAspectLifecycleTests` | două ocurențe ale aceluiași Aspect, ocurențe ItemsControl + retragere/swap, template de componentă, înlocuire Aspect, ascuns cu transport + Motion vizual la show, 100× attach/detach (101 starturi, LiveScopes=1, ActiveVoices=0), cancel pending + reader eliberat, eșec la deschiderea device-ului fără oprirea corpului, reentranță, cursă transport vs completare (Barrier, 200 cicluri), bubbling Servo, idle 30 frame-uri (0 starturi, 0 evaluări, 0 invalidări) | 12 passed |
| `SoundMarkupParityTests` | factory și paired partial vs C# manual: aceleași stări, poziții, trace (started/canceled/pause/resume/seek/loop wraps/blocks) și PCM (toleranța 1e-6) pentru override-uri, LowPass+Delay, overlap, Loop, replacement, Pause/Resume/Seek, cancel; clip declarat neutilizat = 0 starturi | 2 passed |
| `UiMarkupGeneratorTests.Sound*` | + acțiuni în factory/paired: simboluri legate de `GeneratedMarkup.*`, `SoundStartOptions.Volume/Loop/Set<float>`, ctor-ul `MarkupConditionRule` cu 8 parametri, `RegisterLifetime` pentru `@template` | 7 passed |
| `SoundSemanticTests` | corpus 56 cazuri + legare | 63 passed |

RED: testele de integrare au eșuat pe consumerul generat executat înainte de implementare (0 redări pornite, nu erori
de compilare/fixture). Pentru transport, o mutație temporară care coboară Pause/Resume/Seek ca no-op a făcut să pice exact
cele 4 teste dependente de transport; mutația a fost eliminată și suita a revenit la verde. Cele 22 de teste runtime
au trecut în 4 rulări repetate consecutive.

Suita completă `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` (legacy visual/cascade/routing
inclus): toate proiectele verzi, vezi `full-suite.txt` (Cerneala.Tests 4195 passed/2 skipped preexistente, SourceGen 632,
Timbre 336, Language 322/1 skipped, LanguageServer 40, PreviewHost 17, VisualStudio 47, SdlGpu 600/241 skipped native).

Eșec preexistent găsit la prima rulare completă și reparat în owner: `ApplicationSoundsIntegrationTests` pica și pe un
worktree curat la `a838fc35` (3/3) — condiția de submit a mixerului `queued + Block <= QueueBudget` făcea overflow `int`
când un output raporta `QueuedFrames = int.MaxValue`, trimitea PCM și declara device lost. Comparația este acum
`queued <= QueueBudget - Block` (`Timbre/SoundRuntime.Mixer.cs`); testul existent este regresia.

Bugetul LSP `FullSolutionIncrementalRequestsRespectWarmBudgets` a ieșit o dată 110.68 ms (>100 ms) în suita completă;
izolat a trecut 5/5. Scanarea Language a directivelor audio construia un buffer cât tot conținutul fiecărui element
(cost O(mărime × adâncime) adăugat de această etapă); acum verifică întâi nodurile text directe. Rerularea completă a
trecut.
