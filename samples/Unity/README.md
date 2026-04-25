# Entitas Unity Sample

This is a manual Unity sample for the Entitas 2.0 runtime and Roslyn incremental generator path. It is not part of the normal CI test matrix.

## Unity Version

- Target Unity version: `6000.3.4f1`.
- Generator analyzer config is enabled through `Assets/csc.rsp`, which points Unity compilation at `.editorconfig`.

## Entitas DLLs

`Assets/Entitas` contains checked-in DLL snapshots so the sample can open without first building the solution.

| DLL | Role |
| --- | --- |
| `Entitas.dll` | Runtime ECS API. |
| `Entitas.CodeGeneration.Attributes.dll` | Normal reference for context, component, event, cleanup, index, and editor metadata attributes. |
| `Entitas.CodeGeneration.dll` | Roslyn analyzer/source generator. Its `.meta` file must keep the `RoslynAnalyzer` label and plugin execution disabled. |
| `Entitas.Unity.dll` | Optional Unity runtime helpers. |
| `Entitas.Unity.Editor.dll` | Optional editor and visual-debugging integration. |

When the repo assemblies change, refresh these DLLs from a release build output and keep the `.meta` files intact.

```powershell
dotnet build Entitas.sln -c Release
dotnet publish Entitas.sln -c Release --no-build -p:UseAppHost=false -p:PublishDir=dist/Assemblies
```

## Multi-Assembly Layout

The sample exercises the shared-context, multi-assembly generator path.

| Assembly | Purpose |
| --- | --- |
| `Entitas.Sample.MultiAssembly.Root` | Owns shared context marker types and context-root generation. |
| `Entitas.Sample.MultiAssembly.FeatureA` | Defines feature-owned components and systems against the shared context. |
| `Entitas.Sample.MultiAssembly.FeatureB` | Defines another feature assembly against the shared context. |
| `Entitas.Sample.MultiAssembly.Bootstrap` | Registers contexts, schema, systems, and Unity bootstrap code. |

Generated Unity project files such as `.csproj`, `.sln`, `Library`, `Temp`, `UserSettings`, `obj`, and generated Entitas source output are intentionally ignored by the repo-level `.gitignore`.
