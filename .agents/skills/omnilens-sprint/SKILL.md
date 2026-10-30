---
name: omnilens-sprint
description: Plan, execute and audit one approved OmniLens XR sprint against the V2 WBS, engineering specification, test evidence and quality gates. Use for sprint handoff, implementation, QA, status reconciliation and Codex/Antigravity code review.
---

# OmniLens sprint execution and evidence

## Source priority

Read `AGENTS.md`, `docs/planning/engineering-spec-v2.md`, `docs/planning/pm-audit-v2.md`, and the relevant rows in `docs/planning/master-plan-v2.xlsx`. Never infer workbook results from its filename; when XLSX parsing is unavailable, state so, and use the engineering specification as the readable reference.

## Workflow

1. Determine the exact authorized Sprint ID. By default **S01 ONLY**; verify the user approved the current sprint.
2. Map Sprint → module → task IDs → dependency IDs → evidence/test IDs → acceptance criteria. Report blockers and prerequisite status.
3. Inspect `git status` and workspace; preserve existing changes. Inventory tooling and permissions with read-only checks before coding.
4. Produce an implementation plan artifact with distinct files to change, tests, risk controls, and proof required. Request plan review first.
5. Implement only the approved tasks, with small reversible changes. No fake Unity project, fake test or false device compatibility claim.
6. Run available positive/negative tests. On command failure, save the actual error and classify NOT RUN / FAIL / BLOCKED correctly.
7. Record sanitized task-level evidence under `docs/evidence/SXX/`. Include commands, versions (where verified), changed file paths, actual outputs, limitations.
8. Give a Vietnamese sprint report. Explicitly distinguish DONE (evidence), PARTIAL, BLOCKED, NOT STARTED. Never approve a hardware gate on simulator-only evidence.
9. Do not update XLSX percent/done status without explicit approval and proof. Never proceed to S02+ automatically.

## Technical safety

- No paid services, global installs, automatic Git commit/push/deploy, destructive shell commands, secrets, or unsanctioned codebase changes.
- Consider the physical iPhone, Xcode signing, Unity availability, lens parameters, and Wi-Fi conditions **unknown until checked**.
- Prefer local tests and reproducible command outputs to screenshots of a proposed design.
- Protect the device and user: VR-based maps are stationary route previews; active outdoor navigation must use normal 2D mode.
