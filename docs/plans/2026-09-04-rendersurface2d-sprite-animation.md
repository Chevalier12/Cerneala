# Plan: animatie declarativa din sprite sheet

Status: finalizat (2026-09-05; o singura derogare de test, consemnata la etapa 4)
Acopera: #13
Depinde de: `2026-09-04-rendersurface2d-scene-foundation.md`

## Rezultat urmarit

Un sprite sau un tile promovat sparse poate declara in markup clipuri/stari precum `Idle`, `Walk` si `Attack`, cu cadre din atlas, durate, loop si flip. Tinta ramane compatibila Aspect/Motion/Prism. Animatia foloseste ceasul cadrului existent, invalideaza numai cand timpul poate schimba prezentarea si nu porneste timer, thread sau bucla de randare paralela.

## Contracte obligatorii

- Definitia clipului este imutabila/versionata si separata de starea de playback a unei instante.
- Durata fiecarui cadru este canonica; un `FramesPerSecond` convenience, daca este aprobat, este convertit fara a crea doua surse de adevar.
- Sunt definite explicit: frame initial, capat pentru non-loop, loop, ping-pong daca intra in v1, playback rate, pauza, restart si schimbarea starii.
- Flip-ul animatiei se compune cu `Sprite2D.Flip`; precedenta nu ramane ambigua.
- Esantionarea foloseste timpul absolut/delta stabilit de `RenderSurface2DFrame.FrameTime` si este determinista la granitele cadrelor.
- `OnDemand` ramane in repaus pentru sprite static/pauzat. O animatie activa cere cadre numai cat timp prezentarea se poate schimba.
- Attach/detach/reattach, schimbarea DataContext-ului si schimbarea clipului au o politica explicita de reset/pastrare a progresului.
- Aspect poate stabili clipul/starea/proprietatile vizuale aprobate; Motion poate anima transformul, tint/opacity si parametrii numerici aprobati fara sa devina un al doilea frame sampler.
- Prism proceseaza cadrul vizual curent si foloseste bounds-ul lui scene-space. Efectul nu schimba colliderul ori picking-ul geometric.
- Tile-ul promovat reutilizeaza acelasi model de clip/sampler ca sprite-ul; nu se construieste un al doilea motor de animatie pentru tilemap.

## Ownership si impact estimat

- Tipurile de clip/cadru/stare si nodul/proprietatile animate apartin scenei core, nu backend-urilor.
- `RenderSurface2D.UpdateRenderTime`/invalidarea temporala trebuie generalizata la „scena cere timp”, pastrand contractul `Continuous` si comportamentul imperativ existent.
- `Sprite2D` sau un `AnimatedSprite2D` dedicat va reutiliza aceeasi inregistrare, resursa, tint, origin, transform si bounds; alegerea se face la etapa 0 dupa API review.
- Instanta promovata din planul tilemap consuma acelasi contract animat direct sau prin compozitie cu nodul ales; dependenta merge din tile instance catre animatia core, nu invers.
- Generatorul `.crn`, testele SourceGen si documentatia publica sunt afectate.

## Etapa 0 - API review si teste RED

