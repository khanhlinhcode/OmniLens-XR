# Sprint S06 report — Placement, manipulation, localization and fatigue tooling

**Sprint:** S06  
**Tasks:** T-S06-01 through T-S06-04  
**Report date:** 2026-10-30  
**Sprint state:** PARTIAL — SOFTWARE/PHONE RUNTIME PASS / VIEWER FATIGUE QA NOT RUN

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S06-01 | Full-rectangle FOV plane, radial bound, grid snap, deterministic collision search and 20 fixtures | PARTIAL; software PASS, physical FOV NOT RUN | `EV-S06-01.md` |
| T-S06-02 | Typed move/resize/pin, overlap/no-op guards, large handles and live-view sync | PARTIAL; automated/phone PASS, human gaze handles NOT RUN | `EV-S06-02.md` |
| T-S06-03 | Complete 14-key VN/EN catalog, fallback and locale/invariant formatting | PARTIAL; software/phone PASS, through-viewer language review NOT RUN | `EV-S06-03.md` |
| T-S06-04 | Bounded monotonic 600-second protocol tooling and no-long-text policy | PARTIAL; synthetic/device smoke PASS, physical 10-minute protocol NOT RUN | `EV-S06-04.md` |

## Actual results

| Exact command/check | Actual result |
|---|---|
| First S06 Unity run | FAIL 1/94 EditMode: angular edge passed but radial maximum failed; placement implementation hardened |
| Final `zsh scripts/run_unity_s06_checks.sh` | PASS: preview, 94/94 EditMode, 14/14 PlayMode and iOS generation |
| S06-only NUnit cases | PASS: 7/7 EditMode and 3/3 PlayMode |
| Final software preview | PASS visual QA for moved/resized/pinned state; not lens evidence |
| Final unsigned iPhoneOS build | PASS, `** BUILD SUCCEEDED **` |
| First signed 8-second console window | INCONCLUSIVE: application initialization did not reach fixture marker before capture ended |
| 20-second retry | PASS state, but exposed locale-dependent `5,0` machine value; formatter hardened |
| Final clean signed build/install/console/relaunch | PASS: exact S06 state and invariant metrics markers, no managed exception, process alive after 5 seconds |

## Limitations and gate decision

- No viewer exists; physical FOV, gaze handle usability, language readability, misclick,
  head-turn and fatigue/comfort results are `NOT RUN`.
- Synthetic 600 seconds validates tooling only and is never promoted to physical PASS.
- T-S05-04 remains PARTIAL, so the formal dependency is not promoted.
- G00/G01 remain blocked; G02 remains `NOT RUN` and cannot be closed before S07.
- Workbook, git history/remotes and signing configuration were not modified. No commit,
  push, deployment, paid service, SDK install or persisted device/signing identifier was
  performed.

## Next unblockable task

Under the user's broad sequential authorization, S07 is next: versioned local layout
serialization/migration, offline Home/Settings, reset workspace and G02 offline-core
release gate. G02 can only be marked from actual S07 evidence; viewer-dependent rows
remain open.
