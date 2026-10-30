# Sprint S01 implementation report

**Sprint:** S01  
**Module:** M01 Device / XR Foundation  
**Execution date:** 2026-10-29; continued 2026-10-30  
**Overall result:** COMPLETE / EVIDENCE READY  
**Next sprint:** S02 NOT STARTED

## Task status

| Task | Status | Implemented evidence | Remaining proof/blocker |
|---|---|---|---|
| T-S01-01 | DONE / PASS | Sanitized Mac inventory; Unity/Xcode stack; iPhone 11 on USB, iOS 27.2, available, paired/trusted, Developer Mode enabled; sensor baseline; one valid signing identity | Runtime/device behavior intentionally belongs to S02/S03 |
| T-S01-02 | DONE / PASS | ADR-0001, exact versions/rollback, official URLs, Cardboard immutable commit, package resolution, compile and 3/3 Unity tests | Physical compatibility remains provisional for S02/S03 |
| T-S01-03 | DONE / PASS | Git repository, Unity project/compile/tests, fail-closed FastAPI, locked dependencies, shared schema, 11 backend tests, 8 policy tests, CI workflow | Hosted CI NOT RUN because no remote/push is authorized |
| T-S01-04 | DONE / PASS | License/cost register, Python license audit, Unity Personal decision, SDK/certificate checksums and prohibited-content controls | Future weights/assets still require their own license/hash approvals |

All four S01 task acceptance criteria have evidence. G00/G01 remain **NOT RUN / NOT PASSED** because they close only after later physical build, viewer and comfort work.

## Files created or updated

- `.gitignore`, `.env.example`
- `.github/workflows/ci.yml`
- `config/toolchain-versions.json`
- `client/OmniLensXR/` generated project, pinned packages and EditMode tests
- `client/README.md`, `backend/README.md`, `contracts/README.md`, `contracts/health-response.schema.json`
- `backend/src/omnilens_api/`, `backend/tests/`, pinned requirement files and `backend/pyproject.toml`
- `scripts/check_repository.py`
- `tests/test_repository_policy.py`
- `docs/device-matrix/environment-inventory.md`
- `docs/architecture/adr/ADR-0001-xr-foundation-toolchain.md`
- `docs/licenses/initial-license-register.md`
- `docs/evidence/S01/implementation-plan.md`
- `docs/evidence/S01/EV-S01-01.md` through `EV-S01-04.md`
- `docs/evidence/S01/backend-dependency-audit.md`
- `docs/evidence/S01/toolchain-installation.md`
- `docs/evidence/S01/sprint-report.md`

The PM workbook was not modified.

## Exact checks

