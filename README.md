# Entitas 2.0

Entitas is an ECS framework for C# and Unity. This branch is primarily aimed at Unity projects and uses a Roslyn incremental generator instead of the old external code generation workflow.

## Repo layout

- `src/Entitas`: core ECS runtime
- `gen/Entitas.CodeGeneration`: Roslyn incremental generator
- `src/Entitas.CodeGeneration.Attributes`: attributes consumed by the incremental generator
- `src/Entitas.Unity` and `src/Entitas.Unity.Editor`: optional Unity integration and visual debugging

## Unity quick start

If you are using Entitas for a Unity game, this is the main workflow to follow.

### Requirements

- Unity runtime/editor integration in this repo is validated against Unity `2021.3.0f1`
- Incremental source generator support is intended for Unity 6 style Roslyn analyzer integration
- If you are not using `Assembly-CSharp`, update the assembly-name filter in `gen/Entitas.CodeGeneration/EntitasGenerator.cs`

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

By default the generator only runs for `Assembly-CSharp`. That means your context markers and component declarations should live in `Assembly-CSharp`, or you need to extend the filter in `EntitasGenerator.cs` for your own asmdef names.

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
- `Contexts`
- `MainComponentsLookup`
- `entity.AddMyFeatureUser(...)`
- `entity.ReplaceMyFeatureUser(...)`
- `context.SetMyFeatureUser(...)`
- `context.SetMyFeatureLoading(true)`
- `context.IsMyFeatureLoading()`
- `MainEventSystems`
- `MainCleanupSystems`

### 7. Use the generated API

```csharp
var contexts = new Contexts();
var main = contexts.main;

var entity = main.CreateEntity();
entity.AddMyFeatureUser("Alice", 42);

main.SetMyFeatureLoading(true);

if (main.IsMyFeatureLoading())
{
    var user = main.GetMyFeatureUser();
}
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

The incremental generator is the main change in this branch.

- It runs inside the compiler as a Roslyn incremental analyzer.
- It removes the old manual or external code generation step.
- It generates contexts, entities, component helpers, entity indices, event systems, cleanup systems, and Unity visual debugging support.
- It relies on `Entitas.CodeGeneration.Attributes`, not `Entitas.Generators.Attributes`.

### Current limitations

The generator currently only runs for assemblies named:

- `Assembly-CSharp`
- `Entitas.CodeGeneration.Tests`
- `Entitas.CodeGeneration-Tests`

If your Unity project uses custom asmdefs, update the `shouldRun` check in `gen/Entitas.CodeGeneration/EntitasGenerator.cs`.

Unity-side generator usage is also tied to Unity's Roslyn analyzer/source-generator support, so treat Unity 6 as the intended path for the incremental generator itself.

## Entitas 1 to 2 migration notes

These are the high-level API changes compared to the old Entitas 1 generator workflow.

### 1. Generator attribute namespace changed

Before:

```csharp
using Entitas.Generators.Attributes;
```

Now:

```csharp
using Entitas.CodeGeneration.Attributes;
```

### 2. Context declaration changed

Entitas 1 used context attributes that referenced a generated context type directly.

Before:

```csharp
[Context(typeof(MainContext))]
```

Entitas 2 uses small marker attributes derived from `ContextAttribute`, then applies those markers to components.

Now:

```csharp
public sealed class MainAttribute : ContextAttribute
{
    public MainAttribute() : base("Main") { }
}

[Main]
public sealed class UserComponent : IComponent { }
```

### 3. Context initialization attributes are gone

Entitas 1 used `ContextInitializationAttribute` and partial initialization methods.

Before:

```csharp
[ContextInitialization(typeof(MainContext))]
public static partial void InitializeMain();
```

That explicit initialization step is no longer the primary workflow. The generated `Contexts` type now acts as the root entry point, and post-constructor setup is generated automatically where needed.

### 4. Generated type names are flatter

Older generated APIs leaned on nested namespaces and types such as:

- `MyApp.Main.Entity`
- `MyApp.Main.ComponentIndex`

The incremental generator now produces flatter names such as:

- `MainEntity`
- `MainContext`
- `MainComponentsLookup`
- `Contexts`

### 5. Generated helper names are more explicit

Generated API names now include the feature or namespace prefix to avoid collisions.

Before:

- `AddUser`
- `SetUser`
- `HasUser`

Now:

- `AddMyFeatureUser`
- `SetMyFeatureUser`
- `HasMyFeatureUser`

### 6. Unique flag helpers changed shape

For unique flag components, the generator now produces boolean-style setters and checks.

Before:

- `SetLoading()`
- `UnsetLoading()`
- `HasLoading()`

Now:

- `SetMyFeatureLoading(true)`
- `SetMyFeatureLoading(false)`
- `IsMyFeatureLoading()`

### 7. Event, cleanup, and entity-index entry points changed

Older generated APIs were usually attached directly to a context instance.

Before:

- `context.AddAllEntityIndexes()`
- `context.CreateEventSystems()`
- `context.CreateCleanupSystems()`

Now the generated root object is `Contexts`, and generated systems take that root object:

- `new Contexts()`
- `new MainEventSystems(contexts)`
- `new MainCleanupSystems(contexts)`

Entity indices are also initialized through the generated `Contexts` flow, and generated keys are exposed on `Contexts`, for example:

- `Contexts.MyFeatureUserName`
- `Contexts.MyFeatureUserAge`

## License

Entitas is available under the [MIT License](LICENSE.md).
