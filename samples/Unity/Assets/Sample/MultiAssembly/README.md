# Multi-Assembly Shared Context Sample

This sample keeps one logical `Shared` context in a root asmdef while component APIs live in separate feature asmdefs and Unity's default `Assembly-CSharp`.

- `Root` declares `SharedAttribute` and owns generated `SharedContext` / `SharedEntity` root types.
- `FeatureA` declares `[assembly: EntitasAssembly("Player")]` and the unique indexed `PlayerComponent`.
- `FeatureB` declares `[assembly: EntitasAssembly("Health")]` and `HealthComponent`.
- `DefaultAssembly` compiles into `Assembly-CSharp` and contributes `Mana`, `Session`, `Expired`, and `Destroyed` components to the same `Shared` context.
- `Bootstrap` still demonstrates an asmdef-only schema. The playable scene uses `AssemblyCSharpContextFactory`, which composes `SharedContext.CreateSchemaBuilder().AddPlayerAssembly().AddHealthAssembly().AddAssemblyCSharpAssembly()` and registers it through generated `contexts.RegisterShared(schema)`.

The scene intentionally uses the runtime schema path instead of an aggregate assembly that references every feature at generation time. Press Play in `Multi Assembly Scene` to verify unique components, matchers, groups, collectors, entity indices, generated event systems, and cleanup systems on one shared context. Visual debugging is enabled for the registered context in the Unity editor.
