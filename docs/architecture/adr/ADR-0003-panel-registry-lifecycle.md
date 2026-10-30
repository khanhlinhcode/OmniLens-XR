# ADR-0003: Deterministic panel registry and bounded view lifecycle

- **Status:** Accepted for S05 software-first implementation
- **Date:** 2026-10-30
- **Scope:** M02 / T-S05-01 through T-S05-04

## Context

S05 needs one source of truth for panel identity, ordering, bounds, focus, pinning,
visibility and lifecycle events. State must remain deterministic across Unity Editor and
iPhone while S04 through-viewer acceptance is still unavailable. View creation also has
to avoid duplicate objects and unbounded retained instances.

## Decision

1. `PanelRegistry` is the sole lifecycle state owner. It accepts only typed enum
   commands with an exact expected version and rejects stale or malformed commands
   without mutation.
2. Panel IDs are restricted to a 64-character alphanumeric, dash and underscore
   allowlist. Panel kinds are limited to Chat, Science3D, Document and Notes.
3. The registry is capped at eight panels and keeps at most 64 metadata-only lifecycle
   events. Events contain sorted visible IDs, never document contents.
4. Exactly one visible panel may be focused. Focus changes are tracked in a deterministic
   most-recent stack; minimizing or closing the focused panel restores the latest still
   visible candidate.
5. Snapshots are immutable copies sorted by z-order and then ordinal panel ID. Closing a
   panel resets its z-order; therefore closed panels sort deterministically before open
   panels.
6. `PanelFactory` accepts only allowlisted panel kinds, permits one active view per panel
   ID and retains at most four inactive views per kind. Excess inactive objects are
   destroyed.
7. `PanelWorkspaceController` is the integration boundary between accepted reducer
   transitions and view acquire/release operations. Internal state/view divergence is a
   detected error, not silently ignored.
8. S05 phone execution proves build/install/launch plumbing only. Readability, focus
   ergonomics and comfort through a viewer remain `NOT RUN`.

## Consequences

- Duplicate IDs/views, stale writes, unknown panels, invalid lifecycle transitions and
  unbounded event/pool growth have automated failure-path coverage.
- Editor and PlayMode tests can prove deterministic state and pooling without claiming
  optical usability.
- Later S06 commands can extend the typed reducer instead of mutating scene objects
  directly.
- The selected bounds and panel appearance remain provisional until viewer evidence is
  available.

## Rejected alternatives

- **Scene hierarchy as source of truth:** rejected because object ordering and lifecycle
  callbacks are not a deterministic domain model.
- **String/reflection commands:** rejected because they bypass the command allowlist and
  make invalid operations difficult to audit.
- **Unbounded prefab cache/event log:** rejected because repeated open/close activity
  could retain memory indefinitely.
- **Mark S05 accepted from Editor screenshots:** rejected because screenshots do not
  establish through-lens readability or comfort.
