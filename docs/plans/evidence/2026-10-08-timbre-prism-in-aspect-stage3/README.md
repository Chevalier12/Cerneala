# Etapa 3 — core: `TimbreClipDefinition`

Plan: [2026-10-08-timbre-prism-in-aspect.md](../../2026-10-08-timbre-prism-in-aspect.md), Etapa 3.

- `Timbre/TimbreClipSound.cs`, `Timbre/TimbreClipDefinition.cs`: semnăturile fixate în Etapa 0. `TimbreParameter.IsIdentifier` devine `internal` (aceeași regulă de identificator pentru numele sunetelor).
- docs-site: `Cerneala.Timbre.TimbreClipDefinition.md`, `Cerneala.Timbre.TimbreClipSound.md`, intrări noi în `manifest.json`.
- Teste: `tests/Cerneala.Tests.Timbre/Definitions/TimbreClipDefinitionTests.cs` (ordinea declarării, căutare după nume, copiere, validarea numelui sunetului, sunete lipsă/duplicate/null, parametri duplicați/null, parametru al unui sunet nedeclarat de clip, același parametru în două sunete). RED: proiectul de test nu compila fără tipuri.
- Verificare: `dotnet test tests/Cerneala.Tests.Timbre -c Release --filter "TimbreClipDefinitionTests|TimbreArchitectureTests|TimbreDefinitionTests"` — 17 passed; `Cerneala.Tests.VisualStudio` (manifest) 48 passed.
