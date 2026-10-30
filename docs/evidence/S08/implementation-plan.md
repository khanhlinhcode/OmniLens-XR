# Sprint S08 implementation plan — Scene manifest, local mesh bundle and baseline

**Sprint:** S08  
**Module:** M08 3D Asset/Scene Platform  
**Tasks:** T-S08-01 through T-S08-04  
**Plan date:** 2026-10-30  
**Authorization:** User requested the next task after S07 and previously authorized
sequential software-first sprint execution.  
**Execution state:** APPROVED / SOFTWARE-FIRST DEPENDENCY EXCEPTION

## Dependency reconciliation

The workbook was inspected read-only and remains unchanged. `Task_WBS!A32:U35` records
all four S08 rows as `Not Started`. T-S08-01 depends on T-S07-04; S07 automated and phone
restore evidence passes, but G02 remains `NOT RUN` because the required viewer video and
human interaction do not exist. This exception permits S08 implementation without
promoting the physical dependency.

| Order | Task | Deliverable | Required proof |
|---:|---|---|---|
| 1 | T-S08-01 | Versioned manifest with model/part hierarchy, animation target names, LOD, units, budgets and license/provenance | Missing targets, cycles, broken parents and invalid budgets rejected |
| 2 | T-S08-02 | Local deterministic JSON mesh bundle loader with SHA-256, bounded file/mesh sizes, timeout, procedural placeholder and disposable lease | Missing provenance/hash mismatch/timeout/malformed bundle fail to usable fallback |
| 3 | T-S08-03 | Versioned typed select/isolate/highlight/focus/clear reducer and view adapter | 20 valid commands hit exact IDs; unknown/stale/invalid commands reject without drift |
| 4 | T-S08-04 | One low-poly generated model and 50-cycle lifecycle benchmark | Per-asset bytes/vertices/triangles/texture metrics; active leases return to zero and peak remains bounded |

## Architecture and safety

- No external asset is downloaded. The baseline mesh bundle is authored in-repository,
  has a deterministic SHA-256 and uses `LicenseRef-OmniLens-Generated` with a local
  `generated://` provenance URI. No third-party content or new dependency is introduced.
- Manifest and bundle parsing are explicit versioned DTOs with finite numeric checks,
  ID allowlists, bounded arrays and hierarchy cycle detection. Unknown schema versions
  fail closed.
- File loading accepts only one safe filename under `Application.streamingAssetsPath`.
  Traversal, absolute names, oversized files, wrong hashes and malformed mesh indexes
  return a typed failure plus a usable procedural placeholder.
- Scene commands are typed enums with expected scene version. They cannot invoke
  reflection, arbitrary C# methods, model text or filesystem/network operations.
- Every loaded object is represented by a disposable lease. The tracker records created,
  disposed, active and peak-active counts; tests do not infer leak safety solely from a
  noisy process-wide memory number. The device fixture also records before/after/delta
  after unload+GC and enforces a conservative 32 MiB observed-growth bound.
- S08 has no physical gate. iPhone development metrics are measurements only and are not
  promoted to viewer readability, comfort or production FPS claims.

## Planned files

| Path | Purpose |
|---|---|
| `docs/architecture/adr/ADR-0006-local-scene-bundle-and-selection.md` | Manifest/loader/selection/lifecycle decision |
| `Assets/StreamingAssets/S08/generated-cube.mesh.json` | Self-authored low-poly mesh bundle |
| `Assets/StreamingAssets/S08/scene-manifest-v1.json` | Validated manifest and exact bundle SHA-256 |
| `Assets/OmniLens/S08/SceneManifest.cs` | DTO, codec and hierarchy/provenance/budget validator |
| `Assets/OmniLens/S08/LocalSceneAssetLoader.cs` | Safe source, hash/timeout/mesh checks, fallback and disposable lease |
| `Assets/OmniLens/S08/SceneSelection.cs` | Typed versioned scene selection reducer/view adapter |
| `Assets/OmniLens/S08/S08SceneFixture.cs` | Runtime 50-cycle and command markers |
| `Assets/Editor/S08SceneBuild.cs` | Reproducible scene/preview/iOS generation |
| `Assets/Tests/Editor/S08ScenePlatformTests.cs` | Manifest, invalid-input, hash, timeout and 20-command tests |
| `Assets/Tests/PlayMode/S08ScenePlatformPlayModeTests.cs` | Mesh/fallback/view/lifecycle integration |
| `scripts/run_unity_s08_checks.sh` | Preview, full suites and iOS generation |
| `docs/evidence/S08/EV-S08-01.md` through `EV-S08-04.md` | Actual outcomes |
| `docs/evidence/S08/sprint-report.md` | Reconciled state |

## Verification and stop conditions

- Run repository, Python, Ruff, complete Unity suites, S08-only counts, unsigned Xcode
  and signed iPhone runtime when the connected device remains available.
- Preserve actual failures and measured metrics; never claim viewer/production FPS.
- Do not edit the workbook, download assets, install dependencies, start S09 before S08
  reconciliation, commit/push/deploy or persist Team ID/device identifiers in the repo.

## Post-implementation reconciliation

- Final Unity result is 112/112 EditMode and 20/20 PlayMode; S08 contributes 8/8 and
  4/4 respectively.
- Final sequential unsigned/signed builds, install, device runtime and five-second process
  check pass. A parallel build attempt corrupted shared generated IL2CPP cache and is
  retained as a harness failure; moving the cache aside and sequential retries passed.
- The signed phone completed 50 load/dispose cycles with active=0, peak=1, no fallback,
  exact 444-byte/8-vertex/12-triangle/0-texture metrics and memory delta `-143,880` bytes
  after unload+GC against a 33,554,432-byte observation bound.
- No external asset was downloaded. G02 remains upstream `NOT RUN`; G03 and G09 were not
  evaluated in S08.
