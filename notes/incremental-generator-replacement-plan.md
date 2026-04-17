# Incremental Generator Replacement Plan

This note is a focused handoff for the next session.

Goal:
- make `gen/Entitas.CodeGeneration` fully replace `gen/Entitas.Generators`
- preserve the good parts of the new incremental generator
- bring over the operational strengths of the old Roslyn generator

## Key Findings

### Old generator strengths we should bring forward

1. It does not hardcode assembly names.
- The old generator runs in any compilation where the analyzer is attached.
- The new generator currently only runs for:
  - `Assembly-CSharp`
  - `Entitas.CodeGeneration.Tests`
  - `Entitas.CodeGeneration-Tests`

2. It supports analyzer-config based feature toggles.
- The old generator reads `.editorconfig` / analyzer config options such as:
  - `entitas_generator.component.cleanup_systems`
  - `entitas_generator.component.context_extension`
  - `entitas_generator.component.entity_extension`
  - `entitas_generator.component.entity_index_extension`
  - `entitas_generator.component.events`
  - `entitas_generator.component.event_systems_extension`
  - `entitas_generator.component.matcher`
  - `entitas_generator.context.component_index`
  - `entitas_generator.context.context`
  - `entitas_generator.context.entity`
  - `entitas_generator.context.matcher`

3. It is more semantic and less heuristic in some places.
- Old contexts are discovered from actual context types implementing `IContext`.
- Old component-to-context mapping uses real type references via `[Context(typeof(MainContext))]`.
- The new generator still relies on naming assumptions for derived context attributes.

### Things we do not need to bring back

1. We do not need to restore the old `ContextInitializationAttribute` workflow unless a real blocker appears.
- The new `Contexts` plus post-constructor approach is simpler.
- We only need to make it explicit and configurable enough.

2. We do not need to restore property-style flag APIs.
- The old generator already used extension-method APIs, not `entity.isFlag = true` style.
- Old flag API shape was already method-based:
  - entity: `AddFlag()`, `ReplaceFlag()`, `RemoveFlag()`, `HasFlag()`
  - unique context: `SetFlag()`, `UnsetFlag()`, `HasFlag()`

### Things the new generator should keep

1. Keep `Entitas.CodeGeneration.Attributes` as the primary namespace.
2. Keep the incremental generator architecture.
3. Keep the newer attribute surface where useful, including:
  - `FlagPrefixAttribute`
  - `DontGenerateAttribute`
  - `ComponentNameAttribute`
  - `PrimaryEntityIndexAttribute`
  - `PostConstructorAttribute`

## Recommended Next Implementation Steps

### Step 1: Remove hardcoded assembly filtering

Status:
- completed

Current file:
- `gen/Entitas.CodeGeneration/EntitasGenerator.cs`

Current problem:
- generator only runs for three hardcoded assembly names

Target:
- generator should run for any attached compilation by default
- optional assembly filtering can exist, but must be configuration-driven rather than mandatory and hardcoded

Completed in this session:
- generator now runs for any attached compilation by default
- optional assembly filtering is now config-driven through `entitas_generator.assembly_names`
- generation now skips empty compilations so unrelated Unity assemblies do not get invalid `Contexts.g.cs`

### Step 2: Add analyzer-config based generator options

Status:
- completed

Use the old generator as reference:
- `gen/Entitas.Generators/EntitasAnalyzerConfigOptions.cs`

Port the idea to the new generator.

Minimum toggles to add:
- contexts generation
- context matcher generation
- context entity generation
- component lookup generation
- entity extension generation
- context extension generation
- entity index generation
- cleanup generation
- events generation
- visual debugging generation

This should replace hardcoded switches like:
- `VisualDebuggingGenerationEnabled = true`

