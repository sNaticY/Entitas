# Entitas 2.0

Entitas is a C# Entity Component System framework for Unity and .NET. The 2.0 line is Unity-first and uses a Roslyn incremental source generator instead of Jenny or the legacy Entitas generator projects.

## Current Status

- The canonical generator path is `gen/Entitas.CodeGeneration` plus `src/Entitas.CodeGeneration.Attributes`.
- The legacy `Entitas.Generators` and `Entitas.Generators.Attributes` projects have been removed from this repo.
- Runtime, Unity runtime helpers, Unity editor/visual-debugging code, generator tests, and a Unity sample are included.
- CI builds and tests against Unity `6000.3.4f1` DLL references and the .NET SDK.
- A finalized UPM package layout and a full Entitas 1 to 2 migration guide are not finished in this repo yet.
- Older Jenny-era docs and wiki material should be treated as historical unless they explicitly mention the Entitas 2.0 incremental generator.

## Repo Layout

| Path | Purpose |
| --- | --- |
| `src/Entitas` | Core ECS runtime. |
| `gen/Entitas.CodeGeneration` | Roslyn incremental generator. |
| `src/Entitas.CodeGeneration.Attributes` | Attributes consumed by the generator. |
| `src/Entitas.Unity` | Optional Unity runtime helpers. |
| `src/Entitas.Unity.Editor` | Optional Unity editor and visual-debugging integration. |
| `tests/Entitas.Tests` | Core runtime tests. |
| `tests/Entitas.CodeGeneration.Tests` | End-to-end generator tests. |
| `tests/Entitas.Unity.Tests` | Unity integration tests. |
| `samples/Unity` | Manual Unity sample project using the current generator path. |

## Requirements

- Use Unity 6 style Roslyn analyzer support for Unity projects.
- The repo build and sample project currently target Unity `6000.3.4f1`.
- Use the .NET 8 SDK for building and testing the solution.
- Runtime libraries target `netstandard2.1`; the generator and generator attributes target `netstandard2.0` for Unity compatibility.

## Unity Setup

Add the runtime and generator pieces to your Unity project with the same split used by `samples/Unity/Assets/Entitas`.

| Assembly or source | Unity role |
| --- | --- |
| `Entitas.dll` or `src/Entitas` | Required runtime ECS API. |
| `Entitas.CodeGeneration.Attributes.dll` or `src/Entitas.CodeGeneration.Attributes` | Required normal reference for component and context attributes. |
| `Entitas.CodeGeneration.dll` built from `gen/Entitas.CodeGeneration` | Required Roslyn analyzer/source generator. Do not reference it from gameplay code. |
| `Entitas.Unity.dll` or `src/Entitas.Unity` | Optional Unity runtime helpers such as entity links and debug systems. |
| `Entitas.Unity.Editor.dll` or `src/Entitas.Unity.Editor` | Optional editor and visual-debugging integration. Keep it editor-only. |

In Unity, mark the `Entitas.CodeGeneration.dll` plugin with the `RoslynAnalyzer` label and keep it out of normal runtime/editor plugin references. Gameplay code should reference `Entitas` and `Entitas.CodeGeneration.Attributes`, not `Entitas.CodeGeneration`.

If you use analyzer-config options in Unity, include a `csc.rsp` like the sample project does:

```text
-analyzerconfig:.editorconfig
```

## Generator Options

The generator runs for attached compilations by default. Use `.editorconfig` only when you need assembly filtering, visual-debugging control, or feature toggles.

```ini
[*.cs]
entitas_generator.assembly_names = Assembly-CSharp, Game.Gameplay
entitas_generator.visual_debugging = true
entitas_generator.visual_debugging.assembly_names = Assembly-CSharp, Game.Gameplay
```

| Option | Default | Purpose |
| --- | --- | --- |
| `entitas_generator.assembly_names` | all assemblies | Optional include list for generator execution. Commas and semicolons are both accepted. |
| `entitas_generator.visual_debugging` | `true` | Enables generated Unity visual-debugging helpers. |
| `entitas_generator.visual_debugging.assembly_names` | `Assembly-CSharp` | Assemblies that receive visual-debugging helpers. |
| `entitas_generator.context.context` | `true` | Generates context classes such as `MainContext`. |
| `entitas_generator.context.entity` | `true` | Generates entity classes such as `MainEntity`. |
| `entitas_generator.context.matcher` | `true` | Generates context matcher roots such as `MainMatcher`. |
| `entitas_generator.context.component_index` | `true` | Participates in component lookup generation. |
| `entitas_generator.component.component_index` | `true` | Participates in component lookup and handle assignment generation. |
| `entitas_generator.component.context_extension` | `true` | Generates unique-component context APIs and `Contexts.GetMain()` accessors. |
| `entitas_generator.component.entity_extension` | `true` | Generates entity component APIs such as `AddUser`. |
| `entitas_generator.component.entity_index_extension` | `true` | Generates entity-index bootstrap and lookup APIs. |
| `entitas_generator.component.events` | `true` | Generates event listener components, listener interfaces, and event systems. |
| `entitas_generator.component.event_systems_extension` | `true` | Generates aggregate event system registration. |
| `entitas_generator.component.cleanup_systems` | `true` | Generates cleanup systems and cleanup system registration. |
| `entitas_generator.component.matcher` | `true` | Generates component matcher APIs. |

