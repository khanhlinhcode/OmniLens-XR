# Sprint S08 report — Scene manifest, loader, selection and baseline

**Sprint:** S08  
**Tasks:** T-S08-01 through T-S08-04  
**Report date:** 2026-10-30  
**Sprint state:** DONE SOFTWARE/PHONE BASELINE / UPSTREAM G02 STILL NOT RUN

## Reconciled task state

| Task | Implementation | Actual QA state | Evidence |
|---|---|---|---|
| T-S08-01 | Manifest v1, hierarchy/target/LOD/unit/budget/license validation | PASS | `EV-S08-01.md` |
| T-S08-02 | Safe local source, SHA-256, timeout, budgets, fallback, disposable lease | PASS | `EV-S08-02.md` |
| T-S08-03 | Versioned typed select/isolate/highlight/focus/clear and view sync | PASS | `EV-S08-03.md` |
| T-S08-04 | Self-authored low-poly asset and 50-cycle Editor/PlayMode/iPhone baseline | PASS for scoped baseline | `EV-S08-04.md` |

## Final actual results

- Unity: 112/112 EditMode and 20/20 PlayMode; S08 contributes 8/8 and 4/4.
- Baseline asset: 444 bytes, 8 vertices, 12 triangles, 0 texture bytes, exact SHA-256,
  self-authored provenance and no external download.
- iPhone 11: 50/50 sequential load/dispose, active=0, peak=1, success/no fallback.
- Unity allocated memory after unload+GC: 77,385,411 → 77,241,531 bytes,
  delta `-143,880` against 33,554,432-byte observation bound.
- Final sequential unsigned and signed Xcode builds, install, runtime markers and
  five-second process check: PASS.
- Repository/tooling, backend, Ruff, diff and workbook checks are recorded after final
  reconciliation.

## Failures retained

- Running final unsigned and signed Xcode builds concurrently corrupted their shared
  generated IL2CPP command log. Both builds failed. The generated cache was moved to a
  temporary recovery directory and sequential retries passed. Source code was unchanged.

## Limitations

- Upstream T-S07-04/G02 remains physically incomplete; S08 used the approved exception.
- Baseline format is local JSON mesh, not glTF/Unity AssetBundle support.
- No complex asset, texture, animation, thermal, viewer or production FPS result exists.
- G03 is S09 and G09 is S35; neither is evaluated here.
- Workbook, Git history/remotes and global signing settings were not modified. No commit,
  push, deployment, dependency installation or external asset download occurred.

## Next unblockable task

S09: bounded animation controller, render-event ACK/idempotency, panel+model integration
and the G03 asset gate. Do not start it before this S08 evidence is reconciled.
