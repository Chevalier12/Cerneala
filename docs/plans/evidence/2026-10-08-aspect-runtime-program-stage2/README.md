# Etapa 2 — tot programul Aspect în `behaviorFactory`

Plan: [2026-10-08-aspect-runtime-program.md](../../2026-10-08-aspect-runtime-program.md), Etapa 2.

## Ce s-a schimbat

### SourceGen
- `UiMarkupAspectEmitter.PrepareAspectBehavior` emite **tot** programul (valori reactive, `@on`, Motion, `@presence`, `@layout`, `@scroll`/`@drag`/press, acțiuni Timbre) pentru orice Aspect: inline, resursă, package. `SupportsPackageBehavior`, `HasMotionBehavior` și parametrul `includeMotion` au dispărut.
- `ApplyAspects` doar atribuie Aspect-ul (`Aspect = …` / `AttachResource`); emisia per element (`bindToElementAspect`) a dispărut din `EmitMotionActivations` și `EmitTimbreActivations`.
- `DeclareAspectBehavior`: delegatul behavior-ului e declarat înaintea Aspect-ului și primește corpul în post-lines — un `$Name` declarat după resursă (`ForwardNamedMotion` → `$Child`) e în scope.
- Sesiunile Motion/Timbre sunt lifetime-uri ale behavior-ului (`CombineLifetimes`); `RegisterLifetime` pe contextul de template nu mai e emis.
- Cache-ul `specializedMotionSpecs` e per corp de behavior, cu nume unice în document (`nextMotionSpecId`) — o lambda nu mai referă o variabilă declarată în alt scope.
- Aspect inline: rezolvarea statică folosește elementul pe care e scris (al său `@prism`, `LayoutId`, descendenți); Aspect resursă: element sintetic de tipul țintă.
- `@presence`/`@layout`: `GeneratedMarkup.ApplyAspectValue` (sursa `AspectBase`, șters la dispose) fără protecția „before the element is attached” (decizia 1A).
- `$owner` dintr-un Aspect resursă compilat în afara unui template: `GeneratedMarkup.GetTemplateOwner(target)`, tipat `Control` (decizia 2A).
- Rădăcina pereche (`UiMarkupUserControlGenerator`, `UiMarkupWindowGenerator`): `ExcludeAspectProgramActivations` exclude activările Motion și Timbre din planul de valori al rădăcinii (sunt ale behavior-ului).
- `UiMarkupAspectSites.cs` (nou): `FindStaticApplicationSites` + deducerea pentru `$owner.parts.$X`, `$owner.prism`, `$self.prism` într-un Aspect resursă (decizia din 2026-10-08): tipul vine din locurile din markup, care trebuie să existe și să se potrivească; altfel eroare de compilare. Rădăcina reală a template-ului e păstrată pe fiecare loc (lanțul de strămoși al unui template continuă prin `<Aspect>` până în `Resources`).
- Prism: `TryResolvePrismMotionTarget`/`TryResolvePrismMotionOwner` primesc Aspect-ul; pentru un Aspect resursă, compoziția vine din locurile din markup, iar codul trece prin `GeneratedMarkup.RequirePrismClip`.

### Runtime
- `UI/Controls/Templates/TemplateOwnership.cs` (nou, intern): legătura element → owner, înregistrată în `ComponentTemplateInstance.Attach` înainte ca root-ul să intre în arbore și ștearsă în `Detach`; elementele template-urilor imbricate își păstrează propriul owner.
- `GeneratedMarkup` (public, nou): `ApplyAspectValue<T>`, `GetTemplateOwner`, `GetTemplatePart<T>`, `RequirePrismClip`.
- `MarkupConditionController`: creat pe un element atașat (de behavior), intră pe calea `Attach()` cu activarea inițială amânată — înainte pornea `Start()` direct și activa sunetul sincron, apoi încă o dată la attach (dovedit cu stack trace: `ElementAspect.AttachBehavior` → `AttachConditions` → ctor → `Start` → `ApplyActivations`, apoi `CompleteInitialActivation`).
- `UIElement.AttachToRoot`: atașează doar lifecycle behaviors existente înainte de behavior-ul Aspect-ului (cele adăugate de behavior se atașează singure).
- `AspectProcessor.AttachBehaviors` + apelul din `AttachToRoot`: behavior-urile din package (Aspect-uri default) se atașează odată cu elementul, înainte de `Loaded`; trecerea Aspect din frame le reutilizează. Fără asta, `@on Loaded` dintr-un Aspect default rata evenimentul (regresie prinsă de testul Drag și acoperită de `DefaultAspectOnLoadedRunsWhenTheElementAttaches`).

## Teste

- `tests/Cerneala.Tests.Timbre/Markup/AspectRuntimeProgramTests.cs`: RED-urile Etapei 0 sunt GREEN (swap A→B, A→B→A→B, aceeași resursă pe două elemente, Aspect default pe element creat la runtime, Aspect inline copiat cu `$Speaker`); nou `DefaultAspectOnLoadedRunsWhenTheElementAttaches`.
- `tests/Cerneala.Tests/Controls/TemplateOwnershipTests.cs` (nou): owner, owner după înlocuirea template-ului, template-uri imbricate, gărzile `GetTemplatePart`/`GetTemplateOwner`.
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorMotionTests.cs`: `ResourceAspectOwnerPartsMustAgreeAcrossApplicationSites` (tipuri diferite, parte lipsă, Aspect nefolosit — erori de compilare).
- Așteptări actualizate la contractul nou (nu la comportament greșit): `MotionMarkupCompilesOneBehaviorThatEveryNamedAspectApplicationRuns` (o sesiune per behavior, nu per element în text), `MotionClipCreatesIndependentFactoriesForEveryRunAndAspectInstance` (2 factory-uri în behavior), `AssertTimbreActionsBindCoreOperations` (fără `RegisterLifetime`), testele Presence/Layout (`ApplyAspectValue`, `Presence` adus la attach, nu la construcție).

## Verificare

`dotnet build Cerneala.slnx -c Release -m:1` și `-c Debug`: 0 erori. `dotnet test Cerneala.slnx -c Release --no-build --no-restore -m:1`, TRX în `full/`:

| Proiect | Rezultat |
|---|---|
| Cerneala.Tests | 4201 passed, 2 skipped (preexistente) |
| Cerneala.Tests.SourceGen | 644 passed |
| Cerneala.Tests.Timbre | 376 passed |
| Cerneala.Tests.Language | 374 passed, 1 skipped (preexistent) |
| Cerneala.Tests.LanguageServer | 45 passed |
| Cerneala.Tests.PreviewHost | 22 passed |
| Cerneala.Tests.SdlGpu | 601 passed, 241 skipped (nativ dezactivat) |
| Cerneala.Tests.VisualStudio | 48 passed |
| Scene2DImporters / Scene2DPackages / SceneVillage / Tetris | 173 / 104 / 36 (+8 skip) / 31 passed |

Emisia per element pentru Motion/`@on`/Timbre nu mai există: `ApplyAspects` conține doar atribuirea, iar `MotionMarkupCompilesOneBehaviorThatEveryNamedAspectApplicationRuns` verifică textul generat (o singură `AttachMotionSession(target)`, nicio `.Aspect!);`).

Sondele temporare (`ZzDumpGeneratedProbe`, `ZzDoubleActivationProbeTests`) au fost șterse.