| Command/check | Result |
|---|---|
| `git status --short --branch` before initialization | FAIL as diagnostic: not a Git repository |
| `git init -b main` | PASS |
| `python3 scripts/check_repository.py` | PASS |
| `.venv/bin/ruff check backend/src backend/tests scripts tests` | PASS |
| `.venv/bin/ruff format --check backend/src backend/tests scripts tests` | PASS, 7 files |
| `.venv/bin/pytest -c backend/pyproject.toml backend/tests` | PASS, 11/11 |
| clean lockfile install + clean-venv backend tests | PASS, 11/11 |
| `.venv/bin/pip check` | PASS |
| live Uvicorn `GET /healthz` / unauthorized `POST /tools/execute` | PASS, HTTP 200 / expected HTTP 404 |
| `python3 -m unittest discover -s tests -v` final run | PASS, 8/8 |
| JSON parse of `config/toolchain-versions.json` | PASS |
| YAML parse of `.github/workflows/ci.yml` | PASS |
| `xcodebuild -version` | PASS — Xcode 27.1, build 27A9275 |
| Xcode `codesign` / Gatekeeper assessment | PASS / PASS — Mac App Store source |
| Xcode iOS SDK inventory | PASS — iOS 27.1 |
| `xcodebuild -checkFirstLaunchStatus` | PASS / exit 0 on final check; initial post-license check exited 69 |
| `xcrun devicectl --version` | PASS — 651.13.5 |
| Sanitized `xcdevice list` | PASS — one iPhone 11, iOS 27.2 (24B5089g), USB, available=true, no error; identifiers not retained |
| Sanitized `devicectl list devices` | PASS / exit 0; iPhone present, no error marker, no identifiers retained |
| Sanitized `devicectl device info details` | PASS / exit 0 — paired/trusted, Developer Mode enabled |
| `system_profiler SPUSBDataType` sanitized presence | NOT FOUND — retained discrepancy; Xcode device services independently pass |
| Code-signing identities | BLOCKED — inventory command PASS, 0 valid identities |
| Post-Apple-Account identity check | BLOCKED — user reports account added; rerun still shows 0 identities and no Apple Development certificate |
| Post-certificate identity check | BLOCKED — one time-valid Apple Development certificate exists, but valid signing identities remain 0 |
| Certificate/private-key pairing in My Certificates | PASS — Computer Use expanded the paired private key; no secret material read/exported |
| Apple WWDR trust-chain diagnosis | FAIL then fixed — only expired legacy WWDR was present; leaf requires G3 |
| Official Apple WWDR G3 import | PASS after explicit confirmation — System Keychain, expires 2030-02-20, verified SHA-256 |
| Final code-signing identities | PASS — exactly 1 valid Apple Development identity; no private-key/account details retained |
| Xcode GUI required shared-components dialog | PASS — user confirmed and Install clicked; Xcode advanced to Components |
| Xcode iOS Components | PASS — user-confirmed install completed; iOS 27.1 shown at 15.94 GB; runtime build 24A94425 registered |
| First 30-minute runtime monitor | TIMEOUT / exit 124 at 99.8% of item 1; Xcode continued normally and the follow-up monitor confirmed both items completed |
| Xcode external-agent access | PASS — limited to `While Xcode is Open`; broader `Always` default not retained |
| Optional watchOS/tvOS/visionOS/AI/Metal components | NOT INSTALLED — deliberately outside project scope |
| Unity Hub `3.22.5` DMG SHA-256, Apple signature/notarization | PASS |
| Unity Editor `6000.3.27f1` arm64 + iOS module install | PASS |
| Initial Unity project creation | FAIL/BLOCKED — license exit `198`; incomplete output preserved outside repository |
| Unity Personal activation and no-cloud project retry | PASS |
| Cardboard package resolution | PASS — `1.36.0`, hash `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68` |
| First Unity batch compile | FAIL — test `PackageInfo` namespace ambiguity |
| Final Unity batch compile | PASS, exit 0 |
| Unity EditMode tests | PASS, 3/3 |
| Sanitized physical iPhone readiness | PASS for discovery/pairing only; build/run NOT RUN |
| Unity batch compile | PASS |
| Unity iOS/Xcode project generation | NOT RUN |
| FastAPI import/server/tests | PASS locally; hosted CI NOT RUN |
| HelloCardboard physical device run | NOT RUN; S02 scope and prerequisites unavailable |
| G00/G01 | NOT RUN / NOT PASSED |

## Initial failures retained

The first unit-test run failed one content assertion due to Markdown bold markers. The next run correctly identified a secret-like dummy literal embedded in the negative test source. During the 2026-10-30 continuation, one policy assertion also rejected the new explicit `installation_in_progress` state; it was replaced with a bounded allowlist of unverified lifecycle states plus an explicit prohibition on `device_verified`. A combined metadata shell command later used zsh's read-only variable name `status` and exited 1 after its Xcode check; the corrected rerun used `xcode_check_rc` and passed with value `0`. After adding the final Xcode runtime assertions, `ruff format --check` failed one line-layout check; `ruff format` changed that file and the final formatting check passed for all seven files. One monitoring-tool call also had a JSON syntax typo and was immediately retried without affecting Xcode. No underlying safety validation was removed. The final run passes all eight policy tests.

## Next-sprint boundaries

1. S01 has no remaining implementation blocker; review the evidence before authorizing S02.
2. S02 must generate the Unity iOS project, configure Personal Team/bundle ID, provision, build and run HelloCardboard on the connected device.
3. Viewer model/QR/profile and comfort measurements belong to S03; do not claim them from S01 inventory.

No S02 task has been started. G00/G01 remain pending later physical evidence.
