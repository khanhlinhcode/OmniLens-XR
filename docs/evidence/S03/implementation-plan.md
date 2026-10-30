# Sprint S03 implementation plan — Viewer optics and feasibility gate

**Sprint:** S03  
**Module:** M01 Device / XR Foundation  
**Tasks:** T-S03-01 through T-S03-04  
**Plan date:** 2026-10-30  
**Authorization:** The user explicitly requested the next work after being told S03
requires separate approval. This authorizes S03 planning and execution only; it does not
authorize S04, a viewer purchase, paid tooling, global installs, workbook status edits,
commit, push or deployment.  
**Execution state:** SOFTWARE-FIRST IMPLEMENTATION AUTHORIZED / PHYSICAL VIEWER QA BLOCKED

## 1. Source reconciliation

The source priority is `engineering-spec-v2.md`, then the read-only rows in
`master-plan-v2.xlsx`, then `pm-audit-v2.md`. The workbook was parsed read-only with the
Python standard library because `openpyxl` is not installed; no workbook cell was edited.

Workbook state before S03 execution:

| Item | Workbook state | Repository evidence state |
|---|---|---|
| S03 | Not Started | Authorized for planning; no S03 task PASS yet |
| T-S03-01..04 | Not Started / 0% | NOT RUN |
| EV-S03-01..04 | Not Run | No evidence files yet beyond this plan |
| G01 | Not Started | NOT RUN; release blocker |
| T-S02-04 dependency | Workbook dependency marked Pending | Repository EV-S02-04 is PASS with physical deny/retry/allow/resume evidence |

The workbook remains unchanged. Repository implementation evidence supersedes the stale
dependency assumption for planning, but it does not authorize changing workbook status.

## 2. Dependency order and acceptance

| Order | Task | Dependency | Required outcome | Evidence |
|---:|---|---|---|---|
| 1 | T-S03-01 | T-S02-04 | Record actual viewer model/profile, tray geometry, iPhone 11 fit, camera obstruction, trigger behavior, lens adjustment and QR profile | EV-S03-01 |
| 2 | T-S03-02 | T-S03-01 | Implement bounded gaze-dwell fallback with visible progress, cancellation, cooldown and recenter-safe behavior; complete accessibility review | EV-S03-02 |
| 3 | T-S03-03 | T-S03-01 | Run the physical 5/10/20-minute stereo protocol and record frame time, readability, glare/distortion, thermal state and user stop/comfort result | EV-S03-03 |
| 4 | T-S03-04 | T-S03-02 and T-S03-03 | Decide G00/G01 PASS or FAIL with evidence, blockers, purchase decision and a 2D fallback | EV-S03-04 |

T-S03-02 and T-S03-03 must not be marked PASS before T-S03-01 has an actual viewer
profile. T-S03-04 cannot close either gate until both dependent tasks have real results.

## 3. Immediate physical input required for T-S03-01

The repository still records the viewer as unknown. The following observations are
required before physical compatibility or comfort QA can proceed. On 2026-10-30, the
user confirmed that no viewer is currently available and explicitly authorized the
remaining software-only implementation first:

1. Viewer/VR Box brand and model, or clear photos of its front, phone tray, lenses,
   adjustment controls and trigger.
2. Whether the viewer is physically available now and the iPhone 11 can be inserted.
3. Phone-tray internal width/height/depth and maximum phone thickness, measured with a
   ruler or caliper.
4. Whether the rear camera is exposed or obstructed when the tray is closed.
5. Trigger type and actual behavior: capacitive touch, magnetic lever, mechanical screen
   tap, Bluetooth controller or no trigger.
6. Lens controls: interpupillary-distance adjustment, focus adjustment and usable ranges.
7. Cardboard QR/profile availability. Do not invent or reuse an unrelated viewer profile.

If the viewer is unavailable, T-S03-01 is BLOCKED. T-S03-02 and T-S03-03 may advance
only as software/test tooling with `PARTIAL` status; their physical acceptance remains
`NOT RUN`. T-S03-04 and G00/G01 remain blocked. A purchase recommendation is not issued
from photos/speculation alone.

## 4. Authorized software-first implementation files

Production changes remain inside the S03 boundary. These files can prepare the fallback
and measurement fixture, but they cannot establish viewer compatibility or comfort.

| Planned path | Purpose |
|---|---|
| `client/OmniLensXR/Assets/OmniLens/S03/GazeDwellStateMachine.cs` | Pure bounded state machine for idle/focusing/selected/cooldown, progress and cancellation |
| `client/OmniLensXR/Assets/OmniLens/S03/GazeDwellSelector.cs` | Cardboard reticle integration without arbitrary execution or trigger dependency |
| `client/OmniLensXR/Assets/OmniLens/S03/GazeDwellRecenterTarget.cs` | Explicit allowlisted Cardboard recenter action when a trigger is absent or broken |
| `client/OmniLensXR/Assets/OmniLens/S03/S03ComfortProbe.cs` | Bounded 5/10/20-minute frame/lifecycle/thermal-state checkpoints; no raw media or identity data |
| `client/OmniLensXR/Assets/Tests/Editor/S03ViewerAndDwellTests.cs` | Positive, invalid-input, cancellation, cooldown, duplicate-selection and missing-target tests |
| `client/OmniLensXR/Assets/Tests/PlayMode/S03GazeDwellPlayModeTests.cs` | Real physics-raycast selection, deny-layer, gaze-exit re-arm and lifecycle cancellation tests |
| `scripts/run_unity_s03_checks.sh` | Reproducible compile/EditMode/iOS-generation verification scoped to S03 |
| `scripts/analyze_s03_comfort_log.py` | Standard-library validation and p50/p95 summary for sanitized S03 JSONL |
| `config/s03-viewer-evidence.template.json` | Fail-closed structured template for future physical inventory, dwell, comfort and gate evidence |
| `scripts/validate_s03_viewer_evidence.py` | Reject unsupported fields, private identifiers and premature G00/G01 PASS claims |
| `docs/evidence/S03/viewer-qa-runbook.md` | Seated/stationary physical execution and safe-stop instructions |
| `docs/evidence/S03/EV-S03-01.md` through `EV-S03-04.md` | Actual per-task device evidence; no invented PASS |
| `docs/evidence/S03/sprint-report.md` | Task reconciliation and G00/G01 decision |

