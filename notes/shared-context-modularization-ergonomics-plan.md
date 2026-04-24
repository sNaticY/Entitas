# Shared Context Modularization Ergonomics Plan

This plan captures follow-up API improvements after the first shared-context modularization implementation.

Current state:
- the runtime/schema architecture works
- Unity sample multi-assembly edit-mode tests pass
- single-assembly generation remains supported
- modular shared-context usage is functional but too verbose in bootstrap and some generated API names

Primary goal:
- keep the modular architecture explicit and deterministic
- reduce user-written bootstrap noise
- make generated APIs feel close to normal single-assembly Entitas usage

Non-goals for this pass:
- do not add an "all features" aggregator
- do not add a general API name collision strategy yet
- do not make reflection/assembly scanning the default bootstrap path
- do not replace application-owned context factory/bootstrap code with generated global factories

## Agreed Improvements

### 1. Generate Feature-Level Schema Registration Methods

Problem:
- composing a schema by manually adding every component, index, event system, and cleanup system does not scale
- examples like this are acceptable for a tiny sample but bad for real projects:

```csharp
new ContextSchemaBuilder("Shared")
    .AddSharedPlayer()
    .AddSharedPlayerEntityIndices()
    .AddSharedHealth()
    .Build();
```

Desired API:

```csharp
SharedContext.CreateSchemaBuilder()
    .AddPlayerFeature()
    .AddCombatFeature()
    .Build();
```

Generator behavior:
- emit one feature-level registration method per context per feature assembly
- the generated method should call all relevant generated registrations from that assembly:
  - component handles
  - entity-index initializers
  - cleanup-system registrations
  - event-system registrations
- users compose feature assemblies, not individual components

Example generated shape:

```csharp
public static class SharedPlayerFeatureSchemaExtensions
{
    public static ContextSchemaBuilder AddPlayerFeature(this ContextSchemaBuilder builder)
    {
        return builder
            .AddSharedPlayer()
            .AddSharedPlayerEntityIndices()
            .AddSharedHealth();
    }
}
```

Implementation notes:
- this should be generated in the feature assembly
- it should only include outputs generated from components visible in that feature assembly
- it should preserve deterministic registration order
- it should not create a cross-feature aggregate owner assembly

Tests:
- generator test for one feature assembly producing one feature registration method
- generator test for two contexts in one feature assembly producing separate context-specific feature methods
- Unity sample edit-mode test should switch from per-component schema calls to feature-level calls

### 2. Add Explicit Feature Name Configuration

Problem:
- feature-level method names need good names
- deriving names from asmdefs can produce ugly APIs such as:

```csharp
AddEntitasSampleMultiAssemblyFeatureAFeature()
```

Desired user input:

```csharp
[assembly: Entitas.CodeGeneration.Attributes.EntitasFeature("Player")]
```

Desired generated API:

```csharp
.AddPlayerFeature()
```

Implementation notes:
- add a new attribute in `src/Entitas.CodeGeneration.Attributes`
- generator reads the assembly-level feature name
- if absent, fall back to sanitized assembly name
- if the feature name is empty or invalid, emit a useful diagnostic or fall back safely

Naming suggestion:
- `EntitasFeatureAttribute`
- constructor: `EntitasFeatureAttribute(string name)`

Tests:
- feature method uses explicit feature name
- feature method falls back to sanitized assembly name when attribute is missing
- invalid/empty feature name behavior is covered

### 3. Shorten Feature-Local Entity-Index API Names

Problem:
- current modular entity-index API names can be too long:

```csharp
shared.GetEntityWithSampleMultiAssemblyFeatureAPlayer("Ada");
```

Desired API:

```csharp
shared.GetEntityWithPlayerName("Ada");
```

For non-primary indices:

```csharp
shared.GetEntitiesWithPlayerTeam(teamId);
```

Design decision:
- use scoped component names for feature-local entity-index APIs
- include the member name consistently, even if the component has only one index
- do not solve cross-feature name collisions in this pass

Implementation notes:
- keep namespace-safe constants if needed internally
- shorten public extension method names for feature-local generated entity-index APIs
- single-assembly global entity-index behavior should not regress unless deliberately changed in a separate compatibility pass

Tests:
- generated modular index API uses `GetEntityWithPlayerName`
- generated modular non-primary index API uses `GetEntitiesWithPlayerTeam`
- current long method name is not emitted for the modular feature-local path
- Unity sample test should use the shortened API

### 4. Rename Sample Bootstrap To `ContextFactory`

Problem:
- `MultiAssemblyContextFactory` sounds like an Entitas concept or required architecture piece
- it is just user/application bootstrap code

Desired sample shape:

```csharp
public static class ContextFactory
{
    public static Contexts Create()
    {
        var schema = SharedContext.CreateSchemaBuilder()
            .AddPlayerFeature()
            .AddHealthFeature()
            .Build();

        return new Contexts()
            .Register(new SharedContext(schema), schema);
    }
}
```

