"""Offline S01-S08 repository policy checks with no third-party dependencies."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

REQUIRED_PATHS = (
    ".env.example",
    ".github/workflows/ci.yml",
    ".gitignore",
    "AGENTS.md",
    "backend/README.md",
    "backend/pyproject.toml",
    "backend/requirements-dev.txt",
    "backend/requirements-lock.txt",
    "backend/requirements.txt",
    "backend/src/omnilens_api/contracts.py",
    "backend/src/omnilens_api/main.py",
    "backend/src/omnilens_api/settings.py",
    "backend/tests/test_health.py",
    "client/README.md",
    "client/OmniLensXR/Assets/Tests/Editor/OmniLensXR.Tests.Editor.asmdef",
    "client/OmniLensXR/Assets/Tests/Editor/S01FoundationTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S02IosConfigurationTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S03ViewerAndDwellTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S04SpatialFoundationTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S05PanelLifecycleTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S06WorkspaceTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S07WorkspaceTests.cs",
    "client/OmniLensXR/Assets/Tests/Editor/S08ScenePlatformTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/OmniLensXR.Tests.PlayMode.asmdef",
    "client/OmniLensXR/Assets/Tests/PlayMode/S03GazeDwellPlayModeTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/S04SpatialInteractionPlayModeTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/S05PanelLifecyclePlayModeTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/S06WorkspacePlayModeTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/S07WorkspacePlayModeTests.cs",
    "client/OmniLensXR/Assets/Tests/PlayMode/S08ScenePlatformPlayModeTests.cs",
    "client/OmniLensXR/Assets/Editor/S02IosBuild.cs",
    "client/OmniLensXR/Assets/Editor/S03IosBuild.cs",
    "client/OmniLensXR/Assets/Editor/S04SpatialFixtureBuild.cs",
    "client/OmniLensXR/Assets/Editor/S05PanelLifecycleBuild.cs",
    "client/OmniLensXR/Assets/Editor/S06WorkspaceBuild.cs",
    "client/OmniLensXR/Assets/Editor/S07WorkspaceBuild.cs",
    "client/OmniLensXR/Assets/Editor/S08SceneBuild.cs",
    "client/OmniLensXR/Assets/OmniLens/S02/S02RuntimeProbe.cs",
    "client/OmniLensXR/Assets/OmniLens/S03/GazeDwellSelector.cs",
    "client/OmniLensXR/Assets/OmniLens/S03/GazeDwellRecenterTarget.cs",
    "client/OmniLensXR/Assets/OmniLens/S03/GazeDwellStateMachine.cs",
    "client/OmniLensXR/Assets/OmniLens/S03/S03ComfortProbe.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialAccessibilityProfile.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialFocusState.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialInputEvent.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialInputRouter.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialLayout.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/SpatialUiTokens.cs",
    "client/OmniLensXR/Assets/OmniLens/S04/Fixtures/S04HomeLauncher.unity",
    "client/OmniLensXR/Assets/OmniLens/S04/Fixtures/S04PanelBackground.mat",
    "client/OmniLensXR/Assets/OmniLens/S04/Fixtures/S04SettingsAccessibility.unity",
    "client/OmniLensXR/Assets/OmniLens/S04/Fixtures/S04StaticFocus.mat",
    "client/OmniLensXR/Assets/OmniLens/S04/Fixtures/S04ThreePanelWorkspace.unity",
    "client/OmniLensXR/Assets/OmniLens/S05/PanelFactory.cs",
    "client/OmniLensXR/Assets/OmniLens/S05/PanelRegistry.cs",
    "client/OmniLensXR/Assets/OmniLens/S05/PanelState.cs",
    "client/OmniLensXR/Assets/OmniLens/S05/S05PanelLifecycleFixture.cs",
    "client/OmniLensXR/Assets/OmniLens/S05/S05PanelLifecycleFixture.unity",
    "client/OmniLensXR/Assets/OmniLens/S05/S05PanelBackground.mat",
    "client/OmniLensXR/Assets/OmniLens/S06/PanelManipulation.cs",
    "client/OmniLensXR/Assets/OmniLens/S06/PanelPlacement.cs",
    "client/OmniLensXR/Assets/OmniLens/S06/S06FatigueProtocol.cs",
    "client/OmniLensXR/Assets/OmniLens/S06/S06PanelBackground.mat",
    "client/OmniLensXR/Assets/OmniLens/S06/S06WorkspaceFixture.cs",
    "client/OmniLensXR/Assets/OmniLens/S06/S06WorkspaceFixture.unity",
    "client/OmniLensXR/Assets/OmniLens/S06/SpatialLocalization.cs",
    "client/OmniLensXR/Assets/OmniLens/S07/AgentActivityTimeline.cs",
    "client/OmniLensXR/Assets/OmniLens/S07/OfflineWorkspaceShell.cs",
    "client/OmniLensXR/Assets/OmniLens/S07/S07PanelBackground.mat",
    "client/OmniLensXR/Assets/OmniLens/S07/S07WorkspaceFixture.cs",
    "client/OmniLensXR/Assets/OmniLens/S07/S07WorkspaceFixture.unity",
    "client/OmniLensXR/Assets/OmniLens/S07/SandboxWorkspaceRepository.cs",
    "client/OmniLensXR/Assets/OmniLens/S07/WorkspacePersistence.cs",
    "client/OmniLensXR/Assets/OmniLens/S08/LocalSceneAssetLoader.cs",
    "client/OmniLensXR/Assets/OmniLens/S08/S08SceneFixture.cs",
    "client/OmniLensXR/Assets/OmniLens/S08/S08SceneFixture.unity",
    "client/OmniLensXR/Assets/OmniLens/S08/S08SceneMaterial.mat",
    "client/OmniLensXR/Assets/OmniLens/S08/SceneManifest.cs",
    "client/OmniLensXR/Assets/OmniLens/S08/SceneSelection.cs",
    "client/OmniLensXR/Assets/StreamingAssets/S08/generated-cube.mesh.json",
    "client/OmniLensXR/Assets/StreamingAssets/S08/scene-manifest-v1.json",
    "client/OmniLensXR/Assets/Plugins/iOS/S03ThermalState.mm",
    "client/OmniLensXR/Packages/manifest.json",
    "client/OmniLensXR/Packages/packages-lock.json",
    "client/OmniLensXR/ProjectSettings/ProjectVersion.txt",
    "client/OmniLensXR/ProjectSettings/UnityConnectSettings.asset",
    "config/toolchain-versions.json",
    "config/s03-viewer-evidence.template.json",
    "contracts/README.md",
    "contracts/health-response.schema.json",
    "docs/architecture/adr/ADR-0001-xr-foundation-toolchain.md",
    "docs/architecture/adr/ADR-0002-spatial-layout-input-foundation.md",
    "docs/architecture/adr/ADR-0003-panel-registry-lifecycle.md",
    "docs/architecture/adr/ADR-0004-placement-localization-fatigue.md",
    "docs/architecture/adr/ADR-0005-workspace-persistence-offline-shell.md",
    "docs/architecture/adr/ADR-0006-local-scene-bundle-and-selection.md",
    "docs/design/S04-spatial-layout-spec.md",
    "docs/device-matrix/environment-inventory.md",
    "docs/evidence/S01/EV-S01-01.md",
    "docs/evidence/S01/EV-S01-02.md",
    "docs/evidence/S01/EV-S01-03.md",
    "docs/evidence/S01/EV-S01-04.md",
    "docs/evidence/S01/toolchain-installation.md",
    "docs/evidence/S02/implementation-plan.md",
    "docs/evidence/S02/EV-S02-01.md",
    "docs/evidence/S02/EV-S02-02.md",
    "docs/evidence/S02/EV-S02-03.md",
    "docs/evidence/S02/EV-S02-03-runtime-smoke.jsonl",
    "docs/evidence/S02/EV-S02-03-runtime-motion.jsonl",
    "docs/evidence/S02/EV-S02-03-runtime-recenter.jsonl",
    "docs/evidence/S02/EV-S02-04.md",
    "docs/evidence/S02/EV-S02-04-runtime.jsonl",
    "docs/evidence/S02/sprint-report.md",
    "docs/evidence/S03/implementation-plan.md",
    "docs/evidence/S03/EV-S03-01.md",
    "docs/evidence/S03/EV-S03-02.md",
    "docs/evidence/S03/EV-S03-03.md",
    "docs/evidence/S03/EV-S03-03-runtime-smoke.jsonl",
    "docs/evidence/S03/EV-S03-04.md",
    "docs/evidence/S03/sprint-report.md",
    "docs/evidence/S03/viewer-qa-runbook.md",
    "docs/evidence/S04/implementation-plan.md",
    "docs/evidence/S04/hardening-plan.md",
    "docs/evidence/S04/EV-S04-01.md",
    "docs/evidence/S04/EV-S04-02.md",
    "docs/evidence/S04/EV-S04-03.md",
    "docs/evidence/S04/EV-S04-04.md",
    "docs/evidence/S04/sprint-report.md",
    "docs/evidence/S05/implementation-plan.md",
    "docs/evidence/S05/EV-S05-01.md",
    "docs/evidence/S05/EV-S05-02.md",
    "docs/evidence/S05/EV-S05-03.md",
    "docs/evidence/S05/EV-S05-04.md",
    "docs/evidence/S05/sprint-report.md",
    "docs/evidence/S06/implementation-plan.md",
    "docs/evidence/S06/EV-S06-01.md",
    "docs/evidence/S06/EV-S06-02.md",
    "docs/evidence/S06/EV-S06-03.md",
    "docs/evidence/S06/EV-S06-04.md",
    "docs/evidence/S06/sprint-report.md",
    "docs/evidence/S07/implementation-plan.md",
    "docs/evidence/S07/EV-S07-01.md",
    "docs/evidence/S07/EV-S07-02.md",
    "docs/evidence/S07/EV-S07-03.md",
    "docs/evidence/S07/EV-S07-04.md",
    "docs/evidence/S07/sprint-report.md",
    "docs/evidence/S08/implementation-plan.md",
    "docs/evidence/S08/EV-S08-01.md",
    "docs/evidence/S08/EV-S08-02.md",
    "docs/evidence/S08/EV-S08-03.md",
    "docs/evidence/S08/EV-S08-04.md",
    "docs/evidence/S08/sprint-report.md",
    "docs/licenses/initial-license-register.md",
    "docs/licenses/S08-generated-cube.md",
    "scripts/run_unity_s01_checks.sh",
    "scripts/run_unity_s02_checks.sh",
    "scripts/collect_s02_device_log.sh",
    "scripts/sanitize_s02_evidence.py",
    "scripts/analyze_s03_comfort_log.py",
    "scripts/collect_s03_comfort_log.sh",
    "scripts/run_unity_s03_checks.sh",
    "scripts/run_unity_s04_checks.sh",
    "scripts/run_unity_s05_checks.sh",
    "scripts/run_unity_s06_checks.sh",
    "scripts/run_unity_s07_checks.sh",
    "scripts/run_unity_s08_checks.sh",
    "scripts/validate_s03_viewer_evidence.py",
    "tests/test_s03_comfort_log.py",
    "tests/test_s03_viewer_evidence.py",
)

REQUIRED_IGNORE_RULES = (
    ".env",
    ".env.*",
    "!.env.example",
    "*.p12",
    "*.mobileprovision",
    "*.key",
    "*.pem",
    "**/Secrets/**",
    "**/UserSettings/**",
)

SECRET_SUFFIXES = {".p12", ".mobileprovision", ".key", ".pem"}
BINARY_SUFFIXES = {".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".pdf"}
EXCLUDED_DIRECTORY_NAMES = {
    ".git",
    ".mypy_cache",
    ".pytest_cache",
    ".ruff_cache",
    ".venv",
    "Build",
    "Builds",
    "Library",
    "Logs",
    "MemoryCaptures",
    "Obj",
    "Temp",
    "UserSettings",
    "__pycache__",
}
SECRET_PATTERNS = (
    re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    re.compile(r"\bAKIA[0-9A-Z]{16}\b"),
    re.compile(
        r"(?i)\b(?:api[_-]?key|access[_-]?token|client[_-]?secret)\b"
        r"\s*[:=]\s*['\"]?[A-Za-z0-9_./+\-=]{16,}"
    ),
)


def project_files(root: Path = ROOT) -> list[Path]:
    files: list[Path] = []
    for path in root.rglob("*"):
        if not path.is_file() or any(
            part in EXCLUDED_DIRECTORY_NAMES for part in path.parts
        ):
            continue
        files.append(path)
    return files


def validate(root: Path = ROOT) -> list[str]:
    errors: list[str] = []

    for relative in REQUIRED_PATHS:
        if not (root / relative).is_file():
            errors.append(f"missing required file: {relative}")

    gitignore_path = root / ".gitignore"
    if gitignore_path.is_file():
        ignore_lines = {
            line.strip()
            for line in gitignore_path.read_text(encoding="utf-8").splitlines()
            if line.strip() and not line.lstrip().startswith("#")
        }
        for rule in REQUIRED_IGNORE_RULES:
            if rule not in ignore_lines:
                errors.append(f"missing .gitignore rule: {rule}")

    manifest_path = root / "config/toolchain-versions.json"
    if manifest_path.is_file():
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            errors.append(f"invalid toolchain manifest JSON: {exc}")
        else:
            expected = {
                "unity": "6000.3.27f1",
                "cardboard": "1.36.0",
                "xcode": "27.1",
            }
            actual = {
                "unity": manifest.get("unity", {}).get("version"),
                "cardboard": manifest.get("cardboard", {}).get("version"),
                "xcode": manifest.get("apple", {}).get("xcode_version"),
            }
            for component, version in expected.items():
                if actual[component] != version:
                    errors.append(
                        f"unexpected {component} pin: {actual[component]!r}; expected {version}"
                    )
            if (
                manifest.get("decision_status")
                != "iphone11_hello_cardboard_verified_viewer_not_verified"
            ):
                errors.append("toolchain decision does not match verified S02 evidence")
            s03 = manifest.get("s03_software_verification", {})
            expected_s03 = {
                "viewer_profile": "blocked_no_physical_viewer",
                "gaze_dwell_automated": "pass_23_editmode_and_3_playmode_cases",
                "gaze_dwell_physical": "not_run_no_viewer",
                "comfort_tooling_automated": "pass",
                "viewer_evidence_validator": "pass_7_of_7_cases",
                "native_thermal_bridge_unsigned_build": "pass",
                "signed_build_install_launch": "pass_iphone11",
                "comfort_runtime_smoke": "partial_11_033_seconds_no_viewer",
                "comfort_20_minute_physical": "not_run_no_viewer",
                "g00": "blocked_pending_s03_physical_evidence",
                "g01": "blocked_pending_viewer_optics_readability_comfort",
            }
            for field, expected_value in expected_s03.items():
                if s03.get(field) != expected_value:
                    errors.append(
                        f"unexpected S03 state for {field}: {s03.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )
            s04 = manifest.get("s04_software_verification", {})
            expected_s04 = {
                "layout_fixtures": "pass_3_curved_kinds_mono_and_stereo",
                "layout_physical_readability": "not_run_no_viewer",
                "gaze_dwell_automated": "pass_30_target_state_and_playmode",
                "gaze_dwell_physical": "not_run_no_viewer",
                "typed_input_automated": "pass",
                "viewer_trigger_controller": "not_run_no_viewer",
                "accessibility_automated": "pass",
                "accessibility_physical": "not_run_no_viewer",
                "unity_tests": "pass_82_editmode_and_6_playmode",
                "unsigned_iphoneos_build": "pass",
                "signed_build_install_launch": "pass_iphone11_process_alive_5_seconds",
                "hardening": "pass_layout_input_and_fixture_failure_paths",
                "g02": "not_run_s07",
            }
            for field, expected_value in expected_s04.items():
                if s04.get(field) != expected_value:
                    errors.append(
                        f"unexpected S04 state for {field}: {s04.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )
            s05 = manifest.get("s05_software_verification", {})
            expected_s05 = {
                "panel_registry": "pass_deterministic_versioned_bounded",
                "panel_factory": (
                    "pass_allowlisted_duplicate_guard_and_pool_cap_4_per_kind"
                ),
                "lifecycle_transitions": (
                    "pass_15_transition_sequence_and_failure_paths"
                ),
                "snapshot_preview": "pass_3_visible_ids_software_preview",
                "unity_tests": "pass_87_editmode_and_11_playmode",
                "unsigned_iphoneos_build": "pass",
                "signed_build_install_launch": (
                    "pass_iphone11_runtime_markers_and_process_alive_5_seconds"
                ),
                "viewer_interaction": "not_run_no_viewer",
                "upstream_s04_dependency": (
                    "partial_physical_viewer_acceptance_pending"
                ),
                "g02": "not_run_s07",
            }
            for field, expected_value in expected_s05.items():
                if s05.get(field) != expected_value:
                    errors.append(
                        f"unexpected S05 state for {field}: {s05.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )
            s06 = manifest.get("s06_software_verification", {})
            expected_s06 = {
                "placement_fixtures": (
                    "pass_20_full_rectangle_fov_snap_and_no_overlap"
                ),
                "manipulation": "pass_typed_move_resize_pin_focus_preserved",
                "localization": ("pass_14_keys_vn_en_fallback_and_locale_formatting"),
                "fatigue_tooling": ("pass_synthetic_600_seconds_physical_not_run"),
                "unity_tests": "pass_94_editmode_and_14_playmode",
                "unsigned_iphoneos_build": "pass",
                "signed_build_install_launch": (
                    "pass_iphone11_invariant_runtime_markers_and_process_alive_5_seconds"
                ),
                "viewer_fov_handles_fatigue": "not_run_no_viewer",
                "upstream_s05_dependency": (
                    "partial_physical_viewer_acceptance_pending"
                ),
                "g02": "not_run_s07",
            }
            for field, expected_value in expected_s06.items():
                if s06.get(field) != expected_value:
                    errors.append(
                        f"unexpected S06 state for {field}: {s06.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )
            s07 = manifest.get("s07_software_verification", {})
            expected_s07 = {
                "layout_persistence": (
                    "pass_v2_roundtrip_v1_migration_authenticated_envelope"
                ),
                "offline_shell": (
                    "pass_local_science_settings_reset_and_backend_agent_denied"
                ),
                "agent_activity_mock": (
                    "pass_30_fixtures_including_empty_and_error"
                ),
                "unity_tests": "pass_104_editmode_and_16_playmode",
                "unsigned_iphoneos_build": "pass",
                "signed_build_install_launch": (
                    "pass_iphone11_restore_markers_and_process_alive_5_seconds"
                ),
                "restart_restore": (
                    "pass_first_launch_false_relaunch_true_three_active_panels"
                ),
                "development_fps_samples": (
                    "measured_22_0_25_7_and_22_2_no_threshold_claim"
                ),
                "viewer_video_interaction": "not_run_no_viewer",
                "upstream_s06_dependency": (
                    "partial_physical_viewer_acceptance_pending"
                ),
                "g02": "not_run_missing_viewer_video_and_interaction",
            }
            for field, expected_value in expected_s07.items():
                if s07.get(field) != expected_value:
                    errors.append(
                        f"unexpected S07 state for {field}: {s07.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )
            s08 = manifest.get("s08_software_verification", {})
            expected_s08 = {
                "manifest_schema": (
                    "pass_v1_hierarchy_targets_lod_units_license_and_budgets"
                ),
                "asset_loader": (
                    "pass_sha256_timeout_provenance_fallback_and_disposable_lease"
                ),
                "scene_selection": (
                    "pass_20_typed_commands_unknown_stale_and_noop_rejected"
                ),
                "baseline_asset": (
                    "generated_cube_444_bytes_8_vertices_12_triangles_0_texture_bytes"
                ),
                "lifecycle_benchmark": (
                    "pass_50_cycles_active_0_peak_1_created_equals_disposed"
                ),
                "device_memory": (
                    "pass_before_77385411_after_77241531_delta_minus_143880_bound_33554432"
                ),
                "unity_tests": "pass_112_editmode_and_20_playmode",
                "unsigned_iphoneos_build": "pass",
                "signed_build_install_launch": (
                    "pass_iphone11_runtime_markers_and_process_alive_5_seconds"
                ),
                "external_assets_downloaded": "none",
                "upstream_s07_dependency": "partial_g02_viewer_video_pending",
                "g03": "not_run_s09",
                "g09": "not_run_s35",
            }
            for field, expected_value in expected_s08.items():
                if s08.get(field) != expected_value:
                    errors.append(
                        f"unexpected S08 state for {field}: {s08.get(field)!r}; "
                        f"expected {expected_value!r}"
                    )

    for path in project_files(root):
        relative = path.relative_to(root)
        if path.name == ".env" or (
            path.name.startswith(".env.") and path.name != ".env.example"
        ):
            errors.append(
                f"secret environment file must not be in repository: {relative}"
            )
        if path.suffix.lower() in SECRET_SUFFIXES:
            errors.append(
                f"credential/signing file must not be in repository: {relative}"
            )
        if path.suffix.lower() in BINARY_SUFFIXES:
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        for pattern in SECRET_PATTERNS:
            if pattern.search(text):
                errors.append(f"possible secret material in: {relative}")
                break

    return errors


def main() -> int:
    errors = validate()
    if errors:
        for error in errors:
            print(f"FAIL: {error}")
        return 1
    print("PASS: required S01-S08 repository files are present")
    print("PASS: manifest preserves device evidence and open physical viewer gates")
    print("PASS: required secret/signing ignore rules are present")
    print("PASS: no prohibited credential files or obvious secret values found")
    return 0


if __name__ == "__main__":
    sys.exit(main())
