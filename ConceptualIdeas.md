# Conceptual Ideas

Directie posibila pentru Cerneala: un UI framework retained-mode pentru MonoGame, cu invalidation, debugging si scheduling inspirate mai mult din game engines decat din framework-uri desktop clasice.

## 1. UI ca graf de dependente

In loc ca totul sa fie modelat doar ca arbore visual/logical, layout-ul, input-ul, render-ul, resursele si state-ul pot fi vazute ca un graf incremental explicit.

Fiecare nod ar putea declara ce consuma si ce produce:

- `Measure` depinde de font metrics, continut, constraints si style.
- `Arrange` depinde de rezultatul de measure si de layout parent.
- `Render` depinde de style, geometry, text shaping si assets.
- `HitTest` depinde de bounds, visibility, enabled state si clipping.

Invalidarea ar deveni propagare prin graf, nu doar flags propagate in sus si in jos prin arbore.

## 2. Frame scheduler cu buget real

Pentru MonoGame, scheduler-ul poate fi o diferenta majora. In loc sa proceseze mereu toata munca, poate decide ce incape in bugetul frame-ului.

Exemple:

- Layout critic procesat imediat.
- Text rasterization amanat cand frame-ul e aglomerat.
- Image decode facut in background.
- Hit-test cache refacut partial.
- Render cache degradat temporar si reconstruit ulterior.

Scopul ar fi un UI care ramane responsiv chiar si cand are multa munca de facut.

## 3. Declarative retained UI peste game loop immediate

Cerneala poate combina retained UI pentru meniuri, toolbars, inspectoare si editori cu predictibilitatea unui game loop.

Ideea centrala:

- Structura UI este retained.
- Starea poate fi declarativa.
- Input-ul si rendering-ul raman procesate explicit per frame.
- Integrarea cu gameplay/editor runtime ramane directa, fara strat magic greu de controlat.

## 4. Diagnostic-first UI

`InvalidationTrace` poate deveni o trasatura principala, nu doar un helper de debugging.

Framework-ul ar putea raspunde direct la intrebari precum:

- De ce s-a relayout-uit elementul acesta?
- Ce proprietate a provocat render?
- Ce handler a consumat input-ul?
- Ce frame a depasit bugetul?
- Ce cache a fost invalidat inutil?

Un UI framework unde debugging-ul este first-class ar avea identitate puternica, mai ales pentru tooling si jocuri.

## 5. Layout bazat pe constraints si prioritati

Pe langa modelul clasic `Measure` / `Arrange`, Cerneala ar putea avea primitive de layout bazate pe relatii.

Exemple:

- Aliniere dupa baseline.
- Pastrare aspect ratio.
- Pin to safe area.
- Size dupa continut, dar limitat de viewport.
- Distributie in functie de prioritati.
- Relatii intre elemente care nu sunt neaparat parent-child directe.

Nu trebuie un solver complet de la inceput. Important este sa existe loc pentru layout relational acolo unde `Measure` / `Arrange` devine greoi.

## 6. Input unificat ca timeline

Input-ul poate fi tratat ca stream/timeline, nu doar ca evenimente izolate.

Surse posibile:

- Mouse.
- Keyboard.
- Text composition.
- Touch.
- Stylus.
- Gamepad.
- Focus transitions.
- Gestures.

Pentru MonoGame, suportul coerent pentru mouse, tastatura, touch si gamepad ar fi un avantaj real fata de framework-urile desktop clasice.

## 7. Styles ca date compilabile

In loc de styling foarte dinamic si greu de urmarit, styles pot fi date validate si eventual compilate.

Avantajul important: framework-ul poate sti dinainte efectele unei schimbari.

Exemple:

- `Background` este render-only.
- `FontSize` afecteaza measure, arrange si render.
- `IsEnabled` afecteaza hit-test si visual input state.

Asta s-ar potrivi bine cu sistemul existent de `UiPropertyOptions`.

## 8. Render cache explicit pe elemente

Elementele pot declara explicit cum participa la caching.

Exemple de metadata:

- Cacheable.
- Volatile.
- Depends on transform.
- Depends on clipping.
- Can be atlased.
- Text cache key.
- Partial redraw region.

Scopul este sa nu fie nevoie de redraw complet cand doar o bucata mica din UI s-a schimbat.

## Pozitionare

Cerneala nu trebuie sa fie doar o clona mai mica de WPF sau Avalonia. O pozitionare mai interesanta:

**Un UI framework retained-mode pentru MonoGame, cu invalidation, diagnostics si scheduling de game engine.**

Aceasta directie pastreaza ideile bune din framework-urile desktop, dar le adapteaza pentru runtime-uri interactive unde predictibilitatea frame-ului, debugging-ul si controlul explicit conteaza mai mult.
