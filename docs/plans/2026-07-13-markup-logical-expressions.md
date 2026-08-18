# Plan: logical expressions in `@when` and `@if`

Date: 2026-07-13

## Objective

We extend the language of the directives from the files `.crn` with lowercase operators
`and`, `or` and grouping brackets, without turning the markup into a
second C#. Expressions must remain typed, source-generated, reactive and
compatible with the existing syntax.

Target examples:
```xml
@when IsMouseOver and IsEnabled
{
    Background = $HoverBrush;
}
```

```xml
@when $DataContext.Temperature
{
    @if value >= 80 and value < 100
    {
        Foreground = $WarningBrush;
    }
}
```

```xml
@when $DataContext.HasTarget and
      ($DataContext.HasLineOfSight or $DataContext.CanShootThroughWalls)
{
    <Button Content="Fire" />
}
```
## Language contract

- [x] Documents the accepted operators: exclusively `and`, `or` and `(` `)`; no
      add `not`, `&&`, `||` or arbitrary C# expressions in this change.
- [x] Defines the precedent as `comparatie` > `and` > `or`; the brackets have
      explicit priority.
- [x] Treat `and` and `or` as keywords only at token boundaries, thus
      so that members like `IsAndroidReady` remain normal identifiers.
- [x] Allows whitespace and newline between tokens, but does not interpret
      keywords found in string literals.
- [x] Keeps the existing syntax with a single fully compatible source.
- [x] For a compound `@when`, require each leaf to be a Boolean source;
      a simple `@when` can still observe any type used later
      through `@if value ...`.
- [x] Defines `value` from a `@if` located in a `@when` straight compound
      the Boolean result of the entire expression `@when`.
- [x] Allows in `@if` logically related comparisons, including repeated comparisons with
      `value` and comparisons with other typical reactive sources.
- [x] Keep the existing rules for comparators, `Null`, strings,
      enums, numbers and type compatibility.
- [x] Evaluates short-circuit predicates, but discovers and observes all
      syntactic dependencies. An unexecuted branch must not become stale.
- [x] Apply the guard of a nullable/incomplete data path to the leaf that
      use, not over the entire expression; in a `or`, the other branch
      it must be able to become `true`.

## Stage 1: RED tests for grammar

- [x] Add tests in
      `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs` for one
      `@when A and B` and a `@when A or B`.
- [x] Add tests for `@if value >= min and value <= max`.
- [x] Add a test that demonstrates the precedent of `and` before `or`.
- [x] Add a test that proves that parentheses overwrite the previous one.
- [x] Add tests for multiline expressions and variable whitespace.
- [x] Add compatibility tests for `@when Source`, shorthand Boolean
      and multiple existing `@if` blocks.
- [x] Add tested diagnostics for missing operator, missing operator,
      missing parenthesis, extra parenthesis and empty expression.
- [x] Add tested diagnostics for a non-Boolean leaf in a `@when`
      compound and comparisons between incompatible types.
- [x] Add a test that confirms that `and`/`or` from a string or name
      member are not tokenized as operators.

## Stage 2: AST and parser

- [x] Replaces the payment representation `SourceExpression` / `Comparator` /
      `Operand` with a minimal internal AST for expressions: source, `value`,
      literal, comparison, `and`, `or` and grouping.
- [x] Implements a small, deterministic lexer that preserves the source `XObject`
      and the token offset for accurate diagnostics.
- [x] Implements the parser by recursive descent: `ParseOr`, `ParseAnd`,
      `ParsePrimary` and `ParseComparison`.
- [x] Reuses the same logical parser for the `@when` and `@if` headers, with
      different contextual validation instead of duplicating the grammar.
- [x] Keep `ReadHeaderUntilBrace` responsible only for delimitation
      header and strings; move the interpretation of the expression to the new parser.
- [x] Check the protection of comparators `<` and `<=` carried out in
      `UiMarkupGenerator` before parsing the XML and adds a case with brackets.
- [x] Issue diagnostics that indicate the problematic token, not just the beginning
      the whole block.

## Stage 3: semantic binding and dependencies

- [x] Solve each source leaf through the same paths used today by
      `EmitObservation`: element property, `$DataContext`, `$owner`,
      `$self` and template parts.
