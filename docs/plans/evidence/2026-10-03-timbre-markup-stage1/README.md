# Timbre markup/Aspect — etapa 1: SoundClip și binding/lowering comun

Snapshot: `master` @ `a838fc35` + modificările necomise ale planului. Host: Windows 11 Pro 10.0.26200 x64.

## Implementare

- Owner unic build-time: `Cerneala.Language/Timbre/SoundMarkupSyntax.cs` (parser al corpului SoundClip și al
  instrucțiunilor `@sound/@pause/@resume/@seek`) și `SoundMarkupBinder.cs` (`BoundSoundClip`, `BoundSoundAction`,
  `BoundSoundAspect`, `SoundMarkupModel`). Toate range-urile, default-urile și unitățile vin din
  `Timbre/Catalog/TimbreCatalog.cs`, același fișier compilat de core.
- `CernealaSemanticModel.Sound.cs`: `ResourceKind.SoundClip` (tip `Cerneala.Timbre.SoundClip`, fără `TargetType`,
  permis în `Application.Resources`), contextul acțiunilor, tiparea handle-urilor audio/Motion, scanarea directivelor
  audio din afara Aspect/SoundClip; codurile CERNEALAUI030–033 în catalogul comun; `DirectiveSyntaxParser` cunoaște
  keyword-urile audio și raportează `Sound directive '@x' must end with ';'.`
- SourceGen consumă `SourceGeneratorSemanticModel.Sound`: `ReadSoundClip` emite `SoundParameter<float>`, `SoundClip`,
  `LowPass`/`Delay` cu `SoundInput<float>` și `SoundSource.FromFile`, apoi `Resources.SetResource(ResourceId<SoundClip>)`
  în factory, paired partial și Application. Cursorul de directive doar delimitează instrucțiunile audio
  (`SoundActionNode`), fără validare; emisia acțiunilor aparține etapei 2.

## Dovezi

| Verificare | Rezultat |
| --- | --- |
| `SoundSemanticTests` (corpus 54 cazuri: diagnostice Language == SourceGen ca id/mesaj/span, compilare generată pentru valide; legare tipată, ordine, intersecție de range, override-uri fără mutarea clipului, shadowing) | 65 passed |
| `UiMarkupGeneratorTests.Sound*` (factory + paired: simboluri bind-uite pe constructorii/metodele core din asamblarea `Cerneala`, constante `volume/loop`, fără reflection/dynamic/SDL; execuția factory comparată structural cu definiții C# manuale; App + două documente care referă același clip; template/ContentTemplate și shadowing) | 5 passed |
| Cerneala.Tests.Language complet | 320 passed, 1 skipped preexistent |
| Cerneala.Tests.SourceGen complet | 630 passed |
| Cerneala.Tests.LanguageServer complet | 40 passed (golden-ul catalogului extins aditiv cu 030–033) |
| Cerneala.Tests.VisualStudio / PreviewHost | 47 / 17 passed |

Referirea clipurilor (inclusiv din Application, cu fișiere inexistente) atașată unui `UIRoot` cu `SoundRuntime` real:
`ISoundOutput.Open` apelat de 0 ori, 0 playback-uri active, 0 eșuate — declararea și referirea nu fac I/O sau redare.
