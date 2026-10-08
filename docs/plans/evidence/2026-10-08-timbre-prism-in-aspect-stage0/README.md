# Etapa 0 — baseline, inventar, contract C#, RED

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 0.

## Precondiție

Planul-fundație [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md) are `Status: finalizat`, cu gate-ul final verificat în `../2026-10-08-aspect-runtime-program-stage4/README.md`.

## Baseline GREEN

Starea de cod de la începutul acestei etape este identică cu cea a verificării finale a fundației: build Release 0 erori și suita completă GREEN cu `CERNEALA_SDL_NATIVE_TESTS=1`, `CERNEALA_TIMBRE_AUDIO_DEVICE=1` (`../2026-10-08-aspect-runtime-program-stage4/full/`, TRX pentru fiecare proiect) și smoke-urile `timbre`, `timbre-markup`, `timbre-motion` exit 0. Etapa 0 adaugă doar teste RED.

## Contract C# fixat

### Core (`Timbre/`, namespace `Cerneala.Timbre`, fără dependență de UI)

```csharp
public sealed class TimbreClipSound
{
    public TimbreClipSound(string name, TimbreSound sound, bool autoPlay = false);
    public string Name { get; }          // identificator; unic în clip
    public TimbreSound Sound { get; }
    public bool AutoPlay { get; }        // pornește la fiecare aplicare a Aspect-ului
}

public sealed class TimbreClipDefinition
{
    public TimbreClipDefinition(
        string name,
        IEnumerable<TimbreClipSound> sounds,
        IEnumerable<TimbreParameter>? parameters = null);
    public string Name { get; }
    public IReadOnlyDictionary<string, TimbreClipSound> Sounds { get; }   // enumerat în ordinea declarării
    public IReadOnlyList<TimbreParameter> Parameters { get; }
}
```

Validare (`ArgumentException`/`ArgumentNullException`): nume nevid; cel puțin un sunet (simetric cu `PrismClipDefinition`, care cere cel puțin un nod); nume de sunete identificatori și unice; parametri ne-nuli, cu nume unice; fiecare parametru declarat de un `TimbreSound` din clip trebuie să fie (prin referință) unul dintre `Parameters`. `TimbreSound` (fostul `TimbreClip`) rămâne neschimbat. Folosire C#: `element.Timbre.Play(ui.Sounds["Hover"].Sound)`.

### `GeneratedMarkup` (`UI/Markup/GeneratedMarkupTimbre.cs`)

Rămân: `AttachTimbreSession(UIElement)` (deține handler-ele `@on` cu comenzi Timbre și execuțiile Motion audio ale unei aplicări; ignoră ascunderea), `AddTimbreTrigger`, `StartTimbreMotion`, `GetTimbreParameter`.

Noi:

```csharp
public static IDisposable AttachTimbre(UIElement target, ResourceId<TimbreClipDefinition> clip, IReadOnlyDictionary<string, float>? arguments);
public static IDisposable AttachTimbre(UIElement target, TimbreClipDefinition clip, IReadOnlyDictionary<string, float>? arguments);
public static TimbrePlayback PlayTimbre(UIElement target, string sound);
public static void StopTimbre(UIElement target, string sound);
public static void PauseTimbre(UIElement target, string sound);
public static void ResumeTimbre(UIElement target, string sound);
public static Task SeekTimbre(UIElement target, string sound, TimeSpan position);
public static MotionHandle StartTimbreMotionProperty(
    UIElement target, string sound, string parameterName,
    bool hasFrom, float from, bool toCurrent, float to,
    MotionSpec<float>? spec, MotionPropertyStartOptions options);
```

Eliminate (înlocuite de cele de mai sus): `PlayTimbre(IDisposable, ResourceId<TimbreSound>, Action<TimbreSound, TimbreStartOptions>?, string?)`, `CancelTimbre(IDisposable, string)`, `PauseTimbre(IDisposable, string)`, `ResumeTimbre(IDisposable, string)`, `SeekTimbre(IDisposable, string, TimeSpan)`, `StartTimbreMotionProperty(IDisposable, string, string, …)`.

Comportament: `AttachTimbre` rezolvă resursa la apel (la aplicarea Aspect-ului), creează câte un `TimbreHandle` per sunet într-un scope al elementului, pornește sunetele cu `AutoPlay` și înregistrează atașarea în registrul per element (`ConditionalWeakTable<UIElement, …>`); `Dispose` oprește tot și scoate înregistrarea. Comenzile caută în registru: element neatașat, fără atașare sau fără acel sunet → `InvalidOperationException`; fără redare activă → no-op (în afară de `PlayTimbre`, care pornește/repornește). Argumentele `@timbre $Clip(Nume = valoare)` se aplică la pornirea fiecărui sunet care folosește parametrul.

### Diff ApiCompat așteptat față de `59c0e257`

Pe lângă diff-ul deja clasificat (fundația și Etapele 1–2, vezi `../2026-10-08-aspect-runtime-program-stage3/api-compat-strict.log`):

- tipuri adăugate: `Cerneala.Timbre.TimbreClipDefinition`, `Cerneala.Timbre.TimbreClipSound`;
- membri adăugați în `GeneratedMarkup`: cele două `AttachTimbre`, `PlayTimbre(UIElement, string)`, `StopTimbre`, `PauseTimbre(UIElement, string)`, `ResumeTimbre(UIElement, string)`, `SeekTimbre(UIElement, string, TimeSpan)`, `StartTimbreMotionProperty(UIElement, string, string, …)`;
- membri eliminați din `GeneratedMarkup`: cele șase overload-uri pe sesiune + nume de handle listate mai sus.

