# Sprint S07 report — Persistence, offline shell and G02 evidence

**Sprint:** S07  
**Tasks:** T-S07-01 through T-S07-04  
**Report date:** 2026-10-30  
**Sprint state:** PARTIAL — SOFTWARE/PHONE RUNTIME PASS / G02 VIEWER VIDEO NOT RUN

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S07-01 | Validated schema v2, v1 migration, authenticated encrypted envelope, staged sandbox repository and reducer replay | PASS software + phone restart restore; Keychain hardening remains future work | `EV-S07-01.md` |
| T-S07-02 | Offline Home/Science 3D/Settings/reset/onboarding/network shell and denied Agent path | PARTIAL; software/phone PASS, through-viewer UX NOT RUN | `EV-S07-02.md` |
| T-S07-03 | Bounded non-executing activity timeline with exactly 30 fixtures | PARTIAL; fixtures PASS, through-viewer readability NOT RUN | `EV-S07-03.md` |
| T-S07-04 | Three-panel restart, denied input fallback, development FPS markers and process check | PARTIAL; automated/phone PASS, required video + physical interaction NOT RUN | `EV-S07-04.md` |

## Final actual results

- Unity: 104/104 EditMode and 16/16 PlayMode; S07 contributes 10/10 and 2/2.
- Repository/tooling: 25/25; backend: 11/11; Ruff: PASS.
- `git diff --check`: PASS after removing three Unity-generated trailing spaces.
- Workbook integrity: unchanged (`git diff --quiet -- docs/planning/master-plan-v2.xlsx`).
- Unsigned and final signed iPhoneOS builds: PASS; install and launch: PASS.
- Phone persistence: first run `restored=false`, relaunch `restored=true`, three active
  panels both times. Process remained alive after five seconds.
- Development FPS samples: 22.0, 25.7 and 22.2. They are measurements only, with no threshold
  or physical-viewer performance conclusion.

## Failures retained

- A root-level combined `pytest` collection failed because the backend `src` directory
  was not on `PYTHONPATH`; correct isolated commands then passed 25 and 11 tests.
- One retry was invoked from the backend directory with a root-relative `.venv` path and
  failed before tests; the corrected root command passed both suites.
- Two signed builds failed before compilation: missing team, then selection of an empty
  team setting. The final command used the existing non-empty team in-memory only and
  passed. No Team ID, device identifier or signing identity was written to the repo.

## Limitations and gate decision

- No viewer exists. Required device video, physical denied-input recovery, through-lens
  Home/Settings/timeline readability and human interaction are `NOT RUN`.
- The sandbox key is not iOS Keychain-backed; no stronger at-rest claim is made.
- T-S06-04 remains PARTIAL, so the formal upstream physical dependency is not promoted.
- G00/G01 remain blocked and G02 remains `NOT RUN`.
- Workbook, git history/remotes and global signing settings were not modified. No commit,
  push, deployment, paid service or dependency/SDK installation was performed.

## Next unblockable task

S08 is the next software-first sprint only after this S07 reconciliation. Viewer-dependent
G02 evidence remains queued for the day a viewer is available.
