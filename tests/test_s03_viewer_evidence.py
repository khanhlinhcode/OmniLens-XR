from __future__ import annotations

import copy
import json
import tempfile
import unittest
from pathlib import Path

from scripts import validate_s03_viewer_evidence

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE_PATH = ROOT / "config/s03-viewer-evidence.template.json"


def complete_evidence() -> dict:
    evidence = json.loads(TEMPLATE_PATH.read_text(encoding="utf-8"))
    evidence["viewer"] = {
        "availability": "available",
        "brand_model": "Measured Cardboard-compatible viewer",
        "tray_dimensions_mm": {
            "width": 155.0,
            "height": 80.0,
            "depth": 12.0,
            "max_phone_thickness": 10.0,
        },
        "iphone11_fit": "pass",
        "camera_obstruction": "clear",
        "trigger": {"type": "none", "result": "not_applicable"},
        "lens": {
            "controls_recorded": True,
            "ipd_adjustable": True,
            "focus_adjustable": True,
        },
        "qr_profile": {
            "available": True,
            "source": "Manufacturer-supplied viewer profile",
        },
    }
    evidence["dwell"] = {name: "pass" for name in evidence["dwell"]}
    evidence["comfort"] = {
        "duration_seconds": 1200.0,
        "continuous": True,
        "stopped_early": False,
        "stop_reason": None,
        "temperature_method": "none",
        "checkpoints": [
            {
                "minute": minute,
                "p95_ms": 34.0,
                "max_ms": 40.0,
                "thermal_state": "fair",
                "readability_result": "pass",
                "glare_result": "pass",
                "distortion_result": "pass",
                "symptoms": [],
                "surface_temperature_c": None,
            }
            for minute in (0, 5, 10, 20)
        ],
    }
    evidence["gate"] = {"claimed_result": "pass", "fallback_2d_retained": True}
    return evidence


class S03ViewerEvidenceTests(unittest.TestCase):
    def test_repository_template_is_valid_but_incomplete(self) -> None:
        summary = validate_s03_viewer_evidence.validate_evidence(
            validate_s03_viewer_evidence.load_evidence(TEMPLATE_PATH)
        )

        self.assertFalse(summary.gate_pass_validated)
        self.assertFalse(summary.eligible_for_gate_pass)
        self.assertEqual(summary.claimed_result, "blocked")
        self.assertEqual(summary.checkpoint_minutes, [])

    def test_complete_measured_evidence_allows_gate_pass(self) -> None:
        summary = validate_s03_viewer_evidence.validate_evidence(complete_evidence())

        self.assertTrue(summary.gate_pass_validated)
        self.assertTrue(summary.viewer_complete)
        self.assertTrue(summary.dwell_complete)
        self.assertTrue(summary.comfort_complete)
        self.assertEqual(summary.checkpoint_minutes, [0, 5, 10, 20])

    def test_pass_claim_without_viewer_is_rejected(self) -> None:
        evidence = json.loads(TEMPLATE_PATH.read_text(encoding="utf-8"))
        evidence["gate"]["claimed_result"] = "pass"

        with self.assertRaises(validate_s03_viewer_evidence.ViewerEvidenceError):
            validate_s03_viewer_evidence.validate_evidence(evidence)

    def test_celsius_without_external_thermometer_is_rejected(self) -> None:
        evidence = complete_evidence()
        evidence["comfort"]["checkpoints"][0]["surface_temperature_c"] = 35.0

        with self.assertRaises(validate_s03_viewer_evidence.ViewerEvidenceError):
            validate_s03_viewer_evidence.validate_evidence(evidence)

    def test_duplicate_checkpoint_is_rejected(self) -> None:
        evidence = complete_evidence()
        evidence["comfort"]["checkpoints"][1]["minute"] = 0

        with self.assertRaises(validate_s03_viewer_evidence.ViewerEvidenceError):
            validate_s03_viewer_evidence.validate_evidence(evidence)

    def test_external_thermometer_requires_every_checkpoint_temperature(self) -> None:
        evidence = complete_evidence()
        evidence["comfort"]["temperature_method"] = "external_surface_thermometer"
        for checkpoint in evidence["comfort"]["checkpoints"]:
            checkpoint["surface_temperature_c"] = 35.0

        complete = validate_s03_viewer_evidence.validate_evidence(evidence)
        self.assertTrue(complete.gate_pass_validated)

        evidence["comfort"]["checkpoints"][2]["surface_temperature_c"] = None
        with self.assertRaises(validate_s03_viewer_evidence.ViewerEvidenceError):
            validate_s03_viewer_evidence.validate_evidence(evidence)

    def test_private_identifier_material_is_rejected(self) -> None:
        evidence = copy.deepcopy(complete_evidence())
        evidence["viewer"]["brand_model"] = "owner@example.com"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "viewer-evidence.json"
            path.write_text(json.dumps(evidence), encoding="utf-8")

            with self.assertRaises(validate_s03_viewer_evidence.ViewerEvidenceError):
                validate_s03_viewer_evidence.load_evidence(path)


if __name__ == "__main__":
    unittest.main()
