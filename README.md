# OmniLens XR — Antigravity Starter Kit

A workspace-ready implementation repository for **Google Antigravity IDE / Antigravity App** with an iPhone 11, Apple Silicon Mac, and optional low-cost Google Cardboard viewer. S01 and S02 have implementation evidence. S03 through S08 software-first work is implemented, but physical viewer compatibility, readability and comfort remain blocked because no viewer is currently available.

## Current verified state

- FastAPI health-only foundation with fail-closed paid-service/raw-media settings.
- Pinned direct and transitive Python dependencies; 11 backend tests and 25 total repository/tooling tests.
- Shared health JSON Schema drift-tested against the Pydantic model.
- Unity Hub `3.22.5`, Unity Editor `6000.3.27f1` arm64, and iOS Build Support installed and locally verified.
- Cardboard package resolution and Unity compile are verified. The pinned official HelloCardboard `1.36.0` sample imports reproducibly, 27/27 S02 EditMode tests pass, and Unity generates its iOS Xcode project. Xcode `27.1` resolves Personal Team signing, builds, installs and launches on the connected iPhone 11. A direct device screenshot verifies two-eye stereo; stable frame windows are about 33.33 ms (approximately 30 FPS, not 60 FPS), and background/resume completed without a crash.
- A live device run captured 20 finite unique pose checkpoints plus a bounded recenter retry. Pitch/yaw/roll spans were `96.8°`/`275.8°`/`271.7°`; the retry reduced pose to `pitch 8.5°`, `yaw -3.6°`. No screen recording was produced; the user explicitly accepted that evidence deviation for T-S02-03 on 2026-10-30.
- The physical camera matrix is complete: Camera OFF produced `Camera Permission Required` with deterministic Settings guidance on two invocations; Camera ON opened the native Cardboard scanner with the iOS camera indicator, and the app returned to stereo without a crash. Unity's `Application.HasUserAuthorization(WebCam)` remained false during the native-plugin path and is retained as a diagnostic limitation, not treated as the authority for native scanner access.
- S03 now includes a fail-closed no-trigger gaze-dwell fallback, progress/cooldown/cancellation, an allowlisted `RECENTER` target, bounded 0/5/10/20-minute frame-time logger, native iOS thermal-state bridge, safe log collector and strict analyzer. Unity passes 53/53 cases (50 EditMode + 3 PlayMode raycast/lifecycle), generated/unsigned/signed iOS builds pass, the app installs and launches on iPhone 11, and all 18 Python repository/tooling tests pass.
- A sanitized physical-phone smoke log validates the runtime pipeline for 300 frames with p95 `33.486 ms`, thermal state `fair` and no interruption. It covers only `11.033 s`, so it is deliberately `INCOMPLETE`; it is not viewer or 20-minute comfort evidence.
- The future physical run now has a strict JSON template, a seated/stationary safety runbook and a 7-case fail-closed validator. A Gate PASS is rejected without measured viewer dimensions/profile, the complete dwell/accessibility matrix, an uninterrupted 0/5/10/20-minute run and retained 2D fallback.
- S04 adds versioned radial/curved layout tokens, viewer-facing panels, three deterministic high-contrast spatial fixtures, typed bounded input routing, a 30-target dwell matrix and reduced-motion/static-focus policy. Unity passes 88/88 cases (82 EditMode + 6 PlayMode); unsigned and signed iPhoneOS builds pass, and the signed app installs, launches and remains alive on iPhone 11 for the automated 5-second check.
- S05 adds a deterministic versioned `PanelRegistry`, four-kind allowlisted factory, bounded pooling/event journal, exact focus/minimize/restore/close semantics, source scene/material and lifecycle preview. Unity now passes 98/98 cases (87 EditMode + 11 PlayMode). Final unsigned/signed iPhoneOS builds pass; device console verifies `version=7`, four registered panels, three active views and the exact focused/visible IDs with no managed exception. An earlier iOS shader-stripping exception was captured and fixed with a serialized material asset.
- S06 adds full-rectangle FOV/radial placement, deterministic 5 cm snapping and collision search, typed move/resize/pin handles, 14-key VN/EN localization and a bounded non-claiming fatigue protocol. Unity now passes 108/108 cases (94 EditMode + 14 PlayMode). Final iPhone console verifies version 9, three active panels, focused+pinned Document, `vi-VN`, invariant metrics and explicit `physical=NOT_RUN`.
- S07 adds validated schema-v2 workspace persistence with v1 migration, AES-256-CBC + HMAC authenticated envelopes, staged sandbox writes, offline Home/Settings/reset, a typed local launcher and 30 bounded Agent activity fixtures. Unity now passes 120/120 cases (104 EditMode + 16 PlayMode). Signed iPhone runs prove first-save and restart restore (`restored=false` then `restored=true`), three active panels, offline Agent denial, gaze-dwell fallback and process survival after five seconds. Development FPS samples were `22.0`, `25.7` and `22.2`; no threshold claim is made.
- S08 adds a versioned scene manifest, provenance/license/hash/budget validation, a dependency-free local JSON mesh loader, procedural fallback, disposable leases and typed versioned scene selection. The checked-in baseline is a self-authored 444-byte cube with 8 vertices, 12 triangles and no texture. Unity now passes 132/132 cases (112 EditMode + 20 PlayMode). Final iPhone evidence completes 50 load/dispose cycles with active=0, peak=1 and allocated-memory delta `-143,880` bytes after unload+GC.
- S03 through S08 retain explicit physical limitations: viewer model/tray/camera obstruction/trigger/lenses/QR profile, through-lens dwell/accessibility/readability, focus ergonomics, physical FOV/handles, glare/distortion and the 10/20-minute comfort protocols are `NOT RUN`. G00/G01 remain blocked, G02 remains `NOT RUN`, and G03 is scheduled for S09.
- No paid service, model weight, private data ingestion, commit, push, or deployment has been performed.

