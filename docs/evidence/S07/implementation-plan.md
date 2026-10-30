# Sprint S07 implementation plan — Persistence, offline shell and G02 evidence

**Sprint:** S07  
**Module:** M02 Spatial Workspace  
**Tasks:** T-S07-01 through T-S07-04  
**Plan date:** 2026-10-30  
**Authorization:** User authorized sequential software-first execution of all planned
sprints; phone tests run now and viewer-only checks are deferred until a viewer exists.  
**Execution state:** APPROVED BY BROAD USER AUTHORIZATION / SOFTWARE-FIRST EXCEPTION

## Dependency reconciliation

The workbook was inspected read-only and remains unchanged. Rows 28–31 define S07 as
versioned layout persistence, offline Home/Settings, a 30-state mock Agent activity
timeline and the G02 gate. T-S07-01 depends on T-S06-04, whose software and phone-runtime
evidence passes but whose physical viewer protocol remains `NOT RUN`. This exception
permits S07 software implementation; it does not promote T-S06-04 or G02 to PASS.

| Order | Task | Software deliverable | Deferred proof |
|---:|---|---|---|
| 1 | T-S07-01 | Versioned validated DTO, v1→v2 migration, authenticated encryption and sandbox repository | Long-running/device backup and restore policy review |
| 2 | T-S07-02 | Offline Home/Settings model, onboarding, connectivity state, local launcher allowlist and reset | Human through-viewer usability |
| 3 | T-S07-03 | Bounded fake activity stream and 30 fixtures including empty/error | Human readability through the viewer |
| 4 | T-S07-04 | Three-panel restart restore, denied-input fallback, FPS probe and phone-runtime marker | Required device video and viewer interaction |

## Architecture and safety

- Persistence accepts only explicit panel/application enums and finite, bounded values.
  Unknown schema versions, duplicate IDs, invalid focus/selection and malformed data
  fail closed without replacing the last valid state.
- The envelope uses AES-256-CBC with a random IV plus HMAC-SHA-256 (encrypt-then-MAC).
  The generated installation key and ciphertext are kept inside the app sandbox; no
  key or plaintext is logged. This is not claimed as Keychain-backed secret storage.
- File writes use a temporary file followed by atomic replacement/move. Paths are fixed
  names under an absolute non-root sandbox and do not accept user-controlled segments.
  Unsupported replacement fails without deleting the prior valid object.
- Offline launcher actions are a typed allowlist. Local Science 3D, Settings and reset
  remain available; backend-only Agent actions report an offline error and never execute.
- Agent activity is fixture-only. It cannot dispatch model text, shell code, reflection
  calls or network requests. IDs/messages are length-bounded and the timeline is capped.
- G02 is never promoted from generated tests alone. Missing device video and viewer
  interaction remain explicit `NOT RUN` even if persistence and phone runtime pass.

## Planned files

| Path | Purpose |
|---|---|
| `docs/architecture/adr/ADR-0005-workspace-persistence-offline-shell.md` | Persistence, migration, crypto and offline boundary |
| `Assets/OmniLens/S07/WorkspacePersistence.cs` | Schema, validation, migration and JSON codec |
| `Assets/OmniLens/S07/SandboxWorkspaceRepository.cs` | Authenticated envelope and atomic sandbox storage |
| `Assets/OmniLens/S07/OfflineWorkspaceShell.cs` | Home/Settings/onboarding/network/reset allowlist |
| `Assets/OmniLens/S07/AgentActivityTimeline.cs` | Fake bounded stream and 30 fixtures |
| `Assets/OmniLens/S07/S07WorkspaceFixture.cs` | Three-panel persist/restore/offline runtime probe |
| `Assets/Editor/S07WorkspaceBuild.cs` | Reproducible scene/preview/iOS generation |
| `Assets/Tests/Editor/S07WorkspaceTests.cs` | Migration, tamper, invalid input, offline and 30 fixtures |
| `Assets/Tests/PlayMode/S07WorkspacePlayModeTests.cs` | Runtime persistence/reset/restore integration |
| `scripts/run_unity_s07_checks.sh` | Preview, complete tests and iOS generation |
| `docs/evidence/S07/EV-S07-01.md` through `EV-S07-04.md` | Actual command/test/device outcomes |
| `docs/evidence/S07/sprint-report.md` | Reconciled task and gate state |

## Verification

- Round-trip v2 and v1→v2 migration preserve three panel positions, focus, pin state,
  visibility and selected app; unknown versions and invalid/duplicate panels are rejected.
- Wrong keys, modified ciphertext/MAC, truncated envelopes and invalid JSON fail without
  returning plaintext or mutating repository state.
- Offline mode launches local Science 3D and Settings, rejects backend Agent action,
  and reset clears persisted state then returns the documented default workspace.
- Exactly 30 deterministic activity fixtures render, including empty, pending, accepted,
  failed, cancelled and stream-error states; malformed/overflow events are rejected.
- Runtime fixture reports restore across repository re-open, typed denied-input fallback,
  three active panels, an invariant FPS sample and `viewer_video=NOT_RUN`.
- Run repository, Python, Ruff, Unity, unsigned Xcode and signed iPhone checks where the
  local toolchain and connected phone permit.

## Stop conditions

- Do not edit the workbook, start S08 before S07 evidence reconciliation, close G02,
  commit/push/deploy, install dependencies or persist signing/device identifiers.
- Do not call phone-only runtime evidence viewer/video evidence.

## Post-implementation reconciliation

- Final Unity result is 104/104 EditMode and 16/16 PlayMode; S07 contributes 10/10 and
  2/2 respectively. Repository/tooling passes 25/25 and backend passes 11/11.
- Final unsigned and signed iPhoneOS builds, signed install and normal launch pass.
  First fixture run created schema v2 (`restored=false`); relaunch loaded the sandbox
  state (`restored=true`) with three active panels.
- Offline Agent denial, gaze-dwell fallback and exactly 30 activity fixtures are present
  on all phone runs. Development FPS samples are 22.0, 25.7 and 22.2 with no threshold claim.
- Required device video and human through-viewer interaction remain `NOT RUN`; G02 is
  therefore not closed.
