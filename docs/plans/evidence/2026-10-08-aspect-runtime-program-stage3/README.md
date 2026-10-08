# Etapa 3 — runtime și API public

Plan: [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md), Etapa 3.

## Schimbare

- `GeneratedMarkup.AttachMotionSession(UIElement, ElementAspect?)` și `AttachTimbreSession(UIElement, ElementAspect?)` eliminate, împreună cu logica „aspect-scoped” din `MarkupMotionSession` și `MarkupTimbreSession` (captura Aspect-ului, abonarea la `AspectProperty`, `IsOwnedByCurrentAspect`). Sesiunile sunt lifetime-uri ale behavior-ului Aspect-ului; înlocuirea Aspect-ului le dispune.
- Testul `MarkupMotionExecutionTests.AspectScopedSessionIgnoresActivationWhileItsAspectIsUnavailable` eliminat: verifica exact varianta scoasă; comportamentul nou e acoperit de testele de swap din `AspectRuntimeProgramTests`.
- `docs-site/documentation/classes/Cerneala.UI.Markup.GeneratedMarkup.md`: rândurile overload-urilor scoase, paragraful „Aspect-scoped overload”, `CanStartMotionExecution`, `AddTimbreTrigger`, exemplul `AttachTimbreSession(button)` și helper-ele noi din Etapa 2.

## Teste noi (`tests/Cerneala.Tests.Timbre/Markup/AspectRuntimeProgramTests.cs`)

| Test | Ce dovedește |
|---|---|
| `PresenceAndLayoutComeAndGoWithARuntimeAspect` | Un Aspect pus la runtime pe un element atașat aduce `Presence`, `LayoutMotion`, `LayoutMotionId` fără excepție; înlocuit, le ia înapoi (toate `null`). |
| `ReplacingTheAspectStopsItsRunningAnimationAndSoundAtOnce` | La jumătatea unei animații de 1000 ms și a unei redări, înlocuirea Aspect-ului anulează sincron redarea (`Canceled`), iar valoarea scrisă de animație revine la bază (`Opacity` 1) și rămâne acolo — „ce a adus Aspect-ul vechi dispare”. |
| `AHandlerCanReplaceTheAspectWhileItsClickIsDispatched` | Un handler `Click` care schimbă Aspect-ul în timpul click-ului nu aruncă; click-ul următor rulează programul noului Aspect. |
| `IdleFramesAfterAReplacementDoNoWork` | După înlocuire și 3 frame-uri de stabilizare, 30 de frame-uri fără input nu pornesc sunete, nu reevaluează condiții (`EvaluationCount` constant) și nu invalidează nimic (`InvalidationTrace`). |

## ApiCompat (strict, față de `59c0e257`)

`../2026-10-08-timbre-prism-in-aspect-stage1/api-compat.proj`, log `api-compat-strict.log`. Pe lângă redenumirile Prism și Timbre din planul Timbre/Prism (Etapele 1–2):

- adăugate: `GeneratedMarkup.ApplyAspectValue<T>(UiObject, UiProperty<T>, T)`, `GetTemplateOwner(UIElement)`, `GetTemplatePart<T>(UIElement, string)`, `RequirePrismClip(UIElement, string)`;
- eliminate: `GeneratedMarkup.AttachMotionSession(UIElement, ElementAspect?)`, `GeneratedMarkup.AttachTimbreSession(UIElement, ElementAspect?)`.

## Verificare

Build Release și Debug: 0 erori. TRX în acest director: `Cerneala.Tests` 4200 passed / 2 skipped; `Cerneala.Tests.Timbre` 380 passed; `Cerneala.Tests.SourceGen` 644 passed; `Cerneala.Tests.PreviewHost` 22 passed.
