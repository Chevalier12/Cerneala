# Plan: Scroll timelines, Drag si Gesture din Aspect markup

> Data: 2026-07-15
> Status: finalizat
> Dependenta: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Scop: legam input-ul semantic si scroll progress la Motion fara `$event`, polling inutil sau subscription leaks.

## 1. Baseline si defecte relevante

`ScrollTimeline` produce vertical/horizontal normalized progress, dar cere `Update()`. `ScrollMotionBinding<T>` se aboneaza la progress in constructor si pastreaza listeners fara un contract de unbind/dispose. `DragMotionController` pastreaza doua subscriptions, dar nu implementeaza disposal. Acestea sunt bug-uri reale de lifecycle care blocheaza wiring-ul markup repetabil.

## 2. Etape de implementare

### Etapa 0 - RED si reparatii runtime

- [x] Adauga teste RED pentru disposal `ScrollMotionBinding`: dupa unbind/detach, progress nu mai scrie proprietatea si listener count nu creste la reattach.
- [x] Introdu un contract idempotent de unbind/dispose pentru scroll binding si pastreaza subscription-ul la progress pentru eliberare.
- [x] Adauga teste RED pentru `DragMotionController` disposal si reattach; confirma ca vechile `DragX/DragY` nu mai scriu elementul.
- [x] Repara controllerul Drag sa detina si sa elibereze subscriptions si active handles fara sa schimbe semantica Begin/Move/End.
- [x] Verifica daca `ScrollTimeline` necesita disposal pentru graph values/event wiring si implementeaza ownership-ul minim real. (Nu necesita disposal propriu: valorile idle nu sunt graph work; bindings detin si elibereaza singurul event wiring.)
- [x] Actualizeaza toate paginile API public afectate si manifestul daca este cazul. (Manifestul nu necesita schimbare: nu au fost adaugate sau redenumite pagini.)
- [x] Reindexeaza solutia.

**Gate etapa 0**

- [x] API-urile runtime pot fi create/distruse de 100 de ori fara listeners sau graph work rezidual.

### Etapa 1 - `@scroll`

- [x] Parseaza source, axis si assignments float range in declaratia `@scroll`.
- [x] Rezolva source la un `ScrollViewer` attached si targets la proprietati `float` animabile.
- [x] Genereaza o singura timeline per declaratie/sesiune, o actualizare initiala si updates din evenimentul ScrollChanged relevant; fara polling per frame cand offsetul nu se schimba.
- [x] Mapeaza ranges exclusiv prin `ScrollTimelineProgress.Map(from,to)` si `AllowLayout()` numai cand `allowLayout=true` este explicit.
- [x] Elibereaza event subscription, bindings si timeline la detach.
- [x] Respinge pixel ranges, easing, input subranges, keyframe scroll si non-float targets.
- [x] Adauga teste pentru vertical/horizontal, clamp, zero extent, layout opt-in si detach.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Scroll render-only nu produce measure/arrange si nu lasa frame requester activ cand scroll-ul este idle.

### Etapa 2 - `@drag`

- [x] Parseaza restricted `@drag with spec` fara event variables sau options inexistente.
- [x] Genereaza routed pointer subscriptions pentru begin/move/end/capture-lost si traduce intern args in apelurile controllerului.
- [x] Creeaza controllerul numai dupa attach si il dispose-uieste la detach; capture state nu supravietuieste sesiunii.
- [x] Mapeaza exact ambele translation axes, velocity projection fixa si settle/capture-lost behavior din runtime.
- [x] Respinge axis, bounds, resistance, snapping, separate source/target si Decay release.
- [x] Adauga teste cu input routed real, capture loss, detach mid-drag si reattach.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Un singur pointer event produce un singur update indiferent de numarul ciclurilor attach/detach anterioare.

### Etapa 3 - `@gesture press`

- [x] Parseaza exclusiv `@gesture press with spec`.
- [x] Genereaza pressed/released/capture-lost wiring catre `GestureMotionController`, fara `$event` in limbaj.
- [x] Pastreaza endpoint-urile runtime 0.97 si 1 si respinge pinch/rotate/custom scale endpoints.
- [x] Adauga teste pentru press/release, rapid retarget, detach pressed si reduced motion.
- [x] Reindexeaza solutia.

**Gate etapa 3**

- [x] Gesture markup este doar adaptor peste controllerul semantic existent, nu un al doilea gesture recognizer.

## 3. Verificare si definitia de gata

- [x] Ruleaza testele Motion Input, ScrollViewer si sourcegen Motion targetate.
- [x] Ruleaza stress click/drag/scroll + attach/detach si verifica memorie stabilizata si listener counts. (100 de cicluri per adaptor; handlers raman unici si graph work revine la zero.)
- [x] Ruleaza `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.
- [x] Scroll, Drag si Gesture functioneaza fara polling inutil, `$event` sau leaks.
