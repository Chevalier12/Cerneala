# Servo API compatibility baseline

Date: 2026-09-03
Baseline commit: `fed724b954bc2823c4799db69c94b92e2790b2b5`
Detached worktree: `C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b`

The baseline was created with `git worktree add --detach`; the dirty primary worktree was not checked out, reset, or cleaned.

## SDK

- .NET SDK: `10.0.303` (`e730f1db75`)
- MSBuild: `18.6.14+e730f1db7`
- Host: `10.0.11`, `win-x64`
- `global.json`: `C:\Users\lauri\Desktop\Cerneala\global.json`

## Reproducible baseline build

```powershell
git worktree add --detach C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b fed724b954bc2823c4799db69c94b92e2790b2b5
dotnet build .\Cerneala.csproj -c Release --no-restore
```

Result: exit code `0`, zero warnings, zero errors.

Baseline assembly: `C:\Users\lauri\Desktop\Cerneala-baseline-servo-fed724b\bin\Release\net8.0\Cerneala.dll`

- Size: `5,031,936` bytes
- SHA-256: `2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1`

An attempted `dotnet build .\Cerneala.slnx -c Release` produced the core assembly, then was terminated after the existing MonoGame `mgfxc` invocation for `CopyComposite.fx` stopped making progress for more than ten minutes. The API baseline uses the successful, narrower core project build above; no full-solution baseline result is claimed.

## Stage 5 strict comparison

The archived [`api-compat.proj`](api-compat.proj) invokes
`Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` with:

- `EnableStrictMode="true"`;
- `EnableRuleCannotChangeParameterName="true"`;
- `RespectInternals` left at its default `false`;
- the detached baseline assembly on the left and the current assembly on the right;
- reference directories from each worktree's SDL_GPU test output plus the installed .NET 8 runtime.

Current assembly: `C:\Users\lauri\Desktop\Cerneala\bin\Release\net8.0\Cerneala.dll`

- Size: `5,077,504` bytes
- SHA-256: `68EA2E581C8DD306607E7134160E7BA80FB872E0AA2123485E71FFAC06041945`

The exact final command is:

```powershell
dotnet msbuild .\benchmarks\results\2026-09-03-servo\api-compat.proj -t:Compare -v:minimal
```

[`api-compat.suppressions.xml`](api-compat.suppressions.xml) contains exactly 119
approved differences:

- 20 Servo-plan differences from section 6.5: the 12 public Servo type
  additions, the seven removed `Cerneala.UI.Automation` types, and
  `Window.CreateAutomationSession()` removal;
- 98 differences belonging to the concurrent Detective API migration, which
  the user explicitly approved on 2026-09-03. These cover the moves from
  `Cerneala.UI.Aspect`, `Cerneala.UI.Diagnostics`, and
  `Cerneala.UI.Motion.Diagnostics` into `Cerneala.UI.Detective`, plus the
  corresponding owner/member signature changes;
- the existing public
  `GeneratedMarkup.AttachMotionSession(UIElement, ElementAspect?)` overload,
  introduced by `e0bc2175 fix(aspect-motion-prism): repair lifecycle
  integration` and explicitly approved by the user on 2026-09-03.

The suppression file intentionally excludes every other difference.

The latest strict output is archived in
[`api-compat.current.txt`](api-compat.current.txt). Reference resolution is
complete. With exactly the three approved categories above, the command exits
`0`; there are no other public or protected differences.
