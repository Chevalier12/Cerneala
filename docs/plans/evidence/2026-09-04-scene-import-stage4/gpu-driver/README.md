# Investigatie GPU/driver — 2026-09-05

Extindere autorizata explicit de utilizator. Investigatie, nu fix: etapa 4 ramane deschisa. Nu s-a instalat un driver, nu s-a schimbat configuratia globala NVIDIA si nu s-a introdus un workaround in renderer.

## Configuratia observata si probele de profil

- NVIDIA GeForce RTX 2060, driver 591.59 / Windows 32.0.15.9159; MonoGame.Framework.WindowsDX 3.8.4.1.
- NVAPI DRS raporteaza in profilul global AA mode `1` (override), metoda `0`, gamma correction `2`. Aceasta observatie nu demonstreaza cauza defectului.
- S-au creat doua profile temporare unice numai pentru executabilul temporar al probei: application-controlled, apoi application-controlled cu gamma correction dezactivat. Ambele pastreaza esecurile originale: text 47/255, dreptunghi semitransparent 25/255. Salvarea si citirea inapoi a AA mode au reusit; aceasta nu este o masurare independenta a tuturor setarilor efective din driver.
- Fiecare profil creat a fost eliminat in `finally`. La final, toate cele sase interogari originale sunt identice cu cele initiale, iar cautarea profilului executabilului temporar intoarce `NVAPI_EXECUTABLE_NOT_FOUND` (-166). Sunt verificate setarile interogate, nu fiecare setare existenta in baza NVIDIA.

Dovezi: `profiles-before.json`, `profiles-final.json`, `aa-override-baseline.json`, `aa-application-controlled.json`, `aa-gamma-off.json`.

## Matricea minima MonoGame

Doua triunghiuri subtiri opace G, un dreptunghi semitransparent, apoi aceleasi triunghiuri opace M. Se compara cu dreptunghi/M, fara desenare Cerneala. Suprafata 180x90, rotatie 0.2, scala 1.5, translatie (20,25). Se compara canalul G pentru aceasta proba minima; regresia permanenta compara toate canalele RGB.

| Cale | Format | MSAA efectiv | Delta maxima rezolvata, /255 |
| --- | --- | --- | ---: |
| Hardware | RGBA8 UNorm | 1, 2, 4 | 0 |
| Hardware | RGBA8 UNorm | 8 | 25 |
| Hardware | RGBA16 float / RGBA32 float | 1, 2, 4, 8 | 0 |
| WARP | toate cele trei formate | 1, 2, 4, 8 | 0 |

`msaa-matrix-valid.json` inregistreaza numarul efectiv de mostre, nu numai cererea. In rapoartele MonoGame, campul `Adapter` provine din `GraphicsDevice.Adapter` si ramane numele adaptorului enumerat chiar cand este cerut WARP; nu trebuie interpretat drept adaptorul D3D efectiv al caii software. Proba Direct3D separata interogheaza adaptorul DXGI efectiv si confirma `Microsoft Basic Render Driver` pentru WARP.

Pattern-ul D3D standard explicit `{8,-1}` nu schimba esecul RGBA8. Bufferele vertex/index dedicate in locul ring bufferelor `DrawUserIndexedPrimitives` nu il elimina. Acestea sunt experimente de diagnostic, nu propuneri de reducere a MSAA sau schimbare a formatului in productie.

## Mostre individuale si limitele atribuirii

Un compute shader citeste `Texture2DMS.Load` pentru fiecare mostra dintr-o copie GPU a resursei MSAA; rezultatul numeric este copiat in staging. Nu se creeaza capturi OS sau imagini alternative capturilor `Window.SaveScreenshot`.

La pixelul (34,28), dupa G sunt observate mostre G `[221,20,221,221,20,20,20,221]`. Dupa dreptunghiul semitransparent, in prima secventa MonoGame sunt toate `171`, in loc sa ramana distincte `221` si `121`. Dupa M, mostrele acoperite devin `80`, celelalte pastreaza `171`; referinta fara G pastreaza `121`. Shader-ele si constant buffer-ul observate sunt identice intre cele trei draw-uri. Alpha-to-coverage este false, sample mask -1, blend One/InverseSourceAlpha/Add.

`msaa-each-draw.json` contine aceste valori. `msaa-pre-resolve.json` verifica separat ca diferenta exista si cand citirea este facuta **inainte** de `SetRenderTarget(null)`/resolve: delta maxima pe mostra 87/255, fata de 25/255 dupa resolve. O concluzie intermediara despre citirea initiala a mostrelor a fost corectata: prima citire era dupa resolve si nu dovedea singura aceasta localizare.

