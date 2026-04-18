# DesperateDevs, TCPeasy, And Sherlog Removal Plan

This note captures a concrete removal strategy for the remaining `DesperateDevs` dependencies in this fork and clarifies how `TCPeasy` and `Sherlog` should be handled during the cleanup.

The main goal is:
- remove direct `DesperateDevs` package dependencies where they are no longer justified
- avoid breaking the current Entitas 2.0 path while doing so
- explicitly account for `TCPeasy` and `Sherlog` as compatibility and packaging concerns even though they are not current live code dependencies in this repo

## Current State

### Live DesperateDevs package references

The current solution still has these package references:

`src/Entitas/Entitas.csproj`
- `DesperateDevs.Caching`
- `DesperateDevs.Reflection`

`src/Entitas.Unity.Editor/Entitas.Unity.Editor.csproj`
- `DesperateDevs.Reflection`
- `DesperateDevs.Unity.Editor`

### Current code usage by area

#### 1. Core runtime

`src/Entitas/Context/Context.cs`
- uses `DesperateDevs.Caching`

`src/Entitas/Extensions/PublicMemberInfoEntityExtension.cs`
- uses `DesperateDevs.Reflection`
- currently relies on `CopyPublicMemberValues`

This is the most important removal surface because it affects the main runtime package.

#### 2. Source generator output

`gen/Entitas.CodeGeneration/VisualDebugging/Feature/FeatureTemplates.cs`
- still emits code that references `DesperateDevs.Extensions.TypeExtension`
- still emits code that references `DesperateDevs.Extensions.StringExtension`

This is a small, high-value removal target because it is isolated and easy to validate.

#### 3. Unity editor tooling

`src/Entitas.Unity.Editor/*`
- still depends on `DesperateDevs.Reflection`
- still depends on `DesperateDevs.Unity.Editor`

Representative files:
- `EntitasMenuItems.cs`
- `EntityDrawer.cs`
- `ContextObserverEditor.cs`
- `DebugSystemsEditor.cs`
- `TypeDrawer/*`

This is a larger subsystem-level cleanup, not a quick dependency swap.

### TCPeasy status

`TCPeasy` is not a live code or package dependency in the current repo.

Search results show it only appears in historical docs/changelog references such as:
- `CHANGELOG.md`

That means:
- there is no current technical removal task for `TCPeasy` package references
- but it still matters as a release/documentation boundary because Entitas historically referenced it as an adjacent ecosystem tool

For planning purposes, `TCPeasy` should be treated as:
- a compatibility and messaging concern
- a documentation cleanup concern
- a non-goal for code migration unless a hidden package or sample dependency appears later

### Sherlog status

`Sherlog` is also not a live code or package dependency in the current repo.

Search results show:
- no source references
- no package references
- no current project-level technical coupling

That means:
- there is no active code removal task for `Sherlog`
- but it should be tracked the same way as `TCPeasy` so release docs and migration messaging do not imply it is still part of the active Entitas 2.0 story

## Strategic Position

The removal should not be treated as one big repo-wide task.

There are really three separate tracks:

1. generator cleanup
2. core runtime cleanup
3. Unity editor tooling cleanup

These tracks have different risk levels and should not be combined into one large commit or one large phase.

## Removal Principles

1. Prefer removing dependencies from `src/Entitas` first.
2. Prefer replacing small extension/helper uses with small local implementations instead of introducing new third-party packages.
3. Keep public runtime behavior stable unless a change is intentionally documented.
4. Treat `Entitas.Unity.Editor` as an optional layer and avoid blocking runtime cleanup on editor cleanup.
5. Treat `TCPeasy` and `Sherlog` as docs/release concerns unless real code references appear.

## Phase Plan

### Phase 0: Inventory And Lock Down Behavior

Goal:
- make dependency removal measurable and safe

Work:
- record the current `DesperateDevs` package references and code usage locations
- identify which tests cover runtime copy behavior, context behavior, and editor-adjacent behavior
- add focused tests if current coverage is weak for:
  - entity component copying
  - context behavior touched by caching
  - generated visual debugging output shape if that path remains enabled in tests

Success criteria:
- there is a clear before/after dependency inventory
- replacement work has targeted validation instead of relying on broad solution confidence

### Phase 1: Remove DesperateDevs From Generated Visual-Debugging Output

Goal:
- remove `DesperateDevs` references from generated code in the easiest low-risk place first

Current usage:
- `gen/Entitas.CodeGeneration/VisualDebugging/Feature/FeatureTemplates.cs`

