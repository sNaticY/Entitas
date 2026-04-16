# Entitas 2.0 Vision And Stabilization Roadmap

This document captures the likely original vision behind Entitas 2.0-beta, why it appears to have stalled in beta, and the concrete roadmap for carrying it to a real 2.0 release.

The goal is to preserve context across future sessions so the project does not drift back into one-off fixes without a release strategy.

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

### Still visibly unfinished

- `gen/Entitas.Generators` still exists.
- `src/Entitas.Generators.Attributes` still exists.
- `samples/Unity` still targets the old generator attribute namespace and old setup model.
- `EntitasUpgradeGuide.md` still reflects old generator eras and not the current 2.0 path.
- `src/Entitas.Unity.Editor` still has legacy references to older attribute assemblies.
- `gen/Entitas.CodeGeneration/EntitasGenerator.cs` still hardcodes supported assembly names.
- There is no finalized release checklist or 2.0 definition of done.

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
- old path: `Entitas.Generators`
- old attributes still appear in samples and some Unity/editor-facing code
- users can still get contradictory signals about which workflow is canonical

Target state:

- one clearly primary generator path
- one clearly primary attributes namespace
- old path either removed or marked legacy and isolated

### 2. Assembly-name filtering is a prototype constraint

The new generator currently only runs for hardcoded assembly names.

Problems:

- it works for tests and default `Assembly-CSharp`
- it is fragile for real Unity projects using asmdefs
- it undermines one of the stated goals: proper asmdef support

Target state:

- configurable assembly include/exclude rules
- defaults that work for normal Unity users
- documented behavior for custom asmdefs

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

- old `Entitas.Generators.Attributes` usage remains
- old `ContextInitialization` flow remains
- sample code does not teach the new primary workflow

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

## Recommended Roadmap

This roadmap is structured around release phases rather than random fixes.

## Phase 1: Stabilize The New Default Path

Goal: make the incremental generator path the clearly working default inside this repo.

### Work items

1. Remove accidental friction in the current repo.
2. Finish converting internal docs to the new path.
3. Audit all project references that still point to old generator attributes.
4. Verify the full solution builds/tests cleanly in a fresh checkout.
5. Record supported local dev workflows for Rider and Unity.

### Definition of done

- repo builds without hidden manual setup
- current docs match current code
- no ambiguity inside the repo about which generator path is primary

## Phase 2: Make The Incremental Generator Production-Ready

Goal: remove prototype assumptions from `gen/Entitas.CodeGeneration`.

### Work items

1. Replace hardcoded assembly-name gating with configuration.
2. Decide on the configuration mechanism.
   - candidate: `.editorconfig`
   - candidate: MSBuild properties
   - candidate: generator-specific analyzer config options
3. Define expected behavior for:
   - `Assembly-CSharp`
   - one custom asmdef
   - multiple asmdefs
4. Expand tests to cover custom assembly scenarios.
5. Review current attribute surface and remove or formalize transitional leftovers.

### Definition of done

- custom asmdef support is intentional, not accidental
- generator behavior is configurable and documented
- tests prove the supported assembly scenarios

## Phase 3: Migrate Unity Samples And Canonical Usage

Goal: make the sample project teach the actual 2.0 workflow.

### Work items

1. Migrate `samples/Unity` from `Entitas.Generators.Attributes` to `Entitas.CodeGeneration.Attributes`.
2. Replace old context initialization patterns with the new primary flow.
3. Update sample systems/controllers to use current generated entry points.
4. Verify the sample is internally coherent as a learning resource.
5. Add a minimal sample-focused README if needed.

### Definition of done

- sample code reflects real 2.0 usage
- new users can learn from the sample without mixing old and new APIs

## Phase 4: Finish The Unity Package Story

Goal: make Entitas 2.0 installable in a Unity-first way.

### Work items

1. Decide package layout strategy.
2. Define how runtime, editor, and analyzer artifacts are distributed.
3. Decide whether generator DLLs are built in-repo, shipped as release artifacts, or both.
4. Document Unity install steps for package consumers.
5. Validate package usage in a clean Unity project.

