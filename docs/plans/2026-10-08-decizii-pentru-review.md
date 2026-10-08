# Decizii luate cât ai lipsit (pentru review)

> Data: 2026-10-08
> Regula convenită: la orice decizie am ales varianta pe care aș fi recomandat-o; fiecare are un exemplu concret și motivul, ca să le poți schimba.

Format: **Decizie** — exemplu — ce se întâmplă — de ce.

## Decizii

### 1. Cum am măsurat „costul nu crește” (planul fundație, Etapa 4)

- **Exemplu:** un `StackPanel` cu 200 de butoane, fiecare cu același program: `@when IsMouseOver` (animație Opacity la 0.8 / înapoi la 1) și `@on Click` (animație la 0.5). L-am rulat de trei ori: Aspect resursă (`Aspect="$Busy"`), Aspect inline (`<Button.Aspect>…`) și Aspect implicit (`<Aspect TargetType="Button">` fără nume). Același test, identic, pe commit-ul vechi `59c0e257` și pe codul de acum.
- **Ce am numărat:** câte behavior-uri are fiecare buton, câte invalidări se fac până se liniștește ecranul, și câte se fac în 30 de frame-uri în care nu atinge nimeni nimic (trebuie 0).
- **Rezultat:** acum sunt la fel sau mai puține peste tot. Inline: înainte 3 behavior-uri pe buton, acum 2. La idle: 0 și înainte, și acum.
- **Timpul:** am măsurat „creare + atașare” împreună, nu doar atașarea. Motivul, concret: înainte, sesiunile unui buton se făceau în constructor; acum se fac la atașare. Dacă număram doar atașarea, Aspect-ul inline părea mai lent (≈280 ms față de ≈210 ms), dar asta doar pentru că munca se mutase din constructor în atașare. Pe total: resursă ≈100 ms față de ≈260 ms, inline ≈320 ms față de ≈460 ms, implicit ≈50–90 ms față de ≈225 ms. Deci e mai rapid.
- **De ce:** timpul e zgomotos (aceeași rulare variază de 2×), deci gate-ul se bazează pe numere care nu variază (behavior-uri, invalidări); timpul e doar informativ.

### 2. Am reparat 2 invalidări în plus pe buton, în loc să le accept

- **Exemplu:** butonul de mai sus, cu `@if value == false { @animate … }`. La atașare, condiția „mouse-ul nu e deasupra” e adevărată. Codul nou anunța „condiția s-a schimbat” și cerea redesenarea butonului (o invalidare `Aspect`), deși condiția nu schimbă nicio proprietate — doar pornește o animație. 200 de butoane = 400 de invalidări inutile.
- **Ce am făcut:** semnalul „condiția s-a schimbat” se trimite doar pentru condițiile care chiar setează ceva (`@if … { Background = Red; }`) sau aduc conținut — exact cum era înainte. Acum numărul de invalidări e identic cu cel vechi (8261 la 200 de butoane).
- **De ce:** era o regresie mică introdusă de mutarea programului în behavior; planul cere ca costul să nu crească. Fișier: `Cerneala.SourceGen/UiMarkupReactiveEmitter.cs`.

### 3. Forma C# a lui `TimbreClipDefinition` (planul Timbre/Prism, Etapa 0)

- **Exemplu:**
  ```csharp
  var ui = new TimbreClipDefinition("UiSounds",
      sounds: [new TimbreClipSound("Click", new TimbreSound(TimbreSource.FromFile("audio/click.wav"))),
               new TimbreClipSound("Music", new TimbreSound(TimbreSource.FromFile("audio/music.ogg"), loop: true), autoPlay: true)]);
  button.Timbre.Play(ui.Sounds["Click"].Sound);
  ```
- **Ce am ales:** `Sounds` e un dicționar după nume (ca să meargă exact `ui.Sounds["Click"]`, cum ai aprobat), dar se enumeră în ordinea în care ai scris sunetele — ca `AutoPlay` să pornească în ordinea din markup. `AutoPlay` stă pe `TimbreClipSound` (sunetul din clip), nu pe `TimbreSound`, pentru că `TimbreSound` se poate reda și singur din C#, unde „AutoPlay” nu înseamnă nimic.
- **Un clip fără niciun `@sound` e eroare** (`TimbreClip needs at least one @sound.`), la fel cum `PrismClip` cere cel puțin un `@layer`.
- **De ce:** e forma aprobată, plus cele două detalii care lipseau (ordinea și unde stă AutoPlay).

### 4. Argumentele `@timbre $UiSounds(Brightness = 800)` ajung la runtime ca un dicționar nume → număr

- **Exemplu:** `@timbre $UiSounds(Brightness = 800);` devine în codul generat `AttachTimbre(target, new ResourceId<TimbreClipDefinition>("UiSounds"), new Dictionary<string, float> { ["Brightness"] = 800f })`.
- **De ce:** resursa `$UiSounds` se caută abia la aplicarea Aspect-ului (decizia aprobată), deci argumentele trebuie potrivite după nume atunci, nu la compilare. Dacă cineva înlocuiește resursa din C# cu un clip care nu are `Brightness`, aplicarea dă excepție cu numele parametrului.