- [x] Se inventariaza toti consumatorii `FrameTime`, `RedrawMode`, `UpdateRenderTime`, `Sprite2D.SourceRect` si `Flip` prin RoslynIndexer.
- [x] Se compara extinderea `Sprite2D` cu un nod `AnimatedSprite2D`; se alege varianta cu o singura cale de desen si fara proprietati invalide pentru sprite static.
- [x] Se stabileste sintaxa markup pentru atlas, colectia de clipuri, cadre, stare curenta, loop, rate si flip; exemplul trebuie sa compileze conceptual fara API-uri fictive.
- [x] Sintaxa Aspect/Motion/Prism foloseste exact gramatica existenta demonstrata in `Tetrisish/MainWindow.crn`: property-element/`Aspect`, `@animate with` si `@prism { ... }`; orice sintaxa noua pentru clip sau tile promovat are mai intai test parser/SourceGen RED.
- [x] Se adauga teste RED pentru selectia cadrului la t=0, granite exacte, salt mare de timp, loop, non-loop, pauza, rate 0/negativ conform contractului si schimbarea starii.
- [x] Se adauga teste RED pentru invalidarea temporala in `OnDemand`: activa cere urmatorul cadru, pauzata/terminata nu mai cere; `Continuous` ramane compatibil.
- [x] Se adauga teste RED pentru attach/detach/reattach, schimbare atlas/clip, binding al starii si mai multe instante care impart definitia.
- [x] Se adauga teste RED combinate pentru Aspect+Motion+Prism pe sprite animat si tile promovat: Aspect schimba starea/tint, Motion schimba transformul/opacity, iar Prism incadreaza numai cadrul curent cu bounds corecte.
- [x] Se adauga teste RED pentru interactiunea Motion vs frame animation: fiecare proprietate are un singur owner efectiv, schimbarea starii este determinista si anularea/restaurarea Motion nu corupe progresul clipului.
- [x] Se adauga teste SourceGen RED pentru markup-ul complet `Idle`/`Walk`/`Attack` si diagnostic pentru nume duplicate, clip inexistent, source rect invalid si durata nepozitiva.

### Gate etapa 0

- [x] Toate RED-urile esueaza pentru capabilitatea absenta.
- [x] API review-ul alege clar nod dedicat sau extensie si elimina sursele de adevar duble pentru FPS/durate si flip.
- [x] API review-ul fixeaza reutilizarea de catre tile-ul promovat si matricea exacta a proprietatilor Aspect/Motion; resursele/clip collections neanimabile primesc diagnostic.
- [x] Politica de timp si lifecycle este scrisa in testele de contract.

