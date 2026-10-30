# Sprint S02 implementation plan — Device signing & Cardboard baseline

**Sprint:** S02  
**Module:** M01 Device / XR Foundation  
**Tasks:** T-S02-01 through T-S02-04  
**Plan date:** 2026-10-30  
**Execution state:** APPROVED / IN PROGRESS — approved by the user on 2026-10-30;
T-S02-01 build acceptance is met, T-S02-02 through T-S02-04 remain evidence-gated

## 1. Authorization and boundary

This plan is the review checkpoint required before S02 implementation. Approval of this
plan authorizes only S02. It does not authorize S03 viewer purchase, optics/comfort
claims, future modules, paid Apple enrollment, publishing, deployment, commit, push, or
storage of Apple Account, Team ID, certificate private key, device UDID, serial number,
or provisioning profile in the repository.

G00 is the S02 output gate, not an already-passed prerequisite. It may be marked PASS
only if the actual iPhone 11 builds and runs the sample with original, sanitized logs and
actual compatibility is stated explicitly. G01 remains NOT RUN until S03 viewer optics
and 20-minute comfort work.

## 2. Readiness evidence before implementation

| Check | Actual result on 2026-10-30 | S02 meaning |
|---|---|---|
| T-S01-04 dependency | DONE / PASS in S01 evidence | T-S02-01 dependency satisfied by implementation evidence; workbook remains unchanged |
| `python3 scripts/check_repository.py` | PASS | Repository and secret/signing policies remain valid |
| `zsh scripts/run_unity_s01_checks.sh` | PASS; Unity compile and 3/3 EditMode tests | Existing Unity baseline is healthy before S02 changes |
| `security find-identity -v -p codesigning` sanitized summary | PASS; 1 valid identity and 1 Apple Development identity | A local development signing identity is available |
| `xcodebuild -checkFirstLaunchStatus` | PASS; exit 0 | Xcode first-launch setup is complete |
| Sanitized `xcrun xcdevice list` | PASS; one iPhone 11, iOS 27.2, USB, available, no error | Physical device is discoverable without retaining identifiers |
| Official Cardboard sample in pinned package cache | PASS; `Hello Cardboard`, package 1.36.0, immutable commit `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68` | The exact official sample source is locally available |

The planning workbook was inspected read-only. Its S02 rows remain `Not Started`, QA
cases remain `Not Run`, and G00 remains `Not Started`. The workbook will not be edited
during implementation unless the user separately approves a proven status update.

## 3. Task mapping and order

| Order | Task | Dependency | Planned outcome | Evidence ID |
|---:|---|---|---|---|
| 1 | T-S02-01 | T-S01-04 | Generate a Unity iOS project, select the Personal Team only in Xcode, enable automatic signing, build and install a minimal signed app or retain a reproducible blocker | EV-S02-01 |
| 2 | T-S02-02 | T-S02-01 | Import the pinned official Hello Cardboard sample, configure the iOS scene/settings, build, install and launch on the iPhone 11; verify stereo or record a reproducible failure | EV-S02-02 |
| 3 | T-S02-03 | T-S02-01 | Add bounded runtime diagnostics, execute a 20-orientation head-pose protocol, recenter checks and frame-time capture on the physical device | EV-S02-03 |
| 4 | T-S02-04 | T-S02-02 and T-S02-03 | Test QR-camera allow and deny/retry, background/resume and failure paths without crashes | EV-S02-04 |

T-S02-02 and T-S02-03 may share the same signed Hello Cardboard build after T-S02-01
passes. T-S02-04 starts only after both predecessors have actual device evidence.

## 4. Planned repository changes

The implementation will keep generated Xcode output and local provisioning artifacts
under ignored paths. Exact source paths may be reduced if the official imported sample
already provides the required behavior.

