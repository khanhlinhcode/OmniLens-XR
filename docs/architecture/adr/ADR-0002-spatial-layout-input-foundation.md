# ADR-0002: Spatial layout and typed input foundation

- **Status:** Accepted for S04 software-first implementation
- **Date:** 2026-10-30
- **Scope:** M02 / T-S04-01 through T-S04-04

## Context

S04 needs deterministic spatial layouts, gaze focus and input parity across the Unity
Editor simulator, iPhone Cardboard runtime and 2D fallback. S03 already provides a
fail-closed dwell state, allowlisted targets, cooldown, gaze-exit re-arm and lifecycle
cancellation. The physical viewer and G00/G01 evidence are still unavailable.

## Decision

1. Reuse `GazeDwellStateMachine` from S03 inside the S04 focus contract. Do not create a
   second timer or duplicate-selection policy.
2. Represent layouts as versioned pure data (`SpatialLayoutDefinition` and
   `SpatialPanelSpec`) validated before scene mutation.
3. Use a fixed enum of typed input events and sources. Each event requires a positive
   monotonic sequence and finite timestamp; head-pose events additionally require a
   finite non-zero quaternion.
4. Route events directly through at most 16 explicitly registered sinks. Do not expose
   arbitrary command strings, reflection or an unbounded queue.
5. Keep mono and stereo fixture IDs/state semantics identical. Mono is the operational
   fallback while viewer optics/comfort remain unverified.
6. Make reduced motion the default and retain a static, contrast-checked focus cue.
7. Treat all distance/text/contrast tokens as provisional software candidates until
   measured through the selected viewer.
8. Place panels on a radial arc, store viewer-facing yaw explicitly, require exactly one
   primary panel and copy caller-provided panel collections defensively.
9. Accept head rotations only when finite and near normalized. Dispatch input to a
   bounded sink snapshot so one throwing or self-removing sink cannot interrupt others.
10. Generate persistent high-contrast fixture materials and a static primary focus
    outline; scene structure is test evidence, not through-lens evidence.

## Consequences

- Automated layout, invalid-input, 30-target and lifecycle tests can run without a
  viewer.
- Physical readability, mis-selection, trigger compatibility and comfort remain
  `NOT RUN`; S04 cannot close G00/G01 or G02.
- S05 can consume the versioned layout/input contracts only after S04 evidence is
  reconciled.
- No new package or production dependency is introduced.

## Rejected alternatives

- **Copy S03 dwell into S04:** rejected because two timers would drift and create
  duplicate-action risk.
- **Unity `SendMessage`/string commands:** rejected because they bypass typed allowlists
  and verification.
- **Assume a controller exists:** rejected because the current viewer/trigger is unknown.
- **Declare text sizes readable from Editor screenshots:** rejected because lens optics
  and actual viewer profile are unmeasured.

