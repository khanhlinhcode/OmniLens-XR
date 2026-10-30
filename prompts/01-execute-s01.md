# Codex: Implement OmniLens XR Sprint S01 only

The user approves implementing **Sprint S01 / Module M01**, task IDs T-S01-01, T-S01-02, T-S01-03, and T-S01-04, subject to repository rules in `AGENTS.md`.

Canonical sources:
- `docs/planning/engineering-spec-v2.md` (task-level implementation and acceptance)
- `docs/planning/master-plan-v2.xlsx` (WBS, sprint schedule, QA mapping)
- `docs/planning/pm-audit-v2.md` (baseline, open risks)

Complete only these S01 deliverables:

T-S01-01 — Gather READ-ONLY Mac evidence: `sw_vers`, `uname -m`, RAM/hardware metadata, Xcode selection/version, Unity Hub or Unity installation presence, available storage, local developer tools. Record iPhone 11 exact iOS version and device signing state ONLY if the device is actually discoverable; otherwise report `UNKNOWN / USER ACTION REQUIRED`. Save sanitized inventory at `docs/evidence/S01/` and create a device matrix. Never include private serial numbers, Apple ID or credentials in tracked files.

T-S01-02 — Research/verify current official compatibility matrix for Unity 6 patch, Google Cardboard XR plugin, Xcode, iOS and viewer profile. Record exact sourced version constraints, alternatives, ADR rationale, release URLs and open blockers in `docs/architecture/adr/`. Do not declare the stack confirmed if required components or physical device tests are missing.

T-S01-03 — Create minimal, maintainable repository foundation *only where justified by the spec*: Unity client project structure if Unity tooling is available; otherwise prepare explicit setup instructions without fake project artifacts. For Python create a lean FastAPI skeleton **only if dependencies/install are approved**; else prepare non-executable structure and precise setup plan. Add .gitignore, environment templates without secrets, tests/checks appropriate to actual created files. Avoid speculative architectural overengineering.

T-S01-04 — Create license/cost register for Unity Personal, Cardboard, Xcode/signing, CV/RAG libraries, speech libraries, sample models and content sources. Classify freeware/open-source/free-tier/paid, show source URLs and explicit unknowns. Include strict no-paid-services rule and no OpenStax AI ingestion without verified permission.

Testing and evidence:
- Run all offline/read-only checks available on the Mac.
- Run automated tests for actual code created. Report exact commands/output, or `NOT RUN` plus the reason; never fabricate PASS.
- Prepare S02 `HelloCardboard` checklist, **do not execute S02**.
- For each T-S01-xx, include Done/Blocked/Partial, evidence path and remaining proof.
- Do not edit PM workbook status unless user specifically asks and actual evidence supports it.

Hard restrictions: no destructive shell commands, no global installs, no changing Git remotes, no paid API keys or cloud billing, no automatic commit or Git push, no Unity/Xcode install without approval; do not touch projects outside this repo.

At the end: concise Vietnamese delivery report with file paths, evidence, commands, status for four S01 tasks, blocked issues and recommended next action.
