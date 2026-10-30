# S04 hardening plan

**Sprint:** S04  
**Tasks:** T-S04-01 through T-S04-04  
**Date:** 2026-10-30  
**Authorization:** User requested a best-quality fix pass after the first S04 handoff.  
**Workbook:** Read-only; S04 and EV-S04-01..04 remain `Not Started` / `Not Run` there.

## Confirmed audit findings

1. Panel definitions did not model yaw, so the documented curved-placement requirement
   was not enforced by the layout contract.
2. Layouts accepted more than one primary panel and retained the caller's mutable panel
   collection.
3. Head-pose validation accepted finite but severely non-normalized quaternions.
4. One input sink exception or a sink modifying registration during dispatch could
   interrupt the bounded router.
5. Generated fixtures used the primitive's default panel material and did not contain a
   static focus outline, so scene-level contrast and focus-cue policy were not proven.

## Approved hardening scope

- Add radial curved placement, viewer-facing yaw and orientation validation.
- Require exactly one primary panel and copy input panel collections defensively.
- Bound quaternion squared magnitude around a normalized rotation.
- Isolate sink exceptions and dispatch from a bounded snapshot.
- Generate persistent dark-panel/focus materials, stable physical text scaling and a
  static primary focus outline; verify them in Editor tests.
- Render three deterministic software previews and visually inspect orientation,
  overlap, clipping, contrast and focus-cue visibility. This is not lens evidence.
- Add positive and failure tests without weakening the 30-target PlayMode fixture.
- Re-run repository policy, Python, Ruff, Unity EditMode/PlayMode, Unity iOS generation,
  unsigned Xcode build and `git diff --check`.

## Stop conditions

- Do not alter the planning workbook, start S05, commit, push or install dependencies.
- Do not mark through-lens readability, trigger behavior, comfort or G02 as PASS.
- Preserve all unrelated S02/S03 worktree changes.
