# Sprint S05 implementation plan — Window registry and panel lifecycle

**Sprint:** S05  
**Module:** M02 Spatial Workspace  
**Tasks:** T-S05-01 through T-S05-04  
**Plan date:** 2026-10-30  
**Authorization:** User authorized sequential software-first execution of all WBS sprints;
phone tests run now and viewer-only checks are deferred until a viewer exists.  
**Execution state:** APPROVED BY BROAD USER AUTHORIZATION / SOFTWARE-FIRST EXCEPTION

## Dependency reconciliation

The workbook was read without edits. It records S05 and T-S05-01..04 as `Not Started`,
EV-S05-01..04 as `Not Run`, and dependency T-S04-04 → T-S05-01 as `Pending`.
Repository evidence shows S04 software/build/phone plumbing PASS but through-lens
acceptance remains `NOT RUN`. This authorization permits sequential S05 software work;
it does not convert T-S04-04 or any hardware gate to PASS.

| Order | Task | Software deliverable | Deferred proof |
|---:|---|---|---|
| 1 | T-S05-01 | Versioned immutable panel schema, deterministic registry/reducer, bounds and duplicate rejection | Upstream viewer acceptance |
| 2 | T-S05-02 | Allowlisted Chat/3D/Document/Notes factory, bounded pooling and launcher adapter | Through-viewer launch usability |
| 3 | T-S05-03 | Focus/minimize/restore/close transitions and previous-focus restoration | Human gaze ergonomics |
| 4 | T-S05-04 | Deterministic lifecycle snapshot/event journal, Editor and PlayMode fixtures, iOS generation/build/phone launch | Viewer video and G02 (S07 only) |

## Architecture and safety

- `PanelRegistry` is the sole state owner. Commands are a fixed enum with typed panel
  IDs; no `SendMessage`, reflection command names or arbitrary model-generated code.
- State changes are copy-on-write and versioned. Invalid bounds, duplicate IDs, unknown
  panels, stale expected versions and panel limits fail without mutation.
- Exactly one visible panel may own focus. Closing/minimizing the focused panel restores
  the most recent eligible panel deterministically.
- `PanelFactory` accepts only four allowlisted kinds, creates at most one active view per
  panel ID and keeps a bounded pool. Closing returns the view; reopening reuses it.
- Lifecycle events contain bounded metadata and a sorted snapshot of panel IDs. No
  unbounded log or sensitive document content is retained.

## Planned files

| Path | Purpose |
|---|---|
| `docs/architecture/adr/ADR-0003-panel-registry-lifecycle.md` | State/reducer/pooling decision |
| `client/OmniLensXR/Assets/OmniLens/S05/PanelState.cs` | IDs, bounds, state, commands and result contracts |
| `client/OmniLensXR/Assets/OmniLens/S05/PanelRegistry.cs` | Deterministic reducer, focus stack and snapshots |
| `client/OmniLensXR/Assets/OmniLens/S05/PanelFactory.cs` | Allowlisted bounded GameObject pooling |
| `client/OmniLensXR/Assets/OmniLens/S05/S05PanelLifecycleFixture.cs` | Runtime fixture opening three internal panels |
| `client/OmniLensXR/Assets/Editor/S05PanelLifecycleBuild.cs` | Reproducible fixture scene/iOS generation |
| `client/OmniLensXR/Assets/Tests/Editor/S05PanelLifecycleTests.cs` | Schema/reducer/failure/15-transition tests |
| `client/OmniLensXR/Assets/Tests/PlayMode/S05PanelLifecyclePlayModeTests.cs` | Factory/pool/view/snapshot integration |
| `scripts/run_unity_s05_checks.sh` | Compile, all tests and iOS generation |
| `docs/evidence/S05/EV-S05-01.md` through `EV-S05-04.md` | Actual outcomes |
| `docs/evidence/S05/sprint-report.md` | Reconciled sprint state |

## Verification

- Positive: register four allowlisted panels, open/focus/minimize/restore/close, exact
  version increments, deterministic z-order and previous-focus restore.
- Invalid/failure: duplicate ID, invalid ID/kind/bounds, unknown panel, stale expected
  version, panel limit, duplicate open and registry/view divergence fail without state
  drift. The pool is a retention cap: excess inactive views are destroyed rather than
  rejecting an otherwise valid close.
- At least 15 lifecycle transitions are asserted against exact focused/visible IDs.
- PlayMode opens three panels, prevents duplicate active views, closes/returns them and
  proves reopen uses the bounded pool.
- Snapshot/event output contains expected sorted IDs and is capped.
- Run repository, Python, Ruff, Unity, unsigned Xcode and signed iPhone plumbing checks.
- Viewer readability/comfort remains `NOT RUN`; G02 is not evaluated before S07.

## Stop conditions

- Do not edit the workbook, start S06 before S05 evidence is reconciled, close G02,
  commit/push/deploy or persist signing/device identifiers.
- Do not weaken deterministic transition, duplicate, stale-version, pooling or snapshot
  assertions to obtain green output.

## Post-implementation reconciliation

- Final S05-only result is 5/5 EditMode and 5/5 PlayMode cases; the full project result
  is 87/87 EditMode and 11/11 PlayMode.
- Visual review reduced long-label sizing after the first preview showed `SCIENCE-3D`
  wider than its panel.
- A first physical-phone process check passed but a subsequent console capture exposed
  an iOS-stripped runtime shader and a managed exception. The fixture now serializes a
  source material asset; the final signed build reports version 7, four registered
  panels, three active views and exact focused/visible IDs without a managed exception.
- These results close the S05 software/phone-runtime work only. Through-viewer QA and
  the pending S04 physical dependency remain open.
