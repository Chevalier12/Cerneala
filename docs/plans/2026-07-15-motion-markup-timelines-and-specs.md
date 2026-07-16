# Plan: keyframes, repeat, ping-pong, stagger si specs avansate

> Data: 2026-07-15
> Status: finalizat
> Dependenta: `docs/plans/2026-07-15-motion-markup-foundation.md`, `docs/plans/2026-07-15-motion-markup-composition-and-clips.md`
> Scop: expunem numai timeline/spec semantics sustinute real de runtime si respingem combinatiile care ar minti utilizatorul.

## 1. Contract

`KeyframesSpec<T>` cere frames la offset 0 si 1 si avanseaza automat pe o durata finita. Nu are seek, reverse sau progress extern. Repeat/PingPong infasoara numai `TweenSpec<T>`. `MotionStagger` calculeaza exclusiv `offset * index`.

Decay ramane o problema explicita: `DecaySpec<T>` foloseste velocity si ignora destinatia `to` primita de sampler. Nu se va introduce o forma `@animate` care afiseaza un `@to` mincinos. Contractul declarativ Decay trebuie inchis printr-o decizie documentata si teste inainte de emitere.

**Decizie Decay:** markup-ul Decay ramane deferred. Nu acceptam nici resource `Decay`, nici constructor inline in aceasta verticala, deoarece toate execution-urile de proprietate disponibile cer `@to`, iar `DecaySpec<T>` ignora acel endpoint. O sintaxa viitoare trebuie sa porneasca explicit de la valoarea vizuala curenta si o velocity tipizata, fara `@to`, dupa ce runtime-ul ofera un execution contract dedicat.

## 2. Etape de implementare

### Etapa 0 - Caracterizare runtime si diagnostics RED

- [x] Adauga/completeaza teste runtime pentru boundaries Keyframes, gap retention, duplicate offsets, Hold, StepEasing si completion value.
- [x] Pastreaza testele Repeat/PingPong pentru cycle count si final value par/impar ca plasa de siguranta.
- [x] Adauga teste sourcegen RED pentru ranges invalide, overlap pe aceeasi proprietate, overlap legal pe proprietati diferite, Spring/Decay in keyframes si nested groups ilegale.
- [x] Adauga teste RED pentru restrictiile Repeat/PingPong la Tween si Stagger la un singur Tween `@animate`.
- [x] Scrie in acest plan, inainte de implementarea Decay markup, forma acceptata si motivul pentru care nu cere un endpoint ignorat; daca decizia nu exista, mentine Decay markup deferred. (Decay markup ramane deferred: runtime-ul nu are execution fara endpoint.)
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] Fiecare constructie poate fi coborata la un API runtime real fara camp decorativ sau comportament inventat.

### Etapa 1 - Keyframes timeline

- [x] Parseaza `@keyframes duration` cu copii exclusiv ranged `@animate start%..end%`.
- [x] Grupeaza segmentele per target property si construieste un singur `KeyframesSpec<T>` per proprietate.
- [x] Insereaza frames sintetice la 0/1 si la marginile gap-urilor astfel incat runtime-ul sa retina ultima valoare exact cum spune proposal-ul.
- [x] Respinge ranges goale, inversate, in afara intervalului si overlaps pe aceeasi target property; permite boundary comun.
- [x] Permite Tween easing si `Step(...)`; respinge Spring, Decay, Repeat si PingPong in ranged children. (`Step(...)` este inchis complet in etapa 2.)
- [x] Emite timeline-ul ca execution body compatibil cu composition si MotionClip.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Timeline-urile din proposal au values exacte la 0%, boundaries, gaps si 100% sub `ManualMotionTimeline`/clock de test.

### Etapa 2 - Hold si steps

- [x] Mapeaza `hold` la `MotionKeyframe<T>.Hold` pe segmentul corect, fara sa il confunde cu `holdOnComplete`.
- [x] Mapeaza `Step(count, JumpStart|JumpEnd|JumpBoth|JumpNone)` la `StepEasing` si valideaza count/options la build.
- [x] Adauga teste care diferentiaza sampling hold de persistence dupa completion.
- [x] Adauga diagnostics pentru steps/hold in afara keyframes.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] `hold` schimba numai sampling-ul segmentului; `holdOnComplete` ramane singurul control al value-source persistence.

### Etapa 3 - Repeat si PingPong

- [x] Parseaza `Repeat(Tween(...), count|forever)` si `PingPong(Tween(...), cycles)` ca spec constructors, nu execution nodes.
- [x] Specializeaza wrappers la `TweenSpec<T>` si respinge Spring/Decay/clip/group arguments.
- [x] Valideaza count pozitiv si PingPong finit; documenteaza reduced-motion pentru repeat forever.
- [x] Adauga teste generated-code + runtime pentru odd/even completion si cancellation infinite.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Even PingPong termina la `@from`, odd la `@to`, inclusiv prin property binding.

### Etapa 4 - Stagger

- [x] Parseaza restricted `@stagger target ... each ...` cu exact un Tween-based `@animate`.
- [x] Rezolva colectia si item type static, face snapshot la execution start si aplica `WithDelay(offset * index)`.
- [x] Respinge reverse/center ordering, Spring, arbitrary sequence si mutation-driven rescheduling.
- [x] Adauga teste pentru empty collection, snapshot mutation, cancellation si cleanup.
- [x] Reindexeaza solutia.

**Gate etapa 4**

- [x] Stagger nu introduce scheduler paralel si nu enumera colectia pe fiecare frame.

### Etapa 5 - Spec options si Decay gate

- [x] Parseaza Tween `Delay`, `FillMode` si Spring `RestSpeed`, `RestDelta`, `VelocityMode`, pastrand nota ca property retarget nu foloseste astazi sampler retarget pentru velocity preservation.
- [x] Valideaza Decay `ValueType`, typed `InitialVelocity`, `Deceleration`, bounds pereche, comparability si Bounce spec type. (Neaplicabil dupa Gate 0: forma intreaga este respinsa inainte de validarea optiunilor, nu partial acceptata.)
- [x] Implementeaza executia Decay numai daca Gate etapa 0 a stabilit o sintaxa fara `@to` fals; altfel documenteaza declaratia/executia ca deferred si nu accepta resource inutil in markup.
- [x] Actualizeaza proposal-ul daca decizia Decay schimba grammar-ul, in acelasi change cu tests.
- [x] Reindexeaza solutia.

**Gate etapa 5**

- [x] Nicio optiune acceptata de parser nu este ignorata silentios de codul generat.

## 3. Verificare si definitia de gata

- [x] Ruleaza testele Specs/Core Motion si sourcegen Motion.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.
- [x] Keyframes, Hold, Step, Repeat, PingPong si Stagger au semantics deterministe demonstrate cu clock manual.
- [x] Seek/reverse/scrubbing si combinatiile nesustinute primesc diagnostics, nu pseudo-suport.
