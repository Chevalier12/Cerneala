## 1. Roadmap Reconciliation

- [x] 1.1 Update `ROADMAPv2.md` section 1 to reference canonical OpenSpec spec paths that exist today.
- [x] 1.2 Remove or replace stale legacy section 1 spec names without creating duplicate spec folders.
- [x] 1.3 Mark section 1 canonical spec entries complete when their files exist.

## 2. Architecture Boundary Tests

- [x] 2.1 Add `tests/Cerneala.Tests/Architecture/RepositoryShapeTests.cs`.
- [x] 2.2 Add `tests/Cerneala.Tests/Architecture/NamespaceBoundaryTests.cs`.
- [x] 2.3 Prove repository shape tests validate section 1 planning files and canonical specs.
- [x] 2.4 Prove namespace boundary tests reject backend-specific dependencies outside adapter, test, and playground boundaries.

## 3. Verification

- [x] 3.1 Run architecture tests.
- [x] 3.2 Run full test suite.
- [x] 3.3 Run `openspec validate complete-architecture-contracts-memory --strict`.
- [x] 3.4 Run `openspec validate --all --strict`.
