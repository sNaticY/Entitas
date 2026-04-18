# Entitas 2.0 Roadmap

This document is the execution roadmap for this fork.

Vision, historical context, current-state assessment, and release criteria now live in `notes/entitas-2.0-vision.md`.

## Fork Priority Tracker

This section is the practical release tracker for this fork.

Status key:

- `[x]` done
- `[~]` in progress / partially done
- `[ ]` not done

### 1. Remove Extra Dependencies

Goal: reduce or eliminate non-essential `DesperateDevs` and other legacy dependency baggage from the 2.0 path.

- `[x]` Generator integration no longer depends on the removed legacy generator projects.
- `[x]` `src/Entitas.Unity.Editor` now uses `Entitas.CodeGeneration.Attributes` instead of the old generator attributes package.
- `[x]` Audit remaining `DesperateDevs` dependencies across runtime, Unity runtime, and Unity editor code.
- `[x]` Decide which external dependencies are still intentional for 2.0 and which should be removed.
- `[x]` Remove or replace remaining legacy/helper dependencies where practical.
- `[x]` Document the final dependency stance for runtime, editor, and package consumers.

### 2. Support One Context In Multi-Assembly

Goal: make the 2.0 generator work intentionally for one context in a custom asmdef / multi-assembly setup.

- `[x]` The incremental generator is now analyzer-config driven instead of relying on only hardcoded assembly assumptions.
- `[x]` Tests exist for default assembly behavior and custom assembly filtering.
- `[~]` The runtime/bootstrap design now supports explicit context registration into `Entitas.Contexts`.
- `[ ]` Define and document the canonical "one context in one custom asmdef" workflow.
- `[ ]` Validate the workflow in the Unity sample or a focused clean Unity project setup.
- `[ ]` Add an explicit sample or fixture that demonstrates one-context multi-assembly usage end-to-end.
- `[ ]` Document expected behavior for one asmdef, multiple asmdefs, and mixed `Assembly-CSharp` projects.

### 3. Package Support

Goal: make Entitas 2.0 consumable as a real Unity package instead of a repo-only setup.

- `[ ]` Decide the final UPM package layout.
- `[ ]` Decide how runtime, editor, attributes, and analyzer assets are distributed.
- `[ ]` Decide whether analyzer DLLs are built in-repo, shipped as release artifacts, or both.
- `[ ]` Implement package structure and packaging workflow.
- `[ ]` Write Unity-first install steps for package consumers.
- `[ ]` Validate package consumption in a clean Unity project.

### 4. Legacy Cleanup

Goal: remove remaining old-workflow ambiguity so users see one canonical 2.0 path.

- `[x]` `gen/Entitas.Generators` has been removed from the repo and solution.
- `[x]` `src/Entitas.Generators.Attributes` has been removed from the repo and solution.
- `[x]` Internal docs have shifted to the incremental-generator-first path.
- `[x]` `samples/Unity` migration is complete.
- `[x]` Remove or isolate all remaining old generator assumptions from `samples/Unity`.
- `[~]` Remove or replace obsolete docs that still describe the old generator workflow as active.
- `[x]` Confirm release/package flow no longer depends on removed legacy pieces.

### 5. Entitas 1 -> 2 Migration

Goal: give existing users a practical migration path instead of forcing rediscovery.

- `[ ]` Replace or supersede `EntitasUpgradeGuide.md`.
- `[ ]` Document Jenny -> incremental generator migration.
- `[ ]` Document attribute namespace changes.
- `[ ]` Document generated API naming and `Contexts` access changes.
- `[ ]` Document context definition, entity index, event, and cleanup-system changes.
- `[ ]` Add troubleshooting for common migration failures.

### 6. Documentation + Wiki

Goal: make the public docs match the actual 2.0 workflow.

- `[x]` `README.md` has been rewritten to be Unity-first and incremental-generator-first.
- `[x]` `AGENTS.md` reflects the current project structure and validation flow.
- `[ ]` Finish Unity-first install/setup documentation beyond the README.
- `[ ]` Add explicit docs for Rider, Unity, asmdefs, and analyzer loading.
- `[ ]` Update or replace wiki-era guidance that still points users to old workflows.
- `[ ]` Add package-install documentation once package support is finalized.
- `[ ]` Add a supported Unity version matrix and editor/visual-debugging validation guidance.