The official imported Cardboard sample will be changed only where the pinned sample
cannot consume the isolated OmniLens component. Any sample modification remains small,
documented and covered by configuration tests.

## 5. Gaze-dwell safety contract

- Dwell duration must be explicit and configurable within a bounded range; the proposed
  starting value is 1.5 seconds and is not final until the viewer/accessibility review.
- Selection requires uninterrupted gaze on one allowlisted target.
- Leaving the target, losing focus, backgrounding the app or changing scene cancels
  progress without firing.
- Exactly one selection may fire per completed dwell, followed by cooldown and gaze-exit
  re-arm. No repeated action while staring at the same target.
- Native Cardboard close/gear regions and recenter gestures are excluded.
- A visible progress indicator and reduced-motion mode are required.
- Trigger input, when available, remains an optional faster path; dwell is the fallback.
- Tests must cover zero/negative/NaN timing, missing camera/target, cancellation just
  before threshold, threshold completion, cooldown and duplicate suppression.

## 6. Physical 20-minute protocol

The phone starts charged, at a recorded ambient condition and with the same build/viewer
profile used for the full run. Stop immediately for nausea, dizziness, eye strain,
headache, excessive heat or unstable rendering; a stopped run is evidence, not failure
to report.

| Time | Required observations |
|---:|---|
| 0 min | Viewer/profile, fit, trigger, lens settings, readability fixture, initial iOS thermal state and surface temperature if an external thermometer exists |
| 5 min | Frame p50/p95/max, dropped/stalled frame evidence, readable text size, glare/distortion, thermal state, subjective symptoms |
| 10 min | Repeat the same measurements without changing lens/profile settings |
| 20 min | Final measurements, tolerance/stop result, device heat and safe exit to 2D |

iOS does not expose a reliable public exact battery/SoC temperature to Unity. The probe
may record the native `ProcessInfo.thermalState`; Celsius claims require a separate
external thermometer and must state that it is a surface measurement. Current S02 frame
windows are about 33.33 ms (roughly 30 FPS), so S03 must not advertise 60 FPS unless a
new measured build actually achieves it.

## 7. Verification matrix

### EV-S03-01

- Positive: actual iPhone 11 fit, lens controls, camera opening, trigger and QR/profile are
  measured and photographed privately where safe.
- Negative: obstruction, pressure on buttons, unsupported dimensions, missing QR or
  nonfunctional trigger are recorded as FAIL/limitation, not normalized away.
- Privacy: no serial, account, personal photo content or precise location is committed.

### EV-S03-02

- Automated: pure state-machine tests for select/cancel/cooldown/invalid timing.
- Device: select an allowlisted target by dwell with no trigger; exit gaze before the
  threshold and verify zero action; verify accessibility progress and reduced motion.
- Failure: missing raycast target and lifecycle interruption fail closed.

### EV-S03-03

- Device-only: one continuous 20-minute run or an explicitly documented safe early stop.
- Record actual p50/p95/max frame time, checkpoints, thermal state and readability.
- No comfort PASS from simulator, short run or source inspection.

### EV-S03-04

- G00/G01 PASS only if device/viewer compatibility, dwell fallback and full physical
  protocol meet acceptance without an unresolved release blocker.
- Otherwise record FAIL/BLOCKED and select the 2D mode fallback. Do not authorize S04
  merely because the sample launches.

## 8. Planned commands

```text
python3 scripts/check_repository.py
python3 -m unittest discover -s tests -q
zsh scripts/run_unity_s02_checks.sh
zsh scripts/run_unity_s03_checks.sh
zsh scripts/collect_s03_comfort_log.sh
python3 scripts/analyze_s03_comfort_log.py <sanitized-jsonl>
python3 scripts/validate_s03_viewer_evidence.py <viewer-evidence-json>
```

Signed device build/install/launch will reuse the verified Personal Team flow without
storing the Team ID, UDID, account email, provisioning profile or private key.

## 9. Stop conditions

- Stop before any physical PASS claim, comfort claim or gate closure if no physical
  viewer/profile is available; software-only implementation and automated tests are
  explicitly authorized.
- Stop the wear test immediately on discomfort, excessive heat or unstable rendering.
- Do not buy a viewer, install paid tools/apps, change global settings or add production
  dependencies without explicit approval.
- Do not mark G00/G01 PASS with missing 20-minute evidence.
- Do not begin S04 without an explicit user request after EV-S03-04 is reconciled.

## Review checkpoint

The software-only portion is approved and may be implemented now. To resume physical
acceptance, provide the viewer brand/model or photos plus tray/trigger/lens information.
Until then T-S03-01 is blocked, T-S03-02/T-S03-03 can be only partial, and T-S03-04 plus
G00/G01 remain `NOT RUN/BLOCKED`.
