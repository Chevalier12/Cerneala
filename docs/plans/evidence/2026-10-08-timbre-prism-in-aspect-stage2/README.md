# Etapa 2 — `TimbreClip` → `TimbreSound` în C#

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 2. Baseline: `59c0e257`.

## Schimbare

- Tipul public `Cerneala.Timbre.TimbreClip` → `TimbreSound` (`git mv Timbre/TimbreClip.cs Timbre/TimbreSound.cs`), fără schimbare de semantică (aceiași constructori, parametri, modificatori, loading).
- Identificatori C# legați de tip, pentru a nu lăsa nume cu dublu sens: proprietatea `TimbrePlayback.Clip` → `Sound`; parametrii publici `clip` → `sound` în `TimbreScope.Play`, `TimbreRuntime.PrepareAsync`, `GeneratedMarkup.PlayTimbre`, `GeneratedMarkup.GetTimbreParameter`; identificatorul `clip` → `sound` în codul core Timbre (`Timbre/**`, `UI/Markup/GeneratedMarkupTimbre.cs`, `UI/Timbre/TimbrePlaybackMotion.cs`). Mesajele de excepție și cuvintele de clipping audio (`clipped`, `clippedSamples`) neschimbate.
- Markup-ul `<TimbreClip>` neschimbat: string-urile cu numele elementului în Language/SourceGen/LanguageServer, mesajele de diagnostic, tipurile interne de markup (`BoundTimbreClip`, `ResourceKind.TimbreClip`, …), corpus, `.crn`, gramatica VS. Doar numele complet al tipului emis/legat s-a schimbat (`TimbreClipTypeName`, `TimbreClipType` → `Cerneala.Timbre.TimbreSound`).
- Docs: pagina `Cerneala.Timbre.TimbreClip.md` → `Cerneala.Timbre.TimbreSound.md` (+ manifest), referințele din paginile Timbre/`Application`/`GeneratedMarkup`/`MarkupConditionRule`, `TimbrePlayback.Sound`, numele parametrilor; `docs/timbre-guide.md` doar mențiunile C# (tabelul modelului, exemplele C#, „C# `TimbreSound`”).

## Incident în timpul etapei

Scriptul de redenumire trata liniile din raw string-uri ca și cod și, din cauza tokenizării lui `/`, a transformat tag-urile de închidere markup `</TimbreClip>` în `</TimbreSound>` în testele Language, SourceGen, Timbre, LanguageServer, PreviewHost. Prima rulare a gate-ului a eșuat (Language 8, SourceGen 10, Timbre markup 10 — „Markup must contain exactly one UI root element”). Tag-urile au fost refăcute (zero `</TimbreSound>`; numărul de `</TimbreClip>` egal cu HEAD în fișierele verificate). Rezultatul verde inițial al LanguageServer cu markup stricat a fost invalidat și proiectul rerulat.

Două așteptări din `UiMarkupGeneratorTimbreTests` țineau de numele tipului: lista sortată a constructorilor (`TimbreSound` sortează după `TimbreParameter`) și `ContainingType.Name == "TimbreClip"` → `"TimbreSound"`.

PreviewHost a eșuat apoi cu `CS0234 'TimbreClip' does not exist in 'Cerneala.Timbre'`: MSBuildWorkspace-ul previewului evaluează configurația implicită Debug, iar artefactele Debug (`Cerneala.SourceGen` 10:41) erau dinaintea redenumirii; build-ul fusese doar Release. După `dotnet build Cerneala.slnx -c Debug`, PreviewHost 22/22 — cauză confirmată, de mediu.

## Verificare (starea finală)

- `dotnet build Cerneala.slnx -c Release -m:1` și `-c Debug`: 0 erori.
- Release, `--no-build`, TRX în acest director: `Cerneala.Tests.Timbre` 368; `Cerneala.Tests.SourceGen` 641; `Cerneala.Tests.Language` 371 / 1 skipped; `Cerneala.Tests.LanguageServer` 45; `Cerneala.Tests.PreviewHost` 22; `Cerneala.Tests` 4198 / 2 skipped; `Cerneala.Tests.SdlGpu` 601 / 241 skipped (nativ dezactivat); `Cerneala.Tests.VisualStudio` 48 (inclusiv manifest). `Cerneala.Tests`, `SdlGpu`, `VisualStudio` au rulat după ultima modificare de producție; schimbările ulterioare au fost doar în fișiere de test din alte proiecte.
- `git grep -w TimbreClip` în `.cs`: doar numele elementului markup (string-uri, tag-uri din markup de test, tipuri interne de markup).

## ApiCompat (strict, fără suppression-uri)

`../2026-10-08-timbre-prism-in-aspect-stage1/api-compat.proj`, log `api-compat-strict.log`: 26 de intrări = cele 14 redenumiri Prism din Etapa 1 + 12 perechi de redenumire Timbre:

- CP0001: `TimbreClip` → `TimbreSound`.
- CP0002 (eliminat/adăugat): `TimbrePlayback.Clip` → `TimbrePlayback.Sound`; `TimbreScope.Play(TimbreClip→TimbreSound, …)`; `TimbreRuntime.PrepareAsync(TimbreClip→TimbreSound, …)`; `GeneratedMarkup.GetTimbreParameter(TimbreClip→TimbreSound, string)`; `GeneratedMarkup.PlayTimbre(…, ResourceId<TimbreClip→TimbreSound>, Action<TimbreClip→TimbreSound, …>, …)`.

Nicio altă diferență.
