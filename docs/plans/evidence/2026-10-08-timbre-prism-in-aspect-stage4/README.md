# Etapa 4 — Language

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 4.

## Schimbări

- `Cerneala.Language/Timbre/TimbreMarkupSyntax.cs`: corpul unui `<TimbreClip>` (și al unui `@timbre { … }`) are la nivel de clip `@parameter` și noduri `@sound Nume { … }` (Source, Volume, Loop, AutoPlay, `@modifier`); o proprietate sau un `@modifier` scris direct în clip e forma veche (`LegacySpan`). Comenzile `@play/@stop/@pause/@resume/@seek` acceptă doar `$self|$owner|$Name.timbre.Sunet` (`@seek … to <durată>`). `@timbre $Clip(Parametru = valoare)` are parserul lui.
- `Cerneala.Language/Timbre/TimbreMarkupBinder.cs`: modelul legat nou — `BoundTimbreClip` (parametri + sunete), `BoundTimbreSound` (parametrii folosiți de modificatorii lui), `BoundTimbreAttachment`, `BoundTimbreCommand` (`Self`/`Owner`/`Named`), `BoundTimbreAspect`. Dispar handle-urile (`TimbreHandleKind`, `BoundTimbreAction`, schema pe handle).
- `CernealaSemanticModel.AspectAttachments.cs` (nou): găsește `@timbre`/`@prism` în corpul fiecărui Aspect, raportează contextul (în `@on/@when/@if`, de două ori, fără `;`), le leagă o singură dată și le golește din textul citit de restul binder-elor (setter-ele Aspect, Motion, condiții), ca `Source = …` sau `Radius = …` din ele să nu fie luate drept setter-e. `@prism` în conținutul unui element → `PRISM2013 @prism is written only in an Aspect body.`
- Prism: fiecare element primește în `prismApplications` Prism-ul Aspect-ului lui static (inline, `Aspect="$X"` sau implicit), deci `$self.prism.…`/`$Name.prism.…` se rezolvă ca înainte.
- Ținte: `$self` = `@timbre`-ul Aspect-ului; `$Name` = elementul din namescope-ul unde e scris Aspect-ul, verificat în Aspect-ul lui static (`'$Speaker' has no sound 'Lipsa' in its Aspect.`); în `App.crn` doar `$self`/`$owner`; `$owner` se verifică la runtime.
- Motion `$X.timbre.Sunet.Volume|Parametru`: parametrul trebuie folosit de sunet (`Sound 'Click' does not use parameter 'Brightness'.`).
- Simboluri: `TimbreSound` (declarație pe `@sound`, referințe din comenzi și Motion, navigare și token semantic), `MotionTarget` pe `$Name`, `TimbreDirective` pe `@timbre/@sound/@play…`.
- Completion (colateral necesar testelor Language existente; restul tooling-ului rămâne în Etapa 7): `@timbre`/`@prism` la începutul Aspect-ului, comenzile în `@on/@when/@if`, `$self.timbre.Sunet`, corpul `@sound`, parametrii unui `@timbre $Clip(…)`.

## Teste

- Corpus înghețat în Etapa 0 (`timbre-aspect-corpus.json`, 26 de cazuri) — GREEN, inclusiv cazurile valide compilate de generator (Etapele 5 și 6 au fost scrise înainte de acest gate; vezi decizia 10 din `docs/plans/2026-10-08-decizii-pentru-review.md`).
- Corpusurile existente migrate la sintaxa nouă: `timbre-corpus.json` (61 de cazuri; dispar cele cu handle-uri și cu argumente per redare, apar `@sound`, `AutoPlay`, ținte cross-element, forma veche ca eroare) și `timbre-motion-corpus.json` (21 de cazuri, inclusiv `$Speaker.timbre.Music.Volume` și AutoPlay fără `@play`).
- `TimbreSemanticTests`, `TimbreMotionSemanticTests`, `TimbreToolingTests` rescrise pe modelul nou; `MotionPrismSemanticTests` și `constructs.json` cu `@prism` mutat în Aspect; `sourcegen-diagnostics.json` regenerat (doar pozițiile s-au mutat; aceleași diagnostice).
- `dotnet test tests/Cerneala.Tests.Language -c Release`: 408 passed, 1 skipped (preexistent) — `language.trx`.
