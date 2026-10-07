# Timbre markup/Aspect — etapa 3: tooling și preview

Snapshot: `master` @ `a838fc35` + modificările necomise ale planului. Host: Windows 11 Pro 10.0.26200 x64.

## Implementare

- **Language (completion/signature/navigation/structure)** — servite din același `SoundMarkupModel` pe care îl consumă
  generatorul: corpul `SoundClip` (Source/Volume/Loop, `@parameter`/`@modifier`, modificatorii și intrările lor din
  `TimbreCatalog`, parametrii declarați, `true`/`false`, `float`), acțiunile (`@sound`/`@pause`/`@resume`/`@seek` numai
  în `@on`/`@when`/`@if`), clipurile `$Clip`, argumentele `$Clip(...)` (Volume, Loop + parametrii clipului), handle-urile
  (`as`, `@pause`, `@resume`, `@seek`, inclusiv cu instrucțiunea curentă incompletă), `to` și unitățile seek
  (`0s`/`30s`/`500ms`). Signature help pentru `$Clip(`, go-to-definition pentru clip/handle/parametru, semantic tokens
  (Keyword/Function/Parameter/Property/Label). Fix preexistent: `IsInsideDirective` număra acoladele după închiderea
  blocului.
- **LSP standalone** (`DiagnosticService.CreateStandaloneEmbeddedDiagnostics`): `SoundClip` e legat complet prin
  `SoundMarkupBinder.BindClip` (depinde numai de catalogul comun), iar property-element-urile `*.Aspect` primesc aceleași
  diagnostice sintactice ca `<Aspect>` (semicolon-uri audio). Diagnosticele audio sunt identice standalone vs. proiect.
- **VS grammar** (`cerneala.tmLanguage.json`): `sound-tags` (`meta.element.sound`), `sound-directives`
  (`keyword.control.sound` pentru `@sound/@pause/@resume/@seek/@modifier`, handle `entity.name.label.handle`, modificator
  `support.function.sound`, `to`). Pachetul VSIX conține exact gramatica sursă (test nou de egalitate).
- **PreviewHost**:
  - audio dezactivat implicit: fiecare sesiune primește propriul `SoundRuntime` (BaseDirectory = directorul runtime al
    compilării) cu `DisabledPreviewSoundOutput`, care refuză și numără deschiderile; redările eșuează explicit
    (`DeviceUnavailable`, „Live Preview audio is disabled…”), nu succes tăcut;
  - enable explicit: `PreviewRequest.AudioEnabled` (Render) → runtime pe output-ul platformei
    (`Application.PlatformSoundOutput`, internal nou); schimbarea flag-ului recreează sesiunea;
  - stare vizibilă: frame-ul poartă `AudioEnabled` + `BlockedAudioRequests`; VS afișează „audio off (N blocked)” /
    „audio on” în bara de mod și un buton „Audio off/on” (nicio pornire automată);
  - hot reload: sesiunea veche (și toate scope-urile ei audio) e retrasă înaintea celei noi; nimic nu e restaurat, iar
    regulile reactive ale noii sesiuni se aplică o singură dată. Bug preexistent reparat: serverul crea sesiunea nouă
    înaintea eliberării celei vechi, iar `Application.Install` permite o singură aplicație — orice recompilare cu o sesiune
    activă eșua;
  - fără succes fals la compilare: generatorul omite documentele cu erori, iar un partial companion păstra tipul, deci
    preview-ul ar fi randat un control gol. `PreviewCompiler` cere acum o declarație generată pentru tipul țintă (numai
    când proiectul rulează `Cerneala.SourceGen`) și raportează diagnosticele generatorului (ex. `CERNEALAUI032`).
  - editările corpurilor de directive (`SoundClip`, `*.Aspect`) și ale `Name` cer recompilare; editările de atribute
    rămân pe calea rapidă.
- **SourceGen** — bug găsit prin preview: rădăcinile paired (UserControl/Window) re-colectau condițiile Aspect-ului
  rădăcinii într-un plan propriu, iar activarea audio era emisă de două ori (2 redări pentru un `@when` initial-true).
  `ExcludeSoundActivations` lasă audio numai în planul emis de `ApplyAspects`. RED dovedit pe sursa generată (două
  `AttachConditions` cu aceeași acțiune audio) și pe runtime (`PlaybacksStarted = 2`); regresie
  `PairedRootAspectEmitsEachReactiveSoundActivationOnce` (UserControl, Window).
- Build: `Cerneala.csproj` exclude `.claude\**` din glob-ul explicit `AdditionalFiles **\*.crn` (worktree-urile altor
  sesiuni din `.claude/worktrees` intrau în compilarea bibliotecii; glob-urile implicite SDK exclud deja folderele cu
  punct).

## Dovezi (Release)

| Suită | Conținut nou relevant | Rezultat |
| --- | --- | --- |
| `Cerneala.Tests.Language` | `SoundToolingTests` (completion corp clip/acțiuni/argumente/handles/seek, signature help, navigare, tokens), corpusul audio 56 cazuri Language↔SourceGen | 340 passed, 1 skipped (preexistent) |
| `Cerneala.Tests.SourceGen` | `PairedRootAspectEmitsEachReactiveSoundActivationOnce` (UserControl, Window) + fixtures Sound existente | 634 passed |
| `Cerneala.Tests.LanguageServer` | `SoundDiagnosticsAreTheSameForStandaloneAndProjectDocuments` (030/031/032 identice standalone vs. proiect), `SoundDiagnosticsFollowTheUnsavedOverlayAndThenTheSavedAdditionalFile` (overlay nesalvat → close+reload revine la fișierul salvat → fișier salvat invalid după reload) | 45 passed |
| `Cerneala.Tests.VisualStudio` | corpus golden + linii audio (29–41), `SoundDirectivesKeepTheirScopesWithoutSemicolonsAndDoNotLeakIntoTheFollowingTag`, gramatica din VSIX == sursa | 48 passed |
| `Cerneala.Tests.PreviewHost` | fast path vs. recompilare pentru corpuri audio/`Name`, protocol audio, compilare unsaved validă + `CERNEALAUI032` fără succes fals, audio dezactivat implicit (exact o deschidere blocată per sesiune), retragerea runtime-ului vechi și o singură activare nouă după recompilare, host: dezactivat implicit → enable explicit recreează sesiunea | 22 passed |
| `Cerneala.Tests.Timbre` | regresie runtime/paritate după schimbările SourceGen/Application | 336 passed |

Consumeri nativi desktop (Release, generator real): `CernealaPresentation` și `Playground/Cerneala.Playground` — build
reușit, 0 erori, 0 warnings. După editările .crn/AdditionalFiles/proiect workspace-ul semantic a fost reîncărcat
(`ReloadAsync` în testele LSP; analizorul design-time `Debug` al `Cerneala.SourceGen` reconstruit înaintea testelor
PreviewHost, care încarcă proiectul prin `MSBuildWorkspace` cu `Configuration=Debug`).

`git diff --check` curat pe fișierele etapei.
