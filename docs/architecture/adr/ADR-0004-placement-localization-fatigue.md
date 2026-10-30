# ADR-0004: Bounded placement, typed manipulation and non-claiming fatigue metrics

- **Status:** Accepted for S06 software-first implementation
- **Date:** 2026-10-30
- **Scope:** M02 / T-S06-01 through T-S06-04

## Context

S06 extends S05 panel lifecycle state with placement and manipulation. It must keep full
panel rectangles in a provisional field of view, avoid overlap, support Vietnamese and
English UI, and prepare a 10-minute fatigue protocol without claiming physical comfort
while no viewer exists.

## Decision

1. Placement uses a 5 cm metric grid. Size and distance are clamped to S04 tokens, while
   the full panel rectangle—not only its center—must fit inside the angular safe zone.
2. Collision resolution searches a deterministic square perimeter for at most 32 rings.
   Exhaustion is an explicit failure; no random placement or unbounded loop is allowed.
3. S05 gains typed `Move` and `Resize` commands. They retain exact-version checks,
   reject non-finite/out-of-bounds/overlapping/no-op changes and preserve focus/pin state.
4. Gaze manipulation exposes only nine enum handles. Translation/resize increments are
   fixed and handle hit size is at least 12 cm. Long free-text entry is not an action.
5. Localization uses enum keys and local dictionaries. English is complete and required;
   Vietnamese is complete for the scoped menu, with deterministic English fallback
   available for future missing values. Date/number formatting uses `en-US` or `vi-VN`.
6. Fatigue instrumentation retains counts and elapsed time only, caps recorded events at
   600 and rejects non-monotonic time. A synthetic 600-second run can validate tooling
   but always reports physical viewer status `NOT_RUN`.
7. The iPhone fixture emits bounded state/metrics markers. Device identifiers, signing
   data, precise pose and user text are not recorded.

## Consequences

- Twenty deterministic layout fixtures and edge/failure paths can run without a viewer.
- Move/resize/pin scene views stay synchronized with the reducer and can be device-smoke
  tested.
- Localization drift and fallback behavior are unit-testable without network services.
- Physical field of view, misclicks, head-turn burden and 10-minute fatigue remain open
  until a real viewer and human protocol are available.

## Rejected alternatives

- **Free drag coordinates:** rejected because non-snapped values create drift and make
  layout replay nondeterministic.
- **Random collision search:** rejected because identical commands could produce
  different workspaces.
- **Direct transform mutation:** rejected because it bypasses S05 version/state checks.
- **System locale strings as keys:** rejected because missing translations become silent
  runtime defects.
- **Synthetic clock equals comfort PASS:** rejected because it does not measure a human
  through lenses.
