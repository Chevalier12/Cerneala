# Plan: Presence si Layout Motion din Aspect markup

> Data: 2026-07-15
> Status: finalizat
> Dependenta: `docs/plans/2026-07-15-motion-markup-foundation.md`
> Scop: configuram coordinatorii existenti prin Aspect, cu timing si lifecycle identice API-ului runtime actual.

## 1. Baseline si defecte relevante

`PresenceOptions.FadeAndScale` accepta specs float pentru enter/exit si endpoint-uri fixe. `LayoutMotionOptions.Spring` accepta un `MotionSpec<Transform>` pentru correction. Presence trebuie setat inainte de attach.

Auditul a identificat un risc runtime real: `PresenceCoordinator.MarkAttached` creeaza subscriptions pentru opacity/scale fara sa le retina pentru disposal. Planul nu are voie sa acopere asta prin generated cleanup imposibil; runtime-ul trebuie reparat RED/GREEN inainte de markup Presence.

## 2. Etape de implementare

### Etapa 0 - RED pentru lifecycle runtime

- [x] Adauga test RED in `PresenceCoordinatorTests` pentru attach/detach/reattach repetat si demonstreaza ca subscriptions/graph values de enter nu se acumuleaza.
- [x] Repara `PresenceCoordinator` astfel incat enter handles si subscriptions sa aiba owner, sa fie anulate/eliberate la detach, replacement si exit handoff.
- [x] Verifica re-add in timpul exit si coexistenta cu layout correction.
- [x] Actualizeaza API docs daca se schimba vreun membru public si reindexeaza solutia. (Nu a fost necesar: API-ul public a ramas neschimbat.)

**Gate etapa 0**

- [x] Presence runtime este stabil fara markup dupa stress attach/detach.

### Etapa 1 - `@presence`

- [x] Extinde Aspect AST cu exact o declaratie `@presence` si fields `enter`, `exit`, `excludeInputWhileExiting`.
- [x] Specializeaza enter/exit la `MotionSpec<float>` si mapeaza exclusiv la `PresenceOptions.FadeAndScale`.
- [x] Emite assignment-ul Presence inainte ca elementul sa intre in retained tree.
- [x] Respinge custom endpoints, custom bodies, initial mode si Presence aplicat retroactiv unui element deja attached.
- [x] Adauga teste sourcegen si runtime pentru enter, exit, input exclusion, removal o singura data si reduced motion.
- [x] Reindexeaza solutia.

**Gate etapa 1**

- [x] Markup Presence produce acelasi state machine ca API-ul runtime si nu detine o a doua copie a lifecycle-ului.

### Etapa 2 - `@layout`

- [x] Parseaza `@layout id expression with spec` ca declaratie Aspect-owned unica.
- [x] Rezolva ID-ul prin grammar-ul reactiv existent si specializeaza spec-ul la `MotionSpec<Transform>`.
- [x] Emite `LayoutMotionId` si `LayoutMotionOptions` inainte de layout/attach, folosind coordinatorul existent pentru snapshots si correction.
- [x] Adauga teste pentru layout rect change, mid-flight retarget, reparent cu acelasi element si detach cleanup.
- [x] Adauga idle-frame assertions: tick-urile correction nu enqueuie measure/arrange.
- [x] Respinge position/size modes, crossfade, shared element intre controale distincte si custom layout sequences.
- [x] Reindexeaza solutia.

**Gate etapa 2**

- [x] Layout markup produce numai render correction si revine la identity fara layout storm.

## 3. Verificare si definitia de gata

- [x] Ruleaza testele Presence/Layout targetate si sourcegen Motion.
- [x] Ruleaza stress attach/detach/reparent cu diagnostics counters.
- [x] Ruleaza `dotnet test .\Cerneala.slnx`, `git diff --check` si reindexarea finala.
- [x] Presence si Layout sunt declarative, coordinator-owned si fara extensiile crazy excluse de proposal.
