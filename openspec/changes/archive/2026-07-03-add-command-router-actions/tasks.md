## 1. Command Binding Ownership

- [x] 1.1 Add `UI/Input/CommandBindingCollection.cs` with insertion-ordered storage, null rejection, enumeration, and command-specific dispatch helpers.
- [x] 1.2 Add `UIElement.CommandBindings` to `UI/Elements/UIElement.cs` and keep ownership lazy or lightweight.
- [x] 1.3 Add `tests/Cerneala.Tests/Input/CommandBindingCollectionTests.cs` for ordering, null rejection, and matching-command behavior.

## 2. Command Router Core

- [x] 2.1 Add `UI/Input/RoutedCommandContext.cs` with command, retained target, parameter, and `ElementInputRouteMap`.
- [x] 2.2 Add `UI/Input/CommandRouter.cs` with explicit `CanExecute` and `Execute` APIs over retained routes.
- [x] 2.3 Route `PreviewCanExecute`/`CanExecute` and `PreviewExecuted`/`Executed` with preview-handled suppression matching existing `RoutedEventRouter.RaisePair` behavior.
- [x] 2.4 Ensure command bindings on retained route elements can handle matching command args and ignore non-matching commands.
- [x] 2.5 Add `tests/Cerneala.Tests/Input/CommandRouterTests.cs` for route order, handled preview suppression, missing target behavior, and can-execute result propagation.

## 3. RoutedCommand and ActionCommand

- [x] 3.1 Update `UI/Input/RoutedCommand.cs` so direct execution fails with a clear router-required error while routed execution is covered by `CommandRouter`.
- [x] 3.2 Add `UI/Input/ActionCommand.cs` with execute delegate, optional can-execute delegate, argument validation, and default executable state.
- [x] 3.3 Add `tests/Cerneala.Tests/Input/ActionCommandTests.cs` for execution, can-execute false, default true, and constructor validation.
- [x] 3.4 Add `tests/Cerneala.Tests/Input/RoutedCommandExecutionTests.cs` for routed `CanExecute`/`Execute` behavior and direct-call failure.

## 4. ButtonBase Command Integration

- [x] 4.1 Add `ButtonBase.CommandProperty` and `ButtonBase.CommandParameterProperty` to `UI/Controls/Primitives/ButtonBase.cs`.
- [x] 4.2 Add explicit command state refresh behavior for `ButtonBase` that can update visual/input state without global requery.
- [x] 4.3 Connect retained click synthesis in `UI/Input/ElementInputBridge.cs` to button command execution after a matching click release.
- [x] 4.4 Ensure disabled, excluded, canceled, or non-matching click targets do not execute button commands.
- [x] 4.5 Add `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandTests.cs` for direct command execution, routed command execution, cannot-execute behavior, parameter forwarding, and canceled clicks.

## 5. Roadmap and Verification

- [x] 5.1 Update `ROADMAPv2.md` section 9 checkboxes for completed OpenSpec artifacts, implementation files, tests, and acceptance checklist items.
- [x] 5.2 Run `dotnet build Cerneala.slnx -warnaserror`.
- [x] 5.3 Run `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj`.
- [x] 5.4 Run focused command/input tests for `CommandRouter`, `CommandBindingCollection`, `ActionCommand`, routed command execution, `ButtonBase` command behavior, and retained input bridge behavior.
- [x] 5.5 Run `openspec validate add-command-router-actions --strict`.
- [x] 5.6 Run `openspec validate --all --strict`.
