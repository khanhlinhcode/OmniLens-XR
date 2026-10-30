# Sprint S04 report — Spatial design and input foundations

**Sprint:** S04  
**Tasks:** T-S04-01 through T-S04-04  
**Report date:** 2026-10-30  
**Sprint state:** PARTIAL — SOFTWARE PASS / PHYSICAL VIEWER ACCEPTANCE NOT RUN

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S04-01 | Versioned radial tokens, curved viewer-facing layout, immutable validation, mono/stereo parity and three high-contrast Unity fixtures | PARTIAL; automated/source/build PASS, through-lens readability NOT RUN | `EV-S04-01.md` |
| T-S04-02 | S03-composed dwell focus, 30-target state and physics fixtures, cancellation/competition checks | PARTIAL; all automated cases PASS, human viewer interaction NOT RUN | `EV-S04-02.md` |
| T-S04-03 | Typed bounded input events/router, simulator/Cardboard sources, normalized pose and failure-isolated dispatch | PARTIAL; software/build PASS, actual viewer trigger/controller NOT RUN | `EV-S04-03.md` |
| T-S04-04 | Scene-applied contrast/static-focus/reduced-motion policy and physical checklist | PARTIAL; policy/fixture tests PASS, lens/focus/comfort checklist NOT RUN | `EV-S04-04.md` |

## Actual automated results

| Exact command | Actual result |
|---|---|
| First `zsh scripts/run_unity_s04_checks.sh` | FAIL at compile on unsupported `Quaternion.sqrMagnitude`; source fixed without weakening tests |
| Final hardening `zsh scripts/run_unity_s04_checks.sh` | PASS: configure, 82/82 EditMode, 6/6 PlayMode, iOS project generation |
| S04-only NUnit cases | PASS: 32/32 EditMode and 3/3 PlayMode |
| First preview command with `-nographics` | FAIL, exit 139 in native line rendering; runner fixed to use graphics for preview only |
| First visual preview review | FAIL: mirrored/overlapping text and invisible panel faces; implementation fixed and regression bounds added |
| Final three 1600×900 software previews | PASS visual QA; no through-lens claim |
| Hardening `xcodebuild ... CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO build` | PASS: exit code 0, `** BUILD SUCCEEDED **` |
| Signed build attempt with misparsed destination | FAIL: wrong table column; parser corrected |
| Signed build attempt without explicit local team | FAIL: `DEVELOPMENT_TEAM` missing; no repository signing change |
| Final signed build/install/launch/process check | PASS on connected iPhone 11; process alive after 5 seconds; raw identifiers/logs remain under `/tmp` |
| `python3 scripts/check_repository.py` | PASS: S01-S04 required artifacts, bounded manifest state and secret/signing policy |
| `python3 -m unittest discover -s tests -q` | PASS, 25/25 |
| `.venv/bin/ruff format --check backend/src backend/tests scripts tests` | PASS, 12 files already formatted |
| `.venv/bin/ruff check backend/src backend/tests scripts tests` | PASS |
| `.venv/bin/pytest -q backend/tests` | PASS, 11/11 |
| `git diff --check` | PASS after final Unity-generated whitespace normalization |

## Files and architecture

- Runtime foundation is under `Assets/OmniLens/S04/`; it adds no package or production
  dependency, preserves S03 dwell as the single timer primitive, and hardens layout/input
  failure paths documented in `hardening-plan.md`.
- Three deterministic scenes under `Assets/OmniLens/S04/Fixtures/` are generated from the
  curved validated layouts with persistent panel/focus materials.
- ADR-0002 records the typed/bounded input and mono/VR parity decisions.
- The design spec labels text and geometry tokens provisional until physical review.
- Editor/PlayMode tests include positive, invalid-input, lifecycle, replay, missing-source,
  cancellation and reduced-motion failure paths.

## Limitations and gate decision

- No physical viewer exists; viewer model, lens settings and trigger type remain unknown.
- Through-lens readability, mis-selection, glare/distortion, focus visibility and comfort
  are `NOT RUN`.
- T-S03-04 is still blocked, so its dependency on T-S04-01 is not retroactively passed.
- G00/G01 remain blocked and G02 is `NOT RUN`; S04 software results cannot close them.
- The planning workbook was not modified. No commit, push, deployment, purchase, paid
  service, signing identity or global dependency installation was performed.

## Next unblockable task

Review this S04 software checkpoint. The next sequential implementation sprint is S05,
but it must not be started automatically from this report. When a viewer becomes
available, rerun S03/S04 physical readability, dwell, trigger and comfort checks before
promoting any PARTIAL task or gate.
