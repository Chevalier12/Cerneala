# Developer Preview Checklist

- Motion system is root-owned and deterministic.
- Timing tests use manual clocks or manual timelines.
- Render-only animations do not enqueue measure/arrange.
- Layout motion uses FLIP correction, not layout spam.
- Presence exit keeps elements renderable until completion and excludes input.
- Scroll-linked motion avoids scroll feedback loops.
- Reduced motion preserves final target values.
- Diagnostics expose trace events and active graph snapshots.
- Legacy animation remains compatibility-only.

## Verification Commands

Targeted preview contracts:

```powershell
dotnet test Cerneala.slnx --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests|FullyQualifiedName~DeveloperPreviewScopeTests"
```

Full suite:

```powershell
dotnet test
dotnet test Cerneala.slnx
```

Archive:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```
