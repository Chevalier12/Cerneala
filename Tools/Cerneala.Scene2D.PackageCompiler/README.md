# Scene package compiler

Prepare a new self-contained directory using the existing Tiled or LDtk importer:

```powershell
dotnet run --project .\Tools\Cerneala.Scene2D.PackageCompiler\Cerneala.Scene2D.PackageCompiler.csproj -c Release -- tiled <input-map> <asset-root> <new-output-directory>
```

Use `ldtk` instead of `tiled` for LDtk projects. The output parent directory must
already exist and the output directory must not exist. The command does not
delete, replace or merge an old package. Distribute the generated directory as a
unit; the runtime reader does not need the importers or this executable.

The compiler preserves importer diagnostics and passes the exact explicit file
dependency list to the common package writer. It does not infer dependencies
from text or recursively inspect referenced files. Atlas images are copied, not
decoded or re-encoded. An editor map change requires preparing a new package.

Exit codes: `0` success, `1` import/preparation failure, `2` invalid arguments,
`130` cancellation. Ctrl+C requests cancellation; the synchronous importer must
return before the cancellation can be observed by the asynchronous writer.

Canonical contracts:

- [Scene2DPackageWriter](../../docs-site/documentation/classes/Cerneala.Scene2D.Packages.Scene2DPackageWriter.md)
- [Scene2DPackage](../../docs-site/documentation/classes/Cerneala.Scene2D.Packages.Scene2DPackage.md)

This command is the preparation entry point, not an automatic migration of
arbitrary applications. The maintained Playground integrates it through
[SceneWorldPackages.targets](../../Playground/Cerneala.Playground/SceneWorldPackages.targets).
That application's build owns two generated directories under its dedicated
`obj/SceneWorldPackages` subtree; it copies the complete packages to build and
publish output. The compiler itself remains create-only. Applications must keep
their package open while its sources can request data, use the common
image-resource leases and supply their own gameplay/entity composition contracts.
