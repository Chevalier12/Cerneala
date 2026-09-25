# Stage 0 — baseline-ul worktree-ului înainte de implementare

> Captură: 2026-09-24, Windows / PowerShell, `C:\Users\lauri\Desktop\Cerneala`
> HEAD: `443f100b86f8c4ca5f5e649651ac37c9cd5a330f` (`master`)
> Rol: dovadă de proveniență/limită. Nu transferă ownership-ul fișierelor deja dirty către acest plan.

## Comenzi read-only și ordinea observată

1. Înaintea rerulării obligatorii a generatorului pentru acest task: `git status --short --untracked-files=all`. Rezultatul exact este mai jos, **fără** fișierul-propunere nou.
2. `.\Tools\scripts\New-FileTree.ps1` a raportat `Unchanged C:\Users\lauri\Desktop\Cerneala\FileTree.md`. `FileTree.md` era deja `M` în rezultatul de la pasul 1. Acest pas nu dovedește cine a introdus diff-ul mai vechi.
3. Agenții read-only au rulat `git rev-parse HEAD`, `git branch --show-current`, `git diff --numstat`, `git status --short --untracked-files=all` și SHA-256 pentru fișierele dirty. Nu au editat, construit sau rulat teste.

```text
 M .codex/config.toml
 M .codex/skills/cerneala-checklist-plan/SKILL.md
 M FileTree.md
 M UI/Controls/TileMap2D.cs
 M docs-site/documentation/classes/Cerneala.Scene2D.Importers.LdtkScene2DImporter.md
 M docs-site/documentation/classes/Cerneala.Scene2D.Importers.TiledScene2DImporter.md
 M docs-site/documentation/classes/Cerneala.UI.Controls.TileMap2D.md
 M docs-site/documentation/classes/Cerneala.UI.Controls.TileMap2DModel.md
?? docs/plans/2026-09-23-scene2d-simple-collections-and-internal-spatial.md
?? tests/Cerneala.Tests/Controls/TileMap2DFactoryTests.cs
```

| Fișier tracked deja dirty | `git diff --numstat` | SHA-256 la inventar |
| --- | ---: | --- |
| `.codex/config.toml` | `+29/-4` | `D4EDB0B9D5FE4B7F7323027A670FFF14CD06AF22617A8B21F9E184638C73D712` |
| `.codex/skills/cerneala-checklist-plan/SKILL.md` | `+3/-3` | `D0DC6C1C9DEA0AF888A8BF29ED7BDD47DD39F2938D953160970A1B25101B9088` |
| `FileTree.md` | `+59/-56` | `54A76DAB31592A6E4CEC219EC9A85F4B7BF8F90285F615C4DBE658E713C71A65` |
| `UI/Controls/TileMap2D.cs` | `+7/-0` | `D2F8DF60E51E9C4ACEC93D2F6BE110F8E0FA6CAF4389F0525DBD1C7A4B790BE8` |
| `docs-site/documentation/classes/Cerneala.Scene2D.Importers.LdtkScene2DImporter.md` | `+1/-1` | `A7DDE3A30150B5209AAA421FFD6F26CDB59D343E2BB7705B9244B8EDA3681962` |
| `docs-site/documentation/classes/Cerneala.Scene2D.Importers.TiledScene2DImporter.md` | `+1/-1` | `4B3739D2CD6FAB19E2DED1B499E5F4CEDBFE17B6690CAF3DF56C80855D8123D3` |
| `docs-site/documentation/classes/Cerneala.UI.Controls.TileMap2D.md` | `+5/-8` | `A5EF6EEA88D9529B6987553242BB42FD565332F89D29C9A5DA3DD2BBA54011F6` |
| `docs-site/documentation/classes/Cerneala.UI.Controls.TileMap2DModel.md` | `+3/-3` | `49EEAAA9BBE8F3C7361A4EE8D1355B13EBB3F9EB095BA405B419882C57916360` |

Untracked deja prezent la prima captură: planul sursă SHA-256 `B6CBF612E8EF690BC67454910179131AD5A6E793615632EFA116FA4A0598556D`; `tests/Cerneala.Tests/Controls/TileMap2DFactoryTests.cs` SHA-256 `E6C9B6F979FEB422DF3F09CF6891F611BBF0A49BCBC4E17F6A33C4E88634B8A2`.

## Proveniență și reguli de conservare

- Planul sursă (`docs/plans/2026-09-23-scene2d-simple-collections-and-internal-spatial.md:30,78`) identifică `TileMap2D.FromModel`, documentația lui și noul test drept modificări existente ale utilizatorului. Diff-ul actual confirmă metoda la `UI/Controls/TileMap2D.cs:37-42`; testul este untracked. Ele se păstrează, inclusiv dimensiunile imaginilor și ordinea modelului.
- `.codex/config.toml` și skill-ul checklist erau dirty înainte de acest Stage 0; sunt în afara scope-ului implementării Scene2D și nu se ating.
- `FileTree.md` era dirty înainte de rerularea generatorului în acest task, iar generatorul a raportat `Unchanged`. Nu este posibil să atribuim cu certitudine diff-ul său unei persoane din această captură; nu se resetează.
- O rerulare ulterioară, după crearea artefactelor etapei 0, a raportat `Wrote C:\Users\lauri\Desktop\Cerneala\FileTree.md`; SHA-256-ul curent este `1F9A9777CD9DB6FA11C7A749FAA9BDE9680998DC6B8DCE901860DBC20160CA1A`. Această schimbare incrementală a arborelui este produsă de task, distinctă de diff-ul deja existent la prima captură. SHA-256-ul anterior din tabel rămâne dovada baseline-ului inițial, nu hash-ul actual.
- Planul sursă era untracked înainte de companionul nou. Companionul și artefactele de evidență sunt singurele fișiere noi produse de task-ul de contract până la checkpoint; statusul lor trebuie separat de baseline-ul utilizatorului.
- Nu s-a rulat niciun test/build/harness în Stage 0. Baseline-ul este de fișiere și sursă, nu un rezultat GREEN.