Completed in this session:
- added analyzer-config support for assembly filtering via `entitas_generator.assembly_names`
- added analyzer-config support for feature toggles covering:
  - `entitas_generator.component.cleanup_systems`
  - `entitas_generator.component.component_index`
  - `entitas_generator.component.context_extension`
  - `entitas_generator.component.entity_extension`
  - `entitas_generator.component.entity_index_extension`
  - `entitas_generator.component.events`
  - `entitas_generator.component.event_systems_extension`
  - `entitas_generator.component.matcher`
  - `entitas_generator.context.component_index`
  - `entitas_generator.context.context`
  - `entitas_generator.context.entity`
  - `entitas_generator.context.matcher`
- added analyzer-config support for visual debugging via:
  - `entitas_generator.visual_debugging`
  - `entitas_generator.visual_debugging.assembly_names`
- analyzer config is now resolved from compilation syntax-tree options instead of only global options, which is required for Unity `.editorconfig` usage
- added focused toggle coverage in `tests/Entitas.CodeGeneration.Tests` for the new option surface
- added focused visual debugging toggle coverage

Still remaining:
- none for the current option surface; remaining follow-up is broader multi-assembly coverage rather than additional toggle plumbing

### Step 3: Make context parsing more semantic

Status:
- completed

Current file:
- `gen/Entitas.CodeGeneration/Contexts/ContextGenerationHelper.cs`

Current problem:
- context names are inferred largely from class names ending in `Attribute`
- there is commented-out code showing a more semantic approach was already considered

Target:
- parse context marker attributes semantically
- avoid relying only on class-name stripping
- make context discovery robust for namespaced and custom setups

Completed in this session:
- generator now resolves context names from the `ContextAttribute` base-constructor argument instead of relying only on the marker class name
- added focused coverage for aliased context marker names
- added dedicated namespaced-context coverage for generated context surfaces and accessors

### Step 4: Define the official assembly behavior

Status:
- partially implemented

We need an explicit rule for:
- default Unity `Assembly-CSharp`
- one custom asmdef
- multiple asmdefs
- test assemblies

Target:
- the generator should work when attached to a custom assembly without requiring source changes

Current clarified behavior:
- custom asmdefs now work by default without requiring `Assembly-CSharp`
- generation is compilation-scoped, so each assembly currently generates its own `Contexts` based only on the contexts discovered in that compilation
- two different assemblies defining two different contexts will not automatically get one merged cross-assembly `Contexts`

Chosen direction:
- move `Contexts` into the core `Entitas` runtime instead of generating a separate root `Contexts` class per compilation
- treat `Contexts` as a typed registry/container owned by `Entitas`
- feature assemblies generate access extensions for the contexts they define, instead of trying to generate one merged cross-assembly `Contexts`
- users explicitly create and register contexts during bootstrap

Proposed user-facing model:
- `Entitas` provides the base `Contexts` runtime API
- each feature assembly can define its own contexts and generate local accessors such as `GetMain(this Contexts contexts)`
- bootstrap code manually creates `MainContext`, `UiContext`, etc. and registers them into one `Contexts` instance
- systems in feature assemblies can depend on concrete context types directly, or use the shared `Contexts` plus generated extension methods

Why this direction:
- it matches Roslyn compilation boundaries better than cross-assembly aggregation
- it avoids requiring a special aggregate asmdef just to combine contexts
- it keeps multi-assembly projects explicit and predictable
- it is close to the old generator's concrete-context usage model while still giving users one shared `Contexts` object

Implementation implications:
- add `Contexts` to `src/Entitas`
- define registration and typed retrieval APIs in runtime, e.g. `Register<TContext>()` and `Get<TContext>()`
- stop generating the root `Contexts` container class in `gen/Entitas.CodeGeneration/Contexts/ContextTemplates.cs`
- start generating per-context extension accessors against runtime `Entitas.Contexts`
- keep context construction/registration explicit unless we later add an optional aggregate bootstrap feature

