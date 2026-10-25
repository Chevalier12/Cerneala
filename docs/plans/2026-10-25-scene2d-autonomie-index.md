# Plan: obiecte care se mișcă singure în Scene2D (index)

> Data: 2026-10-25
> Status: in progres (planul 1 finalizat)
> Context: pasul 6 din pagina „Drumul vulpii în Cerneala”: un obiect se mișcă fără să apeși nimic (o albină care caută flori). Pasul 13 (sfârșitul atacului) are nevoie de evenimentul de animație din planul 6.
> Depinde de: `InputHeld` (PR #761, pe master din 2026-10-25).

## Rezumat

Azi un sprite se mișcă singur doar în două feluri:

- din `RenderSurface2D.Draw`, adică tot jocul scris într-un callback per frame pe suprafață, ca în `Playground/Cerneala.SceneVillage/VillageGameSurface.cs`;
- prin Motion pe obiect (`MotionExtensions.Motion<T>()`), pe care utilizatorul l-a respins ca neintuitiv pentru logica de joc.

Utilizatorul a ales un API nou direct pe nod. Ținta de utilizare (schiță, **nu** API existent):

```csharp
bee.Vision = new ConeCollider2D { Radius = 150, Angle = 90 };   // vede 150 px, într-un con de 90°
bee.Vision.Entered += (_, e) =>
{
    if (e.Other == flower.Collider)
    {
        bee.MoveTo(flower, speed: 80);   // 80 px pe secundă, urmărește floarea
    }
};
bee.Arrived += (_, _) => bee.AnimationState = "eat";
bee.Blocked += (_, e) => bee.AnimationState = "idle";

bee.IsAwake = true;                       // primește Awake o dată pe frame
bee.Awake += (_, e) => hunger -= e.Elapsed.TotalSeconds;
```

Regula comună pentru tot indexul: **fereastra lucrează numai cât timp ceva se mișcă sau e treaz, apoi doarme.** O scenă fără `IsAwake`, fără `MoveTo` activ și fără contacte de procesat nu cere frame-uri noi.

## Planuri dependente

Fiecare plan se livrează separat, în ordinea de mai jos.

| # | Plan | Livrează | Depinde de |
| --- | --- | --- | --- |
| 1 | [scene2d-contacte](2026-10-25-scene2d-contacte.md) | `Collider2D.Entered` / `Exited` și argumentele lor | — |
| 2 | [scene2d-vision](2026-10-25-scene2d-vision.md) | `ConeCollider2D` și `Vision` (numai con) pe `Sprite2D` și `Scene2D` | planul 1 |
| 3 | [scene2d-awake](2026-10-25-scene2d-awake.md) | `IsAwake`, `Awake`, `FrameTimeEventArgs` pe `Sprite2D` și `Scene2D` | — |
| 4 | [scene2d-moveto](2026-10-25-scene2d-moveto.md) | `MoveTo`, `IsMoving`, `Stop`, `Arrived`, `Blocked` pe `Sprite2D` și `Scene2D` | planul 3 (ceasul per frame) |
| 5 | [scene2d-model-ml](2026-10-25-scene2d-model-ml.md) | Pachetul opțional `Cerneala.ML`: `UseModel` cu un model `.onnx` gata antrenat | planul 3; exemplul cere și planurile 2 și 4 |
| 6 | [scene2d-misc](2026-10-25-scene2d-misc.md) | `Sprite2D` nesigilat, `Collider2D.Owner`, `Scene2D.IsPaused`, `MoveDirection`, `AnimationCompleted`, `AnimationLooped`, `AnimationEvent` | nimic pentru primele părți; pauza cere planurile 3 și 4, `MoveDirection` cere planul 4 |

Planurile 1 și 3 nu se ating între ele. Ordinea aleasă de utilizator este totuși 1 → 2 → 3 → 4. Planul 5 vine după ele. Planul 6 poate începe oricând: părțile lui care cer alte planuri așteaptă.

## Decizii luate de utilizator

- API nou pe nod, nu Motion și nu un callback global per frame.
- Respinse: un eveniment generic `TimeElapsed`, sistem de Behaviors, `WhileKeyUp`.
- `Collider2D` primește evenimentele `Entered` și `Exited`. Argumentele au `Other`: colliderul care a intrat sau a ieșit.
- Forma evenimentelor de contact (D1, D2, D3, decise 2026-10-25):
  - D1: `Entered` și `Exited` sunt evenimente CLR, `event EventHandler<ColliderContactEventArgs>`. Sender-ul este colliderul pe care stă handlerul. Nu urcă prin arbore (nu sunt routed events).
  - D2: clasa de argumente se numește `ColliderContactEventArgs` și are `Collider2D Other`.
  - D3: „se ating” înseamnă aceeași regulă ca `CollisionWorld2D.Intersects`: suprapunere sau atingere pe muchie, cu toleranța existentă `CollisionNarrowPhase2D.Epsilon`. Filtrul layer/mask se aplică în ambele sensuri. Triggerele și corpurile solide contează la fel.
- `Vision`:
  - este separat de colliderul corpului;
  - este `null` implicit, pentru că nu orice obiect are nevoie de vedere;
  - **este numai de tipul nou `ConeCollider2D`** (decizie din 2026-10-25, care înlocuiește varianta „orice tip de collider”).
- `ConeCollider2D` este un collider nou în formă de con: un cerc tăiat. Se poate modifica, ca să alegi ce fel de vedere are obiectul. Exemple: con îngust și lung pentru un paznic, con larg pentru o albină, 360° pentru vedere în jur.
- Proprietățile conului (D16, decis 2026-10-25):
  - `Radius`, în px;
  - `Angle`: cât de larg vede, în grade, între 0 și 360 (360 = cerc întreg);
  - `Direction`: unde privește, în grade, 0 = dreapta, crește în sensul acelor de ceasornic pe ecran;
  - `OffsetX`/`OffsetY`, moștenite.

  Gradele sunt alese pentru markup, deși `Rotation` din motor este în radiani (`Sprite2D.cs:810`).
- `ConeCollider2D` poate fi numai `Vision` în prima livrare (D17, decis 2026-10-25). Pus în `Sprite2D.Collider`, aruncă excepție. Ca collider de corp poate veni mai târziu, cu plan separat.
- Conul urmează `Flip` (D18, decis 2026-10-25): `Flip` orizontal oglindește `Direction`, iar `Flip` vertical la fel. Este o excepție de la regula de azi, în care `Flip` nu schimbă coliziunea (`Sprite2D.cs:69`). Conul urmează și `Rotation`.
- `Vision` este doar senzor (D4, decis 2026-10-25):
  - nu blochează pe nimeni: vulpea trece prin con fără să se oprească;
  - nu poate fi nimerit cu click-ul;
  - atingerea există și e detectată, dar o află **numai proprietarul conului** (`bee.Vision.Entered`). Corpul care intră nu primește nimic: `fox.Collider.Entered` nu vine pentru un con;
  - interogările altora (`MoveAndCollide`, `Raycast`, `Overlap`) nu văd conul;
  - două `Vision` nu se văd între ele.
- `Vision`, `IsAwake`/`Awake` și `MoveTo` apar pe `Sprite2D` și `Scene2D`, **nu** pe `SceneNode2D`, deci nici pe `Collider2D` sau `TileMap2D`.
- `Scene2D.Vision` există pentru obiecte complexe făcute din mai multe sprite-uri (un dragon). Dacă și un copil are `Vision`, fiecare anunță separat.
- `IsAwake` + `event Awake` cu `FrameTimeEventArgs` (are `Elapsed`). `Awake` vine o dată pe frame, numai cât timp `IsAwake` este `true`.
- `MoveTo(punct, viteză)`: linie dreaptă, viteza în px/s.
- `MoveTo(obiect, viteză)`: urmărește ținta pe măsură ce se mută.
- `IsMoving` și `Stop()`.
- `Arrived` vine o singură dată, la sosire. Apoi mișcarea se oprește (`IsMoving = false`). Pentru o nouă urmărire se cheamă din nou `MoveTo`.
- `Blocked` vine când obiectul lovește un zid. Argumentul conține `CollisionHit2D`-ul existent.
- Un `MoveTo` nou anulează traseul vechi. Traseul anulat nu primește `Arrived`.
- Căutarea nu primește API nou: este `CollisionWorld.Overlap(vision)`. Animația se alege tot cu `AnimationState`. Evenimentele de animație vin în planul 6.
- `PathfindTo(țintă, viteză)` și o hartă de navigație vin mai târziu, cu design separat.
- Decizii din 2026-10-25 pentru planul 6 (misc):
  - `Sprite2D` devine nesigilat, ca să meargă `class Bee : Sprite2D`;
  - din collider se ajunge direct la obiectul care îl are (de exemplu `e.Other.Owner is Bee bee`);
  - `IsPaused` pe `Scene2D` oprește `Awake`, `MoveTo` și animațiile din toată scena;
  - `MoveDirection` (încotro merge obiectul) și evenimentele de animație intră tot în planul 6.
- Ce **nu** oprește `IsPaused` (D21, decis 2026-10-25): inputul (click, `InputHeld`), Motion, `RenderSurface2D.Draw` și apelurile manuale (`MoveAndCollide`) merg mai departe. Așa funcționează un meniu de pauză. Codul de joc citește `world.IsPaused` dacă vrea să ignore tastele.
- `class Bee : Sprite2D` nu poate strica coliziunile sau animațiile (D19, decis 2026-10-25): pe `Sprite2D`, `OnPropertyChanged`, `OnAttached` și `OnDetached` devin `protected sealed override`. Clasa derivată primește hook-uri goale (`OnSpritePropertyChanged`, `OnSpriteAttached`, `OnSpriteDetached`), care rulează după codul din `Sprite2D`. E tiparul din `Collider2D.OnColliderPropertyChanged`.
- Evenimentele de animație (D24, decis 2026-10-25): `AnimationCompleted` (la finalul unui clip care nu se repetă), `AnimationLooped` (la fiecare tură a unui clip care se repetă) și `AnimationEvent` (la un frame marcat, de exemplu `"hit"`). Fiecare vine exact la momentul lui, cu cel mult un frame întârziere, și trezește fereastra numai dacă cineva ascultă.
- Problema Servo cu obiecte mereu treze (`WaitForIdle` nu se termină, pașii Servo nu avansează timpul) are issue separat: [#762](https://github.com/Chevalier12/Cerneala/issues/762). Decizia de acolo poate schimba regula Servo din planurile 3 și 4.

## Decizii deschise

Fiecare decizie blochează Etapa 0 a planului indicat. Recomandarea este propunerea agentului, **nu** decizie luată.

| Id | Plan | Întrebare | Recomandare |
| --- | --- | --- | --- |
| D5 | 4 | Ce face `MoveTo` după `Blocked`? | Se oprește (`IsMoving = false`, fără `Arrived`). Utilizatorul decide în handler ce urmează. Alunecarea pe zid nu intră în prima livrare. |
| D6 | 4 | La `MoveTo(obiect)`, ce se întâmplă când corpul atinge corpul țintei? | Asta înseamnă `Arrived`, nu `Blocked`. Altfel o albină cu collider nu „ajunge” niciodată la o floare cu collider. |
| D7 | 4 | `Scene2D.MoveTo` ține cont de coliziuni? | Nu în prima livrare: `Scene2D` nu are corp propriu. Pe `Scene2D`, `Blocked` nu apare. |
| D8 | 4 | Tipul țintei la `MoveTo(obiect)`. | `SceneNode2D`: se urmărește originea ei în spațiul scenei. Ținta scoasă din scenă oprește mișcarea fără eveniment. |
| D9 | 3 | Primește `Awake` o scenă fără `RenderSurface2D` (headless, `SceneSimulationContext2D.Update`)? | Nu în prima livrare. Acolo `Update()` nu primește timp. |
| D10 | 4 | Numele argumentelor pentru `Blocked`. | `MoveBlockedEventArgs`, cu `CollisionHit2D Hit`. |
| D11 | 5 | Ce primește `act`? | Un singur tip, `ModelOutput`, cu `Action` (argmax), `this[int]` și `Length`. Două overload-uri cu lambda (`int` vs ieșire brută) ar fi ambigue în C#. |
| D12 | 5 | Cât de des rulează modelul? | Pe fiecare frame implicit. Opțional, un parametru `every: TimeSpan`. |
| D13 | 5 | Pe ce fir rulează modelul? | Sincron, pe firul UI, în `Awake`. Modelele mici rulează sub o milisecundă. Rularea asincronă rămâne pentru mai târziu. |
| D14 | 5 | Ce întoarce `UseModel` și ce face un al doilea apel? | Un `IDisposable` care oprește legătura. Un al doilea `UseModel` pe același sprite îl înlocuiește pe primul. |
| D15 | 5 | Cum se împarte un model între sprite-uri? | Un cache intern după calea completă, cu contor de referințe. Modelul se eliberează la ultima oprire. |
| D20 | 6 | Numele și tipul pentru „obiectul care are colliderul”. | `Collider2D.Owner`, de tip `SceneNode2D?`: sprite-ul, scena (pentru `Vision`) sau `TileMap2D`. `null` când colliderul nu e atașat. |
| D22 | 6 | Tipul lui `MoveDirection`. | `Vector2` de lungime 1, în spațiul scenei. `Vector2.Zero` când obiectul nu merge. |
| D23 | 6 | Numele argumentelor pentru evenimentele de animație. | `SpriteAnimationEventArgs`, cu `string ClipName` (numele clipului, mereu setat) și `string? EventName` (numele de pe frame, setat numai la `AnimationEvent`). Un singur `Name` ar fi ambiguu: ar însemna clipul la un eveniment și frame-ul la altul. |
| D25 | 6 | Cum se marchează un frame cu un eveniment? | Parametru opțional `string? eventName` în constructorii `SpriteAnimationFrame`, proprietatea `EventName` și atributul de markup `EventName="hit"`. Un nume gol sau doar din spații aruncă excepție. |

## Non-obiective pentru tot indexul

- `PathfindTo`, harta de navigație și orice algoritm de pathfinding. Azi există doar interfața de debug `IScene2DDebugNavigationGrid` (`UI/Controls/Scene2DDebugOverlay.cs:25`).
- Fizică (viteză, forțe, gravitație), alunecare pe ziduri, rotire automată spre direcția de mers.
- Animații pornite automat de `MoveTo`.
- API de căutare (`Search`) peste `CollisionWorld2D.Overlap`.
- Un eveniment global per frame sau un game loop public.
- Schimbarea `RenderSurface2D.Draw`, a Motion sau a `InputHeld`.
- Orice dependență de ML în motorul `Cerneala`. Modelele antrenate stau numai în pachetul opțional `Cerneala.ML` (planul 5). Antrenarea se face în afara Cerneala.

## Definiția de gata pentru index

- [ ] Planurile 1–6 sunt finalizate, fiecare cu propria definiție de gata bifată.
- [ ] Un test de integrare pe `WindowApplicationRuntime` cu un fake de platformă arată scenariul din Rezumat cap-coadă. Albina vede floarea, merge la ea și primește `Arrived`. După `IsAwake = false` și sosire, `PumpOnce` nu mai randează frame-uri.
- [ ] Pagina „Drumul vulpii în Cerneala” are pasul 6 verde, cu un exemplu care folosește API-ul livrat.
- [ ] `docs/architecture/scene2d.md` descrie contactele, `Vision`, `Awake` și `MoveTo`. `hosting-and-platform.md` și `servo.md` listează noile condiții de trezire.
- [ ] Fiecare raport de verificare spune că suita completă nu a fost rulată (regula din `CLAUDE.md`).
