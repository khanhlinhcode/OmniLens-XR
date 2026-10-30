# Sprint S02 report — Device signing and Cardboard baseline

**Sprint:** S02  
**Tasks:** T-S02-01 through T-S02-04  
**Report date:** 2026-10-30  
**Sprint state:** COMPLETE / AUTHORIZED S02 TASKS PASS

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S02-01 | Deterministic iOS settings, signing probe, Xcode project and redaction tooling | PASS: signed Xcode build/install and later device process launch verified | `EV-S02-01.md` |
| T-S02-02 | Official pinned HelloCardboard import, XR loader, scene configuration and iOS generation | PASS: physical launch, two-eye stereo screenshot and device-parameter return verified | `EV-S02-02.md` |
| T-S02-03 | Bounded pose/recenter/frame-time probe and 20-orientation protocol | PASS WITH APPROVED DEVIATION: runtime motion 20/20, recenter retry and frame-time PASS; user accepted absent video artifact | `EV-S02-03.md` |
| T-S02-04 | Camera/device-parameter and lifecycle diagnostics; deny/retry/allow/resume protocol | PASS: denial dialog, deterministic retry/Settings guidance, allow scanner and background/resume no-crash observed | `EV-S02-04.md` |

## Implemented source

- `S02IosBuild.cs`: official sample import, validated non-identifying bundle ID, iOS
  settings, Cardboard XR loader and reproducible Unity iOS builds.
- `S02RuntimeProbe.cs`: bounded local JSONL lifecycle, pose/recenter and frame-time
  diagnostics without raw media or identifiers; tap/hold classification prevents
  duplicate checkpoints and keeps recenter from consuming checkpoint 21.
- `S02IosConfigurationTests.cs`: positive and invalid-input/configuration paths.
- Runtime/editor/test assembly definitions make production code directly testable.
- `run_unity_s02_checks.sh` and `sanitize_s02_evidence.py`: repeatable QA and safe
  evidence handling.

## Actual automated results

| Exact command | Actual result |
|---|---|
| `python3 scripts/check_repository.py` | PASS; required files, S02 evidence state, ignore rules and obvious-secret scan |
| `python3 -m unittest tests.test_repository_policy -q` | PASS, 11/11 |
| `.venv/bin/ruff format --check backend/src backend/tests scripts tests` | PASS, 8 files already formatted |
| `.venv/bin/ruff check backend/src backend/tests scripts tests` | PASS, no lint findings |
| `.venv/bin/pytest -q backend/tests` | PASS, 11/11 |
| `zsh scripts/run_unity_s01_checks.sh` | PASS; Unity compile and EditMode 20/20 |
| `zsh scripts/run_unity_s02_checks.sh` | PASS; sample configuration/import, EditMode 27/27 and HelloCardboard iOS project generation |

- Unity HelloCardboard iOS project generation reported 0 errors and 1 warning.
- Signing probe unsigned Xcode build: PASS.
- Signing probe automatic-signing build/install: PASS on the connected iPhone 11.
- Physical HelloCardboard build/install/launch: PASS on the iPhone 11.
- Physical two-eye stereo screenshot: PASS, safe app-only image reviewed.
- Corrected physical input smoke: PASS; one short tap produced one checkpoint, a
  1.334-second hold produced one recenter event without a checkpoint, and the next tap
  produced the next single checkpoint.
- Physical motion log: PASS for 20/20 finite unique pose checkpoints; pitch/yaw/roll
  spans are 96.8°/275.8°/271.7°.
- Targeted recenter retry: PASS; pose reduced from `pitch 25.8°`, `yaw -11.2°` to
  `pitch 8.5°`, `yaw -3.6°` after a 1.333-second hold.
- Video artifact: NOT PRODUCED; user explicitly accepted this evidence deviation for
  T-S02-03 on 2026-10-30. No recording is claimed.
- Stable runtime frame windows: approximately 33.33 ms average/p95 (about 30 FPS), not
  60 FPS.
- Physical Camera OFF: PASS; `Camera Permission Required` remained responsive and
  offered `Cancel`/`Settings` on two scanner invocations.
- Permission recovery: PASS; Settings showed Camera OFF, then Camera ON after the
  approved toggle. With Camera ON the native Cardboard scanner opened with the iOS
  camera privacy indicator and no permission dialog.
- Scanner/lifecycle return: PASS; an earlier physical `Skip` returned to stereo, and the
  final verification launch rendered stereo after the permission sequence without a
  crash.

Local Unity logs are under `/tmp/omnilens-unity-s01-*` and
`/tmp/omnilens-unity-s02-*`. The sanitized physical runtime excerpt is committed as
`EV-S02-03-runtime-smoke.jsonl`, `EV-S02-03-runtime-motion.jsonl`,
`EV-S02-03-runtime-recenter.jsonl` and `EV-S02-04-runtime.jsonl`; raw Xcode/device logs
remain local because they can contain account and device identifiers.

## Files changed in this continuation

- Runtime and test: `S02RuntimeProbe.cs`, `S02IosConfigurationTests.cs`.
- Verified-state policy: `config/toolchain-versions.json`, `scripts/check_repository.py`,
  `tests/test_repository_policy.py`.
- Handoff and evidence: root/client README, `EV-S02-03.md`, `EV-S02-04.md`,
  four bounded S02 runtime JSONL files, `implementation-plan.md`, this report.

## Gate state and limitations

- No stereo, head pose, recenter, frame-time, QR permission or lifecycle result is
  inferred from source/build success.
- G00 is not closed here because the canonical spec assigns final G00/G01 closure to
  T-S03-04 after S03 viewer/readability/comfort work, not because S02 device QA is being
  inferred or omitted.
- G01 remains NOT RUN. No viewer optics or comfort claim is made.
- The planning workbook remains unmodified and may not be reconciled to these evidence
  files until a separate evidence-backed status update is approved.
- No commit, push, deployment, paid service, credential export or secret retention was
  performed.

## Exact next unblockable action

Review the completed S02 evidence. Do not start S03 until the user explicitly authorizes
that sprint and its gate; G01 viewer optics/readability/comfort remains NOT RUN.
