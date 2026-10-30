# S09 implementation plan

## Authorized scope

- Sprint: `S09` only.
- Tasks: `T-S09-01` through `T-S09-04`.
- Module: `M08 — 3D Asset/Scene Platform`.
- Dependency: the checked-in S08 local asset/manifest/selection baseline and its
  recorded software/device evidence.
- Gate under test: `G03`. Physical through-lens interaction remains `NOT RUN`
  while no viewer is available.

## Planned implementation

1. Add a typed animation catalog and controller for allowlisted manifest animation
   names. Enforce finite durations, bounded seek, bounded playback speed, expected
   scene version and explicit idle/playing/paused state transitions.
2. Add a bounded render command queue with validated `command_id`, expected scene
   version, terminal `completed`/`failed` ACKs and replay of the cached ACK for a
   duplicate ID. A duplicate must never execute its operation twice.
3. Add a model-viewer panel controller that composes the S05 `Science3D` panel and
   S08 selection controller. Support bounded zoom/rotation, annotation toggle,
   recenter and exact part selection. Panel-hit commands consume the input so they
   cannot click through to the scene.
4. Add an S09 runtime fixture and iOS build scene. Exercise 20 exact fixture part
   selections, animation commands, duplicate ACK protection, panel actions, asset
   failure taxonomy and 50 load/dispose cycles. Publish sanitized state markers and
   observed FPS without claiming an unverified performance threshold.
5. Add EditMode and PlayMode coverage for positive, invalid-input, stale-version,
   duplicate, missing/malformed/hash-mismatch and lifecycle paths.
6. Update repository policy/config/README and write task evidence only after real
   commands complete.

## Files planned

- `client/OmniLensXR/Assets/OmniLens/S09/AnimationControllerV1.cs`
- `client/OmniLensXR/Assets/OmniLens/S09/RenderEventQueue.cs`
- `client/OmniLensXR/Assets/OmniLens/S09/ModelViewerPanel.cs`
- `client/OmniLensXR/Assets/OmniLens/S09/S09SceneFixture.cs`
- `client/OmniLensXR/Assets/Editor/S09SceneBuild.cs`
- `client/OmniLensXR/Assets/Tests/Editor/S09ScenePlatformTests.cs`
- `client/OmniLensXR/Assets/Tests/PlayMode/S09ScenePlatformPlayModeTests.cs`
- `scripts/run_unity_s09_checks.sh`
- `docs/architecture/adr/ADR-0007-render-ack-and-model-viewer.md`
- `docs/evidence/S09/EV-S09-01.md` through `EV-S09-04.md`
- `docs/evidence/S09/sprint-report.md`
- existing repository policy/config/readmes, limited to S09 declarations.

## Required proof

- Unity compile, EditMode and PlayMode actual results.
- S09-only test counts extracted from NUnit XML.
- iOS project generation; unsigned Xcode build; signed device install/launch when
  the connected iPhone and signing identity remain available.
- Device markers for animation, ACK, 20 selections, failure taxonomy, lifecycle,
  memory and observed FPS.
- Repository/backend tests, formatter/linter checks and `git diff --check`.
- Read-only proof that `docs/planning/master-plan-v2.xlsx` is unchanged.

## Risk controls

- No new dependency, external asset, paid service, credential, arbitrary code
  execution, commit, push, deploy or workbook mutation.
- Queue/history and identifiers are bounded; commands are typed and allowlisted.
- Generated fixture remains the S08 self-authored asset with recorded provenance.
- Run Xcode builds sequentially because concurrent builds share generated IL2CPP
  state and previously caused a real harness failure.
- Report physical viewer input/readability/comfort as `NOT RUN` until hardware is
  available.