### 5. Păstrez „sesiunea Timbre” a Aspect-ului, dar fără sloturi de handle

- **Exemplu:** butonul `Music` cu `@on Click { @pause $Speaker.timbre.Music; }` nu are `@timbre` propriu, dar tot trebuie să asculte `Click` și să moară odată cu Aspect-ul lui. Asta face sesiunea (`AttachTimbreSession`): ține abonarea la `Click` și animațiile de volum pornite de buton. Sunetele în sine stau în atașarea lui `@timbre` de pe `Speaker`.
- **De ce:** mecanismul există deja și ignoră ascunderea elementului (un buton ascuns nu-și oprește sunetul), cum cere planul. Scot doar partea cu handle-uri.

### 6. Mesajele de eroare noi (înghețate în corpus)

- **Exemple:** `@play Click;` → „@play requires a sound path: '$self.timbre.Sound', '$owner.timbre.Sound' or '$Name.timbre.Sound'.”; `@on Click { @timbre { … } }` → „@timbre is written at the top of the Aspect body, not inside @on, @when or @if; use @play $self.timbre.Sound to start a sound.”; `<TimbreClip Name="Tone">Source = "a.wav";</TimbreClip>` (forma veche) → „TimbreClip declares @parameter and @sound; write 'Source' inside '@sound Name { … }'.”
- **De ce:** fiecare mesaj spune și ce să scrii în loc, pentru că sintaxa veche dispare fără alias.

### 7. Două elemente din proiect au deja `Aspect="$Resursă"` și `@prism` în conținut

- **Exemplu 1:** `Playground/…/CyberReel.crn` — `<Grid Aspect="$Director">` cu `@prism` în conținut. `Director` e folosit doar de acest Grid → mut `@prism` în `Director`.
- **Exemplu 2:** `tests/Fixtures/VisualStudioConsumer/MainView.crn` — `$CardAspect` e pus pe două `Border`-e, dar doar al doilea are `@prism`. Dacă puneam `@prism` în `CardAspect`, și primul Border ar fi primit efectul. Fac o resursă nouă, `GlowingCardAspect`, cu același corp plus `@prism`, pusă doar pe al doilea Border.
- **De ce:** un element are un singur Aspect, deci efectul trebuie să intre în Aspect-ul lui, fără să schimbe alte elemente.

### 8. Schimbare de comportament la migrarea panourilor și testelor: click repetat repornește, nu suprapune

- **Exemplu:** azi `@on Click { @timbre $Wav; }` — trei click-uri rapide = trei sunete suprapuse. După migrare `@on Click { @play $self.timbre.Wav; }` — trei click-uri = același sunet pornit de la început de trei ori (fiecare oprește pe cel dinainte).
- **De ce:** e regula aprobată („`@play` pe redare activă = restart”, overlap e non-obiectiv). O notez pentru că smoke-urile și testele vechi care numărau suprapuneri se schimbă.

### 9. Exemplul din §4 al planului nu respecta două reguli existente de Motion; l-am corectat, nu am schimbat regulile

- **Exemplu (cum era în plan):**
  ```
  @on MouseEnter
  {
      @play $self.timbre.Hover;
      @animate with Tween(300ms, EaseOut)
      {
          @to { $self.timbre.Hover.Volume = 0.8; $self.prism.Foreground.Opacity = 0.5; }
      }
  }
  ```
- **Problema:** există deja (din planul Timbre Motion) regula „o animație nu amestecă sunet cu efecte vizuale” — pentru că animația de sunet merge și când butonul e ascuns, iar cea vizuală se oprește. Și regula „un `@on` are o singură animație” (altfel ceri `@parallel`, care la rândul lui nu poate amesteca sunet cu vizual).
- **Ce am scris în loc (în corpus și în teste):**
  ```
  @on MouseEnter
  {
      @play $self.timbre.Hover;
      @animate with Tween(300ms, EaseOut) { @to { $self.timbre.Hover.Volume = 0.8; } }
  }
  @on MouseEnter { @animate with Tween(300ms, EaseOut) { @to { $self.prism.Foreground.Opacity = 0.5; } } }
  ```
  Același efect: la intrarea mouse-ului sunetul crește și stratul Prism se estompează.
- **De ce:** regulile sunt aprobate și au motiv clar; exemplul din plan era doar ilustrativ. Dacă vrei să poți scrie cele două într-o singură animație, e o schimbare separată (ar trebui să împartă animația în două pe dedesubt).

### 10. Ordinea etapelor 4–6: le-am implementat împreună, apoi le bifez pe rând

- **Exemplu concret:** testele Language (Etapa 4) rulează și generatorul de cod și compilează rezultatul. Deci testul pentru `@timbre $UiSounds;` trece abia după ce există și `GeneratedMarkup.AttachTimbre` (Etapa 5) și generatorul care îl scrie (Etapa 6).
- **Ce am făcut:** am scris Language, runtime și SourceGen, apoi verific gate-urile în ordinea 4 → 5 → 6. Nimic nu se bifează înainte să fie verificat.
- **De ce:** altfel aș fi scris cod provizoriu în generator doar ca să treacă Etapa 4, apoi l-aș fi aruncat.