### Definition of done

- a Unity developer can install Entitas 2.0 without reverse-engineering the repo
- package/analyzer setup is documented and repeatable

## Phase 5: Publish A Real Migration Guide

Goal: help Entitas 1 users move to 2.0 instead of treating it as a new framework.

### Work items

1. Replace or supersede `EntitasUpgradeGuide.md`.
2. Document the change from Jenny to incremental generation.
3. Document namespace changes.
4. Document generated API naming changes.
5. Document context definition changes.
6. Document entity index, event system, and cleanup system changes.
7. Add troubleshooting for common migration errors.

### Definition of done

- a real Entitas 1 project owner can plan migration with confidence

## Phase 6: Harden Unity Editor And Visual Debugging

Goal: make Unity-facing tooling trustworthy on current supported versions.

### Work items

1. Audit open/publicly known editor regressions.
2. Validate visual debugging on supported Unity versions.
3. Fix lingering legacy dependencies in editor code.
4. Define what is officially supported vs best effort.
5. Add smoke validation guidance for future changes.

### Definition of done

- visual debugging is not treated as experimental for supported Unity versions

## Phase 7: Remove Or Isolate Legacy Generator Path

Goal: stop shipping two conflicting mental models.

### Work items

1. Decide whether `gen/Entitas.Generators` remains supported, deprecated, or removed.
2. Decide whether `src/Entitas.Generators.Attributes` remains supported, deprecated, or removed.
3. If keeping legacy support:
   - isolate it clearly
   - document it as legacy
   - make README and samples avoid it
4. If removing legacy support:
   - delete dead references
   - replace or remove obsolete tests and docs

### Definition of done

- users are not confused about the canonical generation path

## Phase 8: Release Management

Goal: ship a credible 2.0 release instead of an eternal beta branch.

### Work items

1. Define release milestones:
   - `2.0-beta stabilization`
   - `2.0-rc1`
   - `2.0`
2. Define a versioning policy for runtime, attributes, generator, and Unity packages.
3. Add a release checklist.
4. Publish release notes centered on Unity users.
5. Announce migration guidance together with release artifacts.

### Definition of done

- the project has an explicit path from beta to release
- release criteria are written down, not implied

## Suggested Beta Exit Criteria

Entitas should not leave beta until these are true:

1. The incremental generator is the documented default path.
2. Custom asmdef support is configurable and tested.
3. `samples/Unity` uses the 2.0 workflow.
4. Unity-first install docs are complete.
5. A migration guide from Entitas 1 exists.
6. Visual debugging/editor tooling is validated on supported Unity versions.
7. Legacy generator ambiguity is resolved.
8. Release artifacts and package story are settled.

## Immediate Next Priorities

If progress must be incremental, these should be the next concrete priorities in order:

1. Finish internal migration to the new generator path.
2. Make generator assembly selection configurable.
3. Migrate `samples/Unity`.
4. Replace the old upgrade guide with a 1 -> 2 migration guide.
5. Define the Unity package/analyzer shipping model.

## Working Principles For Future Sessions

Future work on this fork should follow these rules:

1. Prefer finishing the 2.0 path over adding new side systems.
2. Prefer one canonical workflow over supporting multiple overlapping workflows.
3. Prefer Unity-first documentation and validation.
4. Treat asmdef support, sample migration, and package delivery as core release work, not optional cleanup.
5. Do not let compatibility shims become permanent architecture unless there is a clear release reason.

## Recommended Session Checklist

When continuing work in future sessions, check:

1. Which roadmap phase the task belongs to.
2. Whether the task reduces old/new generator ambiguity.
3. Whether docs or samples need updating with the code change.
4. Whether the change affects Unity package/analyzer behavior.
5. Whether a new release blocker has been discovered and should be added here.

## Maintenance Note

This roadmap is intended to be a living project memory file.

When major discoveries happen, update this document instead of relying on conversation history.

If priorities change, preserve the original vision section and add a fork-specific strategy section rather than rewriting history.
