# OmniLens XR — Project instructions for Antigravity and Codex

## Mission and scope
- Build OmniLens XR as a software-first, low-cost iPhone 11 + Mac spatial workstation with AI Agent, Computer Vision, RAG, Voice, 3D Science, Engineering, Medical Anatomy, Learning Progress, Recommendation and Maps in later approved phases.
- For this repository, follow `docs/planning/engineering-spec-v2.md`, `docs/planning/master-plan-v2.xlsx`, and `docs/planning/pm-audit-v2.md` in that order for detailed tasks / PM tracking. New implementation evidence overrides untested assumptions. Never fabricate a PASS.
- Only implement the currently authorized sprint. Initially **S01 only**, task IDs T-S01-01 through T-S01-04. Do not start future sprints without an explicit user request and the associated gate.

## Engineering boundaries
- Device baseline: iPhone 11 and MacBook Apple Silicon. Never assume exact iOS, macOS, RAM, Unity, Xcode versions or viewer properties; inspect them or mark Unknown.
- Mobile XR target: Unity 6 + official Google Cardboard XR plugin as determined by current compatibility audit; iOS build via Xcode. Verify release requirements before pinning versions.
- AI backend target: Python/FastAPI modular monolith, typed tool contracts, SQLite/FAISS as justified by later requirements; no Kubernetes or microservices in S01.
- AI Agent must use typed allowlisted tools, bounded execution, scene state checks, verification and recovery; never execute arbitrary model-generated code.
- VR Box has no native real eye tracking, room-scale 6DoF or high-grade stereo passthrough. Never claim Apple Vision Pro hardware parity.
- Medical features are education-only; verify anatomy and licensing before asset ingestion. Map previews in opaque VR headset are stationary-only, and real navigation must hand off to 2D.
- No paid LLM/API/billing or hosting without explicit opt-in. Never put API keys, credentials, private medical documents or precise location in source control or model logs.

## Development quality
- Before modifying files, inspect repository state and existing changes; protect user work. Do not remove unrelated changes.
- Do not add new production dependencies, change global/system settings, install heavy SDKs, run destructive commands or modify Git remotes without explicit approval.
- Provide implementation evidence for each task: actual commands, logs, test output, files changed, blockers and remaining work under `docs/evidence/`.
- Mark tasks and QA gates PASS only with evidence. Failed or unrun tests must be labeled accordingly.
- Tests should include positive, invalid-input, permission and failure paths where applicable; do not weaken tests to produce a green result.
- Keep architectural decisions in `docs/architecture/adr/`. Prefer minimal maintainable code over speculative scaffolding.
- Do not automatically commit, push, deploy, publish or activate paid services.

## Mandatory response format
At the end of each agent task, summarize: Sprint/Task IDs; files changed; implementation; exact tests and actual PASS/FAIL/NOT RUN results; evidence paths; limitations; next unblockable tasks. Use Vietnamese for the progress report, preserve code identifiers in English.
