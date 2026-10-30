# OmniLens XR — Antigravity Starter Kit

A workspace-ready implementation repository for **Google Antigravity IDE / Antigravity App** with an iPhone 11, Apple Silicon Mac, and optional low-cost Google Cardboard viewer. S01 implementation evidence is complete. S02 is in progress: the official HelloCardboard sample, deterministic iOS build tooling, runtime diagnostics and QA protocols are implemented; a signed build has installed on the physical phone, while launch/stereo and motion/permission QA await explicit trust of the local developer certificate on that iPhone.

## Current verified state

- FastAPI health-only foundation with fail-closed paid-service/raw-media settings.
- Pinned direct and transitive Python dependencies; 11 backend tests and 9 repository-policy tests.
- Shared health JSON Schema drift-tested against the Pydantic model.
- Unity Hub `3.22.5`, Unity Editor `6000.3.27f1` arm64, and iOS Build Support installed and locally verified.
- Cardboard package resolution and Unity compile are verified. The pinned official HelloCardboard `1.36.0` sample imports reproducibly, 13/13 S02 EditMode tests pass, and Unity generates its iOS Xcode project. Xcode `27.1` resolves Personal Team signing, builds and installs on the connected iPhone 11. iOS currently blocks first launch until the device trusts that local developer certificate; no stereo, head-pose, frame-time or permission PASS is claimed yet.
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
