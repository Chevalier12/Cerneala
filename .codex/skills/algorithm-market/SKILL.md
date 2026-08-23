---
name: algorithm-market
description: Exhaustively research, classify, and compare established algorithms for any named computational, systems, data, scientific, or visual problem, then recommend both the strongest absolute method and the best fit for the user's constraints. Use when the user asks what algorithms exist, sends Codex "to the market", wants algorithm families compared before implementation, asks for the best known method, or wants to replace a current algorithm. Do not use when the algorithm is already fixed and the request is ordinary implementation work.
---

# Algorithm Market

Research the algorithmic landscape before implementing. Treat the user's named
problem as the target; do not assume a particular domain, framework, product, or
repository context unless the request supplies one.

## Output Boundary

- This is a read-only research workflow by default. Return the complete market
  report in chat.
- A named file, component, checklist, repository, or plan is research context,
  not permission to edit it.
- Do not create, edit, delete, or check off project files unless the user
  explicitly asks to persist the report or implement the selected algorithm.
  Repository-mandated inspection artifacts are the only exception.
- When persistence is explicitly requested, modify only the requested artifact
  and still return the decision summary in chat.
- Keep research and implementation as separate phases. An implementation
  request authorizes implementation work only within its stated scope.

## Workflow

### 1. Establish the current state when one exists

- If the user names a repository, component, or existing implementation, follow
  its repository instructions and inspect the owning code, tests, documentation,
  and relevant call path before browsing.
- State precisely which method is currently implemented and distinguish a
  placeholder, a basic established method, a hybrid, and a production-grade
  implementation.
- If no implementation is supplied, do not invent one or force repository
  inspection. Research the problem from the stated contract.

### 2. Normalize the comparison target

Define the problem independently of any candidate algorithm:

- required inputs, outputs, and correctness or quality contract;
- scale and data characteristics;
- exact versus approximate results;
- offline, online, streaming, incremental, distributed, or real-time behavior;
- latency, throughput, memory, storage, communication, energy, and hardware
  constraints that actually matter;
- determinism, reproducibility, numerical stability, security, privacy, and
  failure behavior where relevant;
- target language, platform, dependencies, integration boundaries, and license
  constraints when adoption is contemplated.

Do not silently broaden or narrow the requested problem. Separate hard gates
from preferences and label missing constraints as unknown rather than guessing.

### 3. Search independent evidence tracks

Use primary sources and run distinct queries for both tracks:

- **Research track:** original papers, proceedings, standards, specifications,
  author project pages, official benchmark suites, supplemental material, and
  official reference implementations. Trace important improvements through
  their original publications.
- **Production track:** maintained source in mature libraries, databases,
  compilers, operating systems, engines, scientific packages, vendor samples,
  or other deployed systems relevant to the domain. Product pages, demos, and
  blog posts without inspectable implementation evidence are not enough.

A method appearing in both tracks counts as one candidate. Prefer tagged
releases; otherwise pin the exact commit. For implementation evidence, record
the repository, exact file or function, stable URL, revision, license identifier,
and license URL. Read [license-policy.md](references/license-policy.md) before
accepting source adoption as viable.

Use domain-appropriate evidence. For example, a graph algorithm needs
complexity and graph-family evidence; a numerical solver needs convergence and
conditioning evidence; a distributed algorithm needs failure and consistency
assumptions; an image method needs fidelity and artifact evidence. Do not score
every field as though it were graphics.

### 4. Meet the exhaustive-search gate

- Fully evaluate at least **ten distinct credible algorithms** when the field
  contains that many. Parameter changes, ports, and minor variants of the same
  method count once.
- Cover at least three genuinely different algorithm families when available.
- A rejected method counts only when it is relevant and has enough evidence to
  evaluate its behavior, cost, maturity, and licensing.
- Continue until two consecutive query rounds in each evidence track reveal no
  new credible candidate or family. Record query themes and the saturation
  result.
- If the field saturates below ten credible candidates, report the shortfall
  instead of padding the list or claiming exhaustive coverage.
- If the user's scope or time budget explicitly requests a smaller scan, label
  the result as a shortlist rather than an exhaustive market.

