# Etapa 5 — Runtime

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 5.

## Schimbări

- `UI/Timbre/TimbreAttachment.cs` (nou, intern): sunetele aduse de o aplicare a unui Aspect — un `TimbreClipDefinition`, câte un slot (`TimbreHandle`) per sunet într-un scope propriu (`ElementTimbreOwner`), argumentele `@timbre` aplicate la pornirea fiecărui sunet care folosește parametrul. `Attach` pornește sunetele `AutoPlay` în ordinea declarării și devine atașarea curentă a elementului (`ConditionalWeakTable<UIElement, …>`); `Dispose` oprește tot și scoate înregistrarea doar dacă e încă cea curentă. `Require(target, sound)` aruncă `InvalidOperationException` pentru element detașat, element fără `@timbre` sau sunet nedeclarat.
- `UI/Markup/GeneratedMarkupTimbre.cs`: `AttachTimbre` (resursă, rezolvată la aplicare, și inline), `PlayTimbre/StopTimbre/PauseTimbre/ResumeTimbre/SeekTimbre(UIElement, string)`, `StartTimbreMotionProperty(UIElement, string sound, …)`; sesiunea Timbre păstrează doar handler-ele `@on` și execuțiile Motion audio (fără sloturi). Eliminate overload-urile pe sesiune + handle.
- docs-site: `Cerneala.UI.Markup.GeneratedMarkup.md` (metode, remarks, exemplu, excepții), `Cerneala.UI.Markup.MarkupConditionRule.md` (exemplul `PlayTimbre`).

## Teste (deterministe: `DeterministicTimbreOutput`, `ManualClock`)

| Cerință | Test |
|---|---|
| AutoPlay la aplicare | `TimbreAttachmentMarkupTests.AutoPlaySoundStartsWhenTheAspectIsApplied` (RED din Etapa 0) |
| `@play` repornește | `TimbreAttachmentMarkupTests.PlayRestartsTheSameSound`, `TimbreAspectIntegrationTests.EachUserClickRestartsTheSound` |
| stop/pause/resume/seek | `TimbreMarkupTransportTests` (9), `TimbreMarkupParityTests` (factory + paired, identic cu C#) |
| detach oprește | `TimbreAspectIntegrationTests.DetachCancelsOwnedPlaybacksAndReattachActivatesAgain`, `TimbreAspectLifecycleTests.HundredAttachDetachCyclesStartOncePerAttachAndReleaseEveryScope`, `DetachCancelsAPendingPlaybackAndReleasesItsReader` |
| swap oprește, noul Aspect aplică | `TimbreAttachmentMarkupTests.ReplacingTheAspectRemovesItsSoundsAndPrismAndAppliesTheNewOnes`, `AspectRuntimeProgramTests` |
| ascunderea nu oprește | `TimbreAspectIntegrationTests.HiddenControlStartsInitialTrueAndFalseToTrueWithoutReplayOnShow`, `HiddenAncestorDoesNotCancelOrReplayAudio`, `TimbreAspectLifecycleTests.HiddenControlKeepsAudioTransportWhileVisualMotionKeepsItsLifecycle` |
| țintă neatașată / fără sunet aruncă | `TimbreAttachmentMarkupTests.ACommandToADetachedElementThrows`, `ACommandFollowsTheTargetsCurrentAspectAndThrowsWhenItHasNoSuchSound` (nou: același nume în Aspect-ul nou merge, fără el aruncă) |
| comenzi fără redare = no-op | `TimbreMarkupTransportTests.TransportOnASoundThatIsNotPlayingIsANoOp`, `TransportOnACompletedSoundIsANoOp`, `TimbreMotionMarkupTests.TimbreMotionOnASoundThatIsNotPlayingIsANoOp` |
| resursa rezolvată la aplicare | `TimbreAttachmentMarkupTests.ReplacingTheTimbreClipResourceReachesOnlyTheNextApplication` |
| argumentele ajung doar la sunetele care folosesc parametrul | `TimbreMarkupTransportTests.AttachmentArgumentsReachOnlyTheSoundsThatUseTheParameter` (nou) |
| Motion audio: redarea curentă capturată, pause/seek, înlocuirea încheie animația | `TimbreMotion*Tests` migrate |

Testele vechi migrate la sintaxa nouă: `AspectRuntimeProgramTests`, `TimbreAspectIntegrationTests`, `TimbreAspectLifecycleTests`, `TimbreMarkupTransportTests`, `TimbreMarkupParityTests`, `TimbreMotionMarkupTests`, `TimbreMotionLifecycleTests`, `TimbreMotionTestKit`. Unde vechiul test număra suprapuneri sau folosea handle-uri/argumente per pornire, așteptarea nouă e restart pe același sunet (decizia 8) și argumentele pe `@timbre`.

`dotnet test tests/Cerneala.Tests.Timbre -c Release`: 391 passed (`timbre.trx`), inclusiv `TimbreArchitectureTests` și `ExternalConsumerTests`.
