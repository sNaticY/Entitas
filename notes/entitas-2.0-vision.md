# Entitas 2.0 Vision

This document captures the likely original vision behind Entitas 2.0-beta, why it appears to have stalled in beta, the current state of this fork, and the release-level target state for shipping a real 2.0.

Its purpose is to preserve product and architecture intent separately from the execution roadmap.

## Why This Document Exists

Upstream Entitas development appears to have slowed or stopped before Entitas 2.0 was finalized.

The strongest public signals are:

- Upstream issue `#1005`: `Entitas with Roslyn Code Generation via dotnet IIncrementalGenerator`
- Upstream issue `#1074`: `Update on Entitas 2.0-beta and my situation`
- Upstream discussion `#1008`: `One Entitas version - Desperate Devs is open-source now`
- The absence of any upstream release newer than `1.14.1`

These point to a real 2.0 transition that started, but was interrupted before release hardening, migration, packaging, and documentation were completed.

## Reconstructed Original Vision

Based on upstream public statements and the current codebase, the intended direction for Entitas 2.0 appears to have been:

1. Replace Jenny with a Roslyn incremental/source generator.
2. Simplify the Entitas codebase and reduce project count.
3. Support Unity asmdefs and multiple projects more naturally.
4. Support namespaces for contexts and components.
5. Remove or reduce dependencies on `DesperateDevs` where possible.
6. Make Entitas work cleanly as a Unity package.
7. Modernize the repo around SDK-style projects and current .NET tooling.
8. Keep Unity as the primary user experience.

This aligns with Simon Schmid's public summary in issue `#1074`, where he described:

- replacing Jenny with a new .NET source generator
- supporting Unity asmdefs
- supporting namespaces for contexts and components
- removing dependencies on DesperateDevs
- using Entitas as a Unity package
- migrating to .NET 6
- simplifying the codebase substantially

## Why Entitas 2.0 Stayed Beta

The public evidence suggests that beta status was not only about code quality. It was primarily about interruption before completion.

### Likely non-technical reason

Issue `#1074` explicitly says development was interrupted by layoffs and a change in the maintainer's personal situation.

That likely prevented the final push from:

- active development
- internal use in real projects
- migration polish
- packaging and documentation
- public release

### Likely technical/product reasons

Even with the core direction established, several release-critical areas still appear unfinished:

1. The old and new generator paths coexist.
2. Documentation still largely describes the old Jenny workflow.
3. Samples still use old attributes and initialization patterns.
4. The incremental generator still contains hardcoded assembly-name assumptions.
5. Unity packaging is not finished.
6. Visual debugging/editor support still appears tied to older assumptions and older Unity behavior.
7. There is no polished Entitas 1 to 2 migration path for real users.

In other words: the architecture pivot happened, but the productization phase did not fully finish.

## Current State In This Fork

The current fork already moved meaningfully toward the intended 2.0 direction.

### Completed or improved already

- `gen/Entitas.CodeGeneration` has been integrated into the solution.
- `src/Entitas.CodeGeneration.Attributes` has been integrated into the solution.
- End-to-end integration coverage now exists in `tests/Entitas.CodeGeneration.Tests`.
- The solution and Rider loading story was fixed so the repo builds cleanly by default.
- `README.md` has been rewritten to be Unity-first and incremental-generator-first.
- `AGENTS.md` now reflects the current project structure and validation flow.
- `gen/Entitas.Generators` has been removed from the repo and solution.
- `src/Entitas.Generators.Attributes` has been removed from the repo and solution.
- `src/Entitas.Unity.Editor` now uses `Entitas.CodeGeneration.Attributes`.

### Still visibly unfinished

- `samples/Unity` still has migration and coherence work left before it is a clean 2.0 reference.
- `EntitasUpgradeGuide.md` still reflects old generator eras and not the current 2.0 path.
- There is no finalized release checklist or 2.0 definition of done implemented in repo workflows.

## Definition Of A Real Entitas 2.0 Release

The project should only be considered a valid 2.0 release when all of the following are true.

### Product expectations

1. A Unity developer can install and use Entitas 2.0 without Jenny.
2. The incremental generator works in the default Unity path and in custom asmdef setups.
3. The main docs describe the current workflow accurately.
4. Existing Entitas 1 users have a migration path.
5. Samples demonstrate the new workflow.
6. Unity-facing tooling is stable on supported Unity versions.
7. The old generator path is either removed or explicitly documented as legacy.