## Define Contexts

Create one marker attribute per context. The generator detects non-abstract classes that end with `Attribute` and derive directly from `Entitas.CodeGeneration.Attributes.ContextAttribute`.

```csharp
using Entitas.CodeGeneration.Attributes;

namespace MyGame;

public sealed class MainAttribute : ContextAttribute
{
    public MainAttribute() : base("Main") { }
}
```

This generates root types such as `MainContext`, `MainEntity`, `MainMatcher`, and `contexts.GetMain()`. These root context/entity/matcher types are generated in the global namespace.

## Define Components

Components must be non-abstract classes that end with `Component` and implement `Entitas.IComponent`. Public fields and public auto-properties become generated API parameters.

```csharp
using Entitas;
using Entitas.CodeGeneration.Attributes;
using MyGame;

namespace MyFeature;

[Main]
[Unique]
public sealed class LoadingComponent : IComponent { }

[Main]
[Unique]
public sealed class UserComponent : IComponent
{
    [PrimaryEntityIndex]
    public string Name;

    [EntityIndex]
    public int Age;
}
```

Use explicit context attributes such as `[Main]` for clarity. Components without a context attribute default to the logical `Game` context, which is only useful when your project also defines a `Game` context marker.

The generator currently consumes these attributes from `Entitas.CodeGeneration.Attributes`: `ContextAttribute`, derived context marker attributes such as `[Main]`, `Unique`, `Event`, `Cleanup`, `EntityIndex`, `PrimaryEntityIndex`, `FlagPrefix`, `DontGenerate`, and assembly-level `EntitasAssembly`.

## Generated API Shape

The generator runs during compilation. There is no separate Jenny or command-line generation step.

Depending on the attributes in your project, the generator produces APIs such as:

| Generated API | Notes |
| --- | --- |
| `MainContext`, `MainEntity`, `MainMatcher` | Root context surface for the `Main` context. |
| `contexts.GetMain()` | Extension method on the runtime `Entitas.Contexts` container. |
| `MainComponentsLookup` | Component lookup for single-assembly context-owned components. |
| `entity.AddUser("Alice", 42)` | Entity component API generated in the component namespace. |
| `entity.ReplaceUser(...)`, `entity.RemoveUser()`, `entity.GetUser()`, `entity.HasUser()` | Entity component helpers for member components. |
| `entity.SetLoading(true)`, `entity.IsLoading()` | Flag component helpers for memberless components. |
| `main.SetLoading(true)`, `main.IsLoading()` | Unique flag component context helpers. |
| `main.SetUser(...)`, `main.GetUser()`, `main.GetUserEntity()` | Unique member component context helpers when `[Unique]` is present. |
| `MainMatcher.MyFeatureUser()` | Namespace-safe matcher for namespaced context-owned components. |
| `MainEntityIndices` and `main.GetEntityWithMyFeatureUserName(...)` | Entity-index constants and lookup APIs. |
| `MainEventSystems`, `MainCleanupSystems` | Aggregate systems generated when components use `[Event]` or `[Cleanup]`. |

For namespaced components, direct entity and context extension methods are emitted into the component namespace and use short names such as `AddUser`, `SetUser`, and `SetLoading`. Shared artifacts remain namespace-safe and flattened, such as `MainComponentsLookup.MyFeatureUser`, `MainMatcher.MyFeatureUser()`, entity-index constants, event/listener type names, and cleanup system names.

## Runtime Bootstrap

Create and register contexts explicitly. Generated root properties like `contexts.main` are not part of the Entitas 2.0 runtime container.

```csharp
using Entitas;
using MyFeature;

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

Only call generated `Initialize{Context}EntityIndices()` methods for contexts that actually have generated entity indices. If you do not use `[EntityIndex]` or `[PrimaryEntityIndex]`, that file and method are not generated.

For multiple independent contexts, register each one and run each generated bootstrap method you need:

```csharp
var contexts = new Contexts()
    .Register(new MainContext())
    .Register(new ConfigContext());

