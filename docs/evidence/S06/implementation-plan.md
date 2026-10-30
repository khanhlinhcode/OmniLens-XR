# Sprint S06 implementation plan — Placement, manipulation, localization and fatigue tooling

**Sprint:** S06  
**Module:** M02 Spatial Workspace  
**Tasks:** T-S06-01 through T-S06-04  
**Plan date:** 2026-10-30  
**Authorization:** User authorized sequential software-first execution; phone tests run
now and viewer-only checks are deferred until a viewer exists.  
**Execution state:** APPROVED BY BROAD USER AUTHORIZATION / SOFTWARE-FIRST EXCEPTION

## Dependency reconciliation

The workbook was inspected read-only and remains unchanged. Its baseline records the S06
task rows as `Not Started`; T-S06-01 depends on T-S05-04. Repository evidence now proves
S05 software and physical-phone runtime behavior, but T-S05-04 remains PARTIAL because
viewer QA is unavailable. This authorization permits S06 software implementation; it
does not promote the pending physical dependency or any gate to PASS.

| Order | Task | Software deliverable | Deferred proof |
|---:|---|---|---|
| 1 | T-S06-01 | Safe placement plane, edge-aware FOV bounds, deterministic snapping/collision avoidance and 20 fixtures | Through-lens field-of-view validation |
| 2 | T-S06-02 | Typed move/resize/pin manipulation, large handle policy, clamped min/max and focus preservation | Human gaze handle accuracy/fatigue |
| 3 | T-S06-03 | VN/EN key catalog, runtime switching, number/date formatting and English fallback audit | Native-speaker through-viewer UX review |
| 4 | T-S06-04 | Bounded 10-minute protocol/metrics state machine, misclick/head-turn counters and long-text prohibition | Actual 10-minute viewer session |

## Architecture and safety

- Placement consumes finite typed vectors only. It snaps to a fixed metric grid, keeps
  the full panel rectangle inside the S04 angular safe zone and searches a bounded,
  deterministic set of offsets to avoid overlap.
- Move and resize extend the S05 versioned reducer. Commands preserve focus/pin state,
  enforce exact expected versions and cannot mutate arbitrary scene objects or execute
  string/reflection commands.
- Handle kinds are an enum allowlist and their minimum hit size is checked against a
  spatial accessibility token. Long free-text entry while wearing the viewer is not an
  available action.
- Localization keys are explicit enums. Vietnamese and English are local static data;
  missing Vietnamese values fall back to English, while unknown enum values fail.
- Fatigue metrics contain counts/durations only, are capped, and do not claim comfort.
  Synthetic clock tests validate the protocol; physical results remain `NOT RUN`.

## Planned files

| Path | Purpose |
|---|---|
| `docs/architecture/adr/ADR-0004-placement-localization-fatigue.md` | Placement/manipulation/localization/metrics decisions |
| `Assets/OmniLens/S06/PanelPlacement.cs` | FOV plane, snapping, collision search and fixtures |
| `Assets/OmniLens/S06/PanelManipulation.cs` | Typed large-handle move/resize/pin adapter |
| `Assets/OmniLens/S06/SpatialLocalization.cs` | VN/EN catalog and locale formatting |
| `Assets/OmniLens/S06/S06FatigueProtocol.cs` | Bounded protocol state and metrics |
| `Assets/OmniLens/S06/S06WorkspaceFixture.cs` | Runtime fixture and bounded summary markers |
| `Assets/Editor/S06WorkspaceBuild.cs` | Reproducible scene/preview/iOS generation |
| `Assets/Tests/Editor/S06WorkspaceTests.cs` | 20 layouts, bounds, reducer, localization and protocol tests |
| `Assets/Tests/PlayMode/S06WorkspacePlayModeTests.cs` | Active-view manipulation and fixture integration |
| `scripts/run_unity_s06_checks.sh` | Preview, complete test suites and iOS generation |
| `docs/evidence/S06/EV-S06-01.md` through `EV-S06-04.md` | Actual outcomes |
| `docs/evidence/S06/sprint-report.md` | Reconciled sprint state |

## Verification

- Exactly 20 deterministic placement fixtures keep the primary panel's full rectangle
  inside the configured FOV and contain no overlap.
- Invalid NaN/infinite positions, impossible panel sizes, stale versions, unknown panel
  IDs and exhausted collision searches fail without state drift.
- Move/resize requests snap and clamp at min/max edges; focused panels remain focused.
  Pin toggle is typed and synchronized to the active view.
- Every scoped menu key resolves in VN and EN or through an asserted English fallback;
  Vietnamese/English number and date outputs differ as expected.
- Synthetic 600-second protocol reaches completion only with a monotonic clock, bounds
  counters, rejects long-text actions and reports no comfort PASS.
- Run repository, Python, Ruff, Unity, unsigned Xcode and signed iPhone console checks.

## Stop conditions

- Do not edit the workbook, start S07 before S06 evidence reconciliation, close G02,
  commit/push/deploy, install dependencies or persist signing/device identifiers.
- Do not turn the synthetic 10-minute clock test into physical fatigue evidence or mark
  through-viewer placement/readability PASS.

## Post-implementation reconciliation

- Final project result is 94/94 EditMode and 14/14 PlayMode; S06 contributes 7/7 and
  3/3 respectively.
- The first S06 run exposed a radial-distance defect at the maximum-depth/angular edge;
  lateral placement now consumes only the remaining radial radius and snaps inward.
- The first signed console window ended after eight seconds before the fixture `Start`;
  a 20-second retry reached the marker and exposed locale-dependent `5,0` formatting.
  Machine metrics now use invariant formatting, with a strict regression assertion and
  final device marker `elapsed=5.0 physical=NOT_RUN`.
- Software and phone runtime are verified. Physical FOV, gaze handles and ten-minute
  fatigue remain `NOT RUN`.
