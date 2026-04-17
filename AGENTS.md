# AGENTS

## Source Of Truth
- Trust `.github/workflows/build.yml` over `CONTRIBUTING.md`. `CONTRIBUTING.md` still references missing `./Scripts/bee` commands and obsolete `master`/`develop` branch flow.
- Treat `notes/entitas-2.0-roadmap.md` as the persistent project memory for the 2.0 release vision, release blockers, and roadmap. Read it before making roadmap-level decisions.

## Verify Changes
- CI order is `dotnet build -c Release` -> `dotnet test -c Release --no-build` -> `dotnet publish -c Release --no-build` -> `dotnet pack -c Release --no-build`.
- Full solution build/test now resolves Unity DLLs from `unity/Unity-2021.3.0f1` by default, so these work without extra MSBuild properties:
  `dotnet build Entitas.sln -c Release`
  `dotnet test Entitas.sln -c Release --no-build`
- Passing explicit Unity DLL paths is still valid when needed:
  `dotnet build Entitas.sln -c Release -p:UnityEditor=unity/Unity-2021.3.0f1/UnityEditor.dll -p:UnityEngine=unity/Unity-2021.3.0f1/UnityEngine.dll`
  `dotnet test Entitas.sln -c Release --no-build -p:UnityEditor=unity/Unity-2021.3.0f1/UnityEditor.dll -p:UnityEngine=unity/Unity-2021.3.0f1/UnityEngine.dll`
- Focused checks that avoid Unity projects:
  `dotnet test tests/Entitas.Tests/Entitas.Tests.csproj`
  `dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj`
- Unity-facing changes should at least run:
  `dotnet test tests/Entitas.Unity.Tests/Entitas.Unity.Tests.csproj`
- Single-test pattern:
  `dotnet test tests/Entitas.CodeGeneration.Tests/Entitas.CodeGeneration.Tests.csproj --filter "FullyQualifiedName~GeneratorAssemblyBehaviorTests.GeneratesContextsForCustomAssemblyByDefault"`

## Repo Shape
- `src/Entitas` is the core ECS runtime.
- `gen/Entitas.CodeGeneration` is the new Roslyn incremental generator for Unity-first workflows.
- `src/Entitas.CodeGeneration.Attributes` holds attributes consumed by the incremental generator.
- `src/Entitas.Unity` and `src/Entitas.Unity.Editor` are optional Unity integration layers compiled against raw `UnityEngine.dll` / `UnityEditor.dll` references.
- `tests/*` mirrors the runtime, generator, and Unity split. `benchmarks/Entitas.Benchmarks` is the BenchmarkDotNet perf harness.
- `samples/Unity` is a manual Unity sample project; CI does not validate it.

## Generator And Test Gotchas
- `tests/Entitas.CodeGeneration.Tests` references `gen/Entitas.CodeGeneration` as an analyzer (`OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`) and references `src/Entitas.CodeGeneration.Attributes`; use it to verify end-to-end generated code, not just snapshots.
- The incremental generator now runs for attached compilations by default. Optional assembly filtering is analyzer-config driven through `entitas_generator.assembly_names`.
- The incremental generator now supports analyzer-config feature toggles for contexts, matchers, entity/context extensions, component lookups, events, cleanup, entity indices, and visual debugging. Prefer adding tests in `tests/Entitas.CodeGeneration.Tests` when changing that surface.
- `src/Entitas/Context/Contexts.cs` is now the runtime root context container. Do not reintroduce a generated root `Contexts.g.cs` container unless the design changes explicitly.
- Generated context access now uses extension methods like `contexts.GetMain()` rather than generated root properties like `contexts.main`.
- Generated entity-index setup is explicit bootstrap via methods like `InitializeMainEntityIndices()`, and generated keys live on per-context classes like `MainEntityIndices`, not on `Contexts`.
- The remaining multi-assembly design is runtime-bootstrap based: users register contexts into `Entitas.Contexts` explicitly.
- Namespaced component behavior is intentionally split:
  - direct entity/context APIs are emitted in the component namespace and use short names like `AddUser`, `SetUser`, `SetLoading`
  - shared global artifacts stay namespace-safe and flattened, such as `MainMatcher.MyFeatureUser()`, `MainComponentsLookup.MyFeatureUser`, entity-index constants, event/listener type names, and cleanup system class names
- For documentation changes, keep `README.md` Unity-first: prefer explaining Unity usage, incremental generator wiring, and migration from Entitas 1 over historical/community material.

## Style Constraints
- Respect `.editorconfig`: LF endings, 4 spaces by default, 2 spaces in `*.csproj`, tabs in `*.sln` and `*.DotSettings`.
- Target frameworks come from `Directory.Build.props`: libraries use `netstandard2.1`, tests and benchmarks use `net6.0`, and the generator uses `netstandard2.0`.