### Engineering expectations

1. Full solution build and test pass in CI.
2. Incremental generator output is covered by focused tests.
3. Unity integration and editor behavior are tested enough to trust releases.
4. Configuration assumptions are explicit rather than hardcoded.
5. Release artifacts and versioning are coherent.

## Main Gaps To Close

### 1. Generator transition is incomplete

The largest structural gap is that the repo still straddles two code generation worlds.

Problems:

- new path: `Entitas.CodeGeneration`
- old-path assumptions still appear in samples and migration docs
- users can still get contradictory signals about which workflow is canonical

Target state:

- one clearly primary generator path
- one clearly primary attributes namespace
- old path either removed or marked legacy and isolated

### 2. Assembly-name filtering and multi-assembly need intentional support

The new generator needs to work intentionally for Unity asmdefs and multi-assembly setups.

Problems:

- it works for tests and default `Assembly-CSharp`
- it is fragile for real Unity projects using asmdefs unless the workflow is documented and validated
- it undermines one of the stated goals: proper asmdef support

Target state:

- configurable assembly include/exclude rules
- defaults that work for normal Unity users
- documented behavior for custom asmdefs
- a clear one-context multi-assembly story

### 3. Unity package story is unfinished

Upstream explicitly pointed toward Unity package usage, but the current repo is not yet a polished package product.

Problems:

- no finished UPM-oriented package structure
- no final analyzer distribution story for Unity users
- no end-to-end install docs for Unity package consumption

Target state:

- a Unity package workflow that is documented and repeatable
- a clean story for runtime assemblies, editor assemblies, and analyzer delivery

### 4. Samples are not aligned with the new architecture

The sample project is still a major source of truth for users, but it currently preserves old generator patterns.

Problems:

- old `Entitas.Generators.Attributes` usage remains in the migration surface
- old setup assumptions still leak into the sample experience
- sample code does not yet teach the final primary workflow cleanly

Target state:

- samples use `Entitas.CodeGeneration.Attributes`
- samples demonstrate `Contexts` and new generated APIs
- samples are valid references for 2.0 adoption

### 5. Documentation and upgrade guidance are incomplete

The docs were one of the biggest beta blockers even before this fork started updating them.

Problems:

- upstream setup guidance was confusing enough that users wrote temporary setup issues
- `EntitasUpgradeGuide.md` is obsolete for the 2.0 transition
- there is no practical migration guide from Jenny to incremental generation

Target state:

- Unity-first install docs
- migration guide from Entitas 1 to 2
- explicit troubleshooting docs for Rider, Unity, asmdefs, and analyzer loading

### 6. Unity editor and visual debugging need current-version confidence

There is public evidence of Unity editor breakage around newer versions.

Problems:

- visual debugging is a core part of Entitas's Unity value proposition
- editor regressions can make the whole release feel unfinished even if runtime/generator are solid

Target state:

- supported Unity version matrix
- editor smoke tests or at least repeatable manual validation
- clear owner-level confidence in inspector/debug tooling

## Suggested Beta Exit Criteria

Entitas should not leave beta until these are true:

1. The incremental generator is the documented default path.
2. Custom asmdef support is configurable, validated, and tested.
3. `samples/Unity` uses the 2.0 workflow.
4. Unity-first install docs are complete.
5. A migration guide from Entitas 1 exists.
6. Visual debugging/editor tooling is validated on supported Unity versions.
7. Legacy generator ambiguity is resolved.
8. Release artifacts and package story are settled.

## Working Principles For Future Sessions

Future work on this fork should follow these rules:

1. Prefer finishing the 2.0 path over adding new side systems.
2. Prefer one canonical workflow over supporting multiple overlapping workflows.
3. Prefer Unity-first documentation and validation.
4. Treat asmdef support, sample migration, and package delivery as core release work, not optional cleanup.
5. Do not let compatibility shims become permanent architecture unless there is a clear release reason.

## Maintenance Note

This document is intended to be the persistent memory for Entitas 2.0 intent and release criteria.

When major discoveries happen, update this document instead of relying on conversation history.

If priorities change, preserve the original vision section and add fork-specific strategy rather than rewriting history.
