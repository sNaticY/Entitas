# Maintainability Refactor Plan

Generated from a static review of the repo on 2026-04-25. Scope covered core runtime, Roslyn generator, code-generation attributes, Unity/editor integration, tests, samples, docs, and build infrastructure.

Baseline:

- 591 tracked files, 331 C# files.
- Main code areas: `src/Entitas`, `gen/Entitas.CodeGeneration`, `src/Entitas.CodeGeneration.Attributes`, `src/Entitas.Unity`, `src/Entitas.Unity.Editor`, `tests`, and `samples/Unity`.
- Largest maintainability pressure points are generator complexity, Unity sample/package drift, and large runtime/test types.

## Refactor Principles

- Prefer behavior-preserving internal refactors before public API changes.
- Keep generator output stable unless a change is explicitly intended.
- Treat `notes/entitas-2.0-roadmap.md` as the 2.0 release source of truth.
- Run focused tests for each refactor, then the full CI sequence before release-level changes.

## Phase 0: Safety And Hygiene

1. Remove dead generator helper `gen/Entitas.CodeGeneration/Contexts/Extensions/ContextDataExtensions.cs`.
   Verification: `dotnet build Entitas.sln -c Release` and `dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj`.

2. Add a generator-output stability check before larger template refactors.
   Candidate scope: contexts, matchers, entity/context extensions, cleanup, events, entity indices, feature-owned matcher generation, and visual debugging toggles.
   Verification: generated source comparisons or focused end-to-end tests in `tests/Entitas.CodeGeneration.Tests`.

3. Make implicit internal types explicit where useful, starting with `src/Entitas/Caching/ObjectPool.cs`.
   Verification: solution build only.

## Phase 1: Core Runtime Cleanup

1. Extract duplicated entity-index retain/release guard logic from `src/Entitas/EntityIndex/EntityIndex.cs` and `src/Entitas/EntityIndex/PrimaryEntityIndex.cs`.
   Suggested shape: protected helper methods on `AbstractEntityIndex<TEntity, TKey>` or a small internal helper local to entity-index code.
   Preserve the current `SafeAERC.Owners.Contains(this)` behavior.
   Verification: `EntityIndexTests`, `PrimaryEntityIndexTests`, and full `tests/Entitas.Tests`.

2. Normalize private field naming in `src/Entitas/Context/Context.cs`.
   Current mixed-case fields `_OnEntityReleasedDelegate` and `_OnDestroyEntityDelegate` should match `_onEntityChangedDelegate` style.
   Verification: build and `ContextTests`.

3. Defer large `Entity` and `Context` decomposition until smaller duplication work is complete.
   `src/Entitas/Entity/Entity.cs` and `src/Entitas/Context/Context.cs` are large but still cohesive. Avoid splitting them unless a follow-up refactor has a clear seam and strong tests.

4. Treat matcher partial consolidation as low priority.
   `src/Entitas/Matcher/Matcher*.cs` is fragmented, but each file has a clear concern. Consolidating would mostly improve navigation and create a broad diff.

## Phase 2: Generator Structure

1. Rename one of the duplicate extension class names.
   `gen/Entitas.CodeGeneration/Components/Extensions/ComponentDataExtensions.cs` and `gen/Entitas.CodeGeneration/Events/Extensions/ComponentDataExtensions.cs` both define `ComponentDataExtensions` in different namespaces. Rename the event one to something like `EventComponentDataExtensions`.
   Verification: event generator tests, listener extension tests, and full code-generation tests.

2. Consolidate or clarify string extension placement.
   `gen/Entitas.CodeGeneration/Extensions/StringExtensions.cs` and `gen/Entitas.CodeGeneration/Components/Extensions/StringExtensions.cs` split related naming helpers. Either move `ToComponentName()` to the shared namespace or rename the component-specific file to make the boundary obvious.
   Verification: generator build and code-generation tests.

3. Replace deeply nested incremental-generator tuples with named input structs.
   Primary target: `GenerateContextSharedSources` in `gen/Entitas.CodeGeneration/EntitasGenerator.cs`.
   Suggested shape: private readonly structs such as `ContextSharedSourceInput` and `ComponentOwnedSourceInput`.
   Verification: tests covering components lookup, entity indices, cleanup, event systems, feature schema registration, and context registration.

4. Reduce analyzer-config option boilerplate in `gen/Entitas.CodeGeneration/EntitasGeneratorOptions.cs`.
   Group keys by concern and centralize parsing metadata for boolean toggles. Avoid changing option names.
   Verification: `GeneratorOptionToggleTests` and assembly filtering tests.

5. Guard duplicated event enum semantics.
   `src/Entitas.CodeGeneration.Attributes/EventAttribute.cs` and `gen/Entitas.CodeGeneration/Components/Data/EventData.cs` define parallel `EventTarget` and `EventType` enums. Do not add a generator dependency on the attributes package unless packaging is intentionally changed. Prefer tests or explicit mapping to prevent ordinal drift.
   Verification: add a test parsing `[Event(EventTarget.Self, EventType.Removed)]` and asserting generator `EventData` values.

6. Split `gen/Entitas.CodeGeneration/Components/ComponentTemplates.cs` only after output stability coverage exists.
   Suggested split: handle templates, schema registration templates, context API templates, entity API templates, matcher templates.
   Verification: full code-generation tests and generated output comparisons.

