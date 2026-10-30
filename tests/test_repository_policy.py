from __future__ import annotations

import json
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from scripts import analyze_s03_comfort_log, check_repository, sanitize_s02_evidence

ROOT = Path(__file__).resolve().parents[1]


class RepositoryPolicyTests(unittest.TestCase):
    def test_offline_repository_check_passes(self) -> None:
        result = subprocess.run(
            [sys.executable, "scripts/check_repository.py"],
            cwd=ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_toolchain_is_pinned_and_device_evidence_is_bounded(self) -> None:
        manifest = json.loads(
            (ROOT / "config/toolchain-versions.json").read_text(encoding="utf-8")
        )
        self.assertEqual(manifest["unity"]["version"], "6000.3.27f1")
        self.assertEqual(manifest["cardboard"]["version"], "1.36.0")
        self.assertEqual(manifest["apple"]["xcode_version"], "27.1")
        self.assertEqual(
            manifest["apple"]["status"],
            "s02_signed_build_install_launch_verified",
        )
        self.assertEqual(manifest["apple"]["ios_simulator_runtime_version"], "27.1")
        self.assertEqual(manifest["apple"]["ios_simulator_runtime_build"], "24A94425")
        self.assertEqual(manifest["apple"]["physical_device_model"], "iPhone 11")
        self.assertEqual(
            manifest["apple"]["physical_device_status"],
            "available_paired_trusted_developer_mode_enabled",
        )
        self.assertEqual(
            manifest["apple"]["signing"],
            "personal_team_automatic_signing_build_install_launch_verified",
        )
        self.assertEqual(manifest["apple"]["wwdr_intermediate"]["version"], "G3")
        self.assertEqual(
            manifest["decision_status"],
            "iphone11_hello_cardboard_verified_viewer_not_verified",
        )
        self.assertEqual(
            manifest["unity"]["status"],
            "project_package_and_iphone11_sample_verified",
        )
        self.assertIn("stereo_verified", manifest["cardboard"]["status"])
        runtime = manifest["s02_runtime_verification"]
        self.assertEqual(runtime["signed_build_install_launch"], "pass")
        self.assertEqual(runtime["hello_cardboard_two_eye_stereo"], "pass")
        self.assertEqual(
            runtime["head_pose_recenter_20_direction"],
            "pass_runtime_log_user_accepted_video_waiver",
        )
        self.assertEqual(
            runtime["camera_permission_deny_retry_allow"],
            "pass_runtime_dialog_retry_settings_allow_and_resume",
        )
        self.assertEqual(runtime["viewer_optics_readability_comfort"], "not_run_s03")
        s03 = manifest["s03_software_verification"]
        self.assertEqual(s03["viewer_profile"], "blocked_no_physical_viewer")
        self.assertEqual(
            s03["gaze_dwell_automated"],
            "pass_23_editmode_and_3_playmode_cases",
        )
        self.assertEqual(s03["gaze_dwell_physical"], "not_run_no_viewer")
        self.assertEqual(s03["signed_build_install_launch"], "pass_iphone11")
        self.assertEqual(s03["viewer_evidence_validator"], "pass_7_of_7_cases")
        self.assertEqual(
            s03["comfort_runtime_smoke"],
            "partial_11_033_seconds_no_viewer",
        )
        self.assertEqual(
            s03["comfort_20_minute_physical"],
            "not_run_no_viewer",
        )
        self.assertEqual(s03["g00"], "blocked_pending_s03_physical_evidence")
        self.assertEqual(
            s03["g01"],
            "blocked_pending_viewer_optics_readability_comfort",
        )
        s04 = manifest["s04_software_verification"]
        self.assertEqual(
            s04["layout_fixtures"],
            "pass_3_curved_kinds_mono_and_stereo",
        )
        self.assertEqual(s04["layout_physical_readability"], "not_run_no_viewer")
        self.assertEqual(
            s04["gaze_dwell_automated"],
            "pass_30_target_state_and_playmode",
        )
        self.assertEqual(s04["gaze_dwell_physical"], "not_run_no_viewer")
        self.assertEqual(s04["typed_input_automated"], "pass")
        self.assertEqual(s04["viewer_trigger_controller"], "not_run_no_viewer")
        self.assertEqual(s04["accessibility_automated"], "pass")
        self.assertEqual(s04["accessibility_physical"], "not_run_no_viewer")
        self.assertEqual(s04["unity_tests"], "pass_82_editmode_and_6_playmode")
        self.assertEqual(s04["unsigned_iphoneos_build"], "pass")
        self.assertEqual(
            s04["signed_build_install_launch"],
            "pass_iphone11_process_alive_5_seconds",
        )
        self.assertEqual(
            s04["hardening"],
            "pass_layout_input_and_fixture_failure_paths",
        )
        self.assertEqual(s04["g02"], "not_run_s07")
        s05 = manifest["s05_software_verification"]
        self.assertEqual(
            s05["panel_registry"],
            "pass_deterministic_versioned_bounded",
        )
        self.assertEqual(
            s05["panel_factory"],
            "pass_allowlisted_duplicate_guard_and_pool_cap_4_per_kind",
        )
        self.assertEqual(
            s05["lifecycle_transitions"],
            "pass_15_transition_sequence_and_failure_paths",
        )
        self.assertEqual(
            s05["snapshot_preview"],
            "pass_3_visible_ids_software_preview",
        )
        self.assertEqual(s05["unity_tests"], "pass_87_editmode_and_11_playmode")
        self.assertEqual(s05["unsigned_iphoneos_build"], "pass")
        self.assertEqual(
            s05["signed_build_install_launch"],
            "pass_iphone11_runtime_markers_and_process_alive_5_seconds",
        )
        self.assertEqual(s05["viewer_interaction"], "not_run_no_viewer")
        self.assertEqual(
            s05["upstream_s04_dependency"],
            "partial_physical_viewer_acceptance_pending",
        )
        self.assertEqual(s05["g02"], "not_run_s07")
        s06 = manifest["s06_software_verification"]
        self.assertEqual(
            s06["placement_fixtures"],
            "pass_20_full_rectangle_fov_snap_and_no_overlap",
        )
        self.assertEqual(
            s06["manipulation"],
            "pass_typed_move_resize_pin_focus_preserved",
        )
        self.assertEqual(
            s06["localization"],
            "pass_14_keys_vn_en_fallback_and_locale_formatting",
        )
        self.assertEqual(
            s06["fatigue_tooling"],
            "pass_synthetic_600_seconds_physical_not_run",
        )
        self.assertEqual(s06["unity_tests"], "pass_94_editmode_and_14_playmode")
        self.assertEqual(s06["unsigned_iphoneos_build"], "pass")
        self.assertEqual(
            s06["signed_build_install_launch"],
            "pass_iphone11_invariant_runtime_markers_and_process_alive_5_seconds",
        )
        self.assertEqual(s06["viewer_fov_handles_fatigue"], "not_run_no_viewer")
        self.assertEqual(
            s06["upstream_s05_dependency"],
            "partial_physical_viewer_acceptance_pending",
        )
        self.assertEqual(s06["g02"], "not_run_s07")
        s07 = manifest["s07_software_verification"]
        self.assertEqual(
            s07["layout_persistence"],
            "pass_v2_roundtrip_v1_migration_authenticated_envelope",
        )
        self.assertEqual(
            s07["offline_shell"],
            "pass_local_science_settings_reset_and_backend_agent_denied",
        )
        self.assertEqual(
            s07["agent_activity_mock"],
            "pass_30_fixtures_including_empty_and_error",
        )
        self.assertEqual(s07["unity_tests"], "pass_104_editmode_and_16_playmode")
        self.assertEqual(s07["unsigned_iphoneos_build"], "pass")
        self.assertEqual(
            s07["signed_build_install_launch"],
            "pass_iphone11_restore_markers_and_process_alive_5_seconds",
        )
        self.assertEqual(
            s07["restart_restore"],
            "pass_first_launch_false_relaunch_true_three_active_panels",
        )
        self.assertEqual(
            s07["development_fps_samples"],
            "measured_22_0_25_7_and_22_2_no_threshold_claim",
        )
        self.assertEqual(s07["viewer_video_interaction"], "not_run_no_viewer")
        self.assertEqual(
            s07["upstream_s06_dependency"],
            "partial_physical_viewer_acceptance_pending",
        )
        self.assertEqual(
            s07["g02"],
            "not_run_missing_viewer_video_and_interaction",
        )
        s08 = manifest["s08_software_verification"]
        self.assertEqual(
            s08["manifest_schema"],
            "pass_v1_hierarchy_targets_lod_units_license_and_budgets",
        )
        self.assertEqual(
            s08["asset_loader"],
            "pass_sha256_timeout_provenance_fallback_and_disposable_lease",
        )
        self.assertEqual(
            s08["scene_selection"],
            "pass_20_typed_commands_unknown_stale_and_noop_rejected",
        )
        self.assertEqual(
            s08["baseline_asset"],
            "generated_cube_444_bytes_8_vertices_12_triangles_0_texture_bytes",
        )
        self.assertEqual(
            s08["lifecycle_benchmark"],
            "pass_50_cycles_active_0_peak_1_created_equals_disposed",
        )
        self.assertEqual(
            s08["device_memory"],
            "pass_before_77385411_after_77241531_delta_minus_143880_bound_33554432",
        )
        self.assertEqual(s08["unity_tests"], "pass_112_editmode_and_20_playmode")
        self.assertEqual(s08["unsigned_iphoneos_build"], "pass")
        self.assertEqual(
            s08["signed_build_install_launch"],
            "pass_iphone11_runtime_markers_and_process_alive_5_seconds",
        )
        self.assertEqual(s08["external_assets_downloaded"], "none")
        self.assertEqual(
            s08["upstream_s07_dependency"],
            "partial_g02_viewer_video_pending",
        )
        self.assertEqual(s08["g03"], "not_run_s09")
        self.assertEqual(s08["g09"], "not_run_s35")

    def test_license_register_blocks_restricted_content(self) -> None:
        register = (ROOT / "docs/licenses/initial-license-register.md").read_text(
            encoding="utf-8"
        )
        self.assertIn("No weights approved or downloaded", register)
        self.assertIn("Blocked from RAG", register)
        self.assertIn("No blanket approval", register)
        self.assertIn("No billing", register)

    def test_backend_security_boundary_is_explicit(self) -> None:
        backend = (ROOT / "backend/README.md").read_text(encoding="utf-8")
        self.assertIn("127.0.0.1", backend)
        self.assertIn("Paid services remain disabled", backend)
        self.assertIn("Never evaluate arbitrary model-generated", backend)

    def test_unity_project_is_generated_and_pinned(self) -> None:
        project_version_path = (
            ROOT / "client/OmniLensXR/ProjectSettings/ProjectVersion.txt"
        )
        project_version = project_version_path.read_text(encoding="utf-8")
        self.assertIn("m_EditorVersion: 6000.3.27f1", project_version)
        self.assertIn("672d562a42a7", project_version)

        manifest = json.loads(
            (ROOT / "client/OmniLensXR/Packages/manifest.json").read_text(
                encoding="utf-8"
            )
        )
        self.assertEqual(
            manifest["dependencies"]["com.google.xr.cardboard"],
            "https://github.com/googlevr/cardboard-xr-plugin.git#v1.36.0",
        )

        package_lock = json.loads(
            (ROOT / "client/OmniLensXR/Packages/packages-lock.json").read_text(
                encoding="utf-8"
            )
        )
        cardboard = package_lock["dependencies"]["com.google.xr.cardboard"]
        self.assertEqual(cardboard["hash"], "7fb2c0e438d2ef686f51cd59b53da16cc07b1b68")

        connect_settings = (
            ROOT / "client/OmniLensXR/ProjectSettings/UnityConnectSettings.asset"
        ).read_text(encoding="utf-8")
        self.assertIn("m_EngineDiagnosticsEnabled: 0", connect_settings)

        client_readme = (ROOT / "client/README.md").read_text(encoding="utf-8")
        normalized = client_readme.replace("**", "")
        self.assertIn("generated by the official Unity CLI", normalized)
        self.assertIn("Cardboard `1.36.0`", normalized)

    def test_missing_repository_artifacts_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            errors = check_repository.validate(Path(directory))
        self.assertTrue(any("missing required file" in error for error in errors))

    def test_invalid_toolchain_pin_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            temporary_root = Path(directory)
            self._copy_minimum_repository(temporary_root)
            manifest_path = temporary_root / "config/toolchain-versions.json"
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["unity"]["version"] = "moving-latest"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            errors = check_repository.validate(temporary_root)
        self.assertTrue(any("unexpected unity pin" in error for error in errors))

    def test_secret_files_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            temporary_root = Path(directory)
            self._copy_minimum_repository(temporary_root)
            (temporary_root / ".env.local").write_text(
                "api_" + "key=" + ("A" * 24) + "\n", encoding="utf-8"
            )
            errors = check_repository.validate(temporary_root)
        self.assertTrue(any("secret environment file" in error for error in errors))
        self.assertTrue(any("possible secret material" in error for error in errors))

    def test_s02_evidence_sanitizer_redacts_local_signing_and_device_data(self) -> None:
        raw = """/Users/example/Library/Developer/Xcode
account=person@example.com
Apple Development: Example Person (A1B2C3D4E5)
DEVELOPMENT_TEAM = A1B2C3D4E5
serial number: SECRET123
UDID=00008110-0011223344556677
destination id=00008110-0011223344556677"""

        sanitized = sanitize_s02_evidence.sanitize(raw)

        for sensitive in (
            "/Users/example",
            "person@example.com",
            "Example Person",
            "A1B2C3D4E5",
            "SECRET123",
            "00008110-0011223344556677",
        ):
            self.assertNotIn(sensitive, sanitized)
        self.assertIn("$USER_HOME", sanitized)
        self.assertIn("<redacted-device-id>", sanitized)

    def test_s02_runtime_smoke_evidence_is_bounded_jsonl(self) -> None:
        path = ROOT / "docs/evidence/S02/EV-S02-03-runtime-smoke.jsonl"
        lines = [
            json.loads(line)
            for line in path.read_text(encoding="utf-8").splitlines()
            if line
        ]

        events = [line["event"] for line in lines]
        self.assertEqual(events.count("pose_checkpoint"), 2)
        self.assertEqual(events.count("recenter_hold_released"), 1)
        self.assertGreaterEqual(events.count("frame_window"), 3)

        serialized = path.read_text(encoding="utf-8")
        self.assertNotIn("/Users/", serialized)
        self.assertNotIn("@", serialized)
        self.assertNotIn("device_id", serialized.lower())

    def test_s02_motion_and_recenter_evidence_is_bounded_jsonl(self) -> None:
        motion_path = ROOT / "docs/evidence/S02/EV-S02-03-runtime-motion.jsonl"
        motion_rows = [
            json.loads(line)
            for line in motion_path.read_text(encoding="utf-8").splitlines()
            if line
        ]
        motion_poses = [row for row in motion_rows if row["event"] == "pose_checkpoint"]
        self.assertEqual(len(motion_poses), 21)
        self.assertEqual(
            [
                int(re.search(r"index=(\d+)", row["details"]).group(1))
                for row in motion_poses
            ],
            list(range(1, 22)),
        )
        self.assertEqual(
            sum(row["event"] == "recenter_hold_released" for row in motion_rows),
            1,
        )

        axes = {"pitch": [], "yaw": [], "roll": []}
        for row in motion_poses[:20]:
            for axis, value in re.findall(
                r"(pitch|yaw|roll)=(-?[0-9.]+)", row["details"]
            ):
                axes[axis].append(float(value))
        self.assertGreater(max(axes["pitch"]) - min(axes["pitch"]), 60.0)
        self.assertGreater(max(axes["yaw"]) - min(axes["yaw"]), 180.0)
        self.assertGreater(max(axes["roll"]) - min(axes["roll"]), 180.0)

        recenter_path = ROOT / "docs/evidence/S02/EV-S02-03-runtime-recenter.jsonl"
        recenter_rows = [
            json.loads(line)
            for line in recenter_path.read_text(encoding="utf-8").splitlines()
            if line
        ]
        recenter_poses = [
            row for row in recenter_rows if row["event"] == "pose_checkpoint"
        ]
        self.assertEqual(len(recenter_poses), 2)
        post_values = {
            axis: float(value)
            for axis, value in re.findall(
                r"(pitch|yaw|roll)=(-?[0-9.]+)", recenter_poses[-1]["details"]
            )
        }
        self.assertLess(abs(post_values["pitch"]), 10.0)
        self.assertLess(abs(post_values["yaw"]), 10.0)

        serialized = motion_path.read_text(encoding="utf-8") + recenter_path.read_text(
            encoding="utf-8"
        )
        self.assertNotIn("/Users/", serialized)
        self.assertNotIn("@", serialized)
        self.assertNotIn("device_id", serialized.lower())

    def test_s03_runtime_smoke_is_bounded_and_incomplete(self) -> None:
        path = ROOT / "docs/evidence/S03/EV-S03-03-runtime-smoke.jsonl"
        summary = analyze_s03_comfort_log.analyze_rows(
            analyze_s03_comfort_log.load_rows(path)
        )

        self.assertFalse(summary.complete)
        self.assertEqual(summary.checkpoints, [0])
        self.assertEqual(summary.frame_windows, 1)
        self.assertEqual(summary.total_frame_samples, 300)
        self.assertEqual(summary.missing_checkpoints, [5, 10, 20])
        self.assertIn("fair", summary.thermal_states)

        serialized = path.read_text(encoding="utf-8")
        self.assertNotIn("/Users/", serialized)
        self.assertNotIn("@", serialized)
        self.assertNotIn("device_id", serialized.lower())

    @staticmethod
    def _copy_minimum_repository(destination: Path) -> None:
        for relative in check_repository.REQUIRED_PATHS:
            source = ROOT / relative
            target = destination / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)


if __name__ == "__main__":
    unittest.main()
