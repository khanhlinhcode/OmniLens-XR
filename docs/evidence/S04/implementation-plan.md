# Sprint S04 implementation plan — Spatial design and input foundations

**Sprint:** S04  
**Module:** M02 Spatial Workspace  
**Tasks:** T-S04-01 through T-S04-04  
**Plan date:** 2026-10-30  
**Authorization:** The user explicitly authorized sequential implementation of all
sprints on 2026-10-30. This plan applies that authorization to S04 only.  
**Execution state:** IMPLEMENTED / SOFTWARE PASS / PHYSICAL GATE OPEN

## 1. Source and dependency reconciliation

Source priority is `engineering-spec-v2.md`, the read-only S04 rows in
`master-plan-v2.xlsx`, then `pm-audit-v2.md`. The workbook was read directly from its
XLSX XML package with the Python standard library; it was not edited.

| Item | Workbook state | Repository state before S04 |
|---|---|---|
| S04 | Not Started | Software-first implementation complete; physical acceptance remains PARTIAL |
| T-S04-01..04 | Not Started / 0% | Implemented in sequence; all remain PARTIAL pending viewer checks |
| EV-S04-01..04 | Not Run | Evidence recorded under `docs/evidence/S04/`; workbook intentionally unchanged |
| T-S03-04 → T-S04-01 | Pending | T-S03-04 BLOCKED because no viewer exists |
| G00/G01 | Open / evidence required | Phone/software verified; viewer optics and comfort NOT RUN |
| G02 | Not Started | Must not be evaluated in S04 |

The user's broad authorization permits preparatory S04 software work, but it does not
turn the unresolved dependency into PASS. S04 code and automated tests may become
`PARTIAL`; no S04 task may be marked fully PASS until its physical/readability
acceptance and the upstream gate are reconciled.

## 2. Task order and acceptance boundary

| Order | Task | Dependency | Software-first outcome | Deferred physical proof |
|---:|---|---|---|---|
| 1 | T-S04-01 | T-S03-04 (open) | Versioned layout tokens, safe-zone rules, three deterministic wireframe fixtures, mono/VR parity contract | Readability through actual viewer |
| 2 | T-S04-02 | T-S04-01 | Reuse S03 dwell safety; focus/raycast state and 30-target automated fixture with cancellation | 30-target through-lens interaction review |
| 3 | T-S04-03 | T-S04-01 | Typed input events/router, simulator and Cardboard adapter; missing source degrades safely | Actual trigger/controller behavior for selected viewer |
| 4 | T-S04-04 | T-S04-02, T-S04-03 | Accessibility profile, contrast/focus/reduced-motion automated checks and physical checklist | Lens readability, focus and comfort report |

T-S04-02 and T-S04-03 begin only after the software deliverable for T-S04-01 is reviewed.
T-S04-04 can integrate automated branches but remains PARTIAL without physical viewer
evidence. S05 is outside this sprint and must not start automatically.

## 3. Planned architecture

S04 will extend the existing `OmniLensXR.Runtime` assembly rather than introduce another
production dependency. S03 remains the low-level, fail-closed dwell primitive; S04 adds
spatial UI contracts and input routing without copying its state machine.

```text
Cardboard / simulator sources
          |
          v
  ISpatialInputSource
          |
          v
  SpatialInputRouter ---- rejects invalid/disabled/unsupported events
          |
          +----> focus/raycast state ----> IGazeDwellTarget allowlist
          |
          +----> typed Back/Home/Recenter/Trigger/LongPress events

SpatialUiTokens ---> layout validator ---> three-panel wireframe fixture
                         |
                         +----> accessibility profile / reduced motion
```

No arbitrary callback name, `SendMessage`, reflection-driven production command, model
output or unbounded event queue will be introduced. Input actions remain an explicit
enum/typed payload with finite dispatch and lifecycle cancellation.

## 4. Planned files

Exact names may be reduced during implementation, but scope remains S04.

