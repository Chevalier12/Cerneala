# Stage 1 — baseline-ul binar/API și caracterizarea înainte de RED

> Data: 2026-09-24, Windows / PowerShell, `C:\Users\lauri\Desktop\Cerneala`.
> HEAD la captură: `443f100b86f8c4ca5f5e649651ac37c9cd5a330f` (`master`).
> Contractul selectat: [decizia Stage 0](../../2026-09-24-scene2d-stage0-contract-proposal.md). Acesta este un baseline al **worktree-ului curent**, care include modificarea deja existentă a utilizatorului `TileMap2D.FromModel`, nu o reconstrucție din HEAD curat.
> Faza: capturat **înainte** de noile teste RED; producția/API docs nu au fost editate în Stage 1 la această captură.

## 1. Compilația inițială și corecția non-distructivă a inputului

Comanda inițială, fără schimbări de proiect sau ștergeri, a eșuat:

```powershell
dotnet build .\Cerneala.Scene2D.Packages\Cerneala.Scene2D.Packages.csproj -c Release -m:1 --nologo -v:minimal
```

[Output integral](baseline-build-package-release.log): exit 1, 52 erori de compilare. Core a primit fișiere `.cs` generate/copiate sub `artifacts/rendersurface3d/control-stage4/.../obj/...`, producând duplicate assembly attributes și `Xunit` lipsă. Nu este un RED de contract. `artifacts/` este ignorat de `.gitignore`, iar fișierele observate acolo nu sunt tracked; originea lor umană nu poate fi atribuită din Git. `Cerneala.csproj` folosește globul SDK implicit și excluderi de directoare țintite care nu includ `artifacts/`. SDK 10.0.400 adaugă `DefaultItemExcludesInProjectFolder` la excluderile globului implicit; această proprietate nu modifică fișierele de pe disc.

Pentru dovada exactă a inputului, s-au evaluat itemii Core prin:

```powershell
dotnet msbuild .\Cerneala.csproj -getItem:Compile
dotnet msbuild .\Cerneala.csproj '-p:DefaultItemExcludesInProjectFolder=artifacts/**' -getItem:Compile
dotnet msbuild .\Cerneala.csproj '-p:DefaultItemExcludesInProjectFolder=artifacts/**' -getItem:AdditionalFiles
```

Output-urile brute sunt [Compile implicit](compile-items-default.json), [Compile ajustat](compile-items-artifact-excluded.json) și [AdditionalFiles ajustat](additional-files-artifact-excluded.json). Comparația identităților: 1111 itemi Compile implicit, 1094 ajustați, **exact 17 eliminați și 0 adăugați**; toate cele 17 identități eliminate sunt sub `artifacts/`, [lista completă](compile-items-removed.txt), 0 în afara lui. `AdditionalFiles` ajustat are 1 item, 0 sub `artifacts/`. Evaluarea nu dovedește conținutul unei surse prin ea însăși, dar arată că acest retry nu exclude un item Compile din sursa intenționată. Nu s-au șters/mutat artefacte și nu s-a editat `.csproj`.

Comanda corectată, care trebuie păstrată pentru build-urile dependente de acest baseline:

```powershell
dotnet build .\Cerneala.Scene2D.Packages\Cerneala.Scene2D.Packages.csproj -c Release -m:1 --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**'
```

[Output integral](baseline-build-package-release-artifact-excluded.log): exit 0, 0 warnings, 0 errors. Pachetul construiește transitiv Core/SourceGen/Language. Nu pretindem că succesul compilației dovedește noul contract.

## 2. Binarul înghețat pentru ApiCompat

Copiile au fost făcute imediat după build-ul Release corectat, înaintea testelor RED, în [binary-baseline](binary-baseline/). Nu au fost suprascrise după aceea.

| Assembly | Dimensiune | SHA-256 înghețat | Sursa Release |
| --- | ---: | --- | --- |
| `Cerneala.dll` | 5,483,008 bytes | `F5F16215D563E712337DE83D4ACF1FC8CE1D2A0E5F2BB94E94EDE6B78BB6EA83` | `bin/Release/net8.0/Cerneala.dll` |
| `Cerneala.Scene2D.Packages.dll` | 78,336 bytes | `7D4B36BD358FEB21889BB4828DDE7B27BF72ECD153B0549B5C2B1D507DD97F2F` | `Cerneala.Scene2D.Packages/bin/Release/net8.0/Cerneala.Scene2D.Packages.dll` |

Hash-urile output-urilor Release au fost recitite după caracterizarea de mai jos și erau **identice** cu copiile înghețate. La verificarea unei suspiciuni de citire tranzitorie, `UI/Controls/SceneItems2D.cs` avea același Git blob în worktree și HEAD (`f779503ba379c5206fa212738c3416b71d4c5dcb`), fără diff; SHA-256 worktree `E5F493D67154C8B4E2E7A532696C80E9B4E96610B6FB704A8580265E925F3575`. Nu există dovadă curentă că fișierul de producție s-a modificat în timpul caracterizării.

## 3. Caracterizare existentă, secvențială

