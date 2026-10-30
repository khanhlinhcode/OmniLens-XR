# Sprint S05 report — Window registry and panel lifecycle

**Sprint:** S05  
**Tasks:** T-S05-01 through T-S05-04  
**Report date:** 2026-10-30  
**Sprint state:** PARTIAL — SOFTWARE/PHONE RUNTIME PASS / VIEWER ACCEPTANCE NOT RUN

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S05-01 | Typed versioned panel schema, safe-zone bounds, deterministic reducer/snapshot and bounded event journal | PARTIAL; software PASS, upstream T-S04-04 physical dependency pending | `EV-S05-01.md` |
| T-S05-02 | Four-kind factory, duplicate protection, bounded pooling, labels/material and state/view conflict guard | PARTIAL; automated + phone runtime PASS, viewer launcher QA NOT RUN | `EV-S05-02.md` |
| T-S05-03 | Focus stack, z-order, minimize/restore/close/pin semantics and active-view synchronization | PARTIAL; 15-transition and failure paths PASS, through-viewer interaction NOT RUN | `EV-S05-03.md` |
| T-S05-04 | Scene builder, preview, lifecycle markers and signed iPhone fixture | PARTIAL; expected IDs verified on phone, viewer QA/G02 NOT RUN | `EV-S05-04.md` |

## Actual results

| Exact command/check | Actual result |
|---|---|
| First `zsh scripts/run_unity_s05_checks.sh` | FAIL: two `CS0177` definite-assignment errors; implementation corrected |
| Baseline rerun | PASS: 87/87 EditMode, 9/9 PlayMode, iOS generation |
| Final hardening `zsh scripts/run_unity_s05_checks.sh` | PASS: final preview, 87/87 EditMode, 11/11 PlayMode and iOS generation |
| S05-only NUnit cases | PASS: 5/5 EditMode and 5/5 PlayMode |
| First device process-alive probe | PASS as plumbing only; later console capture correctly overrode it for functional status |
| First device console functional run | FAIL: stripped runtime shader caused `InvalidOperationException`; serialized material fix implemented |
| Final unsigned iPhoneOS Xcode build | PASS, `** BUILD SUCCEEDED **` |
| Final signed build/install/console/relaunch | PASS: exact registry/view markers, no managed exception and process alive after 5 seconds |
| Final visual preview review | PASS as software screenshot; no through-lens claim |

## Files and architecture

- Runtime state/factory code is under `Assets/OmniLens/S05/`; the generated source scene
  and material are in the same folder.
- Editor build and preview entry points are in `S05PanelLifecycleBuild.cs`.
- ADR-0003 records reducer, focus, event and bounded-pool decisions.
- No production package/dependency, paid API, model, credential or arbitrary string
  command path was added.

## Limitations and gate decision

- No physical viewer exists. Lens readability, gaze/focus ergonomics, mis-selection and
  comfort are `NOT RUN`.
- T-S04-04 remains PARTIAL, so T-S05-01's formal dependency is not promoted to PASS.
- G00/G01 remain blocked and G02 remains `NOT RUN` until S07.
- Pool bounds were asserted structurally; no long-duration Unity Profiler memory trace
  was captured.
- The planning workbook remains unchanged and still shows the earlier PM baseline.
- No commit, push, deployment, purchase, paid service, remotes, global SDK settings or
  repository signing identifiers were changed.

## Next unblockable task

Under the user's broad sequential authorization, the next software-first sprint is S06:
plane bounds/snapping, move/resize/pin commands, VN/EN localization and automated
fatigue-protocol tooling. Physical fatigue/comfort results must remain `NOT RUN` until a
viewer is available.
