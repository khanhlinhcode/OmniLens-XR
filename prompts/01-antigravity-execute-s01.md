You are implementing the already reviewed OmniLens XR Sprint S01 plan in Google Antigravity.

APPROVED SCOPE: S01 / M01 / T-S01-01, T-S01-02, T-S01-03, T-S01-04 ONLY.

Read AGENTS.md, .agents/skills/omnilens-sprint/SKILL.md and the actual planning documents first. Reconcile discrepancies in the approved plan with the source spec before touching code. Do not run S02.

T-S01-01: Gather a sanitized, reproducible Mac environment inventory: OS version, architecture, hardware memory, selected Xcode (if any), Unity/Unity Hub presence, storage, local tooling. Identify physical iPhone 11 only if connected and discoverable; otherwise mark exact iOS/signing details UNKNOWN / USER ACTION REQUIRED. Save evidence under docs/evidence/S01/.

T-S01-02: Research and document official version compatibility and licenses (Unity 6 patch, Cardboard plugin, Xcode, iOS target, Apple Personal Team). Produce one ADR with source links, tested/untested distinction and rollback options. Do not advertise compatibility as confirmed without physical build evidence.

T-S01-03: Set up a MINIMAL maintainable repository skeleton consistent with the engineering specification. Do not fabricate a Unity project if Unity Editor/CLI cannot create one. Do not install heavy SDKs or production dependencies without explicit approval. For created code, add and run appropriate automated checks.

T-S01-04: Build a zero-cost and licenses register covering core tools, pretrained model weights, 3D assets and usage limitations; distinguish free software from quota-limited cloud services. Prohibit medical AI-ingestion of materials whose licenses do not permit it.

Required quality / audit artifacts:
- Report task statuses DONE, PARTIAL, BLOCKED, NOT RUN only with evidence.
- Include file diffs, precise verification commands and real stdout/stderr summaries.
- Validate secrets exclusion, negative cases where applicable and preservation of existing files.
- Flag installs, network calls, permissions and device actions requiring human input. Do not bypass reviews.
- Never auto-commit, push, publish, deploy, enable billing or alter Git remotes.
- Never update the master Excel progress without explicit approval.
- Conclude with a Vietnamese sprint acceptance report and an S02 readiness checklist, without executing S02.