## Inventar și migrare

| Ce | Unde (număr) | Migrare |
|---|---|---|
| `@prism` în conținutul elementului, `.crn` | 44 în `Playground/**` (InvestorReel, Playground, SolarSystem), `Tetrisish/MainWindow.crn`, `tests/Fixtures/VisualStudioConsumer/*.crn` | mutat în `<X.Aspect>` (existent sau nou). Două elemente au deja `Aspect="$Resursă"`: `CyberReel.crn` `Grid Aspect="$Director"` (resursa e folosită o dată → `@prism` intră în `Director`) și `VisualStudioConsumer/MainView.crn:57` `Border Aspect="$CardAspect"` (resursa e folosită de două elemente, doar unul are `@prism` → resursă nouă cu același corp plus `@prism`). |
| `@prism` în conținut, teste C# și corpus | `PrismMarkupContractTests` (34), `MotionPrismSemanticTests` (6), `constructs.json` (5), teste RenderSurface2D/3D, Scene*, Sprite*, TileMap*, `CompletionTests`, `EmbeddedSyntaxTests`, `Stage4IntegrationHarnessTests`, golden VS | markup mutat în Aspect; testele care verifică respingerea rămân negative |
| `@prism` în docs | `CernealaMarkupGuide.md` (5), `prism-guide.md`, docs-site `RenderSurface2D`, `Scene2DDebugOverlay`, `Sprite2D` | sintaxa nouă; `prism-markup-syntax-proposal.md`, `prism-technical-design.md` și rezultatele benchmark rămân documente istorice |
| `<TimbreClip>` cu un singur sunet | corpus `timbre-corpus.json` (49) și `timbre-motion-corpus.json` (21), teste Language (13), LanguageServer (5), PreviewHost (2), SourceGen (10), Timbre (17), smoke (12), golden VS, ghiduri (7) | devine `<TimbreClip>` cu `@sound`; cazurile de corpus pentru concepte scoase (handle, argumente per `@timbre`, `Volume`/`Loop` per redare) devin negative sau se șterg |
| `@timbre $X` ca acțiune, `@handle`/`as H`/`@cancel H` pe sunet | aceleași fișiere; `@cancel` pe sunet în 10 fișiere; `AspectRuntimeProgramTests` (fundația) | `@timbre` la începutul Aspect-ului + `@play/@stop/@pause/@resume/@seek $self.timbre.Sunet` |
| Motion `$self.timbre.Handle.Param` | `timbre-motion-corpus.json`, `TimbreMotion*Tests`, `TimbreMotionPanel.crn` | `$self.timbre.Sunet.Param` |

Schimbare de comportament de reținut la migrare: azi `@on Click { @timbre $Wav; }` fără handle pornește o redare nouă la fiecare click (se suprapun); acum `@play $self.timbre.Wav` repornește același sunet (overlap pe același `@sound` e non-obiectiv). `Volume = 0.2` per redare devine `Volume` pe `@sound`.

## Corpus înghețat

`tests/Cerneala.Tests.Language/Corpus/timbre-aspect-corpus.json` (26 de cazuri, rulat de `TimbreAspectSemanticTests`): pozitive — contractul §4 integral, transport inline (`@pause/@resume/@seek/@stop/@play`), resursă fără argumente pe două butoane, Motion pe parametru, comenzi în `@when/@if`, Aspect din `App.crn` cu `$self`; negative — `@timbre`/`@prism` în conținut, `@timbre`/`@prism` în `@on`, două `@timbre`, două `@prism`, `@sound` duplicat, `@parameter` în `@sound`, `TimbreClip` fără `@sound`, `TimbreClip` vechi cu `Source` direct, argument care nu e `@parameter`, `@play Click;`, `@play $Click;`, `@pause $Speaker;`, `$Speaker.timbre.Lipsa`, `$self.timbre.Lipsa`, `$Name` în `App.crn`, `@play` în `@parallel`, Motion pe parametru nefolosit de sunet, forma veche cu handle. Mesajele sunt fixate în fișier.

## RED

| Fișier | Teste | Eșuează azi pentru că |
|---|---|---|
| `tests/Cerneala.Tests.Language/TimbreAspectSemanticTests.cs` (`red-language.trx`) | 26 din 27 (cele 26 de cazuri; doar testul de acoperire a familiilor trece) | Language respinge sintaxa nouă (`@sound is allowed only inside an Aspect @on, @when or @if body`, `TimbreClip requires Source`, `Unknown Motion directive '@prism'`/`'@play'`) și nu are mesajele noi |
| `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTimbreAttachmentTests.cs` (`red-sourcegen.trx`) | 2/2 | generatorul raportează aceleași erori; nu emite `AttachTimbre`/`TimbreClipDefinition` |
| `tests/Cerneala.Tests.Timbre/Markup/TimbreAttachmentMarkupTests.cs` (`red-timbre.trx`) | 5/5: AutoPlay la aplicare, `@play` repornește, swap A→B (sunetele și Prism-ul lui A dispar, B aplică, înapoi la A AutoPlay repornește), comandă spre element detașat aruncă, înlocuirea resursei `<TimbreClip>` ajunge doar la aplicarea următoare | markup-ul nu compilează (aceleași diagnostice) |

Testele runtime folosesc doar markup (nu tipurile C# noi), ca proiectul de test să compileze până în Etapa 3.