Totusi, proba Direct3D11 separata NU reproduce esecul. Ea deseneaza continut real (10806 mostre diferite de fundal la 8x), nu compara suprafete goale. Au fost verificate si combinatia shader-elor/constantelor MonoGame, vertex color packed, draw indexed, dispozitivul imprumutat din fixture si resolve, precum si separat obiectele native de blend/rasterizer/depth create prin MonoGame. Mostrele raman distincte, cu delta 0. `direct-aligned-state.json` si `direct-monogame-states.json` arhiveaza aceste controale.

Concluzie sustinuta: defectul depinde de calea MonoGame/hardware in configuratia RGBA8 8x observata. Nu este demonstrat inca daca invariantul este incalcat de utilizarea API, de o tranzitie/stare neizolata sau de driver. Nu exista temei pentru a declara generic „driver NVIDIA defect” ori pentru un patch de renderer bazat numai pe aceasta diferenta.

## Blocajul initial de diagnostic

Cererea explicita de creare a unui dispozitiv D3D11 cu debug layer esueaza cu `0x887A002D`: componenta SDK necesara lipseste sau nu corespunde. MonoGame incearca apoi fara debug flag; de aceea o rulare care doar cere `GraphicsAdapter.UseDebugLayers=true` nu produce validare D3D. `debug-layer-availability.json` contine exceptia directa, fara fallback.