### 5. Compare on one problem-specific decision model

- Define a 100-point scoring matrix before scoring. Choose and publish weights
  that reflect the normalized target rather than reusing a fixed domain matrix.
- Typical dimensions include correctness or result quality, asymptotic and
  observed cost, robustness, fit to hard constraints, scalability, operational
  complexity, production maturity, integration risk, and maintainability. Use
  only dimensions relevant to the problem.
- Apply the same matrix to every candidate. Support scores with cited evidence
  or label them clearly as engineering inference. Never present inferred scores
  as benchmark measurements.
- Treat correctness requirements, platform support, required semantics,
  security constraints, and license compatibility as hard gates when the
  target makes them mandatory.
- Compare theoretical complexity and real implementation behavior separately.
  State benchmark workload, hardware, dataset, and revision before using a
  measured result; do not transfer benchmark rankings to a materially different
  workload without labeling the extrapolation.
- Identify both the strongest algorithm without target integration constraints
  and the strongest algorithm that satisfies every hard gate. Explain the
  separating constraint when they differ.
- Hybrids are valid candidates only when the composition is established or the
  proposed combination is evaluated transparently as an engineering design,
  not mislabeled as a published algorithm.

### 6. Return the decision report

Write in the user's language unless they request another language. Use the
following section order so coverage, evidence, and the verdict remain auditable.

## Mandatory Chat Report Format

Start with `Research coverage:` (translated to the response language if
appropriate). Report:

- research-track query themes and saturation result;
- production-track query themes and saturation result;
- number of fully evaluated candidates and distinct families;
- whether the exhaustive-search gate was met.

Then include these sections:

1. **Target contract** — Inputs, outputs, required behavior, workload, hard
   gates, preferences, and unknowns.
2. **Evaluation matrix** — The problem-specific dimensions and weights totaling
   100 points, plus non-scoreable hard gates.
3. **Comparison** — One row for every fully evaluated candidate using this
   header:

   ```markdown
   | Candidate | Family | Score | Hard gate | Decisive tradeoff |
   |---|---|---:|---|---|
   ```

   Immediately state that scores are evidence-backed engineering judgments
   unless explicitly identified as measurements.
4. **Candidate evidence** — One card per candidate, in table order:

   ```markdown
   [ ] **{AlgorithmName}**

   Family: {algorithm family and defining idea}

   Sources: {original paper, standard, and/or production source with direct links}

   License: {SPDX identifier, stable license link, adoption verdict, or not applicable}

   Guarantees and quality: {correctness, approximation, convergence, fidelity, robustness, and edge cases as relevant}

   Cost: {time, memory, passes, communication, hardware, operational complexity, and measured evidence as relevant}

   Fit: {target constraints, integration implications, maturity, and risks}

   Verdict: {recommended / viable alternative / reject, with the decisive reason}
   ```

5. **Current method** — When code or a system was supplied, name the current
   algorithm, behavior, and limitations with clickable local file links and line
   numbers. Otherwise state that no current implementation was provided.
6. **Best absolute** — The strongest method when target integration cost is
   unconstrained.
7. **Best fit** — The highest-scoring candidate that passes every hard gate and
   the short decisive reason. Identify the separating constraint if it differs
   from the absolute winner.
8. **Implementation outline** — The smallest concrete scope, validation plan,
   and principal risks for adopting the recommendation. State whether any files
   were modified.

The unchecked candidate boxes are report formatting, not permission to edit a
repository checklist.

## Evidence Rules

- Cite every factual, benchmark, source-code, revision, and license claim with a
  direct link near the claim.
- Prefer papers, standards, official documentation, and maintained upstream
  repositories over summaries and secondary rankings.
- Separate facts, measurements, and engineering inference visibly.
- Pin production evidence to a release or commit whenever possible.
- Do not call the search exhaustive unless candidate count, family coverage,
  and two-round saturation gates were met.
- Do not paste substantial copyrighted source code into the report.
- Treat license conclusions as engineering screening, not legal advice.
- If no candidate passes all hard gates, say so and identify what constraint or
  product decision must change. Do not manufacture a winner.
