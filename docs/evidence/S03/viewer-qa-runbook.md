# S03 physical viewer QA runbook

**Scope:** T-S03-01 through T-S03-04 physical completion only.  
**Current state:** READY FOR A FUTURE VIEWER / NOT RUN.  
**Safety:** Perform the test seated and stationary. Stop immediately for nausea,
dizziness, eye strain, headache, excessive heat or unstable rendering.

## 1. Prepare evidence safely

1. Copy `config/s03-viewer-evidence.template.json` to a temporary working file outside
   source control. Do not store account email, serial, UDID, Team ID, personal media or
   precise location.
2. Record the viewer brand/model, tray dimensions, iPhone 11 fit, camera opening,
   trigger type, lens controls and manufacturer QR/profile source.
3. Keep photos private unless they contain no identifiers or personal screen content.
4. The template starts as `blocked`; never change `gate.claimed_result` to `pass` before
   completing every required observation.

## 2. Dwell and recenter matrix

With the phone mounted and the correct viewer profile loaded:

| Check | Required observation |
|---|---|
| `selection` | One uninterrupted 1.5-second gaze fires one allowlisted action |
| `cancellation` | Leaving before threshold fires zero actions |
| `duplicate_suppression` | Continued gaze after selection does not repeat |
| `recenter` | Gaze at `RECENTER`; forward direction resets without a trigger |
| `progress_readability` | Progress is visible and understandable through the lenses |
| `reduced_motion_review` | Default indicator does not introduce unnecessary motion |

Record `pass` or `fail`; do not normalize a failure to pass because automated tests pass.

## 3. Continuous comfort protocol

- Use the same build, viewer profile and lens setting for the entire run.
- Record checkpoints at exactly 0, 5, 10 and 20 minutes.
- For each checkpoint, copy measured p95/max frame time and native thermal state from
  the sanitized JSONL. Record manual readability, glare, distortion and symptoms.
- Exact Celsius is optional. It is allowed only when measured by an external surface
  thermometer and `temperature_method` is `external_surface_thermometer`; iOS thermal
  state is not an exact temperature.
- Any background/focus interruption means the comfort run is not continuous.
- A safe early stop remains valid evidence but cannot produce a comfort PASS.

## 4. Collect and validate

```bash
zsh scripts/collect_s03_comfort_log.sh > /tmp/omnilens-s03-comfort.jsonl
python3 scripts/analyze_s03_comfort_log.py /tmp/omnilens-s03-comfort.jsonl
python3 scripts/validate_s03_viewer_evidence.py /tmp/s03-viewer-evidence.json
```

For the current no-viewer template, the expected non-gate result is:

```bash
python3 scripts/validate_s03_viewer_evidence.py \
  --allow-incomplete config/s03-viewer-evidence.template.json
```

The validator rejects a Gate PASS claim unless viewer inventory, dwell/accessibility,
the continuous 20-minute protocol and 2D fallback are all complete. A validator PASS is
necessary evidence but does not replace human review of the measurements.