## Sources of truth

1. `AGENTS.md` — workspace guardrails and approved sprint scope (read automatically by Antigravity).
2. `docs/planning/engineering-spec-v2.md` — canonical task specifications, acceptance criteria and dependencies.
3. `docs/planning/master-plan-v2.xlsx` — PM tracking, 68 sprints and 272 tasks (do not edit task status without actual evidence).
4. `docs/planning/pm-audit-v2.md` — baseline and limitations.
5. `.agents/skills/omnilens-sprint/SKILL.md` — Antigravity skill for audit → implement → verify → review.

## Startup

1. Extract all files into a NEW empty folder `~/Developer/omnilens-xr`, avoiding accidental overwrite of an existing project.
2. Open **the folder itself** in Antigravity IDE (File → Open Folder).
3. Use Agent **Planning Mode** and set command / artifact review permissions to require approval.
4. Paste `prompts/00-antigravity-audit.md` into the Agent. This prompt is READ-ONLY.
5. Review the plan artifact and S01 task mapping, correct any inconsistency with source-of-truth documents.
6. When satisfied, paste `prompts/01-antigravity-execute-s01.md` to authorize **S01 only**. Review changes and logs.
7. Do not move to S02 until genuine S01 evidence and review.

Antigravity recognizes `AGENTS.md` and `.agents/skills/` as workspace instructions. Old IDE Workflows have a published October 19, 2026 deprecation notice; **do not rely on `.agents/workflows/`** for this kit.

## Scope / security

- No fabrication of phone builds or FPS numbers. Device QA must use the physical iPhone 11 and actual runtime logs.
- Do not install Unity, Xcode or paid tools, enable billing, change remotes, commit or push without explicit user authorization.
- iPhone 11 + VR Box does not provide native room-scale 6DoF, eye tracking or true stereo passthrough.
- No sensitive values in commits or evidence logs.
- Do not let competing Antigravity/Codex agents modify the same files simultaneously.
# vietops-ai
