# Sprint S03 report — Software-first viewer feasibility work

**Sprint:** S03  
**Tasks:** T-S03-01 through T-S03-04  
**Report date:** 2026-10-30  
**Sprint state:** PARTIAL / WAITING FOR PHYSICAL VIEWER

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S03-01 | Physical inventory protocol and required fields defined | BLOCKED; viewer unavailable, measurements NOT RUN | `EV-S03-01.md` |
| T-S03-02 | Fail-closed gaze-dwell fallback, allowlisted target contract, explicit recenter, progress/cooldown/cancellation | PARTIAL; 23/23 EditMode and 3/3 PlayMode S03 cases PASS, physical/accessibility QA NOT RUN | `EV-S03-02.md` |
| T-S03-03 | Bounded comfort probe, native thermal category, collector and strict analyzer | PARTIAL; tooling/build tests PASS, 20-minute viewer run NOT RUN | `EV-S03-03.md` |
| T-S03-04 | Dependency/gate reconciliation and 2D fallback | BLOCKED; G00/G01 not closed | `EV-S03-04.md` |

## Actual automated results

| Exact command | Actual result |
|---|---|
| `python3 scripts/check_repository.py` | PASS after S03 artifacts were registered |
| `python3 -m unittest discover -s tests -q` | PASS, 25/25 |
| `.venv/bin/ruff format --check backend/src backend/tests scripts tests` | PASS |
| `.venv/bin/ruff check backend/src backend/tests scripts tests` | PASS |
| `.venv/bin/pytest -q backend/tests` | PASS, 11/11 |
| `zsh scripts/run_unity_s03_checks.sh` | PASS: configure, 50/50 EditMode, 3/3 PlayMode, iOS project generation |
| Unsigned Xcode iPhoneOS build | PASS: native thermal bridge compiled/linked; `** BUILD SUCCEEDED **` |
| Automatic-signing build/install/process launch | PASS on the connected iPhone 11 |
| Sanitized phone runtime smoke | PARTIAL as intended: 300 frames / `11.033 s`, p95 `33.486 ms`, thermal `fair`, no interruption; missing 5/10/20-minute checkpoints, analyzer `INCOMPLETE` |
| Viewer evidence validator | PASS, 7/7 positive/blocked/premature-PASS/temperature/checkpoint/privacy cases; repository template reports `INCOMPLETE` as intended |
| `git diff --check` | PASS |

The Xcode command used the generated `S03ViewerFixture/Unity-iPhone.xcodeproj`, scheme
`Unity-iPhone`, Debug iPhoneOS, `/tmp/omnilens-s03-derived`, and
`CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO`. Generated builds and local logs are
ignored and are not committed.

## Implemented source and tooling

- Pure deterministic dwell state, allowlisted selector and progress ring under
  `Assets/OmniLens/S03/`.
- Native iOS `NSProcessInfo.thermalState` bridge under `Assets/Plugins/iOS/`.
- Reproducible scene configuration/iOS generation, 23 S03 EditMode cases and 3 S03
  PlayMode physics/lifecycle cases.
- Strict standard-library JSONL analyzer, safe device collector and one-command Unity
  verification script.
- Strict physical-evidence JSON template/validator and seated/stationary QA runbook;
  synthetic validator fixtures are not treated as hardware evidence.
- The imported sample change is limited to adapting its existing teleport action to
  `IGazeDwellTarget`; the target already resides on allowlisted layer 7.

## Limitations and gate decision

- No viewer model, tray dimensions, lens controls, trigger type or QR profile is known.
- No through-lens readability, glare/distortion, accidental dwell activation or
  20-minute comfort result exists.
- Signed phone launch and the short runtime smoke validate plumbing only; neither is
  promoted to viewer or comfort acceptance.
- Automated/source/build success cannot satisfy physical acceptance. T-S03-01 and
  T-S03-04 remain blocked; T-S03-02 and T-S03-03 remain partial.
- G00/G01 remain open/blocked, the 2D fallback remains active, and S04 was not started.
- The planning workbook was not modified. No commit, push, deployment, purchase, paid
  service or global dependency installation was performed.

## Next unblockable task

Obtain or borrow one concrete viewer. Then record its model/photos, tray dimensions,
camera/trigger/lens/QR behavior in EV-S03-01; run the dwell cancellation/accessibility
matrix and the safe 0/5/10/20-minute protocol; analyze the sanitized log; and only then
decide T-S03-02/T-S03-03 plus G00/G01 in T-S03-04.

