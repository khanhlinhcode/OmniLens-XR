# S04 spatial layout specification

**Verification state:** SOFTWARE CANDIDATE / VIEWER READABILITY NOT RUN  
**Schema:** `SpatialUiTokens.SchemaVersion = 1`

## Provisional tokens

| Token | Candidate value | Boundary |
|---|---:|---|
| Panel distance | 1.0–4.0 m; default 2.25 m | Reject outside range |
| Panel width | 0.35–2.4 m | Reject outside range |
| Panel height | 0.25–1.6 m | Reject outside range |
| Panel gap | at least 0.12 m | Reject overlap/undersized gap |
| Horizontal safe zone | 80° total | ±40° from forward |
| Vertical safe zone | 55° total | ±27.5° from forward |
| Panel facing error | at most 10° | Reject panels not facing the viewer |
| Text tiers | 0.060 / 0.040 / 0.028 m | Provisional until lens test |
| Focus contrast | at least 4.5:1 candidate | Static cue remains required |
| Reduced motion | enabled by default | Focus scale delta must be zero |

Panel centers lie on a radial arc and each panel yaw faces the viewer origin. A layout
must contain exactly one primary panel. These values are implementation candidates, not
claims of comfort or readability.

## Wireframe 1 — Home / Launcher

```text
forward safe zone
┌─────────────────────────────────────────────┐
│                                             │
│         ┌─────────────────────────┐         │
│         │ HOME / LOCAL LAUNCHER   │         │
│         │ Chat  3D  Docs  Notes   │         │
│         │ network: offline-safe   │         │
│         └─────────────────────────┘         │
│                                             │
└─────────────────────────────────────────────┘
```

One primary panel. Mono and stereo candidates use the same `home-launcher` ID and
actions; only candidate distance differs.

## Wireframe 2 — Three-panel Workspace

```text
┌──────────────┐  ┌────────────────┐  ┌──────────────┐
│ LEFT TOOL    │  │ PRIMARY PANEL  │  │ RIGHT TOOL   │
│ context      │  │ focused work   │  │ activity     │
│ local only   │  │ static focus   │  │ status       │
└──────────────┘  └────────────────┘  └──────────────┘
```

Panels have unique IDs, at least 0.12 m horizontal separation and exactly one primary.
Side panels sit forward on the same radial arc and rotate inward toward the viewer.
No arbitrary iOS application embedding is implied; these are Unity-owned panels.

## Wireframe 3 — Settings / Accessibility

```text
┌──────────────────┐   ┌──────────────────┐
│ SETTINGS         │   │ ACCESSIBILITY    │
│ mono / stereo    │   │ reduced motion ✓ │
│ recenter         │   │ static focus   ✓ │
│ reset workspace  │   │ text tier: Body  │
└──────────────────┘   └──────────────────┘
```

The accessibility panel never hides the static focus cue. Long-form text entry is not a
headset requirement and must hand off to a normal 2D surface.

The generated fixtures use a persistent dark panel material, high-contrast white/cyan
labels at the provisional Body tier and a non-animated cyan outline on the primary panel.
These scene checks prove deterministic contrast policy, not optical readability.

## Input parity

Simulator and Cardboard sources emit the same typed event kinds. A missing trigger or
controller yields `SourceDisabled`/no event and leaves gaze dwell available; it never
synthesizes a press. Back, Home and Recenter are distinct allowlisted events.

## Physical review still required

- Smallest readable text tier through actual lenses.
- Glare/distortion at safe-zone edges.
- False selection rate across 30 targets.
- Static focus visibility and reduced-motion comfort.
- Viewer trigger/controller mapping if present.

Until those checks exist, EV-S04-01 and EV-S04-04 cannot be fully PASS.