| Planned path | Purpose |
|---|---|
| `client/OmniLensXR/Assets/Samples/Google Cardboard XR Plugin for Unity/1.36.0/Hello Cardboard/` | Unity Package Manager import of the pinned official sample; no hand-recreated sample |
| `client/OmniLensXR/Assets/Editor/S02IosBuild.cs` and `.meta` | Deterministic sample import/build validation and iOS Xcode-project generation; no Team ID or device identifier in source |
| `client/OmniLensXR/Assets/OmniLens/S02/S02RuntimeProbe.cs` and `.meta` | Bounded lifecycle, pose/recenter and frame-time diagnostics with sanitized local output |
| `client/OmniLensXR/Assets/Tests/Editor/S02IosBuildTests.cs` and `.meta` | Positive and invalid-config tests for scene, bundle ID format, camera usage text, orientation and deployment target |
| `client/OmniLensXR/ProjectSettings/ProjectSettings.asset` | Non-secret iOS settings: unique non-identifying bundle ID, landscape orientation, iOS 15.0 minimum, camera usage description and deferred gestures |
| `client/OmniLensXR/ProjectSettings/EditorBuildSettings.asset` | HelloCardboard scene selected for the S02 device build |
| `scripts/run_unity_s02_checks.sh` | Reproducible compile, EditMode tests and unsigned iOS project generation checks |
| `scripts/sanitize_s02_evidence.py` | Standard-library-only redaction of Team ID, UDID, serial, account email and local home path before evidence is saved |
| `tests/test_repository_policy.py` | Policy tests for S02 evidence redaction and no committed signing/provisioning material |
| `config/toolchain-versions.json` | Evidence state only after actual S02 runs; never claim device verification before it occurs |
| `client/README.md`, `README.md` | Actual S02 result and rerun instructions, including limitations |
| `docs/evidence/S02/EV-S02-01.md` through `EV-S02-04.md` | Per-task commands, sanitized logs, actual PASS/FAIL/BLOCKED results and artifact hashes |
| `docs/evidence/S02/sprint-report.md` | Sprint reconciliation and G00 decision; G01 remains NOT RUN |

Generated files remain outside source control:

- `client/OmniLensXR/Builds/iOS/S02Signing/`
- `client/OmniLensXR/Builds/iOS/HelloCardboard/`
- Xcode `DerivedData/`, `.xcuserdata/`, `.mobileprovision`, `.p12`, private keys and raw device logs
- unsanitized screenshots/video or logs containing device/account identifiers

## 5. Configuration rules

1. Import sample through Unity's package sample API from `com.google.xr.cardboard`
   1.36.0. Do not copy from an unpinned branch.
2. Use a stable, non-identifying reverse-DNS bundle ID with a locally generated suffix.
   Do not use an email, full name, Team ID or device ID.
3. Keep iOS deployment target at the S01-pinned `15.0`; the connected device is newer.
4. Set landscape orientation and a truthful camera usage description for Cardboard viewer
   QR scanning. Keep `Top`, `Left` and `Right` deferred system gestures as required by
   the official quickstart.
5. Use automatic signing in Xcode. Team selection is a GUI/local setting and must not be
   copied into repository evidence.
6. Do not add capabilities, paid services, cloud analytics or unrelated packages.
7. Do not delete/revoke certificates, profiles or the older Xcode certificate record as
   part of S02.

## 6. Verification matrix

### T-S02-01 / EV-S02-01

- Positive: Unity generates the iOS Xcode project from the current project.
- Positive: Xcode resolves automatic signing with the Personal Team and selected iPhone.
- Positive: `Unity-iPhone` Debug build succeeds, installs and launches, or the task records
  the exact sanitized build blocker.
- Negative: invalid/missing bundle ID is rejected by the Editor validation test.
- Negative: missing signing/provisioning produces a retained FAIL/BLOCKED log; it is not
  relabeled PASS.
- Policy: generated project and profiles are ignored; no Team ID/UDID/email/private key
  appears in tracked evidence.

### T-S02-02 / EV-S02-02

- Positive: official HelloCardboard scene exists at the imported versioned sample path.
- Positive: Unity compile and EditMode tests pass after import.
- Positive: app launches on the physical iPhone 11 and displays two rendered eye views.
- Failure path: launch/build/plugin failure is reproduced from a clean generated Xcode
  directory and captured with sanitized logs.
- Human observation: user confirms what appears on the phone; no simulator-only claim.

### T-S02-03 / EV-S02-03