## Phase 3: Unity, Samples, And Infrastructure

1. Clarify the editor-to-attributes dependency.
   `src/Entitas.Unity.Editor/Entitas.Unity.Editor.csproj` references `Entitas.CodeGeneration.Attributes`, and editor code currently uses `DontDrawComponentAttribute` and `ContextAttribute`. Keep the dependency for now, but document that this package contains runtime/editor metadata attributes as well as generator markers.
   Verification: `dotnet build Entitas.sln -c Release` and Unity editor smoke test.

2. Rename vendored compatibility namespaces after the next stable checkpoint.
   Compatibility code under `src/Entitas.Unity.Editor/Compatibility/DesperateDevs.*` is vendored, not external, but the namespace keeps the old dependency name visible. Rename to `Entitas.Unity.Editor.Compatibility` or similar in one mechanical pass.
   Verification: `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj` and Unity editor compile.

3. Align CI Unity DLL resolution with repo defaults.
   `.github/workflows/build.yml` still passes explicit Unity paths even though `Directory.Build.props` resolves vendored Unity DLLs by default. Simplify CI only after confirming Linux path resolution remains stable.
   Verification: CI build, test, publish, and pack sequence.

4. Clean up Unity sample reproducibility.
   `samples/Unity/Assets/Entitas/*.dll` are tracked binaries. Decide whether the sample should keep checked-in release DLLs or restore/copy them from build artifacts. If keeping binaries, document the refresh command and version source.
   Verification: clean clone sample opens and generator runs.

5. Add a local `samples/Unity/README.md`.
   Cover package layout, analyzer wiring, multi-assembly sample intent, how DLLs are refreshed, and which generated Unity files are intentionally ignored.

## Phase 4: Test Organization

1. Split oversized runtime test files by behavior area.
   Current large files include `ContextTests.cs`, `EntityTests.cs`, `MatcherTests.cs`, and `GroupTests.cs`. Split by lifecycle, component operations, group events, cache behavior, and exception behavior.
   Verification: no production changes; full `tests/Entitas.Tests` should remain green.

2. Add missing multi-assembly generator tests.
   Cover feature-owned matchers for components in feature assemblies, multi-context components, and context-root assemblies consuming feature-owned plain APIs.
   Verification: focused tests in `tests/Entitas.CodeGeneration.Tests`.

3. Add Unity sample workflow validation to the roadmap checklist once reproducibility is decided.
   This matches the existing unchecked roadmap item for validating the Unity sample workflow.

## Suggested Execution Order

1. Phase 0 hygiene and generator-output stability checks.
2. Runtime entity-index duplication cleanup.
3. Generator extension naming and tuple input cleanup.
4. Analyzer option cleanup and event enum guard tests.
5. Unity sample reproducibility documentation.
6. Larger template/test-file splits.

## Verification Matrix

- Core runtime changes: `dotnet test tests/Entitas.Tests/Entitas.Tests.csproj`.
- Generator changes: `dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj`.
- Unity-facing changes: `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj`.
- Release-level confidence: `dotnet build Entitas.sln -c Release`, `dotnet test Entitas.sln -c Release --no-build`, `dotnet publish Entitas.sln -c Release --no-build`, `dotnet pack Entitas.sln -c Release --no-build`.

## Findings Intentionally Not Treated As Refactors

- `ComponentHandle<TComponent>.AssignIndex()` already prevents reassignment to a different index. Its idempotent same-index assignment appears intentional and should not be changed without a specific bug.
- `Entitas.Unity.Editor` does use code-generation attributes at runtime/editor time for `DontDrawComponentAttribute` and `ContextAttribute`, so removing that project reference is not currently a safe cleanup.
- Unity `Library`, `UserSettings`, and `obj` folders are already ignored globally. The tracked sample concern is the checked-in DLLs under `samples/Unity/Assets/Entitas`.

## Optional API Simplification Ideas Requiring Manual Approval

These are intentionally outside the behavior-preserving refactor plan.

1. Rename `Contexts` to `ContextRegistry` or `ContextContainer`.
   Benefit: clearer new-user mental model.
   Cost: breaking generated/user API.

2. Revisit `IContext` and `IContext<TEntity>` layering.
   Benefit: simpler public interface model.
   Cost: breaking or subtle generic API changes.

3. Decide whether `IComponentHandle` should be public.
   Benefit: smaller public surface if handles are generated-code infrastructure only.
   Cost: may break users relying on handle-based APIs.

4. Replace legacy scripting-define menu toggles with generator/analyzer-config-first settings.
   Benefit: aligns Unity editor controls with the Roslyn generator path.
   Cost: possible workflow break for users relying on `ENTITAS_DISABLE_VISUAL_DEBUGGING`, `ENTITAS_DISABLE_DEEP_PROFILING`, or `ENTITAS_FAST_AND_UNSAFE`.

5. Rework `Systems` into more specialized containers or fluent builders.
   Benefit: cleaner lifecycle separation.
   Cost: breaking or high-churn API change for minimal runtime benefit.

6. Replace generator string templates with a stronger template abstraction.
   Benefit: fewer magic replacement keys and safer generated output changes.
   Cost: large rewrite risk; only worth it if generator work expands significantly.
