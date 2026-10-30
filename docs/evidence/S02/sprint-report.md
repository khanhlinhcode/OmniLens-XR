# Sprint S02 report — Device signing and Cardboard baseline

**Sprint:** S02  
**Tasks:** T-S02-01 through T-S02-04  
**Report date:** 2026-10-30  
**Sprint state:** IN PROGRESS / PHYSICAL MOTION AND PERMISSION QA REQUIRED

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S02-01 | Deterministic iOS settings, signing probe, Xcode project and redaction tooling | PASS: signed Xcode build/install and later device process launch verified | `EV-S02-01.md` |
| T-S02-02 | Official pinned HelloCardboard import, XR loader, scene configuration and iOS generation | PASS: physical launch, two-eye stereo screenshot and device-parameter return verified | `EV-S02-02.md` |
| T-S02-03 | Bounded pose/recenter/frame-time probe and 20-orientation protocol | IN PROGRESS: stable frame windows captured; pose/recenter fixture NOT RUN | `EV-S02-03.md` |
| T-S02-04 | Camera/device-parameter and lifecycle diagnostics; deny/retry/allow/resume protocol | IN PROGRESS: background/resume no-crash PASS; denied/retry/allow NOT RUN | `EV-S02-04.md` |

## Implemented source

- `S02IosBuild.cs`: official sample import, validated non-identifying bundle ID, iOS
  settings, Cardboard XR loader and reproducible Unity iOS builds.
- `S02RuntimeProbe.cs`: bounded local JSONL lifecycle, pose/recenter and frame-time
  diagnostics without raw media or identifiers.
- `S02IosConfigurationTests.cs`: positive and invalid-input/configuration paths.
- Runtime/editor/test assembly definitions make production code directly testable.
- `run_unity_s02_checks.sh` and `sanitize_s02_evidence.py`: repeatable QA and safe
  evidence handling.

## Actual automated results

- Repository policy: PASS, 9/9 after adding S02 redaction coverage.
- Unity S02 import/configuration: PASS.
- Unity S02 EditMode: PASS, 13/13, 0 failed.
- Unity HelloCardboard iOS project generation: PASS, 0 errors, 1 warning.
- Signing probe unsigned Xcode build: PASS.
- Signing probe automatic-signing build/install: PASS on the connected iPhone 11.
- Physical HelloCardboard build/install/launch: PASS on the iPhone 11.
- Physical two-eye stereo screenshot: PASS, safe app-only image reviewed.
- Stable runtime frame windows: approximately 33.33 ms average/p95 (about 30 FPS), not
  60 FPS.

## Gate state and limitations

- No stereo, head pose, recenter, frame-time, QR permission or lifecycle result is
  inferred from source/build success.
- G00 is not closed here; the physical sample run is incomplete and the canonical spec
  assigns final G00/G01 closure to T-S03-04 after S03 viewer/readability/comfort work.
- G01 remains NOT RUN. No viewer optics or comfort claim is made.
- The planning workbook remains unmodified and may not be reconciled to these evidence
  files until a separate evidence-backed status update is approved.
- No commit, push, deployment, paid service, credential export or secret retention was
  performed.

## Exact next unblockable action

With HelloCardboard still running on the physical iPhone, execute the EV-S02-03
20-orientation/recenter fixture and EV-S02-04 deny/retry/allow sequence. Then retrieve
the app-container JSONL log and reconcile this report.