- [x] Separate semantic resolution from C# emission: the validated AST must
      contains the type and observation associated with each leaf.
- [x] Deduplicate identical sources from the same expression, so that
      `value >= 80 and value < 100` not to create two observations for
      same property.
- [x] Validates Boolean for the leaves of a compound `@when`.
- [x] Validate the comparators and types of each comparison in `@if` using
      the existing rules of `EmitComparison`.
- [x] Keep all observations in `ReactivePlan`, regardless of the branches
      which will be short-circuited during evaluation.
- [x] Confirm that observations created in templates are registered in
      the existing `TemplateEmissionContext` lifetime.

## Stage 4: reactive emission

- [x] Issue fully parenthesized C# predicates with `&&` and `||`, as its result
      don't accidentally depend on the previous string generator.
- [x] Keep the C# short-circuit on evaluation.
- [x] Composes the predicate of the expression with `inheritedPredicate` used by Aspect and
      of nested `@when` blocks.
- [x] Keeps the existing order and cascade for conditional assignments.
- [x] Keep the existing lifecycle for conditional content: creation at
      activation, detachment and disposal upon deactivation.
- [x] Does not introduce allocations per revaluation; AST and dependency discovery are
      exclusively the responsibility of the source generator.
- [x] Do not modify the public runtime API if the current infrastructure
      `MarkupObservation` + `MarkupConditionRule` can represent the expressions.

## Stage 5: runtime and lifecycle tests

- [x] Demonstrates that changing any dependency reevaluates a `and`.
- [x] Demonstrates that changing any dependency reevaluates a `or`,
      including the dependence initially located in a short-circuited branch.
- [x] Tests expressions that combine element properties and paths
      `$DataContext`.
- [x] Test `$owner` and `$self` in a composite template.
- [x] Tests a part template property in a compound expression.
- [x] Tests the restore of the base value when the expression goes from `true` to
      `false`.
- [x] Conditionally tests children upon activation, deactivation and reactivation.
- [x] Test the detachment of the element/template and confirm that subscriptions
      I no longer receive notifications.
- [x] Add a case with intermediate data path `null` in a `or` to
      validate the guards per leaf.
- [x] Confirms that two occurrences of the same source produce one
      observation/subscription.

## Stage 6: documentation and real example

- [x] Updates the conceptual documentation for the `.crn` directives with
      grammar, precedent and examples for `and`, `or` and brackets.
- [x] Explain the difference between short-circuiting the evaluation and observing everything
      addicts.
- [x] Explicitly documents the limitations: no `not`, no arbitrary C# and no
      non-Boolean leaves in a compound `@when`.
- [x] Updates the old examples on the site that describe the reactive syntax.
- [x] Add a small, readable example to the Playground that combines two states and
      displays the effect without turning `MainWindow.crn` into a soup.
- [x] If the implementation modifies any public API, update the same
      change the pages of `docs-site/documentation/classes/` and the manifesto;
      otherwise it explicitly records that the change is only of language/sourcegen.

## Stage 7: final check

- [x] Run the targeted tests from `Cerneala.Tests.SourceGen` after each
      RED/GREEN stage.
- [x] Runs the full test suite and does not accept any new failed or skipped tests.
- [x] Start the Playground and manually check `and`, `or`, previous and one
      case with brackets, also following frame stats for obvious regressions.
- [x] Inspect the generated code for a complex example: observations
      deduplicated, fully bracketed predicates and lifetime registration.
- [x] Run a repeated attach/detach smoke test for conditional content
      and check the lack of subscription leaks.
- [x] Reindex the solution and confirm zero warnings in RoslynIndexer.
- [x] Check the checklist only as each step is demonstrated.

## The definition of ready
- [x] The old syntax compiles and behaves identically.
- [x] `and`, `or` and brackets work in `@when` and `@if` according
      documented precedents.
- [x] All dependencies are reactive even if the evaluation makes a short-circuit.
- [x] Diagnostics for invalid expressions are precise and actionable.
- [x] Conditional properties, Aspect templates and conditional children
      keep the waterfall and the lifecycle.
- [x] The playground has been manually tested, the documentation is also updated
      the whole suite is GREEN.