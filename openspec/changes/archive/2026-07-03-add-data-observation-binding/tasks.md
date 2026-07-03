## 1. Observable Values

- [x] 1.1 Create `UI/Data/ObservableValue{T}.cs`.
- [x] 1.2 Create typed value change event args.
- [x] 1.3 Implement equality-aware value assignment.
- [x] 1.4 Ensure value changes expose old and new typed values.
- [x] 1.5 Add `tests/Cerneala.Tests/UI/Data/ObservableValueTests.cs`.

## 2. Observable Lists

- [x] 2.1 Create `UI/Data/IObservableList{T}.cs`.
- [x] 2.2 Create `UI/Data/ObservableList{T}.cs`.
- [x] 2.3 Create list change kind and event args types as needed.
- [x] 2.4 Implement ordered count, indexer, enumeration, add, insert, remove, replace, move, clear, and reset behavior.
- [x] 2.5 Emit typed list change notifications with indexes and affected item data.
- [x] 2.6 Add `tests/Cerneala.Tests/UI/Data/ObservableListTests.cs`.

## 3. Typed Property Adapters

- [x] 3.1 Create `UI/Data/PropertyAdapter{TOwner,TValue}.cs`.
- [x] 3.2 Support typed read and write delegates without string property paths.
- [x] 3.3 Add helper creation for retained `UiObject` typed properties if useful.
- [x] 3.4 Prove adapter writes use existing typed property invalidation.

## 4. Binding-Light APIs

- [x] 4.1 Create `UI/Data/Binding.cs`.
- [x] 4.2 Create `UI/Data/Binding{T}.cs`.
- [x] 4.3 Create `UI/Data/BindingMode.cs`.
- [x] 4.4 Create `UI/Data/IValueConverter{TIn,TOut}.cs`.
- [x] 4.5 Implement one-way binding from observable source to explicit target setter.
- [x] 4.6 Implement disposable binding subscriptions.
- [x] 4.7 Implement explicit two-way target commit to source writer.
- [x] 4.8 Implement typed converter support for one-way target updates.
- [x] 4.9 Add `tests/Cerneala.Tests/UI/Data/TypedBindingTests.cs`.

## 5. Collection Views

- [x] 5.1 Create `UI/Data/CollectionView{T}.cs`.
- [x] 5.2 Create `UI/Data/SortDescription{T}.cs`.
- [x] 5.3 Create `UI/Data/FilterPredicate{T}.cs`.
- [x] 5.4 Implement typed filtering over source data.
- [x] 5.5 Implement deterministic typed sorting over filtered data.
- [x] 5.6 Refresh collection view when observable source list changes.
- [x] 5.7 Emit view reset notifications after refresh.
- [x] 5.8 Add `tests/Cerneala.Tests/UI/Data/CollectionViewTests.cs`.

## 6. String Path Deferral

- [x] 6.1 Create `UI/Data/StringPropertyPath.cs`.
- [x] 6.2 Ensure string property paths explicitly report unsupported/deferred behavior.
- [x] 6.3 Ensure string property paths are not used by binding hot paths.
- [x] 6.4 Add `tests/Cerneala.Tests/UI/Data/StringPropertyPathTests.cs`.

## 7. Roadmap And Boundaries

- [x] 7.1 Update `ROADMAPv2.md` section 18 file checklist as files are completed.
- [x] 7.2 Update `ROADMAPv2.md` section 18 test checklist as tests are completed.
- [x] 7.3 Add architecture boundary coverage for section 18 data APIs.
- [x] 7.4 Prove data APIs avoid MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.

## 8. Validation

- [x] 8.1 Verify `openspec validate add-data-observation-binding --strict` passes.
- [x] 8.2 Verify `openspec validate --all --strict` passes.
- [x] 8.3 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 8.4 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
