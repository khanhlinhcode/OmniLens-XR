#!/usr/bin/env python3
"""Validate and summarize a sanitized S03 20-minute comfort JSONL log."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

MAX_BYTES = 2_000_000
MAX_ROWS = 10_000
FRAME_WINDOW_SAMPLES = 300
MINIMUM_COVERED_FRAME_SECONDS = 18 * 60
REQUIRED_CHECKPOINTS = frozenset({0, 5, 10, 20})
ALLOWED_THERMAL_STATES = frozenset(
    {"unavailable", "nominal", "fair", "serious", "critical"}
)
ALLOWED_EVENTS = frozenset(
    {
        "startup",
        "comfort_checkpoint",
        "frame_window",
        "frame_window_rejected",
        "background",
        "resume",
        "focus_gained",
        "focus_lost",
    }
)
PRIVATE_PATTERNS = (
    re.compile(r"/Users/"),
    re.compile(r"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", re.IGNORECASE),
    re.compile(r"(?i)\b(?:udid|serial|device_id|team_id)\b"),
)


class ComfortLogError(ValueError):
    """Raised for malformed, unsafe or unbounded comfort evidence."""


@dataclass(frozen=True)
class ComfortSummary:
    complete: bool
    checkpoints: list[int]
    frame_windows: int
    total_frame_samples: int
    covered_frame_seconds: float
    worst_p95_ms: float | None
    worst_max_ms: float | None
    thermal_states: list[str]
    interruptions: list[str]
    rejected_windows: int
    missing_checkpoints: list[int]


def parse_details(details: str) -> dict[str, str]:
    parsed: dict[str, str] = {}
    for item in details.split(","):
        if "=" not in item:
            continue
        key, value = item.split("=", 1)
        parsed[key.strip()] = value.strip()
    return parsed


def load_rows(path: Path) -> list[dict[str, Any]]:
    if not path.is_file():
        raise ComfortLogError(f"missing log: {path}")
    if path.stat().st_size > MAX_BYTES:
        raise ComfortLogError(f"log exceeds {MAX_BYTES} bytes")

    text = path.read_text(encoding="utf-8")
    for pattern in PRIVATE_PATTERNS:
        if pattern.search(text):
            raise ComfortLogError("log contains prohibited private identifier material")

    rows: list[dict[str, Any]] = []
    previous_timestamp = -1.0
    for line_number, line in enumerate(text.splitlines(), start=1):
        if not line.strip():
            continue
        if len(rows) >= MAX_ROWS:
            raise ComfortLogError(f"log exceeds {MAX_ROWS} rows")
        try:
            row = json.loads(line)
        except json.JSONDecodeError as exc:
            raise ComfortLogError(
                f"line {line_number}: invalid JSON: {exc.msg}"
            ) from exc
        if not isinstance(row, dict):
            raise ComfortLogError(f"line {line_number}: row must be an object")
        if set(row) != {"event", "time", "details"}:
            raise ComfortLogError(f"line {line_number}: unexpected row fields")
        event = row["event"]
        timestamp = row["time"]
        details = row["details"]
        if event not in ALLOWED_EVENTS:
            raise ComfortLogError(f"line {line_number}: unknown event {event!r}")
        if (
            not isinstance(timestamp, (int, float))
            or isinstance(timestamp, bool)
            or not math.isfinite(float(timestamp))
            or timestamp < 0
        ):
            raise ComfortLogError(f"line {line_number}: invalid time")
        if float(timestamp) < previous_timestamp:
            raise ComfortLogError(f"line {line_number}: time moved backwards")
        previous_timestamp = float(timestamp)
        if not isinstance(details, str) or len(details) > 2_000:
            raise ComfortLogError(f"line {line_number}: invalid details")
        rows.append(row)

    if not rows:
        raise ComfortLogError("log has no rows")
    return rows


def analyze_rows(rows: list[dict[str, Any]]) -> ComfortSummary:
    startup_rows = [row for row in rows if row["event"] == "startup"]
    if len(startup_rows) != 1:
        raise ComfortLogError("log must contain exactly one startup event")
    startup_time = float(startup_rows[0]["time"])
    startup_details = parse_details(startup_rows[0]["details"])
    if startup_details != {"probe_limit_minutes": "21", "raw_media": "false"}:
        raise ComfortLogError("startup safety contract is missing or invalid")

    checkpoint_times: dict[int, float] = {}
    p95_values: list[float] = []
    max_values: list[float] = []
    thermal_states: set[str] = set()
    interruptions: list[str] = []
    total_frame_samples = 0
    covered_frame_seconds = 0.0
    rejected_windows = 0

    for row in rows:
        details = parse_details(row["details"])
        thermal_state = details.get("thermal_state")
        if thermal_state:
            if thermal_state not in ALLOWED_THERMAL_STATES:
                raise ComfortLogError(f"unknown thermal state: {thermal_state!r}")
            thermal_states.add(thermal_state)

        if row["event"] in {"comfort_checkpoint", "frame_window"} and not thermal_state:
            raise ComfortLogError(f"{row['event']} is missing thermal state")

        if row["event"] in {"background", "focus_lost"}:
            interruptions.append(row["event"])

        if row["event"] == "comfort_checkpoint":
            try:
                minute = int(details["minute"])
            except (KeyError, ValueError) as exc:
                raise ComfortLogError("comfort checkpoint has invalid minute") from exc
            if minute not in REQUIRED_CHECKPOINTS:
                raise ComfortLogError(f"unexpected checkpoint minute: {minute}")
            if minute in checkpoint_times:
                raise ComfortLogError(f"duplicate checkpoint minute: {minute}")
            checkpoint_times[minute] = float(row["time"])

        if row["event"] == "frame_window":
            try:
                elapsed_seconds = float(details["elapsed_seconds"])
                samples = int(details["samples"])
                average = float(details["avg_ms"])
                p50 = float(details["p50_ms"])
                p95 = float(details["p95_ms"])
                minimum = float(details["min_ms"])
                maximum = float(details["max_ms"])
            except (KeyError, ValueError) as exc:
                raise ComfortLogError("frame window is missing numeric fields") from exc
            numeric_values = (
                elapsed_seconds,
                average,
                p50,
                p95,
                minimum,
                maximum,
            )
            if samples != FRAME_WINDOW_SAMPLES or not all(
                math.isfinite(value) for value in numeric_values
            ):
                raise ComfortLogError("frame window has invalid numeric fields")
            if (
                elapsed_seconds < 0
                or minimum <= 0
                or not minimum <= p50 <= p95 <= maximum
                or not minimum <= average <= maximum
            ):
                raise ComfortLogError("frame window percentiles are inconsistent")
            observed_elapsed = float(row["time"]) - startup_time
            if abs(observed_elapsed - elapsed_seconds) > 2.0:
                raise ComfortLogError("frame window elapsed time is inconsistent")
            total_frame_samples += samples
            covered_frame_seconds += samples * average / 1000.0
            p95_values.append(p95)
            max_values.append(maximum)

        if row["event"] == "frame_window_rejected":
            rejected_windows += 1

    if 0 in checkpoint_times:
        run_start = checkpoint_times[0]
        for minute, timestamp in checkpoint_times.items():
            minimum_timestamp = run_start + minute * 60.0 - 1.0
            if timestamp < minimum_timestamp:
                raise ComfortLogError(
                    f"checkpoint {minute} was emitted before its elapsed threshold"
                )

    checkpoints = set(checkpoint_times)
    missing = sorted(REQUIRED_CHECKPOINTS - checkpoints)
    covered_frame_seconds = round(covered_frame_seconds, 3)
    return ComfortSummary(
        complete=(
            not missing
            and bool(p95_values)
            and covered_frame_seconds >= MINIMUM_COVERED_FRAME_SECONDS
            and bool(thermal_states)
            and not interruptions
            and rejected_windows == 0
        ),
        checkpoints=sorted(checkpoints),
        frame_windows=len(p95_values),
        total_frame_samples=total_frame_samples,
        covered_frame_seconds=covered_frame_seconds,
        worst_p95_ms=max(p95_values) if p95_values else None,
        worst_max_ms=max(max_values) if max_values else None,
        thermal_states=sorted(thermal_states),
        interruptions=interruptions,
        rejected_windows=rejected_windows,
        missing_checkpoints=missing,
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("path", type=Path)
    parser.add_argument(
        "--allow-incomplete",
        action="store_true",
        help="return success for a valid partial log while still reporting incomplete",
    )
    arguments = parser.parse_args()
    try:
        summary = analyze_rows(load_rows(arguments.path))
    except (OSError, ComfortLogError) as exc:
        print(f"INVALID: {exc}", file=sys.stderr)
        return 2

    print(json.dumps(asdict(summary), sort_keys=True))
    if summary.complete:
        print(
            "PASS: uninterrupted 0/5/10/20-minute log with at least "
            "18 minutes of measured frame coverage"
        )
        return 0
    print("INCOMPLETE: physical comfort gate remains NOT RUN")
    return 0 if arguments.allow_incomplete else 3


if __name__ == "__main__":
    raise SystemExit(main())
