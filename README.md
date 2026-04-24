# Entitas 2.0

Entitas is an ECS framework for C# and Unity. This branch is primarily aimed at Unity projects and uses a Roslyn incremental generator as the canonical code generation path.

## Repo layout

- `src/Entitas`: core ECS runtime
- `gen/Entitas.CodeGeneration`: Roslyn incremental generator
- `src/Entitas.CodeGeneration.Attributes`: attributes consumed by the incremental generator
- `src/Entitas.Unity` and `src/Entitas.Unity.Editor`: optional Unity integration and visual debugging

The legacy `Entitas.Generators` and `Entitas.Generators.Attributes` projects have been removed from the repo. `Entitas.CodeGeneration` plus `Entitas.CodeGeneration.Attributes` is now the only in-repo generation path.

## Unity quick start

If you are using Entitas for a Unity game, this is the main workflow to follow.

### Requirements

- Unity runtime/editor integration in this repo is validated against Unity `6000.3.4f1`
- Incremental source generator support is intended for Unity 6 style Roslyn analyzer integration
- The incremental generator now runs for attached compilations by default; use analyzer config only if you want to filter assemblies explicitly

### 1. Add Entitas runtime code to your Unity project

At minimum you need:

- `src/Entitas`
- `src/Entitas.Unity`
- `src/Entitas.Unity.Editor` if you want visual debugging and editor integration
- `src/Entitas.CodeGeneration.Attributes`
- the built analyzer from `gen/Entitas.CodeGeneration`

If you are consuming Entitas from source outside Unity, use the project references shown below. Inside Unity, the same split still applies conceptually: runtime code is referenced normally, while `Entitas.CodeGeneration` must be loaded as a Roslyn analyzer rather than as a gameplay assembly.

### 2. Make the incremental generator available to Unity

The generator in `gen/Entitas.CodeGeneration` is a compiler analyzer, not a runtime dependency.

In practice that means:

- compile `Entitas.CodeGeneration` into a DLL
- compile `Entitas.CodeGeneration.Attributes` into a DLL or include its source in your project
- add the generator DLL to Unity as a Roslyn analyzer package/plugin
- do not reference the generator DLL from gameplay code directly

Your gameplay code should only reference the attribute types from `Entitas.CodeGeneration.Attributes`.

### 3. Put generated-code inputs in the Unity gameplay assembly

Put your context markers and component declarations in the gameplay assemblies where they belong. The generator runs per compilation, so each asmdef only generates code for the contexts and components visible in that asmdef.

If you want to restrict generation to specific assemblies, use analyzer config, for example:

```ini
[*.cs]
entitas_generator.assembly_names = Assembly-CSharp, Game.Gameplay
```

### 4. Declare your contexts

Create one attribute per context by deriving from `Entitas.CodeGeneration.Attributes.ContextAttribute`:

```csharp
using Entitas.CodeGeneration.Attributes;

namespace MyGame;

public sealed class MainAttribute : ContextAttribute
{
    public MainAttribute() : base("Main") { }
}
```

### 5. Declare components with generator attributes

```csharp
using Entitas;
using Entitas.CodeGeneration.Attributes;
using MyGame;

namespace MyFeature;

[Main]
[Unique]
public sealed class LoadingComponent : IComponent { }

[Main]
public sealed class UserComponent : IComponent
{
    [PrimaryEntityIndex]
    public string Name;

    [EntityIndex]
    public int Age;
}
```

### 6. Let Unity compile

The incremental generator runs as part of compilation. There is no separate code generation step.

The component declarations above generate APIs such as:

- `MainContext`
- `MainEntity`
- `GetMain()` on `Entitas.Contexts`
- `MainComponentsLookup`
- `entity.AddUser(...)`
- `entity.ReplaceUser(...)`
- `context.SetUser(...)`
- `context.SetLoading(true)`
- `context.IsLoading()`
- `MainMatcher.MyFeatureUser()`
- `MainEventSystems`
- `MainCleanupSystems`

For namespaced components, Entitas now splits the generated API shape deliberately:

- direct context and entity APIs are emitted in the component namespace and use short names such as `AddUser`, `SetUser`, and `SetLoading`
- shared global artifacts remain namespace-safe and flattened, such as `MainComponentsLookup.MyFeatureUser`, `MainMatcher.MyFeatureUser()`, entity-index constants, and generated event/listener system type names

### 7. Use the generated API

```csharp
var contexts = new Contexts()
    .Register(new MainContext());

contexts.InitializeMainEntityIndices();

var main = contexts.GetMain();

var entity = main.CreateEntity();
entity.AddUser("Alice", 42);

main.SetLoading(true);

if (main.IsLoading())
{
    var user = main.GetUser();
}
```

For multiple contexts, register each one explicitly and run each generated bootstrap extension you need:

```csharp
var contexts = new Contexts()
    .Register(new MainContext())
    .Register(new ConfigContext());

contexts.InitializeMainEntityIndices();
contexts.InitializeConfigEntityIndices();

var main = contexts.GetMain();
var config = contexts.GetConfig();
```

## Source project setup

If you are consuming Entitas from source in a standard .NET project, wire your project like this:

```xml
<ItemGroup>
  <ProjectReference Include="../../src/Entitas/Entitas.csproj" />
  <ProjectReference Include="../../src/Entitas.CodeGeneration.Attributes/Entitas.CodeGeneration.Attributes.csproj" />
  <ProjectReference Include="../../gen/Entitas.CodeGeneration/Entitas.CodeGeneration.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

This is the same reference pattern used by `tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj`.

## Incremental generator

The incremental generator is the main generation path in this branch.

- It runs inside the compiler as a Roslyn incremental analyzer.
- It replaces the removed legacy generator path in this repo.
- It generates contexts, entities, component helpers, entity indices, event systems, cleanup systems, and Unity visual debugging support.
- It relies on `Entitas.CodeGeneration.Attributes`, not `Entitas.Generators.Attributes`.

### Current limitations

- The single-assembly workflow remains the default and best-documented path.
- The generator is compilation-scoped. If two different assemblies define two different contexts, each assembly generates its own local types and accessors from the source it can see.
- Shared-context modular assemblies use an explicit handle/schema path: feature assemblies generate handle-based component APIs plus assembly-level schema methods such as `AddPlayerAssembly()` that register component handles, event systems, cleanup systems, and entity indices into a runtime `ContextSchema`.
- Cross-assembly context composition stays application-owned through APIs such as `SharedContext.CreateSchemaBuilder()` and `new Contexts().Register(new SharedContext(schema), schema)`; there is still no generated root container that merges every feature assembly.

Unity-side generator usage is also tied to Unity's Roslyn analyzer/source-generator support, so treat Unity 6 as the intended path for the incremental generator itself.

## License

Entitas is available under the [MIT License](LICENSE.md).