Toate comenzile de mai jos au fost rulate **una după terminarea celeilalte**, în Release, cu `-m:1` și aceeași excludere de input; fișierele `.log` sunt output-uri brute terminale, iar `.trx` păstrează rezultatele pe caz. Filtrul Core este exact:

```text
FullyQualifiedName~SceneItems2D|FullyQualifiedName~SceneSpatial|FullyQualifiedName~TileMap|FullyQualifiedName~SceneCollision|FullyQualifiedName~ScenePresentation|FullyQualifiedName~SceneSimulationContext|FullyQualifiedName~ScenePrism
```

| Comandă `dotnet test` (toate cu `-c Release -m:1 --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**'`, `--logger 'trx;LogFileName=<TRX>'`, `--results-directory docs/plans/evidence/2026-09-23-scene2d-stage1/test-results`) | Log / TRX | Exit | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: | ---: |
| `.\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter <Core filter de mai sus>` | [log](core-characterization.log) / [TRX](test-results/core-characterization.trx) | 0 | 410 | 0 | 0 |
| `.\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj` | [log](sourcegen-characterization.log) / [TRX](test-results/sourcegen-characterization.trx) | 0 | 610 | 0 | 0 |
| `.\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj` | [log](packages-characterization.log) / [TRX](test-results/packages-characterization.trx) | 0 | 81 | 0 | 0 |
| `.\tests\Cerneala.Tests.Scene2DImporters\Cerneala.Tests.Scene2DImporters.csproj` | [log](importers-characterization.log) / [TRX](test-results/importers-characterization.trx) | 0 | 173 | 0 | 0 |
| `.\Tetrisish\Tests\Tetris.Tests.csproj` | [log](tetris-characterization.log) / [TRX](test-results/tetris-characterization.trx) | 0 | 30 | 0 | 0 |

Fiecare comandă a inclus efectiv argumentele comune din antetul tabelului; logul arată proiectul/assembly-ul construit și totalul executat. Filtrul Core caracterizează numele de teste relevante, nu întreaga suită Core. `SourceGen`, package, importers și Tetris au fost rulate ca proiecte întregi. Total local pentru aceste cinci invocări: **1304 passed, 0 failed, 0 skipped**. Nu s-au șters sau slăbit teste existente pentru acest rezultat.

Caracterizare **numai a skip-urilor** native, cu `CERNEALA_SDL_NATIVE_TESTS` neactivat:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release -m:1 --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~NativePackageWarmStreamingTests|FullyQualifiedName~NativePackageGridSubdivisionTests|FullyQualifiedName~NativeScenePrismStreamingTests' --logger 'trx;LogFileName=sdl-native-no-optin-characterization.trx' --results-directory docs/plans/evidence/2026-09-23-scene2d-stage1/test-results
```

[Log](sdl-native-no-optin-characterization.log) / [TRX](test-results/sdl-native-no-optin-characterization.trx): exit 0, **0 passed, 0 failed, 6 skipped**. Cele șase cazuri au mesajul TRX `Set CERNEALA_SDL_NATIVE_TESTS=1 on a configured native matrix runner.` Acest rezultat **nu** închide conformance-ul nativ cerut ulterior. Nu a fost rulat testul SDL cu opt-in.

## 4. ApiCompat strict: proiect dedicat și sanity self-compare

[api-compat.proj](api-compat.proj) este un proiect MSBuild nou, separat de artefactele istorice. Folosește căi derivate din `MSBuildThisFileDirectory`/`MSBuildSDKsPath`, nu căi absolute ale unui alt host; `ValidateAssembliesTask` este încărcat din SDK-ul selectat de `global.json` (`10.0.400`). Parametrii `LeftAssemblies`, `RightAssemblies`, `RoslynAssembliesPath`, `EnableStrictMode`, `EnableRuleCannotChangeParameterName`, `GenerateSuppressionFile` și `RespectInternals` au fost verificați pe task-ul instalat; proiectul compară Core cu Core și Packages cu Packages, fără suppressions moștenite sau generare de suppressions. Strict mode și regula de nume al parametrilor sunt activate, `RespectInternals=false` limitează la contractul public.

Comanda sanity, rulată cât candidații Release erau identici cu baseline-ul înghețat:

```powershell
dotnet msbuild .\docs\plans\evidence\2026-09-23-scene2d-stage1\api-compat.proj -t:Compare -v:minimal
```

[Output brut](api-compat-baseline-selfcompare.log): exit 0. Aceasta verifică invocarea proiectului în starea identică, **nu** clasifică rupturile viitoare și nu este API gate final. Artefactele vechi cu căi absolute/suppressions nu sunt importate.

## 5. Limită

Acest baseline dovedește GREEN doar pentru comenzile de caracterizare enumerate, pe inputul Release și worktree-ul de la captură. Nu dovedește noul contract `IEnumerable`, nu execută noile RED-uri, nu este full suite, nu este comparație API finală și nu acoperă WindowsDX/Linux/macOS. Probe RED și gate-urile etapei 1 sunt încă în lucru.
