# Shared Context Modularization Plan

This note captures a concrete path for exploring one logical context shared across modular feature assemblies without introducing an aggregate owner assembly that references every feature.

The design goal is:
- keep components and systems together in each feature assembly
- preserve the existing single-assembly Entitas workflow
- move as much generated code as possible into feature assemblies
- leave only truly context-global residue for runtime composition

## Working Position

We are not trying to make the whole framework runtime-driven.

We are trying to:
- push feature-local generated outputs into feature assemblies
- switch those local outputs directly to component handles
- keep context-global schema and bootstrap as the only runtime-composed part

This is intentionally different from an aggregate-owner assembly model.

## Current Generator Output Buckets

The current generator already falls into three natural categories.

### 1. Pure local outputs

These can live in a feature assembly without needing the full context schema.

- event listener interfaces
- generated helper component classes for event listener components
- future feature-local helper wrappers

Current sources:
- `gen/Entitas.CodeGeneration/Events/EventsGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/Components/ComponentTemplates.cs`

### 2. Local outputs that currently depend on global slot/schema data

These are generated from a single component and conceptually belong to the feature assembly, but today they hardcode context-global indices or root metadata.

- entity extension APIs: `AddX`, `ReplaceX`, `RemoveX`, `GetX`, `HasX`
- unique context APIs: `SetX`, `ReplaceX`, `RemoveX`, flag helpers
- component matcher APIs: `GameMatcher.X()`
- event listener components
- event entity APIs
- per-event system classes
- per-component cleanup system classes

Current sources:
- `gen/Entitas.CodeGeneration/Components/ComponentGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/Components/ComponentTemplates.cs`
- `gen/Entitas.CodeGeneration/Events/EventsGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/Cleanup/CleanupGenerationHelper.cs`

### 3. Pure global outputs

These require the union of all components belonging to the logical context.

- `${Context}Context`
- `${Context}Entity`
- root `${Context}Matcher`
- `${Context}ContextsExtension`
- `${Context}ComponentsLookup`
- `${Context}EntityIndices`
- `${Context}CleanupSystems`
- `${Context}EventSystems`
- visual debugging context bootstrap

Current sources:
- `gen/Entitas.CodeGeneration/Contexts/ContextGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/ComponentsLookups/ComponentsLookupGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/EntityIndex/EntityIndexGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/Cleanup/CleanupGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/Events/EventsGenerationHelper.cs`
- `gen/Entitas.CodeGeneration/VisualDebugging/VisualDebuggingGenerationHelper.cs`

## Main Design Rule

Do not add a compatibility shim where local generated APIs keep depending on `${Context}ComponentsLookup` and handles are only layered on top.

Instead:
- introduce component handles as the new source of truth for the first migrated local APIs
- switch the smallest useful local slice to handles directly
- treat context schema metadata as a separate concern

This gives a cleaner split:
- handle: component slot identity used by feature-local generated APIs
- schema metadata: total components, names, types, indices, bootstrap info used by the logical context

Important scope limit:
- the first handle migration should cover only plain component entity/context APIs
- do not move matcher roots, events, cleanup, entity indices, or visual debugging in the same phase
- keep commits small enough that each step is reviewable and reversible

## Non-Goals

These should not be optimized for during the first implementation passes.

- do not support cross-assembly partial type merging
- do not build an aggregate owner assembly that references every feature assembly
- do not make all runtime/global systems dynamic in one step
- do not require backward-compatible internal generator plumbing when a direct replacement is simpler

## Constraints

Each phase must satisfy all of the following.

1. The repo must compile.
2. Existing automated tests must pass, or be updated in the same phase to reflect an intentional API change.
3. Single-assembly Entitas usage must remain supported.
4. The new modular path may be partial or experimental at first, but it must not break the single-assembly path.

## Proposed Architecture Direction

### Feature assembly responsibilities

Feature assemblies should eventually own as much of the following as possible:

- component declarations
- systems that use those components
- generated entity APIs for those components
- generated unique-context APIs for unique components in those features
- generated per-component matcher accessors or equivalent feature-local matcher handles
- generated event listener APIs and per-event systems
- generated per-component cleanup systems

### Context/global runtime responsibilities

The context runtime should own only the residue that truly requires the union of all components in a logical context:

- final component slot assignment
- total component count
- component names and types metadata
- logical context construction
- entity index registration/bootstrap
- aggregate event-systems registration
- aggregate cleanup-systems registration
- visual debugging bootstrap

## Phased Plan

### Phase 0: Characterize The Existing Generator Surface

Goal:
- lock down current behavior so later structural changes do not accidentally drift

Work:
- add or tighten tests for generated output sets and key public API shapes
- explicitly cover:
  - context outputs
  - entity APIs
  - unique context APIs
  - matcher APIs
  - event outputs
  - cleanup outputs
  - entity index outputs

Success criteria:
- `tests/Entitas.CodeGeneration.Tests` stays green
- single-assembly behavior is explicitly covered, not inferred

Notes:
- this phase should not change generation semantics

### Phase 1: Refactor Generator Internals Around Local vs Global Ownership

Goal:
- restructure the generator so local and global emitters are clearly separated in code

Work:
- split generator flow into:
  - pure local emitters
  - local emitters with slot dependency
  - pure global emitters
- keep emitted source behavior unchanged where possible

Primary files:
- `gen/Entitas.CodeGeneration/EntitasGenerator.cs`
- helper classes under `Components/`, `Events/`, `Cleanup/`, `Contexts/`, `EntityIndex/`, `ComponentsLookups/`

Success criteria:
- all tests pass
- no user-facing single-assembly regression
- codebase reflects the architectural boundary we intend to evolve