Urmatorul pas propus este instalarea/repararea componentei optionale Windows Graphics Tools si rerularea probei cu debug layer confirmat activ. Aceasta este o modificare de sistem pentru care se cere aprobare separata, nu este executata implicit prin autorizarea investigatiei. Microsoft documenteaza Graphics Tools drept componenta necesara [pentru debug layer D3D11](https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-devices-layers); NVIDIA recomanda [capturarea diagnosticelor D3D](https://nvidia.custhelp.com/app/answers/detail/a_id/5604/kw/command/related/1) pentru separarea defectelor de aplicatie/runtime.

### Rezolvat: Graphics Tools instalat cu aprobare explicita

Utilizatorul a autorizat instalarea la 2026-09-05 08:59 UTC. Verificarea elevata a confirmat `NotPresent`. Prima incercare de instalare nu a modificat sistemul: parametrul `-NoRestart` fusese atribuit gresit cmdlet-ului `Add-WindowsCapability`, care nu il accepta. Sintaxa locala a fost verificata si comanda corectata la:

```powershell
Add-WindowsCapability -Online -Name 'Tools.Graphics.DirectX~~~~0.0.1.0'
```

Instalarea prin UAC si Windows Update s-a incheiat la 09:13 UTC: `Installed`, `RestartNeeded=false`, fara eroare. Scriptul nu a executat o repornire. `graphics-tools-install.json` arhiveaza verificarea de stare inainte/dupa. Driverul NVIDIA ramane `32.0.15.9159`; nu s-au modificat profile NVIDIA in aceasta operatie.

Verificarea directa a unui dispozitiv D3D11 Debug si `InfoQueue` reuseste acum (`debug-layer-after-install.json`). Matricea MonoGame a fost rerulata pe acelasi executabil verificat anterior: 24/24 cazuri au debug layer activ, zero exceptii, iar mesajele colectate sunt exclusiv Create/Destroy de resurse. Maximul este 57 mesaje/caz, sub limita de 128 a colectorului; nu s-au trunchiat aceste rezultate. Nu s-a raportat o utilizare invalida a API-ului in aceasta proba. Absenta diagnosticului nu demonstreaza corectitudinea intregului renderer.

`msaa-debug-layer-installed.json`: defectul RGBA8/8x hardware persista la 25/255; celelalte 23 cazuri au delta zero. Regresia permanenta rerulata dupa instalare ramane 1 PASS / 2 FAIL, text 47/255 si dreptunghi 25/255 (`graphics-tools-installed-occlusion.trx`). Instalarea rezolva disponibilitatea instrumentului, nu defectul grafic si nu gate-ul etapei 4. Cauza exacta ramane neidentificata; suita completa si corpusul vizual nu sunt declarate verzi.

Scriptul temporar de instalare a fost eliminat dupa verificare; nu exista proces de instalare sau harness activ. Iesirile `bin/obj` mentionate mai jos raman ignorate, fara surse/proiect temporar.

### Captura apelurilor Direct3D dupa instalare

`DXCap.exe` instalat impreuna cu Graphics Tools poate inregistra executabilul existent. Captura de comenzi `msaa-dxcap.vsglog` (1.631.433 bytes, SHA256 `28DE0021029079E55F8E3ED1A27C096981DE9F3EFF761BB8D27233EFBEDB7337`) pastreaza defectul: `msaa-under-dxcap.json` are 25/255 numai la Color/8x/hardware, celelalte 23 cazuri au delta zero. Nu s-a folosit optiunea de screenshot; aceasta este o inregistrare API, nu o captura alternativa de fereastra.

```powershell
# CERNEALA_MSAA_MATRIX=1 numai in procesul care lanseaza proba
DXCap.exe -file .artifacts/scene-import-stage4/gpu-driver/msaa-dxcap.vsglog -c tests/CodexGpuDriverHarness/bin/Debug/net8.0-windows/CodexGpuDriverHarness.exe .artifacts/scene-import-stage4/gpu-driver/msaa-under-dxcap.json
DXCap.exe -p .artifacts/scene-import-stage4/gpu-driver/msaa-dxcap.vsglog -toXML .artifacts/scene-import-stage4/gpu-driver/msaa-dxcap.xml
```

Exportul XML reuseste. La momentul 925 este creata resursa RGBA8 180x90, SampleDesc `{8,0}`; la 928 sunt legate opt sloturi RTV, cu numai primul nenul. Draw-urile sunt `DrawIndexed`, blend One/InverseSourceAlpha, sample mask `0xffffffff`, alpha-to-coverage dezactivat. Aceste observatii nu identifica o utilizare invalida a API-ului.

Controlul Direct3D a fost extins fara modificare de sursa, activand impreuna toate variantele deja disponibile: dispozitiv imprumutat, VS/PS/constante MonoGame, culoare packed, stari native MonoGame, opt sloturi RTV, draw indexed si resolve cu target legat. `direct-all-observed-states.json`: 16/16 cazuri, delta pe mostra zero, continut nenul. Combinatia nu reproduce defectul; ramane de izolat diferenta de executie dintre cele doua cai, nu de presupus ca o singura stare deja comparata este cauza.

Validarea automata hardware/WARP a capturii nu este o dovada utilizabila pentru acest defect. `DXCap.exe -v -file ... -examine draw,copy,clear` raporteaza mai intai diferente in initializarea fixture-ului (moment 575, inaintea resursei investigate) si avertismente ca formatele float nu au comparatie specializata. Apoi se opreste din progres cu `Couldn't create intermediate surface: 80070057`, inaintea cazului RGBA8/8x investigat. Procesul parinte a fost intrerupt si cele doua procese de replay ramase au fost oprite dupa verificarea PID-ului si a caii exacte a capturii. `msaa-dxcap-validation.log` pastreaza rezultatul incomplet. Nu se declara conformance verde sau cauza de driver pe baza lui.

Nu s-a modificat cod de productie, driver sau profil NVIDIA. Nu exista procese DXCap/harness ramase. Etapa 4 si investigatia cauzei raman deschise; urmatoarea izolare trebuie sa conserve secventa capturata care esueaza si sa elimine initializarea fixture-ului din proba de replay.

## Verificare si cleanup

- Build-ul harness-ului: 0 warnings, 0 errors. Unele iteratii anterioare de fixture au esuat la compilare/reflection; nu sunt folosite drept dovezi grafice.
- Dupa restaurarea profilelor, regresia permanenta a fost rerulata: `gpu-investigation-occlusion.trx`, 1 PASS (control), 2 FAIL (47/255 si 25/255). Nicio toleranta nu a fost relaxata.
- Nu s-a modificat cod Cerneala de productie in aceasta extindere de investigatie. Nu se declara suita completa sau corpusul vizual verde.
- Cele sapte fisiere temporare de sursa/proiect/script au fost eliminate. Snapshoturile lor locale sunt pastrate ca `.txt` necompilate in `.artifacts/scene-import-stage4/gpu-driver/harness-source/`, pentru reluarea diagnosticului.
- Stergerea recursiva a directorului temporar a fost refuzata de politica executiei. Au ramas numai iesirile ignorate `tests/CodexGpuDriverHarness/bin/` si `obj/`; nu s-a incercat ocolirea refuzului prin alt shell. Nu exista proces de harness activ sau profil NVIDIA temporar ramas.
- Indexul a fost reimprospatat dupa eliminarea sursei: 3959 documente, 11 warnings existente. `git diff --check` pentru checkpoint nu raporteaza erori. Validarea vizuala umana nu a fost efectuata.

Comanda regresiei permanente:

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent --logger "trx;LogFileName=gpu-investigation-occlusion.trx" --results-directory docs/plans/evidence/2026-09-04-scene-import-stage4/gpu-driver
```
