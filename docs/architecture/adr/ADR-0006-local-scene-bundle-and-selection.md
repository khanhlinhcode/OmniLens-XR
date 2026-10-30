# ADR-0006: Validated local scene bundles and typed scene selection

- **Status:** Accepted for S08
- **Date:** 2026-10-30
- **Tasks:** T-S08-01 through T-S08-04

## Context

S08 needs a verifiable 3D asset foundation without assuming third-party models, adding a
runtime package dependency or loading unknown content. It also needs stable semantic part
IDs and measurable cleanup before later animation and domain packs.

## Decision

1. Use a versioned scene manifest containing stable model/part IDs, one rooted acyclic
   hierarchy, target names, animation-to-part references, LOD budgets, units, SHA-256,
   byte/mesh/texture budgets and explicit license/provenance.
2. Use a deliberately small JSON mesh bundle for the first loader. This is a local bundle
   format, not Unity `AssetBundle`; it keeps parsing and budgets inspectable and avoids a
   new glTF/import dependency. Later formats must conform to the same validated manifest
   and disposable-lease boundary.
3. Ship only a self-authored generated cube in S08. Its exact bytes and SHA-256 are checked
   in tests and its provenance uses `LicenseRef-OmniLens-Generated`.
4. Resolve only a safe filename beneath the fixed StreamingAssets S08 root. Validate size,
   timeout, SHA-256, asset ID, finite vertices, triangle indices and budgets before mesh
   construction. Any failure returns a typed outcome and procedural tetrahedron fallback.
5. Represent every loaded scene as an idempotent disposable lease. Track created,
   disposed, active and peak-active counts. The 50-cycle baseline requires active=0,
   created=disposed and peak=1 after each sequential lifecycle. After unload and managed
   collection, record process-wide Unity allocated memory and apply a conservative 32 MiB
   observed-growth bound; report the raw before/after/delta values rather than treating it
   as a general production memory guarantee.
6. Route select/isolate/highlight/focus/clear through an enum command with an exact expected
   scene version and allowlisted part ID. Unknown, stale, invalid and no-op commands reject
   without state drift. No reflection or arbitrary method name exists in the contract.

## Consequences

- S08 can prove provenance, integrity, hierarchy and cleanup on Editor and iPhone without
  downloading an asset or installing a package.
- The baseline contains no texture (`texture_bytes=0`) and is intentionally not evidence
  for production art quality, viewer readability or complex-model performance.
- Animation state/ACK, Unity panel integration and the G03 asset gate remain S09 work.