### Phase 2: Introduce Component Handles For Plain Component APIs Only

Goal:
- remove direct slot dependence on `${Context}ComponentsLookup.X` from plain feature-local component APIs

Work:
- define a component handle abstraction for generated per-component APIs
- switch only these outputs to handles directly:
  - entity APIs
  - unique context APIs
- do not switch matcher APIs, events, cleanup, entity indices, or visual debugging in this phase
- do not add a bridge layer where handles are only wrappers around old local codegen assumptions

Important boundary:
- handles replace local slot references
- handles do not replace full context schema metadata

Success criteria:
- plain generated component APIs compile and pass tests using handles
- single-assembly support still works
- the generator no longer treats `${Context}ComponentsLookup.X` as the primary source of truth for plain component APIs

Commit-size guidance:
- if needed, split this phase into separate commits for:
  - handle abstraction
  - non-flag entity APIs
  - flag entity APIs
  - unique context APIs

### Phase 3: Move Plain Component APIs To Feature Ownership

Goal:
- make normal component APIs feature-owned while keeping one logical context model intact

Work:
- generate plain component APIs as feature-local outputs that depend only on:
  - shared context/entity root types
  - component handles
- keep single-assembly support using the same code path
- focus only on plain components first

Included:
- `AddX`, `ReplaceX`, `RemoveX`, `GetX`, `HasX`
- unique context APIs for plain and flag components

Optional later follow-up after this phase is stable:
- feature-local matcher accessors if they can be kept local cleanly

Deferred:
- events
- cleanup
- entity indices
- visual debugging

Success criteria:
- a feature-local component API model exists
- single-assembly path still compiles and passes tests
- the first useful slice works before advanced features move

### Phase 4: Move Event And Cleanup Per-Component Outputs To Feature Ownership

Goal:
- extend feature ownership to the remaining per-component generated surfaces

Work:
- move these to feature-local generation based on handles and shared root types:
- event listener components
- event entity APIs
- event listener interfaces
- per-event system classes
- per-component cleanup system classes

Commit-size guidance:
- do not move events and cleanup in one commit unless the changes are genuinely small
- prefer splitting by subsystem:
  - event listener contracts and APIs
  - per-event system classes
  - per-component cleanup systems

Still global after this phase:
- aggregate `${Context}EventSystems`
- aggregate `${Context}CleanupSystems`

Success criteria:
- per-component event and cleanup artifacts are local
- aggregate context-level registration is the only remaining global event/cleanup residue
- single-assembly tests remain green

### Phase 5: Introduce Runtime-Composed Context Schema

Goal:
- make the logical context schema the first real runtime-composed global surface

Work:
- add runtime schema/bootstrap primitives for:
  - feature registration
  - deterministic component slot assignment
  - total component count
  - component names/types metadata
  - logical context construction
- keep single-assembly support by treating one assembly as the trivial one-feature case

Key design rule:
- this phase composes only the schema/global residue
- feature-local generated APIs should already be using handles from earlier phases

Success criteria:
- one logical context can be built from registered feature contributions
- single-assembly still works as the simplest registration case
- tests cover deterministic schema assembly

### Phase 6: Runtime-Compose Remaining Global Residue

Goal:
- remove the last static full-context assumptions one subsystem at a time

Recommended order:
1. component lookup metadata equivalent
2. aggregate cleanup-system registration
3. aggregate event-system registration
4. entity-index bootstrap
5. visual debugging bootstrap

Why this order:
- lookup/schema metadata is foundational
- cleanup and event aggregation are simpler than entity indices
- entity indices and visual debugging are more sensitive to global assumptions

Success criteria:
- each subsystem has targeted tests before moving to the next one
- single-assembly remains supported at every step

### Phase 7: Reassess Canonical Architecture

Goal:
- decide whether the runtime-composed shared-context model becomes the documented primary modular path

Work:
- compare complexity, ergonomics, and maintenance cost against the current single-assembly default
- document the intended supported architecture clearly
- only remove old generator assumptions after parity exists for the supported feature set

Success criteria:
- there is one clear story for:
  - single-assembly projects
  - modular shared-context projects
- documentation reflects the actual supported model

## Recommended First Execution Slice

The smallest high-value implementation slice is:

1. Phase 1
2. Phase 2
3. the plain-component subset of Phase 3

That means:
- no event migration yet
- no cleanup migration yet
- no entity-index runtime composition yet
- no visual debugging migration yet

If this slice fails to stay clean, the larger architecture is probably too expensive.

If this slice succeeds, the rest of the plan becomes much more credible.

Recommended commit rhythm for this slice:

1. generator refactor only
2. add handle abstraction only
3. switch non-flag entity APIs
4. switch flag entity APIs
5. switch unique context APIs
6. move plain component outputs toward feature ownership

## Open Questions To Resolve During Execution

1. What is the minimal handle shape needed for generated APIs?
- raw index wrapper
- richer descriptor
- context-bound slot object

2. Should per-component matcher access stay on `${Context}Matcher.X()` or move to feature-local matcher surfaces?

3. How should deterministic slot ordering be defined for runtime-composed schema?
- registration order is risky
- stable ordering by assembly and full component type name is safer

4. How should entity indices be registered in the modular model?
- fully runtime-declared
- generated local declarations plus runtime bootstrap

5. How much of visual debugging is worth preserving in the first modular path?

## Implementation Principle

Prefer the simplest architecture that preserves these three things simultaneously:

1. single-assembly projects remain easy
2. feature assemblies keep components and systems together
3. only truly global context residue becomes runtime-composed