Implementation notes:
- rename only the sample class and references
- keep it under the sample bootstrap namespace to avoid conflicts with user projects
- update scene controller and edit-mode test

Tests:
- Unity edit-mode multi-assembly sample test still passes

### 5. Generate Context Schema Starter Methods

Problem:
- this is stringly typed:

```csharp
new ContextSchemaBuilder("Shared")
```

Desired API:

```csharp
SharedContext.CreateSchemaBuilder()
```

Generated shape:

```csharp
public sealed partial class SharedContext
{
    public static ContextSchemaBuilder CreateSchemaBuilder()
    {
        return new ContextSchemaBuilder("Shared");
    }
}
```

Benefits:
- avoids context-name typos
- makes the modular bootstrap read as context-owned setup
- keeps factory/bootstrap user-owned

Tests:
- generated context exposes `CreateSchemaBuilder()`
- schema built from the generated starter can create a context
- existing default constructor still works for single-assembly use

### 6. Add One-Step Context Registration With Schema Initialization

Problem:
- current bootstrap has a footgun:

```csharp
var contexts = new Contexts()
    .Register(new SharedContext(schema));

schema.InitializeEntityIndices(contexts);
```

Users can forget to initialize schema residue such as entity indices.

Desired API:

```csharp
var contexts = new Contexts()
    .Register(new SharedContext(schema), schema);
```

Behavior:
- registers the context
- initializes schema-owned global residue that depends on `Contexts`
- initially this means entity indices
- future schema residue can also hook into this path if needed

Implementation notes:
- add overload on `Entitas.Contexts`
- keep existing `Register(context)` unchanged
- the schema should still be reusable for creating cleanup/event systems explicitly

Possible runtime shape:

```csharp
public Contexts Register<TContext>(TContext context, ContextSchema schema)
    where TContext : class, IContext
{
    Register(context);
    schema.InitializeEntityIndices(this);
    return this;
}
```

Tests:
- registering with schema initializes entity indices
- existing explicit `schema.InitializeEntityIndices(contexts)` path remains valid or is documented as lower-level
- duplicate registration behavior remains unchanged

### 7. Improve Modular Diagnostics

Problem:
- modular setup mistakes can currently surface as generic compile or runtime errors

Target diagnostics/runtime errors:
- feature assembly references a context attribute but the root context assembly is not referenced
- schema is missing a component handle used by generated APIs
- duplicate component names/types in a schema
- entity-index registration is added without the component handle registration
- default `SharedContext()` constructor is used accidentally for modular contexts that expect a runtime schema

Implementation notes:
- prefer generator diagnostics when the generator can know the problem
- otherwise improve runtime exception messages in `ComponentHandle`, `ContextSchemaBuilder`, and generated APIs
- keep diagnostics actionable: include context name, component name, feature name, and suggested fix

Tests:
- runtime tests for clearer missing/unassigned handle errors
- generator tests for missing root context references if feasible
- Unity sample should not emit diagnostics in the valid path

### 8. Hide Event/Cleanup/Index Details Behind Feature Methods

Problem:
- users should not need to know whether a feature contributes cleanup systems, event systems, or entity indices

Desired API:

```csharp
SharedContext.CreateSchemaBuilder()
    .AddPlayerFeature()
    .AddCombatFeature()
    .Build();
```

Not:

```csharp
builder
    .AddSharedPlayer()
    .AddSharedPlayerEntityIndices()
    .AddSomeCleanupSystem()
    .AddSomeEventSystem();
```

Implementation notes:
- this is mostly satisfied by the feature-level registration method
- lower-level generated methods can remain public for debugging or custom bootstrap, but samples/docs should teach feature-level composition

Tests:
- feature-level method registers all artifacts needed for the existing Unity multi-assembly sample test
- removing explicit per-component/per-index calls from sample bootstrap still passes

## Recommended Implementation Order

1. Add `EntitasFeatureAttribute` and generator parsing.
2. Generate feature-level schema registration methods.
3. Update Unity sample bootstrap to use feature-level methods.
4. Shorten modular entity-index API names.
5. Rename sample `MultiAssemblyContextFactory` to `ContextFactory`.
6. Generate `SharedContext.CreateSchemaBuilder()`.
7. Add `Contexts.Register(context, schema)` convenience overload.
8. Improve diagnostics around missing schema registration and modular setup mistakes.
9. Refresh docs and rerun Unity edit-mode tests from command line.

## Success Criteria

- sample bootstrap composes by feature, not by component
- sample factory is named `ContextFactory`
- modular index usage reads like `GetEntityWithPlayerName("Ada")`
- Unity edit-mode test still passes from command line
- single-assembly generator/runtime tests remain green
- docs clearly state which pieces are hand-written and which are generated