contexts.InitializeMainEntityIndices();
contexts.InitializeConfigEntityIndices();

var main = contexts.GetMain();
var config = contexts.GetConfig();
```

If you use generated event or cleanup systems in a single assembly, add them to your system pipeline explicitly:

```csharp
var systems = new Systems()
    .Add(new MainEventSystems(contexts))
    .Add(new GameplaySystems(contexts))
    .Add(new MainCleanupSystems(contexts));
```

## Multi-Assembly Shared Contexts

The generator is compilation-scoped. A context root assembly owns `SharedContext` and `SharedEntity`; feature assemblies can contribute components to that shared context by referencing the root assembly and using its context marker attribute.

```csharp
using Entitas;
using Entitas.CodeGeneration.Attributes;
using Sample.MultiAssembly.Root;

[assembly: EntitasAssembly("Player")]

namespace Sample.MultiAssembly.FeatureA;

[Shared]
public sealed class LevelComponent : IComponent
{
    public int Value;
}
```

Feature assemblies generate handle-based component APIs, feature-owned matchers, and schema registration methods. For example, a `Player` feature assembly contributing `LevelComponent` to `SharedContext` generates `SharedPlayerMatcher.g.cs` plus `SharedPlayerMatcher.Level.g.cs`, and code can use `SharedPlayerMatcher.Level().Added()` normally.

The application composes the shared context schema explicitly:

```csharp
var schema = SharedContext.CreateSchemaBuilder()
    .AddPlayerAssembly()
    .AddHealthAssembly()
    .Build();

var contexts = new Contexts()
    .Register(new SharedContext(schema), schema);

var shared = contexts.GetShared();
```

`Contexts.Register(context, schema)` initializes entity indices registered in the schema. Event and cleanup systems registered through feature schema methods can be created from the schema:

```csharp
var systems = new Systems()
    .Add(schema.CreateEventSystems(contexts))
    .Add(new GameplaySystems(contexts))
    .Add(schema.CreateCleanupSystems(contexts));
```

There is intentionally no generated root `Contexts` container that merges every feature assembly. Cross-assembly composition stays application-owned through `ContextSchemaBuilder`, feature registration methods such as `AddPlayerAssembly()`, and explicit context registration.

See `samples/Unity/Assets/Sample/MultiAssembly/README.md` for the current Unity sample of this pattern.

## .NET Source References

For a non-Unity SDK-style project that consumes Entitas from source, reference the runtime and attributes normally and reference the generator as an analyzer:

```xml
<ItemGroup>
  <ProjectReference Include="../../src/Entitas/Entitas.csproj" />
  <ProjectReference Include="../../src/Entitas.CodeGeneration.Attributes/Entitas.CodeGeneration.Attributes.csproj" />
  <ProjectReference Include="../../gen/Entitas.CodeGeneration/Entitas.CodeGeneration.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

The generator is a compiler input, not a runtime dependency. Application code should use attributes from `Entitas.CodeGeneration.Attributes`, not types from `Entitas.CodeGeneration`.

## Build And Test

The default build resolves Unity DLLs from `unity/Unity-6000.3.4f1` when they are present.

```bash
dotnet build Entitas.sln -c Release
dotnet test Entitas.sln -c Release --no-build
```

Focused test commands:

```bash
dotnet test tests/Entitas.Tests/Entitas.Tests.csproj
dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj
dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj
```

The GitHub build workflow runs build, test, publish, pack, and coverage reporting. `CONTRIBUTING.md` still contains old `bee` and branch-flow instructions, so use `.github/workflows/build.yml` as the current automation source of truth.

## Migration Notes

- Replace `Entitas.Generators.Attributes` usages with `Entitas.CodeGeneration.Attributes`.
- Remove Jenny generation steps from the active workflow; generated code is produced by the compiler.
- Define context marker attributes in source instead of relying on old generator settings.
- Register contexts explicitly with `new Contexts().Register(new MainContext())`.
- Use generated accessors such as `contexts.GetMain()` instead of generated root properties.
- Initialize entity indices explicitly with generated `Initialize{Context}EntityIndices()` methods, or use `Contexts.Register(context, schema)` for schema-composed shared contexts.
- Expect direct APIs for namespaced components to use short names in the component namespace, while shared artifacts use flattened namespace-safe names.

## License

Entitas is available under the [MIT License](LICENSE.md).
