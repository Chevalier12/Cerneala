# Developer Preview Checklist

Use this checklist before publishing or archiving a Developer Preview build.

## Targeted Gates

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~RuntimePreviewContractTests"
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DeveloperPreviewScopeTests"
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~GettingStartedDocsTests|FullyQualifiedName~GettingStartedSampleContractTests"
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~AuthoringAppSampleContractTests|FullyQualifiedName~RuntimePreviewSampleContractTests|FullyQualifiedName~RuntimePreviewIntegrationTests|FullyQualifiedName~PlaygroundSampleTests"
```

## Full Test Suite

```powershell
dotnet test Cerneala.slnx
dotnet test
```

## Archive

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\scripts\Archive-Repo.ps1 -RepoRoot .
```

## Scope Check

The preview supports the retained code-first path: `UIRoot`, `UiHost`, default theme, typed binding, commands, `TextBox`, `Button`, `ListBox`, `Grid`, `StackPanel`, Tab focus navigation, no-work frames, and draw-purity. Markup, package split, full IME, and native accessibility completion remain deferred.
