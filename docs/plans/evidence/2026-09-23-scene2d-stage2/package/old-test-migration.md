# Stage 2 package test migration ledger

This records why existing GREEN characterization assertions changed during the
approved public-API cutover. Package-project verification is recorded separately
in `verification.md`; this mapping is **not** a native or integrated GREEN report.
Baseline results and pre-cutover CPV2 hashes remain in the Stage 1 ledger. The selected contract is
`docs/plans/2026-09-24-scene2d-stage0-contract-proposal.md` §§3, 5, 7.

| Old test area | Stage 2 replacement expectation | Contract rationale |
| --- | --- | --- |
| `Scene2DPackageTests` prepared chunk headers and earlier null charge | Inspect the unchanged private CPV2 index in the package test assembly; public `TileMapIds` and complete `LoadMapModelAsync` must work. | Charge/entry/catalog types leave the public API, but the wire, old unknown charge and streaming headers remain internal. |
| Self-contained directory, map/level/document metadata, entity and promotion reads | Assert local declared file paths and decoded **direct values**; inspect private chunk payload for partial palette versus full authored metadata. | No package lease in the selected API; decoded values are caller-owned. |
| Corrupt/unrequested region, concurrent reads, cancellation and budgets | Read specific private package blocks by the package's internal byte path; preserve hash isolation, short-range concurrency, cancellation and limit checks. | The package still streams only requested CPV2 bytes. No public source/entry interface is reintroduced merely to test its private wire. |
| Package disposal and weak retention | A read admitted before `Dispose` may complete; new reads fail, `DisposeAsync` drains, returned values remain usable, and discarded decoded values are collectable. | Reader lifetime belongs to the package; value lifetime belongs to the caller. The old disposed-lease guard is deliberately removed. |
| Grid subdivision and imported corpus | Assert private prepared chunks, palettes, collision metadata, authored extents and byte equality; use `CreateTileMap` for package map construction and compare to in-memory prepared source internally. | Public catalog/`TileMaps` disappear, while CPV2 subdivision, exact payload and model behavior remain. |
| Grid collision geometry, movement and codec identity | Use public `TileMap2D.FromModel` and package `CreateTileMap`; codec identity test uses the internal `FromSource` fixture. Retain raycast, travel/normal and coalescing assertions. | The test seam may access internals; no removed spatial source becomes public again. |
| Entity header selection and adapter mutability | Preserve header identity/geometry and corrupt-payload detection; application selects header IDs and loads direct entities. Remove assertions that a package creates/mutates a custom runtime envelope. | Companion §7(2) explicitly removes `CreateEntitySource` custom pre-load selection and publication; no semantic replacement is claimed. |
| Entity lease retention and disposal | Assert direct entity values do not stay pinned after caller release and remain readable after package disposal; new reads fail. | Direct value ownership replaces `SceneSpatialLease2D<object>`. |
| Headless package entity streaming | Load entities into an application-owned ordinary collection, mark the realized NPC collider, retain owner-thread region preparation, movement, collision and node-identity checks. Do **not** assert viewport-only realization or missing-entry readiness after eagerly loading all entities. | Companion §§2, 4, 7(2),(4) explicitly replace custom pre-load envelopes with simple eager collection plus realized collider interest; lazy actors require explicit region management. This is an approved capability reduction, not preservation of old viewport-only residency. |
| Native package warm streaming | Construct the package map through public `CreateTileMap`; instrument its private source through the test friend and internal `FromSource`. Preserve warm budget, actual load/release counts, image cache retirement, collision readiness, weak payload retention and application-owned screenshot checks. | The warm/cache path stays framework-owned and testable without exporting it as a public source family. |

The package project is 103/103 GREEN after the fixture repairs recorded in
`verification.md`. Pending: integrated SDL compile, native execution, broader
suite/ApiCompat, and independent audit. The removed custom pre-load selection
assertions are an approved capability reduction, not a claim of equivalent
behavior from the new eager collection.