| Planned path | Purpose |
|---|---|
| `docs/architecture/adr/ADR-0002-spatial-layout-input-foundation.md` | Record reuse of S03 dwell, typed input boundary and mono/VR parity |
| `docs/design/S04-spatial-layout-spec.md` | UI tokens, safe zone, placement rules and three wireframes |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialUiTokens.cs` | Bounded distances, angular/safe-zone tokens and validation |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialLayout.cs` | Pure panel pose/bounds model and deterministic layout validation |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialFocusState.cs` | Hover/focus/cancel state that composes with S03 dwell |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialInputEvent.cs` | Typed event kinds and validated head-pose/button payloads |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialInputRouter.cs` | Allowlisted, bounded dispatch and missing-source degradation |
| `client/OmniLensXR/Assets/OmniLens/S04/SpatialAccessibilityProfile.cs` | Contrast, focus visibility and reduced-motion rules |
| `client/OmniLensXR/Assets/Editor/S04SpatialFixtureBuild.cs` | Generate three deterministic fixture layouts without hardware claims |
| `client/OmniLensXR/Assets/Tests/Editor/S04SpatialFoundationTests.cs` | Unit/configuration, invalid input, layout and 30-target state tests |
| `client/OmniLensXR/Assets/Tests/PlayMode/S04SpatialInteractionPlayModeTests.cs` | Actual raycast/focus/cancellation integration fixtures |
| `scripts/run_unity_s04_checks.sh` | Reproducible compile, EditMode, PlayMode and iOS-generation checks |
| `docs/evidence/S04/EV-S04-01.md` through `EV-S04-04.md` | Actual task results with physical gaps kept NOT RUN |
| `docs/evidence/S04/sprint-report.md` | Reconciled S04 status; never closes G02 |

Generated scenes/build output remain ignored. No new package, paid service, cloud SDK or
global installation is planned.

## 5. Layout and accessibility contract

- The three required wireframes are `Home/Launcher`, `Three-panel Workspace` and
  `Settings/Accessibility`; each has deterministic mono and stereo fixture variants.
- Layout units are meters plus documented angular intent; no token is labeled readable
  through the viewer until measured.
- Primary panels stay inside a bounded forward safe zone. Invalid NaN/infinite position,
  scale, negative size, excessive distance and duplicate panel ID fail closed.
- Text tiers and contrast tokens are provisional software candidates. Physical minimum
  readable tier remains Unknown.
- Focus must have a non-motion cue. Reduced-motion disables animated scale/pulse effects
  while retaining a static focus indicator.
- Long-form typing is not a headset interaction requirement.
- Mono mode preserves the same action IDs and state semantics; it is the safe fallback
  when stereo/viewer use is unavailable.

## 6. Input and dwell contract

- Event kinds are limited to head pose, gaze enter/exit, trigger, back, home, long press,
  recenter, pause/resume and source lost/restored.
- Every event has a monotonic sequence number and finite timestamp; invalid payloads are
  rejected before state mutation.
- Missing controller/trigger does not crash and does not synthesize a press. Gaze dwell
  remains the verified fallback.
- Losing focus, pausing, disabling a source or changing target cancels pending dwell.
- One uninterrupted dwell can fire one action; gaze exit is required to re-arm.
- Recenter is an explicit allowlisted action; no generic arbitrary command execution.
- Queues and retry counts are bounded. Stale/out-of-order events are rejected.

## 7. Verification matrix

### EV-S04-01

- Positive: three fixture layouts validate in mono and stereo candidates.
- Negative: duplicate IDs, invalid dimensions, NaN/infinite pose, out-of-safe-zone primary
  panel and unsupported token version are rejected.
- Physical: through-lens readability is `NOT RUN` until a viewer exists.

### EV-S04-02

- Unit: at least 30 distinct target IDs can enter/focus/select deterministically.
- Failure: exit at threshold-minus-one-step fires zero actions; disabled/deny-layer/
  missing target fires zero actions; continued gaze never repeats.
- PlayMode: actual physics raycasts cover positive, cancellation and competing target.
- Physical: mis-selection and readability through the viewer remain `NOT RUN`.

### EV-S04-03

- Positive: simulator and Cardboard adapter emit the same typed contract.
- Invalid: NaN pose, negative timestamp, duplicate/out-of-order sequence, unsupported
  event and disabled source fail closed.
- Missing input: absent trigger/controller degrades to gaze without synthetic events.

### EV-S04-04

- Automated: text/focus/contrast candidates validate, static focus remains in
  reduced-motion mode, and motion-heavy effects are disabled.
- Physical checklist: smallest readable tier, lens focus, contrast, glare/distortion and
  comfort are `NOT RUN` until the viewer is available.
- Task cannot be fully PASS from source, simulator or automated tests alone.

## 8. Planned commands

```text
python3 scripts/check_repository.py
python3 -m unittest discover -s tests -q
.venv/bin/ruff format --check backend/src backend/tests scripts tests
.venv/bin/ruff check backend/src backend/tests scripts tests
.venv/bin/pytest -q backend/tests
zsh scripts/run_unity_s04_checks.sh
git diff --check
```

If the S04 iOS fixture is later installed, signing output remains local/sanitized and no
Team ID, UDID, certificate identity or provisioning profile is stored.

## 9. Stop conditions

- Stop and record FAIL/PARTIAL rather than weakening any 30-target, cancellation,
  invalid-input or accessibility test.
- Do not claim minimum readable text, comfort, trigger compatibility or lens quality
  without the physical viewer.
- Do not close G00, G01 or G02 from S04 software evidence.
- Do not start S05 until S04 software evidence is reconciled and this sprint is reviewed.
- Do not edit workbook status, commit, push, deploy, purchase or enable paid services.

## Execution reconciliation

The user approved continuation after this plan was created. S04 was implemented in the
order T-S04-01 → T-S04-02/T-S04-03 → T-S04-04 and the actual results are reconciled in
`sprint-report.md`. Physical checks stay `NOT RUN`, upstream gates remain open, and no
later sprint starts automatically.

