# Etapa 4 — documentație și verificare completă

Plan: [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md), Etapa 4.

## Documentație

- `docs/CernealaMarkupGuide.md`: secțiunea nouă *Applying and replacing an aspect* (după §12 Templates) — `Quiet`/`Loud` cu înlocuire din C#, ce se întâmplă la înlocuire, unde se rezolvă `$Name` (namescope-ul de declarare; în `App.crn` doar `$self`/`$owner`), `$owner.parts`/`$owner.prism`/`$self.prism` într-un Aspect resursă (tipul din locurile de aplicare din markup, eroare de build la nepotrivire, excepție la runtime).
- `docs/timbre-guide.md`: paragraful despre noul Aspect care își aduce sunetele, după *Scope and lifetime*.
- docs-site `Cerneala.UI.Markup.GeneratedMarkup.md`: actualizat în Etapele 2–3 (helper-ele noi, overload-urile eliminate).
- Exemplele din ghiduri compilează: `Cerneala.Tests.SourceGen` (inclusiv `DocumentedTimbreExamplesCompile`) GREEN în rularea completă de mai jos.

## ApiCompat

Nicio schimbare de API public după Etapa 3 (singura modificare de cod din Etapa 4 este în `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`, intern). Rezultatul strict față de `59c0e257` rămâne cel din `../2026-10-08-aspect-runtime-program-stage3/api-compat-strict.log`, clasificat acolo: redenumirile Prism și Timbre (planul Timbre/Prism, Etapele 1–2), plus `ApplyAspectValue`, `GetTemplateOwner`, `GetTemplatePart`, `RequirePrismClip` adăugate și `AttachMotionSession(UIElement, ElementAspect?)`, `AttachTimbreSession(UIElement, ElementAspect?)` eliminate.

## Regresie cost

Probe temporar `AspectCostProbe.cs.txt` (scos din proiect după măsurare), rulat identic în worktree-ul `59c0e257` și în codul curent: 200 de butoane cu același program (`@when IsMouseOver` cu două `@if` care animă `Opacity`, `@on Click` cu animație), în trei variante: Aspect resursă, Aspect inline, Aspect implicit. Contoare din `LifecycleBehaviors`, `MarkupConditionController.EvaluationCount` și `InvalidationTrace` (Request/Queue/Phase).

| Varianta | Behavior-uri (baseline → acum) | Evaluări la stabilizare | Invalidări la stabilizare | Evaluări / invalidări în 30 frame-uri idle |
|---|---|---|---|---|
| resursă | 400 → 400 | 601 → 401 | 8261 → 8261 | 0 / 0 → 0 / 0 |
| inline | 600 → 400 | 1002 → 401 | 9465 → 8261 | 0 / 0 → 0 / 0 |
| implicit | 400 → 400 | 601 → 401 | 8261 → 8261 | 0 / 0 → 0 / 0 |

Defalcarea pe motiv/proprietate: `cost-baseline-59c0e25.txt`, `cost-current.txt` (identice, cu excepția invalidărilor `Aspect condition … changed` pe care baseline-ul inline le avea în plus).

Regresie găsită și reparată în această etapă: programul mutat în behavior comuta cheia de condiție (`AspectConditionKey.SetActive`) și pentru reguli care doar pornesc Motion/Timbre, fără valori sau conținut — 2 invalidări `Aspect` în plus pe buton. `UiMarkupReactiveEmitter` emite acum callback-ul de stare doar pentru regulile cu atribuiri sau conținut, ca înainte.

Timp (informativ, zgomotos; mediana a 5 rulări, creare + atașare, două rulări alternate): resursă ≈253/268 ms → ≈98/117 ms; inline ≈503/417 ms → ≈313/336 ms; implicit ≈226/227 ms → ≈47/90 ms. Doar atașarea singură părea mai lentă la inline (≈205 → ≈265 ms) pentru că sesiunile se creau înainte în constructor, iar acum la atașare.

## Verificare completă

- `dotnet build .\Cerneala.slnx -c Release -m:1`: 0 erori (`full/build.log`).
- `dotnet test .\Cerneala.slnx -c Release --no-build --no-restore -m:1` cu `CERNEALA_SDL_NATIVE_TESTS=1`, `CERNEALA_TIMBRE_AUDIO_DEVICE=1` (`full/test.log`, TRX în `full/`): Cerneala.Tests 4333; Language 374 / 1 skip; LanguageServer 45; PreviewHost 22; Scene2DImporters 173; Scene2DPackages 104; SceneVillage 43 / 1 skip; SdlGpu 1009 / 5 skip; SourceGen 644; Timbre 380; VisualStudio 48; Tetris 31. 0 eșecuri; skip-urile sunt preexistente.
- Smoke Windows (`smoke/*.log`): `--mode timbre` exit 0, `SDL_GPU_SMOKE_OK scenarios=18`; `--mode timbre-markup` exit 0, `scenarios=10`; `--mode timbre-motion` exit 0, `scenarios=8`.
- `git diff --check`: curat.