Evidenta etapei 0 este arhivata in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage0/`. API-ul aprobat extinde `Sprite2D`, reutilizeaza acelasi contract prin compozitie pe `TileInstance2D`, exclude `FramesPerSecond` si ping-pong din v1 si compune flip-ul de baza cu flip-ul cadrului prin XOR pe fiecare axa. Cele 7 teste core si 5 teste SourceGen compileaza si sunt RED exclusiv la tipurile/proprietatile/diagnosticul `CERNEALAUI016` absente; rezultatele TRX sunt pastrate in directorul etapei.

## Etapa 1 - datele de animatie si esantionarea determinista

- [x] Se implementeaza definitiile imutabile/versionate de clip si cadru cu validare la constructie/rezolvare.
- [x] Se implementeaza un sampler pur: `(clip, elapsed, playback options) -> frame/progress/state`, fara dependenta de backend sau UI thread.
- [x] Se trateaza overflow-ul, duratele mari, elapsed regresiv si limitele numerice fara bucle proportionale cu numarul de loop-uri sarite.
- [x] Se testeaza sampler-ul cu tabele de cazuri si secvente randomizate cu seed fix.

### Gate etapa 1

- [x] Acelasi timp produce acelasi frame pe toate platformele.
- [x] Saltul cu milioane de cicluri are cost constant si nu acumuleaza drift iterativ.

Evidenta etapei 1 din 2026-09-04: `SpriteAnimationFrame`, `SpriteAnimationClip` si `SpriteAnimationSet` copiaza defensiv intrarile, valideaza geometria/duratele/numele/versiunile si nu contin progres per instanta. Samplerul intern foloseste ticks, reducere modulo/clamp si cautare binara, deci saltul temporal nu itereaza ciclurile sarite; overflow-ul ratei este saturat determinist, iar elapsed negativ/rata nefinita sau negativa sunt respinse. 5/5 teste focusate trec, inclusiv tabelul de granite, 10.000 de secvente cu seed fix, 10.000 de salturi de cate un milion de cicluri si corpusul RED al samplerului devenit GREEN (76 ms total raportat de runner). Documentatia canonica pentru cele patru API-uri publice si manifestul au fost sincronizate; testul manifestului trece 1/1. Indexul actualizat contine 3.748 documente, 99.716 simboluri si 406.056 referinte, cu cele 7 avertismente baseline.

## Etapa 2 - integrarea cu sprite-ul si markup-ul

- [x] Nodul/proprietatile aprobate reutilizeaza resursa si calea de inregistrare `Sprite2D`, schimband numai source rect/flip-ul efectiv si starea necesara.
- [x] Tile-ul promovat reutilizeaza aceeasi definitie/sampler si este exclus din batch-ul static pe durata promovarii; schimbarea cadrului nu reconstruieste alte tile-uri din chunk.
- [x] Starea curenta este bindable si schimbarea ei invalideaza o singura data; politica restart/resume este aplicata exact.
- [x] Generatorul construieste colectiile/definitiile declarative si emite diagnostice actionabile cu locatie sursa.
- [x] Definitiile partajate nu contin progres per instanta si nu tin referinte la suprafata/radacina.
- [x] Bounds, ordering, culling si picking folosesc geometria sprite-ului, nu dimensiuni arbitrare ale intregului atlas.
- [x] Aspect si Motion folosesc UiProperties/mixerele aprobate, iar Prism deschide un scope exception-safe in jurul cadrului curent pentru sprite si tile promovat.

### Gate etapa 2

- [x] Doua instante care impart `Walk` pot avea progres si flip diferit fara clonarea atlasului/clipului.
- [x] Markup-ul `Idle`/`Walk`/`Attack` compileaza si schimba source rect-urile asteptate.
- [x] Un tile promovat poate rula clipul si un efect Prism individual fara desen dublu si fara a dezactiva batching-ul celorlalte tile-uri.

Evidenta etapei 2 din 2026-09-04:

- `Sprite2D` si `TileInstance2D` compun acelasi `SpriteAnimationPlayback`; definitiile nu contin referinte la UI sau imagine, iar inregistrarea reutilizeaza sursa existenta. Source rect-ul efectiv si flip-ul XOR nu modifica valorile statice de fallback.
- 11/11 teste core ale etapelor 1/2 trec: progres independent, restart/resume, invalidare exact o data la schimbarea starii, selectie Aspect, Motion si restaurarea sa la urmatorul commit de cadru, scope Prism si bounds. Tile-ul promovat pastreaza batch-urile vecine: dupa schimbarea cadrului, `BatchesRebuilt=0` si `BatchesReused>0`.
- SourceGen trece integral 507/507, inclusiv factory compilat si executat cu binding `Idle`/`Walk`/`Attack`, verificarea dreptunghiurilor in comenzile reale de desen si diagnostice cu calea/intervalul `.crn` corecte. Language trece integral 186/186; golden-ul diagnosticelor LanguageServer trece 1/1 dupa includerea `CERNEALAUI016`.
- Regresiile RenderSurface2D/TileMap2D trec 80/80. Testul `ApiDocumentationManifestIsValidAndReferencesExistingFiles` trece 1/1. Paginile canonice Sprite2D, TileInstance2D si UiMarkupGenerator sunt sincronizate. Rezultatele TRX sunt in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage2/`.
- Corectii de fixture descoperite dupa eliminarea capabilitatii absente din RED-ul initial: sursa imaginii se verifica prin `DrawCommand.ImageSource`, nu `SourceRect`; culorile din directive sunt neghilimetate; diagnosticele additional-file au `LocationKind.ExternalFile`, nu un C# SourceTree; Motion necesita proba initiala t=0 si commit de cadru pentru restaurare. Afirmatia istorica despre RED acoperea primul esec observat, nu validitatea acestor asertiuni ulterioare.
- Scheduling-ul automat ramane intentionat pentru etapa 3; aici avansarea samplerului este explicita in testele de integrare. Nu se declara conformance nativ sau suita completa finala.

## Etapa 3 - scheduling si lifecycle

