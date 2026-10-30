# S01 implementation plan

**Authorized scope:** S01 / M01 / T-S01-01 through T-S01-04 only  
**Execution date:** 2026-10-29; continued 2026-10-30  
**Approval basis:** The user reviewed the repository audit and explicitly requested implementation.

## Sequence

1. `T-S01-01`: record a sanitized Mac/device inventory and classify unknown build prerequisites.
2. `T-S01-02`: pin a provisional Unity/Cardboard/Xcode stack from official sources, with a rollback candidate and explicit unverified items.
3. `T-S01-03`: initialize Git and a minimal repository foundation. Create only checks and documentation that can be verified with installed tools. Do not fabricate Unity or FastAPI output.
4. `T-S01-04`: create the initial cost/license register and prohibit unapproved paid services, model weights, and restricted content ingestion.

## Planned changes

- `docs/device-matrix/environment-inventory.md`
- `docs/architecture/adr/ADR-0001-xr-foundation-toolchain.md`
- `config/toolchain-versions.json`
- `client/README.md`
- `backend/README.md`
- `contracts/README.md`
- `.env.example`
- `.github/workflows/ci.yml`
- `scripts/check_repository.py`
- `tests/test_repository_policy.py`
- `docs/licenses/initial-license-register.md`
- `docs/evidence/S01/EV-S01-01.md` through `EV-S01-04.md`
- `docs/evidence/S01/sprint-report.md`

The PM workbook will not be edited without a separate evidence-backed review. No model, asset, paid service, Git remote, commit, push, or deployment is authorized by this plan.

## Authorization amendment — 2026-10-29

The user subsequently granted permission to install the required SDKs and dependencies. Python dependencies may now be installed only inside the repository `.venv` and must be pinned, licensed, scanned, and tested. Unity Hub/Editor and Xcode may be installed only from official distribution channels. Paid services, billing, credentials, automatic commit/push, and bypassing sprint gates remain prohibited.

## Verification

- `python3 scripts/check_repository.py`
- `python3 -m unittest discover -s tests -v`
- `python3 -m json.tool config/toolchain-versions.json`
- `git status --short --branch`
- targeted secret-pattern scan over tracked project files
- manual consistency review of the ADR, manifest, license register, and task evidence

## Risk controls

- Redact hostnames, serial numbers, UDIDs, Apple IDs, signing identities, and credentials.
- Treat Unity, Xcode, iPhone iOS/signing, viewer geometry, and device performance as unknown until verified.
- Keep Cardboard/iPhone 11 compatibility provisional because the current plugin release does not list iPhone 11 among its tested devices.
- Keep OpenStax A&P 2e out of RAG/AI ingestion without prior written permission.
- Stop at S01. S02 device signing and HelloCardboard execution require a separate approved gate.

## Proof required for full completion

- Full Xcode selected and `xcodebuild -version` succeeds.
- Unity Editor plus iOS Build Support installed at the pinned version.
- Physical iPhone 11 discoverable, with iOS version and signing readiness verified without recording private identifiers.
- A real Unity-generated project and local checks succeed.
- FastAPI dependencies are explicitly approved, installed in an isolated environment, and the service tests pass.

