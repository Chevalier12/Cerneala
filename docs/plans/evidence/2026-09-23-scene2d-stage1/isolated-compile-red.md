# Stage 1 — consumer compile RED izolat pentru `SceneItems2D.ItemsSource`

> Data: 2026-09-24. Acesta este un RED intenționat, **nu** un build GREEN al soluției.

Consumer-ul permanent este [proiectul](sceneitems-compile-red/SceneItems2DCollectionCompileRed.csproj) și [sursa](sceneitems-compile-red/SceneItems2DCollectionCompileRed.cs). Proiectul nu este adăugat în `Cerneala.slnx`, setează `EnableDefaultCompileItems=false` și include numai propriul `.cs`. Calea este sub `docs/`, pe care `Cerneala.csproj` o exclude din `Compile`; o evaluare post-creare a itemilor Core a returnat exit 0 și 0 potriviri pentru sursa probei în [lista brută](compile-items-after-isolated-probe.json). Prin urmare, RED-ul nu sparge build-urile normale prin globbing.

Proba folosește **tipul real public** `Cerneala.Tetris.TetrisSpriteModel` din `Tetrisish/Tetris.csproj`, fără a inventa constructor sau date. Ea face doar două asignări, dintr-un `IEnumerable` și dintr-un `ObservableCollection<TetrisSpriteModel>`, la `SceneItems2D.ItemsSource`.

Comanda exactă:

```powershell
dotnet build .\docs\plans\evidence\2026-09-23-scene2d-stage1\sceneitems-compile-red\SceneItems2DCollectionCompileRed.csproj -c Release -m:1 --nologo -v:minimal '-p:DefaultItemExcludesInProjectFolder=artifacts/**'
```

[Output brut](sceneitems-compile-red.log): exit **1**, 0 warnings și **exact 2 erori CS0266** la cele două asignări (liniile 13 și 14). Destinația actuală raportată de compiler este `ISceneSpatialSource2D<object>`, incompatibilă cu `System.Collections.IEnumerable` și cu `ObservableCollection<Cerneala.Tetris.TetrisSpriteModel>`. Core, Tetris și dependențele s-au construit înaintea acelor diagnostice; nu există eroare de fixture/restaurare în log. Acesta este RED pentru motivul de contract urmărit, nu o excepție de mediu.

După cutover-ul API din etapa 2, aceeași probă trebuie să devină GREEN fără a-i slăbi cele două asignări. Niciun rezultat GREEN nou nu este pretins aici.
