# Incremental Generator Migration Plan

Context:
- Merge conflicts on `entitas-2.0-beta` were resolved and the merge commit is already done.
- This note is the handoff plan for evaluating `../Entitas-IncrementalGenerator` as a replacement for the current generator.

## Verified Facts

- `Entitas.sln` currently includes:
  - `gen/Entitas.Generators/Entitas.Generators.csproj`
  - `src/Entitas.Generators.Attributes/Entitas.Generators.Attributes.csproj`
- The sibling repo contains a real Roslyn incremental generator:
  - `../Entitas-IncrementalGenerator/Entitas.CodeGeneration/EntitasGenerator.cs`
- The sibling repo is not a drop-in replacement:
  - It uses `Entitas.CodeGeneration.Attributes` instead of `Entitas.Generators.Attributes`.
  - It only runs for hardcoded assemblies: `Assembly-CSharp` and `Entitas.CodeGeneration-Tests`.
  - Its generated API shape differs from the current generator output.

## Recommended Migration Path

1. Add the sibling projects to `Entitas.sln` side-by-side. Do not remove the current generator yet.
   - `../Entitas-IncrementalGenerator/Entitas.CodeGeneration/Entitas.CodeGeneration.csproj`
   - `../Entitas-IncrementalGenerator/Entitas.CodeGeneration.Attributes/Entitas.CodeGeneration.Attributes.csproj`
2. Patch the sibling generator so it can run for this repo's integration test assembly, or make the assembly gate configurable.
   - Current gate is in `../Entitas-IncrementalGenerator/Entitas.CodeGeneration/EntitasGenerator.cs`.
   - It currently allows only `Assembly-CSharp` and `Entitas.CodeGeneration-Tests`.
3. Trial the replacement only in `tests/Entitas.Generators.IntegrationTests/Entitas.Generators.IntegrationTests.csproj`.
   - Replace the analyzer reference to the current generator.
   - Wire the sibling generator as an analyzer using `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`.
   - Swap or bridge the attributes reference as needed.
4. Fix compile and API mismatches in the integration test project.
   - Attribute namespace mismatch is the first expected blocker.
   - Matcher accessors may be methods like `GameMatcher.Health()`.
   - Generated entity/context APIs may be extension-method based.
5. Run focused verification.
   - `dotnet test "tests/Entitas.Generators.IntegrationTests/Entitas.Generators.IntegrationTests.csproj"`
   - `dotnet test "..\\Entitas-IncrementalGenerator\\Entitas.CodeGeneration.Tests\\Entitas.CodeGeneration.Tests.csproj"`
   - `dotnet test "tests/Entitas.Tests/Entitas.Tests.csproj"`
6. Only after the trial passes, decide whether to fully replace the current generator.
   - Migrate or replace current generator snapshot tests.
   - Update samples and Unity-facing attribute usages.
   - Remove old generator projects and references only after the new path is proven.

## Known Risks

- Namespace mismatch between current and incremental attributes.
- Hardcoded assembly-name filtering in the incremental generator.
- Existing `Entitas.Generators.Tests` snapshots likely will not match the incremental generator output.
- Unity/sample adoption may need extra migration work because the incremental generator is compile-time only and has its own assumptions.

## Suggested New-Session Prompt

```md
The merge on `entitas-2.0-beta` is already committed.

Please evaluate replacing the current generator with the sibling repo at `../Entitas-IncrementalGenerator`.

Facts already verified:
- Current solution includes `gen/Entitas.Generators/Entitas.Generators.csproj` and `src/Entitas.Generators.Attributes/Entitas.Generators.Attributes.csproj`.
- Sibling repo contains a Roslyn incremental generator at `../Entitas-IncrementalGenerator/Entitas.CodeGeneration/EntitasGenerator.cs`.
- It is not drop-in:
  - attributes namespace is `Entitas.CodeGeneration.Attributes`
  - generator only runs for `Assembly-CSharp` and `Entitas.CodeGeneration-Tests`
  - generated API shape differs from the current generator

Please take the safest path:
1. Add the sibling generator + attributes projects to `Entitas.sln` side-by-side.
2. Patch the sibling generator so it can run for `Entitas.Generators.IntegrationTests` or make that configurable.
3. Swap only `tests/Entitas.Generators.IntegrationTests/Entitas.Generators.IntegrationTests.csproj` to use the sibling generator as analyzer.
4. Handle the attributes namespace mismatch.
5. Run focused tests and report diffs/blockers before attempting full replacement.

Do not remove the old generator yet.
```
