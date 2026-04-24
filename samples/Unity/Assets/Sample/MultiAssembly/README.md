# Multi-Assembly Shared Context Sample

This sample keeps one logical `Shared` context in a root asmdef while component APIs live in separate feature asmdefs.

- `Root` declares `SharedAttribute` and owns generated `SharedContext` / `SharedEntity` root types.
- `FeatureA` declares `[assembly: EntitasFeature("Player")]` and the unique indexed `PlayerComponent`.
- `FeatureB` declares `[assembly: EntitasFeature("Health")]` and `HealthComponent`.
- `Bootstrap` composes the runtime `ContextSchema` with `SharedContext.CreateSchemaBuilder().AddPlayerFeature().AddHealthFeature()`, registers `SharedContext(schema)` through `Contexts.Register(context, schema)`, and drives the scene controller.

The scene intentionally uses the runtime schema path instead of an aggregate assembly that references every feature at generation time.
