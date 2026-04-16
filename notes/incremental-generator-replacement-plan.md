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

Current file:
- `gen/Entitas.CodeGeneration/EntitasGenerator.cs`

Current problem:
- generator only runs for three hardcoded assembly names

Target:
- generator should run for any attached compilation by default
- optional assembly filtering can exist, but must be configuration-driven rather than mandatory and hardcoded

### Step 2: Add analyzer-config based generator options

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

### Step 3: Make context parsing more semantic

Current file:
- `gen/Entitas.CodeGeneration/Contexts/ContextGenerationHelper.cs`

Current problem:
- context names are inferred largely from class names ending in `Attribute`
- there is commented-out code showing a more semantic approach was already considered

Target:
- parse context marker attributes semantically
- avoid relying only on class-name stripping
- make context discovery robust for namespaced and custom setups

### Step 4: Define the official assembly behavior

We need an explicit rule for:
- default Unity `Assembly-CSharp`
- one custom asmdef
- multiple asmdefs
- test assemblies

Target:
- the generator should work when attached to a custom assembly without requiring source changes

### Step 5: Add focused tests before removing the old generator

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

### Step 6: Migrate downstream repo usage

After the new generator is operationally complete:
- migrate `samples/Unity`
- migrate remaining Unity/editor references
- remove old generator references from sample and editor code

Only then:
- remove `gen/Entitas.Generators`
- remove `src/Entitas.Generators.Attributes`

## Suggested Execution Order For Next Session

1. Implement analyzer-config support in `gen/Entitas.CodeGeneration`.
2. Remove hardcoded assembly-name filtering.
3. Add tests for configurable/multi-assembly behavior.
4. Refine semantic context parsing if needed to make those tests pass cleanly.
5. Report remaining blockers before touching samples or deleting old code.

## Suggested New Session Prompt

```md
Please continue the Entitas 2.0 generator replacement work.

Read these first:
- `notes/entitas-2.0-roadmap.md`
- `notes/incremental-generator-replacement-plan.md`

Goal for this session:
- improve `gen/Entitas.CodeGeneration` so it can replace `gen/Entitas.Generators`

Immediate priorities:
1. remove hardcoded assembly-name filtering from `gen/Entitas.CodeGeneration/EntitasGenerator.cs`
2. add analyzer-config based feature toggles similar to `gen/Entitas.Generators/EntitasAnalyzerConfigOptions.cs`
3. add or update tests in `tests/Entitas.CodeGeneration.Tests` for custom assembly / multiple assembly behavior

Important constraints:
- keep `Entitas.CodeGeneration.Attributes` as the primary path
- do not reintroduce old property-style APIs
- do not remove the old generator yet
- prefer the smallest correct changes
```
