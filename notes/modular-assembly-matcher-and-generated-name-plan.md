# Modular Assembly Matcher And Generated Name Plan

This plan captures follow-up cleanup after introducing modular shared-context matcher support.

Current state:
- single-assembly/root-context matcher APIs still use normal Entitas shape such as `GameMatcher.Player()`
- feature-owned modular components now get matcher APIs that work across asmdef boundaries
- current modular matcher generation emits one matcher class per component, such as `SharedPlayerMatcher`
- some generated hint names are unnecessarily long, such as `Sample.MultiAssembly.FeatureA.SharedSampleMultiAssemblyFeatureAPlayerComponent.g.cs`

Primary goal:
- preserve normal single-assembly Entitas ergonomics
- make modular shared-context matcher APIs less noisy
- reduce generated type/file clutter in feature assemblies
- keep cross-assembly ownership explicit and deterministic

Non-goals:
- do not generate `SharedMatcher.Player()` for feature-owned modular components
- do not introduce an aggregate owner assembly
- do not make the root context assembly reference feature assemblies
- do not change single-assembly/root-context matcher behavior
- do not add broad collision strategy beyond what is needed for the generated names touched here

## Agreed API Shape

### Single-Assembly Or Root-Context Components

This behavior remains unchanged.

```csharp
GameMatcher.Position().Added();
SharedMatcher.Player().Added();
MainMatcher.User();
```

Applies when:
- the component is generated in the same assembly as the context root, or
- the project is a normal single-assembly setup

### Modular Feature-Owned Components

Feature-owned components should use one matcher class per context per feature assembly.

```csharp
SharedPlayerMatcher.Player().Added();
SharedPlayerMatcher.PlayerBuff().Added();
SharedCombatMatcher.AttackIntent().Added();
```

Where `Player` / `Combat` comes from:

```csharp
[assembly: Entitas.CodeGeneration.Attributes.EntitasAssembly("Player")]
```

Fallback when `EntitasAssemblyAttribute` is absent:
- use the sanitized assembly name, matching the assembly-level schema registration fallback

## Why Not `SharedMatcher.Player()` In Modular Mode

`SharedMatcher.Player()` cannot be generated safely for feature-owned modular components under the current no-aggregator architecture.

Reasons:
- `SharedMatcher` is generated in the root context assembly
- feature assemblies compile separately
- C# partial types cannot be extended across assemblies
- extension methods cannot add static methods callable as `SharedMatcher.Player()`
- generating exact `SharedMatcher.Player()` would require either an aggregate assembly or root-to-feature references

Therefore the modular API keeps assembly ownership visible:

```csharp
SharedPlayerMatcher.Player().Added();
```

## Implementation Tasks

### 1. Aggregate Feature-Owned Matchers By Assembly

Problem:
- current modular matcher path generates one matcher class per component
- this creates too many generated classes and files

Desired generated shape:

```csharp
namespace Sample.MultiAssembly.FeatureA
{
    public static class SharedPlayerMatcher
    {
        static IMatcher<SharedEntity> _matcherPlayer;
        static IMatcher<SharedEntity> _matcherPlayerBuff;

        public static IMatcher<SharedEntity> Player()
        {
            // uses SharedPlayerComponentHandle.Handle
        }

        public static IMatcher<SharedEntity> PlayerBuff()
        {
            // uses SharedPlayerBuffComponentHandle.Handle
        }
    }
}
```

Generator behavior:
- emit one matcher class per context per feature assembly
- class name: `{ContextName}{AssemblyName}Matcher`
- method name: scoped component name, such as `Player()`
- matcher creation should use component handles, not root components lookup constants
- generated class should live in the feature assembly namespace when components are namespaced
- preserve deterministic method ordering

Compatibility requirement:
- root-context and single-assembly components must continue generating normal `{ContextName}Matcher.Component()` APIs

Tests:
- generator test: two components in one feature assembly produce one matcher class with two methods
- generator test: feature-owned matcher class uses assembly name from `[assembly: EntitasAssembly("Player")]`
- generator test: fallback sanitized assembly name is used when attribute is absent
- generator test: single-assembly/root-context matcher still uses `{ContextName}Matcher.Component()`
- generator compile test: reactive system compiles with `{ContextName}{AssemblyName}Matcher.Component().Added()`

### 2. Update Unity Multi-Assembly Sample System

Current sample should demonstrate a real feature-owned system.

Desired sample shape:

```csharp
return context.CreateCollector(SharedPlayerMatcher.Player().Added());
```

After aggregation, ensure the sample still uses:

```csharp
SharedPlayerMatcher.Player().Added()
```

but now `SharedPlayerMatcher` is the per-assembly matcher class, not a per-component generated class.

Tests:
- Unity edit-mode sample test executes a feature-owned `ReactiveSystem`
- test verifies adding `Player` triggers the system

### 3. Shorten Generated Hint Names

Problem:
- current generated hint names can be too long and hard to inspect

Bad example:

```text
Sample.MultiAssembly.FeatureA.SharedSampleMultiAssemblyFeatureAPlayerComponent.g.cs
```

Preferred common case:

```text
Sample.MultiAssembly.FeatureA.PlayerComponent.g.cs
```

Guidelines:
- for component-owned files, prefer the real component namespace and short component type name
- keep context/artifact suffixes only when required for correctness or collision avoidance
- preserve collision safety for multi-context components
- do not change public API names just to shorten hint names

Possible naming shape:
- single-context component APIs: `{Namespace}.{ShortTypeName}.g.cs`
- multi-context component APIs: `{Namespace}.{ContextName}.{ShortTypeName}.g.cs`
- assembly matcher APIs: `{Namespace}.{ContextName}{AssemblyName}Matcher.g.cs`
- entity-index registrations: `{Namespace}.{ContextName}{ScopedComponentName}EntityIndices.g.cs` unless safely shorten-able

Tests:
- generator test asserts a namespaced single-context feature component produces `Game.Feature.UserComponent.g.cs`
- generator test asserts multi-context component hint names remain distinct
- existing generated-source lookup tests should not depend on old long names

## Recommended Implementation Order

1. Refactor feature-owned matcher generation to group components by context and assembly name.
2. Update generator tests for aggregated matcher output and reactive-system compile coverage.
3. Update Unity sample feature-owned system and edit-mode test if needed.
4. Shorten component-owned generated hint names.
5. Add generated filename tests for single-context and multi-context cases.
6. Refresh Unity sample DLLs.
7. Run .NET validation and Unity batch-mode edit-mode tests if Unity is available.

## Success Criteria

- default single-assembly matcher APIs still use `{ContextName}Matcher.Component()`
- root-context assembly components still use `{ContextName}Matcher.Component()`
- modular feature-owned components use `{ContextName}{AssemblyName}Matcher.Component()`
- one matcher class is generated per context per feature assembly, not per component
- Unity multi-assembly sample has a feature-owned system test using the modular matcher
- generated hint names are substantially shorter in the common single-context component case
- full .NET tests pass
- Unity edit-mode tests pass when Unity is available