Work:
- replace generated calls to:
  - `DesperateDevs.Extensions.TypeExtension.ToCompilableString`
  - `DesperateDevs.Extensions.TypeExtension.ShortTypeName`
  - `DesperateDevs.Extensions.StringExtension.ToSpacedCamelCase`
- use local helper logic instead
- keep generated `Feature` behavior unchanged from the user’s perspective

Likely implementation shape:
- add local helper methods inside the generated template or generator helpers for:
  - getting the type name
  - extracting a short type name
  - converting camel case to a spaced label

Validation:
- `dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj -c Release`
- `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj -c Release`
- optional focused sample build if visual debugging generation is enabled there

Success criteria:
- generated visual-debugging code no longer references `DesperateDevs`
- no behavioral regression in debug-systems naming

### Phase 2: Replace Reflection Copy Helpers In Core Runtime

Goal:
- remove `DesperateDevs.Reflection` from the core runtime package

Current usage:
- `src/Entitas/Extensions/PublicMemberInfoEntityExtension.cs`

Work:
- replace `CopyPublicMemberValues` with an internal runtime helper
- implement only the behavior Entitas actually needs:
  - copy public fields and supported writable properties from one component instance to another
- keep the helper minimal and internal to the runtime

Design constraints:
- do not recreate the whole `DesperateDevs.Reflection` helper surface
- keep behavior deterministic and easy to test
- be explicit about whether fields-only or fields-plus-properties are supported

Validation:
- runtime tests for entity copy behavior
- full solution build/test after replacement

Success criteria:
- `src/Entitas` no longer needs `DesperateDevs.Reflection`
- `PublicMemberInfoEntityExtension.CopyTo(...)` behavior remains correct

### Phase 3: Replace Or Inline Caching Used By Core Runtime

Goal:
- remove `DesperateDevs.Caching` from the main runtime package

Current usage:
- `src/Entitas/Context/Context.cs`

Work:
- identify the exact cache abstraction currently used from `DesperateDevs.Caching`
- replace it with one of:
  - a small internal cache helper
  - plain BCL collections if the abstraction is trivial
  - a localized rewrite if caching is only used in one path

Important rule:
- do not introduce a new package just to replace this package

Validation:
- runtime tests
- allocation-sensitive tests if current caching behavior is observable
- benchmark spot-check if necessary

Success criteria:
- `src/Entitas` no longer references `DesperateDevs.Caching`
- runtime behavior and expected allocation characteristics remain acceptable

### Phase 4: Remove DesperateDevs From The Core Runtime Project File

Goal:
- make `src/Entitas` independent of `DesperateDevs`

Work:
- remove package references from `src/Entitas/Entitas.csproj`
- run full build/test
- confirm consumers still only need Entitas packages, not hidden transitive `DesperateDevs`

Validation:
- `dotnet build Entitas.sln -c Release`
- `dotnet test Entitas.sln -c Release --no-build`

Success criteria:
- the core runtime package has zero direct `DesperateDevs` package references

### Phase 5: Audit And Trim Remaining Generator/Docs References

Goal:
- clean up non-runtime references after the core removal is done

Work:
- remove or update stale documentation references such as:
  - `EntitasUpgradeGuide.md`
  - roadmap notes if needed
- check package metadata/tags such as `Directory.Build.targets`
- ensure the generator and README no longer imply `DesperateDevs` is part of the current canonical workflow

Validation:
- grep audit for remaining non-historical references

Success criteria:
- remaining references are either historical changelog entries or intentional editor-tooling dependencies

### Phase 6: Isolate Unity Editor Dependency Surface

Goal:
- make `Entitas.Unity.Editor` the only remaining place where `DesperateDevs` may still exist, if necessary

Work:
- document the exact remaining `DesperateDevs` usages in `src/Entitas.Unity.Editor`
- group them by need:
  - editor layout helpers
  - reflection helpers
  - menu/settings helpers
  - type drawer helpers
- decide whether to:
  - replace them all in-place
  - introduce tiny internal editor helpers
  - or temporarily leave `Entitas.Unity.Editor` as the only remaining dependency boundary

Why this is a separate phase:
- editor tooling is optional for runtime consumers
- editor code has different APIs, different risks, and broader UI behavior to preserve

Success criteria:
- there is a written decision on whether editor removal is part of 2.0 or post-2.0
- runtime cleanup is no longer blocked on editor cleanup

