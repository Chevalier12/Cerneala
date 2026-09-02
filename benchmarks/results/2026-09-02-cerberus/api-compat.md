# Cerberus API compatibility gate

Date: 2026-09-02
Baseline commit: `d009162ecb74164fcc7490b29de2d2588e9b0d3a`
SDK task: `C:\Program Files\dotnet\sdk\10.0.303\Sdks\Microsoft.NET.Sdk\tools\net10.0\Microsoft.DotNet.ApiCompat.Task.dll`

The baseline was built in a detached temporary worktree. The current dirty worktree was not checked out, reset, or cleaned.

## Compared assemblies

| Assembly | Baseline (left/contract) | Current (right/implementation) |
| --- | --- | --- |
| Core | `C:\Users\lauri\Desktop\Cerneala-baseline-cerberus-temp\bin\Release\net8.0\Cerneala.dll` | `C:\Users\lauri\Desktop\Cerneala\bin\Release\net8.0\Cerneala.dll` |
| SDL_GPU backend | `C:\Users\lauri\Desktop\Cerneala-baseline-cerberus-temp\Cerneala.Backends.SdlGpu\bin\Release\net8.0\Cerneala.Backends.SdlGpu.dll` | `C:\Users\lauri\Desktop\Cerneala\Cerneala.Backends.SdlGpu\bin\Release\net8.0\Cerneala.Backends.SdlGpu.dll` |

Reference resolution used the installed .NET 8 runtime directory, `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.30`, plus each worktree's `tests\Cerneala.Tests.SdlGpu\bin\Release\net8.0` output directory.

## MSBuild project

The exact project is preserved as [`api-compat.proj`](api-compat.proj). It invokes the SDK `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` twice with:

- `EnableStrictMode="true"`;
- `EnableRuleCannotChangeParameterName="true"`;
- `RespectInternals` left at its default `false`, so the gate covers public and protected API;
- baseline assemblies on the left and current assemblies on the right.

## Command and output

```powershell
dotnet msbuild .\benchmarks\results\2026-09-02-cerberus\api-compat.proj -t:Compare -v:minimal
Write-Host "API_COMPAT_EXIT=$LASTEXITCODE"
exit $LASTEXITCODE
```

```text
MSBuild version 18.6.14+e730f1db7 for .NET
API_COMPAT_EXIT=0
```

Exit code: `0`.

Result: the strict public/protected API diff is empty for both `Cerneala.dll` and `Cerneala.Backends.SdlGpu.dll`. Cerberus remains an internal backend implementation detail.
