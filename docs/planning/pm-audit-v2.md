# OmniLens XR V2 — Structural PM & Technical Lead Audit

**Audit baseline:** 21 October 2026  
**Artifact:** `OmniLens_XR_Master_Plan_V2_Implementation_Ready.xlsx`  
**Scope of approval:** Detailed *implementation plan* approved for **Sprint S01 discovery/compatibility**. This approval is **not a statement** that the VR app, AI Agent, scientific content, or device compatibility has been implemented or tested.

## 1. File and structure integrity

| Check | Verified result |
|---|---:|
| Excel sheets | 20 |
| Legacy module IDs preserved | 20/20 |
| Modules with at least one task | 20/20 |
| Two-week scheduled sprints | 68 |
| Distinct WBS task IDs | 272 |
| Technical dependency edges | 355 |
| Invalid or forward-referencing task dependencies | 0 |
| Requirements mapped to tasks/workflows | 88 |
| Individual task QA cases | 272 |
| End-to-end acceptance workflows | 20 |
| Quality gates | 22 |
| Tracked technical/project risks | 25 |
| Planned, unverified 3D/asset entries | 40 |
| Source and policy links | 28 |
| Architecture decisions | 20 |
| Cash hardware estimate (lower / upper) | 60,000 / 170,000 VND |
| Planned effort (hours) | 2,312 |
| Available scheduled capacity (hours) | 2,720 |
| Unallocated risk reserve (hours) | 408 |

All 272 tasks contain a unique work package ID, a human-authored implementation output, task-specific acceptance criterion, evidence test ID, estimate, named module, scheduled sprint and status. Each sprint has 34 estimated task hours and six reserved hours, assuming **20 focused hours per week by one developer**.

## 2. Logic / calculation smoke tests completed

- Marked `T-S01-01` **Done** temporarily: Dashboard Done changed 0→1 and completion became 1/272; the module hour-weighted completion reflected 7/102.
- Sprint `S01` correctly changed `Not Started`→`In Progress` when a task was Done; restored to `Not Started` after rollback.
- Marked gate `G00` PASS temporarily: required gate counter increased 0→1; restored to 0 afterward.
- Hardware-only estimate formula correctly returned **60,000–170,000 VND**, excluding variable services.
- Scanned formula values across workbook for `#REF!`, `#DIV/0!`, `#VALUE!`, `#NAME?`, `#N/A`, `#CIRC!`, and `#NUM!`: **0 matches** at audit time.
- Confirmed exported `.xlsx` package passes ZIP integrity test with no corrupted entries.
- **No task or gate remains marked complete** after testing; Dashboard starts at 0% and `NOT READY - EVIDENCE REQUIRED`.

These checks verify **planning workbook structure and its local formula behavior**, not Microsoft Excel/Numbers interoperability on every user device. Open the file in Excel/Numbers and perform one manual compatibility check before relying on the tracker for long-term work.

## 3. Defects corrected from V1

1. Added scheduled work for Chemistry (M11): 3 sprints, 12 tasks and G12.
2. Added Learning Progress (M14): 2 sprints, 8 tasks and G15; fixed prerequisite into Recommendation.
3. Added deferred Hardware R&D (M20): 2 future sprints, 8 research tasks, explicit non-blocking G21.
4. Replaced artificial global serial chain with validated, linked task dependencies (355 edges).
5. Separated gate mappings for Unity Mobile release, visionOS Simulator PoC and Custom Hardware.
6. Added implementation-specific acceptance test/evidence IDs for **all** tasks.
7. Added effort/capacity assumptions and automated progress tracking from task statuses.
8. Added RTM, test matrix, workflows, assets/license inventory, decision log, source register and change control.
9. Added Apple MapKit provider requirement and safe 2D navigation handoff rather than dangerous headset navigation.
10. Added strict restrictions for medical model accuracy and OpenStax AI data ingestion rights.
11. Made performance, science, cloud and device states explicitly pending verification; never pre-marked them PASS.

## 4. Release boundaries and forecast

- **Start assumption:** Monday, 26 October 2026.
- **S01–S03**: iPhone 11 Cardboard compatibility, actual lenses, input, performance and purchase gate.
- **S04–S61**: full Mobile VR workstreams: Spatial, Agent, RAG, Docs, Voice, Vision, scientific packs, Learning, Recommendation and Maps.
- **S62–S63**: optional Mac screen capture and visionOS Simulator feasibility experiments.
- **S64–S66**: full mobile integration, threat model, QA and release evidence.
- **Target end of Mobile VR release audit:** 6 May 2029, *only a capacity-based baseline; replan after real velocity is measured*.
- **S67–S68**: unfunded custom headset hardware research, target baseline 3 June 2029; not part of low-cost product or Mobile VR release.

### Critical conditions remaining open

- **Hardware:** iPhone 11 + current iOS + Unity/Cardboard HelloCardboard has **not** been built or validated on the user's device. G00/G01 determine GO/NO-GO before buying a viewer.
- **Maps:** `MKDirections` provider output, region coverage and iOS-native Unity bridge must be tested; no model-generated routes or ETA. While wearing an opaque viewer, no real-world navigation in motion.
- **Voice:** Vietnamese on-device speech availability must be measured on iPhone 11; use Mac `whisper.cpp` fallback if necessary.
- **Science:** Each molecule, engineering animation and anatomical label requires domain-specific evaluation, correct units and legitimate source asset rights. A medical reviewer must sign off before claiming medical content is validated.
- **Cloud:** Free tiers are quota- and terms-dependent; 0-VND lifetime operating cost cannot be guaranteed.
- **Optical quality:** Small print, complex multiwindow productivity and 60-FPS comfort cannot be assured before measuring through the viewer.

## 5. Lead decision

**APPROVED FOR EXECUTION OF S01 ONLY.** Sprint S02 is conditionally scheduled; full program dates, scope and effort must be re-baselined at G00/G01, after Sprint S03 and subsequently at every major domain gate. The plan is sufficiently specified to assign and begin tasks, but cannot provide an a priori guarantee of all software/features' functioning, scientific accuracy, hardware parity with Vision Pro, future API pricing or release dates.