Decision (2026-04-18):
- `Entitas.Unity.Editor` removal was initially deferred to protect the 2.0 runtime path.
- A follow-up Unity import failure showed that the shipped sample `Entitas.Unity.Editor.dll` still hard-referenced `DesperateDevs.*`, so Phase 7 became a practical compatibility fix rather than optional cleanup.
- The editor project now uses local compatibility helpers instead of direct `DesperateDevs` package references, and the sample `Entitas.Unity.Editor.dll` should be refreshed from the cleaned build output.
- `samples/Unity` may still carry legacy `TCPeasy` and `Sherlog` binaries, but they are separate from the `Entitas.Unity.Editor` assembly-load blocker that triggered this phase.

### Phase 7: Remove DesperateDevs From Unity Editor Tooling

Goal:
- fully remove direct `DesperateDevs` package dependencies from the repo, if that is still judged worthwhile

Current package references:
- `src/Entitas.Unity.Editor/Entitas.Unity.Editor.csproj`

Work:
- replace `DesperateDevs.Unity.Editor` helpers with local Unity editor helper code
- replace `DesperateDevs.Reflection` calls with local reflection helpers shared with the editor layer or runtime where appropriate
- remove the package references from the editor project file

Likely migration chunks:
1. type-name/string helpers
2. reflection/member-info helpers
3. editor layout wrappers
4. scripting define symbol helpers and menu tooling

Validation:
- `dotnet build src/Entitas.Unity.Editor/Entitas.Unity.Editor.csproj -c Release`
- `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj -c Release`
- manual smoke check in Unity editor for inspector/debug tooling

Success criteria:
- `Entitas.Unity.Editor` no longer directly references `DesperateDevs`
- core editor workflows still function acceptably

Status (2026-04-18):
- completed in source by replacing editor-side `DesperateDevs` helpers with local compatibility implementations
- validated with `dotnet build src/Entitas.Unity.Editor/Entitas.Unity.Editor.csproj -c Release`
- validated with `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj -c Release`
- validated by checking that the rebuilt `Entitas.Unity.Editor.dll` no longer references `DesperateDevs.Unity.Editor`, `DesperateDevs.Reflection`, or `DesperateDevs.Extensions`

## TCPeasy And Sherlog Workstream

Because `TCPeasy` and `Sherlog` are not live dependencies here, this workstream is smaller and mostly non-code.

### Phase A: Confirm No Live Dependency

Work:
- keep grep-based verification that there are no package references or code usages for:
  - `TCPeasy`
  - `Sherlog`
- repeat this check before release work

Success criteria:
- `TCPeasy` and `Sherlog` remain absent from project/package dependency graphs

### Phase B: Documentation Cleanup

Work:
- remove or clarify references that imply `TCPeasy` or `Sherlog` are part of the current Entitas 2.0 workflow
- keep historical mentions only in changelog/history where appropriate

Targets:
- upgrade/migration docs
- release notes drafts
- package descriptions if they imply ecosystem coupling

Success criteria:
- users do not infer that `TCPeasy` or `Sherlog` are required for Entitas 2.0

### Phase C: Release Boundary Definition

Work:
- explicitly state in release docs whether `TCPeasy` is:
  - unsupported
  - external/historical only
  - or separately maintained outside the main Entitas 2.0 path
- explicitly state the same for `Sherlog`

Success criteria:
- no ambiguity remains about whether `TCPeasy` or `Sherlog` are part of this release effort

## Recommended Execution Order

The best order is:

1. Phase 1: generated visual-debugging cleanup
2. Phase 2: runtime reflection helper replacement
3. Phase 3: runtime caching replacement
4. Phase 4: remove `DesperateDevs` from `src/Entitas`
5. Phase 5: docs/package metadata cleanup
6. Phase 6: decide editor scope
7. Phase 7: editor removal if still worth doing

And in parallel but lightweight:

1. Phase A
2. Phase B
3. Phase C

## Why This Order

This order creates early wins while preserving momentum:

- generator cleanup is small and visible
- core runtime cleanup gives the biggest architectural value
- docs cleanup should follow truth, not lead it
- editor tooling can be isolated and handled separately instead of blocking runtime cleanup
- TCPeasy and Sherlog are kept visible in planning without pretending they are current technical blockers

## Definition Of Done

Minimum acceptable outcome:

1. `src/Entitas` has no direct `DesperateDevs` dependencies.
2. generated runtime-facing code no longer references `DesperateDevs`.
3. docs do not present `TCPeasy` or `Sherlog` as part of the active 2.0 workflow.
4. any remaining `DesperateDevs` usage is explicitly isolated to `Entitas.Unity.Editor` with a conscious decision.

Full outcome:

1. no direct `DesperateDevs` package references remain anywhere in the repo.
2. Unity editor tooling has internal replacements for the old helper surface.
3. `TCPeasy` and `Sherlog` are clearly documented as historical/external unless real support work is intentionally resumed.
