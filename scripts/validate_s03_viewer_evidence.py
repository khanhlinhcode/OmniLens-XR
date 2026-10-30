#!/usr/bin/env python3
"""Fail-closed validation for future S03 physical viewer evidence."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

MAX_BYTES = 512_000
REQUIRED_MINUTES = frozenset({0, 5, 10, 20})
THERMAL_STATES = frozenset({"unavailable", "nominal", "fair", "serious", "critical"})
RESULTS = frozenset({"not_run", "pass", "fail"})
TRIGGER_TYPES = frozenset(
    {"unknown", "none", "capacitive", "magnetic", "mechanical", "bluetooth"}
)
TRIGGER_RESULTS = frozenset({"not_run", "pass", "fail", "not_applicable"})
SYMPTOMS = frozenset(
    {
        "nausea",
        "dizziness",
        "eye_strain",
        "headache",
        "excessive_heat",
        "unstable_rendering",
        "other",
    }
)
PRIVATE_PATTERNS = (
    re.compile(r"/Users/"),
    re.compile(r"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", re.IGNORECASE),
    re.compile(r"(?i)\b(?:udid|serial|device_id|team_id)\b"),
)


class ViewerEvidenceError(ValueError):
    """Raised when viewer evidence is malformed, inconsistent or unsafe."""


@dataclass(frozen=True)
class ViewerEvidenceSummary:
    gate_pass_validated: bool
    eligible_for_gate_pass: bool
    claimed_result: str
    viewer_complete: bool
    dwell_complete: bool
    comfort_complete: bool
    checkpoint_minutes: list[int]
    missing_requirements: list[str]


def _require_exact_fields(value: Any, expected: set[str], path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise ViewerEvidenceError(f"{path} must be an object")
    actual = set(value)
    if actual != expected:
        missing = sorted(expected - actual)
        extra = sorted(actual - expected)
        raise ViewerEvidenceError(
            f"{path} fields mismatch; missing={missing}, extra={extra}"
        )
    return value


def _enum(value: Any, allowed: frozenset[str], path: str) -> str:
    if not isinstance(value, str) or value not in allowed:
        raise ViewerEvidenceError(f"{path} has an unsupported value")
    return value


def _boolean(value: Any, path: str) -> bool:
    if not isinstance(value, bool):
        raise ViewerEvidenceError(f"{path} must be boolean")
    return value


def _optional_boolean(value: Any, path: str) -> bool | None:
    if value is not None and not isinstance(value, bool):
        raise ViewerEvidenceError(f"{path} must be boolean or null")
    return value


def _number(value: Any, path: str, *, allow_null: bool = False) -> float | None:
    if value is None and allow_null:
        return None
    if (
        not isinstance(value, (int, float))
        or isinstance(value, bool)
        or not math.isfinite(float(value))
    ):
        raise ViewerEvidenceError(f"{path} must be a finite number")
    return float(value)


def load_evidence(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise ViewerEvidenceError(f"missing evidence file: {path}")
    if path.stat().st_size > MAX_BYTES:
        raise ViewerEvidenceError(f"evidence exceeds {MAX_BYTES} bytes")
    text = path.read_text(encoding="utf-8")
    for pattern in PRIVATE_PATTERNS:
        if pattern.search(text):
            raise ViewerEvidenceError(
                "evidence contains prohibited private identifiers"
            )
    try:
        evidence = json.loads(text)
    except json.JSONDecodeError as exc:
        raise ViewerEvidenceError(f"invalid JSON: {exc.msg}") from exc
    if not isinstance(evidence, dict):
        raise ViewerEvidenceError("root must be an object")
    return evidence


def validate_evidence(evidence: dict[str, Any]) -> ViewerEvidenceSummary:
    root = _require_exact_fields(
        evidence,
        {"schema_version", "viewer", "dwell", "comfort", "gate"},
        "root",
    )
    if root["schema_version"] != 1:
        raise ViewerEvidenceError("schema_version must be 1")

    missing: list[str] = []
    viewer_complete = _validate_viewer(root["viewer"], missing)
    dwell_complete = _validate_dwell(root["dwell"], missing)
    comfort_complete, minutes = _validate_comfort(root["comfort"], missing)

    gate = _require_exact_fields(
        root["gate"], {"claimed_result", "fallback_2d_retained"}, "gate"
    )
    claimed_result = _enum(
        gate["claimed_result"],
        frozenset({"not_run", "pass", "fail", "blocked"}),
        "gate.claimed_result",
    )
    fallback_retained = _boolean(
        gate["fallback_2d_retained"], "gate.fallback_2d_retained"
    )
    if not fallback_retained:
        missing.append("gate.fallback_2d_retained")

    eligible = (
        viewer_complete and dwell_complete and comfort_complete and fallback_retained
    )
    if claimed_result == "pass" and not eligible:
        raise ViewerEvidenceError(
            "gate PASS claim lacks complete viewer, dwell, comfort or fallback evidence"
        )

    return ViewerEvidenceSummary(
        gate_pass_validated=claimed_result == "pass" and eligible,
        eligible_for_gate_pass=eligible,
        claimed_result=claimed_result,
        viewer_complete=viewer_complete,
        dwell_complete=dwell_complete,
        comfort_complete=comfort_complete,
        checkpoint_minutes=minutes,
        missing_requirements=sorted(set(missing)),
    )


def _validate_viewer(value: Any, missing: list[str]) -> bool:
    viewer = _require_exact_fields(
        value,
        {
            "availability",
            "brand_model",
            "tray_dimensions_mm",
            "iphone11_fit",
            "camera_obstruction",
            "trigger",
            "lens",
            "qr_profile",
        },
        "viewer",
    )
    availability = _enum(
        viewer["availability"],
        frozenset({"unavailable", "available"}),
        "viewer.availability",
    )
    brand_model = viewer["brand_model"]
    if brand_model is not None and (
        not isinstance(brand_model, str)
        or not brand_model.strip()
        or len(brand_model) > 200
    ):
        raise ViewerEvidenceError("viewer.brand_model must be null or a short string")

    dimensions = _require_exact_fields(
        viewer["tray_dimensions_mm"],
        {"width", "height", "depth", "max_phone_thickness"},
        "viewer.tray_dimensions_mm",
    )
    dimension_values = {
        name: _number(number, f"viewer.tray_dimensions_mm.{name}", allow_null=True)
        for name, number in dimensions.items()
    }
    for name, number in dimension_values.items():
        if number is not None and number <= 0:
            raise ViewerEvidenceError(
                f"viewer.tray_dimensions_mm.{name} must be positive"
            )

    fit = _enum(viewer["iphone11_fit"], RESULTS, "viewer.iphone11_fit")
    camera = _enum(
        viewer["camera_obstruction"],
        frozenset({"not_run", "clear", "obstructed"}),
        "viewer.camera_obstruction",
    )

    trigger = _require_exact_fields(
        viewer["trigger"], {"type", "result"}, "viewer.trigger"
    )
    trigger_type = _enum(trigger["type"], TRIGGER_TYPES, "viewer.trigger.type")
    trigger_result = _enum(trigger["result"], TRIGGER_RESULTS, "viewer.trigger.result")
    if trigger_type == "unknown" and trigger_result != "not_run":
        raise ViewerEvidenceError("unknown trigger must remain not_run")
    if trigger_type == "none" and trigger_result != "not_applicable":
        raise ViewerEvidenceError("missing trigger must use not_applicable result")
    if trigger_type not in {"unknown", "none"} and trigger_result == "not_applicable":
        raise ViewerEvidenceError("present trigger cannot use not_applicable result")

    lens = _require_exact_fields(
        viewer["lens"],
        {"controls_recorded", "ipd_adjustable", "focus_adjustable"},
        "viewer.lens",
    )
    controls_recorded = _boolean(
        lens["controls_recorded"], "viewer.lens.controls_recorded"
    )
    ipd_adjustable = _optional_boolean(
        lens["ipd_adjustable"], "viewer.lens.ipd_adjustable"
    )
    focus_adjustable = _optional_boolean(
        lens["focus_adjustable"], "viewer.lens.focus_adjustable"
    )

    qr = _require_exact_fields(
        viewer["qr_profile"], {"available", "source"}, "viewer.qr_profile"
    )
    qr_available = _boolean(qr["available"], "viewer.qr_profile.available")
    qr_source = qr["source"]
    if qr_source is not None and (
        not isinstance(qr_source, str) or not qr_source.strip() or len(qr_source) > 500
    ):
        raise ViewerEvidenceError("viewer.qr_profile.source is invalid")
    if qr_available != (qr_source is not None):
        raise ViewerEvidenceError("QR availability and source are inconsistent")

    complete = (
        availability == "available"
        and brand_model is not None
        and all(number is not None for number in dimension_values.values())
        and fit == "pass"
        and camera == "clear"
        and (
            (trigger_type == "none" and trigger_result == "not_applicable")
            or (trigger_type not in {"unknown", "none"} and trigger_result == "pass")
        )
        and controls_recorded
        and ipd_adjustable is not None
        and focus_adjustable is not None
        and qr_available
        and qr_source is not None
    )
    if not complete:
        missing.append("viewer.inventory_and_compatibility")
    return complete


def _validate_dwell(value: Any, missing: list[str]) -> bool:
    fields = {
        "selection",
        "cancellation",
        "duplicate_suppression",
        "recenter",
        "progress_readability",
        "reduced_motion_review",
    }
    dwell = _require_exact_fields(value, fields, "dwell")
    results = [_enum(dwell[name], RESULTS, f"dwell.{name}") for name in fields]
    complete = all(result == "pass" for result in results)
    if not complete:
        missing.append("dwell.physical_accessibility_matrix")
    return complete


def _validate_comfort(value: Any, missing: list[str]) -> tuple[bool, list[int]]:
    comfort = _require_exact_fields(
        value,
        {
            "duration_seconds",
            "continuous",
            "stopped_early",
            "stop_reason",
            "temperature_method",
            "checkpoints",
        },
        "comfort",
    )
    duration = _number(comfort["duration_seconds"], "comfort.duration_seconds")
    if duration is None or duration < 0 or duration > 21 * 60:
        raise ViewerEvidenceError("comfort.duration_seconds is outside 0..1260")
    continuous = _boolean(comfort["continuous"], "comfort.continuous")
    stopped_early = _boolean(comfort["stopped_early"], "comfort.stopped_early")
    stop_reason = comfort["stop_reason"]
    if stop_reason is not None and (
        not isinstance(stop_reason, str)
        or not stop_reason.strip()
        or len(stop_reason) > 500
    ):
        raise ViewerEvidenceError("comfort.stop_reason is invalid")
    if stopped_early != (stop_reason is not None):
        raise ViewerEvidenceError("stopped_early and stop_reason are inconsistent")
    temperature_method = _enum(
        comfort["temperature_method"],
        frozenset({"none", "external_surface_thermometer"}),
        "comfort.temperature_method",
    )
    checkpoints = comfort["checkpoints"]
    if not isinstance(checkpoints, list) or len(checkpoints) > 4:
        raise ViewerEvidenceError(
            "comfort.checkpoints must be a list of at most 4 rows"
        )

    minutes: list[int] = []
    all_rows_pass = True
    temperatures_complete = True
    for index, raw_checkpoint in enumerate(checkpoints):
        path = f"comfort.checkpoints[{index}]"
        checkpoint = _require_exact_fields(
            raw_checkpoint,
            {
                "minute",
                "p95_ms",
                "max_ms",
                "thermal_state",
                "readability_result",
                "glare_result",
                "distortion_result",
                "symptoms",
                "surface_temperature_c",
            },
            path,
        )
        minute = checkpoint["minute"]
        if (
            not isinstance(minute, int)
            or isinstance(minute, bool)
            or minute not in REQUIRED_MINUTES
        ):
            raise ViewerEvidenceError(f"{path}.minute is invalid")
        if minute in minutes:
            raise ViewerEvidenceError(f"duplicate comfort checkpoint minute: {minute}")
        minutes.append(minute)

        p95 = _number(checkpoint["p95_ms"], f"{path}.p95_ms")
        maximum = _number(checkpoint["max_ms"], f"{path}.max_ms")
        if p95 is None or maximum is None or p95 <= 0 or maximum < p95:
            raise ViewerEvidenceError(f"{path} frame values are inconsistent")
        _enum(checkpoint["thermal_state"], THERMAL_STATES, f"{path}.thermal_state")
        manual_results = [
            _enum(checkpoint[name], frozenset({"pass", "fail"}), f"{path}.{name}")
            for name in ("readability_result", "glare_result", "distortion_result")
        ]
        symptoms = checkpoint["symptoms"]
        if not isinstance(symptoms, list) or any(
            not isinstance(symptom, str) or symptom not in SYMPTOMS
            for symptom in symptoms
        ):
            raise ViewerEvidenceError(f"{path}.symptoms contains unsupported values")
        if len(symptoms) != len(set(symptoms)):
            raise ViewerEvidenceError(f"{path}.symptoms contains duplicates")

        surface_temperature = _number(
            checkpoint["surface_temperature_c"],
            f"{path}.surface_temperature_c",
            allow_null=True,
        )
        if temperature_method == "none" and surface_temperature is not None:
            raise ViewerEvidenceError(
                "Celsius evidence requires external_surface_thermometer method"
            )
        if surface_temperature is not None and not 0 <= surface_temperature <= 80:
            raise ViewerEvidenceError(f"{path}.surface_temperature_c is implausible")
        if temperature_method == "external_surface_thermometer":
            temperatures_complete = (
                temperatures_complete and surface_temperature is not None
            )

        all_rows_pass = (
            all_rows_pass
            and all(result == "pass" for result in manual_results)
            and not symptoms
        )

    complete = (
        set(minutes) == REQUIRED_MINUTES
        and duration >= 20 * 60
        and continuous
        and not stopped_early
        and stop_reason is None
        and all_rows_pass
        and temperatures_complete
    )
    if not complete:
        missing.append("comfort.continuous_20_minute_protocol")
    return complete, sorted(minutes)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("path", type=Path)
    parser.add_argument(
        "--allow-incomplete",
        action="store_true",
        help="return success for a valid BLOCKED/NOT RUN evidence template",
    )
    arguments = parser.parse_args()
    try:
        summary = validate_evidence(load_evidence(arguments.path))
    except (OSError, ViewerEvidenceError) as exc:
        print(f"INVALID: {exc}", file=sys.stderr)
        return 2

    print(json.dumps(asdict(summary), sort_keys=True))
    if summary.gate_pass_validated:
        print("PASS: physical viewer evidence supports the claimed gate result")
        return 0
    print("INCOMPLETE: G00/G01 remain blocked or not run")
    return 0 if arguments.allow_incomplete else 3


if __name__ == "__main__":
    raise SystemExit(main())