- Runtime probe records bounded quaternion/angle and frame-time samples without location,
  camera frames, microphone content or account/device identifiers.
- A manual fixture covers 20 defined directions around neutral, then a recenter action and
  repeated neutral check.
- PASS requires all 20 motions to produce an expected pose response, recenter to establish
  a new neutral reference, and an actual frame-time report. No 60-FPS or comfort claim is
  made from a launch-only test.
- Video/screen evidence is referenced only after identifier review and stays private if it
  cannot be safely sanitized.

### T-S02-04 / EV-S02-04

- Camera/QR permission allow path: scanner opens and returns safely.
- Camera/QR permission deny path: denial is visible, app remains responsive and retry or
  Settings guidance is deterministic.
- Retry path: after permission change, the scanner can be invoked again or the exact plugin
  limitation is recorded.
- Lifecycle path: foreground → background → foreground preserves or safely reinitializes
  XR state; no crash.
- Interruption/failure logs distinguish expected permission denial from an application
  error.

## 7. Planned commands and evidence capture

Commands are run from the repository root unless noted. Exact Unity/Xcode paths are read
from the verified S01 manifest rather than guessed.

```text
python3 scripts/check_repository.py
zsh scripts/run_unity_s01_checks.sh
zsh scripts/run_unity_s02_checks.sh
xcodebuild -project <generated>/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Debug -destination generic/platform=iOS CODE_SIGNING_ALLOWED=NO build
```

The first physical signed run uses Xcode's `Signing & Capabilities` and `Product > Run`
with the connected iPhone. Subsequent command-line checks may use a locally resolved
destination, but raw identifiers are redacted before any output is written under
`docs/evidence/S02/`.

Each EV file will contain:

- timestamp, verified tool versions and task/dependency IDs;
- exact command or Computer Use action;
- exit code and actual result (PASS/FAIL/BLOCKED/NOT RUN);
- changed files and generated artifact hashes where safe;
- sanitized build/runtime excerpts, not invented summaries;
- limitations and the next reproducible action.

## 8. Human-assisted checkpoints

The following physical actions cannot be honestly automated or inferred:

1. Select the user's Personal Team in Xcode if the generated project has no team.
2. Accept any iPhone trust/developer prompt that appears.
3. Confirm stereo output on the phone.
4. Move the phone/viewer through the 20-direction fixture and trigger recenter.
5. Choose Allow once and Deny once for the QR camera permission test.
6. Background and resume the app, then confirm whether the display and input recover.

Computer Use may operate Xcode and the Mac UI. It will pause for physical iPhone actions
and will not read or store account email, Team ID, serial, UDID or private-key material.

## 9. Stop conditions and result semantics

- Stop and mark BLOCKED if signing would require paid enrollment, destructive certificate
  changes, credential export or a new global/system setting.
- Stop and mark FAIL/BLOCKED if the sample cannot build or launch; retain the exact cause.
- Do not mark T-S02-02 PASS without physical stereo observation.
- Do not mark T-S02-03 PASS without actual 20-direction, recenter and frame-time evidence.
- Do not mark T-S02-04 PASS without deny/retry/background/resume results.
- Do not mark G00 PASS until the actual iPhone 11 sample build and compatibility evidence
  satisfy the workbook gate. G01 stays NOT RUN in S02.

## 10. Sources

- Google Cardboard Unity quickstart: https://developers.google.com/cardboard/develop/unity/quickstart
- Pinned package source: https://github.com/googlevr/cardboard-xr-plugin/tree/v1.36.0
- Apple physical-device workflow: https://developer.apple.com/documentation/xcode/running-your-app-on-simulated-or-physical-devices
- Apple automatic signing/device registration: https://developer.apple.com/documentation/xcode/distributing-your-app-to-registered-devices
- Apple developer account and Personal Team limits: https://developer.apple.com/help/account/basics/about-your-developer-account

## Review decision

**APPROVED 2026-10-30.** Implementation proceeded in dependency order. See
`EV-S02-01.md` through `EV-S02-04.md` for actual results. This approval does not widen
scope beyond S02, edit the planning workbook, close G00/G01, or authorize commit,
push, deployment or paid services.