### Suggested Tracking Rule

When work lands, update this tracker first before expanding the deeper roadmap sections below.

## Recommended Roadmap

This roadmap is structured around release phases rather than random fixes.

## Phase 1: Stabilize The New Default Path ✓ COMPLETE

Goal: make the incremental generator path the clearly working default inside this repo.

### Work items

1. ~~Remove accidental friction in the current repo.~~
2. ~~Finish converting internal docs to the new path.~~
3. ~~Audit all project references that still point to old generator attributes.~~
4. ~~Verify the full solution builds/tests cleanly in a fresh checkout.~~
5. ~~Record supported local dev workflows for Rider and Unity.~~

### Definition of done

- [x] repo builds without hidden manual setup
- [x] current docs match current code
- [x] no ambiguity inside the repo about which generator path is primary

## Phase 2: Make The Incremental Generator Production-Ready ✓ COMPLETE

Goal: remove prototype assumptions from `gen/Entitas.CodeGeneration`.

### Work items

1. ~~Replace hardcoded assembly-name gating with configuration.~~
2. ~~Decide on the configuration mechanism.~~
   - ~~chosen: analyzer-config options (`.editorconfig` compatible)~~
3. ~~Define expected behavior for:~~
   - ~~`Assembly-CSharp`~~
   - ~~one custom asmdef~~
   - ~~multiple asmdefs~~
4. ~~Expand tests to cover custom assembly scenarios.~~
5. ~~Review current attribute surface and remove or formalize transitional leftovers.~~

### Definition of done

- [x] custom asmdef support is intentional, not accidental
- [x] generator behavior is configurable and documented
- [x] tests prove the supported assembly scenarios

## Phase 3: Migrate Unity Samples And Canonical Usage ✓ COMPLETE

Goal: make the sample project teach the actual 2.0 workflow.

### Work items

1. ~~Migrate `samples/Unity` from `Entitas.Generators.Attributes` to `Entitas.CodeGeneration.Attributes`.~~
2. ~~Replace old context initialization patterns with the new primary flow.~~
3. ~~Update sample systems/controllers to use current generated entry points.~~
4. ~~Verify the sample is internally coherent as a learning resource.~~
5. ~~Add a minimal sample-focused README if needed.~~

### Definition of done

- [x] sample code reflects real 2.0 usage
- [x] new users can learn from the sample without mixing old and new APIs

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

## Phase 6: Harden Unity Editor And Visual Debugging ✓ COMPLETE

Goal: make Unity-facing tooling trustworthy on current supported versions.

### Work items

1. ~~Audit open/publicly known editor regressions.~~
2. ~~Validate visual debugging on supported Unity versions.~~
3. ~~Fix lingering legacy dependencies in editor code.~~
4. ~~Define what is officially supported vs best effort.~~
5. ~~Add smoke validation guidance for future changes.~~

### Definition of done

- [x] visual debugging is not treated as experimental for supported Unity versions

## Phase 7: Remove Or Isolate Legacy Generator Path ✓ COMPLETE

Goal: stop shipping two conflicting mental models.

### Work items

1. ~~Remove or migrate remaining sample references to `Entitas.Generators.Attributes`.~~
2. ~~Remove or replace obsolete docs that still describe the old generator path as active.~~
3. ~~Confirm package/analyzer distribution no longer depends on the removed legacy projects.~~

### Definition of done

- [x] users are not confused about the canonical generation path

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

## Immediate Next Priorities

If progress must be incremental, these should be the next concrete priorities in order:

1. Finish internal migration to the new generator path.
2. Make generator assembly selection configurable.
3. Migrate `samples/Unity`.
4. Replace the old upgrade guide with a 1 -> 2 migration guide.
5. Define the Unity package/analyzer shipping model.

## Recommended Session Checklist

When continuing work in future sessions, check:

1. Which roadmap phase the task belongs to.
2. Whether the task reduces old/new generator ambiguity.
3. Whether docs or samples need updating with the code change.
4. Whether the change affects Unity package/analyzer behavior.
5. Whether a new release blocker has been discovered and should be added here.

## Maintenance Note

This roadmap is intended to be a living execution tracker.

When major discoveries affect product intent or release criteria, update `notes/entitas-2.0-vision.md` as well.

If priorities change, update the tracker and phases here rather than re-expanding this file into a mixed vision/history document.