### 11. `$owner.timbre.X` se verifică doar la runtime, nu la compilare

- **Exemplu:** într-un template de buton, `@on Click { @play $owner.timbre.Click; }`. La compilare nu știu sigur ce Aspect va avea butonul-proprietar (poate primi altul din C#), deci nu dau eroare dacă `Click` lipsește. La runtime, dacă proprietarul nu are sunetul `Click` → `InvalidOperationException` cu mesaj clar.
- **Pentru `$self` și `$Speaker`** verificarea e la compilare (eroare dacă sunetul nu există în Aspect-ul static), cum s-a aprobat.
- **De ce:** planul cere verificare statică doar pentru „elementul numit”; pentru `$owner` legătura e la runtime (decizia 2A din fundație).

### 12. Smoke-ul `timbre-markup`: scenariul „overlap” devine „restart” (același număr de scenarii)

- **Exemplu:** butonul MP3 din panoul smoke, apăsat de două ori repede. Înainte: două sunete suprapuse, ambele terminate. Acum (`@play $self.timbre.Mp3;`): primul e oprit, al doilea se termină. Scenariul se numește acum `click/restart` și verifică: 2 porniri, 1 anulare, 1 terminare.
- **De ce:** urmează regula „`@play` repornește” (decizia 8); planul cere același număr de scenarii smoke.

### 13. `@timbre` pe panoul de transport din smoke: un singur clip inline cu două sunete

- **Exemplu:** Border-ul `Transport` avea două handle-uri (`Music`, `Looper`) cu clipuri diferite. Un Aspect are un singur `@timbre`, deci acum are `@timbre { @sound Music { … } @sound Looper { … } }`, iar checkbox-urile folosesc `@play/@pause/@seek/@stop $self.timbre.Music` și `$self.timbre.Looper`.
- **De ce:** e forma aprobată („un `@timbre` pe Aspect, cu mai multe `@sound`”).

### 14. Am reparat în generator Aspect-ul inline pe un control cu prefix (`<local:X.Aspect>`)

- **Exemplu:** `Playground/Cerneala.Playground/DrawingApiShowcaseView.crn` avea `<local:DrawingApiShowcase> @prism $DrawingApiPrism; </local:DrawingApiShowcase>`. Mutat în Aspect devine `<local:DrawingApiShowcase.Aspect>@prism $DrawingApiPrism;</local:DrawingApiShowcase.Aspect>`, pe care generatorul îl respingea („Markup property 'DrawingApiShowcase.Aspect' is not supported”), deși Language îl accepta.
- **Ce am făcut:** generatorul rezolvă acum tipul elementului cu prefix (prin `xmlns:local="clr-namespace:…"`) înainte să citească Aspect-ul inline.
- **De ce:** era o limitare veche, scoasă la iveală de mutarea `@prism` în Aspect; fără ea Playground nu compila.

### 15. LanguageServer fără proiect: pentru `@timbre`/`@prism` din Aspect verific doar forma, nu referințele

- **Exemplu:** deschizi în VS Code un `View.crn` care nu face parte din niciun proiect și scrii `<Button.Aspect>@timbre $Tone</Button.Aspect>` (fără `;`). Primești eroarea `CERNEALAUI030 Timbre directive '@timbre' must end with ';'.`, pe același interval ca într-un proiect. La fel pentru `@timbre { @sound … }` greșit (de exemplu `Volume = 2;` → eroare de valoare) și pentru `@prism` scris greșit.
- **Ce nu primești fără proiect:** `@timbre $Lipsa;` (resursă inexistentă) sau `@timbre` în `@on`. Pentru acestea trebuie modelul semantic, care există doar într-un proiect. Așa era și înainte pentru Motion din Aspect.
- **De ce:** înainte, fișierul fără proiect trimitea tot corpul Aspect-ului parserului Motion, care ar fi dat „`@prism`/`@sound` necunoscut” pentru markup corect. Acum `@timbre` și `@prism` sunt scoase din text înainte de parsarea Motion, exact ca în proiect.

### 16. PreviewHost: AutoPlay verificat cu același test ca `@play`

- **Exemplu:** preview-ul unui document cu `<TimbreClip Name="PreviewTone">@sound Tone { Source = "…"; AutoPlay = true; }</TimbreClip>` și `<UserControl.Aspect>@timbre $PreviewTone;</UserControl.Aspect>` (fără nicio comandă). Primul preview încearcă o dată să pornească sunetul (blocat, audio e oprit implicit); după recompilare, preview-ul vechi e retras (sunetul lui oprit), iar cel nou pornește din nou o singură dată.
- **Ce am făcut:** testul existent `PreviewAudioIsDisabledByDefaultAndARecompiledSessionRetiresTheOldScopes` rulează acum de două ori: o dată cu `@when IsEnabled { @play $self.timbre.Tone; }`, o dată cu `AutoPlay = true` fără comenzi.
- **De ce:** planul cere explicit „AutoPlay în preview-ul nou”; testul de recompilare deja verifica restul.