Completed in this session:
- added runtime `Entitas.Contexts` as the shared typed context container
- added explicit runtime registration/retrieval APIs and runtime tests
- stopped generating the root `Contexts.g.cs` container class
- switched generated context access to per-context extension methods such as `GetMain()`
- switched generated cleanup/event/entity-index flows to use runtime `Entitas.Contexts`
- switched generated entity-index initialization to explicit bootstrap extension methods like `InitializeMainEntityIndices()`
- removed the remaining generated-root `Contexts` assumption from visual debugging by moving it to explicit bootstrap extensions

Still remaining:
- decide whether to add optional convenience bootstrap helpers on top of explicit registration
- verify the new runtime `Contexts` model in downstream Unity sample usage

### Step 5: Add focused tests before removing the old generator

Status:
- completed

Primary test target:
- `tests/Entitas.CodeGeneration.Tests`

Add or expand coverage for:
- custom assembly names
- multiple contexts
- namespaced contexts/components
- unique flag components
- events
- cleanup systems
- entity indices
- visual debugging generation toggles

Do not remove the old generator until these pass.

Completed in this session:
- added tests covering custom assembly names
- added tests covering explicit assembly filtering
- added tests confirming empty compilations generate nothing
- added tests covering explicit bootstrap with multiple registered contexts
- added tests covering explicit entity-index initialization across multiple generated contexts
- added focused tests for analyzer-config feature toggles
- added focused tests for visual debugging toggles
- added explicit namespaced API-shape tests for namespaced components
- updated the existing code-generation integration fixture to validate the new namespace behavior end to end

Still remaining:
- none for the current replacement scope inside the solution

Note:
- multiple generated contexts are now covered inside one compilation/bootstrap flow, and per-compilation multi-assembly behavior is covered explicitly through focused generator tests

Additional behavior clarified in this session:
- for namespaced components, direct context/entity APIs are now emitted inside the component namespace and use short names:
  - `SetUser`, `AddUser`, `GetUser`, `SetLoading`
- shared global artifacts remain namespace-safe and flattened to avoid collisions:
  - `MainMatcher.MyFeatureUser()`
  - `MainComponentsLookup.MyFeatureUser`
  - entity-index constants and accessors
  - event/listener type names
  - cleanup system class names

### Step 6: Migrate downstream repo usage

Status:
- partially completed

After the new generator is operationally complete:
- migrate `samples/Unity`
- migrate remaining Unity/editor references
- remove old generator references from sample and editor code

Completed in this session:
- migrated `src/Entitas.Unity.Editor` from `Entitas.Generators.Attributes` to `Entitas.CodeGeneration.Attributes`
- removed legacy generator test projects from the solution
- removed `gen/Entitas.Generators`
- removed `src/Entitas.Generators.Attributes`

Still remaining:
- migrate `samples/Unity`
- replace remaining sample-only uses of old generator attributes and initialization flow

## Suggested Execution Order For Next Session

1. Verify and migrate the new runtime `Entitas.Contexts` bootstrap model in downstream Unity/sample usage.
2. Replace remaining sample-only uses of `Entitas.Generators.Attributes` and `ContextInitialization`.
3. Update upgrade/docs material to describe the old generator removal and the 2.0 migration path.
4. Report remaining blockers for package/analyzer distribution and Unity validation.

## Suggested New Session Prompt

```md
Please continue the Entitas 2.0 generator replacement work.

Read these first:
- `notes/entitas-2.0-roadmap.md`
- `notes/incremental-generator-replacement-plan.md`

Goal for this session:
- continue the post-replacement cleanup now that `gen/Entitas.Generators` has been removed

Immediate priorities:
1. verify downstream Unity/sample usage of runtime `Entitas.Contexts` bootstrap
2. replace remaining sample-only references to `Entitas.Generators.Attributes`
3. update migration/docs material to reflect the legacy generator removal

Important constraints:
- keep `Entitas.CodeGeneration.Attributes` as the primary path
- do not reintroduce old property-style APIs
- prefer the smallest correct changes
```