- [x] Scena agregheaza cererea de timp a descendentilor fara scanari/alocari necontrolate per frame.
- [x] `RenderSurface2D` solicita redraw la termenul/cadrul necesar prin mecanismul existent; nu se introduce un timer separat per sprite.
- [x] Sprite-urile invizibile/culled au politica explicita de progres (timpul poate continua, dar inregistrarea nu se face); revenirea in viewport este determinista.
- [x] Pauza, finalul non-loop, detach si disposal elimina cererea activa de timp.
- [x] Se masoara CPU, alocari si invalidari pentru 1, 100 si 10.000 de sprite-uri, cu proportii active/inactive si warmup documentat.
- [x] Se masoara separat costul tile-urilor promovate animate/cu Prism si numarul de batch splits; zero promovari pastreaza calea statica initiala.

### Gate etapa 3

- [x] In `OnDemand`, zero animatii active inseamna zero invalidari temporale dupa stabilizare.
- [x] Nicio instanta nu porneste timer/thread propriu si detach-ul elimina toate cererile.
- [x] Costul si alocarile sunt arhivate; orice prag numeric este stabilit din baseline inainte de optimizare.

Evidenta etapei 3 din 2026-09-04 este in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage3/`:

- Suprafata mentine un registru sparse al instantelor active, cu eliminare O(1), foloseste delta UI existent si emite cel mult o invalidare temporala pe tick. Traversarea UI nu rescaneaza scena; overlay-ul continua sa primeasca timpul UI. Lifecycle-ul elimina/reinregistreaza cererile fara resetarea progresului.
- 25/25 teste de animatie sunt GREEN, inclusiv 7 cazuri de scheduling/lifecycle. RED-urile au demonstrat si coada non-loop cu prezentari identice, precum si lipsa propagarii versiunii Prism spre grupul logic. Tranzitiile vizuale sunt calculate o singura data in clipul imutabil; scena foloseste acum aceeasi agregare Prism cu parintele sau logic de randare. Nu exista invalidare generala de cache.
- Culling-ul foloseste intersectia conservatoare existenta; inputurile unui Prism propriu sau de stramos sunt pastrate deoarece efectul poate extinde bounds-ul. Doua fixture-uri foundation de transformare/ordonare au fost mutate in viewport fara eliminarea asertiunilor lor. Suita core initiala: 3.386 trecute, 2 esecuri de fixture, 2 conformance opt-in omise; rerularea celor doua fixture-uri corectate: 2/2 GREEN. Restul rezultatelor sunt reutilizate fara modificari ulterioare de productie; nu se declara inca suita finala GREEN.
- Benchmark Release: 128 iteratii warmup, 256 masurate, delta 16 ms, 1/100/10.000 sprite-uri cu 0/~10/100% active, plus 1.024 tile-uri cu 0/1/100 promovari si variante Prism. Pentru 10.000 active, P95 temporal scade de la 7.825,3 us la 5.818,1 us; alocarile de la 2.720.008,97 la 8,97 bytes/tick, eliminand exact 272 bytes/instanta/tick. Zero active ramane zero invalidari/alocari temporale. Clock+commit+record pentru 10.000 active ramane P95 33.479,8 us: nu se promite 60 FPS sau zero alocari pentru cadrul complet.
- Zero promovari pastreaza un singur batch static, fara redraw temporal. Cu 1/100 promovari sunt 1/100 splits, 0 rebuilds si 2/101 batch-uri vecine reutilizate la schimbarea cadrului, inclusiv cu Prism individual. Costurile nu includ executie GPU; valorile si gate-ul fixat anterior optimizarii sunt in README/baseline/optimized.
- Documentatia canonica pentru timp/lifecycle a fost sincronizata prin `writing-api-documentation`; manifestul trece 1/1. `git diff --check` trece. Indexul a fost actualizat; al optulea avertisment este TRX-ul core arhivat (4,7 MB) peste limita de indexare text, pe langa cele 7 baseline.

## Etapa 4 - conformance, documentatie si integrare

- [x] Se adauga scenarii deterministe multi-frame pe MonoGame/WindowsDX si SDL_GPU pentru source rect, tint, opacity, transform, ordering si flip.
- [x] Scenariile includ Aspect+Motion+Prism simultan pe sprite si tile promovat, cu verificarea ca gameplay collider/picking nu este modificat de Prism.
- [x] Se capteaza cadrele Cerneala numai cu `Window.SaveScreenshot` si se compara la timpi ficsi, nu „cand pare ca a trecut destul”.
- [x] Se foloseste `writing-api-documentation` pentru nod, clipuri, cadre, stari si proprietati.
- [x] Se documenteaza timpul, loop-ul, schimbarea starii, lifecycle-ul, culling-ul si exemple markup reale.
- [x] Se documenteaza folosirea pe tile promovat si sintaxa Cerneala reala; nu se publica pseudo-XML din discutia de design.
- [x] Se actualizeaza manifestul, se reindexeaza, se ruleaza API Compat strict si testul manifestului.
- [x] Se ruleaza testele focusate, conformance-ul ambelor backend-uri si `dotnet test .\Cerneala.slnx` (GREEN cu exceptia unica aprobata explicit mai jos).

Evidenta istorica partiala din 2026-09-04 (inainte de remediere si inchiderea etapei):

Decizie explicita a utilizatorului la reluare: remedierea SpinBlur este autorizata in etapa 4; remedierea bugetului LanguageServer este exclusa. Utilizatorul autorizeaza exceptarea exclusiv a testului `HardeningProtocolTests.FullSolutionIncrementalRequestsRespectWarmBudgets` din gate-ul final, cu esecul preexistent consemnat. Nu se modifica testul, pragul <100 ms sau implementarea LanguageServer. Toate celelalte teste si intregul corpus nativ raman obligatorii.

- Scenariul nativ comun WindowsDX/SDL_GPU trece la 0/100/200/300 ms, cu Aspect, Motion si Prism pe sprite/tile, scope de grup, flip, tint, opacity, transform si geometrie de picking/coliziune verificata. Capturile folosesc numai `Window.SaveScreenshot`; diferenta maxima intre backend-uri este 1 pe canal, zero pixeli peste toleranta 3, fara modificari de golden/toleranta. Motion foloseste endpoint-uri completate explicit, nu pretinde esantionare nativa intermediara.
- Documentatia canonica si exemplele declarative au fost completate prin `writing-api-documentation`; manifestul are 1.113 intrari valide, iar testul sau trece. API Compat strict trece cu exact cele 20 de diferente aprobate fata de suppressions-urile planurilor anterioare; auditul explicit include membrii noi ai TileInstance2D. Dovezile sunt in `benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage4/`.
- Suita completa Release cu `CERNEALA_SDL_NATIVE_TESTS=1`: 4.465 trecute, 2 esecuri, zero skip-uri. Toate cele 25 de teste core de animatie trec; SourceGen 507/507, Language 186/186, SDL_GPU 124/124, PreviewHost 13/13, VisualStudio 47/47 si Tetris 29/29. Core are 3.520 trecute si un esec; LanguageServer 39 trecute si un esec.
- Blocaj nativ: SpinBlur depaseste maximul permis 49 cu valoarea 62 (MAE 0,0431, P99 1), atat in suita, cat si izolat. Acelasi fixture nemodificat pe baseline-ul `fed724b954` reproduce exact aceleasi valori. Corpusul Prism este 131/132 verde; mismatch-ul este preexistent, dar backend-ul vinovat/cauza exacta nu sunt inca stabilite.
- Blocaj de performanta: completarea incrementala LanguageServer are P95 151,79 ms in suita si 172,30 ms izolat, fata de pragul <100 ms. Baseline-ul reproduce esecul la 190,95 ms. Ipoteza ca numai executia paralela explica problema a fost respinsa; cauza exacta ramane nedeterminata.
- La acel checkpoint nu se schimbasera productie, praguri ori asteptari pentru aceste doua esecuri; etapa si done-ul au ramas nebifate pana la decizia explicita de scop/derogare. Nu s-a efectuat validare manuala umana.

Evidenta finala a etapei 4 din 2026-09-05, arhivata in acelasi director:

- SpinBlur: proba fara Prism este identica intre backend-uri (max delta 0). Regresia GPU permanenta izoleaza citirea accidentala a nivelurilor mip: baza rosie constanta cu mip-uri santinela albastre trece fara mipmaps si este RED cu ele (eroare Vector4 maxima 0,78807193). Shaderul comun cere acum explicit LOD 0 numai pentru SpinBlur, conform samplerului CPU; nu se modifica formule, praguri, golden-uri sau ceilalti consumatori ai helperului. Versiunile shader sunt 57 pe ambele backend-uri, iar artefactele SDL DXIL/SPIR-V/MSL sunt regenerate/verificate.
- Regresia si reproducerea nativa originala sunt GREEN 3/3. SpinBlur are acum MAE 0,0011, P99 0, max 1, fata de max 62 anterior. Intregul corpus Prism trece 132/132. Scenariul animat este recapturat pe ambele backend-uri si cele patru comparatii raman la max delta 1, fara pixeli peste toleranta sau allowances consumate.
- Gate-ul final: `dotnet test .\Cerneala.slnx -c Release -m:1 --no-build --no-restore --filter 'FullyQualifiedName!=Cerneala.Tests.LanguageServer.HardeningProtocolTests.FullSolutionIncrementalRequestsRespectWarmBudgets'`, cu `CERNEALA_SDL_NATIVE_TESTS=1`, trece 4.468/4.468, zero esecuri si zero skip-uri. Exclus exact testul autorizat, nu declarat trecut. Core 3.523, Language 186, LanguageServer 39, PreviewHost 13, SDL_GPU 124, SourceGen 507, VisualStudio 47, Tetris 29; logul si cele opt TRX-uri sunt in `verified-suite.log`/`verified-suite/`.
- Incercarea anterioara a gate-ului a avut un blocaj de build VisualStudio: assets LanguageServer fara targetul `net10.0/win-x64`. Invocarea separata, nemodificata, a proiectului VisualStudio a restaurat/publicat componentele, produs VSIX-ul si trecut 47/47. Gate-ul complet a fost apoi rerulat pe build-urile curente. Nu s-au modificat proiecte ori LanguageServer pentru a masca blocajul.
- Documentatia/manifestul sunt sincronizate si testul manifestului trece in gate-ul final. SHA256 al core ramane identic auditului API Compat strict, deci dovada este reutilizata. Indexul actualizat are 3.812 documente, 100.250 simboluri, 409.387 referinte; cele 11 avertismente sunt cele 7 baseline plus cele 4 TRX-uri core mari arhivate. `git diff --check` trece.
- Limitari explicite: build-ul shader WindowsDX are 158 avertismente FX si zero erori; nu se declara warning-free. Validarea manuala umana si executia nativa pe alte sisteme de operare nu au fost efectuate. Instrumentarea temporara a fost eliminata; curatarea artefactelor generate ale probei fara Prism a fost blocata de politica tool-ului, iar locatiile ramase sunt consemnate in README. Nu exista cod de proba ramas in productie sau in fixture-ul catalogului.

## Definitia de done

- [x] #13 functioneaza declarativ cu stari, durate/ritm, loop si flip.
- [x] Sprite-ul animat si tile-ul promovat satisfac Aspect/Motion/Prism fara sampler duplicat sau pierderea batching-ului pentru tile-urile obisnuite.
- [x] Esantionarea este determinista si separata de backend.
- [x] Scheduling-ul nu consuma cadre cand scena este statica si nu are timer per sprite.
- [x] Lifecycle-ul, sharing-ul si culling-ul sunt verificate.
- [x] Documentatia, manifestul, API diff-ul, conformance-ul si suita completa sunt verzi, cu exceptia exclusiva a testului LanguageServer autorizat explicit de utilizator.
