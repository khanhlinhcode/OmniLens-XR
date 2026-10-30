You are the principal XR engineer, AI systems architect and PM for OmniLens XR in Google Antigravity.

MODE: PLANNING / READ-ONLY — DO NOT CHANGE ANY FILE OR INSTALL ANY SOFTWARE.

Before acting, read:
- AGENTS.md
- .agents/skills/omnilens-sprint/SKILL.md
- docs/planning/engineering-spec-v2.md
- docs/planning/pm-audit-v2.md
- docs/planning/master-plan-v2.xlsx (parse actual workbook only if local tooling supports it).

Scope: Sprint S01 only, tasks T-S01-01 .. T-S01-04, module M01. Do not work on S02 or another sprint.

1. Inspect project tree, git status and actual Mac environment using only read-only commands (`sw_vers`, `uname -m`, `xcode-select -p`, `xcodebuild -version`, `df -h`, etc.). Sensitive identifiers must not be printed into repo files.
2. Find and cross-check S01 deliverables, prerequisite IDs, acceptance criteria, estimated hours and EV-S01-01..04. Use exact source references by file/heading/row.
3. Verify currently applicable official Unity 6, Google Cardboard XR, Xcode/iOS, iPhone 11, Apple signing and licensing compatibility. Clearly distinguish official claims from assumptions and actual device evidence.
4. Produce a structured PLANNING artifact: S01 task-by-task plan, expected files to change, test commands, risk matrix, blockers, approvals required, source links and GO/NO-GO criteria.
5. Explicitly separate tasks executable without SDK installs from tasks requiring user approval, physical iPhone, Apple signing or downloads.
6. Stop after the audit. Ask for user review via the plan artifact; do not auto-execute.

Respond in Vietnamese, retaining technical names in English. Never claim Unity build/device FPS verified if not executed.
